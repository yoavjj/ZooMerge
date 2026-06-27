using UnityEngine;

public abstract class SfxBehaviourTirgger : MonoBehaviour
{
    protected void PlayUiSfx(SfxCue cue)
    {
        AudioManager.Instance?.PlaySfx(cue);
    }

    public void PlayUiSfxFromEvent(SfxCue cue)
    {
        AudioManager.Instance?.PlaySfx(cue);
    }

    protected void PlayRandomMergeSfx()
    {
        AudioManager.Instance?.PlayRandomMergeSfx();
    }

    public void PlayRandomMergeSfxFromEvent()
    {
        AudioManager.Instance?.PlayRandomMergeSfx();
    }

    protected void PlayRandomMergeBlockedSfx(
        float impactVolume = 1f)
    {
        AudioManager.Instance?.PlayRandomMergeBlockedSfx(
            impactVolume
        );
    }

    public void PlayRandomMergeBlockedSfxFromEvent()
    {
        AudioManager.Instance?.PlayRandomMergeBlockedSfx();
    }

    protected void PlayEnemyDefeatSequence()
    {
        AudioManager.Instance?.PlayEnemyDefeatSequence();
    }

    public void PlayEnemyDefeatSequenceFromEvent()
    {
        AudioManager.Instance?.PlayEnemyDefeatSequence();
    }

    protected void PlayRandomEnemyHitSfx()
    {
        AudioManager.Instance?.PlayRandomEnemyHitSfx();
    }

    public void PlayRandomEnemyHitSfxFromEvent()
    {
        AudioManager.Instance?.PlayRandomEnemyHitSfx();
    }

    protected void PlayRandomPopCollectSfx()
    {
        AudioManager.Instance?.PlayRandomPopCollectSfx();
    }

    public void PlayRandomPopCollectSfxFromEvent()
    {
        AudioManager.Instance?.PlayRandomPopCollectSfx();
    }
}