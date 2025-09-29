using UnityEngine;
using UnityEngine.EventSystems;

public class CircleDragInput : MonoBehaviour,
    IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerUpHandler
{
    #region Singleton
    public static CircleDragInput Instance { get; private set; }
    #endregion

    #region Inspector Fields

    [Header("Movement Target")]
    [SerializeField] public Transform spawnContainer;     // Holds the CURRENT draggable prefab while dragging
    [SerializeField] private Transform droppedContainer;   // Where prefabs go AFTER drop (optional)

    [Header("Refs")]
    public BallSpawner spawner;
    public DragBounds dragBounds;
    [SerializeField] private Transform exemptSpawnChild;

    [Header("Fallback Movement Limits (used if dragBounds is null)")]
    public float minX = -2.5f;
    public float maxX = 2.5f;

    [Header("Drag Settings")]
    [SerializeField] private float minPressTimeBeforeDrop = 0.05f; // seconds
    private DragSmoother dragSmoother = new DragSmoother(0.05f);

    [Header("Spawn Settings")]
    [SerializeField] private float spawnDelay = 0.5f; // seconds between drop and next spawn
    [SerializeField] private Transform entryAnchor;   // move this up/down in Scene view
    [SerializeField] private float entryGap = 0f;     // extra gap above the line
    [SerializeField] private float entryLineWidth = 6f;   // line length in world units
    [SerializeField] private bool centerLineOnSpawnX = true; // center the line on spawnContainer.x

    #endregion

    #region Runtime State

    private Camera cam;
    private CircleDropController activeBall;

    private float pointerDownTime;
    private int pointerDownFrame;
    private int activePointerId = int.MinValue;
    private bool spawnedAfterThisPress;

    // Bounds cache
    private float cachedMinX;
    private float cachedMaxX;
    private bool hasCachedBounds;

    // Next spawn position memory
    private float lastDropX;
    private bool hasLastDropX;

    // Cooldown/spawn coroutine
    private bool isSpawnCooldown = false;
    private Coroutine spawnDelayRoutine;

    // The actual prefab root for the current active ball (never a container)
    private Transform currentPrefabRoot;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        cam = Camera.main;
        CacheBounds();
    }

    public bool HasActiveBall() => activeBall != null;

    public Transform SpawnContainer => spawnContainer;

    #endregion

    #region Public API

    /// <summary>Re-calc movement bounds (e.g., after resize/orientation change).</summary>
    public void RefreshBounds() => CacheBounds();

    /// <summary>
    /// Called when a new draggable ball becomes active.
    /// Parents its prefab root under the spawnContainer so moving the container moves the ball.
    /// </summary>
    public void SetActiveBall(CircleDropController ball)
    {
        if (ball == null) { Debug.LogWarning("SetActiveBall: null"); return; }

        // Resolve prefab root to parent
        var prefabRoot = ResolvePrefabRoot(ball.transform);

        // Make sure there are no leftovers before we assign
        EnsureSpawnContainerHasSingleChild(prefabRoot);

        activeBall = ball;
        currentPrefabRoot = prefabRoot;

        // Align and parent
        spawnContainer.position = new Vector3(
            currentPrefabRoot.position.x,
            spawnContainer.position.y,
            spawnContainer.position.z
        );

        if (currentPrefabRoot.parent != spawnContainer)
            currentPrefabRoot.SetParent(spawnContainer, true);

        AlignActiveToEntryAnchor();
    }

    private void AlignActiveToEntryAnchor()
    {
        if (entryAnchor == null || currentPrefabRoot == null) return;

        // Try get the collider
        var circle = currentPrefabRoot.GetComponentInChildren<CircleCollider2D>(true);
        if (circle == null) return;

        float targetTopY = entryAnchor.position.y + entryGap;

        float radius = circle.radius;
        float scaleY = circle.transform.lossyScale.y; // account for scale
        float ballTopY = currentPrefabRoot.position.y + (radius * scaleY);

        float dy = targetTopY - ballTopY;
        currentPrefabRoot.position += new Vector3(0f, dy, 0f);
    }

    private static bool TryGetWorldBounds(Transform root, out Bounds bounds)
    {
        // Prefer 2D colliders (accurate for circles)
        var col2d = root.GetComponentInChildren<Collider2D>(true);
        if (col2d != null) { bounds = col2d.bounds; return true; }

        // Fallback to renderers if no collider
        var rend = root.GetComponentInChildren<Renderer>(true);
        if (rend != null) { bounds = rend.bounds; return true; }

        bounds = new Bounds(root.position, Vector3.zero);
        return false;
    }

    /// <summary>Clears the active ball if it matches.</summary>
    public void ClearActiveBall(CircleDropController ball)
    {
        if (activeBall == ball) activeBall = null;
    }

    #endregion

    #region EventSystem Handlers

    public void OnPointerDown(PointerEventData eventData)
    {
        if (isSpawnCooldown) return;

        spawnedAfterThisPress = false;
        activePointerId = eventData.pointerId;
        eventData.useDragThreshold = false;

        pointerDownTime = Time.unscaledTime;
        pointerDownFrame = Time.frameCount;

        if (!hasCachedBounds) CacheBounds();
        MoveActiveBallTo(eventData.position, instant: true);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointerId) return;
        // All logic is in OnDrag
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isSpawnCooldown) return;
        if (eventData.pointerId != activePointerId) return;
        if (activeBall == null || !activeBall.IsDraggable()) return;

        var worldPos = cam.ScreenToWorldPoint(new Vector3(eventData.position.x, eventData.position.y, 0f));
        float min = hasCachedBounds ? cachedMinX : minX;
        float max = hasCachedBounds ? cachedMaxX : maxX;

        var t = GetMoveTarget();
        if (t == null) return;

        var pos = t.position;
        pos.x = Mathf.Clamp(worldPos.x, min, max);
        t.position = new Vector3(pos.x, pos.y, t.position.z);
    }

    // Do not drop on end drag; drop only on pointer up
    public void OnEndDrag(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointerId) return;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (isSpawnCooldown) return;
        if (eventData.pointerId != activePointerId) return;

        // Ignore if released outside the window
        if (!IsWithinScreen(eventData.position)) { activePointerId = int.MinValue; return; }

        // Ignore click-through/focus glitches
        if (Time.frameCount == pointerDownFrame) return;
        if (Time.unscaledTime - pointerDownTime < minPressTimeBeforeDrop) return;

        dragSmoother.Reset();
        DropAndSpawn();
    }

    #endregion

    #region Core Logic

    /// <summary>Move the spawn container horizontally to match a screen position.</summary>
    private void MoveActiveBallTo(Vector2 screenPos, bool instant = false)
    {
        if (activeBall == null || !activeBall.IsDraggable()) return;

        var t = GetMoveTarget();
        if (t == null) return;

        var worldPos = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
        float min = hasCachedBounds ? cachedMinX : minX;
        float max = hasCachedBounds ? cachedMaxX : maxX;

        float targetX = Mathf.Clamp(worldPos.x, min, max);

        float finalX = instant ? targetX : dragSmoother.Smooth(t.position.x, targetX);

        t.position = new Vector3(finalX, t.position.y, t.position.z);
    }

    /// <summary>
    /// Record last X, reparent the prefab root out of the spawn container,
    /// call Drop() on the ball, and start the spawn delay.
    /// </summary>
    private void DropAndSpawn()
    {
        if (spawnedAfterThisPress) return;
        spawnedAfterThisPress = true;

        var t = GetMoveTarget();
        if (t != null)
        {
            lastDropX = t.position.x;
            hasLastDropX = true;
        }

        if (activeBall != null && activeBall.IsDraggable())
        {
            // Move ONLY the cached prefab root under droppedContainer (not containers)
            if (currentPrefabRoot != null)
            {
                if (droppedContainer != null)
                    currentPrefabRoot.SetParent(droppedContainer, worldPositionStays: true);
                else
                    currentPrefabRoot.SetParent(null, worldPositionStays: true);
            }

            EnsureSpawnContainerHasSingleChild(null);

            activeBall.Drop();
        }

        activePointerId = int.MinValue;
        activeBall = null;
        currentPrefabRoot = null;

        if (spawner != null)
        {
            if (spawnDelayRoutine != null) StopCoroutine(spawnDelayRoutine);
            isSpawnCooldown = true;
            spawnDelayRoutine = StartCoroutine(SpawnAfterDelay());
        }
    }

    #endregion

    #region Coroutines

    private System.Collections.IEnumerator SpawnAfterDelay()
    {
        if (spawnDelay > 0f)
            yield return new WaitForSeconds(spawnDelay);

        if (hasLastDropX)
            spawner.PromoteFromPreviewAtX(lastDropX);
        else
            spawner.PromoteFromPreview();

        isSpawnCooldown = false;
        spawnDelayRoutine = null;
    }

    #endregion

    #region Helpers

    private void EnsureSpawnContainerHasSingleChild(Transform keepThis = null)
    {
        if (spawnContainer == null) return;

        // Walk all children; keep only the requested one and the exempt art child
        for (int i = spawnContainer.childCount - 1; i >= 0; i--)
        {
            var child = spawnContainer.GetChild(i);

            if (keepThis != null && child == keepThis) continue;                  // keep the active prefab root
            if (exemptSpawnChild != null && child == exemptSpawnChild) continue;  // keep the art/marker

#if UNITY_EDITOR
            Debug.LogWarning($"[CircleDragInput] Removing stray child under spawnContainer: {child.name}");
#endif

            if (droppedContainer != null)
                child.SetParent(droppedContainer, true);
            else
                child.SetParent(null, true);
        }
    }


    private void CacheBounds()
    {
        if (dragBounds != null)
        {
            cachedMinX = dragBounds.MinX;
            cachedMaxX = dragBounds.MaxX;
        }
        else
        {
            cachedMinX = minX;
            cachedMaxX = maxX;
        }
        hasCachedBounds = true;
    }

    private Transform GetMoveTarget() => spawnContainer;

    /// <summary>
    /// Starting from a leaf (e.g., the controller on a child), climb up until the next parent would be
    /// a container; stop there. Ensures we always return the prefab root, never a container.
    /// </summary>
    private Transform ResolvePrefabRoot(Transform leaf)
    {
        var cur = leaf;
        while (cur.parent != null &&
               cur.parent != spawnContainer &&
               cur.parent != droppedContainer)
        {
            cur = cur.parent;
        }
        return cur; // Prefab_Bug_X(Clone)
    }

    private static bool IsWithinScreen(Vector2 p)
        => p.x >= 0 && p.x <= Screen.width && p.y >= 0 && p.y <= Screen.height;

    private void OnDrawGizmosSelected()
    {
        if (entryAnchor == null) return;

        float xCenter = centerLineOnSpawnX && spawnContainer != null
            ? spawnContainer.position.x
            : entryAnchor.position.x;

        float anchorY = entryAnchor.position.y;
        float lineY = anchorY + entryGap; // <-- line follows entryGap

        Vector3 a = new Vector3(xCenter - entryLineWidth * 0.5f, lineY, 0f);
        Vector3 b = new Vector3(xCenter + entryLineWidth * 0.5f, lineY, 0f);

        Gizmos.color = new Color(0f, 0.8f, 1f, 0.9f);
        Gizmos.DrawLine(a, b);
        Gizmos.DrawSphere(a, 0.04f);
        Gizmos.DrawSphere(b, 0.04f);

        // show the offset from anchor to line
        Gizmos.DrawLine(new Vector3(xCenter, anchorY, 0f),
                        new Vector3(xCenter, lineY, 0f));
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (entryAnchor == null) return;

        float xCenter = centerLineOnSpawnX && spawnContainer != null
            ? spawnContainer.position.x
            : entryAnchor.position.x;

        var anchorPos = entryAnchor.position;
        var linePos = new Vector3(xCenter, anchorPos.y + entryGap, anchorPos.z);

        // vertical slider placed on the line; dragging updates entryGap
        var newLinePos = UnityEditor.Handles.Slider(
            linePos, Vector3.up,
            UnityEditor.HandleUtility.GetHandleSize(linePos) * 0.2f,
            UnityEditor.Handles.ConeHandleCap, 0f);

        if (!Mathf.Approximately(newLinePos.y, linePos.y))
        {
            entryGap += newLinePos.y - linePos.y;           // <-- edit the gap
            if (!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(this);
        }

        // also draw the line
        OnDrawGizmosSelected();
    }
#endif

    #endregion

    #region App Focus/Pause

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            activePointerId = int.MinValue;   // cancel without drop
            spawnedAfterThisPress = false;
        }
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            activePointerId = int.MinValue;   // cancel without drop
            spawnedAfterThisPress = false;
        }
    }

    #endregion
}
