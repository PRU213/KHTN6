using System;

[Serializable]
public class HistoryItem
{
    public string opponent;
    public string mode;
    public string result;
    public int score;
    public string time;
    public string subject;
    public string chapter;
}

[Serializable]
public class HistoryResponse
{
    public bool success;

    public string username;
    public string fullName;
    public string message;

    public int total;
    public int win;
    public int lose;
    public int winRate;

    public HistoryItem[] histories;
}
