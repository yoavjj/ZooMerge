using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class CircleDropController : MonoBehaviour
{
    private Rigidbody2D rb;
    private bool isDragging = true;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    private void OnEnable()
    {
        // Auto-register with input panel if it exists
        if (CircleDragInput.Instance != null)
            CircleDragInput.Instance.SetActiveBall(this);
    }

    private void OnDisable()
    {
        if (CircleDragInput.Instance != null)
            CircleDragInput.Instance.ClearActiveBall(this);
    }

    public bool IsDraggable() => isDragging;

    public void Drop()
    {
        if (!isDragging) return;
        isDragging = false;
        rb.bodyType = RigidbodyType2D.Dynamic;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Enclosure"))
            Debug.Log("Hit Enclosure!");
    }
}
