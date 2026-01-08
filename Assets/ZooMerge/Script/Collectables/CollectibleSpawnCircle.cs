using UnityEngine;

[ExecuteAlways]
public class CollectibleSpawnCircle : MonoBehaviour
{
    [Header("Spawn Circle")]
    public float radius = 100f;
    public int previewPointCount = 8;
    public Color gizmoColor = new Color(0f, 1f, 0f, 0.5f);

    public Vector2 GetPointOnCircle()
    {
        float angle = Random.Range(0f, Mathf.PI * 2f);
        return new Vector2(
            Mathf.Cos(angle) * radius,
            Mathf.Sin(angle) * radius
        );
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Vector3 center = transform.position;

        Vector3 prev = center + new Vector3(radius, 0, 0);
        for (int i = 1; i <= previewPointCount; i++)
        {
            float angle = i / (float)previewPointCount * Mathf.PI * 2f;
            Vector3 next = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
}
