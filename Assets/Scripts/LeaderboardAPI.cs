using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System;

[Serializable]
public class LeaderboardEntry
{
    public string username;
    public string fullname;
    public string subject;
    public string chapter;
    public string difficulty;
    public string time;
}

[Serializable]
public class LeaderboardData
{
    public List<LeaderboardEntry> records;
}

public class LeaderboardAPI : MonoBehaviour
{
    public static LeaderboardAPI Instance { get; private set; }
    
    // YÊU CẦU: Dán link Web App URL của Google Apps Script vào đây (sau khi Deploy)
    public string webAppUrl = "https://script.google.com/macros/s/AKfycbxxE2R2ZoitgM647aQqnebUcG90lhIlodU0DcyiaZuKkLaVWl6oopI-TkeNM8_KKDWhUw/exec";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("LeaderboardAPI");
            Instance = go.AddComponent<LeaderboardAPI>();
            DontDestroyOnLoad(go);
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SaveScore(string username, string fullname, string subject, string chapter, string difficulty, string time, Action<bool> onComplete)
    {
        if (string.IsNullOrEmpty(webAppUrl) || webAppUrl == "YOUR_WEB_APP_URL_HERE")
        {
            Debug.LogWarning("Chưa cài đặt Web App URL cho LeaderboardAPI!");
            onComplete?.Invoke(false);
            return;
        }
        StartCoroutine(PostScore(username, fullname, subject, chapter, difficulty, time, onComplete));
    }

    IEnumerator PostScore(string username, string fullname, string subject, string chapter, string difficulty, string time, Action<bool> onComplete)
    {
        WWWForm form = new WWWForm();
        form.AddField("action", "saveLeaderboard");
        form.AddField("Username", username);
        form.AddField("Fullname", fullname);
        form.AddField("Subject", subject);
        form.AddField("Chapter", chapter);
        form.AddField("Difficulty", difficulty);
        form.AddField("Time", time);

        using (UnityWebRequest www = UnityWebRequest.Post(webAppUrl, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error saving score: " + www.error);
                onComplete?.Invoke(false);
            }
            else
            {
                Debug.Log("Score saved successfully!");
                onComplete?.Invoke(true);
            }
        }
    }

    public void GetScores(Action<List<LeaderboardEntry>> onComplete)
    {
        if (string.IsNullOrEmpty(webAppUrl) || webAppUrl == "YOUR_WEB_APP_URL_HERE")
        {
            onComplete?.Invoke(new List<LeaderboardEntry>());
            return;
        }
        StartCoroutine(FetchScores(onComplete));
    }

    IEnumerator FetchScores(Action<List<LeaderboardEntry>> onComplete)
    {
        string url = webAppUrl + "?action=getLeaderboard";
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error fetching scores: " + www.error);
                onComplete?.Invoke(null);
            }
            else
            {
                string json = www.downloadHandler.text;
                try
                {
                    LeaderboardData data = JsonUtility.FromJson<LeaderboardData>("{\"records\":" + json + "}");
                    onComplete?.Invoke(data.records);
                }
                catch (Exception e)
                {
                    Debug.LogError("JSON Parse Error: " + e.Message);
                    onComplete?.Invoke(null);
                }
            }
        }
    }
}
