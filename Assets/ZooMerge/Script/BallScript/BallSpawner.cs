using UnityEngine;
using UnityEngine.AddressableAssets;
using static BallFactoryAddressables;

public class BallSpawner : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private AddressableInstantiator instantiator;
    [SerializeField] private Transform spawnPoint;                 // where active ball appears
    [SerializeField] private Transform previewContainer;           // shows the "next" ball
    [SerializeField] private BallPicker picker;
    [SerializeField] private Transform ignoredSpawnChild;

    [SerializeField] private float previewScale = 1.0f; // constant scale for all previews

    private SpawnedBall previewBall;

    private GameObject previewGo;
    private BallSet.Entry queuedEntry;
    private string lastPickWhy;

    private bool warmedUp = false;

    public void WarmupPreview()
    {
        if (warmedUp) return;
        warmedUp = true;

        // Generate the first preview while you're still on the main menu.
        PrepareNextPreview();
    }

    public void BeginSession()
    {
        // If not warmed up, do it now.
        if (!warmedUp)
            PrepareNextPreview();

        PromotePreviewToActive(null);

        // Next preview for after the first active ball
        PrepareNextPreview();
    }


    // called by input after a drop (optionally with X override)
    public void PromoteFromPreview() => PromotePreviewAndQueueNext(null);
    public void PromoteFromPreviewAtX(float x) => PromotePreviewAndQueueNext(x);

    // ---------- preview helpers ----------

    private void PrepareNextPreview()
    {
        // clear leftover preview
        if (previewGo != null)
        {
            BallFactoryAddressables.Instance.Despawn(previewGo);
            previewGo = null;
        }

        if (picker != null && picker.TryPickRandomEntry(out var entry, out lastPickWhy))
            queuedEntry = entry;
        else
            queuedEntry = null;

        if (queuedEntry == null || previewContainer == null) return;

        var pos = previewContainer.position;

        // 🔁 Use SpawnEntryWithRefs
        previewBall = BallFactoryAddressables.Instance.SpawnEntryWithRefs(queuedEntry, pos, previewContainer);
        previewGo = previewBall.root;

        if (previewGo != null)
        {
            SetPreviewMode(previewBall, true);
            previewGo.transform.localScale = Vector3.one * previewScale;

            // 🔁 Trigger "New" using cached animator
            previewBall.animator?.SetTrigger("New");
        }
    }

    private void PromotePreviewAndQueueNext(float? overrideX)
    {
        PromotePreviewToActive(overrideX);
        PrepareNextPreview();
    }

    private void PromotePreviewToActive(float? overrideX)
    {
        if (!CanSpawnActiveBall())
            return;

        if (!previewBall.IsValid || queuedEntry == null)
        {
            Debug.LogWarning("PromotePreviewToActive: Invalid preview. Using fallback.");
            SpawnCircleInternal(overrideX);
            return;
        }

        // 🔁 Scale correctly
        float scale = picker.GetScaleForEntry(queuedEntry);
        previewGo.transform.localScale = Vector3.one * scale;

        SetPreviewMode(previewBall, false);

        // Move to active spawn position
        var pos = spawnPoint != null ? spawnPoint.position : transform.position;
        if (overrideX.HasValue) pos.x = overrideX.Value;
        previewGo.transform.position = pos;

        // Set as active under input
        var spawnContainer = CircleDragInput.Instance?.spawnContainer;
        if (spawnContainer != null)
            previewGo.transform.SetParent(spawnContainer, worldPositionStays: true);

        // 🔁 Use cached animator
        var anim = previewBall.animator;
        if (anim != null)
        {
            anim.ResetTrigger("Merged");
            anim.ResetTrigger("New");
            anim.SetTrigger("Hover");
        }

        // 🔁 Use cached controller
        var controller = previewBall.controller;
        if (controller != null)
        {
            controller.PrepareForDrag();
            CircleDragInput.Instance?.SetActiveBall(controller);
            controller.PlayIntroNew();
        }

        // Clear references
        previewGo = null;
        previewBall = default;
        queuedEntry = null;
    }

    private void SetPreviewMode(SpawnedBall ball, bool on)
    {
        // Unity Physics
        if (ball.allRigidbodies != null)
            foreach (var rb2 in ball.allRigidbodies)
                rb2.simulated = !on;

        if (ball.allColliders != null)
            foreach (var col in ball.allColliders)
                col.enabled = !on;

        // ✅ Force idle pose + freeze / unfreeze
        if (ball.controller != null)
            ball.controller.SetPreviewFreeze(on);

        // ✅ Spine Physics Constraints (you can keep this)
        if (ball.controller != null && ball.controller.Spine != null)
        {
            var skeleton = ball.controller.Spine.Skeleton;
            if (skeleton != null && skeleton.PhysicsConstraints != null)
            {
                for (int i = 0; i < skeleton.PhysicsConstraints.Count; i++)
                    skeleton.PhysicsConstraints.Items[i].Mix = on ? 0f : 1f;
            }
        }
    }

    // ---------- legacy spawn (used as fallback) ----------

    public void SpawnCircle() => SpawnCircleInternal(null);
    public void SpawnCircleAtX(float x) => SpawnCircleInternal(x);

    private void SpawnCircleInternal(float? overrideX)
    {
        if (!CanSpawnActiveBall()) return;
        if (instantiator == null)
        {
            Debug.LogError("BallSpawner: No AddressableInstantiator assigned!");
            return;
        }

        var pos = spawnPoint != null ? spawnPoint.position : transform.position;
        if (overrideX.HasValue) pos.x = overrideX.Value;

        SpawnedBall spawned;

        if (picker != null && picker.TryPickRandomEntry(out var entry, out lastPickWhy))
        {
            spawned = BallFactoryAddressables.Instance.SpawnEntryWithRefs(entry, pos, CircleDragInput.Instance?.spawnContainer);
        }
        else
        {
            spawned = BallFactoryAddressables.Instance.SpawnEntryWithRefs(
                new BallSet.Entry { type = BallType.Bug, level = 0 }, pos, CircleDragInput.Instance?.spawnContainer);
        }

#if UNITY_EDITOR
        if (!spawned.IsValid)
        {
            var msg = string.IsNullOrEmpty(lastPickWhy) ? "Picker returned null." : lastPickWhy;
            Debug.LogWarning($"BallSpawner: {msg} Falling back to default _ballPrefab.");
        }
#endif

        if (spawned.IsValid && spawned.controller != null)
        {
            CircleDragInput.Instance?.SetActiveBall(spawned.controller);
            spawned.controller.PlayIntroNew();
        }
    }

    private bool CanSpawnActiveBall()
    {
        var input = CircleDragInput.Instance;
        if (input == null) return true;

        var sc = input.spawnContainer;
        if (sc == null) return true;

        for (int i = 0; i < sc.childCount; i++)
        {
            var child = sc.GetChild(i);
            if (ignoredSpawnChild != null && child == ignoredSpawnChild) continue;
            return false; // anything else means “already have an active ball”
        }
        return true;
    }
}
