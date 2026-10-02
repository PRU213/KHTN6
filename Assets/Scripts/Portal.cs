using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class Portal : MonoBehaviour
{
    [Header("=== CÀI ĐẶT CỔNG ===")]
    [Tooltip("Tên scene sẽ chuyển tới khi đi vào cổng")]
    public string nextSceneName = "Bean_2_Biology";
    
    [Tooltip("Yêu cầu tiêu diệt hết quái vật mới hiện cổng?")]
    public bool requireAllEnemiesDefeated = true;

    private RectTransform rectTransform;
    private Image portalImage;
    private bool isActivated = false;
    private bool isFinished = false;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        portalImage = GetComponent<Image>();
    }

    void Start()
    {
        // Nếu yêu cầu diệt hết quái, ban đầu sẽ tàng hình
        if (requireAllEnemiesDefeated)
        {
            if (portalImage != null) portalImage.enabled = false;
        }
        else
        {
            isActivated = true;
        }
    }

    void Update()
    {
        if (isFinished) return;

        // 1. Kiểm tra xem đã diệt hết quái chưa
        if (!isActivated && requireAllEnemiesDefeated)
        {
#pragma warning disable CS0618
            // Tìm tất cả quái vật đang ACTIVE trong scene
            Enemy[] activeEnemies = FindObjectsOfType<Enemy>();
#pragma warning restore CS0618

            if (activeEnemies.Length == 0)
            {
                // Không còn quái nào -> hiện cổng lên
                isActivated = true;
                if (portalImage != null) portalImage.enabled = true;
                Debug.Log("✅ Đã diệt hết quái! Cổng đã mở!");
            }
        }

        // 2. Nếu cổng đã hiện, kiểm tra va chạm với Player
        if (isActivated)
        {
            // Tìm đúng script PlayerController thay vì tìm theo tên để tránh nhầm lẫn object
#pragma warning disable CS0618
            PlayerController player = FindObjectOfType<PlayerController>();
#pragma warning restore CS0618

            if (player == null) return;

            RectTransform playerRT = player.GetComponent<RectTransform>();
            if (playerRT == null) return;

            // Kiểm tra va chạm (đơn giản hóa để dễ chạm hơn)
            if (RectsOverlap(GetWorldRect(rectTransform), GetWorldRect(playerRT)))
            {
                isFinished = true;
                Debug.Log("🚀 Chuyển sang màn: " + nextSceneName);
                SceneManager.LoadScene(nextSceneName);
            }
        }
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

    private bool RectsOverlap(Rect a, Rect b)
    {
        // Kiểm tra va chạm cơ bản, không cần thu nhỏ
        return a.xMin < b.xMax && a.xMax > b.xMin &&
               a.yMin < b.yMax && a.yMax > b.yMin;
    }
}
