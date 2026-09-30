using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Quản lý câu hỏi dùng UI Text mặc định. Liên kết với GameData.
/// Đã cập nhật kích thước nhận diện để phù hợp với giao diện bạn vừa phóng to.
/// </summary>
public class QuestionManager : MonoBehaviour
{
    [System.Serializable]
    public class Question
    {
        public string questionText;
        public string[] answers = new string[4];
        public int correctIndex;
    }

    // === CÂU HỎI SINH HỌC ===
    private Question[] questions = new Question[]
    {
        new Question {
            questionText = "Hạt đậu cần những điều kiện gì để nảy mầm?",
            answers = new string[] {
                "A. Nước, nhiệt độ thích hợp, không khí",
                "B. Chỉ cần ánh sáng mặt trời",
                "C. Chỉ cần đất màu mỡ",
                "D. Chỉ cần bón phân" },
            correctIndex = 0
        },
        new Question {
            questionText = "Phần nào của hạt đậu sẽ mọc ra đầu tiên khi nảy mầm?",
            answers = new string[] {
                "A. Lá mầm",
                "B. Rễ mầm",
                "C. Thân mầm",
                "D. Chồi non" },
            correctIndex = 1
        },
        new Question {
            questionText = "Lá mầm của hạt đậu có chức năng chính là gì?",
            answers = new string[] {
                "A. Quang hợp tạo chất hữu cơ",
                "B. Dự trữ chất dinh dưỡng nuôi phôi",
                "C. Hút nước từ đất",
                "D. Bảo vệ hạt khỏi côn trùng" },
            correctIndex = 1
        },
        new Question {
            questionText = "Trong quá trình nảy mầm, nước có vai trò gì?",
            answers = new string[] {
                "A. Làm mềm vỏ hạt và kích hoạt enzym",
                "B. Tạo màu sắc cho cây",
                "C. Làm hạt cứng hơn",
                "D. Giúp hạt bay đi xa" },
            correctIndex = 0
        },
        new Question {
            questionText = "Hạt đậu gồm những bộ phận chính nào?",
            answers = new string[] {
                "A. Vỏ hạt, phôi và chất dự trữ",
                "B. Rễ, thân và lá",
                "C. Hoa, quả và hạt",
                "D. Biểu bì, vỏ và lõi" },
            correctIndex = 0
        }
    };

    // === UI ===
    private Image questionBoard;
    private Image[] answerImages = new Image[4];
    private Image[] healthIcons = new Image[3];
    private Text questionText;
    private Text[] answerTexts = new Text[4];
    private Text feedbackText;
    private Button[] answerButtons = new Button[4];

    private int currentQuestion;
    private Color originalAnswerColor;

    void Start()
    {
        if (GameData.Instance == null)
        {
            GameObject go = new GameObject("GameData");
            go.AddComponent<GameData>();
        }

        FindUIElements();
        SetupUIComponents();
        UpdateHealthIcons();
        currentQuestion = Random.Range(0, questions.Length);
        ShowQuestion();
    }

    private void FindUIElements()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        Image[] allImages = canvas.GetComponentsInChildren<Image>(true);
        int ansIdx = 0, hpIdx = 0;
        float maxBoardArea = 0f;

        foreach (Image img in allImages)
        {
            RectTransform rt = img.rectTransform;
            float w = rt.sizeDelta.x, h = rt.sizeDelta.y;
            float area = w * h;

            // Bỏ qua ảnh nền full màn hình
            if (rt.anchorMin == Vector2.zero && rt.anchorMax == Vector2.one) continue;
            
            // KÍCH THƯỚC MỚI: Nút đáp án (chiều ngang 250 -> 500)
            if (w > 250 && w < 500 && h > 100 && h < 200 && ansIdx < 4) { answerImages[ansIdx++] = img; continue; }
            
            // KÍCH THƯỚC MỚI: Icon máu (khoảng 30-100)
            if (w > 20 && w < 100 && h > 20 && h < 100 && hpIdx < 3) { healthIcons[hpIdx++] = img; continue; }

            // TÌM BẢNG CÂU HỎI: Lấy khung có diện tích bự nhất trên màn hình (loại trừ các nút ở trên)
            if (area > maxBoardArea && w > 400 && h > 250) 
            { 
                maxBoardArea = area;
                questionBoard = img; 
            }
        }
    }

    private void SetupUIComponents()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (questionBoard != null)
        {
            questionText = CreateText("QuestionText", questionBoard.transform, font,
                new Vector2(0.05f, 0.35f), new Vector2(0.95f, 0.88f),
                36, Color.black, TextAnchor.MiddleCenter, FontStyle.Bold); // Đã tăng cỡ chữ

            feedbackText = CreateText("FeedbackText", questionBoard.transform, font,
                new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.32f),
                30, Color.red, TextAnchor.MiddleCenter, FontStyle.Bold); // Đã tăng cỡ chữ
            feedbackText.text = "";
        }

        SortAnswersByPosition();

        for (int i = 0; i < 4; i++)
        {
            if (answerImages[i] == null) continue;
            if (i == 0) originalAnswerColor = answerImages[i].color;

            Button btn = answerImages[i].gameObject.GetComponent<Button>();
            if (btn == null) btn = answerImages[i].gameObject.AddComponent<Button>();
            answerButtons[i] = btn;

            answerTexts[i] = CreateText("AnsText", answerImages[i].transform, font,
                Vector2.zero, Vector2.one,
                24, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, // Đã tăng cỡ chữ
                new Vector2(10, 5), new Vector2(-10, -5));

            int idx = i;
            btn.onClick.AddListener(() => OnAnswerClicked(idx));
        }
    }

    private void SortAnswersByPosition()
    {
        for (int i = 0; i < 4; i++)
        {
            for (int j = i + 1; j < 4; j++)
            {
                if (answerImages[i] == null || answerImages[j] == null) continue;
                Vector2 posI = answerImages[i].rectTransform.anchoredPosition;
                Vector2 posJ = answerImages[j].rectTransform.anchoredPosition;

                bool swap = false;
                if (posI.y < posJ.y - 10) swap = true;
                else if (Mathf.Abs(posI.y - posJ.y) < 10 && posI.x > posJ.x) swap = true;

                if (swap)
                {
                    Image tmp = answerImages[i];
                    answerImages[i] = answerImages[j];
                    answerImages[j] = tmp;
                }
            }
        }
    }

    private void ShowQuestion()
    {
        Question q = questions[currentQuestion];
        if (questionText != null) questionText.text = q.questionText;
        if (feedbackText != null) feedbackText.text = "";

        for (int i = 0; i < 4; i++)
        {
            if (answerTexts[i] != null) answerTexts[i].text = q.answers[i];
            if (answerButtons[i] != null) answerButtons[i].interactable = true;
            if (answerImages[i] != null) answerImages[i].color = originalAnswerColor;
        }
    }

    private void OnAnswerClicked(int index)
    {
        Question q = questions[currentQuestion];
        for (int i = 0; i < 4; i++)
            if (answerButtons[i] != null) answerButtons[i].interactable = false;

        if (index == q.correctIndex)
        {
            if (answerImages[index] != null) answerImages[index].color = new Color(0.15f, 0.75f, 0.25f);
            if (feedbackText != null) { feedbackText.color = new Color(0.1f, 0.6f, 0.1f); feedbackText.text = "Chính xác! Đã tiêu diệt quái vật!"; }
            if (GameData.Instance != null) GameData.Instance.DefeatCurrentEnemy();
            Invoke(nameof(ReturnToMainScene), 2f);
        }
        else
        {
            if (answerImages[index] != null) answerImages[index].color = new Color(0.85f, 0.15f, 0.15f);
            if (answerImages[q.correctIndex] != null) answerImages[q.correctIndex].color = new Color(0.15f, 0.75f, 0.25f);

            if (GameData.Instance != null)
            {
                bool alive = GameData.Instance.TakeDamage();
                UpdateHealthIcons();

                if (!alive)
                {
                    if (feedbackText != null) { feedbackText.color = Color.red; feedbackText.text = "Sai rồi! Hết tim... CHƠI LẠI TỪ ĐẦU!"; }
                    Invoke(nameof(RestartGame), 2.5f);
                }
                else
                {
                    if (feedbackText != null) { feedbackText.color = Color.red; feedbackText.text = "Sai rồi! Mất 1 tim. Còn " + GameData.Instance.currentHealth + " tim."; }
                    Invoke(nameof(NextQuestion), 2f);
                }
            }
        }
    }

    private void NextQuestion()
    {
        int newIdx;
        do { newIdx = Random.Range(0, questions.Length); }
        while (newIdx == currentQuestion && questions.Length > 1);
        currentQuestion = newIdx;
        ShowQuestion();
    }

    private void ReturnToMainScene()
    {
        SceneManager.LoadScene("bean_1");
    }

    private void RestartGame()
    {
        if (GameData.Instance != null)
        {
            GameData.Instance.currentHealth = GameData.Instance.maxHealth;
            GameData.Instance.defeatedEnemies.Clear();
            GameData.Instance.currentEnemyName = "";
        }
        SceneManager.LoadScene("bean_1");
    }

    private void UpdateHealthIcons()
    {
        int currentHp = (GameData.Instance != null) ? GameData.Instance.currentHealth : 3;
        for (int i = 0; i < 3; i++)
        {
            if (healthIcons[i] != null)
            {
                Color c = healthIcons[i].color;
                c.a = (i < currentHp) ? 1f : 0.15f;
                healthIcons[i].color = c;
            }
        }
    }

    private Text CreateText(string name, Transform parent, Font font,
        Vector2 anchorMin, Vector2 anchorMax,
        int fontSize, Color color, TextAnchor align, FontStyle style,
        Vector2? offsetMin = null, Vector2? offsetMax = null)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        obj.layer = 5;

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin ?? Vector2.zero;
        rt.offsetMax = offsetMax ?? Vector2.zero;

        Text txt = obj.AddComponent<Text>();
        txt.font = font;
        txt.fontSize = fontSize;
        txt.color = color;
        txt.alignment = align;
        txt.fontStyle = style;
        txt.horizontalOverflow = HorizontalWrapMode.Wrap;
        txt.verticalOverflow = VerticalWrapMode.Overflow;

        return txt;
    }
}
