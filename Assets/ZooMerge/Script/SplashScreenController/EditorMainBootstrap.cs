using System.Collections;
using UnityEngine;

public class EditorMainBootstrap : MonoBehaviour
{
#if UNITY_EDITOR
    [Header("Assign the same prefabs you use in Splash")]
    [SerializeField] private AdManager adManagerPrefab;
    [SerializeField] private AudioManager audioManagerPrefab;

    [Header("Connection Lost Popup")]
    [SerializeField] private PrefabLibrary prefabLibrary;
    [SerializeField] private Transform popupContainer;

    private const string CONNECTION_LOST_POPUP =
        "ConnectionLostPopup";

    private GameObject connectionLostInstance;

    private IEnumerator Start()
    {
        // --------------------------------
        // 1. LOCAL FAST RESUME
        // --------------------------------

        GameInventory.Instance.LoadFromPrefs();

        int g = PlayerProgress.LastGalaxyId;
        int l = PlayerProgress.LastLevelInGalaxy;
        int e = PlayerProgress.LastEnemyIndex;

        MergeLevelManager.SetProgress(g, l, e);

        // --------------------------------
        // 2. CREATE AUDIO MANAGER
        // --------------------------------
        //
        // Audio does not require internet,
        // so it is safe to create immediately.

        if (audioManagerPrefab != null &&
            AudioManager.Instance == null)
        {
            Instantiate(audioManagerPrefab);
        }

        // --------------------------------
        // 3. INTERNET CHECK
        // --------------------------------
        //
        // Boot waits here while offline.
        // Nothing below this point runs until
        // the connection has been restored.

        yield return StartCoroutine(
            EnsureInternetConnection()
        );

        Debug.Log(
            "[EditorMainBootstrap] Internet connection confirmed."
        );

        // --------------------------------
        // 4. CREATE AD MANAGER
        // --------------------------------
        //
        // AdManager is network-dependent,
        // so only create it after internet
        // connectivity has been confirmed.

        if (adManagerPrefab != null &&
            AdManager.Instance == null)
        {
            Instantiate(adManagerPrefab);
        }

        // --------------------------------
        // 5. PREWARM AUDIO
        // --------------------------------

        if (AudioManager.Instance != null)
        {
            StartCoroutine(
                AudioManager.Instance
                    .PrewarmSessionMusic()
            );
        }

        // --------------------------------
        // 6. FIREBASE + REMOTE CONFIG
        // --------------------------------

        bool firebaseReady = false;

        FirebaseInitializer.WaitForFirebase(
            onReady: () =>
            {
                firebaseReady = true;
            },
            onError: err =>
            {
                Debug.LogError(
                    $"[EditorMainBootstrap] Firebase failed: {err}"
                );

                firebaseReady = true;
            }
        );

        while (!firebaseReady)
            yield return null;

        // --------------------------------
        // 7. SYNC PROGRESS
        // --------------------------------

        bool synced = false;

        CloudSaveManager.SyncProgressFromCloud(
            () => synced = true
        );

        while (!synced)
            yield return null;

        // --------------------------------
        // 8. SYNC ECONOMY
        // --------------------------------

        bool econSynced = false;

        CloudSaveManager.SyncEconomyFromCloud(
            () => econSynced = true
        );

        while (!econSynced)
            yield return null;

        // --------------------------------
        // 9. WAIT FOR AUDIO PREWARM
        // --------------------------------

        if (AudioManager.Instance != null)
        {
            yield return new WaitUntil(
                () =>
                    AudioManager.Instance
                        .IsSessionMusicPrewarmed
            );
        }

        // --------------------------------
        // 10. BOOT COMPLETE
        // --------------------------------

        FirebaseInitializer.BootComplete = true;

        var topBar =
            FindObjectOfType<TopBarMenu>();

        if (topBar != null)
            topBar.RefreshCoins();

        Debug.Log(
            "[EditorMainBootstrap] Boot complete " +
            "(internet + audio prewarmed + ads + firebase + " +
            "cloud progress + economy)."
        );
    }
#endif

    private IEnumerator EnsureInternetConnection()
    {
        bool connected = false;

        yield return InternetConnectionChecker.Check(
            result => connected = result
        );

        if (connected)
            yield break;

        Debug.LogWarning(
            "[Splash] No internet connection."
        );

        if (prefabLibrary == null ||
            popupContainer == null)
        {
            Debug.LogError(
                "[Splash] Missing PrefabLibrary or popup container."
            );

            yield break;
        }

        GameObject prefab =
            prefabLibrary.GetRaw(
                CONNECTION_LOST_POPUP
            );

        if (prefab == null)
        {
            Debug.LogError(
                "[Splash] ConnectionLostPopup not found."
            );

            yield break;
        }

        connectionLostInstance =
            Instantiate(
                prefab,
                popupContainer
            );

        ConnectionLostPopup popup =
            connectionLostInstance.GetComponent<
                ConnectionLostPopup
            >();

        if (popup == null)
        {
            Debug.LogError(
                "[Splash] ConnectionLostPopup component missing."
            );

            Destroy(connectionLostInstance);
            connectionLostInstance = null;

            yield break;
        }

        bool restored = false;

        popup.Initialize(
            () => restored = true
        );

        while (!restored)
            yield return null;

        connectionLostInstance = null;

        Debug.Log(
            "[Splash] Internet connection restored."
        );
    }
}