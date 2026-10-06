using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gắn vào object bẫy (bẫy, dart trúng đích...).
/// Khi nhân vật chạm vào bẫy → chết → chơi lại màn từ đầu.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class TrapZone : MonoBehaviour
{
    private RectTransform rectTransform;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
#pragma warning disable CS0618
        PlayerController pc = FindObjectOfType<PlayerController>();
#pragma warning restore CS0618
        if (pc == null) return;
        GameObject player = pc.gameObject;

        RectTransform playerRT = player.GetComponent<RectTransform>();
        if (playerRT == null) return;

        if (RectsOverlap(GetWorldRect(rectTransform), GetWorldRect(playerRT)))
        {
            OnPlayerTouch();
        }
    }

    private void OnPlayerTouch()
    {
        Debug.Log("💀 Chạm bẫy! Chơi lại từ đầu!");

        if (GameData.Instance != null)
        {
            GameData.Instance.currentHealth = GameData.Instance.maxHealth;
            GameData.Instance.defeatedEnemies.Clear();
            GameData.Instance.currentEnemyName = "";
            GameData.Instance.hasSavedPosition = false;
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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
