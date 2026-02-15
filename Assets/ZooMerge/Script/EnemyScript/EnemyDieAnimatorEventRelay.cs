using UnityEngine;

public class EnemyDieAnimatorEventRelay : MonoBehaviour
{
    public void OnEnemyDieAnimationFinished()
    {
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.HandleEnemyDieAnimationEvent();
        }
    }
}