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
        PlanetHistorySync.Enqueue(new PlanetMatchRecord {
            matchId = "practice-" + System.Guid.NewGuid().ToString("N"), username = username,
            opponent = opponent, mode = mode, result = result, score = score, maxScore = 0,
            subject = GameSession.Subject, chapter = GameSession.Chapter,
            questionType = "multipleChoice"
        });
        yield break;
    }
}

public static class AppsScriptEndpoint
{
    [System.Serializable] class Configuration { public string url; }
    [System.Serializable] class Failure { public bool success; public string message; }
    public static bool TryReadFailure(string body, out string message)
    {
        message = null;
        string text = (body ?? "").TrimStart();
        if (!text.StartsWith("{")) {
            message = text.IndexOf("accounts.google.com", System.StringComparison.OrdinalIgnoreCase) >= 0
                || text.IndexOf("signin", System.StringComparison.OrdinalIgnoreCase) >= 0
                ? "Google yêu cầu đăng nhập. Cần kiểm tra quyền truy cập bản triển khai Apps Script."
                : "API không trả dữ liệu JSON. Cần kiểm tra URL và quyền truy cập Apps Script.";
            return true;
        }
        try {
            var reply = JsonUtility.FromJson<Failure>(text);
            if (reply != null && reply.success) return false;
            message = reply != null && !string.IsNullOrWhiteSpace(reply.message)
                ? reply.message : "API từ chối yêu cầu nhưng không cung cấp chi tiết.";
        } catch (System.Exception) { message = "API trả dữ liệu JSON không hợp lệ."; }
        return true;
    }
    public static string Url {
        get {
            var asset = Resources.Load<TextAsset>("AppsScriptSettings");
            var configuration = asset == null ? null : JsonUtility.FromJson<Configuration>(asset.text);
            return configuration != null && !string.IsNullOrWhiteSpace(configuration.url) ? configuration.url.Trim().TrimEnd('/')
                : "https://script.google.com/macros/s/AKfycbxLHvShuipgPg7x28IikDmzU7wHoE2e3ai9rhuoEnN50UsFUAWIPTG_nneHOxz-YmaY/exec";
        }
    }
}

[System.Serializable]
public class PlanetMatchRecord
{
    public string matchId, username, opponent, result, questionType, mode, finishedAt, subject, chapter;
    public int score, maxScore;
}

// Lives outside game panels: leaving the result screen cannot cancel the save.
// The persistent outbox and the Sheet's match ID check make retries safe.
public class PlanetHistorySync : MonoBehaviour
{
    [System.Serializable] class Outbox { public System.Collections.Generic.List<PlanetMatchRecord> records = new System.Collections.Generic.List<PlanetMatchRecord>(); }
    [System.Serializable] class Reply { public bool success; public string message; }
    const string StorageKey = "Planet.MatchHistory.Outbox.v1";
    static PlanetHistorySync instance;
    static readonly System.Collections.Generic.HashSet<string> saved = new System.Collections.Generic.HashSet<string>();
    public static event System.Action Changed;
    Outbox outbox = new Outbox();
    readonly System.Collections.Generic.Dictionary<string, string> errors = new System.Collections.Generic.Dictionary<string, string>();
    float retryAt;
    static string Key(string matchId, string username) { return matchId + ":" + username; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize() { Ensure(); }
    static PlanetHistorySync Ensure()
    {
        if (instance == null) {
            var go = new GameObject("Planet history synchronization");
            instance = go.AddComponent<PlanetHistorySync>();
            DontDestroyOnLoad(go);
        }
        return instance;
    }
    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        try { outbox = JsonUtility.FromJson<Outbox>(PlayerPrefs.GetString(StorageKey, "")) ?? new Outbox(); }
        catch { Debug.LogError("Không đọc được hàng đợi lịch sử Planet; giữ nguyên dữ liệu để kiểm tra."); enabled = false; return; }
        if (outbox.records == null) outbox.records = new System.Collections.Generic.List<PlanetMatchRecord>();
        StartCoroutine(Synchronize());
    }
    public static void Enqueue(PlanetMatchRecord record)
    {
        if (string.IsNullOrWhiteSpace(record.username) || string.IsNullOrEmpty(record.matchId) || (record.mode != "Luyện tập" && record.maxScore <= 0)) {
            Debug.LogError("Không lưu được lịch sử Planet: thiếu tài khoản, mã trận hoặc điểm tối đa."); return;
        }
        if (string.IsNullOrEmpty(record.finishedAt)) record.finishedAt = System.DateTime.UtcNow.ToString("o", System.Globalization.CultureInfo.InvariantCulture);
        var worker = Ensure();
        string key = Key(record.matchId, record.username);
        if (saved.Contains(key) || worker.outbox.records.Exists(r => Key(r.matchId, r.username) == key)) return;
        worker.outbox.records.Add(record); worker.Persist(); worker.retryAt = 0;
    }
    public static string Status(string matchId, string username)
    {
        string key = Key(matchId, username);
        if (saved.Contains(key)) return "ĐÃ LƯU LỊCH SỬ • ĐÃ CẬP NHẬT ĐIỂM XẾP HẠNG";
        if (string.IsNullOrWhiteSpace(username)) return "CHƯA LƯU: HÃY ĐĂNG NHẬP TÀI KHOẢN";
        if (instance != null && instance.errors.TryGetValue(key, out var error)) return "CHƯA ĐỒNG BỘ • " + error;
        return "ĐANG LƯU KẾT QUẢ • CÓ THỂ QUAY VỀ MENU";
    }
    void Persist() { PlayerPrefs.SetString(StorageKey, JsonUtility.ToJson(outbox)); PlayerPrefs.Save(); }
    IEnumerator Synchronize()
    {
        while (true) {
            if (outbox.records.Count == 0 || Time.realtimeSinceStartup < retryAt) { yield return new WaitForSecondsRealtime(1); continue; }
            // Process each item once per pass so one error cannot starve other users.
            var pass = outbox.records.ToArray();
            foreach (var record in pass) {
                var form = new WWWForm(); form.AddField("action", "savePlanetMatch"); form.AddField("record", JsonUtility.ToJson(record));
                bool success = false; string error = "Không kết nối được; sẽ tự thử lại.";
                using (var request = UnityWebRequest.Post(AppsScriptEndpoint.Url, form)) {
                    request.timeout = 45;
                    yield return request.SendWebRequest();
                    if (request.result != UnityWebRequest.Result.Success)
                        error = "Kết nối Google: " + request.error + "; kết quả vẫn đang chờ đồng bộ.";
                    if (request.result == UnityWebRequest.Result.Success) {
                        string body = request.downloadHandler.text;
                        Reply reply = null;
                        try { if (body.TrimStart().StartsWith("{")) reply = JsonUtility.FromJson<Reply>(body); } catch { }
                        success = reply != null && reply.success;
                        if (!success) error = reply != null && reply.message == "Invalid POST action"
                            ? "Cần cập nhật Apps Script có savePlanetMatch."
                            : reply != null && !string.IsNullOrEmpty(reply.message) ? reply.message : "API lưu kết quả trả dữ liệu không hợp lệ.";
                        if (!success && reply == null && AppsScriptEndpoint.TryReadFailure(body, out var apiError)) error = apiError;
                    }
                }
                string key = Key(record.matchId, record.username);
                if (success) {
                    outbox.records.Remove(record); Persist(); errors.Remove(key); saved.Add(key);
                    Changed?.Invoke();
                } else { errors[key] = error; Debug.LogWarning("Lịch sử Planet chưa đồng bộ: " + error); }
            }
            retryAt = Time.realtimeSinceStartup + 30;
        }
    }
}
