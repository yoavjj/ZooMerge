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

        Gizmos.color = gizmoColor;
        Vector3 center = transform.position;

        Gizmos.DrawWireSphere(center, radius);

        var points = GetFixedSpawnPoints();
        foreach (var point in points)
        {
            Gizmos.DrawSphere(center + (Vector3)point, 5f);
        }
    }
}
