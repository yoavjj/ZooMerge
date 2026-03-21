using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AsteroidFieldAnimator : MonoBehaviour
{
    [System.Serializable]
    public class AsteroidGroup
    {
        public List<Transform> asteroids;
    }

    [SerializeField] private List<AsteroidGroup> groups = new();

    [SerializeField] private Animator animator;

    [Header("Scale In")]
    [SerializeField] private AnimationCurve scaleInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float scaleInDuration = 0.4f;

    [Header("Stagger")]
    [SerializeField] private float delayBetweenAsteroids = 0.2f;

    [Header("Hover (Loop)")]
    [SerializeField] private float hoverAmplitude = 0.05f;     // local units
    [SerializeField] private float hoverFrequency = 1.5f;      // cycles per second
    [SerializeField] private Vector3 hoverAxis = Vector3.up;   // hover direction

    [Header("Scale Out")]
    [SerializeField] private AnimationCurve scaleOutCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    [SerializeField] private float scaleOutDuration = 0.35f;
    [SerializeField] private bool scaleOutUseStagger = false;
    [SerializeField] private float scaleOutDelayBetweenAsteroids = 0.05f;

    // Track original positions + running coroutines so hover doesn't fight animations
    private readonly Dictionary<Transform, Vector3> baseLocalPos = new();
    private readonly Dictionary<Transform, Coroutine> hoverRoutines = new();
    private readonly Dictionary<Transform, Coroutine> scaleRoutines = new();

    private void Awake()
    {
        CacheBasePositions();
        InitializeAllAsteroidsHidden();
    }

    private void CacheBasePositions()
    {
        baseLocalPos.Clear();

        foreach (var group in groups)
        {
            if (group?.asteroids == null) continue;

            foreach (var t in group.asteroids)
            {
                if (t == null) continue;
                if (!baseLocalPos.ContainsKey(t))
                    baseLocalPos.Add(t, t.localPosition);
            }
        }
    }

    private void InitializeAllAsteroidsHidden()
    {
        foreach (var group in groups)
        {
            if (group?.asteroids == null) continue;

            foreach (var asteroid in group.asteroids)
            {
                if (asteroid == null) continue;
                asteroid.localScale = Vector3.zero;
            }
        }
    }

    // 🎯 Called from Animator Event
    public void PlayGroup(int groupIndex)
    {
        if (groupIndex < 0 || groupIndex >= groups.Count)
            return;

        StartCoroutine(PlayGroupRoutine(groups[groupIndex]));
    }

    private IEnumerator PlayGroupRoutine(AsteroidGroup group)
    {
        if (group?.asteroids == null) yield break;

        // deterministic order (hierarchy order)
        var ordered = new List<Transform>(group.asteroids);
        ordered.RemoveAll(a => a == null);
        ordered.Sort((a, b) => a.GetSiblingIndex().CompareTo(b.GetSiblingIndex()));

        foreach (var asteroid in ordered)
        {
            StartScaleRoutine(asteroid, AnimateScaleIn(asteroid));
            yield return new WaitForSeconds(delayBetweenAsteroids); // delay between STARTS
        }
    }

    // ---- SCALE IN ----
    private IEnumerator AnimateScaleIn(Transform t)
    {
        if (t == null) yield break;

        StopHover(t); // avoid fighting while scaling

        float time = 0f;
        t.localScale = Vector3.zero;

        while (time < scaleInDuration)
        {
            time += Time.deltaTime;
            float normalized = Mathf.Clamp01(time / scaleInDuration);

            float scale = scaleInCurve.Evaluate(normalized);
            t.localScale = Vector3.one * scale;

            yield return null;
        }

        t.localScale = Vector3.one;

        StartHover(t); // start loop after it appears
    }

    // ---- HOVER LOOP ----
    private void StartHover(Transform t)
    {
        if (t == null) return;

        if (!baseLocalPos.ContainsKey(t))
            baseLocalPos[t] = t.localPosition;

        // restart cleanly
        StopHover(t);
        hoverRoutines[t] = StartCoroutine(HoverRoutine(t));
    }

    private void StopHover(Transform t)
    {
        if (t == null) return;

        if (hoverRoutines.TryGetValue(t, out var c) && c != null)
            StopCoroutine(c);

        hoverRoutines.Remove(t);

        // snap back to base position
        if (baseLocalPos.TryGetValue(t, out var basePos))
            t.localPosition = basePos;
    }

    private IEnumerator HoverRoutine(Transform t)
    {
        Vector3 basePos = baseLocalPos[t];

        // slight phase offset so they don't move perfectly in sync
        float phase = Random.value * Mathf.PI * 2f;

        while (t != null)
        {
            float offset = Mathf.Sin((Time.time * hoverFrequency * Mathf.PI * 2f) + phase) * hoverAmplitude;
            t.localPosition = basePos + hoverAxis.normalized * offset;
            yield return null;
        }
    }

    // ---- SCALE OUT (ALL GROUPS) ----
    // 🎯 Call from Animator Event
    public void ScaleOutAll()
    {
        StartCoroutine(ScaleOutAllRoutine());
    }

    private IEnumerator ScaleOutAllRoutine()
    {
        // collect all asteroids (unique, non-null)
        var all = new List<Transform>();
        var seen = new HashSet<Transform>();

        foreach (var group in groups)
        {
            if (group?.asteroids == null) continue;

            foreach (var t in group.asteroids)
            {
                if (t == null) continue;
                if (seen.Add(t)) all.Add(t);
            }
        }

        // deterministic order (hierarchy order)
        all.Sort((a, b) => a.GetSiblingIndex().CompareTo(b.GetSiblingIndex()));

        foreach (var t in all)
        {
            StartScaleRoutine(t, AnimateScaleOut(t));

            if (scaleOutUseStagger)
                yield return new WaitForSeconds(scaleOutDelayBetweenAsteroids); // delay between STARTS
        }
    }

    private IEnumerator AnimateScaleOut(Transform t)
    {
        if (t == null) yield break;

        StopHover(t);

        float time = 0f;
        Vector3 startScale = t.localScale; // usually Vector3.one, but safe

        while (time < scaleOutDuration)
        {
            time += Time.deltaTime;
            float normalized = Mathf.Clamp01(time / scaleOutDuration);

            float curve = scaleOutCurve.Evaluate(normalized); // typically 1 -> 0
            t.localScale = startScale * curve;

            yield return null;
        }

        t.localScale = Vector3.zero;
    }

    private void StartScaleRoutine(Transform t, IEnumerator routine)
    {
        if (t == null) return;

        // Stop any existing scale animation on this asteroid to avoid stacking
        if (scaleRoutines.TryGetValue(t, out var c) && c != null)
            StopCoroutine(c);

        scaleRoutines[t] = StartCoroutine(routine);
    }

    // UI Button
    public void ClosePopup()
    {
        animator.SetTrigger("Close");
        Destroy(gameObject, 2f); // safety destroy after animation (adjust if needed)
    }
}