using UnityEngine;
using UnityEngine.UI;

public class TimerDisplay : MonoBehaviour
{
    private Text timerText;
    private static TimerDisplay instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Initialize()
    {
        if (instance == null)
        {
            GameObject go = new GameObject("PersistentTimerCanvas");
            instance = go.AddComponent<TimerDisplay>();
            DontDestroyOnLoad(go);
        }
    }

    void Awake()
    {
        // Setup Canvas
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // Dam bao luon nam tren cung

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // Setup Text
        GameObject textObj = new GameObject("TimerText", typeof(RectTransform));
        textObj.transform.SetParent(transform, false);

        timerText = textObj.AddComponent<Text>();
        timerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        timerText.fontSize = 50;
        timerText.color = Color.white;
        timerText.alignment = TextAnchor.UpperLeft;
        timerText.fontStyle = FontStyle.Bold;

        // Shadow
        Shadow shadow = textObj.AddComponent<Shadow>();
        shadow.effectColor = Color.black;
        shadow.effectDistance = new Vector2(2, -2);
        
        Outline outline = textObj.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(1, -1);

        RectTransform rt = textObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1);
        rt.anchorMax = new Vector2(0.5f, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = new Vector2(0, -50); // O giua tren cung, cach le 50px
        rt.sizeDelta = new Vector2(130, 100);
    }

    void Update()
    {
        if (GameData.Instance != null && GameData.Instance.isTimerRunning)
        {
            if (!timerText.gameObject.activeSelf) timerText.gameObject.SetActive(true);

            float timeTaken = Time.time - GameData.Instance.startTime;
            if (timeTaken < 0) timeTaken = 0;

            int minutes = Mathf.FloorToInt(timeTaken / 60F);
            int seconds = Mathf.FloorToInt(timeTaken - minutes * 60);
            
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
        else
        {
            if (timerText != null && timerText.gameObject.activeSelf)
            {
                timerText.gameObject.SetActive(false);
            }
        }
    }
}

