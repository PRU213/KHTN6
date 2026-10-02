using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(RectTransform))]
public class Platform : MonoBehaviour
{
    public static readonly List<Platform> AllPlatforms = new List<Platform>();

    [Header("=== MẶT ĐẤT ===")]

    [Tooltip(
        "Khoảng cách từ mép trên của ảnh Platform xuống mặt đất thật.\n" +
        "Tăng số này = nhân vật đứng thấp xuống.\n" +
        "Giảm số này = nhân vật đứng cao lên."
    )]
    public float surfaceOffset = 35f;

    public RectTransform RectTransform { get; private set; }

    void Awake()
    {
        RectTransform = GetComponent<RectTransform>();
    }

    void OnEnable()
    {
        if (RectTransform == null)
            RectTransform = GetComponent<RectTransform>();

        if (!AllPlatforms.Contains(this))
            AllPlatforms.Add(this);
    }

    void OnDisable()
    {
        AllPlatforms.Remove(this);
    }

    /// <summary>
    /// Trả về tọa độ Y WORLD của mặt đất thật.
    /// surfaceOffset dùng theo pixel/local-unit của chính Platform.
    /// </summary>
    public float GetSurfaceWorldY()
    {
        if (RectTransform == null)
            RectTransform = GetComponent<RectTransform>();

        Vector3[] corners = new Vector3[4];
        RectTransform.GetWorldCorners(corners);

        float topY = Mathf.Max(
            corners[0].y,
            corners[1].y,
            corners[2].y,
            corners[3].y
        );

        // QUAN TRỌNG:
        // Offset phải dùng scale của PLATFORM,
        // không được dùng scale của Player.
        float platformScaleY = Mathf.Abs(RectTransform.lossyScale.y);

        if (platformScaleY < 0.0001f)
            platformScaleY = 1f;

        return topY - surfaceOffset * platformScaleY;
    }

    /// <summary>
    /// Chỉ dùng để nhìn đường mặt đất trong Scene View.
    /// Chọn Platform trong Hierarchy sẽ thấy đường màu vàng.
    /// </summary>
    void OnDrawGizmosSelected()
    {
        RectTransform rt = GetComponent<RectTransform>();

        if (rt == null)
            return;

        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);

        float leftX = Mathf.Min(
            corners[0].x,
            corners[1].x,
            corners[2].x,
            corners[3].x
        );

        float rightX = Mathf.Max(
            corners[0].x,
            corners[1].x,
            corners[2].x,
            corners[3].x
        );

        float topY = Mathf.Max(
            corners[0].y,
            corners[1].y,
            corners[2].y,
            corners[3].y
        );

        float scaleY = Mathf.Abs(rt.lossyScale.y);

        if (scaleY < 0.0001f)
            scaleY = 1f;

        float groundY = topY - surfaceOffset * scaleY;

        Gizmos.color = Color.yellow;

        Gizmos.DrawLine(
            new Vector3(leftX, groundY, rt.position.z),
            new Vector3(rightX, groundY, rt.position.z)
        );
    }
}