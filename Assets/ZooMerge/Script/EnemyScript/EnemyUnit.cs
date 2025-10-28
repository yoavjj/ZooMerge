using UnityEngine;

public class EnemyUnit : MonoBehaviour
{
    [SerializeField] private Animator animator;

    private void OnEnable()
    {
        BallEventManager.OnEnemyHit += HandleHit;
    }

    private void OnDisable()
    {
        BallEventManager.OnEnemyHit -= HandleHit;
    }

    private void HandleHit(GameObject hitObject)
    {
        // Only respond if this is the correct enemy
        if (hitObject == gameObject)
        {
            animator.SetTrigger("Hit");
        }
    }
}
