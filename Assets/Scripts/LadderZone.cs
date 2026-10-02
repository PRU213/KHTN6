using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Marker component - Gắn vào mỗi Image đóng vai trò cái thang.
/// Script tự đăng ký vào danh sách tĩnh để PlayerController tìm thấy tự động.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class LadderZone : MonoBehaviour
{
    /// <summary>Danh sách tất cả Ladder đang active trong scene</summary>
    public static readonly List<LadderZone> AllLadders = new List<LadderZone>();

    public RectTransform RectTransform { get; private set; }

    void Awake()
    {
        RectTransform = GetComponent<RectTransform>();
    }

    void OnEnable()
    {
        if (!AllLadders.Contains(this))
            AllLadders.Add(this);
    }

    void OnDisable()
    {
        AllLadders.Remove(this);
    }
}