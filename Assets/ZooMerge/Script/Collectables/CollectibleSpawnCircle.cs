using UnityEngine;
using System.Collections.Generic;

[ExecuteAlways]
public class CollectibleSpawnCircle : MonoBehaviour
{
    [Header("Spawn Circle")]
    public float radius = 100f;
    public int spawnPointCount = 5;
    public Color gizmoColor = new Color(0f, 1f, 0f, 0.5f);
    public bool previewInScene = true;

    private List<Vector2> fixedPoints = new();

    /// <summary>
    /// Call this before spawning to generate fixed, spaced points on the circle.
    /// </summary>
    public List<Vector2> GetFixedSpawnPoints()
    {
        fixedPoints.Clear();

        float angleStep = 360f / spawnPointCount;

        for (int i = 0; i < spawnPointCount; i++)
        {
            float angleRad = Mathf.Deg2Rad * (i * angleStep);
            Vector2 point = new Vector2(
                Mathf.Cos(angleRad),
                Mathf.Sin(angleRad)
            ) * radius;

            fixedPoints.Add(point);
        }

        return fixedPoints;
    }

    private void OnDrawGizmos()
    {
        if (!previewInScene) return;

        RectTransform rect = transform as RectTransform;
        if (rect == null) return;

        Gizmos.color = gizmoColor;

        // 🔑 VERY IMPORTANT: draw in RectTransform local space
        Gizmos.matrix = rect.localToWorldMatrix;

        // Draw circle
        const int segments = 32;
        Vector3 prevPoint = Vector3.right * radius;

        for (int i = 1; i <= segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            Vector3 nextPoint = new Vector3(
                Mathf.Cos(angle),
                Mathf.Sin(angle),
                0f
            ) * radius;

            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }

        // Draw spawn points
        var points = GetFixedSpawnPoints();
        foreach (var point in points)
        {
            Gizmos.DrawSphere((Vector3)point, 6f);
        }

        // Reset matrix so we don’t break other gizmos
        Gizmos.matrix = Matrix4x4.identity;
    }

}
