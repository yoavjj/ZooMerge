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
        GameInventory.Instance.LoadFromPrefs();

        int g = PlayerProgress.LastGalaxyId;
        int l = PlayerProgress.LastLevelInGalaxy;
        int e = PlayerProgress.LastEnemyIndex;

        MergeLevelManager.SetProgress(g, l, e);

        if (audioManagerPrefab != null &&
            AudioManager.Instance == null)
        {
            Instantiate(audioManagerPrefab);
        }

        yield return StartCoroutine(
            EnsureInternetConnection()
        );

        Debug.Log(
            "[EditorMainBootstrap] Internet connection confirmed."
        );

        if (adManagerPrefab != null &&
            AdManager.Instance == null)
        {
            Instantiate(adManagerPrefab);
        }

        if (AudioManager.Instance != null)
        {
            StartCoroutine(
                AudioManager.Instance.PrewarmSessionMusic()
            );
        }

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

        bool synced = false;

        CloudSaveManager.SyncProgressFromCloud(
            () => synced = true
        );

        while (!synced)
            yield return null;

        bool econSynced = false;

        CloudSaveManager.SyncEconomyFromCloud(
            () => econSynced = true
        );

        while (!econSynced)
            yield return null;

        if (AudioManager.Instance != null)
        {
            yield return new WaitUntil(
                () =>
                    AudioManager.Instance
                        .IsSessionMusicPrewarmed
            );
        }

        FirebaseInitializer.BootComplete = true;

        var topBar =
            FindObjectOfType<TopBarMenu>();

        if (topBar != null)
            topBar.RefreshCoins();

        Debug.Log(
            "[EditorMainBootstrap] Boot complete."
        );
    }

    private IEnumerator EnsureInternetConnection()
    {
        bool connected = false;

        yield return InternetConnectionChecker.Check(
            result => connected = result
        );

        if (connected)
            yield break;

        Debug.LogWarning(
            "[EditorMainBootstrap] No internet connection."
        );

        if (prefabLibrary == null ||
            popupContainer == null)
        {
            Debug.LogError(
                "[EditorMainBootstrap] Missing PrefabLibrary or popup container."
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
                "[EditorMainBootstrap] ConnectionLostPopup not found."
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
                "[EditorMainBootstrap] ConnectionLostPopup component missing."
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
            "[EditorMainBootstrap] Internet connection restored."
        );
    }

#endif
}