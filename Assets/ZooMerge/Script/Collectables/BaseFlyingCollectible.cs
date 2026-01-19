using UnityEngine;

public abstract class BaseFlyingCollectible : MonoBehaviour
{
    public abstract RectTransform Rect { get; }
    public abstract void SetIcon(Sprite sprite);

    /// <summary>
    /// Override this if a collectible needs a special effect when arriving.
    /// </summary>
    public virtual void OnArrive(int count) { }
}
