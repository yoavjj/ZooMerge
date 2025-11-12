using UnityEngine;

public class EnemyUnit : MonoBehaviour
{
    [SerializeField] private Animator animator;

    private void OnEnable()
    {
        BallEventManager.OnEnemyHit += HandleHit;
        BallEventManager.OnEnemySessionEnded += HandleSessionEnd;
    }

    private void OnDisable()
    {
        BallEventManager.OnEnemyHit -= HandleHit;
        BallEventManager.OnEnemySessionEnded -= HandleSessionEnd;
    }

    private void HandleHit(GameObject hitObject)
    {
        if (hitObject == gameObject)
        {
            animator.SetTrigger("Hit");
        }
    }

    private void HandleSessionEnd()
    {
        Destroy(gameObject, 1.5f);
    }
}
