using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class VictoryDisplay : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void OnLoad()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name.Equals("victory", System.StringComparison.OrdinalIgnoreCase))
        {
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas != null && GameData.Instance != null && GameData.Instance.startTime > 0)
            {
                float timeTaken = GameData.Instance.endTime - GameData.Instance.startTime;
                if (timeTaken < 0) timeTaken = 0;

                GameObject textObj = new GameObject("ClearTimeText", typeof(RectTransform));
                textObj.transform.SetParent(canvas.transform, false);

                Text txt = textObj.AddComponent<Text>();
                txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                txt.fontSize = 60;
                
                // You can add a slight shadow for better visibility
                Shadow shadow = textObj.AddComponent<Shadow>();
                shadow.effectColor = Color.black;
                shadow.effectDistance = new Vector2(2, -2);
                
                txt.color = Color.yellow;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.fontStyle = FontStyle.Bold;
                
                int minutes = Mathf.FloorToInt(timeTaken / 60F);
                int seconds = Mathf.FloorToInt(timeTaken - minutes * 60);
                txt.text = string.Format("Thời gian vượt ải: {0:00}:{1:00}", minutes, seconds);

                RectTransform rt = textObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0, -250); // Đặt ở vị trí nửa dưới màn hình
                rt.sizeDelta = new Vector2(800, 150);
            }
        }
    }
}
