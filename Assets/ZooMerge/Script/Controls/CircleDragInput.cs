using UnityEngine;
using UnityEngine.EventSystems;

public class CircleDragInput : MonoBehaviour,
    IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerUpHandler /*, ICancelHandler*/
{
    public static CircleDragInput Instance { get; private set; }

    [Header("Refs")]
    public BallSpawner spawner;
    public DragBounds dragBounds;

    private Camera cam;
    private CircleDropController activeBall;

    [Header("Fallback Movement Limits (used if dragBounds is null)")]
    public float minX = -2.5f;
    public float maxX = 2.5f;

    [Header("Drag Settings")]
    [SerializeField] private float minPressTimeBeforeDrop = 0.05f; // seconds

    [Header("Spawn Settings")]
    [SerializeField] private float spawnDelay = 0.5f;
    private bool isSpawnCooldown = false;
    private Coroutine spawnDelayRoutine;

    private float pointerDownTime;
    private int pointerDownFrame;

    private int activePointerId = int.MinValue;
    private bool spawnedAfterThisPress;

    // Cached bounds
    private float cachedMinX;
    private float cachedMaxX;
    private bool hasCachedBounds;

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

    public void RefreshBounds() => CacheBounds();

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

    public void SetActiveBall(CircleDropController ball) => activeBall = ball;
    public void ClearActiveBall(CircleDropController ball) { if (activeBall == ball) activeBall = null; }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (isSpawnCooldown) return;
        spawnedAfterThisPress = false;
        activePointerId = eventData.pointerId;
        eventData.useDragThreshold = false;

        pointerDownTime = Time.unscaledTime;
        pointerDownFrame = Time.frameCount;

        if (!hasCachedBounds) CacheBounds();
        MoveActiveBallTo(eventData.position);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointerId) return;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isSpawnCooldown) return;
        if (eventData.pointerId != activePointerId) return;
        if (activeBall == null || !activeBall.IsDraggable()) return;

        var worldPos = cam.ScreenToWorldPoint(new Vector3(eventData.position.x, eventData.position.y, 0f));
        float min = hasCachedBounds ? cachedMinX : minX;
        float max = hasCachedBounds ? cachedMaxX : maxX;

        var pos = activeBall.transform.position;
        pos.x = Mathf.Clamp(worldPos.x, min, max);
        activeBall.transform.position = new Vector3(pos.x, pos.y, activeBall.transform.position.z);
    }

    // --- CHANGE: do not drop on end drag anymore ---
    public void OnEndDrag(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointerId) return;
        // Do nothing here (prevents premature drop)
    }

    // --- KEEP drop only here ---
    public void OnPointerUp(PointerEventData eventData)
    {
        if (isSpawnCooldown) return;
        if (eventData.pointerId != activePointerId) return;

        // ignore if released outside the window
        if (!IsWithinScreen(eventData.position)) { activePointerId = int.MinValue; return; }

        // ignore if up happened same frame or too quickly (focus/click-through glitch)
        if (Time.frameCount == pointerDownFrame) return;
        if (Time.unscaledTime - pointerDownTime < minPressTimeBeforeDrop) return;

        DropAndSpawn();
    }

    private void MoveActiveBallTo(Vector2 screenPos)
    {
        if (activeBall == null || !activeBall.IsDraggable()) return;

        var worldPos = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
        float min = hasCachedBounds ? cachedMinX : minX;
        float max = hasCachedBounds ? cachedMaxX : maxX;

        var pos = activeBall.transform.position;
        pos.x = Mathf.Clamp(worldPos.x, min, max);
        activeBall.transform.position = new Vector3(pos.x, pos.y, activeBall.transform.position.z);
    }

    private void DropAndSpawn()
    {
        if (spawnedAfterThisPress) return;
        spawnedAfterThisPress = true;

        if (activeBall != null && activeBall.IsDraggable())
            activeBall.Drop();

        activePointerId = int.MinValue;
        activeBall = null;

        if (spawner != null)
        {
            if (spawnDelayRoutine != null) StopCoroutine(spawnDelayRoutine);
            isSpawnCooldown = true;
            spawnDelayRoutine = StartCoroutine(SpawnAfterDelay());
        }
    }

    private System.Collections.IEnumerator SpawnAfterDelay()
    {
        if (spawnDelay > 0f)
            yield return new WaitForSeconds(spawnDelay);

        spawner.SpawnCircle();

        isSpawnCooldown = false;
        spawnDelayRoutine = null;
    }

    private bool IsWithinScreen(Vector2 p)
    {
        return p.x >= 0 && p.x <= Screen.width && p.y >= 0 && p.y <= Screen.height;
    }

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
}
