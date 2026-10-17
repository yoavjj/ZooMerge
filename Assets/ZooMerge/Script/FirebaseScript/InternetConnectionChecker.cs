using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public static class InternetConnectionChecker
{
    private const string TEST_URL =
        "https://www.google.com/generate_204";

    public static IEnumerator Check(
        Action<bool> onComplete,
        int timeoutSeconds = 5)
    {
        // Fast rejection first.
        if (Application.internetReachability ==
            NetworkReachability.NotReachable)
        {
            onComplete?.Invoke(false);
            yield break;
        }

        using UnityWebRequest request =
            UnityWebRequest.Get(TEST_URL);

        request.timeout = timeoutSeconds;

        yield return request.SendWebRequest();

        bool connected =
            request.result ==
            UnityWebRequest.Result.Success;

        onComplete?.Invoke(connected);
    }
}