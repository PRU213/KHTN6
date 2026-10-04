using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class QuizQuestionLoader : MonoBehaviour
{
    [Header("Google Apps Script /exec")]
    public string baseUrl;

    [Header("Câu hỏi đã load")]
    public List<QuizQuestion> questions = new List<QuizQuestion>();

    public bool IsLoaded { get; private set; }

    public IEnumerator LoadQuestions()
    {
        IsLoaded = false;

        questions.Clear();

        // Ví dụ:
        // Dễ,Trung bình
        string difficultyString =
            string.Join(",", GameSession.Difficulties);

        string url =
            baseUrl
            + "?action=questions"
            + "&subject="
            + UnityWebRequest.EscapeURL(GameSession.Subject)
            + "&difficulty="
            + UnityWebRequest.EscapeURL(difficultyString);

        Debug.Log("Question URL: " + url);

        using (UnityWebRequest request =
               UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result !=
                UnityWebRequest.Result.Success)
            {
                Debug.LogError(
                    "Lỗi load câu hỏi: "
                    + request.error
                );

                yield break;
            }

            Debug.Log(
                "JSON nhận được: "
                + request.downloadHandler.text
            );

            QuizQuestionResponse response =
                JsonUtility.FromJson<QuizQuestionResponse>(
                    request.downloadHandler.text
                );

            if (response == null)
            {
                Debug.LogError(
                    "Không parse được JSON."
                );

                yield break;
            }

            if (!response.success)
            {
                Debug.LogError(
                    "API success = false."
                );

                yield break;
            }

            if (response.questions == null)
            {
                Debug.LogError(
                    "questions = null."
                );

                yield break;
            }

            questions =
                new List<QuizQuestion>(
                    response.questions
                );

            ShuffleQuestions();

            IsLoaded = true;

            Debug.Log(
                "Đã load "
                + questions.Count
                + " câu hỏi."
            );
        }
    }

    private void ShuffleQuestions()
    {
        for (
            int i = questions.Count - 1;
            i > 0;
            i--
        )
        {
            int randomIndex =
                Random.Range(0, i + 1);

            QuizQuestion temp =
                questions[i];

            questions[i] =
                questions[randomIndex];

            questions[randomIndex] =
                temp;
        }
    }
}