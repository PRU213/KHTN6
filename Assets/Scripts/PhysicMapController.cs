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
    public TMP_Text titleText;
    public TMP_Text theoryText;
    public TMP_Text explainText;

    [Header("Vào chơi")]
    public Button playButton;
    public GameSelectScreen gameSelectScreen;
    public string subjectName = "Vật lý";

    [Header("Link Apps Script (/exec)")]
    public string baseUrl = "DÁN_LINK_EXEC_VÀO_ĐÂY";

    private Dictionary<string, ChapterData> cache =
        new Dictionary<string, ChapterData>();

    void Start()
    {
        // Gắn sự kiện cho các nút chặng
        for (int i = 0; i < chapterButtons.Length; i++)
        {
            int chapter = i + 1;

            if (chapterButtons[i] != null)
            {
                chapterButtons[i].onClick.AddListener(
                    () => OpenChapter(chapter)
                );
            }
        }

        // Nút Back trong Theory
        if (theoryBackButton != null)
        {
            theoryBackButton.onClick.AddListener(BackToMap);
        }

        // Nút Vào chơi
        if (playButton != null)
        {
            playButton.onClick.AddListener(OpenGameSelect);
        }

        /*
         * QUAN TRỌNG:
         * Không SetActive mapPanel ở đây.
         *
         * Trạng thái màn hình ban đầu sẽ được cấu hình
         * trực tiếp trong Hierarchy/Inspector.
         */
    }

    public void OpenMap()
    {
        if (mapPanel != null)
            mapPanel.SetActive(true);

        if (theoryPanel != null)
            theoryPanel.SetActive(false);
    }

    void OpenChapter(int chapter)
    {
        if (mapPanel != null)
            mapPanel.SetActive(false);

        if (theoryPanel != null)
            theoryPanel.SetActive(true);

        string id = "CH_PHY_" + chapter.ToString("00");

        if (cache.ContainsKey(id))
        {
            ShowChapter(cache[id]);
            return;
        }

        if (titleText != null)
            titleText.text = "Đang tải...";

        if (theoryText != null)
            theoryText.text = "";

        if (explainText != null)
            explainText.text = "";

        StartCoroutine(LoadChapter(id));
    }

    void OpenGameSelect()
    {
        GameSession.Subject = subjectName;

        if (gameSelectScreen != null)
        {
            gameSelectScreen.Open(theoryPanel);
        }
    }

    void BackToMap()
    {
        if (theoryPanel != null)
            theoryPanel.SetActive(false);

        if (mapPanel != null)
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
                if (titleText != null)
                    titleText.text = "Lỗi kết nối";

                if (theoryText != null)
                    theoryText.text = req.error;

                if (explainText != null)
                    explainText.text = "";

                yield break;
            }

            ChapterData data =
                JsonUtility.FromJson<ChapterData>(
                    req.downloadHandler.text
                );

            if (data == null || !data.success)
            {
                if (titleText != null)
                    titleText.text = "Không có dữ liệu";

                if (theoryText != null)
                    theoryText.text = req.downloadHandler.text;

                if (explainText != null)
                    explainText.text = "";

                yield break;
            }

            cache[chapterId] = data;

            ShowChapter(data);
        }
    }

    void ShowChapter(ChapterData d)
    {
        if (titleText != null)
            titleText.text = d.chapterTitle;

        if (theoryText != null)
            theoryText.text = Clean(d.keyPoints);

        if (explainText != null)
            explainText.text = Clean(d.formulaNote);
    }

    string Clean(string s)
    {
        if (string.IsNullOrEmpty(s))
            return "";

        return s.Replace("\\n", "\n").Trim();
    }
}