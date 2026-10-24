using System;

[Serializable]
public class QuizQuestion
{
    public string id;

    public string monID;

    public string chapter;

    public string difficulty;

    public string question;

    public string option_a;
    public string option_b;
    public string option_c;
    public string option_d;

    public string correct_option;

    public string explanation;

    public int points;
}