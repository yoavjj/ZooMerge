using UnityEngine;

public class ConfirmPopup : SfxBehaviourTirgger
{
    [Header("SFX")]
    [SerializeField] private SfxCue openSfx;
    [SerializeField] private SfxCue accentSfx;
    [SerializeField] private SfxCue closeSfx;


    public void AE_PlayOpenSfx()
    {
        PlayUiSfx(openSfx);
    }

    public void AE_PlayAccentSfx()
    {
        PlayUiSfx(accentSfx);
    }

    public void AE_PlayCloseSfx()
    {
        PlayUiSfx(closeSfx);
    } 

    public void ButtonClick()
    {
        PlayUiSfx(SfxCue.ButtonClick);
    }
}