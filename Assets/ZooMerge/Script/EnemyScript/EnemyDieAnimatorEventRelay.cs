using UnityEngine;

public class EnemyDieAnimatorEventRelay : SfxBehaviourTirgger
{
    public void OnEnemyDieAnimationFinished()
    {
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.HandleEnemyDieAnimationEvent();
        }
    }
}