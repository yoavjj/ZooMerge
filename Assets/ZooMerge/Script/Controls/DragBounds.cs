using UnityEngine;

[ExecuteAlways]
public class DragBounds : MonoBehaviour
{
    [Tooltip("Half the width of the drag area in world units. Line length = 2 * HalfExtent.")]
    public float halfExtent = 2f;

    [Header("Gizmo")]
    public Color lineColor = new Color(1f, 0.6f, 0f, 1f);
    public float endCapRadius = 0.08f;
    public float lineYOffset = 0f; // visual offset if you want it slightly above/below

    public float MinX => transform.position.x - Mathf.Max(0f, halfExtent);
    public float MaxX => transform.position.x + Mathf.Max(0f, halfExtent);

    private void OnValidate()
    {
        if (halfExtent < 0f) halfExtent = 0f;
        if (endCapRadius < 0f) endCapRadius = 0f;
    }

    private void OnDrawGizmos()
    {
        float min = MinX;
        float max = MaxX;

        Vector3 a = new Vector3(min, transform.position.y + lineYOffset, 0f);
        Vector3 b = new Vector3(max, transform.position.y + lineYOffset, 0f);

        Gizmos.color = lineColor;
        Gizmos.DrawLine(a, b);
        if (endCapRadius > 0f)
        {
            Gizmos.DrawSphere(a, endCapRadius);
            Gizmos.DrawSphere(b, endCapRadius);
        }
    }
}
