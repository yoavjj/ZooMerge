using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.IMGUI.Controls;
#endif

public class BottomFloorSync : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private RectTransform bottomFloorUI;

    [Header("World")]
    [SerializeField] private Transform worldFloor;

    [Header("Placement Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float screenHeightPercentage = 0.1f;
    [SerializeField] private float worldYOffset = 0f;

    [Header("Debug Gizmos")]
    [SerializeField] private bool showGizmoDebug = true;

    private Camera cam;
    private BoxCollider2D floorCollider;

    private void Awake()
    {
        cam = Camera.main;
        floorCollider = worldFloor?.GetComponent<BoxCollider2D>();
    }

    private void Start()
    {
        Align();
    }

    public void Align()
    {
        if (!cam || !worldFloor || !floorCollider) return;

        Vector3 worldPoint = cam.ViewportToWorldPoint(new Vector3(0.5f, screenHeightPercentage, cam.nearClipPlane));
        float floorTopOffset = floorCollider.bounds.extents.y;

        worldFloor.position = new Vector3(
            worldFloor.position.x,
            worldPoint.y - floorTopOffset + worldYOffset,
            worldFloor.position.z
        );
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!showGizmoDebug) return;

        if (!cam) cam = Camera.main;
        if (!cam || !worldFloor) return;

        Vector3 worldPoint = cam.ViewportToWorldPoint(new Vector3(0.5f, screenHeightPercentage, cam.nearClipPlane));
        float y = worldPoint.y + worldYOffset;

        Gizmos.color = Color.green;
        Gizmos.DrawLine(
            new Vector3(worldFloor.position.x - 10f, y, 0),
            new Vector3(worldFloor.position.x + 10f, y, 0)
        );

        Handles.color = Color.green;
        Handles.Label(
            new Vector3(worldFloor.position.x, y + 0.2f, 0),
            $"Floor Y @ {(screenHeightPercentage * 100f):0}% + {worldYOffset:0.00}"
        );
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            Align();
        }
    }
#endif
}
