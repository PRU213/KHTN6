using System;

[Serializable]
public class QuizQuestionResponse
{
    public bool success;
    public int count;

    public string subject;

    public string[] difficulties;

    public QuizQuestion[] questions;
}