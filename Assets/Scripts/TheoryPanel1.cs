using System.Collections;
using UnityEngine;
using TMPro;

public class TheoryPanel : MonoBehaviour
{
    public TMP_Text titleText;
    public TMP_Text theoryText;
    public TMP_Text explainText;

    public void Show(string id)
    {
        gameObject.SetActive(true);

        StartCoroutine(LoadTheory(id));
    }

    IEnumerator LoadTheory(string id)
    {
        titleText.text = "Đang tải...";
        theoryText.text = "";
        explainText.text = "";

        if (SheetLoader1.Instance == null)
        {
            Debug.LogError("Không tìm thấy SheetLoader1!");
            yield break;
        }

        TheoryItem result = null;

        yield return StartCoroutine(
            SheetLoader1.Instance.GetTheory(
                id,
                item => result = item
            )
        );

        if (result == null)
        {
            titleText.text = "Không tìm thấy bài";
            theoryText.text = "ID: " + id;
            yield break;
        }

        titleText.text = result.chapterTitle;
        theoryText.text = result.keyPoints;
        explainText.text = result.formulaNote;

        Debug.Log(
            "Đã tải bài: " + result.chapterTitle
        );
    }
}