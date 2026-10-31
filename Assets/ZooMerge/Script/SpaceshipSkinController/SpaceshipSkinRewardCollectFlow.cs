using System;
using UnityEngine;
using UnityEngine.UI;

public class SpaceshipSkinRewardCollectFlow
    : SfxBehaviourTirgger
{
    [Header("Skin")]
    [SerializeField]
    private SpaceshipSkinController skinController;

    [SerializeField]
    private SpaceshipSkinRevealAnimator revealAnimator;

    [Header("Choice Buttons")]
    [SerializeField]
    private Button collectButton;

    [SerializeField]
    private Button rejectButton;

    [Header("Choice Animator")]
    [SerializeField]
    private Animator collectAnimator;

    [SerializeField]
    private string collectInTrigger = "In";

    [SerializeField]
    private string collectOutTrigger = "Out";

    private Action<bool> completionCallback;

    private string pendingSkinId;

    private bool isRunning;
    private bool waitingForChoice;

    private void Awake()
    {
        if (collectButton != null)
        {
            collectButton.onClick.AddListener(
                HandleCollectPressed
            );

            collectButton.interactable = false;
        }

        if (rejectButton != null)
        {
            rejectButton.onClick.AddListener(
                HandleRejectPressed
            );

            rejectButton.interactable = false;
        }
    }

    private void OnEnable()
    {
        if (revealAnimator != null)
        {
            revealAnimator.CollectReady +=
                HandleCollectReady;
        }
    }

    private void OnDisable()
    {
        if (revealAnimator != null)
        {
            revealAnimator.CollectReady -=
                HandleCollectReady;
        }
    }

    private void OnDestroy()
    {
        if (collectButton != null)
        {
            collectButton.onClick.RemoveListener(
                HandleCollectPressed
            );
        }

        if (rejectButton != null)
        {
            rejectButton.onClick.RemoveListener(
                HandleRejectPressed
            );
        }
    }

    public void PlayReward(
        string skinId,
        bool allowReject,
        Action<bool> onComplete = null)
    {
        if (isRunning)
        {
            onComplete?.Invoke(false);
            return;
        }

        if (skinController == null ||
            revealAnimator == null)
        {
            onComplete?.Invoke(false);
            return;
        }

        SpaceshipSkinCatalogSO.SkinDefinition definition =
            skinController.GetSkinDefinition(
                skinId
            );

        if (definition == null ||
            definition.spaceshipSprite == null)
        {
            onComplete?.Invoke(false);
            return;
        }

        if (rejectButton != null)
        {
            rejectButton.gameObject.SetActive(
                allowReject
            );
        }

        Sprite oldSprite =
            skinController.CurrentSprite;

        if (oldSprite == null)
        {
            onComplete?.Invoke(false);
            return;
        }

        isRunning = true;
        waitingForChoice = false;

        pendingSkinId =
            skinId;

        completionCallback =
            onComplete;

        SetButtonsInteractable(false);

        bool wasAlreadyUnlocked =
            SpaceshipSkinProgress.IsUnlocked(
                skinId
            );

        // The reward has been RECEIVED,
        // even if the player rejects equipping it.
        SpaceshipSkinProgress.Unlock(
            skinId
        );

        if (!wasAlreadyUnlocked)
        {
            AnalyticsEvents
                .SpaceshipSkinUnlocked(
                    skinId
                );
        }

        bool started =
            revealAnimator.PlayRewardIn(
                oldSprite,
                definition.spaceshipSprite
            );

        if (!started)
        {
            FinishFlow(false);
        }
    }

    private void HandleCollectReady()
    {
        if (!isRunning)
            return;

        waitingForChoice =
            true;

        SetButtonsInteractable(
            true
        );

        if (collectAnimator != null)
        {
            collectAnimator.ResetTrigger(
                collectOutTrigger
            );

            collectAnimator.ResetTrigger(
                collectInTrigger
            );

            collectAnimator.SetTrigger(
                collectInTrigger
            );
        }
    }

    private void HandleCollectPressed()
    {
        if (!isRunning ||
            !waitingForChoice)
        {
            return;
        }

        waitingForChoice =
            false;

        SetButtonsInteractable(
            false
        );

        PlayUiSfx(
            SfxCue.ButtonClick
        );

        PlayChoiceOut();

        SpaceshipSkinProgress.ClearRejected(
            pendingSkinId
        );

        revealAnimator.PlayPreparedReveal(
            HandleClaimFinished
        );
    }

    private void HandleRejectPressed()
    {
        if (!isRunning ||
            !waitingForChoice)
        {
            return;
        }

        waitingForChoice =
            false;

        SetButtonsInteractable(
            false
        );

        PlayUiSfx(
            SfxCue.ButtonClick
        );

        PlayChoiceOut();

        SpaceshipSkinProgress.MarkRejected(
            pendingSkinId
        );

        revealAnimator.PlayReject(
            HandleRejectFinished
        );
    }

    private void HandleClaimFinished()
    {
        if (SpaceshipSkinController.Instance != null)
        {
            SpaceshipSkinController.Instance
                .SelectSkin(
                    pendingSkinId
                );
        }

        FinishFlow(true);
    }

    private void HandleRejectFinished()
    {
        // Do NOT select the new skin.
        // It remains unlocked but the previous
        // spaceship stays equipped.

        FinishFlow(true);
    }

    private void PlayChoiceOut()
    {
        if (collectAnimator == null)
            return;

        collectAnimator.ResetTrigger(
            collectOutTrigger
        );

        collectAnimator.SetTrigger(
            collectOutTrigger
        );
    }

    private void SetButtonsInteractable(
        bool interactable)
    {
        if (collectButton != null)
        {
            collectButton.interactable =
                interactable;
        }

        if (rejectButton != null)
        {
            rejectButton.interactable =
                interactable;
        }
    }

    private void FinishFlow(
        bool success)
    {
        isRunning = false;
        waitingForChoice = false;

        SetButtonsInteractable(
            false
        );

        pendingSkinId =
            null;

        Action<bool> callback =
            completionCallback;

        completionCallback =
            null;

        callback?.Invoke(
            success
        );
    }
}
