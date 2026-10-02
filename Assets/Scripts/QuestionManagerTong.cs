using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Quản lý câu hỏi dành riêng cho beantong_question.
/// Trả lời đúng → quay về bean_tong.
/// Hết máu → chơi lại bean_tong từ đầu.
/// Nhận diện kích thước UI từ scene thực tế:
///   - Đáp án: ~525 x 192
///   - Tim: ~67 x 72
///   - Bảng câu hỏi: ~1237 x 914
/// </summary>
public class QuestionManagerTong : MonoBehaviour
{
    [System.Serializable]
    public class Question
    {
        public string questionText;
        public string[] answers = new string[4];
        public int correctIndex;
    }

    // === CÂU HỎI SINH HỌC TỔNG HỢP ===
    private Question[] questions = new Question[]
    {
        new Question {
            questionText = "Quang hợp xảy ra chủ yếu ở bộ phận nào của cây?",
            answers = new string[] {
                "A. Lá cây",
                "B. Rễ cây",
                "C. Thân cây",
                "D. Hoa" },
            correctIndex = 0
        },
        new Question {
            questionText = "Cây xanh hấp thụ khí gì trong quá trình quang hợp?",
            answers = new string[] {
                "A. Khí O2",
                "B. Khí CO2",
                "C. Khí N2",
                "D. Khí H2" },
            correctIndex = 1
        },
        new Question {
            questionText = "Sản phẩm chính của quá trình quang hợp là gì?",
            answers = new string[] {
                "A. Nước và muối khoáng",
                "B. Khí CO2 và nước",
                "C. Tinh bột và khí O2",
                "D. Protein và chất béo" },
            correctIndex = 2
        },
        new Question {
            questionText = "Rễ cây có chức năng chính là gì?",
            answers = new string[] {
                "A. Quang hợp tạo chất hữu cơ",
                "B. Hút nước và muối khoáng từ đất",
                "C. Sinh sản cho cây",
                "D. Tạo hoa và quả" },
            correctIndex = 1
        },
        new Question {
            questionText = "Thân cây có vai trò gì trong cơ thể thực vật?",
            answers = new string[] {
                "A. Chỉ nâng đỡ cây",
                "B. Quang hợp tạo năng lượng",
                "C. Vận chuyển nước và chất dinh dưỡng",
                "D. Hấp thụ ánh sáng mặt trời" },
            correctIndex = 2
        },
        new Question {
            questionText = "Tế bào thực vật khác tế bào động vật ở điểm nào?",
            answers = new string[] {
                "A. Có nhân tế bào",
                "B. Có thành tế bào và lục lạp",
                "C. Có màng tế bào",
                "D. Có ti thể" },
            correctIndex = 1
        },
        new Question {
            questionText = "Lục lạp chứa sắc tố gì giúp cây có màu xanh?",
            answers = new string[] {
                "A. Hemoglobin",
                "B. Melanin",
                "C. Diệp lục (Chlorophyll)",
                "D. Carotene" },
            correctIndex = 2
        },
        new Question {
            questionText = "Quá trình hô hấp ở thực vật sử dụng chất gì?",
            answers = new string[] {
                "A. Chất hữu cơ và O2",
                "B. CO2 và nước",
                "C. Ánh sáng mặt trời",
                "D. Muối khoáng" },
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
            string objName = img.gameObject.name.ToLower();

            // Bỏ qua ảnh nền full màn hình
            if (rt.anchorMin == Vector2.zero && rt.anchorMax == Vector2.one) continue;

            // Bỏ qua player (trang trí)
            if (objName.Contains("player")) continue;

            // Nút đáp án (~525 x 192)
            if (w > 400 && w < 600 && h > 150 && h < 250 && ansIdx < 4)
            {
                answerImages[ansIdx++] = img;
                continue;
            }

            // Icon tim (~67 x 72)
            if (w > 40 && w < 120 && h > 40 && h < 120 && hpIdx < 3)
            {
                // Bỏ qua bảng máu (604 x 156)
                if (w > 200) continue;
                healthIcons[hpIdx++] = img;
                continue;
            }

            // Bảng câu hỏi (~1237 x 914) - khung lớn nhất
            if (area > maxBoardArea && w > 800 && h > 500)
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
                new Vector2(0.05f, 0.45f), new Vector2(0.95f, 0.85f),
                36, Color.black, TextAnchor.MiddleCenter, FontStyle.Bold);

            feedbackText = CreateText("FeedbackText", questionBoard.transform, font,
                new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.25f),
                30, Color.red, TextAnchor.MiddleCenter, FontStyle.Bold);
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
                24, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold,
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
            Invoke(nameof(ReturnToGameScene), 2f);
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

    private void ReturnToGameScene()
    {
        SceneManager.LoadScene("bean_tong");
    }

    private void RestartGame()
    {
        if (GameData.Instance != null)
        {
            GameData.Instance.currentHealth = GameData.Instance.maxHealth;
            GameData.Instance.defeatedEnemies.Clear();
            GameData.Instance.currentEnemyName = "";
            GameData.Instance.hasSavedPosition = false;
        }
        SceneManager.LoadScene("bean_tong");
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
