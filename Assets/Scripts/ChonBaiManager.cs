using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System.Linq;
using TMPro;

public class ChonBaiManager : MonoBehaviour
{
    [Header("UI Elements")]
    public Transform listContainer;
    public Button batDauButton;
    public Button btnDe;
    public Button btnTrungBinh;
    public Button btnKho;
    public Button btnTongHop;
    public Button btnHomePage;

    [Header("Hình thức làm bài")]
    public Button btnTracNghiem;
    public Button btnTuLuan;
    private string selectedQuestionType = "Trắc nghiệm";

    [Header("Settings")]
    public string monHocFilter = "Sinh học";
    public string webAppUrl = "https://script.google.com/macros/s/AKfycbxLHvShuipgPg7x28IikDmzU7wHoE2e3ai9rhuoEnN50UsFUAWIPTG_nneHOxz-YmaY/exec";
    public string sheetCSVUrl = "https://docs.google.com/spreadsheets/d/1DcJwlN_fUDdcaQ6IfnlkMJApgWuEUqMhyZv9Kn8vfWo/export?format=csv&gid=1463734910";

    [Header("Item Styling")]
    public Sprite itemSprite;

    private List<QuestionData> allQuestions = new List<QuestionData>();
    private List<string> chapters = new List<string>();

    private string selectedChapter = "Tất cả";
    private string selectedDifficulty = "Tổng hợp";

    private Color normalColor = Color.white;
    private Color selectedColor = new Color(0.45f, 0.95f, 0.45f, 1f);

    private Dictionary<string, Button> diffButtons = new Dictionary<string, Button>();
    private List<Button> chapterButtons = new List<Button>();
    private Transform contentContainer;
    private ScrollRect scrollRect;
    private Font defaultFont;

    // 7 chương Sinh học KHTN 6 chuẩn
    private static readonly string[] DefaultBiologyChapters = new string[]
    {
        "Chương 1: Tế bào - Đơn vị cơ sở của sự sống",
        "Chương 2: Từ tế bào đến cơ thể",
        "Chương 3: Virus và Vi khuẩn",
        "Chương 4: Nấm",
        "Chương 5: Giới Thực vật",
        "Chương 6: Giới Động vật",
        "Chương 7: Đa dạng sinh học và Bảo tồn"
    };

    void Awake()
    {
        try
        {
            AutoFindUIElements();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ChonBaiManager] Lỗi trong Awake: {ex}");
        }
    }

    void Start()
    {
        try
        {
            if (batDauButton == null || listContainer == null || contentContainer == null)
            {
                AutoFindUIElements();
            }

            // 1. Cấu hình nút Bắt đầu
            if (batDauButton != null)
            {
                batDauButton.transition = Selectable.Transition.None;
                var nav = batDauButton.navigation;
                nav.mode = Navigation.Mode.None;
                batDauButton.navigation = nav;
                batDauButton.onClick.RemoveAllListeners();
                batDauButton.onClick.AddListener(StartGame);
            }

            // 2. Cấu hình các nút độ khó
            diffButtons.Clear();
            if (btnDe != null) diffButtons["Dễ"] = btnDe;
            if (btnTrungBinh != null) diffButtons["Trung bình"] = btnTrungBinh;
            if (btnKho != null) diffButtons["Khó"] = btnKho;
            if (btnTongHop != null) diffButtons["Tổng hợp"] = btnTongHop;

            foreach (var kvp in diffButtons)
            {
                if (kvp.Value != null)
                {
                    Button btn = kvp.Value;
                    string diff = kvp.Key;
                    btn.transition = Selectable.Transition.None;
                    var nav = btn.navigation;
                    nav.mode = Navigation.Mode.None;
                    btn.navigation = nav;
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => SelectDifficulty(diff));
                }
            }

            // 3. Cấu hình các nút dạng bài Trắc nghiệm / Tự luận
            if (btnTracNghiem != null)
            {
                btnTracNghiem.transition = Selectable.Transition.None;
                var nav = btnTracNghiem.navigation;
                nav.mode = Navigation.Mode.None;
                btnTracNghiem.navigation = nav;
                btnTracNghiem.onClick.RemoveAllListeners();
                btnTracNghiem.onClick.AddListener(() => SelectQuestionType("Trắc nghiệm"));
            }

            if (btnTuLuan != null)
            {
                btnTuLuan.transition = Selectable.Transition.None;
                var nav = btnTuLuan.navigation;
                nav.mode = Navigation.Mode.None;
                btnTuLuan.navigation = nav;
                btnTuLuan.onClick.RemoveAllListeners();
                btnTuLuan.onClick.AddListener(() => SelectQuestionType("Tự luận"));
            }

            // 4. Cấu hình nút HomePage
            if (btnHomePage != null)
            {
                btnHomePage.transition = Selectable.Transition.None;
                var nav = btnHomePage.navigation;
                nav.mode = Navigation.Mode.None;
                btnHomePage.navigation = nav;
                btnHomePage.onClick.RemoveAllListeners();
                btnHomePage.onClick.AddListener(() => {
                    Debug.Log("[ChonBaiManager] Người chơi bấm nút HomePage -> về HomePage");
                    SceneManager.LoadScene("HomePage");
                });
            }

            // 5. Chọn mặc định độ khó và dạng bài
            SelectDifficulty("Tổng hợp");
            SelectQuestionType("Trắc nghiệm");

            // 5. Hiển thị danh sách chương ngay lập tức (Zero-delay)
            InitDefaultData();
            PopulateChapterList();

            // 6. Tải cập nhật dữ liệu từ Google Sheets trong nền (nếu có mạng)
            StartCoroutine(LoadDataRoutine());
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ChonBaiManager] Lỗi trong Start: {ex}");
        }
    }

    private void AutoFindUIElements()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var c in allCanvases)
            {
                if (c.gameObject.scene == gameObject.scene && c.name != "PersistentTimerCanvas")
                {
                    canvas = c;
                    break;
                }
            }
        }
        if (canvas == null)
        {
            Debug.LogError("[ChonBaiManager] Không tìm thấy Canvas trong scene!");
            return;
        }

        Debug.Log($"[ChonBaiManager] AutoFindUIElements trên Canvas: {canvas.name} (scene={canvas.gameObject.scene.name})");

        Transform[] allTransforms = canvas.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in allTransforms)
        {
            if (t.name == "List")
            {
                listContainer = t;
            }
            else if (t.name == "BatDau")
            {
                batDauButton = t.GetComponent<Button>() ?? t.gameObject.AddComponent<Button>();
                Image img = t.GetComponent<Image>();
                if (img != null)
                {
                    img.enabled = true;
                    img.raycastTarget = true;
                    batDauButton.targetGraphic = img;
                }
            }
            else if (t.name == "De")
            {
                btnDe = t.GetComponent<Button>() ?? t.gameObject.AddComponent<Button>();
                Image img = t.GetComponent<Image>();
                if (img != null)
                {
                    img.enabled = true;
                    img.raycastTarget = true;
                    btnDe.targetGraphic = img;
                }
            }
            else if (t.name == "TrungBinh")
            {
                btnTrungBinh = t.GetComponent<Button>() ?? t.gameObject.AddComponent<Button>();
                Image img = t.GetComponent<Image>();
                if (img != null)
                {
                    img.enabled = true;
                    img.raycastTarget = true;
                    btnTrungBinh.targetGraphic = img;
                }
            }
            else if (t.name == "Kho")
            {
                btnKho = t.GetComponent<Button>() ?? t.gameObject.AddComponent<Button>();
                Image img = t.GetComponent<Image>();
                if (img != null)
                {
                    img.enabled = true;
                    img.raycastTarget = true;
                    btnKho.targetGraphic = img;
                }
            }
            else if (t.name == "TongHop")
            {
                btnTongHop = t.GetComponent<Button>() ?? t.gameObject.AddComponent<Button>();
                Image img = t.GetComponent<Image>();
                if (img != null)
                {
                    img.enabled = true;
                    img.raycastTarget = true;
                    btnTongHop.targetGraphic = img;
                }
            }
            else if (t.name == "TracNghiem")
            {
                btnTracNghiem = t.GetComponent<Button>() ?? t.gameObject.AddComponent<Button>();
                Image img = t.GetComponent<Image>();
                if (img != null)
                {
                    img.enabled = true;
                    img.raycastTarget = true;
                    btnTracNghiem.targetGraphic = img;
                }
            }
            else if (t.name == "TuLuan")
            {
                btnTuLuan = t.GetComponent<Button>() ?? t.gameObject.AddComponent<Button>();
                Image img = t.GetComponent<Image>();
                if (img != null)
                {
                    img.enabled = true;
                    img.raycastTarget = true;
                    btnTuLuan.targetGraphic = img;
                }
            }
            else if (t.name == "HomePage")
            {
                btnHomePage = t.GetComponent<Button>() ?? t.gameObject.AddComponent<Button>();
                Image img = t.GetComponent<Image>();
                if (img != null)
                {
                    img.enabled = true;
                    img.raycastTarget = true;
                    btnHomePage.targetGraphic = img;
                }
                btnHomePage.onClick.RemoveAllListeners();
                btnHomePage.onClick.AddListener(() => SceneManager.LoadScene("HomePage"));
            }
            else if (t.name == "ChonDang" || t.name == "KhungChonLoai" || t.name == "KhungDoKho" || t.name == "KhungChonBai" || t.name == "ChonChuong" || t.name == "ChonDoKho" || t.name == "Background")
            {
                // Tắt raycastTarget trên các khung nền trang trí để chuột click xuyên qua tới nút
                Image img = t.GetComponent<Image>();
                if (img != null) img.raycastTarget = false;
            }
        }

        // CẤU HÌNH BẢO VỆ CHO KHUNG DANH SÁCH (List, Viewport, Content)
        if (listContainer != null)
        {
            scrollRect = listContainer.GetComponent<ScrollRect>();
            if (scrollRect == null)
            {
                scrollRect = listContainer.gameObject.AddComponent<ScrollRect>();
            }

            // Tắt thanh cuộn hỏng của scene để tránh ScrollRect tự co Viewport về 0x0
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 40f;
            scrollRect.horizontalScrollbar = null;
            scrollRect.verticalScrollbar = null;
            scrollRect.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

            // Ẩn 2 scrollbar rỗng có sẵn trong scene để không gây lỗi layout
            Transform sbV = listContainer.Find("Scrollbar Vertical");
            if (sbV != null) sbV.gameObject.SetActive(false);
            Transform sbH = listContainer.Find("Scrollbar Horizontal");
            if (sbH != null) sbH.gameObject.SetActive(false);

            // Cấu hình Viewport
            Transform vpTransform = listContainer.Find("Viewport");
            if (vpTransform == null)
            {
                GameObject vpObj = new GameObject("Viewport", typeof(RectTransform));
                vpObj.transform.SetParent(listContainer, false);
                vpTransform = vpObj.transform;
            }

            RectTransform vpRt = vpTransform.GetComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = Vector2.zero;
            vpRt.offsetMax = Vector2.zero;
            vpRt.anchoredPosition = Vector2.zero;
            vpRt.sizeDelta = Vector2.zero;
            vpRt.pivot = new Vector2(0.5f, 0.5f);

            // Thay thế Mask bằng RectMask2D một cách an toàn (không dùng DestroyImmediate)
            Mask oldMask = vpTransform.GetComponent<Mask>();
            if (oldMask != null)
            {
                oldMask.enabled = false;
                Destroy(oldMask);
            }

            RectMask2D rectMask = vpTransform.GetComponent<RectMask2D>();
            if (rectMask == null)
            {
                rectMask = vpTransform.gameObject.AddComponent<RectMask2D>();
            }
            rectMask.enabled = true;

            // Tắt Image trên Viewport để không chắn raycast và không gây lỗi sliced rỗng
            Image vpImg = vpTransform.GetComponent<Image>();
            if (vpImg != null)
            {
                vpImg.enabled = false;
                vpImg.raycastTarget = false;
            }

            scrollRect.viewport = vpRt;

            // Cấu hình Content
            Transform ct = vpTransform.Find("Content");
            if (ct == null) ct = listContainer.Find("Content");
            if (ct == null)
            {
                GameObject contentObj = new GameObject("Content", typeof(RectTransform));
                contentObj.transform.SetParent(vpTransform, false);
                ct = contentObj.transform;
            }
            else
            {
                ct.SetParent(vpTransform, false);
            }
            contentContainer = ct;

            RectTransform cRt = contentContainer.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0, 1);
            cRt.anchorMax = new Vector2(1, 1);
            cRt.pivot = new Vector2(0.5f, 1f);
            cRt.anchoredPosition = Vector2.zero;
            cRt.sizeDelta = new Vector2(0, 0);
            scrollRect.content = cRt;

            // VerticalLayoutGroup trên Content
            VerticalLayoutGroup vlg = contentContainer.GetComponent<VerticalLayoutGroup>();
            if (vlg == null) vlg = contentContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 12;
            vlg.padding = new RectOffset(10, 10, 10, 10);

            // ContentSizeFitter trên Content
            ContentSizeFitter csf = contentContainer.GetComponent<ContentSizeFitter>();
            if (csf == null) csf = contentContainer.gameObject.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Tắt raycastTarget trên ảnh nền của List
            Image listImg = listContainer.GetComponent<Image>();
            if (listImg != null) listImg.raycastTarget = false;
        }
    }

    private void InitDefaultData()
    {
        chapters.Clear();
        foreach (string ch in DefaultBiologyChapters)
        {
            if (!chapters.Contains(ch))
            {
                chapters.Add(ch);
            }
        }

        if (allQuestions.Count == 0)
        {
            for (int i = 0; i < DefaultBiologyChapters.Length; i++)
            {
                string ch = DefaultBiologyChapters[i];
                allQuestions.Add(new QuestionData
                {
                    id = $"BIO_DEF_{i+1}_1",
                    monId = "Sinh học",
                    topic = ch,
                    difficulty = "Dễ",
                    question = $"Câu hỏi luyện tập {ch} (Mức Dễ)",
                    optionA = "Phương án A",
                    optionB = "Phương án B",
                    optionC = "Phương án C",
                    optionD = "Phương án D",
                    correct = "A",
                    explanation = $"Giải thích chi tiết cho kiến thức {ch}.",
                    points = 10
                });
                allQuestions.Add(new QuestionData
                {
                    id = $"BIO_DEF_{i+1}_2",
                    monId = "Sinh học",
                    topic = ch,
                    difficulty = "Trung bình",
                    question = $"Câu hỏi luyện tập {ch} (Mức Trung bình)",
                    optionA = "Phương án A",
                    optionB = "Phương án B",
                    optionC = "Phương án C",
                    optionD = "Phương án D",
                    correct = "B",
                    explanation = $"Giải thích chi tiết cho kiến thức {ch}.",
                    points = 20
                });
                allQuestions.Add(new QuestionData
                {
                    id = $"BIO_DEF_{i+1}_3",
                    monId = "Sinh học",
                    topic = ch,
                    difficulty = "Khó",
                    question = $"Câu hỏi luyện tập {ch} (Mức Khó)",
                    optionA = "Phương án A",
                    optionB = "Phương án B",
                    optionC = "Phương án C",
                    optionD = "Phương án D",
                    correct = "C",
                    explanation = $"Giải thích chi tiết cho kiến thức {ch}.",
                    points = 30
                });
            }
        }
    }

    private IEnumerator LoadDataRoutine()
    {
        bool loaded = false;

        string apiUrl = $"{webAppUrl}?action=questions&subject={UnityWebRequest.EscapeURL("Sinh học")}";
        using (UnityWebRequest req = UnityWebRequest.Get(apiUrl))
        {
            req.certificateHandler = new BypassCertificate();
            req.timeout = 8;
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success && !string.IsNullOrEmpty(req.downloadHandler.text))
            {
                try
                {
                    QuestionList apiData = JsonUtility.FromJson<QuestionList>(req.downloadHandler.text);
                    if (apiData != null && apiData.questions != null && apiData.questions.Length > 0)
                    {
                        allQuestions.Clear();
                        chapters.Clear();

                        foreach (var q in apiData.questions)
                        {
                            allQuestions.Add(q);
                            string topic = q.topic != null ? q.topic.Trim() : "";
                            if (!string.IsNullOrEmpty(topic) && !chapters.Contains(topic))
                            {
                                chapters.Add(topic);
                            }
                        }

                        loaded = true;
                        Debug.Log($"[ChonBaiManager] Tải thành công {allQuestions.Count} câu hỏi từ API ({chapters.Count} chương).");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[ChonBaiManager] Phân tích JSON thất bại: " + ex.Message);
                }
            }
        }

        if (!loaded && !string.IsNullOrEmpty(sheetCSVUrl))
        {
            using (UnityWebRequest req = UnityWebRequest.Get(sheetCSVUrl))
            {
                req.certificateHandler = new BypassCertificate();
                req.timeout = 8;
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success && !string.IsNullOrEmpty(req.downloadHandler.text))
                {
                    ParseCSV(req.downloadHandler.text);
                    if (allQuestions.Count > 0)
                    {
                        loaded = true;
                    }
                }
            }
        }

        if (loaded)
        {
            PopulateChapterList();
        }
    }

    private void ParseCSV(string csvData)
    {
        List<string[]> parsedRows = ParseCSVFully(csvData);
        if (parsedRows == null || parsedRows.Count <= 1) return;

        allQuestions.Clear();
        chapters.Clear();

        bool isFirstRow = true;
        foreach (string[] cols in parsedRows)
        {
            if (isFirstRow)
            {
                isFirstRow = false;
                continue;
            }

            if (cols.Length >= 11)
            {
                string mon = cols[1].Trim();
                if (mon.IndexOf("Sinh", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    QuestionData q = new QuestionData
                    {
                        id = cols[0].Trim(),
                        monId = mon,
                        topic = cols[2].Trim(),
                        difficulty = cols[3].Trim(),
                        question = cols[4].Trim(),
                        optionA = cols.Length > 5 ? cols[5].Trim() : "",
                        optionB = cols.Length > 6 ? cols[6].Trim() : "",
                        optionC = cols.Length > 7 ? cols[7].Trim() : "",
                        optionD = cols.Length > 8 ? cols[8].Trim() : "",
                        correct = cols.Length > 9 ? cols[9].Trim() : "A",
                        explanation = cols.Length > 10 ? cols[10].Trim() : "",
                        points = (cols.Length > 11 && int.TryParse(cols[11], out int p)) ? p : 10
                    };

                    allQuestions.Add(q);
                    string chapterName = q.topic;
                    if (!string.IsNullOrEmpty(chapterName) && !chapters.Contains(chapterName))
                    {
                        chapters.Add(chapterName);
                    }
                }
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

    private void PopulateChapterList()
    {
        if (contentContainer == null)
        {
            Debug.LogWarning("[ChonBaiManager] contentContainer đang null, gọi lại AutoFindUIElements...");
            AutoFindUIElements();
            if (contentContainer == null)
            {
                Debug.LogError("[ChonBaiManager] Không thể khởi tạo contentContainer cho danh sách chương!");
                return;
            }
        }

        // Xóa sạch các nút cũ lập tức
        for (int i = contentContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(contentContainer.GetChild(i).gameObject);
        }
        chapterButtons.Clear();

        // 1. Thêm mục "Tất cả"
        CreateChapterButton("Tất cả");

        // 2. Thêm từng chương
        foreach (string ch in chapters)
        {
            CreateChapterButton(ch);
        }

        if (string.IsNullOrEmpty(selectedChapter) || (!selectedChapter.Equals("Tất cả") && !chapters.Contains(selectedChapter)))
        {
            selectedChapter = "Tất cả";
        }
        SelectChapter(selectedChapter);

        // Buộc Unity cập nhật Layout ngay lập tức
        if (contentContainer is RectTransform rt)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        }

        Debug.Log($"[ChonBaiManager] Đã tạo danh sách {chapterButtons.Count} mục chương trong ScrollView.");
    }

    private void CreateChapterButton(string chapterName)
    {
        GameObject btnObj = new GameObject("Btn_" + chapterName, typeof(RectTransform));
        btnObj.transform.SetParent(contentContainer, false);
        btnObj.layer = 5;

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 65);

        LayoutElement le = btnObj.AddComponent<LayoutElement>();
        le.minHeight = 65;
        le.preferredHeight = 65;
        le.flexibleWidth = 1;

        Image img = btnObj.AddComponent<Image>();
        if (itemSprite != null)
        {
            img.sprite = itemSprite;
            img.type = Image.Type.Sliced;
        }
        else
        {
            img.type = Image.Type.Simple;
        }
        img.color = new Color(0.96f, 0.95f, 0.91f, 1f);
        img.raycastTarget = true;

        Outline outline = btnObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.78f, 0.74f, 0.65f, 0.8f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        Button btn = btnObj.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        var nav = btn.navigation;
        nav.mode = Navigation.Mode.None;
        btn.navigation = nav;
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => {
            Debug.Log($"[ChonBaiManager] Đã chọn chương: {chapterName}");
            SelectChapter(chapterName);
        });
        chapterButtons.Add(btn);

        // Tạo Text hiển thị bằng Text tiêu chuẩn
        GameObject textObj = new GameObject("Text", typeof(RectTransform));
        textObj.transform.SetParent(btnObj.transform, false);
        textObj.layer = 5;

        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(24, 6);
        textRt.offsetMax = new Vector2(-24, -6);

        Text txt = textObj.AddComponent<Text>();
        if (defaultFont == null)
        {
            defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        txt.font = defaultFont;
        txt.fontSize = 22;
        txt.fontStyle = FontStyle.Bold;
        txt.color = new Color(0.18f, 0.14f, 0.08f, 1f);
        txt.alignment = chapterName.Equals("Tất cả") ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
        txt.horizontalOverflow = HorizontalWrapMode.Wrap;
        txt.verticalOverflow = VerticalWrapMode.Truncate;
        txt.raycastTarget = false;
        txt.text = chapterName;
    }

    private void SelectChapter(string chapterName)
    {
        selectedChapter = chapterName;

        for (int i = 0; i < chapterButtons.Count; i++)
        {
            Button btn = chapterButtons[i];
            if (btn == null) continue;

            Text txt = btn.GetComponentInChildren<Text>();
            Image img = btn.GetComponent<Image>();
            Outline outline = btn.GetComponent<Outline>();

            if (txt != null && txt.text == chapterName)
            {
                if (img != null) img.color = new Color(0.35f, 0.85f, 0.35f, 1f);
                txt.color = Color.white;
                if (outline != null)
                {
                    outline.enabled = true;
                    outline.effectColor = new Color(0.08f, 0.45f, 0.12f, 1f);
                    outline.effectDistance = new Vector2(3f, -3f);
                }
                btn.transform.localScale = new Vector3(1.02f, 1.02f, 1f);
            }
            else
            {
                if (img != null) img.color = new Color(0.96f, 0.95f, 0.91f, 1f);
                if (txt != null)
                {
                    txt.color = new Color(0.18f, 0.14f, 0.08f, 1f);
                }
                if (outline != null)
                {
                    outline.enabled = true;
                    outline.effectColor = new Color(0.78f, 0.74f, 0.65f, 0.8f);
                    outline.effectDistance = new Vector2(1.5f, -1.5f);
                }
                btn.transform.localScale = Vector3.one;
            }
        }
    }

    private void SelectDifficulty(string difficulty)
    {
        selectedDifficulty = difficulty;
        Debug.Log($"[ChonBaiManager] Đã chọn độ khó: {difficulty}");

        foreach (var kvp in diffButtons)
        {
            if (kvp.Value != null)
            {
                Button btn = kvp.Value;
                btn.transition = Selectable.Transition.None;
                Image img = btn.GetComponent<Image>();
                Outline outline = btn.GetComponent<Outline>() ?? btn.gameObject.AddComponent<Outline>();

                bool isSelected = (kvp.Key == difficulty);
                if (img != null)
                {
                    img.color = isSelected ? selectedColor : normalColor;
                }

                if (isSelected)
                {
                    btn.transform.localScale = new Vector3(1.06f, 1.06f, 1f);
                    outline.enabled = true;
                    outline.effectColor = new Color(0.12f, 0.85f, 0.25f, 1f);
                    outline.effectDistance = new Vector2(3f, -3f);
                }
                else
                {
                    btn.transform.localScale = Vector3.one;
                    outline.enabled = false;
                }
            }
        }
    }

    private void ApplyQuestionTypeHighlight(Button btn, bool isSelected)
    {
        if (btn == null) return;

        btn.transition = Selectable.Transition.None;
        var nav = btn.navigation;
        nav.mode = Navigation.Mode.None;
        btn.navigation = nav;

        Image img = btn.GetComponent<Image>();
        Outline outline = btn.GetComponent<Outline>() ?? btn.gameObject.AddComponent<Outline>();

        if (isSelected)
        {
            if (img != null) img.color = selectedColor;
            btn.transform.localScale = new Vector3(1.08f, 1.08f, 1f);
            outline.enabled = true;
            outline.effectColor = new Color(0.12f, 0.85f, 0.25f, 1f);
            outline.effectDistance = new Vector2(4f, -4f);
        }
        else
        {
            if (img != null) img.color = normalColor;
            btn.transform.localScale = Vector3.one;
            outline.enabled = false;
        }
    }

    private void SelectQuestionType(string type)
    {
        bool isTuLuan = type != null && (
            type.IndexOf("Tự", StringComparison.OrdinalIgnoreCase) >= 0 ||
            type.IndexOf("Luận", StringComparison.OrdinalIgnoreCase) >= 0 ||
            type.Equals("TuLuan", StringComparison.OrdinalIgnoreCase));

        selectedQuestionType = isTuLuan ? "Tự luận" : "Trắc nghiệm";
        GameSession.QuestionType = selectedQuestionType;
        GameSession.questionSceneName = isTuLuan ? "TuLuan" : "bean_1_question";
        PlayerPrefs.SetString("SelectedQuestionType", selectedQuestionType);

        ApplyQuestionTypeHighlight(btnTracNghiem, !isTuLuan);
        ApplyQuestionTypeHighlight(btnTuLuan, isTuLuan);

        Debug.Log($"[ChonBaiManager] Đã chọn dạng bài: {selectedQuestionType} (questionSceneName={GameSession.questionSceneName})");
    }

    private void StartGame()
    {
        Debug.Log($"[ChonBaiManager] Người chơi bấm nút 'Bắt đầu'!");

        PlayerPrefs.SetString("SelectedChapter", selectedChapter);
        PlayerPrefs.SetString("SelectedDifficulty", selectedDifficulty);

        bool isTuLuan = selectedQuestionType != null && (
            selectedQuestionType.IndexOf("Tự", StringComparison.OrdinalIgnoreCase) >= 0 ||
            selectedQuestionType.IndexOf("Luận", StringComparison.OrdinalIgnoreCase) >= 0 ||
            selectedQuestionType.Equals("TuLuan", StringComparison.OrdinalIgnoreCase));

        GameSession.QuestionType = isTuLuan ? "Tự luận" : "Trắc nghiệm";
        GameSession.Subject = "Sinh học";
        GameSession.questionSceneName = isTuLuan ? "TuLuan" : "bean_1_question";

        PlayerPrefs.SetString("SelectedQuestionType", GameSession.QuestionType);
        PlayerPrefs.Save();

        if (GameData.Instance == null)
        {
            GameObject go = new GameObject("GameData");
            go.AddComponent<GameData>();
        }
        GameData.Instance.allQuestions = new List<QuestionData>(allQuestions);

        Debug.Log($"[ChonBaiManager] Bắt đầu chơi: Môn={GameSession.Subject}, Dạng={GameSession.QuestionType}, Chương={selectedChapter}, Độ khó={selectedDifficulty}, TargetScene={GameSession.questionSceneName}, Tổng câu hỏi={allQuestions.Count}");

        SceneManager.LoadScene("luatchoisinhhoc");
    }
}
