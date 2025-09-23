using UnityEngine;
using UnityEngine.AddressableAssets;

public class BallSpawner : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private AddressableInstantiator instantiator;
    [SerializeField] private Transform spawnPoint;                 // where active ball appears
    [SerializeField] private Transform previewContainer;           // shows the "next" ball
    [SerializeField] private BallPicker picker;
    [SerializeField] private Transform ignoredSpawnChild;

    [SerializeField] private float previewScale = 1.0f; // constant scale for all previews


    private GameObject previewGo;
    private BallSet.Entry queuedEntry;
    private string lastPickWhy;

    public void BeginSession()
    {
        PrepareNextPreview();
        PromotePreviewToActive(null);
        PrepareNextPreview();
    }


    // called by input after a drop (optionally with X override)
    public void PromoteFromPreview() => PromotePreviewAndQueueNext(null);
    public void PromoteFromPreviewAtX(float x) => PromotePreviewAndQueueNext(x);

    // ---------- preview helpers ----------

    private void PrepareNextPreview()
    {
        // clear leftover preview
        if (previewGo != null) { BallFactoryAddressables.Instance.Despawn(previewGo); previewGo = null; }

        if (picker != null && picker.TryPickRandomEntry(out var entry, out lastPickWhy))
            queuedEntry = entry;
        else
            queuedEntry = null;

        if (queuedEntry == null || previewContainer == null) return;

        // spawn under preview container
        var pos = previewContainer.position;
        previewGo = BallFactoryAddressables.Instance.SpawnEntry(queuedEntry, pos, previewContainer);

        if (previewGo != null)
        {
            SetPreviewMode(previewGo, true);

            // 🔹 override to preview scale
            previewGo.transform.localScale = Vector3.one * previewScale;

            // 🔹 play the non-merge intro on the preview
            var anim = previewGo.GetComponentInChildren<Animator>(true);
            if (anim != null)
            {
                anim.ResetTrigger("Merged");
                anim.SetTrigger("New");
            }
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

        // fallback if preview missing
        if (previewGo == null || queuedEntry == null)
        {
            SpawnCircleInternal(overrideX); // legacy path
            return;
        }

        var ballInfo = previewGo.GetComponentInChildren<BallInfo>(true);
        if (ballInfo != null)
        {
            // Reapply full-scale setup
            float scale = picker.GetScaleForEntry(queuedEntry);
            previewGo.transform.localScale = Vector3.one * scale;
        }

        // wake preview to become the active, draggable ball
        SetPreviewMode(previewGo, false);

        // move to spawn position and parent under the actual spawn container
        var pos = spawnPoint != null ? spawnPoint.position : transform.position;
        if (overrideX.HasValue) pos.x = overrideX.Value;

        previewGo.transform.position = pos;

        var spawnContainer = CircleDragInput.Instance?.spawnContainer;
        if (spawnContainer != null)
            previewGo.transform.SetParent(spawnContainer, worldPositionStays: true);

        // 🔹 Play Hover animation on transition to active
        var hoverAnim = previewGo.GetComponentInChildren<Animator>(true);
        if (hoverAnim != null)
        {
            hoverAnim.ResetTrigger("Merged");
            hoverAnim.ResetTrigger("New");
            hoverAnim.SetTrigger("Hover");
        }

        // register with input + play intro
        var controller = previewGo.GetComponentInChildren<CircleDropController>();
        if (controller != null)
        {
            controller.PrepareForDrag();                   // ensure kinematic for drag
            CircleDragInput.Instance?.SetActiveBall(controller);
            controller.PlayIntroNew();
        }

        // consume this preview
        previewGo = null;
        queuedEntry = null;
    }

    private void SetPreviewMode(GameObject go, bool on)
    {
        // no physics / no collisions / no merge while previewing
        foreach (var rb2 in go.GetComponentsInChildren<Rigidbody2D>(true))
            rb2.simulated = !on;

        foreach (var col in go.GetComponentsInChildren<Collider2D>(true))
            col.enabled = !on;

        foreach (var m in go.GetComponentsInChildren<BallCollisionMerge>(true))
            m.enabled = !on;

        var cdc = go.GetComponentInChildren<CircleDropController>(true);
        if (cdc != null) cdc.enabled = !on;  // prevent Awake/logic acting while preview
    }

    // ---------- legacy spawn (used as fallback) ----------

    public void SpawnCircle() => SpawnCircleInternal(null);
    public void SpawnCircleAtX(float x) => SpawnCircleInternal(x);

    private void SpawnCircleInternal(float? overrideX)
    {
        if (!CanSpawnActiveBall()) return;
        if (instantiator == null) { Debug.LogError("BallSpawner: No AddressableInstantiator assigned!"); return; }

        var pos = spawnPoint != null ? spawnPoint.position : transform.position;
        if (overrideX.HasValue) pos.x = overrideX.Value;

        GameObject go = null;
        string why = "";

        if (picker != null && picker.TryPickRandomEntry(out var entry, out why))
            go = BallFactoryAddressables.Instance.SpawnEntry(entry, pos, CircleDragInput.Instance?.spawnContainer);
        else
            go = BallFactoryAddressables.Instance.SpawnLevel(BallType.Bug, 0, pos, CircleDragInput.Instance?.spawnContainer);

#if UNITY_EDITOR
        if (go == null)
        {
            var msg = string.IsNullOrEmpty(why) ? "Picker returned null." : why;
            Debug.LogWarning($"BallSpawner: {msg} Falling back to default _ballPrefab.");
        }
#endif

        if (go != null)
        {
            var controller = go.GetComponentInChildren<CircleDropController>();
            if (controller != null)
            {
                CircleDragInput.Instance?.SetActiveBall(controller);
                controller.PlayIntroNew();
            }
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
