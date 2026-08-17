using System.Collections;
using UnityEngine;

public class ScoreParticleInstance : MonoBehaviour
{
    [SerializeField] private ParticleSystem particles;

    [Header("Soft Stop")]
    [SerializeField, Min(0.01f)] private float emissionFadeDuration = 0.2f;

    private Coroutine stopRoutine;

    public void Play()
    {
        if (particles == null)
            return;

        if (stopRoutine != null)
        {
            StopCoroutine(stopRoutine);
            stopRoutine = null;
        }

        var main = particles.main;
        main.loop = true;

        particles.Clear(true);
        particles.Play(true);
    }

    public void StopSoft()
    {
        if (particles == null)
            return;

        if (stopRoutine != null)
            StopCoroutine(stopRoutine);

        stopRoutine = StartCoroutine(StopSoftRoutine());
    }

    private IEnumerator StopSoftRoutine()
    {
        var main = particles.main;
        var emission = particles.emission;

        main.loop = false;

        float startRate =
            emission.rateOverTime.constant;

        float elapsed = 0f;

        while (elapsed < emissionFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(
                elapsed / emissionFadeDuration
            );

            emission.rateOverTime =
                Mathf.Lerp(startRate, 0f, t);

            yield return null;
        }

        emission.rateOverTime = 0f;

        particles.Stop(
            true,
            ParticleSystemStopBehavior.StopEmitting
        );

        stopRoutine = null;
    }
}