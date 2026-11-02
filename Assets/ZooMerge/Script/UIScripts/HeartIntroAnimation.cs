using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class HeartIntroAnimation : SfxBehaviourTirgger
{
    [Header("Animator")]
    [SerializeField] private Animator animator;
    [SerializeField] private float delay = 0.05f;

    [Header("Triggers")]
    [SerializeField] private string addTrigger = "Add";
    [SerializeField] private string ftueTrigger = "Ftue";
    [SerializeField] private string outTrigger = "Out";

    [Header("FTUE")]
    [SerializeField] private Button ftueContinueButton;

    private Coroutine routine;
    private bool blockingFlight;
    private bool ftueActive;
    private bool closingFtue;

    public bool IsBlockingFlight => blockingFlight;

    private void Awake()
    {
        if (ftueContinueButton != null)
            ftueContinueButton.onClick.AddListener(OnFtueContinuePressed);
    }

    private void OnEnable()
    {
        blockingFlight = false;
        ftueActive = false;
        closingFtue = false;

        if (ftueContinueButton != null)
            ftueContinueButton.interactable = true;
    }

    private void OnDestroy()
    {
        if (ftueContinueButton != null)
            ftueContinueButton.onClick.RemoveListener(OnFtueContinuePressed);
    }

    public void PrepareForFlight(bool showFtue)
    {
        if (animator == null)
        {
            blockingFlight = false;
            return;
        }

        if (routine != null)
            StopCoroutine(routine);

        blockingFlight = showFtue;
        ftueActive = showFtue;
        closingFtue = false;

        routine = StartCoroutine(PlayIntroAfterDelay(showFtue));
    }

    private IEnumerator PlayIntroAfterDelay(bool showFtue)
    {
        animator.enabled = true;
        animator.Rebind();
        animator.Update(0f);

        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        if (showFtue)
        {
            animator.ResetTrigger(ftueTrigger);
            animator.SetTrigger(ftueTrigger);

            // It has now actually been presented to the player.
            HeartFeatureProgress.MarkFtueShown();

            Debug.Log("[HeartIntroAnimation] First Heart FTUE shown.");
        }
        else
        {
            animator.ResetTrigger(addTrigger);
            animator.SetTrigger(addTrigger);
        }

        animator.Update(0f);

        routine = null;
    }

    private void OnFtueContinuePressed()
    {
        if (!ftueActive || closingFtue)
            return;

        closingFtue = true;

        if (ftueContinueButton != null)
            ftueContinueButton.interactable = false;

        if (animator == null || string.IsNullOrEmpty(outTrigger))
        {
            AE_FtueOutFinished();
            return;
        }

        animator.ResetTrigger(outTrigger);
        animator.SetTrigger(outTrigger);
    }

    // Animation Event on the LAST frame of the FTUE Out animation.
    public void AE_FtueOutFinished()
    {
        ftueActive = false;
        closingFtue = false;

        // CollectibleFlyService is waiting for this.
        blockingFlight = false;

        Debug.Log("[HeartIntroAnimation] FTUE finished. Heart flight released.");
    }

    private void OnDisable()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        blockingFlight = false;
        ftueActive = false;
        closingFtue = false;
    }
}