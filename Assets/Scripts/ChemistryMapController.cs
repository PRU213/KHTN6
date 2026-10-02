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
        string id = "CH_CHM_0" + chapter;

        Debug.Log("Đang mở bài: " + id);

        theoryPanel.Show(id);
    }

    void CloseTheory()
    {
        if (theoryPanel != null)
        {
            theoryPanel.gameObject.SetActive(false);
        }
    }
}