using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;

public class XepHangManager : MonoBehaviour
{
    [Header("UI Elements")]
    public Dropdown chuongDropdown;
    public Transform listContainer; // BangXepHang

    public Button btnDe;
    public Button btnTrungBinh;
    public Button btnKho;
    public Button btnTongHop;

    [Header("Item Styling")]
    public Font itemFont;
    
    public string sheetCSVUrl = "https://docs.google.com/spreadsheets/d/1DcJwlN_fUDdcaQ6IfnlkMJApgWuEUqMhyZv9Kn8vfWo/export?format=csv&gid=1463734910";
    
    private List<string> chapters = new List<string>();
    private string selectedChapter = "Tất cả";
    private string selectedDifficulty = "Tổng hợp";

    private Color normalColor = Color.white;
    private Color selectedColor = new Color(0.5f, 1f, 0.5f); // Light green

    private Dictionary<string, Button> diffButtons = new Dictionary<string, Button>();
    private List<LeaderboardEntry> allScores = new List<LeaderboardEntry>();

    void Start()
    {
        AutoFindUIElements();

        if (itemFont == null)
            itemFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

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

        if (chuongDropdown != null)
        {
            chuongDropdown.onValueChanged.AddListener(OnChapterSelected);
        }

        Transform quayLai = FindChildRecursive(transform, "QuayLai");
        if (quayLai != null)
        {
            Button btn = quayLai.GetComponent<Button>();
            if (btn == null) btn = quayLai.gameObject.AddComponent<Button>();
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => UnityEngine.SceneManagement.SceneManager.LoadScene("HomePage"));
        }

        selectedChapter = PlayerPrefs.GetString("SelectedChapter", "Tất cả");
        selectedDifficulty = PlayerPrefs.GetString("SelectedDifficulty", "Tổng hợp");
        
        SelectDifficulty(selectedDifficulty);

        StartCoroutine(LoadChaptersFromSheet());

        if (LeaderboardAPI.Instance != null)
        {
            LeaderboardAPI.Instance.GetScores(OnScoresLoaded);
        }
    }

    private void AutoFindUIElements()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        Transform[] allTransforms = canvas.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in allTransforms)
        {
            if (t.name == "List") 
            {
                ScrollRect sr = t.GetComponent<ScrollRect>();
                if (sr != null && sr.content != null)
                    listContainer = sr.content;
                else
                    listContainer = t;
            }
            else if (t.name == "De") btnDe = t.GetComponent<Button>() ?? t.gameObject.AddComponent<Button>();
            else if (t.name == "TrungBinh") btnTrungBinh = t.GetComponent<Button>() ?? t.gameObject.AddComponent<Button>();
            else if (t.name == "Kho") btnKho = t.GetComponent<Button>() ?? t.gameObject.AddComponent<Button>();
            else if (t.name == "TongHop") btnTongHop = t.GetComponent<Button>() ?? t.gameObject.AddComponent<Button>();
            else if (t.name == "ChapterDropdown") chuongDropdown = t.GetComponent<Dropdown>();
        }

        if (listContainer == null)
        {
            GameObject containerObj = new GameObject("List", typeof(RectTransform));
            containerObj.transform.SetParent(canvas.transform, false);
            listContainer = containerObj.transform;

            RectTransform rt = containerObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.05f, 0.05f); 
            rt.anchorMax = new Vector2(0.95f, 0.65f); 
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        VerticalLayoutGroup vlg = listContainer.GetComponent<VerticalLayoutGroup>();
        if (vlg == null)
        {
            vlg = listContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.spacing = 15;
            vlg.padding = new RectOffset(30, 30, 30, 30);
        }
    }

    private void SelectDifficulty(string diff)
    {
        selectedDifficulty = diff;
        foreach (var kvp in diffButtons)
        {
            if (kvp.Value != null)
            {
                Image img = kvp.Value.GetComponent<Image>();
                if (img != null)
                {
                    img.color = (kvp.Key == diff) ? selectedColor : normalColor;
                }
            }
        }
        UpdateLeaderboardUI();
    }

    private void OnChapterSelected(int index)
    {
        if (chuongDropdown != null && index >= 0 && index < chuongDropdown.options.Count)
        {
            selectedChapter = chuongDropdown.options[index].text;
            UpdateLeaderboardUI();
        }
    }

    private void OnScoresLoaded(List<LeaderboardEntry> scores)
    {
        if (scores == null) return;
        allScores = scores;
        UpdateLeaderboardUI();
    }

    private void UpdateLeaderboardUI()
    {
        if (listContainer == null) return;

        foreach (Transform child in listContainer)
        {
            Destroy(child.gameObject);
        }

        var filtered = allScores.Where(s => 
            (selectedChapter == "Tất cả" || s.chapter == selectedChapter) &&
            (selectedDifficulty == "Tổng hợp" || s.difficulty == selectedDifficulty)
        ).ToList();

        filtered.Sort((a, b) => string.Compare(a.time, b.time));

        CreateRowObject("STT", "Username", "Họ tên", "Thời gian", true);

        // Hiển thị toàn bộ danh sách (hệ thống ScrollView sẽ lo việc cuộn)
        for (int i = 0; i < filtered.Count; i++)
        {
            var s = filtered[i];
            CreateRowObject((i+1).ToString(), s.username, s.fullname, s.time, false);
        }
    }

    private void CreateRowObject(string stt, string user, string full, string time, bool isHeader)
    {
        GameObject row = new GameObject("Row_" + stt, typeof(RectTransform));
        row.transform.SetParent(listContainer, false);
        
        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.spacing = 10;

        RectTransform rt = row.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 50);

        CreateTextObj(row.transform, stt, 0.1f, isHeader);
        CreateTextObj(row.transform, user, 0.3f, isHeader);
        CreateTextObj(row.transform, full, 0.4f, isHeader);
        CreateTextObj(row.transform, time, 0.2f, isHeader);
    }

    private void CreateTextObj(Transform parent, string content, float flex, bool isHeader)
    {
        GameObject txtObj = new GameObject("Txt", typeof(RectTransform));
        txtObj.transform.SetParent(parent, false);

        LayoutElement le = txtObj.AddComponent<LayoutElement>();
        le.flexibleWidth = flex;
        le.preferredWidth = 0; // Bắt buộc tuân theo tỷ lệ flex, không bị text dài đẩy lệch

        Text txt = txtObj.AddComponent<Text>();
        txt.font = itemFont;
        txt.fontSize = isHeader ? 32 : 28;
        // CHO TẤT CẢ CHỮ ĐỀU IN ĐẬM CHO DỄ NHÌN
        txt.fontStyle = FontStyle.Bold;
        txt.color = isHeader ? new Color(0.8f, 0.4f, 0f) : Color.black; 
        
        // Căn lề chuẩn: Cột Thời gian căn giữa, các cột khác căn trái
        if (content.Contains(":") && content.Length > 8) 
        {
            // Xử lý lỗi Google Sheet tự đổi "04:35" thành "Sat Dec 30 1899 04:35:00..."
            System.Text.RegularExpressions.Match m = System.Text.RegularExpressions.Regex.Match(content, @"\d{2}:\d{2}");
            if (m.Success) content = m.Value;
        }

        txt.alignment = (flex == 0.2f || flex == 0.1f) ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
        txt.text = content;
        
        Shadow shadow = txtObj.AddComponent<Shadow>();
        shadow.effectColor = new Color(1, 1, 1, 0.8f); // Bóng trắng đậm hơn một chút
    }

    IEnumerator LoadChaptersFromSheet()
    {
        using (UnityWebRequest req = UnityWebRequest.Get(sheetCSVUrl))
        {
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success)
            {
                ParseCSV(req.downloadHandler.text);
                PopulateDropdown();
            }
        }
    }

    private void ParseCSV(string csvData)
    {
        chapters.Clear();
        string[] rows = csvData.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        bool isFirstRow = true;
        foreach (string row in rows)
        {
            if (isFirstRow) { isFirstRow = false; continue; }
            string[] cols = ParseCSVRow(row);
            if (cols.Length >= 12)
            {
                if (cols[1].Trim().Contains("Sinh"))
                {
                    string chapterName = cols[2].Trim();
                    if (!string.IsNullOrEmpty(chapterName) && !chapters.Contains(chapterName))
                        chapters.Add(chapterName);
                }
            }
        }
    }

    private string[] ParseCSVRow(string row)
    {
        List<string> currentCols = new List<string>();
        bool inQuotes = false;
        string currentField = "";
        for (int i = 0; i < row.Length; i++)
        {
            char c = row[i];
            if (c == '\"')
            {
                if (inQuotes && i + 1 < row.Length && row[i + 1] == '\"') { currentField += '\"'; i++; }
                else inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes) { currentCols.Add(currentField); currentField = ""; }
            else currentField += c;
        }
        currentCols.Add(currentField);
        return currentCols.ToArray();
    }

    private void PopulateDropdown()
    {
        if (chuongDropdown == null) return;
        chuongDropdown.ClearOptions();
        List<string> options = new List<string> { "Tất cả" };
        options.AddRange(chapters);
        chuongDropdown.AddOptions(options);

        int idx = options.IndexOf(selectedChapter);
        if (idx >= 0) chuongDropdown.value = idx;
    }

    private Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name.Equals(name, StringComparison.OrdinalIgnoreCase)) return child;
            Transform found = FindChildRecursive(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
