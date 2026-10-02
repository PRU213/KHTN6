using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Viên gạch di chuyển qua lại giữa 2 điểm.
/// Tự động tính khoảng cách an toàn dựa vào Platform lân cận.
/// Nhân vật đứng trên sẽ được "chở" theo.
/// 
/// Logic: Tìm platform gần nhất bên TRÁI và bên PHẢI,
/// rồi di chuyển qua lại giữa mép phải platform trái ↔ mép trái platform phải.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class MovingPlatform : MonoBehaviour
{
    public static readonly List<MovingPlatform> AllMovingPlatforms = new List<MovingPlatform>();

    [Header("=== DI CHUYỂN ===")]
    [Tooltip("Tốc độ di chuyển (pixel/giây)")]
    public float speed = 80f;

    [Tooltip("Thời gian dừng ở mỗi đầu (giây)")]
    public float waitTime = 0.8f;

    [Tooltip("Khoảng đệm giữa moving platform và platform lân cận (pixel)")]
    public float edgePadding = 10f;

    // --- Platform data ---
    public RectTransform RectTransform { get; private set; }

    // --- State ---
    private float pointA_X; // Giới hạn trái
    private float pointB_X; // Giới hạn phải
    private float direction = 1f;
    private float waitTimer;
    private Vector2 previousPosition;
    private bool boundsCalculated = false;

    /// <summary>
    /// Lượng dịch chuyển mỗi frame (anchoredPosition units)
    /// để PlayerController bám theo.
    /// </summary>
    public Vector2 FrameDelta { get; private set; }

    private Platform platformComp;

    void Awake()
    {
        RectTransform = GetComponent<RectTransform>();

        // Đảm bảo có Platform component để PlayerController va chạm được
        platformComp = GetComponent<Platform>();
        if (platformComp == null)
            platformComp = gameObject.AddComponent<Platform>();
    }

    void Start()
    {
        previousPosition = RectTransform.anchoredPosition;
        CalculateBounds();
    }

    void OnEnable()
    {
        if (!AllMovingPlatforms.Contains(this))
            AllMovingPlatforms.Add(this);
    }

    void OnDisable()
    {
        AllMovingPlatforms.Remove(this);
    }

    /// <summary>
    /// Tự động tính giới hạn di chuyển dựa vào platform lân cận.
    /// Tìm platform gần nhất bên trái và phải ở cùng tầng cao.
    /// </summary>
    private void CalculateBounds()
    {
        float myX = RectTransform.anchoredPosition.x;
        float myY = RectTransform.anchoredPosition.y;
        float myHalfW = RectTransform.sizeDelta.x * 0.5f;

        float nearestLeftEdge = myX - 300f;  // Mặc định nếu không tìm thấy
        float nearestRightEdge = myX + 300f;

        foreach (var plat in Platform.AllPlatforms)
        {
            if (plat == null || plat.gameObject == gameObject) continue;
            // Bỏ qua MovingPlatform khác
            if (plat.GetComponent<MovingPlatform>() != null) continue;

            RectTransform prt = plat.RectTransform;
            if (prt == null) continue;

            float px = prt.anchoredPosition.x;
            float py = prt.anchoredPosition.y;
            float pHalfW = prt.sizeDelta.x * 0.5f;

            // Chỉ xét platform ở gần cùng tầng (chênh lệch Y < 150)
            if (Mathf.Abs(py - myY) > 150f) continue;

            float platLeftEdge = px - pHalfW;
            float platRightEdge = px + pHalfW;

            // Platform ở bên TRÁI ta
            if (px < myX)
            {
                float edge = platRightEdge + edgePadding + myHalfW;
                if (edge > nearestLeftEdge && edge < myX + myHalfW)
                    nearestLeftEdge = edge;
            }
            // Platform ở bên PHẢI ta
            else if (px > myX)
            {
                float edge = platLeftEdge - edgePadding - myHalfW;
                if (edge < nearestRightEdge && edge > myX - myHalfW)
                    nearestRightEdge = edge;
            }
        }

        pointA_X = nearestLeftEdge;
        pointB_X = nearestRightEdge;
        boundsCalculated = true;

        // Bắt đầu từ vị trí hiện tại, di chuyển sang phải
        direction = 1f;

        Debug.Log($"[MovingPlatform] {gameObject.name}: Bounds = [{pointA_X:F0} ↔ {pointB_X:F0}], Start = {myX:F0}");
    }

    void Update()
    {
        if (!boundsCalculated) return;

        if (waitTimer > 0f)
        {
            waitTimer -= Time.deltaTime;
            FrameDelta = Vector2.zero;
            previousPosition = RectTransform.anchoredPosition;
            return;
        }

        // Di chuyển
        Vector2 pos = RectTransform.anchoredPosition;
        pos.x += direction * speed * Time.deltaTime;

        // Kiểm tra giới hạn
        if (pos.x >= pointB_X)
        {
            pos.x = pointB_X;
            direction = -1f;
            waitTimer = waitTime;
        }
        else if (pos.x <= pointA_X)
        {
            pos.x = pointA_X;
            direction = 1f;
            waitTimer = waitTime;
        }

        RectTransform.anchoredPosition = pos;

        // Tính delta
        FrameDelta = RectTransform.anchoredPosition - previousPosition;
        previousPosition = RectTransform.anchoredPosition;
    }
}
