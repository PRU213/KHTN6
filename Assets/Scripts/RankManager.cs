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
    public string message;
    public string rankVersion, weekStartsAt, weekEndsAt, weekStartLabel, weekEndLabel;
    public PlayerRank[] players;
}

public class RankManager : MonoBehaviour
{
    [Header("Tuần xếp hạng (tự tạo nếu chưa gắn)")]
    [SerializeField] private TMP_Text periodText;
    float refreshAt;

    [Header("Bảng xếp hạng (thứ tự từ hàng 1 -> hàng 9)")]
    [SerializeField] private TMP_Text[] nameTexts;
    [SerializeField] private TMP_Text[] scoreTexts;

    [Header("Bục top 3 (thứ tự: hạng 1, hạng 2, hạng 3)")]
    [SerializeField] private TMP_Text[] topNameTexts;

    private void OnEnable()
    {
        EnsurePeriodLabel();
        PlanetHistorySync.Changed += LoadRank;
        LoadRank();
    }
    private void OnDisable() { PlanetHistorySync.Changed -= LoadRank; StopAllCoroutines(); }
    private void Update() { if (Time.realtimeSinceStartup >= refreshAt) LoadRank(); }

    private void EnsurePeriodLabel()
    {
        if (periodText != null || nameTexts == null || nameTexts.Length == 0 || nameTexts[0] == null) return;
        var canvas = nameTexts[0].GetComponentInParent<Canvas>();
        if (canvas == null) return;
        var go = new GameObject("Weekly rank period", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.layer = canvas.gameObject.layer; go.transform.SetParent(canvas.transform, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(.535f, .005f); rect.anchorMax = new Vector2(.97f, .055f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        periodText = go.GetComponent<TextMeshProUGUI>(); periodText.font = nameTexts[0].font;
        periodText.fontSize = 25; periodText.enableAutoSizing = true; periodText.fontSizeMin = 16;
        periodText.color = Color.white; periodText.alignment = TextAlignmentOptions.Center; periodText.raycastTarget = false;
        go.AddComponent<UnityEngine.UI.Outline>().effectColor = new Color(.15f, .08f, .02f);
    }

    // Gắn hàm này vào nút "Làm mới" (OnClick) nếu cần
    public void LoadRank()
    {
        if (!isActiveAndEnabled) return;
        refreshAt = Time.realtimeSinceStartup + 60;
        StopAllCoroutines();
        if (periodText != null) periodText.text = "Đang tải bảng xếp hạng tuần…";
        StartCoroutine(GetRankData());
    }

    private IEnumerator GetRankData()
    {
        string currentApi = AppsScriptEndpoint.Url;

        string url = currentApi + "?action=getProgressRank";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.timeout = 35;
            request.certificateHandler = new BypassCertificate();
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Lỗi tải bảng xếp hạng: " + request.error);
                ClearRank();
                if (periodText != null) periodText.text = "Không tải được xếp hạng. Bấm Làm mới để thử lại.";
                yield break;
            }

            string json = request.downloadHandler.text;
            if (AppsScriptEndpoint.TryReadFailure(json, out var apiError)) {
                ClearRank();
                if (periodText != null) periodText.text = apiError;
                Debug.LogError("Rank API: " + apiError);
                yield break;
            }
            RankResponse response = null;
            try { response = JsonUtility.FromJson<RankResponse>(json); }
            catch (Exception) { Debug.LogError("API xếp hạng không trả JSON hợp lệ."); }

            if (response == null || !response.success || response.players == null)
            {
                Debug.LogWarning("Dữ liệu bảng xếp hạng không hợp lệ.");
                ClearRank();
                if (periodText != null) periodText.text = "API xếp hạng trả dữ liệu không hợp lệ.";
                yield break;
            }

            if (response.rankVersion != "weekly-v2") {
                ClearRank();
                if (periodText != null) periodText.text = "Cần cập nhật API xếp hạng tuần";
                Debug.LogWarning("Apps Script chưa dùng bảng xếp hạng tổng điểm tuần (weekly-v2).");
                yield break;
            }
            if (periodText != null) periodText.text = "Tuần " + response.weekStartLabel + " – " + response.weekEndLabel + " • Tổng điểm";
            if (DateTimeOffset.TryParse(response.weekEndsAt, out var weekEnd))
                refreshAt = Time.realtimeSinceStartup + Mathf.Clamp((float)(weekEnd - DateTimeOffset.UtcNow).TotalSeconds, 1, 60);

            DisplayRank(response.players);
            if (response.players.Length == 0 && periodText != null)
                periodText.text += " • Chưa có lượt hoàn thành trong tuần";
        }
    }

    private void DisplayRank(PlayerRank[] players)
    {
        ClearRank();

        // Sắp xếp điểm giảm dần (hàng 1 = điểm cao nhất)
        Array.Sort(players, (a, b) => { int byScore = b.score.CompareTo(a.score); return byScore != 0 ? byScore : string.CompareOrdinal(a.userID, b.userID); });

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
