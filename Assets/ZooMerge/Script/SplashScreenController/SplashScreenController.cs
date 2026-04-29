using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

#if UNITY_IOS
using Unity.Advertisement.IosSupport;
#endif

public class SplashScreenController : MonoBehaviour
{
    [Header("Scene To Load")]
    [SerializeField] private string mainSceneName = "Main";

    [Header("Managers To Preload")]
    [SerializeField] private AdManager adManagerPrefab;

    [Header("UI")]
    [SerializeField] private Slider progressSlider;
    [SerializeField] private TextMeshProUGUI progressText;

    [Header("Optional")]
    [SerializeField] private float minSplashTime = 1.0f;
    [SerializeField] private float maxWaitTime = 8.0f;

    [Header("Step Timing")]
    [SerializeField] private float attMaxWaitTime = 6.0f;
    [SerializeField] private float stepLerpTime = 0.25f;

    [Header("Startup Delay")]
    [SerializeField] private float startDelay = 0.3f;

    private bool firebaseDone = false;
    private bool adManagerCreated = false;
    private bool attDone = false;

    private float startTime;
    private float displayed = 0f;

    private float _currentTarget = 0f;
    private float _stepFrom = 0f;
    private float _stepTo = 0f;
    private float _stepT = 1f; // 1 = finished

    private IEnumerator Start()
    {

        AnalyticsEvents.SessionStart();
        
        startTime = Time.time;
        SetProgress(0f);

        // ✅ Short delay so the splash UI fully appears before heavy work starts
        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        // 1) Firebase
        FirebaseInitializer.WaitForFirebase(
            onReady: () => { firebaseDone = true; },
            onError: (error) =>
            {
                Debug.LogError($"[Splash] Firebase failed: {error}");
                firebaseDone = true; // still continue
            }
        );

        // 2) Spawn AdManager (Ads + ATT live on that prefab)
        if (adManagerPrefab != null && AdManager.Instance == null)
        {
            Instantiate(adManagerPrefab);
        }

        // Start the boot flow
        yield return StartCoroutine(BootRoutine());
    }

    private IEnumerator BootRoutine()
    {
        float phaseStart = Time.time;

#if UNITY_IOS
        float attStartTime = -1f;   // start timing ATT once we know we're waiting for it
        bool attTimedOut = false;
#else
        bool attTimedOut = true;    // non-iOS: treat as done
#endif

        while (true)
        {
            // Step checks
            if (!adManagerCreated && AdManager.Instance != null)
                adManagerCreated = true;

#if UNITY_IOS
            // Start ATT timer once AdManager exists
            if (adManagerCreated && attStartTime < 0f)
                attStartTime = Time.time;

            if (!attDone)
                attDone = CheckATTDone();

            // If ATT is taking too long, stop waiting for it
            if (!attDone && attStartTime >= 0f && (Time.time - attStartTime) >= attMaxWaitTime)
                attTimedOut = true;
#endif

            // 🌟 THE FIX IS HERE: We check if the JSON is actually parsed!
            bool isJsonParsed = FirebaseInitializer.MergeScoreData != null && FirebaseInitializer.MergeScoreData.galaxies != null;

            // Stop waiting if it takes too long (e.g., bad internet connection)
            bool firebaseTimedOut = (!firebaseDone || !isJsonParsed) && (Time.time - phaseStart) >= maxWaitTime;

            // 🌟 THE FIX PART 2: Require BOTH the SDK to be done AND the JSON to be parsed
            bool isDataFullyLoaded = (firebaseDone && isJsonParsed);

            // Progress target
            float target = 0f;
            if (isDataFullyLoaded || firebaseTimedOut) target = 0.5f;
            if (adManagerCreated) target = 0.75f;

            // Creep upward if waiting on Firebase download
            if (adManagerCreated && !(isDataFullyLoaded || firebaseTimedOut))
                target = Mathf.Max(target, 0.85f);

#if UNITY_IOS
            if (attDone || attTimedOut) target = 0.9f;
#else
            target = 0.9f;
#endif

            // Smooth step transitions
            if (!Mathf.Approximately(target, _currentTarget))
            {
                _currentTarget = target;
                _stepFrom = displayed;
                _stepTo = target;
                _stepT = 0f;
            }

            _stepT += Time.deltaTime / Mathf.Max(0.0001f, stepLerpTime);
            displayed = Mathf.Lerp(_stepFrom, _stepTo, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_stepT)));
            SetProgress(displayed);

#if UNITY_IOS
            bool canProceed = (attDone || attTimedOut);
#else
            bool canProceed = true;
#endif

            // PART 3: Only break the loop when the data is fully loaded!
            if ((isDataFullyLoaded || firebaseTimedOut) && canProceed && displayed >= 0.9f - 0.0001f)
                break;

            yield return null;
        }

        // Ensure minimum splash time
        float elapsed = Time.time - startTime;
        if (elapsed < minSplashTime)
            yield return new WaitForSeconds(minSplashTime - elapsed);

        // Load main scene with progress
        yield return StartCoroutine(LoadMainSceneWithProgress());
    }

    private IEnumerator LoadMainSceneWithProgress()
    {
        var op = SceneManager.LoadSceneAsync(mainSceneName);
        op.allowSceneActivation = false;

        // Unity async progress usually goes 0..0.9 until activation
        while (op.progress < 0.9f)
        {
            float t = Mathf.InverseLerp(0f, 0.9f, op.progress);   // 0..1
            float target = Mathf.Lerp(0.9f, 1.0f, t);            // 0.9..1.0

            displayed = Mathf.MoveTowards(displayed, target, Time.deltaTime * 1.2f);
            SetProgress(displayed);

            yield return null;
        }

        // Force full
        displayed = 1.0f;
        SetProgress(displayed);

        // Activate scene next frame
        yield return null;
        op.allowSceneActivation = true;
    }

    private void SetProgress(float value01)
    {
        value01 = Mathf.Clamp01(value01);

        if (progressSlider != null)
            progressSlider.value = value01;

        if (progressText != null)
            progressText.text = Mathf.RoundToInt(value01 * 100f) + "%";
    }

    private bool CheckATTDone()
    {
#if UNITY_IOS
        var status = ATTrackingStatusBinding.GetAuthorizationTrackingStatus();
        return status != ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED;
#else
        return true; // Android/Editor
#endif
    }
}
