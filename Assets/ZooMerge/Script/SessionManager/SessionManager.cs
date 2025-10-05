using System.Collections.Generic;
using UnityEngine;

public class SessionManager : MonoBehaviour
{
    private bool hasGameOvered = false;

    private void OnEnable()
    {
        BallEventManager.OnGameOver += TriggerGameOver;
    }

    private void OnDisable()
    {
        BallEventManager.OnGameOver -= TriggerGameOver;
    }

    private void TriggerGameOver(BallInfo info)
    {
        Debug.Log($"Game Over triggered by ball {info.name}");
    }

    public void ResetSession()
    {
        hasGameOvered = false;
    }
}

