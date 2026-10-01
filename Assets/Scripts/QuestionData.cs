using System;

[Serializable]
public class QuestionData
{
    public string id;
    public string monId;
    public string topic;
    public string difficulty;
    public string question;
    public string optionA;
    public string optionB;
    public string optionC;
    public string optionD;
    public string correct;      // "A" / "B" / "C" / "D"
    public string explanation;
    public int points;          // điểm của câu hỏi (cột points trong sheet)
}

[Serializable]
public class QuestionList
{
    public bool success;
    public QuestionData[] questions;
}