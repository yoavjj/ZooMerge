using UnityEngine;
using UnityEngine.UI; // ← Needed for GraphicRaycaster
using static BallEventManager;

public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance { get; private set; }

    [Header("UI Animators")]
    [SerializeField] private Animator topUIAnimator;
    [SerializeField] private Animator bottomUIAnimator;

    [Header("Raycast Control")]
    [SerializeField] private GraphicRaycaster overlayRaycaster; // ← Drag your overlay canvas' raycaster here

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 🔻 Disable raycaster on startup
        if (overlayRaycaster != null)
            overlayRaycaster.enabled = false;
    }

    private void OnEnable()
    {
        OnSessionStarted += TriggerSessionStart;
        OnEnemyDefeatedMidLevel += TriggerSessionEnd;
        OnSessionPaused += TriggerSessionEnd;    // 🆕 Pause disables session UI
        OnSessionResumed += TriggerSessionStart; // 🆕 Resume re-enables session UI
    }

    private void OnDisable()
    {
        OnSessionStarted -= TriggerSessionStart;
        OnEnemyDefeatedMidLevel -= TriggerSessionEnd;
        OnSessionPaused -= TriggerSessionEnd;
        OnSessionResumed -= TriggerSessionStart;
    }

    private void HandleGameOver(BallInfo info, GameOverReason reason)
    {
        TriggerSessionEnd();
    }

    public void TriggerSessionStart()
    {
        topUIAnimator?.SetTrigger("Session_Start");
        bottomUIAnimator?.SetTrigger("Session_Start");

        // ✅ Enable raycaster when session starts
        if (overlayRaycaster != null)
            overlayRaycaster.enabled = true;
    }

    public void TriggerSessionEnd()
    {
        topUIAnimator?.SetTrigger("Session_End");
        bottomUIAnimator?.SetTrigger("Session_End");

        // ❌ Disable raycaster when session ends
        if (overlayRaycaster != null)
            overlayRaycaster.enabled = false;
    }
}
