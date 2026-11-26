using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AsteroidFieldAnimator : MonoBehaviour
{
    // ============================================================
    // GROUP
    // ============================================================

    [System.Serializable]
    public class AsteroidGroup
    {
        [Header("ID")]
        public string id;

        [Header("Asteroids")]
        public List<Transform> asteroids =
            new();

        [Header("Scale In")]
        public AnimationCurve scaleInCurve =
            AnimationCurve.EaseInOut(
                0f,
                0f,
                1f,
                1f
            );

        [Min(0.01f)]
        public float scaleInDuration =
            0.4f;

        [Header("Stagger")]
        [Min(0f)]
        public float delayBetweenAsteroids =
            0.2f;

        [Header("Hover")]
        [Min(0f)]
        public float hoverAmplitude =
            0.05f;

        [Min(0f)]
        public float hoverFrequency =
            1.5f;

        public Vector3 hoverAxis =
            Vector3.up;

        [Header("Rotation")]
        public bool rotate;

        [Tooltip(
            "Rotation speed in degrees per second. " +
            "Use negative values for clockwise."
        )]
        public float rotationSpeed =
            30f;

        [Header("Drift")]
        public bool useDrift;

        [Tooltip(
            "Parent/root that contains the asteroid group."
        )]
        public Transform driftRoot;

        [Tooltip(
            "World-space start point for the drift."
        )]
        public Transform driftStartPoint;

        [Tooltip(
            "World-space end point for the drift."
        )]
        public Transform driftEndPoint;

        [Min(0.01f)]
        [Tooltip(
            "Movement speed in world units per second."
        )]
        public float driftSpeed =
            1f;

        [Tooltip(
            "Disable the entire drift root when it reaches the end."
        )]
        public bool disableAtDriftEnd =
            true;

        [Header("Scale Out")]
        public AnimationCurve scaleOutCurve =
            AnimationCurve.EaseInOut(
                0f,
                1f,
                1f,
                0f
            );

        [Min(0.01f)]
        public float scaleOutDuration =
            0.35f;

        public bool scaleOutUseStagger;

        [Min(0f)]
        public float scaleOutDelayBetweenAsteroids =
            0.05f;
    }

    // ============================================================
    // REFERENCES
    // ============================================================

    [Header("Groups")]
    [SerializeField]
    private List<AsteroidGroup> groups =
        new();

    [Header("Animator")]
    [SerializeField]
    private Animator animator;

    // ============================================================
    // RUNTIME
    // ============================================================

    private readonly Dictionary<
        Transform,
        Vector3
    > baseLocalPos = new();

    private class HoverState
    {
        public AsteroidGroup group;

        public Vector3 basePosition;
        public float phase;

        public Vector3 baseEulerAngles;
        public float currentRotationZ;
    }

    private readonly Dictionary<
        Transform,
        HoverState
    > activeHovers = new();

    private readonly Dictionary<
        Transform,
        Coroutine
    > scaleRoutines = new();

    private readonly Dictionary<
    Transform,
    Coroutine
    > driftRoutines = new();

    // ============================================================
    // INITIALIZATION
    // ============================================================

    private void Awake()
    {
        CacheBasePositions();
        InitializeAllAsteroidsHidden();
    }

    private void CacheBasePositions()
    {
        baseLocalPos.Clear();

        foreach (
            AsteroidGroup group
            in groups)
        {
            if (group?.asteroids == null)
                continue;

            foreach (
                Transform asteroid
                in group.asteroids)
            {
                if (asteroid == null)
                    continue;

                if (!baseLocalPos.ContainsKey(
                        asteroid))
                {
                    baseLocalPos.Add(
                        asteroid,
                        asteroid.localPosition
                    );
                }
            }
        }
    }

    private void InitializeAllAsteroidsHidden()
    {
        foreach (
            AsteroidGroup group
            in groups)
        {
            if (group?.asteroids == null)
                continue;

            foreach (
                Transform asteroid
                in group.asteroids)
            {
                if (asteroid == null)
                    continue;

                asteroid.localScale =
                    Vector3.zero;
            }
        }
    }

    // ============================================================
    // PLAY GROUP
    // ============================================================

    // Animation Event
    public void PlayGroup(
        int groupIndex)
    {
        if (groupIndex < 0 ||
            groupIndex >= groups.Count)
        {
            Debug.LogWarning(
                $"[AsteroidFieldAnimator] " +
                $"Group index {groupIndex} does not exist."
            );

            return;
        }

        AsteroidGroup group =
            groups[groupIndex];

        StartCoroutine(
            PlayGroupRoutine(
                group
            )
        );
    }

    // Optional alternative for Animation Events / code
    public void PlayGroupById(
        string groupId)
    {
        AsteroidGroup group =
            GetGroup(
                groupId
            );

        if (group == null)
        {
            Debug.LogWarning(
                $"[AsteroidFieldAnimator] " +
                $"Group '{groupId}' was not found."
            );

            return;
        }

        StartCoroutine(
            PlayGroupRoutine(
                group
            )
        );
    }

    private IEnumerator PlayGroupRoutine(
        AsteroidGroup group)
    {
        if (group?.asteroids == null)
            yield break;

        List<Transform> ordered =
            GetOrderedAsteroids(
                group
            );

        foreach (
            Transform asteroid
            in ordered)
        {
            StartScaleRoutine(
                asteroid,
                AnimateScaleIn(
                    asteroid,
                    group
                )
            );

            if (group.delayBetweenAsteroids > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    group.delayBetweenAsteroids
                );
            }
        }
    }

    // ============================================================
    // SCALE IN
    // ============================================================

    private IEnumerator AnimateScaleIn(
        Transform asteroid,
        AsteroidGroup group)
    {
        if (asteroid == null)
            yield break;

        StopHover(
            asteroid
        );

        asteroid.localScale =
            Vector3.zero;

        float elapsed =
            0f;

        while (
            elapsed <
            group.scaleInDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float normalized =
                Mathf.Clamp01(
                    elapsed /
                    group.scaleInDuration
                );

            float scale =
                group.scaleInCurve.Evaluate(
                    normalized
                );

            asteroid.localScale =
                Vector3.one *
                scale;

            yield return null;
        }

        asteroid.localScale =
            Vector3.one;

        // Let the Animator / UI finish this frame first.
        yield return new WaitForEndOfFrame();

        StartHover(
            asteroid,
            group
        );
    }

    // ============================================================
    // HOVER
    // ============================================================

    private void StartHover(
        Transform asteroid,
        AsteroidGroup group)
    {
        if (asteroid == null ||
            group == null)
        {
            return;
        }

        Vector3 currentPosition =
            asteroid.localPosition;

        Vector3 currentEulerAngles =
            asteroid.localEulerAngles;

        baseLocalPos[
            asteroid
        ] =
            currentPosition;

        activeHovers[
            asteroid
        ] =
            new HoverState
            {
                group =
                    group,

                basePosition =
                    currentPosition,

                phase =
                    Random.value *
                    Mathf.PI *
                    2f,

                baseEulerAngles =
                    currentEulerAngles,

                currentRotationZ =
                    currentEulerAngles.z
            };
    }

    private void StopHover(
        Transform asteroid)
    {
        if (asteroid == null)
            return;

        if (activeHovers.TryGetValue(
                asteroid,
                out HoverState state))
        {
            asteroid.localPosition =
                state.basePosition;

            activeHovers.Remove(
                asteroid
            );

            return;
        }

        if (baseLocalPos.TryGetValue(
                asteroid,
                out Vector3 basePosition))
        {
            asteroid.localPosition =
                basePosition;
        }
    }

    private void LateUpdate()
    {
        if (activeHovers.Count == 0)
            return;

        foreach (
            KeyValuePair<
                Transform,
                HoverState
            > pair
            in activeHovers)
        {
            Transform asteroid =
                pair.Key;

            HoverState state =
                pair.Value;

            if (asteroid == null ||
                state == null ||
                state.group == null)
            {
                continue;
            }

            AsteroidGroup group =
                state.group;

            // ========================================================
            // HOVER
            // ========================================================

            Vector3 axis =
                group.hoverAxis.sqrMagnitude > 0f
                    ? group.hoverAxis.normalized
                    : Vector3.up;

            float offset =
                Mathf.Sin(
                    (
                        Time.unscaledTime *
                        group.hoverFrequency *
                        Mathf.PI *
                        2f
                    ) +
                    state.phase
                ) *
                group.hoverAmplitude;

            asteroid.localPosition =
                state.basePosition +
                axis *
                offset;

            // ========================================================
            // Z ROTATION
            // ========================================================

            if (group.rotate)
            {
                state.currentRotationZ +=
                    group.rotationSpeed *
                    Time.unscaledDeltaTime;

                asteroid.localRotation =
                    Quaternion.Euler(
                        state.baseEulerAngles.x,
                        state.baseEulerAngles.y,
                        state.currentRotationZ
                    );
            }
        }
    }

    // ============================================================
    // SCALE OUT GROUP
    // ============================================================

    public void ScaleOutGroup(
        int groupIndex)
    {
        if (groupIndex < 0 ||
            groupIndex >= groups.Count)
        {
            return;
        }

        StartCoroutine(
            ScaleOutGroupRoutine(
                groups[groupIndex]
            )
        );
    }

    public void ScaleOutGroupById(
        string groupId)
    {
        AsteroidGroup group =
            GetGroup(
                groupId
            );

        if (group == null)
            return;

        StartCoroutine(
            ScaleOutGroupRoutine(
                group
            )
        );
    }

    private IEnumerator ScaleOutGroupRoutine(
        AsteroidGroup group)
    {
        if (group?.asteroids == null)
            yield break;

        List<Transform> ordered =
            GetOrderedAsteroids(
                group
            );

        foreach (
            Transform asteroid
            in ordered)
        {
            StartScaleRoutine(
                asteroid,
                AnimateScaleOut(
                    asteroid,
                    group
                )
            );

            if (
                group.scaleOutUseStagger &&
                group.scaleOutDelayBetweenAsteroids > 0f)
            {
                yield return
                    new WaitForSeconds(
                        group
                            .scaleOutDelayBetweenAsteroids
                    );
            }
        }
    }

    // ============================================================
    // SCALE OUT ALL
    // ============================================================

    public void ScaleOutAll()
    {
        StartCoroutine(
            ScaleOutAllRoutine()
        );
    }

    private IEnumerator ScaleOutAllRoutine()
    {
        HashSet<Transform> processed =
            new();

        foreach (
            AsteroidGroup group
            in groups)
        {
            if (group?.asteroids == null)
                continue;

            List<Transform> ordered =
                GetOrderedAsteroids(
                    group
                );

            foreach (
                Transform asteroid
                in ordered)
            {
                if (asteroid == null ||
                    !processed.Add(
                        asteroid))
                {
                    continue;
                }

                StartScaleRoutine(
                    asteroid,
                    AnimateScaleOut(
                        asteroid,
                        group
                    )
                );

                if (
                    group.scaleOutUseStagger &&
                    group
                        .scaleOutDelayBetweenAsteroids > 0f)
                {
                    yield return
                        new WaitForSeconds(
                            group
                                .scaleOutDelayBetweenAsteroids
                        );
                }
            }
        }
    }

    private IEnumerator AnimateScaleOut(
        Transform asteroid,
        AsteroidGroup group)
    {
        if (asteroid == null)
            yield break;

        StopHover(
            asteroid
        );

        float elapsed =
            0f;

        Vector3 startScale =
            asteroid.localScale;

        while (
            elapsed <
            group.scaleOutDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float normalized =
                Mathf.Clamp01(
                    elapsed /
                    group.scaleOutDuration
                );

            float scale =
                group.scaleOutCurve.Evaluate(
                    normalized
                );

            asteroid.localScale =
                startScale *
                scale;

            yield return null;
        }

        asteroid.localScale =
            Vector3.zero;
    }

    // ============================================================
    // ROUTINES
    // ============================================================

    private void StartScaleRoutine(
        Transform asteroid,
        IEnumerator routine)
    {
        if (asteroid == null)
            return;

        if (scaleRoutines.TryGetValue(
                asteroid,
                out Coroutine current) &&
            current != null)
        {
            StopCoroutine(
                current
            );
        }

        scaleRoutines[
            asteroid
        ] =
            StartCoroutine(
                routine
            );
    }

    // ============================================================
    // DRIFT GROUP
    // ============================================================

    public void DriftGroup(
        int groupIndex)
    {
        if (groupIndex < 0 ||
            groupIndex >= groups.Count)
        {
            Debug.LogWarning(
                $"[AsteroidFieldAnimator] " +
                $"Group index {groupIndex} does not exist."
            );

            return;
        }

        StartDrift(
            groups[groupIndex]
        );
    }

    public void DriftGroupById(
        string groupId)
    {
        AsteroidGroup group =
            GetGroup(
                groupId
            );

        if (group == null)
        {
            Debug.LogWarning(
                $"[AsteroidFieldAnimator] " +
                $"Group '{groupId}' was not found."
            );

            return;
        }

        StartDrift(
            group
        );
    }

    private void StartDrift(
        AsteroidGroup group)
    {
        if (group == null ||
            !group.useDrift ||
            group.driftRoot == null ||
            group.driftStartPoint == null ||
            group.driftEndPoint == null)
        {
            return;
        }

        Transform root =
            group.driftRoot;

        if (driftRoutines.TryGetValue(
                root,
                out Coroutine current) &&
            current != null)
        {
            StopCoroutine(
                current
            );
        }

        root.gameObject.SetActive(
            true
        );

        root.position =
            group.driftStartPoint.position;

        driftRoutines[root] =
            StartCoroutine(
                DriftRoutine(
                    group
                )
            );
    }

    private IEnumerator DriftRoutine(
        AsteroidGroup group)
    {
        Transform root =
            group.driftRoot;

        Transform target =
            group.driftEndPoint;

        if (root == null ||
            target == null)
        {
            yield break;
        }

        while (
            Vector3.Distance(
                root.position,
                target.position
            ) > 0.001f)
        {
            root.position =
                Vector3.MoveTowards(
                    root.position,
                    target.position,
                    group.driftSpeed *
                    Time.unscaledDeltaTime
                );

            yield return null;
        }

        root.position =
            target.position;

        driftRoutines.Remove(
            root
        );

        if (group.disableAtDriftEnd)
        {
            root.gameObject.SetActive(
                false
            );
        }
    }

    // ============================================================
    // HELPERS
    // ============================================================

    private static List<Transform>
        GetOrderedAsteroids(
            AsteroidGroup group)
    {
        List<Transform> ordered =
            new(
                group.asteroids
            );

        ordered.RemoveAll(
            asteroid =>
                asteroid == null
        );

        ordered.Sort(
            (
                a,
                b
            ) =>
                a.GetSiblingIndex()
                    .CompareTo(
                        b.GetSiblingIndex()
                    )
        );

        return ordered;
    }

    private AsteroidGroup GetGroup(
        string groupId)
    {
        if (groups == null ||
            string.IsNullOrEmpty(
                groupId))
        {
            return null;
        }

        return groups.Find(
            group =>
                group != null &&
                group.id ==
                groupId
        );
    }

    // ============================================================
    // CLOSE
    // ============================================================

    public void ClosePopup()
    {
        if (animator != null)
        {
            animator.SetTrigger(
                "Close"
            );
        }

        Destroy(
            gameObject,
            2f
        );
    }
}