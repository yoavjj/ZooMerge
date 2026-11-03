using UnityEngine;

public class FlyCollectiblePrefabRoot : MonoBehaviour
{
    [SerializeField] private MonoBehaviour flyingBehaviour; // must implement IFlyingCollectible
    [SerializeField] private HeartIntroAnimation heartIntroAnimation;

    public IFlyingCollectible Flying => flyingBehaviour as IFlyingCollectible;
    public HeartIntroAnimation HeartIntroAnimation => heartIntroAnimation;
}