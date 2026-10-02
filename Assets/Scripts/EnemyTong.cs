using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Quái vật dành riêng cho scene bean_tong.
/// Khi nhân vật chạm vào → chuyển sang bean_tong_question.
/// Khi quay lại, nếu quái đã bị tiêu diệt thì tự ẩn.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class EnemyTong : MonoBehaviour
{
    private RectTransform rectTransform;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void Start()
    {
        if (GameData.Instance != null && GameData.Instance.IsEnemyDefeated(gameObject.name))
        {
            gameObject.SetActive(false);
        }
    }

    void Update()
    {
        GameObject player = GameObject.Find("Player");
        if (player == null) return;

        RectTransform playerRT = player.GetComponent<RectTransform>();
        if (playerRT == null) return;

        if (RectsOverlap(GetWorldRect(rectTransform), GetWorldRect(playerRT)))
        {
            OnPlayerTouch();
        }
    }

    private void OnPlayerTouch()
    {
        if (GameData.Instance == null)
        {
            GameObject go = new GameObject("GameData");
            go.AddComponent<GameData>();
        }

        GameData.Instance.currentEnemyName = gameObject.name;

        // Lưu vị trí nhân vật
        GameObject player = GameObject.Find("Player");
        if (player != null)
        {
            RectTransform playerRT = player.GetComponent<RectTransform>();
            if (playerRT != null)
            {
                GameData.Instance.lastPlayerPosition = playerRT.anchoredPosition;
                GameData.Instance.hasSavedPosition = true;
            }
        }

        // Chuyển sang scene câu hỏi dành cho bean_tong
        SceneManager.LoadScene("beantong_question");
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
        return a.xMin < b.xMax && a.xMax > b.xMin &&
               a.yMin < b.yMax && a.yMax > b.yMin;
    }
}
