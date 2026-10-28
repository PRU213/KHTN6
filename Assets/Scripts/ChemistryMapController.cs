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

    void Start()
    {
        for (int i = 0; i < chapterButtons.Length; i++)
        {
            int chapter = i + 1;

            chapterButtons[i].onClick.AddListener(() =>
            {
                OpenTheory(chapter);
            });
        }

        if (backTheoryButton != null)
        {
            backTheoryButton.onClick.AddListener(CloseTheory);
        }
    }

    void OpenTheory(int chapter)
    {
        string id = "CH_CHM_" + chapter.ToString("00");

        Debug.Log("Đang mở bài: " + id);

        // Báo cho game biết môn và chương người chơi vừa chọn
        GameSession.Subject = subjectName;
        GameSession.Chapter = "Chương " + chapter;

        System.Action next = null;
        if(chapter < chapterButtons.Length) next = () => OpenTheory(chapter + 1);
        theoryPanel.Show(id, next);
    }

    void CloseTheory()
    {
        if (theoryPanel != null)
        {
            theoryPanel.gameObject.SetActive(false);
        }
    }
}
