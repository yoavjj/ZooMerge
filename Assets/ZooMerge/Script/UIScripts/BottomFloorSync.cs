using UnityEngine;

public class BottomFloorSync : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private RectTransform bottomFloorUI;

    [Header("World")]
    [SerializeField] private Transform worldFloor; // sprite with BoxCollider2D

    private Camera cam;
    private BoxCollider2D floorCollider;

    private void Awake()
    {
        cam = Camera.main;
        floorCollider = worldFloor.GetComponent<BoxCollider2D>();
    }

    private void Start()
    {
        Align();
    }

    public void Align()
    {
        if (!bottomFloorUI || !worldFloor || !floorCollider) return;

        // 1️⃣ Get UI bottom edge in screen space
        Vector3[] corners = new Vector3[4];
        bottomFloorUI.GetWorldCorners(corners);

        // corners[0] = bottom-left
        // corners[3] = bottom-right
        float uiBottomScreenY = corners[0].y;

        // 2️⃣ Convert screen Y → viewport Y (0–1)
        float viewportY = uiBottomScreenY / Screen.height;

        // 3️⃣ Convert viewport Y → world Y
        Vector3 worldPoint = cam.ViewportToWorldPoint(new Vector3(
            0.5f,        // X doesn't matter
            viewportY,
            cam.nearClipPlane
        ));

        // 4️⃣ Move world floor so its TOP matches UI bottom
        float floorTopOffset = floorCollider.bounds.extents.y;

        worldFloor.position = new Vector3(
            worldFloor.position.x,
            worldPoint.y - floorTopOffset,
            worldFloor.position.z
        );
    }
}
