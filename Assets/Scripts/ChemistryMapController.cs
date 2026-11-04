using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChemistryMapController : MonoBehaviour
{
    [Header("Các chặng Hóa học")]
    public Button[] chapterButtons;

    [Header("Panel lý thuyết")]
    public TheoryPanel theoryPanel;

    [Header("Nút quay lại")]
    public Button backTheoryButton;

    [Header("Môn (khớp cột monID trong sheet câu hỏi)")]
    public string subjectName = "Hóa học";

    void Awake()
    {
        FindTheoryPanel();
        AutoFindChapterButtons();
    }

    void Start()
    {
        FindTheoryPanel();
        AutoFindChapterButtons();

        if (chapterButtons != null)
        {
            for (int i = 0; i < chapterButtons.Length; i++)
            {
                int chapter = i + 1;
                if (chapterButtons[i] != null)
                {
                    chapterButtons[i].onClick.RemoveAllListeners();
                    chapterButtons[i].onClick.AddListener(() =>
                    {
                        OpenTheory(chapter);
                    });
                }
            }
        }

        if (backTheoryButton != null)
        {
            backTheoryButton.onClick.RemoveAllListeners();
            backTheoryButton.onClick.AddListener(CloseTheory);
        }
    }

    void FindTheoryPanel()
    {
        if (theoryPanel == null)
        {
            var go = GameObject.Find("theory_chemistry");
            if (go != null)
                theoryPanel = go.GetComponent<TheoryPanel>();

            if (theoryPanel == null)
                theoryPanel = FindFirstObjectByType<TheoryPanel>(FindObjectsInactive.Include);
        }
    }

    void AutoFindChapterButtons()
    {
        if (chapterButtons == null || chapterButtons.Length == 0)
        {
            var found = new List<Button>();
            for (int i = 1; i <= 10; i++)
            {
                var t = transform.Find("chap" + i);
                if (t == null) t = transform.Find("Chap" + i);
                if (t != null)
                {
                    var btn = t.GetComponent<Button>();
                    if (btn != null) found.Add(btn);
                }
            }
            if (found.Count > 0)
            {
                chapterButtons = found.ToArray();
            }
        }
    }

    void OpenTheory(int chapter)
    {
        string id = "CH_CHM_" + chapter.ToString("00");

        Debug.Log("[ChemistryMapController] Đang mở bài: " + id);

        // Báo cho game biết môn và chương người chơi vừa chọn
        GameSession.Subject = subjectName;
        GameSession.Chapter = "Chương " + chapter;

        FindTheoryPanel();

        System.Action next = null;
        if (chapterButtons != null && chapter < chapterButtons.Length)
            next = () => OpenTheory(chapter + 1);

        if (theoryPanel != null)
        {
            theoryPanel.Show(id, next);
        }
        else
        {
            Debug.LogError("[ChemistryMapController] Không tìm thấy theoryPanel (theory_chemistry)!");
        }
    }

    void CloseTheory()
    {
        if (theoryPanel != null)
        {
            theoryPanel.gameObject.SetActive(false);
        }
    }
}
