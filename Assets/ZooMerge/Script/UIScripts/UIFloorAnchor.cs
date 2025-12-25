using UnityEngine;

[ExecuteAlways]
public class UIFloorAnchor : MonoBehaviour
{
    public enum FloorDirection { Left, Right, Top, Bottom, Center }

    [Header("Anchor Settings")]
    public FloorDirection floor = FloorDirection.Right;
    public RectTransform target;
    public float padding = 0f;

    [Header("Gizmo Settings")]
    public Color gizmoColor = Color.magenta;
    public float gizmoLength = 100f;
    public Vector3 gizmoOffset = Vector3.zero;
    public float gizmoRotationAngle = 0f;

    public void AlignNow()
    {
        if (target == null) return;

        RectTransform parent = transform as RectTransform;
        if (parent == null) return;

        Vector2 newPos = target.anchoredPosition;
        float halfParentW = parent.rect.width * 0.5f;
        float halfParentH = parent.rect.height * 0.5f;
        float targetW = target.rect.width;
        float targetH = target.rect.height;

        switch (floor)
        {
            case FloorDirection.Left:
                newPos.x = -halfParentW + (targetW * target.pivot.x) + padding;
                break;

            case FloorDirection.Right:
                newPos.x = halfParentW - (targetW * (1f - target.pivot.x)) - padding;
                break;

            case FloorDirection.Center:
                newPos.x = -((target.pivot.x - 0.5f) * targetW);
                newPos.y = -((target.pivot.y - 0.5f) * targetH);
                break;

            case FloorDirection.Top:
                newPos.y = halfParentH - (targetH * (1f - target.pivot.y)) - padding;
                break;

            case FloorDirection.Bottom:
                newPos.y = -halfParentH + (targetH * target.pivot.y) + padding;
                break;
        }

        target.anchoredPosition = newPos;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (target == null) return;

        // Force layout update so we get accurate target size/position in edit mode
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (target == null) return;
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(target);
        };

        Gizmos.color = gizmoColor;

        Vector3 worldPivotPos = target.TransformPoint(GetLocalEdgePosition());
        Vector3 basePos = worldPivotPos + gizmoOffset;

        Vector3 dir = GetDirection();
        dir = Quaternion.Euler(0f, 0f, gizmoRotationAngle) * dir;

        Gizmos.DrawLine(basePos, basePos + dir.normalized * gizmoLength);
    }
#endif

    private Vector3 GetLocalEdgePosition()
    {
        Rect rect = target.rect;
        Vector3 local = Vector3.zero;

        switch (floor)
        {
            case FloorDirection.Left: local = new Vector3(rect.xMin, 0f); break;
            case FloorDirection.Right: local = new Vector3(rect.xMax, 0f); break;
            case FloorDirection.Top: local = new Vector3(0f, rect.yMax); break;
            case FloorDirection.Bottom: local = new Vector3(0f, rect.yMin); break;
            case FloorDirection.Center: local = Vector3.zero; break;
        }

        return local;
    }

    private Vector3 GetDirection()
    {
        switch (floor)
        {
            case FloorDirection.Left: return Vector3.left;
            case FloorDirection.Right: return Vector3.right;
            case FloorDirection.Top: return Vector3.up;
            case FloorDirection.Bottom: return Vector3.down;
            case FloorDirection.Center: return Vector3.right;
        }
        return Vector3.right;
    }
}
