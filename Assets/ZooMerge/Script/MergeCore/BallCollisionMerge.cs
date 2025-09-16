using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BallCollisionMerge : MonoBehaviour
{
    [SerializeField] private BallInfo self;
    private MergeCore core;
    private bool initialized;

    private void Awake()
    {
        if (BallFactoryAddressables.Instance != null)
            Init(BallFactoryAddressables.Instance);
        else
            BallFactoryAddressables.OnReady += HandleFactoryReady;
    }

    private void OnDisable() => BallFactoryAddressables.OnReady -= HandleFactoryReady;
    private void HandleFactoryReady(BallFactoryAddressables f) { if (!initialized) Init(f); }
    private void Init(BallFactoryAddressables f) { core = new MergeCore(f); initialized = true; }

    private void OnCollisionEnter2D(Collision2D col)
    {
        if (!initialized || core == null) return;

        // Try to get BallInfo
        var other = col.collider.GetComponentInParent<BallInfo>();
        if (other == null || other == self) return;

        // MERGE ATTEMPT
        Vector2 contact = (col.contactCount > 0)
            ? col.GetContact(0).point
            : (Vector2)((self.transform.position + other.transform.position) * 0.5f);
        float z = (self.transform.position.z + other.transform.position.z) * 0.5f;
        var spawnAt = new Vector3(contact.x, contact.y, z);
        core.TryMergeAt(self, other, spawnAt);

        // FRICTION BUMP (skip if either is merging or not Dynamic)
        TryApplyFriction(self, other);

    }

    private void TryApplyFriction(BallInfo a, BallInfo b)
    {
        if (a.IsMerging || b.IsMerging) return;

        var controllerA = a.DropController;
        var controllerB = b.DropController;

        if (controllerA != null && a.IsMergeReady) controllerA.AccelerateSettle(0.55f);
        if (controllerB != null && b.IsMergeReady) controllerA.AccelerateSettle(0.55f);
    }

    // (optional) keep this to merge on sustained contact too
    private void OnCollisionStay2D(Collision2D col) => OnCollisionEnter2D(col);
}

