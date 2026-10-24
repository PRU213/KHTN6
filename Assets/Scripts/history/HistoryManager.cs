using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class HistoryManager : MonoBehaviour
{
    [Header("Google Apps Script")]
    public string baseUrl =
        "DÁN_LINK_WEB_APP_CỦA_BẠN_Ở_ĐÂY";

    [Header("Thông tin người chơi hiện tại (Gắn Text hoặc TMP_Text)")]
    [SerializeField] private UnityEngine.UI.Text txtFullname;
    [SerializeField] private UnityEngine.UI.Text txtRole;
    [SerializeField] private TMP_Text txtFullnameTMP;
    [SerializeField] private TMP_Text txtRoleTMP;

    [Header("Thông tin học sinh")]
    public TMP_Text studentNameText;

    [Header("Thống kê")]
    public TMP_Text totalText;
    public TMP_Text winText;
    public TMP_Text loseText;
    public TMP_Text rateText;

    [Header("Danh sách lịch sử")]
    public Transform historyContent;
    public GameObject historyRowPrefab;
    TMP_Text statusText;


    private void OnEnable()
    {
        PlanetHistorySync.Changed += LoadHistory;
        LoadUserInfo();
        LoadHistory();
    }
    private void OnDisable() { PlanetHistorySync.Changed -= LoadHistory; StopAllCoroutines(); }

    private void LoadUserInfo()
    {
        // Tự động tìm nốt nếu chưa gắn trong Inspector
        if (txtFullnameTMP == null && txtFullname == null)
        {
            GameObject obj = GameObject.Find("txtFullname");
            if (obj == null) obj = GameObject.Find("txtName");

            if (obj != null)
            {
                txtFullnameTMP = obj.GetComponent<TMP_Text>();
                txtFullname = obj.GetComponent<UnityEngine.UI.Text>();
            }
        }

        if (txtRoleTMP == null && txtRole == null)
        {
            GameObject obj = GameObject.Find("txtRole");
            if (obj == null) obj = GameObject.Find("role");

            if (obj != null)
            {
                txtRoleTMP = obj.GetComponent<TMP_Text>();
                txtRole = obj.GetComponent<UnityEngine.UI.Text>();
            }
        }

        // Lấy thông tin đã được lưu từ sheet lúc đăng nhập
        string fullName = GameSettingsController.DisplayName;
        string role = PlayerPrefs.GetString("Role", "Student");

        if (txtFullname != null) txtFullname.text = fullName;
        if (txtRole != null) txtRole.text = role;

        if (txtFullnameTMP != null) txtFullnameTMP.text = fullName;
        if (txtRoleTMP != null) txtRoleTMP.text = role;
    }


    public void LoadHistory()
    {
        if (!isActiveAndEnabled) return;
        StopAllCoroutines();
        SetStatus("Đang tải lịch sử…");
        StartCoroutine(GetHistory());
    }


    IEnumerator GetHistory()
    {
        // Only request the signed-in student's history.
        string username = string.IsNullOrWhiteSpace(GameSession.Username) ? PlayerPrefs.GetString("Username", "") : GameSession.Username;
        if (string.IsNullOrWhiteSpace(username)) { SetStatus("Hãy đăng nhập để xem lịch sử."); yield break; }

        string url =
            AppsScriptEndpoint.Url +
            "?action=history" +
            "&username=" +
            UnityWebRequest.EscapeURL(username);

        UnityWebRequest request =
            UnityWebRequest.Get(url);
        using (request) {
        request.timeout = 35;

        request.certificateHandler = new BypassCertificate();
            yield return request.SendWebRequest();


        if (request.result != UnityWebRequest.Result.Success)
        {
            string error = "Không tải được lịch sử: " + request.error;
            SetStatus(error);
            Debug.LogError(error);

            yield break;
        }


        string body = request.downloadHandler.text;
        if (AppsScriptEndpoint.TryReadFailure(body, out var apiError)) {
            SetStatus(apiError);
            Debug.LogError("History API: " + apiError);
            yield break;
        }


        HistoryResponse response = null;
        try { response = JsonUtility.FromJson<HistoryResponse>(body); }
        catch (System.Exception) { Debug.LogError("API lịch sử không trả JSON hợp lệ."); }


        if (response == null || !response.success)
        {
            SetStatus("API lịch sử trả dữ liệu không hợp lệ.");
            Debug.LogError("History API trả dữ liệu không hợp lệ.");

            yield break;
        }


        ShowHistory(response);
        SetStatus(response.histories == null || response.histories.Length == 0
            ? "Bạn chưa có lịch sử thi đấu." : "");
        }
    }

    void SetStatus(string message)
    {
        if (statusText == null) {
            var scroll = historyContent != null ? historyContent.GetComponentInParent<UnityEngine.UI.ScrollRect>() : null;
            if (scroll == null || scroll.viewport == null) return;
            var go = new GameObject("History Load Status", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            go.transform.SetParent(scroll.viewport, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var backing = go.GetComponent<UnityEngine.UI.Image>();
            backing.color = new Color(1, .95f, .83f, .97f); backing.raycastTarget = false;
            var label = new GameObject("Message", typeof(RectTransform), typeof(TextMeshProUGUI));
            label.transform.SetParent(go.transform, false);
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(.06f, .15f); labelRect.anchorMax = new Vector2(.94f, .85f);
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            statusText = label.GetComponent<TextMeshProUGUI>();
            if (totalText != null) statusText.font = totalText.font;
            statusText.fontSize = 28; statusText.enableAutoSizing = true;
            statusText.fontSizeMin = 18; statusText.fontSizeMax = 28;
            statusText.color = new Color(.3f, .18f, .08f);
            statusText.alignment = TextAlignmentOptions.Center;
            statusText.richText = false; statusText.raycastTarget = false;
        }
        statusText.text = message;
        statusText.transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(message));
    }


    void ShowHistory(HistoryResponse data)
    {
        // =============================
        // Thông tin học sinh
        // =============================

        if (studentNameText != null) {
            studentNameText.text = GameSettingsController.DisplayName;
            studentNameText.richText = false;
        }


        // =============================
        // Thống kê
        // =============================

        if (totalText != null)
            totalText.text = data.total.ToString();

        if (winText != null)
            winText.text = data.win.ToString();

        if (loseText != null)
            loseText.text = data.lose.ToString();

        if (rateText != null)
            rateText.text = data.winRate + "%";


        // =============================
        // Xóa dòng cũ
        // =============================

        foreach (Transform child in historyContent)
        {
            Destroy(child.gameObject);
        }


        // =============================
        // Tạo lịch sử
        // =============================

        if (data.histories == null)
            return;


        foreach (HistoryItem item in data.histories)
        {
            GameObject row =
                            Instantiate(
                                historyRowPrefab,
                                historyContent
                            );

            HistoryRow historyRow =
                row.GetComponent<HistoryRow>();

            if (historyRow != null)
            {
                historyRow.SetData(item);
            }
        }
    }
}
