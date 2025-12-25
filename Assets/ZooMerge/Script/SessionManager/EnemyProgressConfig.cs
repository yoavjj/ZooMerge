using UnityEngine;

[System.Serializable]
public struct CountWidth
{
    public int enemyCount;
    public float width;
    public float leftPaddingOverride;  // Set to -1 to use default
    public float rightPaddingOverride; // Set to -1 to use default
}

public class EnemyProgressConfig
{
    private readonly CountWidth[] _countWidths;
    private readonly float _minWidth, _maxWidth;
    private readonly float _defaultLeft, _defaultRight;

    public EnemyProgressConfig(CountWidth[] countWidths, float min, float max, float left, float right)
    {
        _countWidths = countWidths;
        _minWidth = min;
        _maxWidth = max;
        _defaultLeft = left;
        _defaultRight = right;
    }

    public float GetWidth(int count)
    {
        foreach (var entry in _countWidths)
            if (entry.enemyCount == count)
                return Mathf.Clamp(entry.width, _minWidth, _maxWidth);

        float t = Mathf.InverseLerp(2, 6, Mathf.Clamp(count, 2, 6));
        return Mathf.Lerp(_minWidth, _maxWidth, t);
    }

    public void GetPadding(int count, out float left, out float right)
    {
        left = _defaultLeft;
        right = _defaultRight;

        foreach (var entry in _countWidths)
        {
            if (entry.enemyCount == count)
            {
                if (entry.leftPaddingOverride >= 0f) left = entry.leftPaddingOverride;
                if (entry.rightPaddingOverride >= 0f) right = entry.rightPaddingOverride;
                return;
            }
        }
    }
}
