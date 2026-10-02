using System;

[System.Serializable]
public class QuestionData
{
    public string id;
    public string monID;
    public string topic;
    public string difficulty;
    public string question;
    public string option_a;
    public string option_b;
    public string option_c;
    public string option_d;
    public string correct_option; // "a", "b", "c", hoac "d"
    public string explanation;
    public int points;
}
