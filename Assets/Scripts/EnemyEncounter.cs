using UnityEngine;
using UnityEngine.SceneManagement;

public class EnemyEncounter : MonoBehaviour
{
    [SerializeField] private string enemyId;
    [SerializeField] private RectTransform playerRect;
    
    private bool triggered = false;
    private RectTransform myRect;

    void Start()
    {
        myRect = GetComponent<RectTransform>();
        
        // Ẩn quái vật nếu đã bị tiêu diệt
        if (GameSession.IsEnemyDead(enemyId))
        {
            gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (!triggered && CheckOverlap(myRect, playerRect))
        {
            triggered = true;
            GameSession.StartEncounter(enemyId, playerRect.anchoredPosition);
            SceneManager.LoadScene(GameSession.questionSceneName);
        }
    }

    // Kiểm tra va chạm bằng GetWorldCorners để hoạt động đúng với UI
    private bool CheckOverlap(RectTransform rect1, RectTransform rect2)
    {
        if (rect1 == null || rect2 == null) return false;

        Vector3[] corners1 = new Vector3[4];
        rect1.GetWorldCorners(corners1);

        Vector3[] corners2 = new Vector3[4];
        rect2.GetWorldCorners(corners2);

        float minX1 = Mathf.Min(corners1[0].x, corners1[2].x);
        float maxX1 = Mathf.Max(corners1[0].x, corners1[2].x);
        float minY1 = Mathf.Min(corners1[0].y, corners1[2].y);
        float maxY1 = Mathf.Max(corners1[0].y, corners1[2].y);

        float minX2 = Mathf.Min(corners2[0].x, corners2[2].x);
        float maxX2 = Mathf.Max(corners2[0].x, corners2[2].x);
        float minY2 = Mathf.Min(corners2[0].y, corners2[2].y);
        float maxY2 = Mathf.Max(corners2[0].y, corners2[2].y);

        return minX1 < maxX2 && maxX1 > minX2 && minY1 < maxY2 && maxY1 > minY2;
    }
}
