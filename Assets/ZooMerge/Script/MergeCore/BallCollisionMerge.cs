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
            self.DropController?.ApplyFinalPhysicsImmediately();
            other.DropController?.ApplyFinalPhysicsImmediately();
            TryApplyFriction(self, other);
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

