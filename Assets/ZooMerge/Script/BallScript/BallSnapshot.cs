// File: BallSnapshot.cs
using UnityEngine;

public struct BallSnapshot
{
    public Vector3 position;
    public Quaternion rotation;
    public int level;
    public BallType type;
    public float scale;
    public int sortingOrder;
}
