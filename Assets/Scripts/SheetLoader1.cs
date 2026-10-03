using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class TheoryItem
{
    public bool success;
    public string chapterId;
    public string monId;
    public string chapterOrder;
    public string chapterTitle;
    public string summary;
    public string keyPoints;
    public string formulaNote;
    public string iconImage;

    public string message;
    public string searchedId;
    public string sheetName;
    public int totalRows;
}

public class SheetLoader1 : MonoBehaviour
{
    static SheetLoader1 _instance;

    public static SheetLoader1 Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindAnyObjectByType<SheetLoader1>(FindObjectsInactive.Include);
            return _instance;
        }
    }

    public string url =
        "https://script.google.com/macros/s/AKfycbxxE2R2ZoitgM647aQqnebUcG90lhIlodU0DcyiaZuKkLaVWl6oopI-TkeNM8_KKDWhUw/exec";

    void Awake()
    {
        _instance = this;
    }

    public IEnumerator GetTheory(string chapterId, Action<TheoryItem> callback)
    {
        string requestUrl =
            url + "?chapterId=" + UnityWebRequest.EscapeURL(chapterId);

        Debug.Log("Request URL: " + requestUrl);

        using (UnityWebRequest req = UnityWebRequest.Get(requestUrl))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Lỗi tải Sheet: " + req.error);
                callback?.Invoke(null);
                yield break;
            }

            string json = req.downloadHandler.text;

            Debug.Log("JSON: " + json);

            TheoryItem item =
                JsonUtility.FromJson<TheoryItem>(json);

            if (item == null || !item.success)
            {
                Debug.LogWarning(
                    "Không tìm thấy bài: " + chapterId
                );

                callback?.Invoke(null);
                yield break;
            }

            callback?.Invoke(item);
        }
    }
}