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
        warmedUp = true;

        // The preview may have been generated before the player completed
        // or changed their selection. Rebuild it when it is no longer valid.
        if (!previewBall.IsValid ||
            queuedEntry == null ||
            picker == null ||
            !picker.IsEntryAllowed(queuedEntry))
        {
            ClearPreview();
            PrepareNextPreview();
        }

        if (!previewBall.IsValid || queuedEntry == null)
        {
            Debug.LogError(
                "[BallSpawner] Cannot begin session because no valid " +
                "selected ball entry could be prepared."
            );

            return;
        }

        bool promoted = PromotePreviewToActive(null);

        if (promoted)
            PrepareNextPreview();
    }

    private void ClearPreview()
    {
        if (previewGo != null &&
            BallFactoryAddressables.Instance != null)
        {
            BallFactoryAddressables.Instance.Despawn(previewGo);
        }

        previewGo = null;
        previewBall = default;
        queuedEntry = null;
    }

    // called by input after a drop (optionally with X override)
    public void PromoteFromPreview() => PromotePreviewAndQueueNext(null);
    public void PromoteFromPreviewAtX(float x) => PromotePreviewAndQueueNext(x);

    // ---------- preview helpers ----------

    private void PrepareNextPreview()
    {
        ClearPreview();

        if (picker == null)
        {
            Debug.LogError(
                "[BallSpawner] Cannot prepare preview: picker is null."
            );
            return;
        }

        if (previewContainer == null)
        {
            Debug.LogError(
                "[BallSpawner] Cannot prepare preview: previewContainer is null."
            );
            return;
        }

        if (BallFactoryAddressables.Instance == null)
        {
            Debug.LogError(
                "[BallSpawner] Cannot prepare preview: " +
                "BallFactoryAddressables.Instance is null."
            );
            return;
        }

        if (!picker.TryPickRandomEntry(
            out BallSet.Entry entry,
            out lastPickWhy))
        {
            Debug.LogWarning(
                $"[BallSpawner] Could not prepare preview: {lastPickWhy}"
            );
            return;
        }

        queuedEntry = entry;

        Vector3 pos = previewContainer.position;

        previewBall =
            BallFactoryAddressables.Instance.SpawnEntryWithRefs(
                queuedEntry,
                pos,
                previewContainer
            );

        previewGo = previewBall.root;

        if (!previewBall.IsValid || previewGo == null)
        {
            Debug.LogWarning(
                $"[BallSpawner] Preview spawn failed for " +
                $"{queuedEntry.type}, level {queuedEntry.level}."
            );

            ClearPreview();
            return;
        }

        SetPreviewMode(previewBall, true);

        previewGo.transform.localScale =
            Vector3.one * previewScale;

        previewBall.animator?.SetTrigger("New");
    }

    private void PromotePreviewAndQueueNext(float? overrideX)
    {
        bool promoted = PromotePreviewToActive(overrideX);

        if (promoted)
            PrepareNextPreview();
    }

    private bool PromotePreviewToActive(float? overrideX)
    {
        if (!CanSpawnActiveBall())
            return false;

        if (picker == null ||
            previewGo == null ||
            !previewBall.IsValid ||
            queuedEntry == null ||
            !picker.IsEntryAllowed(queuedEntry))
        {
            Debug.LogWarning(
                "[BallSpawner] Invalid or no-longer-selected preview; " +
                "attempting filtered fallback."
            );

            ClearPreview();
            return SpawnCircleInternal(overrideX);
        }

        float scale = picker.GetScaleForEntry(queuedEntry);
        previewGo.transform.localScale = Vector3.one * scale;

        SetPreviewMode(previewBall, false);

        Vector3 pos = spawnPoint != null
            ? spawnPoint.position
            : transform.position;

        if (overrideX.HasValue)
            pos.x = overrideX.Value;

        previewGo.transform.position = pos;

        Transform spawnContainer =
            CircleDragInput.Instance?.spawnContainer;

        if (spawnContainer != null)
        {
            previewGo.transform.SetParent(
                spawnContainer,
                worldPositionStays: true
            );
        }

        Animator anim = previewBall.animator;

        if (anim != null)
        {
            anim.ResetTrigger("Merged");
            anim.ResetTrigger("New");
            anim.SetTrigger("Hover");
        }

        var controller = previewBall.controller;

        if (controller != null)
        {
            controller.PrepareForDrag();
            CircleDragInput.Instance?.SetActiveBall(controller);
            controller.PlayIntroNew();
        }

        previewGo = null;
        previewBall = default;
        queuedEntry = null;

        return true;
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

    private bool SpawnCircleInternal(float? overrideX)
    {
        if (!CanSpawnActiveBall())
            return false;

        if (instantiator == null)
        {
            Debug.LogError(
                "[BallSpawner] No AddressableInstantiator assigned."
            );
            return false;
        }

        if (picker == null)
        {
            Debug.LogError(
                "[BallSpawner] No BallPicker assigned."
            );
            return false;
        }

        if (BallFactoryAddressables.Instance == null)
        {
            Debug.LogError(
                "[BallSpawner] BallFactoryAddressables.Instance is null."
            );
            return false;
        }

        Vector3 pos = spawnPoint != null
            ? spawnPoint.position
            : transform.position;

        if (overrideX.HasValue)
            pos.x = overrideX.Value;

        if (!picker.TryPickRandomEntry(
            out BallSet.Entry entry,
            out lastPickWhy))
        {
            Debug.LogError(
                $"[BallSpawner] Could not pick a ball entry. Reason: {lastPickWhy}"
            );
            return false;
        }

        SpawnedBall spawned =
            BallFactoryAddressables.Instance.SpawnEntryWithRefs(
                entry,
                pos,
                CircleDragInput.Instance?.spawnContainer
            );

        if (!spawned.IsValid)
        {
            Debug.LogWarning(
                $"[BallSpawner] Failed to spawn {entry.type}, level {entry.level}."
            );
            return false;
        }

        if (spawned.controller == null)
        {
            Debug.LogWarning(
                $"[BallSpawner] Spawned ball {entry.type}, level {entry.level}, " +
                "but its controller is missing."
            );
            return false;
        }

        CircleDragInput.Instance?.SetActiveBall(spawned.controller);
        spawned.controller.PlayIntroNew();

        return true;
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
