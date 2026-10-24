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
    private const string SHEET_CSV_URL = "https://docs.google.com/spreadsheets/d/1DcJwlN_fUDdcaQ6IfnlkMJApgWuEUqMhyZv9Kn8vfWo/export?format=csv&gid=1463734910";

    [System.Serializable]
    public class Question
    {
        public string questionText;
        public string[] answers = new string[4];
        public int correctIndex;
        public string explanation;
    }

    // === CÂU HỎI SINH HỌC TỔNG HỢP (KÈM GIẢI THÍCH) ===
    private Question[] questions = new Question[]
    {
        new Question {
            questionText = "Quang hợp xảy ra chủ yếu ở bộ phận nào của cây?",
            answers = new string[] {
                "A. Lá cây",
                "B. Rễ cây",
                "C. Thân cây",
                "D. Hoa" },
            correctIndex = 0,
            explanation = "Lá cây là cơ quan quang hợp chủ yếu nhờ chứa nhiều lục lạp."
        },
        new Question {
            questionText = "Cây xanh hấp thụ khí gì trong quá trình quang hợp?",
            answers = new string[] {
                "A. Khí O2",
                "B. Khí CO2",
                "C. Khí N2",
                "D. Khí H2" },
            correctIndex = 1,
            explanation = "Trong quá trình quang hợp, cây xanh hấp thụ khí CO2 và thải ra khí O2."
        },
        new Question {
            questionText = "Sản phẩm chính của quá trình quang hợp là gì?",
            answers = new string[] {
                "A. Nước và muối khoáng",
                "B. Khí CO2 và nước",
                "C. Tinh bột và khí O2",
                "D. Protein và chất béo" },
            correctIndex = 2,
            explanation = "Sản phẩm chính của quang hợp là chất hữu cơ (tinh bột) và khí O2."
        },
        new Question {
            questionText = "Rễ cây có chức năng chính là gì?",
            answers = new string[] {
                "A. Quang hợp tạo chất hữu cơ",
                "B. Hút nước và muối khoáng từ đất",
                "C. Sinh sản cho cây",
                "D. Tạo hoa và quả" },
            correctIndex = 1,
            explanation = "Rễ có chức năng hút nước và các muối khoáng hòa tan từ đất nuôi cây."
        },
        new Question {
            questionText = "Thân cây có vai trò gì trong cơ thể thực vật?",
            answers = new string[] {
                "A. Chỉ nâng đỡ cây",
                "B. Quang hợp tạo năng lượng",
                "C. Vận chuyển nước và chất dinh dưỡng",
                "D. Hấp thụ ánh sáng mặt trời" },
            correctIndex = 2,
            explanation = "Thân vận chuyển nước, muối khoáng từ rễ lên lá và chất hữu cơ từ lá đến các bộ phận."
        },
        new Question {
            questionText = "Tế bào thực vật khác tế bào động vật ở điểm nào?",
            answers = new string[] {
                "A. Có nhân tế bào",
                "B. Có thành tế bào và lục lạp",
                "C. Có màng tế bào",
                "D. Có ti thể" },
            correctIndex = 1,
            explanation = "Tế bào thực vật có thêm thành tế bào bảo vệ và lục lạp thực hiện quang hợp."
        },
        new Question {
            questionText = "Lục lạp chứa sắc tố gì giúp cây có màu xanh?",
            answers = new string[] {
                "A. Hemoglobin",
                "B. Melanin",
                "C. Diệp lục (Chlorophyll)",
                "D. Carotene" },
            correctIndex = 2,
            explanation = "Lục lạp chứa diệp lục (chlorophyll) có khả năng hấp thụ ánh sáng mặt trời."
        },
        new Question {
            questionText = "Quá trình hô hấp ở thực vật sử dụng chất gì?",
            answers = new string[] {
                "A. Chất hữu cơ và O2",
                "B. CO2 và nước",
                "C. Ánh sáng mặt trời",
                "D. Muối khoáng" },
            correctIndex = 0,
            explanation = "Hô hấp ở thực vật sử dụng chất hữu cơ và khí O2 để tạo năng lượng cho tế bào."
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

    // === GIẢI THÍCH UI ===
    private GameObject giaiThichObject;
    private Text txtGiaiThichLegacy;
    private TMPro.TMP_Text txtGiaiThichTMP;

    private int currentQuestion;
    private Color originalAnswerColor = Color.white;

    void Start()
    {
        if (GameData.Instance == null)
        {
            GameObject go = new GameObject("GameData");
            go.AddComponent<GameData>();
        }

        LoadDynamicQuestions();

        // Nếu GameData chưa có câu hỏi (ví dụ chạy trực tiếp scene này), tự động tải từ Google Sheet
        if (GameData.Instance.allQuestions == null || GameData.Instance.allQuestions.Count == 0)
        {
            StartCoroutine(FetchQuestionsFromSheet());
        }

        FindUIElements();
        SetupUIComponents();
        UpdateHealthIcons();
        currentQuestion = Random.Range(0, questions.Length);
        ShowQuestion();
    }

    private IEnumerator FetchQuestionsFromSheet()
    {
        using (UnityWebRequest req = UnityWebRequest.Get(SHEET_CSV_URL))
        {
            req.certificateHandler = new BypassCertificate();
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success)
            {
                ParseCSVAndPopulate(req.downloadHandler.text);
            }
            else
            {
                Debug.LogWarning("Không thể tải Google Sheet trực tiếp: " + req.error);
            }
        }
    }

    private void ParseCSVAndPopulate(string csvData)
    {
        List<string[]> rows = ParseCSVFully(csvData);
        if (rows == null || rows.Count <= 1) return;

        List<QuestionData> fetched = new List<QuestionData>();
        for (int i = 1; i < rows.Count; i++)
        {
            string[] cols = rows[i];
            if (cols.Length >= 11)
            {
                if (cols[1].Trim().Contains("Sinh"))
                {
                    QuestionData q = new QuestionData
                    {
                        id = cols[0],
                        monId = cols[1],
                        topic = cols[2],
                        difficulty = cols[3],
                        question = cols[4],
                        optionA = cols[5],
                        optionB = cols[6],
                        optionC = cols[7],
                        optionD = cols[8],
                        correct = cols[9],
                        explanation = cols[10],
                        points = (cols.Length >= 12 && int.TryParse(cols[11], out int p)) ? p : 10
                    };
                    fetched.Add(q);
                }
            }
        }

        if (fetched.Count > 0)
        {
            if (GameData.Instance != null)
                GameData.Instance.allQuestions = fetched;

            LoadDynamicQuestions();

            // Nếu người chơi chưa trả lời, làm mới câu hỏi ngẫu nhiên từ sheet
            if (answerButtons != null && answerButtons.Length > 0 && answerButtons[0] != null && answerButtons[0].interactable)
            {
                currentQuestion = Random.Range(0, questions.Length);
                ShowQuestion();
            }
        }
    }

    private List<string[]> ParseCSVFully(string csvData)
    {
        List<string[]> rows = new List<string[]>();
        List<string> currentCols = new List<string>();
        bool inQuotes = false;
        string currentField = "";

        for (int i = 0; i < csvData.Length; i++)
        {
            char c = csvData[i];

            if (c == '\"')
            {
                if (inQuotes && i + 1 < csvData.Length && csvData[i + 1] == '\"')
                {
                    currentField += '\"';
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                currentCols.Add(currentField);
                currentField = "";
            }
            else if ((c == '\r' || c == '\n') && !inQuotes)
            {
                if (c == '\r' && i + 1 < csvData.Length && csvData[i + 1] == '\n')
                {
                    i++;
                }

                if (currentCols.Count > 0 || !string.IsNullOrEmpty(currentField))
                {
                    currentCols.Add(currentField);
                    rows.Add(currentCols.ToArray());
                }

                currentCols.Clear();
                currentField = "";
            }
            else
            {
                currentField += c;
            }
        }

        if (currentCols.Count > 0 || !string.IsNullOrEmpty(currentField))
        {
            currentCols.Add(currentField);
            rows.Add(currentCols.ToArray());
        }

        return rows;
    }

    private void LoadDynamicQuestions()
    {
        if (GameData.Instance != null && GameData.Instance.allQuestions != null && GameData.Instance.allQuestions.Count > 0)
        {
            string selChapter = PlayerPrefs.GetString("SelectedChapter", "Tất cả");
            string selDiff = PlayerPrefs.GetString("SelectedDifficulty", "Tổng hợp");

            List<Question> dynamicQs = new List<Question>();

            foreach (var q in GameData.Instance.allQuestions)
            {
                bool matchChapter = (selChapter == "Tất cả" || q.topic.Trim() == selChapter);
                bool matchDiff = (selDiff == "Tổng hợp" || q.difficulty.Trim().ToLower() == selDiff.ToLower());

                if (matchChapter && matchDiff)
                {
                    Question newQ = new Question();
                    newQ.questionText = q.question;
                    newQ.answers = new string[] {
                        FormatOption("A.", q.optionA),
                        FormatOption("B.", q.optionB),
                        FormatOption("C.", q.optionC),
                        FormatOption("D.", q.optionD)
                    };
                    newQ.explanation = q.explanation;
                    
                    string co = q.correct.Trim().ToUpper();
                    if (co == "A") newQ.correctIndex = 0;
                    else if (co == "B") newQ.correctIndex = 1;
                    else if (co == "C") newQ.correctIndex = 2;
                    else if (co == "D") newQ.correctIndex = 3;
                    else newQ.correctIndex = 0;

                    dynamicQs.Add(newQ);
                }
            }

            // Nếu không có câu nào khớp với bộ lọc, lấy tất cả câu hỏi Sinh học
            if (dynamicQs.Count == 0)
            {
                foreach (var q in GameData.Instance.allQuestions)
                {
                    Question newQ = new Question();
                    newQ.questionText = q.question;
                    newQ.answers = new string[] {
                        FormatOption("A.", q.optionA),
                        FormatOption("B.", q.optionB),
                        FormatOption("C.", q.optionC),
                        FormatOption("D.", q.optionD)
                    };
                    newQ.explanation = q.explanation;
                    
                    string co = q.correct.Trim().ToUpper();
                    if (co == "A") newQ.correctIndex = 0;
                    else if (co == "B") newQ.correctIndex = 1;
                    else if (co == "C") newQ.correctIndex = 2;
                    else if (co == "D") newQ.correctIndex = 3;
                    else newQ.correctIndex = 0;

                    dynamicQs.Add(newQ);
                }
            }

            if (dynamicQs.Count > 0)
            {
                questions = dynamicQs.ToArray();
            }
            else
            {
                Debug.LogWarning("Không có câu hỏi nào trong GameData! Dùng mặc định.");
            }
        }
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
