using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public static class QuestionSetLoader
{
    // Cách dùng: StartCoroutine(QuestionSetLoader.Load(...)) trong script của game
    public static IEnumerator Load(
        string baseUrl,
        string defaultSubject,     // dùng khi test riêng game (GameSession trống)
        string defaultChapter,
        int easyCount, int mediumCount, int hardCount, int targetPoints,
        Action<List<QuestionData>, string> done)   // done(danhSachCau, thongBaoLoi)
    {
        string mon = string.IsNullOrEmpty(GameSession.Subject) ? defaultSubject : GameSession.Subject;
        string ch = string.IsNullOrEmpty(GameSession.Chapter) ? defaultChapter : GameSession.Chapter;

        string url = baseUrl + "?action=questions&mon=" + UnityWebRequest.EscapeURL(mon);
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                done?.Invoke(null, "Lỗi kết nối: " + req.error);
                yield break;
            }

            QuestionList list = JsonUtility.FromJson<QuestionList>(req.downloadHandler.text);
            if (list == null || !list.success || list.questions == null || list.questions.Length == 0)
            {
                Debug.Log(req.downloadHandler.text);
                done?.Invoke(null, "Không có câu hỏi");
                yield break;
            }

            var set = BuildSet(list.questions, ch, easyCount, mediumCount, hardCount, targetPoints);
            if (set.Count == 0)
            {
                done?.Invoke(null, "Không có câu hỏi cho chương này");
                yield break;
            }

            Debug.Log("Subject=" + mon + " | Chapter=" + ch + " | " + set.Count + " câu");
            done?.Invoke(set, null);
        }
    }

    public static int GetPoints(QuestionData q)
    {
        return q.points > 0 ? q.points : 10;
    }

    static List<QuestionData> BuildSet(QuestionData[] all, string ch,
        int easyCount, int mediumCount, int hardCount, int targetPoints)
    {
        var pool = new List<QuestionData>();
        foreach (var q in all)
        {
            if (string.IsNullOrEmpty(ch) ||
                (q.topic != null && q.topic.Trim().StartsWith(ch.Trim(), StringComparison.OrdinalIgnoreCase)))
                pool.Add(q);
        }

        var easy = new List<QuestionData>();
        var medium = new List<QuestionData>();
        var hard = new List<QuestionData>();
        foreach (var q in pool)
        {
            string d = (q.difficulty ?? "").Trim().ToLower();
            if (d == "dễ") easy.Add(q);
            else if (d == "trung bình") medium.Add(q);
            else if (d == "khó") hard.Add(q);
        }

        Shuffle(easy); Shuffle(medium); Shuffle(hard);

        var result = new List<QuestionData>();
        Take(easy, easyCount, result);
        Take(medium, mediumCount, result);
        Take(hard, hardCount, result);

        int total = 0;
        foreach (var q in result) total += GetPoints(q);

        if (total < targetPoints)
        {
            var rest = new List<QuestionData>();
            foreach (var q in pool) if (!result.Contains(q)) rest.Add(q);
            Shuffle(rest);
            foreach (var q in rest)
            {
                int p = GetPoints(q);
                if (total + p <= targetPoints)
                {
                    result.Add(q);
                    total += p;
                    if (total == targetPoints) break;
                }
            }
        }

        Shuffle(result);
        return result;
    }

    static void Take(List<QuestionData> source, int n, List<QuestionData> dest)
    {
        for (int i = 0; i < n && i < source.Count; i++) dest.Add(source[i]);
    }

    static void Shuffle(List<QuestionData> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            var tmp = list[i]; list[i] = list[j]; list[j] = tmp;
        }
    }
}