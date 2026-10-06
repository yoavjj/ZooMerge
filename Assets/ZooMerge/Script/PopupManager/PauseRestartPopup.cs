using UnityEngine;
using static BallEventManager;

public class PauseRestartPopup : SfxBehaviourTirgger
{
    [SerializeField] private Animator animator;

    [Header("Particle Opacity")]
    [SerializeField] private ParticleSystemRenderer particleRenderer;
    [SerializeField] private Material particleSourceMaterial;
    [SerializeField] private string opacityProperty = "_Opacity";

    [Tooltip("Keyframe this value in the popup animations.")]
    [Range(0f, 1f)]
    [SerializeField] private float particleOpacity = 1f;

    private Material particleRuntimeMaterial;

    private void OnDidApplyAnimationProperties()
    {
        ApplyParticleOpacity();
    }

    private void EnsureParticleMaterial()
    {
        if (particleRenderer == null)
            return;

        if (particleRuntimeMaterial != null)
            return;

        Material baseMaterial = particleSourceMaterial != null
            ? particleSourceMaterial
            : particleRenderer.sharedMaterial;

        if (baseMaterial == null)
            return;

        particleRuntimeMaterial = Instantiate(baseMaterial);
        particleRuntimeMaterial.name = $"{baseMaterial.name}_{gameObject.name}_Runtime";

        particleRenderer.material = particleRuntimeMaterial;
    }

    private void ApplyParticleOpacity()
    {
        if (particleRuntimeMaterial == null)
            return;

        if (!particleRuntimeMaterial.HasProperty(opacityProperty))
            return;

        particleRuntimeMaterial.SetFloat(opacityProperty, particleOpacity);
    }

    private void OnEnable()
    {
        EnsureParticleMaterial();
        ApplyParticleOpacity();

        BallEventManager.OnGameOver += HandleGameOver;
        BallEventManager.OnEnemySessionEnded += HandleEnemySessionEnded;

        PopupManager.OnForceClosePausePopup += CloseFromSystem;
    }

    private void OnDisable()
    {
        BallEventManager.OnGameOver -= HandleGameOver;
        BallEventManager.OnEnemySessionEnded -= HandleEnemySessionEnded;

        PopupManager.OnForceClosePausePopup -= CloseFromSystem;
    }

    private void OnDestroy()
    {
        if (particleRuntimeMaterial != null)
        {
            Destroy(particleRuntimeMaterial);
            particleRuntimeMaterial = null;
        }
    }

    private void HandleGameOver(BallInfo _, GameOverReason __)
    {
        // Simulate resume press
        OnResumeButtonPressed();
    }

    private void HandleEnemySessionEnded()
    {
        OnResumeButtonPressed();
    }

    public void OnResumeButtonPressed()
    {
        BallEventManager.RaiseSessionResumed();

        PlayPauseButtonOut();

        PlayUiSfx(SfxCue.ButtonClick);

        animator.SetTrigger("Out");
        Destroy(gameObject, 1f);
        PopupManager.Instance?.ClearPausePopupReference();
    }

    private void PlayPauseButtonOut()
    {
        SessionManager.Instance?.HidePauseButtonArt();
    }

    public void CloseFromSystem()
    {
        animator?.SetTrigger("Out");
        Destroy(gameObject, 1f);
    }

    public void OnMainMenuButtonPressed()
    {
        AnalyticsEvents.MainMenuExit("pause_menu");

        PlayUiSfx(SfxCue.ButtonClick);

        // ✅ End session UI immediately
        BallEventManager.RaiseReturnToMainMenu();

        PlayPauseButtonOut();

        PopupManager.Instance?.ConfirmReturnToMainMenu();
        animator.SetTrigger("Out");
        Destroy(gameObject, 1f);
        PopupManager.Instance.ClearPausePopupReference();
    }

    public void OnRestartSessionPressed()
    {
        PlayUiSfx(SfxCue.ButtonClick);

        var dropped = CircleDragInput.Instance?.droppedContainer;
        if (dropped != null)
        {
            BallStateSaver.Instance.RestoreState(dropped);
        }

        BallEventManager.RaiseSessionResumed(); // 🆕 Treat restart as a resume

        PopupManager.Instance?.BeginSession(isNewLevel: false, restartmidlevel: true);

        PlayPauseButtonOut();

        animator.SetTrigger("Out");
        Destroy(gameObject, 1.5f);
        PopupManager.Instance?.ClearPausePopupReference();
    }
}
