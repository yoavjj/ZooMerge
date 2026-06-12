using UnityEngine;

public class AddRetriesOnArrive : MonoBehaviour, ICollectibleArriveHandler
{
    public void HandleArrive(int amount, string collectibleId, GameObject source)
    {
        PlayerProgress.AddRetries(amount);

        Debug.Log($"[AddRetriesOnArrive] Added {amount} retries.");
    }
}