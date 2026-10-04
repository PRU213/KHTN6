using System.Collections;
using UnityEngine;

public class QuestionTest : MonoBehaviour
{
    public QuizQuestionLoader loader;


    void OnEnable()
    {
        StartCoroutine(TestLoad());
    }


    IEnumerator TestLoad()
    {
        yield return
            StartCoroutine(
                loader.LoadQuestions()
            );


        Debug.Log(
            "Tổng số câu: "
            + loader.questions.Count
        );


        foreach (
            QuizQuestion q
            in loader.questions
        )
        {
            Debug.Log(
                q.id
                + " | "
                + q.difficulty
                + " | "
                + q.question
            );
        }
    }
}