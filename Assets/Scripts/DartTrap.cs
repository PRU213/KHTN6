using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Gắn vào Image phi tiêu độc trong scene bean_1.
/// Phi tiêu chỉ bắn NGANG khi nhân vật trèo lên cùng tầng cao (y gần bằng).
/// Nếu trúng nhân vật → chết → chơi lại.
/// 
/// Hướng bắn tự động dựa vào localScale.x:
///   scale.x < 0 → bắn sang PHẢI (vì hình bị lật)
///   scale.x > 0 → bắn sang TRÁI
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class DartTrap : MonoBehaviour
{
    public static readonly List<DartTrap> AllDarts = new List<DartTrap>();

    [Header("=== THIẾT LẬP PHI TIÊU ===")]
    [Tooltip("Tốc độ bay của phi tiêu (pixel/giây)")]
    public float dartSpeed = 200f;

    [Tooltip("Khoảng cách tiếp cận tối đa (trục X) để kích hoạt bắn (pixel).")]
    public float triggerDistanceX = 1000f;

    [Tooltip("Khoảng cách chênh lệch Y tối đa để kích hoạt (pixel).")]
    public float triggerHeightRange = 300f;

    [Tooltip("Thời gian chờ trước khi bắn lại (giây)")]
    public float cooldown = 4f;

    [Tooltip("Phi tiêu bay bao xa thì tự reset (pixel)")]
    public float maxFlyDistance = 2500f;

    // --- Components ---
    private RectTransform rectTransform;

    // --- State ---
    private Vector2 startPos;       // Vị trí gốc (anchoredPosition)
    private bool isFiring;          // Đang bay
    private float fireDirectionX;   // +1 = phải, -1 = trái (CHỈ NGANG)
    private float cooldownTimer;    // Đếm ngược cooldown
    private float distanceTraveled; // Quãng đường đã bay
    private Quaternion startRotation; // Rotation gốc

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        startPos = rectTransform.anchoredPosition;
        startRotation = rectTransform.localRotation;
    }

    void OnEnable()
    {
        if (!AllDarts.Contains(this))
            AllDarts.Add(this);
    }

    void OnDisable()
    {
        AllDarts.Remove(this);
    }

    void Start()
    {
        // Nếu dart ở bên trái map (x < 0) → bắn sang phải
        // nếu dart ở bên phải map (x > 0) → bắn sang trái
        // Hoặc tự động nhắm về phía player nếu player đã có sẵn
        GameObject player = GameObject.Find("Player");
        if (player != null)
        {
            RectTransform pRT = player.GetComponent<RectTransform>();
            fireDirectionX = (pRT.anchoredPosition.x > startPos.x) ? 1f : -1f;
        }
        else
        {
            fireDirectionX = (startPos.x < 0) ? 1f : -1f;
        }
    }

    void Update()
    {
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
            return;
        }

        GameObject player = GameObject.Find("Player");
        if (player == null) return;

        RectTransform playerRT = player.GetComponent<RectTransform>();
        if (playerRT == null) return;

        if (!isFiring)
        {
            float playerY = playerRT.anchoredPosition.y;
            float dartY = startPos.y;
            float deltaY = Mathf.Abs(playerY - dartY);
            
            float playerX = playerRT.anchoredPosition.x;
            float dartX = startPos.x;
            float deltaX = Mathf.Abs(playerX - dartX);

            // Bắn khi Player lại gần (cùng tầng Y và khoảng cách X đủ gần)
            if (deltaY < triggerHeightRange && deltaX < triggerDistanceX)
            {
                // Cập nhật lại hướng bắn cho chắc chắn trúng player
                fireDirectionX = (playerX > dartX) ? 1f : -1f;
                isFiring = true;
                distanceTraveled = 0f;
            }
        }
        else
        {
            // Di chuyển phi tiêu NGANG
            float moveAmount = dartSpeed * Time.deltaTime;
            Vector2 pos = rectTransform.anchoredPosition;
            pos.x += fireDirectionX * moveAmount;
            rectTransform.anchoredPosition = pos;
            distanceTraveled += moveAmount;

            // Kiểm tra va chạm với Player
            if (CheckHitPlayer(playerRT))
            {
                OnHitPlayer();
                return;
            }

            // Bay quá xa → reset
            if (distanceTraveled >= maxFlyDistance)
            {
                ResetDart();
            }
        }
    }

    private bool CheckHitPlayer(RectTransform playerRT)
    {
        Rect dartRect = GetWorldRect(rectTransform);
        Rect playerRect = GetWorldRect(playerRT);

        // Thu nhỏ hitbox cho công bằng
        float shrinkX = playerRect.width * 0.25f;
        float shrinkY = playerRect.height * 0.2f;
        playerRect.xMin += shrinkX;
        playerRect.xMax -= shrinkX;
        playerRect.yMin += shrinkY;
        playerRect.yMax -= shrinkY;

        return dartRect.xMin < playerRect.xMax && dartRect.xMax > playerRect.xMin &&
               dartRect.yMin < playerRect.yMax && dartRect.yMax > playerRect.yMin;
    }

    private void OnHitPlayer()
    {
        Debug.Log("💀 Phi tiêu độc trúng Player! Chơi lại!");

        // Reset game
        if (GameData.Instance != null)
        {
            GameData.Instance.currentHealth = GameData.Instance.maxHealth;
            GameData.Instance.defeatedEnemies.Clear();
            GameData.Instance.currentEnemyName = "";
        }

        // Reload scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void ResetDart()
    {
        isFiring = false;
        rectTransform.anchoredPosition = startPos;
        rectTransform.localRotation = startRotation;
        cooldownTimer = cooldown;
    }

    private Rect GetWorldRect(RectTransform rt)
    {
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        float xMin = Mathf.Min(corners[0].x, corners[1].x, corners[2].x, corners[3].x);
        float xMax = Mathf.Max(corners[0].x, corners[1].x, corners[2].x, corners[3].x);
        float yMin = Mathf.Min(corners[0].y, corners[1].y, corners[2].y, corners[3].y);
        float yMax = Mathf.Max(corners[0].y, corners[1].y, corners[2].y, corners[3].y);
        return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
    }
}
