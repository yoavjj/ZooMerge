using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AddressablesPreloader : MonoBehaviour
{
    [Header("What to Preload")]
    [SerializeField] private List<AssetReference> assetsToPreload = new();

    public bool Done { get; private set; }

    public void BeginPreload()
    {
        StartCoroutine(PreloadRoutine());
    }

    private IEnumerator PreloadRoutine()
    {
        Done = false;

        foreach (var asset in assetsToPreload)
        {
            if (asset == null) continue;

            var sizeHandle = Addressables.GetDownloadSizeAsync(asset);
            yield return sizeHandle;

            if (sizeHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Addressables.Release(sizeHandle);
                continue;
            }

            long size = sizeHandle.Result;
            Addressables.Release(sizeHandle);

            if (size > 0)
            {
                var dlHandle = Addressables.DownloadDependenciesAsync(asset);
                yield return dlHandle;
                Addressables.Release(dlHandle);
            }
        }

        Done = true;
    }
}
