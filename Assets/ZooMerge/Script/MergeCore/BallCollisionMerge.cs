using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BallCollisionMerge : MonoBehaviour
{
    [SerializeField] private BallInfo self;
    private MergeCore core;
    private bool initialized;

    private void Awake()
    {
        if (BallFactoryAddressables.Instance != null)
            Init(BallFactoryAddressables.Instance);
        else
            BallFactoryAddressables.OnReady += HandleFactoryReady;
    }

    private void OnDisable() => BallFactoryAddressables.OnReady -= HandleFactoryReady;
    private void HandleFactoryReady(BallFactoryAddressables f) { if (!initialized) Init(f); }
    private void Init(BallFactoryAddressables f) { core = new MergeCore(f); initialized = true; }

    private void OnCollisionEnter2D(Collision2D col)
    {
        if (BallEventManager.MergesBlocked) return;
        
        if (!initialized || core == null)
        {
            //Debug.LogWarning("🚫 Merge skipped: Not initialized or core is null.");
            return;
        }

        if (col.collider.gameObject.layer != LayerMask.NameToLayer("Ball")) return;

        if (!col.collider.TryGetComponent(out BallInfo other) || other == self)
        {
            //Debug.Log("⛔ Merge skipped: Other is null or same as self.");
            return;
        }

        // 🟡 Skip if either ball isn't registered (e.g. still in intro or just spawned)
        if (!BallRegistry.ActiveBalls.Contains(self) || !BallRegistry.ActiveBalls.Contains(other))
        {
            // Skip future checks for these two
            if (!MergeAttemptTracker.HasAlreadySkippedUnregistered(self, other))
            {
#if UNITY_EDITOR
                //Debug.Log($"🕒 Skipped merge check: one or both balls not registered. self={BallRegistry.ActiveBalls.Contains(self)}, other={BallRegistry.ActiveBalls.Contains(other)}");
#endif
                MergeAttemptTracker.MarkSkippedUnregistered(self, other);
            }

            return;
        }

        if (self.DropController != null && self.DropController.IsDraggable())
        {
#if UNITY_EDITOR
            // Only log once per ball pair
            if (MergeAttemptTracker.TryLogSkipOnce(self, self))
                //Debug.Log("⛔ Merge skipped: Self is draggable.");
#endif
                return;
        }

        if (other.DropController != null && other.DropController.IsDraggable())
        {
#if UNITY_EDITOR
            if (MergeAttemptTracker.TryLogSkipOnce(self, other))
                //Debug.Log("⛔ Merge skipped: Other is draggable.");
#endif
                return;
        }

        // 🔴 SKIP if this merge was already attempted and failed
        if (MergeAttemptTracker.HasAlreadyFailed(self, other))
        {
            // 🔴 Only log once per pair
            if (MergeAttemptTracker.TryLogOnce(self, other))
            {
                //Debug.Log($"🛑 Skipping previously failed merge between: {self.name} and {other.name}");
            }

            return;
        }

        // 🟢 Proceed with merge logic
        Vector2 contact = (col.contactCount > 0)
            ? col.GetContact(0).point
            : (Vector2)((self.transform.position + other.transform.position) * 0.5f);
        float z = (self.transform.position.z + other.transform.position.z) * 0.5f;
        var spawnAt = new Vector3(contact.x, contact.y, z);

        bool merged = core.TryMergeAt(self, other, spawnAt);

        if (!merged)
        {
            //Debug.Log($"❌ Merge failed between: {self.name} and {other.name}");
            
            self.DropController?.ReleasePauseBlockIfActive();
            other.DropController?.ReleasePauseBlockIfActive();

            self.DropController?.ApplyFinalPhysicsImmediately();
            other.DropController?.ApplyFinalPhysicsImmediately();
            
            TryApplyFriction(self, other);
            ApplyAntiStackNudge(self, other, col, strength: 0.5f);
        }
        else
        {
            //Debug.Log($"✅ Merge succeeded: {self.name} + {other.name}");
            BallRegistry.Unregister(self);
            BallRegistry.Unregister(other);

            // 🔁 Recycle their sorting orders
            if (self.DropController != null)
                SpineSortingOrderManager.ReleaseOrder(self.DropController.GetAssignedOrder());

            if (other.DropController != null)
                SpineSortingOrderManager.ReleaseOrder(other.DropController.GetAssignedOrder());
        }
    }

    private void ApplyAntiStackNudge(BallInfo a, BallInfo b, Collision2D col, float strength = 0.6f)
    {
        var rbA = a.DropController != null ? a.DropController.Rigidbody : null;
        var rbB = b.DropController != null ? b.DropController.Rigidbody : null;
        
        if (rbA == null || rbB == null) return;

        // Separation direction (mostly sideways)
        Vector2 dir = (Vector2)(a.transform.position - b.transform.position);

        // If almost perfectly vertical stack, force a horizontal split
        if (Mathf.Abs(dir.x) < 0.05f)
            dir.x = Random.value < 0.5f ? -1f : 1f;

        dir.y = 0f;
        dir = dir.normalized;

        // Slight impulse, opposite directions
        rbA.AddForce(dir * strength, ForceMode2D.Impulse);
        rbB.AddForce(-dir * strength, ForceMode2D.Impulse);
    }

    private void TryApplyFriction(BallInfo a, BallInfo b)
    {
        if (a.IsMerging || b.IsMerging) return;

        var controllerA = a.DropController;
        var controllerB = b.DropController;

        if (controllerA != null && a.IsMergeReady) controllerA.AccelerateSettle(0.55f);
        if (controllerB != null && b.IsMergeReady) controllerA.AccelerateSettle(0.55f);
    }

    // (optional) keep this to merge on sustained contact too
    private void OnCollisionStay2D(Collision2D col) => OnCollisionEnter2D(col);
}

