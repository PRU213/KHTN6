using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public static class HistoryAPI
{
    public static IEnumerator SaveHistory(
        string baseUrl,
        string username,
        string opponent,
        string mode,
        string result,
        int score
    )
    {
        WWWForm form = new WWWForm();

        form.AddField("action", "saveHistory");
        form.AddField("username", username);
        form.AddField("opponent", opponent);
        form.AddField("mode", mode);
        form.AddField("result", result);
        form.AddField("score", score);

        using (
            UnityWebRequest request =
                UnityWebRequest.Post(
                    baseUrl,
                    form
                )
        )
        {
            yield return request.SendWebRequest();

            if (
                request.result ==
                UnityWebRequest.Result.Success
            )
            {
                Debug.Log(
                    "SAVE HISTORY SUCCESS: "
                    + request.downloadHandler.text
                );
            }
            else
            {
                Debug.LogError(
                    "SAVE HISTORY ERROR: "
                    + request.error
                );
            }
        }
    }
}