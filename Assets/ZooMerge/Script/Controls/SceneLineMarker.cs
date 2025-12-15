using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class SceneLineMarker : MonoBehaviour
{
    [Header("Drag Bounds Line")]
    [Tooltip("Half the width of the drag area in world units. Line length = 2 * HalfExtent.")]
    public float halfExtent = 2f;

    [Header("Game Over Line")]
    public bool showGameOverLine = true;
    public float gameOverYOffset = -5f;
    public Color gameOverColor = new Color(1f, 0f, 0f, 1f); // red

    [Header("Gizmo Style")]
    public Color lineColor = new Color(1f, 0.6f, 0f, 1f); // orange
    public float endCapRadius = 0.08f;
    public float lineYOffset = 0f; // vertical offset for the drag bounds line

    [Header("Input Buffer Zone")]
    public Vector2 bufferZoneSize = new Vector2(10f, 2f);
    public Vector2 bufferZoneOffset = new Vector2(0f, -6f);
    public Color bufferZoneColor = new Color(0f, 0.4f, 1f, 0.3f);

    public float MinX => transform.position.x - Mathf.Max(0f, halfExtent);
    public float MaxX => transform.position.x + Mathf.Max(0f, halfExtent);
    public float GameOverY => transform.position.y + gameOverYOffset;

    private void OnValidate()
    {
        if (halfExtent < 0f) halfExtent = 0f;
        if (endCapRadius < 0f) endCapRadius = 0f;
    }

    private void OnDrawGizmos()
    {
        // --- Drag Bounds Line ---
        Vector3 a = new Vector3(MinX, transform.position.y + lineYOffset, 0f);
        Vector3 b = new Vector3(MaxX, transform.position.y + lineYOffset, 0f);

        Gizmos.color = lineColor;
        Gizmos.DrawLine(a, b);
        if (endCapRadius > 0f)
        {
            Gizmos.DrawSphere(a, endCapRadius);
            Gizmos.DrawSphere(b, endCapRadius);
        }

        // --- Game Over Line ---
        if (showGameOverLine)
        {
            float left = Camera.main ? Camera.main.ViewportToWorldPoint(new Vector3(0, 0, 0)).x - 2f : -10f;
            float right = Camera.main ? Camera.main.ViewportToWorldPoint(new Vector3(1, 0, 0)).x + 2f : 10f;

            Vector3 ga = new Vector3(left, GameOverY, 0f);
            Vector3 gb = new Vector3(right, GameOverY, 0f);

            Gizmos.color = gameOverColor;
            Gizmos.DrawLine(ga, gb);

#if UNITY_EDITOR
            Handles.color = gameOverColor;
            EditorGUI.BeginChangeCheck();
            Vector3 handlePos = new Vector3(transform.position.x, GameOverY, 0f);
            Vector3 newHandlePos = Handles.FreeMoveHandle(
                handlePos,
                0.15f,
                Vector3.zero,
                Handles.CircleHandleCap
            );
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(this, "Move GameOver Y Line");
                gameOverYOffset = newHandlePos.y - transform.position.y;
            }
#endif
        }

        Rect bufferRect = BufferZoneWorldRect;
        Gizmos.color = bufferZoneColor;
        Gizmos.DrawCube(bufferRect.center, new Vector3(bufferRect.width, bufferRect.height, 0f));
    }

    public Rect BufferZoneWorldRect
    {
        get
        {
            Vector2 center = (Vector2)transform.position + bufferZoneOffset;
            return new Rect(center - bufferZoneSize * 0.5f, bufferZoneSize);
        }
    }
}
