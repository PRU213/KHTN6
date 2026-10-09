using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class VictoryDisplay : MonoBehaviour
{
    static bool hasSavedThisSession = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void OnLoad()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name.Equals("victory", System.StringComparison.OrdinalIgnoreCase))
        {
            if (!hasSavedThisSession) hasSavedThisSession = false;
            // --- NEW LOGIC: Hook up XepHang Button regardless of GameData ---
            GameObject btnXepHang = GameObject.Find("XepHang");
            if (btnXepHang != null)
            {
                Button btn = btnXepHang.GetComponent<Button>();
                if (btn == null) btn = btnXepHang.AddComponent<Button>();
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => {
                    SceneManager.LoadScene("XepHang");
                });
            }

            Canvas canvas = null;
            if (btnXepHang != null) canvas = btnXepHang.GetComponentInParent<Canvas>();
            if (canvas == null) canvas = Object.FindAnyObjectByType<Canvas>();

            if (canvas != null && GameData.Instance != null && GameData.Instance.startTime > 0)
            {
                float timeTaken = GameData.Instance.endTime - GameData.Instance.startTime;
                if (timeTaken < 0) timeTaken = 0;

                GameObject textObj = new GameObject("ClearTimeText", typeof(RectTransform));
                textObj.transform.SetParent(canvas.transform, false);

                Text txt = textObj.AddComponent<Text>();
                txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                txt.fontSize = 60;
                
                Shadow shadow = textObj.AddComponent<Shadow>();
                shadow.effectColor = Color.black;
                shadow.effectDistance = new Vector2(2, -2);
                
                txt.color = Color.yellow;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.fontStyle = FontStyle.Bold;
                
                int minutes = Mathf.FloorToInt(timeTaken / 60F);
                int seconds = Mathf.FloorToInt(timeTaken - minutes * 60);
                string timeStr = string.Format("{0:00}:{1:00}", minutes, seconds);
                txt.text = "Thời gian: " + timeStr;

                RectTransform rt = textObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0, -250); 
                rt.sizeDelta = new Vector2(800, 150);

                // --- NEW LOGIC: Save Score to API ---
                string user = PlayerPrefs.GetString("Username", "Unknown");
                string full = PlayerPrefs.GetString("Fullname", "Unknown");
                string chapter = PlayerPrefs.GetString("SelectedChapter", "Tất cả");
                string diff = PlayerPrefs.GetString("SelectedDifficulty", "Tổng hợp");
                
                if (LeaderboardAPI.Instance != null)
                {
                    LeaderboardAPI.Instance.SaveScore(user, full, "Sinh học", chapter, diff, timeStr, null);
                }

                GameData.Instance.startTime = 0;
            }
        }
    }
}
