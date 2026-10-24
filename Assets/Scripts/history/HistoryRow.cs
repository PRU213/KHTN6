using TMPro;
using UnityEngine;

public class HistoryRow : MonoBehaviour
{
    public TMP_Text opponentText;
    public TMP_Text modeText;
    public TMP_Text resultText;
    public TMP_Text scoreText;
    public TMP_Text timeText;

    public void SetData(HistoryItem item)
    {
        opponentText.text = item.opponent;
        modeText.text = item.mode;
        resultText.text = item.result;
        scoreText.text = item.score.ToString();
        timeText.text = item.time;
    }
}