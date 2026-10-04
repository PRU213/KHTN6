using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System.Linq;

public class ChonBaiManager : MonoBehaviour
{
    [Header("UI Elements")]
    public Transform listContainer;
    public Button batDauButton;
    public Button btnDe;
    public Button btnTrungBinh;
    public Button btnKho;
    public Button btnTongHop;

    [Header("Settings")]
    public string monHocFilter = "Sinh học"; // Changed to Sinh học as per explicit mentions
    public string sheetCSVUrl = "https://docs.google.com/spreadsheets/d/1DcJwlN_fUDdcaQ6IfnlkMJApgWuEUqMhyZv9Kn8vfWo/export?format=csv&gid=1463734910";
    
    [Header("Item Styling")]
    public Sprite itemSprite; // Ảnh gắn vào các nút/chương
    
    private List<QuestionData> allQuestions = new List<QuestionData>();
    private List<string> chapters = new List<string>();

    private string selectedChapter = "Tất cả";
    private string selectedDifficulty = "Tổng hợp";

    private Color normalColor = Color.white;
    private Color selectedColor = new Color(0.5f, 1f, 0.5f); // Light green

    private Dictionary<string, Button> diffButtons = new Dictionary<string, Button>();
    private List<Button> chapterButtons = new List<Button>();
    private Transform contentContainer; // Thêm contentContainer cho ScrollRect

    void Start()
    {
        AutoFindUIElements();

        diffButtons.Add("Dễ", btnDe);
        diffButtons.Add("Trung bình", btnTrungBinh);
        diffButtons.Add("Khó", btnKho);
        diffButtons.Add("Tổng hợp", btnTongHop);

        foreach (var kvp in diffButtons)
        {
            if (kvp.Value != null)
            {
                Button btn = kvp.Value;
                string diff = kvp.Key;
                btn.onClick.AddListener(() => SelectDifficulty(diff));
            }
        }

        if (batDauButton != null)
        {
            batDauButton.onClick.AddListener(StartGame);
        }

        SelectDifficulty("Tổng hợp");

        StartCoroutine(LoadDataFromSheet());
    }

    private void AutoFindUIElements()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        Transform[] allTransforms = canvas.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in allTransforms)
        {
            if (t.name == "List") listContainer = t;
            else if (t.name == "BatDau") batDauButton = t.GetComponent<Button>() ?? t.gameObject.AddComponent<Button>();
            else if (t.name == "De") btnDe = t.GetComponent<Button>() ?? t.gameObject.AddComponent<Button>();
            else if (t.name == "TrungBinh") btnTrungBinh = t.GetComponent<Button>() ?? t.gameObject.AddComponent<Button>();
            else if (t.name == "Kho") btnKho = t.GetComponent<Button>() ?? t.gameObject.AddComponent<Button>();
            else if (t.name == "TongHop") btnTongHop = t.GetComponent<Button>() ?? t.gameObject.AddComponent<Button>();
        }

        if (listContainer != null)
        {
            ScrollRect scrollRect = listContainer.GetComponent<ScrollRect>();
            if (scrollRect != null && scrollRect.content != null)
            {
                // Nếu đã là một ScrollView chuẩn của Unity (có Viewport, Content, Scrollbar)
                contentContainer = scrollRect.content;
            }
            else
            {
                // Nếu chỉ là một frame rỗng bình thường do người dùng tự tạo
                if (scrollRect == null)
                {
                    scrollRect = listContainer.gameObject.AddComponent<ScrollRect>();
                    scrollRect.horizontal = false;
                    scrollRect.vertical = true;
                    scrollRect.movementType = ScrollRect.MovementType.Clamped;
                    scrollRect.scrollSensitivity = 35f;

                    if (listContainer.GetComponent<RectMask2D>() == null && listContainer.GetComponent<Mask>() == null)
                    {
                        listContainer.gameObject.AddComponent<RectMask2D>();
                    }
                }

                Transform existingContent = listContainer.Find("Content");
                if (existingContent != null)
                {
                    contentContainer = existingContent;
                }
                else
                {
                    GameObject contentObj = new GameObject("Content", typeof(RectTransform));
                    contentObj.transform.SetParent(listContainer, false);
                    contentContainer = contentObj.transform;

                    RectTransform rt = contentContainer.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0, 1);
                    rt.anchorMax = new Vector2(1, 1);
                    rt.pivot = new Vector2(0.5f, 1);
                    rt.sizeDelta = new Vector2(0, 0);
                    rt.anchoredPosition = Vector2.zero;

                    scrollRect.content = rt;
                }
            }

            // Đảm bảo Content có VerticalLayoutGroup và ContentSizeFitter
            if (contentContainer.GetComponent<VerticalLayoutGroup>() == null)
            {
                VerticalLayoutGroup vlg = contentContainer.gameObject.AddComponent<VerticalLayoutGroup>();
                vlg.childControlHeight = false;
                vlg.childControlWidth = true;
                vlg.childForceExpandHeight = false;
                vlg.spacing = 10;
                vlg.padding = new RectOffset(10, 10, 10, 10);
            }

            if (contentContainer.GetComponent<ContentSizeFitter>() == null)
            {
                ContentSizeFitter csf = contentContainer.gameObject.AddComponent<ContentSizeFitter>();
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            // Dọn dẹp component dư thừa nếu có trên khung ngoài cùng (listContainer)
            VerticalLayoutGroup oldVlg = listContainer.GetComponent<VerticalLayoutGroup>();
            if (oldVlg != null && listContainer != contentContainer) Destroy(oldVlg);
            ContentSizeFitter oldCsf = listContainer.GetComponent<ContentSizeFitter>();
            if (oldCsf != null && listContainer != contentContainer) Destroy(oldCsf);
        }
    }

    IEnumerator LoadDataFromSheet()
    {
        using (UnityWebRequest req = UnityWebRequest.Get(sheetCSVUrl))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                ParseCSV(req.downloadHandler.text);
                PopulateChapterList();
            }
            else
            {
                Debug.LogError("Failed to load questions: " + req.error);
            }
        }
    }

    private void ParseCSV(string csvData)
    {
        allQuestions.Clear();
        chapters.Clear();

        List<string[]> parsedRows = ParseCSVFully(csvData);
        bool isFirstRow = true;

        foreach (string[] cols in parsedRows)
        {
            if (isFirstRow)
            {
                isFirstRow = false;
                continue; // Skip header
            }

            if (cols.Length >= 12)
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
                    points = int.TryParse(cols[11], out int p) ? p : 10
                };

                // Chỉ lấy môn Sinh học theo yêu cầu
                if (q.monId.Trim().Equals("Sinh học", StringComparison.OrdinalIgnoreCase))
                {
                    allQuestions.Add(q);
                    string chapterName = q.topic.Trim();
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
                    // Escaped quote ""
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

                // Tránh add dòng trắng thừa
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
        if (contentContainer == null) return;

        // Clear existing children
        foreach (Transform child in contentContainer)
        {
            Destroy(child.gameObject);
        }
        chapterButtons.Clear();

        // Add "Tất cả" option
        CreateChapterButton("Tất cả");

        // Add unique chapters
        foreach (string chapter in chapters)
        {
            CreateChapterButton(chapter);
        }

        SelectChapter("Tất cả"); // Default
    }

    private void CreateChapterButton(string chapterName)
    {
        GameObject btnObj = new GameObject("Btn_" + chapterName, typeof(RectTransform));
        btnObj.transform.SetParent(contentContainer, false);
        btnObj.layer = 5; // UI Layer

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 60);

        Image img = btnObj.AddComponent<Image>();
        if (itemSprite != null)
        {
            img.sprite = itemSprite;
            img.type = Image.Type.Sliced; // Hỗ trợ co giãn nếu ảnh có viền
        }
        img.color = normalColor;

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => SelectChapter(chapterName));
        chapterButtons.Add(btn);

        GameObject textObj = new GameObject("Text", typeof(RectTransform));
        textObj.transform.SetParent(btnObj.transform, false);
        textObj.layer = 5;

        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.sizeDelta = Vector2.zero;

        Text txt = textObj.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 28;
        txt.color = Color.black;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.text = chapterName;
    }

    private void SelectChapter(string chapterName)
    {
        selectedChapter = chapterName;

        for (int i = 0; i < chapterButtons.Count; i++)
        {
            Text txt = chapterButtons[i].GetComponentInChildren<Text>();
            Image img = chapterButtons[i].GetComponent<Image>();

            if (txt.text == chapterName)
            {
                img.color = selectedColor;
            }
            else
            {
                img.color = normalColor;
            }
        }
    }

    private void SelectDifficulty(string difficulty)
    {
        selectedDifficulty = difficulty;

        foreach (var kvp in diffButtons)
        {
            if (kvp.Value != null)
            {
                Image img = kvp.Value.GetComponent<Image>();
                if (img != null)
                {
                    img.color = (kvp.Key == difficulty) ? selectedColor : normalColor;
                }
            }
        }
    }

    private void StartGame()
    {
        // Store selected options
        PlayerPrefs.SetString("SelectedChapter", selectedChapter);
        PlayerPrefs.SetString("SelectedDifficulty", selectedDifficulty);
        PlayerPrefs.Save();

        if (GameData.Instance == null)
        {
            GameObject go = new GameObject("GameData");
            go.AddComponent<GameData>();
        }
        GameData.Instance.allQuestions = new List<QuestionData>(allQuestions);
        GameData.Instance.startTime = Time.time;
        GameData.Instance.isTimerRunning = true;

        // Load next scene - user wants to go to rules screen before playing
        SceneManager.LoadScene("luatchoisinhhoc");
    }

}

