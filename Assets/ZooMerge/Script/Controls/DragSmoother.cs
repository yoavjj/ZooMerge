using UnityEngine;

public class DragSmoother
{
    private float smoothTime;
    private float velocity;

    public DragSmoother(float smoothTime = 0.05f)
    {
        this.smoothTime = smoothTime;
        this.velocity = 0f;
    }

    public float Smooth(float current, float target)
    {
        return Mathf.SmoothDamp(current, target, ref velocity, smoothTime);
    }

    public void Reset()
    {
        velocity = 0f;
    }
}