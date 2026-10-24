using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Gắn vào object trang trí để chặn nhân vật đi xuyên qua.
/// Hoạt động như một bức tường vô hình (chặn ngang, không cho đứng lên).
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class WallBlock : MonoBehaviour
{
    public static readonly List<WallBlock> AllWalls = new List<WallBlock>();
    public RectTransform RectTransform { get; private set; }

    void Awake()
    {
        RectTransform = GetComponent<RectTransform>();
    }

    void OnEnable()
    {
        if (!AllWalls.Contains(this))
            AllWalls.Add(this);
    }

    void OnDisable()
    {
        AllWalls.Remove(this);
    }
}
