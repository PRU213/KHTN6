using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;

/// <summary>
/// Quản lý câu hỏi dùng UI Text mặc định. Liên kết với GameData.
/// Hiển thị GiaiThich kèm txtGiaiThich khi user chọn sai đáp án.
/// Đáp án đúng hiện xanh, đáp án sai hiện đỏ.
/// </summary>
public class QuestionManager : MonoBehaviour
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

    // === CÂU HỎI SINH HỌC DỰ PHÒNG (KÈM GIẢI THÍCH) ===
    private Question[] questions = new Question[]
    {
        new Question {
            questionText = "Hạt đậu cần những điều kiện gì để nảy mầm?",
            answers = new string[] {
                "A. Nước, nhiệt độ thích hợp, không khí",
                "B. Chỉ cần ánh sáng mặt trời",
                "C. Chỉ cần đất màu mỡ",
                "D. Chỉ cần bón phân" },
            correctIndex = 0,
            explanation = "Hạt đậu cần có đủ nước, không khí và nhiệt độ thích hợp để nảy mầm."
        },
        new Question {
            questionText = "Phần nào của hạt đậu sẽ mọc ra đầu tiên khi nảy mầm?",
            answers = new string[] {
                "A. Lá mầm",
                "B. Rễ mầm",
                "C. Thân mầm",
                "D. Chồi non" },
            correctIndex = 1,
            explanation = "Rễ mầm sẽ mọc ra đầu tiên để hút nước và khoáng chất nuôi phôi phát triển."
        },
        new Question {
            questionText = "Lá mầm của hạt đậu có chức năng chính là gì?",
            answers = new string[] {
                "A. Quang hợp tạo chất hữu cơ",
                "B. Dự trữ chất dinh dưỡng nuôi phôi",
                "C. Hút nước từ đất",
                "D. Bảo vệ hạt khỏi côn trùng" },
            correctIndex = 1,
            explanation = "Lá mầm chứa chất dinh dưỡng dự trữ để nuôi phôi phát triển thành cây con."
        },
        new Question {
            questionText = "Trong quá trình nảy mầm, nước có vai trò gì?",
            answers = new string[] {
                "A. Làm mềm vỏ hạt và kích hoạt enzym",
                "B. Tạo màu sắc cho cây",
                "C. Làm hạt cứng hơn",
                "D. Giúp hạt bay đi xa" },
            correctIndex = 0,
            explanation = "Nước làm mềm vỏ hạt, giúp các enzym hoạt động chuyển hóa chất dinh dưỡng nuôi mầm."
        },
        new Question {
            questionText = "Hạt đậu gồm những bộ phận chính nào?",
            answers = new string[] {
                "A. Vỏ hạt, phôi và chất dự trữ",
                "B. Rễ, thân và lá",
                "C. Hoa, quả và hạt",
                "D. Biểu bì, vỏ và lõi" },
            correctIndex = 0,
            explanation = "Hạt gồm có vỏ hạt, phôi và chất dinh dưỡng dự trữ."
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

            // Nếu người chơi chưa trả lời câu đầu tiên, cập nhật câu hỏi mới từ sheet
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

            // Nếu không có câu nào khớp với độ khó / chương cụ thể, lấy tất cả câu hỏi Sinh học
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
        float maxBoardArea = 0f;
        List<Image> candidateAnswers = new List<Image>();

        foreach (Image img in allImages)
        {
            RectTransform rt = img.rectTransform;
            string objName = img.gameObject.name.ToLower().Trim();
            float area = rt.sizeDelta.x * rt.sizeDelta.y;

            // Bỏ qua ảnh nền
            if (rt.anchorMin == Vector2.zero && rt.anchorMax == Vector2.one) continue;
            if (objName.Contains("bg") || objName.Contains("background")) continue;

            // Tìm GiaiThich GameObject
            if (objName == "giaithich" || objName.Contains("giaithich"))
            {
                giaiThichObject = img.gameObject;
                continue;
            }

            // Tìm Đáp Án (Hỗ trợ màn 1, màn 2, màn 3 và các biến thể)
            bool isAnsA = objName.Contains("đáp án a") || objName.Contains("dap an a") || objName.Contains("đápán a") || objName == "dapan1" || objName == "dapan (1)" || objName == "dapan(1)";
            bool isAnsB = objName.Contains("đáp án b") || objName.Contains("dap an b") || objName.Contains("đápán b") || objName == "dapan2" || objName == "dapan (2)" || objName == "dapan(2)";
            bool isAnsC = objName.Contains("đáp án c") || objName.Contains("dap an c") || objName.Contains("đápán c") || objName == "dapan3" || objName == "dapan (3)" || objName == "dapan(3)";
            bool isAnsD = objName.Contains("đáp án d") || objName.Contains("dap an d") || objName.Contains("đápán d") || objName == "dapan4" || objName == "dapan (4)" || objName == "dapan(4)";

            if (isAnsA) answerImages[0] = img;
            else if (isAnsB) answerImages[1] = img;
            else if (isAnsC) answerImages[2] = img;
            else if (isAnsD) answerImages[3] = img;
            else if (objName.Contains("dapan") || objName.Contains("đáp án") || objName.Contains("đápán"))
            {
                candidateAnswers.Add(img);
            }
            
            // Tìm Máu (Hỗ trợ cả màn 1 và màn 2)
            else if (objName.Contains("máu 1") || objName.Contains("mau 1") || objName == "tim1") healthIcons[0] = img;
            else if (objName.Contains("máu 2") || objName.Contains("mau 2") || objName == "tim2") healthIcons[1] = img;
            else if (objName.Contains("máu 3") || objName.Contains("mau 3") || objName == "tim3") healthIcons[2] = img;

            // Tìm Khung Bảng (Khung lớn nhất còn lại hoặc tên là Khung)
            else if (objName == "khung" || (area > maxBoardArea && rt.sizeDelta.x > 300 && rt.sizeDelta.y > 150))
            {
                if (objName == "khung" || (rt.sizeDelta.x > 500 && rt.sizeDelta.y > 200))
                {
                    maxBoardArea = area;
                    questionBoard = img;
                }
            }
        }

        // Bổ sung đáp án nếu nhận diện trực tiếp bằng tên chưa đủ 4 nút
        for (int i = 0; i < 4; i++)
        {
            if (answerImages[i] == null && candidateAnswers.Count > 0)
            {
                // Sắp xếp theo vị trí Y (từ trên xuống), rồi X (từ trái qua)
                candidateAnswers.Sort((a, b) => {
                    int yComp = b.rectTransform.position.y.CompareTo(a.rectTransform.position.y);
                    if (Mathf.Abs(b.rectTransform.position.y - a.rectTransform.position.y) < 10)
                        return a.rectTransform.position.x.CompareTo(b.rectTransform.position.x);
                    return yComp;
                });
                answerImages[i] = candidateAnswers[0];
                candidateAnswers.RemoveAt(0);
            }
        }

        // Tìm GiaiThich và txtGiaiThich trong toàn bộ hierarchy nếu chưa tìm thấy
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
            // Tìm txtGiaiThich bên trong giaiThichObject
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

        // Tìm kiếm toàn cục trên Canvas nếu vẫn chưa thấy
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
            // Câu hỏi nằm ở nửa trên của vùng trống
            questionText = CreateText("QuestionText", questionBoard.transform, font,
                new Vector2(0.1f, 0.45f), new Vector2(0.9f, 0.75f),
                36, Color.black, TextAnchor.MiddleCenter, FontStyle.Bold); 

            // Feedback ở trên cái bảng tên
            feedbackText = CreateText("FeedbackText", questionBoard.transform, font,
                new Vector2(0.2f, 0.82f), new Vector2(0.8f, 0.98f),
                30, Color.black, TextAnchor.MiddleCenter, FontStyle.Bold); 
            feedbackText.text = "";
        }

        for (int i = 0; i < 4; i++)
        {
            if (answerImages[i] == null) continue;
            if (i == 0) originalAnswerColor = answerImages[i].color;

            Button btn = answerImages[i].gameObject.GetComponent<Button>();
            if (btn == null) btn = answerImages[i].gameObject.AddComponent<Button>();
            answerButtons[i] = btn;

            answerTexts[i] = CreateText("AnsText", answerImages[i].transform, font,
                Vector2.zero, Vector2.one,
                26, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold,
                new Vector2(15, 10), new Vector2(-15, -10));

            int idx = i;
            btn.onClick.AddListener(() => OnAnswerClicked(idx));
        }

        // Cấu hình bảng GiaiThich
        if (giaiThichObject != null)
        {
            giaiThichObject.SetActive(false); // Ban đầu ẩn đi

            // Thêm nút bấm vào bảng Giải Thích để người chơi có thể click bỏ qua sớm nếu muốn
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

    private void ShowQuestion()
    {
        Question q = questions[currentQuestion];
        if (questionText != null) questionText.text = q.questionText;
        if (feedbackText != null) feedbackText.text = "";

        // Ẩn bảng giải thích khi hiển thị câu hỏi mới
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
            Invoke(nameof(ReturnToMainScene), 2f);
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

    private void ReturnToMainScene()
    {
        if (GameData.Instance != null && !string.IsNullOrEmpty(GameData.Instance.lastSceneName))
        {
            SceneManager.LoadScene(GameData.Instance.lastSceneName);
            return;
        }

        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene.Contains("Bean_2")) SceneManager.LoadScene("Bean_2_Biology");
        else if (currentScene.Contains("Bean_3")) SceneManager.LoadScene("Bean_3_Biology");
        else SceneManager.LoadScene("bean_1");
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
        ReturnToMainScene();
    }

    private void UpdateHealthIcons()
    {
        int currentHp = (GameData.Instance != null) ? GameData.Instance.currentHealth : 3;
        for (int i = 0; i < 3; i++)
        {
            if (healthIcons[i] != null)
            {
                healthIcons[i].enabled = (i < currentHp);
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
