using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(RectTransform))]
public class Platform : MonoBehaviour
{
    public static readonly List<Platform> AllPlatforms = new List<Platform>();

    [Tooltip("Độ dời mặt đất (pixel): Tăng số này nếu nhân vật bay lơ lửng trên cỏ. Giảm (số âm) nếu nhân vật bị lún xuống đất.")]
    public float surfaceOffset = 0f;

    public RectTransform RectTransform { get; private set; }

    void Awake()
    {
        // Tự động dời mặt đất xuống 15px để khắc phục viền ảnh rỗng
        if (surfaceOffset == 0f) surfaceOffset = 15f;
        
        RectTransform = GetComponent<RectTransform>();
    }

    void OnEnable()
    {
        if (!AllPlatforms.Contains(this))
            AllPlatforms.Add(this);
    }

    void OnDisable()
    {
        AllPlatforms.Remove(this);
    }
}
