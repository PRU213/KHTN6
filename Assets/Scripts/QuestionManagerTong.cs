using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;

/// <summary>
/// Quản lý câu hỏi dành riêng cho beantong_question.
/// Trả lời đúng → quay về bean_tong.
/// Hết máu → chơi lại bean_tong từ đầu.
/// Hiển thị GiaiThich kèm txtGiaiThich khi chọn sai đáp án.
/// Đáp án đúng hiện xanh, đáp án sai hiện đỏ.
/// </summary>
public class QuestionManagerTong : MonoBehaviour
{
    [System.Serializable]
    public class Question
    {
        public string questionText;
        public string[] answers = new string[4];
        public int correctIndex;
        public string explanation;
    }

    private Question[] questions = new Question[0];
    private Button bankRetry;
    private bool bankLoading;

    // === UI ===
    private Image questionBoard;
    private Image[] answerImages = new Image[4];
    private Image[] healthIcons = new Image[3];
    private Text questionText;
    private Text[] answerTexts = new Text[4];
    private Text feedbackText;
    private Button[] answerButtons = new Button[4];

    // === GIẢI THÍCH UI ===
    private GameObject giaiThichObject;
    private Text txtGiaiThichLegacy;
    private TMPro.TMP_Text txtGiaiThichTMP;

    private int currentQuestion;
    private Color originalAnswerColor = Color.white;

    void Start()
    {
        if (GameData.Instance == null) new GameObject("GameData").AddComponent<GameData>();
        FindUIElements(); SetupUIComponents(); UpdateHealthIcons();
        LoadDynamicQuestions();
        if (questions.Length > 0) { currentQuestion = Random.Range(0, questions.Length); ShowQuestion(); }
        else StartCoroutine(FetchQuestionsFromSheet());
    }

    private IEnumerator FetchQuestionsFromSheet()
    {
        if (bankLoading) yield break;
        bankLoading = true;
        ShowBankMessage("Đang tải đúng bộ câu hỏi Sinh học…", false);
        string error = null;
        using (var request = UnityWebRequest.Get(BiologyQuestionBank.CsvUrl))
        {
            request.timeout = 25;
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success) error = "Chưa tải được câu hỏi từ Google Sheet. Hãy thử lại.";
            else try {
                GameData.Instance.allQuestions = BiologyQuestionBank.ParseCsv(request.downloadHandler.text);
                LoadDynamicQuestions();
            } catch (System.Exception ex) { error = ex.Message; }
        }
        bankLoading = false;
        if (error != null) { ShowBankMessage(error, true); yield break; }
        if (questions.Length == 0) { ShowBankMessage("Chưa có câu Sinh học đúng chương và độ khó đã chọn. Hãy quay lại chọn bài hoặc tải lại.", true); yield break; }
        currentQuestion = Random.Range(0, questions.Length); ShowQuestion();
    }

    private void LoadDynamicQuestions()
    {
        var selected = BiologyQuestionBank.Select(GameData.Instance != null ? GameData.Instance.allQuestions : null,
            PlayerPrefs.GetString("SelectedChapter", "Tất cả"), PlayerPrefs.GetString("SelectedDifficulty", "Tổng hợp"));
        var mapped = new List<Question>();
        foreach (var q in selected) mapped.Add(new Question {
            questionText = q.question, answers = new[] { FormatOption("A.", q.optionA), FormatOption("B.", q.optionB), FormatOption("C.", q.optionC), FormatOption("D.", q.optionD) },
            correctIndex = BiologyQuestionBank.CorrectIndex(q), explanation = q.explanation
        });
        questions = mapped.ToArray();
    }

    private void ShowBankMessage(string message, bool retry)
    {
        if (questionText != null) questionText.text = message;
        if (feedbackText != null) feedbackText.text = "";
        for (int i = 0; i < 4; i++) {
            if (answerTexts[i] != null) answerTexts[i].text = "";
            if (answerButtons[i] != null) answerButtons[i].interactable = false;
        }
        if (bankRetry == null && questionBoard != null) {
            var go = new GameObject("Biology bank retry", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(questionBoard.transform, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(.3f, .2f); rect.anchorMax = new Vector2(.7f, .35f); rect.offsetMin = rect.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = new Color(.15f, .38f, .22f);
            bankRetry = go.GetComponent<Button>(); bankRetry.targetGraphic = go.GetComponent<Image>();
            var label = CreateText("Text", go.transform, Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"), Vector2.zero, Vector2.one, 24, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            label.text = "Thử tải lại"; label.raycastTarget = false;
            bankRetry.onClick.AddListener(() => StartCoroutine(FetchQuestionsFromSheet()));
        }
        if (bankRetry != null) bankRetry.gameObject.SetActive(retry);
    }

    private string FormatOption(string prefix, string opt)
    {
        if (string.IsNullOrEmpty(opt)) return prefix;
        opt = opt.Trim();
        if (opt.StartsWith(prefix) || opt.StartsWith(prefix[0] + ")") || opt.StartsWith(prefix[0] + " -") || opt.StartsWith(prefix[0] + ":"))
            return opt;
        return prefix + " " + opt;
    }

    private void FindUIElements()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        Image[] allImages = canvas.GetComponentsInChildren<Image>(true);
        int hpIdx = 0;
        float maxBoardArea = 0f;
        
        List<Image> foundAnswers = new List<Image>();

        foreach (Image img in allImages)
        {
            RectTransform rt = img.rectTransform;
            float w = rt.sizeDelta.x, h = rt.sizeDelta.y;
            float area = w * h;
            string objName = img.gameObject.name.ToLower().Trim();

            // Bỏ qua ảnh nền full màn hình
            if (rt.anchorMin == Vector2.zero && rt.anchorMax == Vector2.one) continue;

            // Bỏ qua player (trang trí)
            if (objName.Contains("player")) continue;

            // Bảng Giải Thích
            if (objName == "giaithich" || objName.Contains("giaithich"))
            {
                giaiThichObject = img.gameObject;
                continue;
            }

            // Nút đáp án (~525 x 192)
            bool isNamedAnswer = objName.Contains("dapan") || objName.Contains("đáp án") || objName.Contains("đápán");
            bool isSizedAnswer = w > 400 && w < 600 && h > 150 && h < 250;

            if (isNamedAnswer || isSizedAnswer)
            {
                foundAnswers.Add(img);
                continue;
            }

            // Icon tim (~67 x 72)
            bool isNamedHp = objName.Contains("tim") || objName.Contains("heart") || objName.Contains("mau");
            bool isSizedHp = w > 40 && w < 120 && h > 40 && h < 120;

            if ((isNamedHp || isSizedHp) && hpIdx < 3)
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

        // Sắp xếp các nút đáp án theo toạ độ Y (từ trên xuống) rồi đến X (từ trái qua)
        foundAnswers.Sort((a, b) => {
            int yCompare = b.rectTransform.position.y.CompareTo(a.rectTransform.position.y);
            if (Mathf.Abs(b.rectTransform.position.y - a.rectTransform.position.y) < 10)
                return a.rectTransform.position.x.CompareTo(b.rectTransform.position.x);
            return yCompare;
        });

        for (int i = 0; i < Mathf.Min(4, foundAnswers.Count); i++)
        {
            answerImages[i] = foundAnswers[i];
        }

        // Tìm GiaiThich và txtGiaiThich
        FindGiaiThichUI(canvas);
    }

    private void FindGiaiThichUI(Canvas canvas)
    {
        if (giaiThichObject == null)
        {
            Transform[] allTrans = canvas.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in allTrans)
            {
                if (t.name.ToLower().Trim() == "giaithich")
                {
                    giaiThichObject = t.gameObject;
                    break;
                }
            }
        }

        if (giaiThichObject != null)
        {
            Transform[] children = giaiThichObject.GetComponentsInChildren<Transform>(true);
            foreach (Transform ct in children)
            {
                if (ct != giaiThichObject.transform && ct.name.ToLower().Trim().Contains("giaithich"))
                {
                    txtGiaiThichLegacy = ct.GetComponent<Text>();
                    txtGiaiThichTMP = ct.GetComponent<TMPro.TMP_Text>();
                    break;
                }
            }

            if (txtGiaiThichLegacy == null && txtGiaiThichTMP == null)
            {
                txtGiaiThichLegacy = giaiThichObject.GetComponentInChildren<Text>(true);
                txtGiaiThichTMP = giaiThichObject.GetComponentInChildren<TMPro.TMP_Text>(true);
            }
        }

        if (txtGiaiThichLegacy == null && txtGiaiThichTMP == null)
        {
            Transform[] allTrans = canvas.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in allTrans)
            {
                if (t.name.ToLower().Trim().Contains("txtgiaithich"))
                {
                    txtGiaiThichLegacy = t.GetComponent<Text>();
                    txtGiaiThichTMP = t.GetComponent<TMPro.TMP_Text>();
                    break;
                }
            }
        }
    }

    private void SetupUIComponents()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

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

        // Cấu hình bảng GiaiThich
        if (giaiThichObject != null)
        {
            giaiThichObject.SetActive(false); // Ẩn ban đầu

            Button gtBtn = giaiThichObject.GetComponent<Button>();
            if (gtBtn == null) gtBtn = giaiThichObject.AddComponent<Button>();
            gtBtn.onClick.RemoveAllListeners();
            gtBtn.onClick.AddListener(() =>
            {
                CancelInvoke(nameof(NextQuestion));
                CancelInvoke(nameof(RestartGame));
                if (GameData.Instance != null && GameData.Instance.currentHealth <= 0)
                    RestartGame();
                else
                    NextQuestion();
            });
        }

        // Cấu hình hiển thị cho txtGiaiThich
        if (txtGiaiThichTMP != null)
        {
            txtGiaiThichTMP.color = Color.black;
            txtGiaiThichTMP.alignment = TMPro.TextAlignmentOptions.Center;
            txtGiaiThichTMP.enableAutoSizing = true;
            txtGiaiThichTMP.fontSizeMin = 14;
            txtGiaiThichTMP.fontSizeMax = 24;
        }
        else if (txtGiaiThichLegacy != null)
        {
            if (font != null) txtGiaiThichLegacy.font = font;
            txtGiaiThichLegacy.fontSize = 20;
            txtGiaiThichLegacy.color = Color.black;
            txtGiaiThichLegacy.alignment = TextAnchor.MiddleCenter;
            txtGiaiThichLegacy.horizontalOverflow = HorizontalWrapMode.Wrap;
            txtGiaiThichLegacy.verticalOverflow = VerticalWrapMode.Overflow;
        }
        else if (giaiThichObject != null)
        {
            // Dự phòng nếu chưa có component Text nào trong GiaiThich
            GameObject textChild = new GameObject("txtGiaiThich", typeof(RectTransform));
            textChild.transform.SetParent(giaiThichObject.transform, false);
            RectTransform textRt = textChild.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(20, 20);
            textRt.offsetMax = new Vector2(-20, -20);

            txtGiaiThichLegacy = textChild.AddComponent<Text>();
            if (font != null) txtGiaiThichLegacy.font = font;
            txtGiaiThichLegacy.fontSize = 20;
            txtGiaiThichLegacy.color = Color.black;
            txtGiaiThichLegacy.alignment = TextAnchor.MiddleCenter;
            txtGiaiThichLegacy.horizontalOverflow = HorizontalWrapMode.Wrap;
            txtGiaiThichLegacy.verticalOverflow = VerticalWrapMode.Overflow;
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
        if (questions.Length == 0) return;
        if (bankRetry != null) bankRetry.gameObject.SetActive(false);
        Question q = questions[currentQuestion];
        if (questionText != null) questionText.text = q.questionText;
        if (feedbackText != null) feedbackText.text = "";

        // Ẩn bảng giải thích khi chuyển câu hỏi
        if (giaiThichObject != null)
        {
            giaiThichObject.SetActive(false);
        }

        for (int i = 0; i < 4; i++)
        {
            if (answerTexts[i] != null) answerTexts[i].text = q.answers[i];
            if (answerButtons[i] != null) answerButtons[i].interactable = true;
            if (answerImages[i] != null) answerImages[i].color = originalAnswerColor;
        }
    }

    private void OnAnswerClicked(int index)
    {
        if (bankLoading || questions.Length == 0 || index < 0 || index > 3) return;
        Question q = questions[currentQuestion];
        for (int i = 0; i < 4; i++)
            if (answerButtons[i] != null) answerButtons[i].interactable = false;

        if (index == q.correctIndex)
        {
            // Đáp án đúng hiện xanh
            if (answerImages[index] != null) answerImages[index].color = new Color(0.15f, 0.75f, 0.25f);
            if (feedbackText != null) { feedbackText.color = new Color(0.1f, 0.6f, 0.1f); feedbackText.text = "Chính xác! Đã tiêu diệt quái vật!"; }
            if (giaiThichObject != null) giaiThichObject.SetActive(false);
            if (GameData.Instance != null) GameData.Instance.DefeatCurrentEnemy();
            Invoke(nameof(ReturnToGameScene), 2f);
        }
        else
        {
            // Đáp án sai hiện đỏ
            if (answerImages[index] != null) answerImages[index].color = new Color(0.85f, 0.15f, 0.15f);
            // Đáp án đúng hiện xanh
            if (answerImages[q.correctIndex] != null) answerImages[q.correctIndex].color = new Color(0.15f, 0.75f, 0.25f);

            // HIỂN THỊ GIAITHICH VÀ GÁN NỘI DUNG TXTGIAITHICH TỪ CỘT EXPLANATION
            ShowExplanation(q);

            if (GameData.Instance != null)
            {
                bool alive = GameData.Instance.TakeDamage();
                UpdateHealthIcons();

                if (!alive)
                {
                    if (feedbackText != null) { feedbackText.color = Color.red; feedbackText.text = "Sai rồi! Hết tim... CHƠI LẠI TỪ ĐẦU!"; }
                    Invoke(nameof(RestartGame), 3.5f);
                }
                else
                {
                    if (feedbackText != null) { feedbackText.color = Color.red; feedbackText.text = "Sai rồi! Mất 1 tim. Còn " + GameData.Instance.currentHealth + " tim."; }
                    Invoke(nameof(NextQuestion), 3.5f);
                }
            }
        }
    }

    private void ShowExplanation(Question q)
    {
        if (giaiThichObject != null)
        {
            giaiThichObject.SetActive(true);
            giaiThichObject.transform.SetAsLastSibling();
        }

        string expl = !string.IsNullOrEmpty(q.explanation)
            ? q.explanation
            : ("Đáp án đúng là: " + q.answers[q.correctIndex]);

        SetExplanationText(expl);
    }

    private void SetExplanationText(string expl)
    {
        if (txtGiaiThichLegacy != null)
        {
            if (expl.Length > 200) txtGiaiThichLegacy.fontSize = 16;
            else if (expl.Length > 120) txtGiaiThichLegacy.fontSize = 18;
            else txtGiaiThichLegacy.fontSize = 20;

            txtGiaiThichLegacy.text = expl;
        }
        else if (txtGiaiThichTMP != null)
        {
            txtGiaiThichTMP.enableAutoSizing = true;
            txtGiaiThichTMP.fontSizeMin = 14;
            txtGiaiThichTMP.fontSizeMax = 24;
            txtGiaiThichTMP.text = expl;
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
