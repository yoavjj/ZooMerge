using System.Collections;
using UnityEngine;

public class EditorMainBootstrap : MonoBehaviour
{
#if UNITY_EDITOR
    [Header("Assign the same prefabs you use in Splash")]
    [SerializeField] private AdManager adManagerPrefab;
    [SerializeField] private AudioManager audioManagerPrefab;

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
        // 2. ENSURE PERSISTENT MANAGERS
        // --------------------------------

        if (audioManagerPrefab != null &&
            AudioManager.Instance == null)
        {
            Instantiate(audioManagerPrefab);
        }

        if (adManagerPrefab != null &&
            AdManager.Instance == null)
        {
            Instantiate(adManagerPrefab);
        }

        // --------------------------------
        // 3. PREWARM SESSION MUSIC
        // --------------------------------
        //
        // Start this NOW so the expensive first AudioSource.Play /
        // FMOD initialization happens while Firebase/cloud boot work
        // is running instead of when gameplay begins.

        if (AudioManager.Instance != null)
        {
            StartCoroutine(
                AudioManager.Instance.PrewarmSessionMusic()
            );
        }

        // --------------------------------
        // 4. FIREBASE + REMOTE CONFIG
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
        // 5. SYNC PROGRESS
        // --------------------------------

        bool synced = false;

        CloudSaveManager.SyncProgressFromCloud(
            () => synced = true
        );

        while (!synced)
            yield return null;

        // --------------------------------
        // 6. SYNC ECONOMY
        // --------------------------------

        bool econSynced = false;

        CloudSaveManager.SyncEconomyFromCloud(
            () => econSynced = true
        );

        while (!econSynced)
            yield return null;

        // --------------------------------
        // 7. WAIT FOR AUDIO PREWARM
        // --------------------------------
        //
        // Firebase/cloud work and music prewarming have been running
        // in parallel. Before declaring boot complete, make sure
        // session music is actually ready.

        if (AudioManager.Instance != null)
        {
            yield return new WaitUntil(
                () =>
                    AudioManager.Instance
                        .IsSessionMusicPrewarmed
            );
        }

        // --------------------------------
        // 8. BOOT COMPLETE
        // --------------------------------

        FirebaseInitializer.BootComplete = true;

        var topBar =
            FindObjectOfType<TopBarMenu>();

        if (topBar != null)
            topBar.RefreshCoins();

        Debug.Log(
            "[EditorMainBootstrap] Boot complete " +
            "(audio prewarmed + ads + firebase + " +
            "cloud progress + economy)."
        );
    }
#endif
}