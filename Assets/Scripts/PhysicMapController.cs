using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

public class PhysicMapController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mapPanel;       // Physical_map_game
    public GameObject theoryPanel;    // theory_physic

    [Header("Nút chặng, theo thứ tự chap1..chap5")]
    public Button[] chapterButtons;

    [Header("Nút quay lại trong theory_physic")]
    public Button theoryBackButton;

    [Header("Text hiển thị")]
    public TMP_Text titleText;        // object 'title'   <- cột D (chapterTitle)
    public TMP_Text theoryText;       // object 'theory'  <- cột F (keyPoints)
    public TMP_Text explainText;      // chữ trong object 'explain' <- cột G (formulaNote)

    [Header("Link Apps Script (/exec)")]
    public string baseUrl = "DÁN_LINK_EXEC_VÀO_ĐÂY";

    private Dictionary<string, ChapterData> cache = new Dictionary<string, ChapterData>();

    void Start()
    {
        for (int i = 0; i < chapterButtons.Length; i++)
        {
            int chapter = i + 1;
            chapterButtons[i].onClick.AddListener(() => OpenChapter(chapter));
        }
        theoryBackButton.onClick.AddListener(BackToMap);

        mapPanel.SetActive(true);
        theoryPanel.SetActive(false);
    }

    void OpenChapter(int chapter)
    {
        mapPanel.SetActive(false);
        theoryPanel.SetActive(true);

        string id = "CH_PHY_" + chapter.ToString("00");

        if (cache.ContainsKey(id))
        {
            ShowChapter(cache[id]);
            return;
        }

        titleText.text = "Đang tải...";
        theoryText.text = "";
        if (explainText) explainText.text = "";
        StartCoroutine(LoadChapter(id));
    }

    void BackToMap()
    {
        theoryPanel.SetActive(false);
        mapPanel.SetActive(true);
    }

    IEnumerator LoadChapter(string chapterId)
    {
        string url = baseUrl + "?chapterId=" + chapterId;
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                titleText.text = "Lỗi kết nối";
                theoryText.text = req.error;
                if (explainText) explainText.text = "";
                yield break;
            }

            ChapterData data = JsonUtility.FromJson<ChapterData>(req.downloadHandler.text);
            if (data == null || !data.success)
            {
                titleText.text = "Không có dữ liệu";
                theoryText.text = req.downloadHandler.text;
                if (explainText) explainText.text = "";
                yield break;
            }

            cache[chapterId] = data;
            ShowChapter(data);
        }
    }

    void ShowChapter(ChapterData d)
    {
        titleText.text = d.chapterTitle;                 // cột D -> title
        theoryText.text = Clean(d.keyPoints);            // cột F -> theory
        if (explainText) explainText.text = Clean(d.formulaNote);   // cột G -> explain
    }

    string Clean(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\\n", "\n").Trim();
    }
}