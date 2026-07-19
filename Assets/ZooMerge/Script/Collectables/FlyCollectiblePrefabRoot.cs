using UnityEngine;

public class FlyCollectiblePrefabRoot : MonoBehaviour
{
    [SerializeField] private MonoBehaviour flyingBehaviour; // must implement IFlyingCollectible
    public IFlyingCollectible Flying => flyingBehaviour as IFlyingCollectible;
}