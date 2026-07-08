using System;
using System.Collections.Generic;
using UnityEngine;
using Solo.MOST_IN_ONE;
using TMPro;

public class SettingsPopup : SfxBehaviourTirgger
{
    [Serializable]
    private class SettingToggle
    {
        public string id;
        public Animator animator;
        public bool enabledByDefault = true;

        [HideInInspector]
        public bool isEnabled;
    }

    private const string SOUND_FX_SETTING_ID =
        "SoundFx_Holder";

    private const string MUSIC_SETTING_ID =
    "MusicSetting_Holder";

    private const string HAPTIC_SETTING_ID =
    "HapticSetting_Holder";

    private const string ANALYTICS_SOUND_FX =
    "sound_fx";

    private const string ANALYTICS_MUSIC =
        "music";

    private const string ANALYTICS_HAPTICS =
        "haptics";

    [Header("Setting Buttons")]
    [SerializeField]
    private List<SettingToggle> toggles = new();

    [Header("Trigger Names")]
    [SerializeField]
    private string pressVTrigger = "Press_V";

    [SerializeField]
    private string pressXTrigger = "Press_X";

    [Header("User ID")]
    [SerializeField]
    private TextMeshProUGUI userIdText;

    private void OnEnable()
    {
        UserIdUIHelper.RefreshText(userIdText);
    }

    private void Start()
    {
        InitializeToggleVisuals();
    }

    private void InitializeToggleVisuals()
    {
        for (int i = 0; i < toggles.Count; i++)
        {
            SettingToggle toggle = toggles[i];

            if (toggle == null)
                continue;

            if (toggle.id == SOUND_FX_SETTING_ID &&
                AudioManager.Instance != null)
            {
                toggle.isEnabled =
                    AudioManager.Instance.IsSfxEnabled;
            }
            else if (
                toggle.id == MUSIC_SETTING_ID &&
                AudioManager.Instance != null)
            {
                toggle.isEnabled =
                    AudioManager.Instance.IsMusicEnabled;
            }
            else if (toggle.id == HAPTIC_SETTING_ID)
            {
                toggle.isEnabled =
                    MOST_HapticFeedback.HapticsEnabled;
            }
            else
            {
                toggle.isEnabled =
                    toggle.enabledByDefault;
            }

            ApplyToggleVisual(toggle);
        }
    }

    public void ToggleByIndex(int index)
    {
        if (index < 0 || index >= toggles.Count)
            return;

        SettingToggle toggle = toggles[index];

        if (toggle == null)
            return;

        bool newState = !toggle.isEnabled;

        if (toggle.id == SOUND_FX_SETTING_ID)
        {
            ApplySfxSetting(newState);
        }
        else if (toggle.id == MUSIC_SETTING_ID)
        {
            ApplyMusicSetting(newState);
        }
        else if (toggle.id == HAPTIC_SETTING_ID)
        {
            ApplyHapticSetting(newState);
        }
        else
        {
            PlayUiSfx(SfxCue.ButtonClick);
        }

        toggle.isEnabled = newState;

        ApplyToggleVisual(toggle);
    }

    private void ApplyHapticSetting(bool enabled)
    {
        PlayUiSfx(SfxCue.ButtonClick);

        if (enabled)
        {
            MOST_HapticFeedback.HapticsEnabled = true;

            MOST_HapticFeedback.Generate(
                MOST_HapticFeedback.HapticTypes.LightImpact
            );
        }
        else
        {
            MOST_HapticFeedback.Generate(
                MOST_HapticFeedback.HapticTypes.LightImpact
            );

            MOST_HapticFeedback.HapticsEnabled = false;
        }

        AnalyticsEvents.SettingChanged(
            ANALYTICS_HAPTICS,
            enabled
        );
    }

    private void ApplyMusicSetting(bool enabled)
    {
        PlayUiSfx(SfxCue.ButtonClick);

        AudioManager.Instance?.SetMusicEnabled(
            enabled
        );

        AnalyticsEvents.SettingChanged(
            ANALYTICS_MUSIC,
            enabled
        );
    }

    private void ApplySfxSetting(bool enabled)
    {
        if (enabled)
        {
            AudioManager.Instance?.SetSfxEnabled(true);

            PlayUiSfx(SfxCue.ButtonClick);
        }
        else
        {
            PlayUiSfx(SfxCue.ButtonClick);

            AudioManager.Instance?.SetSfxEnabled(false);
        }

        AnalyticsEvents.SettingChanged(
            ANALYTICS_SOUND_FX,
            enabled
        );
    }

    private void ApplyToggleVisual(
        SettingToggle toggle)
    {
        if (toggle == null ||
            toggle.animator == null)
        {
            return;
        }

        toggle.animator.ResetTrigger(
            pressVTrigger
        );

        toggle.animator.ResetTrigger(
            pressXTrigger
        );

        toggle.animator.SetTrigger(
            toggle.isEnabled
                ? pressVTrigger
                : pressXTrigger
        );
    }

    public void CopyUserIdToClipboard()
    {
        if (!UserIdUIHelper.TryCopyToClipboard())
            return;

        StartCoroutine(
            UserIdUIHelper.FlashCopiedFeedback(
                userIdText
            )
        );
    }
}