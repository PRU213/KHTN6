using TMPro;
using UnityEngine;

public class HistoryRow : MonoBehaviour
{
    public TMP_Text opponentText;
    public TMP_Text modeText;
    public TMP_Text resultText;
    public TMP_Text scoreText;
    public TMP_Text timeText;
    TMP_Text chapterText;
    public static readonly float[] ColumnEdges = { 0, .17f, .33f, .56f, .69f, .78f, 1 };

    public static string Lesson(HistoryItem item)
    {
        if (!string.Equals((item.mode ?? "").Trim(), "Luyện tập", System.StringComparison.OrdinalIgnoreCase)) return "—";
        string subject = (item.subject ?? "").Trim(), chapter = (item.chapter ?? "").Trim();
        switch (subject.ToLowerInvariant()) {
            case "physics": case "vật lý": case "vật lí": subject = "Vật lí"; break;
            case "chemistry": subject = "Hóa học"; break;
            case "biology": subject = "Sinh học"; break;
        }
        var match = System.Text.RegularExpressions.Regex.Match(chapter, @"^(?:Chương\s*|CH_(?:PHY|CHM|BIO)_)(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (match.Success) chapter = "Chương " + int.Parse(match.Groups[1].Value);
        return string.IsNullOrEmpty(subject) && string.IsNullOrEmpty(chapter) ? "—"
            : string.IsNullOrEmpty(chapter) ? subject : string.IsNullOrEmpty(subject) ? chapter : chapter + " - " + subject;
    }

    static void Place(TMP_Text text, int column)
    {
        var rect = text.rectTransform;
        rect.anchorMin = new Vector2(ColumnEdges[column], 0); rect.anchorMax = new Vector2(ColumnEdges[column + 1], 1);
        rect.offsetMin = new Vector2(5, 2); rect.offsetMax = new Vector2(-5, -2);
        text.richText = false; text.raycastTarget = false;
        text.enableAutoSizing = true; text.fontSizeMin = 16; text.fontSizeMax = 24;
        text.alignment = TextAlignmentOptions.Center;
    }

    public void SetData(HistoryItem item)
    {
        if (chapterText == null) {
            chapterText = Instantiate(modeText, transform); chapterText.name = "ChapterText";
            for (int column = 1; column < ColumnEdges.Length - 1; column++) {
                var line = new GameObject("Column divider", typeof(RectTransform), typeof(UnityEngine.UI.Image)); line.transform.SetParent(transform, false);
                var rect = line.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(ColumnEdges[column], 0); rect.anchorMax = new Vector2(ColumnEdges[column], 1); rect.sizeDelta = new Vector2(1, 0); rect.anchoredPosition = Vector2.zero;
                line.GetComponent<UnityEngine.UI.Image>().color = new Color(.88f, .68f, .4f, .65f); line.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            }
            var bottom = new GameObject("Row divider", typeof(RectTransform), typeof(UnityEngine.UI.Image)); bottom.transform.SetParent(transform, false);
            var edge = bottom.GetComponent<RectTransform>(); edge.anchorMin = Vector2.zero; edge.anchorMax = new Vector2(1, 0); edge.sizeDelta = new Vector2(0, 1); edge.anchoredPosition = Vector2.zero;
            bottom.GetComponent<UnityEngine.UI.Image>().color = new Color(.88f, .68f, .4f, .65f); bottom.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
        }
        Place(opponentText, 0); Place(modeText, 1); Place(chapterText, 2); Place(resultText, 3); Place(scoreText, 4); Place(timeText, 5);
        chapterText.text = Lesson(item);
        opponentText.text = item.opponent;
        modeText.text = item.mode;
        resultText.text = item.result;
        scoreText.text = item.score.ToString();
        timeText.text = item.time;
    }
}
