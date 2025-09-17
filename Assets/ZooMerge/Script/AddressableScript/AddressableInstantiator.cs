using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AddressableInstantiator : MonoBehaviour
{
    [Header("Addressable Prefab")]
    [SerializeField] private AssetReferenceGameObject _ballPrefab;

    [Header("Parent Container (optional)")]
    [SerializeField] private Transform _container;

    [Header("Loading Animation")]
    [SerializeField] private Animator _loadingAnimator; // expects triggers "In" and "Out"

    private bool _isSpawning = false;

    public AssetReferenceGameObject DefaultBallPrefab => _ballPrefab;

    // Spawn any addressable prefab (keeps this class generic)
    public void SpawnAssetAtAsync(AssetReferenceGameObject prefabRef, Vector3 worldPosition)
    {
        if (_isSpawning || prefabRef == null) return;
        StartCoroutine(SpawnRoutine(prefabRef, worldPosition));
    }

    public GameObject SpawnAssetAt(AssetReferenceGameObject prefabRef, Vector3 worldPosition, Transform parentOverride = null)
    {
        if (prefabRef == null) return null;

        // Ensure dependencies are present (blocking)
        var sizeHandle = Addressables.GetDownloadSizeAsync(prefabRef);
        long size = sizeHandle.WaitForCompletion();
        Addressables.Release(sizeHandle);

        if (size > 0)
        {
            var dlHandle = Addressables.DownloadDependenciesAsync(prefabRef);
            dlHandle.WaitForCompletion();
            Addressables.Release(dlHandle);
        }

        // Instantiate and wait
        var parentToUse = parentOverride != null ? parentOverride : _container;
        var instHandle = prefabRef.InstantiateAsync(worldPosition, Quaternion.identity, parentToUse);

        var go = instHandle.WaitForCompletion();

        if (go != null)
        {
            // ensure correct release on destroy
            var release = go.GetComponent<ReleaseOnDestroy>();
            if (release == null) release = go.AddComponent<ReleaseOnDestroy>();
            release.handle = instHandle;
        }

        return go;
    }

    // Convenience: uses default _ballPrefab at this object's position
    public void SpawnAddressable()
    {
        if (_isSpawning) return;
        StartCoroutine(SpawnRoutine(_ballPrefab, transform.position));
    }

    // Convenience: uses default _ballPrefab at a given position
    public void SpawnBallAt(Vector3 worldPosition)
    {
        if (_isSpawning) return;
        StartCoroutine(SpawnRoutine(_ballPrefab, worldPosition));
    }

    public void SpawnBall() => SpawnAddressable();

    // --- UPDATED: routine now takes the prefab reference to use ---
    private IEnumerator SpawnRoutine(AssetReferenceGameObject prefabRef, Vector3 position)
    {
        _isSpawning = true;

        // 1) Check download size
        var sizeHandle = Addressables.GetDownloadSizeAsync(prefabRef);
        yield return sizeHandle;

        if (sizeHandle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError("Failed to get download size for prefab.");
            _isSpawning = false;
            yield break;
        }

        long downloadSize = sizeHandle.Result;

        // Show loader if needed
        bool needsDownload = downloadSize > 0;
        if (needsDownload && _loadingAnimator != null)
        {
            _loadingAnimator.SetTrigger("In");
        }

        // 2) Download dependencies if needed
        if (downloadSize > 0)
        {
            var dlHandle = Addressables.DownloadDependenciesAsync(prefabRef);
            yield return dlHandle;
            if (dlHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError("Failed to download dependencies for prefab.");
                _isSpawning = false;
                if (_loadingAnimator != null) _loadingAnimator.SetTrigger("Out");
                yield break;
            }
        }

        // Release the size handle (cleanup)
        Addressables.Release(sizeHandle);

        // 3) Instantiate
        var spawnHandle = prefabRef.InstantiateAsync(position, Quaternion.identity);
        yield return spawnHandle;

        if (spawnHandle.Status == AsyncOperationStatus.Succeeded)
        {
            var go = spawnHandle.Result;

            // If parented, make sure it sits at local zero (optional)
            if (_container != null)
                go.transform.SetParent(_container, worldPositionStays: true);

            // Add release helper so the instance gets released correctly when destroyed
            var release = go.AddComponent<ReleaseOnDestroy>();
            release.handle = spawnHandle;
        }
        else
        {
            Debug.LogError("Failed to instantiate prefab: " + spawnHandle.OperationException);
        }

        // 4) Hide loader
        if (needsDownload && _loadingAnimator != null)
        {
            _loadingAnimator.SetTrigger("Out");
            yield return new WaitForSeconds(0.3f); // optional, lets the outro play
        }

        _isSpawning = false;
    }

    /// <summary>
    /// Helper component to ensure Addressables.ReleaseInstance is called when the spawned object is destroyed.
    /// </summary>
    private class ReleaseOnDestroy : MonoBehaviour
    {
        public AsyncOperationHandle<GameObject> handle;

        private void OnDestroy()
        {
            if (handle.IsValid())
            {
                Addressables.ReleaseInstance(handle);
            }
        }
    }
}
