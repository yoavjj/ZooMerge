using Unity.Collections;
using UnityEngine;

[RequireComponent(typeof(Transform))]
public class BallInfo : MonoBehaviour
{
    [SerializeField, ReadOnly] private int level = 0;
    [SerializeField, ReadOnly] private BallType type = BallType.Bug;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private CircleDropController dropController;
    public CircleDropController DropController => dropController;

    private float finalLinearDamping;
    private float finalAngularDamping;
    private float gravityStart, gravityEnd;
    public float GravityStart => gravityStart;
    public float GravityEnd => gravityEnd;

    public float FinalLinearDamping => finalLinearDamping;
    public float FinalAngularDamping => finalAngularDamping;

    private bool merging;

    public int Level => level;
    public BallType Type => type;

    public bool IsMerging => merging;
    public bool IsMergeReady => rb != null && rb.bodyType == RigidbodyType2D.Dynamic && !merging;

    private void Awake()
    {
        if (rb == null) rb = GetComponentInChildren<Rigidbody2D>(true);
        if (dropController == null) dropController = GetComponentInChildren<CircleDropController>(true);
    }

    public void Setup(int level, BallType type, float linearDamp, float angularDamp,
                      float gravityStart, float gravityEnd, float uniformScale)
    {
        this.level = level;
        this.type = type;
        this.finalLinearDamping = linearDamp;
        this.finalAngularDamping = angularDamp;
        this.gravityStart = gravityStart;
        this.gravityEnd = gravityEnd;

        // ✅ Apply scale to the prefab root (this object)
        transform.localScale = Vector3.one * uniformScale;

        if (dropController == null)
            dropController = GetComponentInChildren<CircleDropController>(true);

        if (dropController != null)
            dropController.SetPhysics(linearDamp, angularDamp, gravityStart, gravityEnd);
        else
            Debug.LogWarning("[BallInfo] No CircleDropController found to receive physics.");
    }

    public void SetLevel(int lvl) => level = lvl;
    public void BeginMerge() => merging = true;
}
