using System;
using System.Collections;
using TMPro;                 // Nếu dùng Text thường: đổi thành "using UnityEngine.UI;" và TMP_Text -> Text
using UnityEngine;
using UnityEngine.Networking;

// ===== Kiểu dữ liệu (đã bỏ field rank) =====
// Nếu bạn đã khai báo 2 class này ở file khác thì XÓA đoạn này đi để tránh trùng.
[Serializable]
public class PlayerRank
{
    public string userID;
    public string fullName;   // lấy từ sheet Users (cột FullName)
    public int score;
}

[Serializable]
public class RankResponse
{
    public bool success;
    public PlayerRank[] players;
}

public class RankManager : MonoBehaviour
{
    // THAY bằng URL Apps Script của bạn (giữ nguyên giá trị cũ trong file của bạn)
    private const string DEFAULT_API_URL = "https://script.google.com/macros/s/AKfycbxxE2R2ZoitgM647aQqnebUcG90lhIlodU0DcyiaZuKkLaVWl6oopI-TkeNM8_KKDWhUw/exec";

    [Header("API")]
    [SerializeField] private string apiUrl;

    [Header("Bảng xếp hạng (thứ tự từ hàng 1 -> hàng 9)")]
    [SerializeField] private TMP_Text[] nameTexts;
    [SerializeField] private TMP_Text[] scoreTexts;

    [Header("Bục top 3 (thứ tự: hạng 1, hạng 2, hạng 3)")]
    [SerializeField] private TMP_Text[] topNameTexts;

    private void OnEnable()
    {
        LoadRank();
    }

    // Gắn hàm này vào nút "Làm mới" (OnClick) nếu cần
    public void LoadRank()
    {
        StartCoroutine(GetRankData());
    }

    private IEnumerator GetRankData()
    {
        string currentApi = string.IsNullOrWhiteSpace(apiUrl)
            ? DEFAULT_API_URL
            : apiUrl.Trim();

        string url = currentApi + "?action=getProgressRank";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Lỗi tải bảng xếp hạng: " + request.error);
                ClearRank();
                yield break;
            }

            string json = request.downloadHandler.text;
            RankResponse response = JsonUtility.FromJson<RankResponse>(json);

            if (response == null || !response.success || response.players == null)
            {
                Debug.LogWarning("Dữ liệu bảng xếp hạng không hợp lệ: " + json);
                ClearRank();
                yield break;
            }

            DisplayRank(response.players);
        }
    }

    private void DisplayRank(PlayerRank[] players)
    {
        ClearRank();

        // Sắp xếp điểm giảm dần (hàng 1 = điểm cao nhất)
        Array.Sort(players, (a, b) => b.score.CompareTo(a.score));

        int count = Mathf.Min(players.Length, nameTexts.Length, scoreTexts.Length);
        for (int i = 0; i < count; i++)
        {
            nameTexts[i].text = GetDisplayName(players[i]);
            scoreTexts[i].text = players[i].score.ToString();
        }

        // Tên top 3 trên bục
        for (int i = 0; i < topNameTexts.Length; i++)
        {
            if (topNameTexts[i] == null) continue;
            topNameTexts[i].text = i < players.Length ? GetDisplayName(players[i]) : "";
        }
    }

    // Ưu tiên tên thật, nếu API chưa trả về thì dùng tạm mã người chơi
    private string GetDisplayName(PlayerRank p)
    {
        return string.IsNullOrWhiteSpace(p.fullName) ? p.userID : p.fullName;
    }

    private void ClearRank()
    {
        for (int i = 0; i < nameTexts.Length; i++)
        {
            if (nameTexts[i] != null) nameTexts[i].text = "";
        }

        for (int i = 0; i < scoreTexts.Length; i++)
        {
            if (scoreTexts[i] != null) scoreTexts[i].text = "";
        }

        for (int i = 0; i < topNameTexts.Length; i++)
        {
            if (topNameTexts[i] != null) topNameTexts[i].text = "";
        }
    }
}