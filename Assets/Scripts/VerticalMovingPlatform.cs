using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Gỗ nâng di chuyển lên xuống giữa 2 điểm (ví dụ giữa đảo nhỏ và gỗ nhỏ).
/// Nhân vật đứng trên sẽ được chở theo.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class VerticalMovingPlatform : MonoBehaviour
{
    public static readonly List<VerticalMovingPlatform> AllVerticalPlatforms = new List<VerticalMovingPlatform>();

    [Header("=== DI CHUYỂN ===")]
    [Tooltip("Tốc độ di chuyển (pixel/giây)")]
    public float speed = 80f;

    [Tooltip("Thời gian dừng ở mỗi đầu (giây)")]
    public float waitTime = 0.8f;

    [Tooltip("Khoảng cách mặc định nếu không tìm thấy bến đỗ (pixel)")]
    public float defaultTravelDistance = 250f;

    [Header("=== CUSTOM BOUNDS ===")]
    public bool useCustomBounds = false;
    public float customMinY = 0f;
    public float customMaxY = 0f;
    [Tooltip("1 = Đi Lên trước, -1 = Đi Xuống trước")]
    public float startDirection = 1f;

    // --- Data ---
    public RectTransform RectTransform { get; private set; }

    // --- State ---
    private float pointTop_Y;
    private float pointBottom_Y;
    private float direction = 1f; // 1 = lên, -1 = xuống
    private float waitTimer;
    private Vector2 previousPosition;
    private bool boundsCalculated = false;

    public Vector2 FrameDelta { get; private set; }

    private Platform platformComp;

    void Awake()
    {
        RectTransform = GetComponent<RectTransform>();

        platformComp = GetComponent<Platform>();
        if (platformComp == null)
            platformComp = gameObject.AddComponent<Platform>();
    }

    void Start()
    {
        previousPosition = RectTransform.anchoredPosition;
        if (useCustomBounds)
        {
            pointBottom_Y = customMinY;
            pointTop_Y = customMaxY;
            boundsCalculated = true;
            direction = startDirection;
        }
        else
        {
            CalculateBounds();
        }
    }

    void OnEnable()
    {
        if (!AllVerticalPlatforms.Contains(this))
            AllVerticalPlatforms.Add(this);
    }

    void OnDisable()
    {
        AllVerticalPlatforms.Remove(this);
    }

    private void CalculateBounds()
    {
        float myX = RectTransform.anchoredPosition.x;
        float myY = RectTransform.anchoredPosition.y;
        float myHalfH = RectTransform.sizeDelta.y * 0.5f;

        float nearestTopEdge = myY + defaultTravelDistance;
        float nearestBottomEdge = myY - defaultTravelDistance;

        foreach (var plat in Platform.AllPlatforms)
        {
            if (plat == null || plat.gameObject == gameObject) continue;
            // Bỏ qua Moving platform khác
            if (plat.GetComponent<VerticalMovingPlatform>() != null) continue;

            RectTransform prt = plat.RectTransform;
            if (prt == null) continue;

            float px = prt.anchoredPosition.x;
            float py = prt.anchoredPosition.y;
            float pHalfH = prt.sizeDelta.y * 0.5f;

            // Chỉ xét platform ở cùng trục X (chênh lệch X < 500 để bắt được các bục lệch)
            if (Mathf.Abs(px - myX) > 500f) continue;

            float platTopEdge = py + pHalfH;
            float platBottomEdge = py - pHalfH;

            // Platform nằm trên
            if (py > myY)
            {
                float edge = platBottomEdge - 10f - myHalfH;
                if (edge < nearestTopEdge && edge > myY + myHalfH)
                    nearestTopEdge = edge;
            }
            // Platform nằm dưới
            else if (py < myY)
            {
                float edge = platTopEdge + 10f + myHalfH;
                if (edge > nearestBottomEdge && edge < myY - myHalfH)
                    nearestBottomEdge = edge;
            }
        }

        pointTop_Y = nearestTopEdge;
        pointBottom_Y = nearestBottomEdge;
        boundsCalculated = true;
        direction = 1f;

        Debug.Log($"[VerticalMoving] {gameObject.name}: Y Bounds = [{pointBottom_Y:F0} ↔ {pointTop_Y:F0}], Start = {myY:F0}");
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

        Vector2 pos = RectTransform.anchoredPosition;
        pos.y += direction * speed * Time.deltaTime;

        if (pos.y >= pointTop_Y)
        {
            pos.y = pointTop_Y;
            direction = -1f;
            waitTimer = waitTime;
        }
        else if (pos.y <= pointBottom_Y)
        {
            pos.y = pointBottom_Y;
            direction = 1f;
            waitTimer = waitTime;
        }

        RectTransform.anchoredPosition = pos;

        FrameDelta = RectTransform.anchoredPosition - previousPosition;
        previousPosition = RectTransform.anchoredPosition;
    }
}
