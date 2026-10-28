using System.Collections;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;
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
    GameObject retryButton;
    UnityWebRequest activeRequest;
    bool loading, reloadRequested;
    HistoryResponse loadedHistory;
    int selectedMode;
    bool filtersReady;
    readonly List<Material> tabMaterials = new List<Material>();

    void SetupFilters()
    {
        if (filtersReady) return;
        filtersReady = true;
        var root = transform.parent;
        string[] names = { "btn_all", "btn_doikhang", "btn_ luyentap" };
        var shader = Resources.Load<Shader>("HistoryTabTint");
        for (int i = 0; i < names.Length; i++) {
            var target = root.Find(names[i]);
            var button = target == null ? null : target.GetComponent<Button>();
            Material material = null;
            if (button != null) {
                int mode = i;
                button.onClick.AddListener(() => SelectMode(mode));
                var image = button.GetComponent<Image>();
                if (image != null && shader != null) { material = new Material(shader); image.material = material; }
            }
            tabMaterials.Add(material);
        }
        BuildChapterHeader(root);
    }

    void SelectMode(int mode)
    {
        selectedMode = mode;
        for (int i = 0; i < tabMaterials.Count; i++)
            if (tabMaterials[i] != null) tabMaterials[i].SetFloat("_Selected", i == mode ? 1 : 0);
        if (loadedHistory != null) RenderHistory();
    }

    void RenderHistory()
    {
        var items = new List<HistoryItem>();
        foreach (var item in loadedHistory.histories ?? new HistoryItem[0]) {
            if (selectedMode == 0 || string.Equals((item.mode ?? "").Trim(), selectedMode == 1 ? "Đối kháng" : "Luyện tập", System.StringComparison.OrdinalIgnoreCase)) items.Add(item);
        }
        ShowHistory(new HistoryResponse { total = loadedHistory.total, win = loadedHistory.win, lose = loadedHistory.lose, winRate = loadedHistory.winRate, histories = items.ToArray() });
        SetStatus(items.Count > 0 ? "" : selectedMode == 0 ? "Bạn chưa có lịch sử thi đấu." : selectedMode == 1 ? "Bạn chưa có lịch sử đối kháng." : "Bạn chưa có lịch sử luyện tập.");
        var scroll = historyContent.GetComponentInParent<ScrollRect>(); if (scroll != null) scroll.verticalNormalizedPosition = 1;
    }

    // Reuse clean wood and parchment from the current board, retaining its
    // decorative frame. Only the interior header and cell grid are relaid out.
    void BuildChapterHeader(Transform root)
    {
        var boardTransform = root.Find("khung 2");
        var board = boardTransform == null ? null : boardTransform.GetComponent<Image>();
        var row = historyRowPrefab == null ? null : historyRowPrefab.GetComponent<HistoryRow>();
        if (board == null || board.sprite == null || row == null) return;
        var texture = board.sprite.texture;
        var header = new GameObject("History six-column header", typeof(RectTransform), typeof(RawImage)); header.transform.SetParent(boardTransform, false);
        var headerRect = header.GetComponent<RectTransform>(); headerRect.anchorMin = new Vector2(.080f, .571f); headerRect.anchorMax = new Vector2(.922f, .626f); headerRect.offsetMin = headerRect.offsetMax = Vector2.zero;
        var wood = header.GetComponent<RawImage>(); wood.texture = texture; wood.raycastTarget = false;
        wood.uvRect = new Rect(150f / texture.width, 650f / texture.height, 12f / texture.width, 38f / texture.height);
        string[] labels = { "Đối thủ", "Chế độ", "Chương", "Kết quả", "Điểm", "Thời gian" };
        for (int i = 0; i < labels.Length; i++) {
            var label = Instantiate(row.modeText, header.transform); label.name = labels[i]; label.text = labels[i]; label.richText = false; label.raycastTarget = false;
            label.rectTransform.anchorMin = new Vector2(HistoryRow.ColumnEdges[i], 0); label.rectTransform.anchorMax = new Vector2(HistoryRow.ColumnEdges[i + 1], 1);
            label.rectTransform.offsetMin = new Vector2(3, 0); label.rectTransform.offsetMax = new Vector2(-3, 0);
            label.fontStyle = FontStyles.Bold; label.color = new Color(1, .96f, .85f); label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true; label.fontSizeMax = 32; label.fontSizeMin = 20;
            label.outlineColor = new Color32(67, 29, 14, 255); label.outlineWidth = .15f;
        }
        var scroll = historyContent.GetComponentInParent<ScrollRect>();
        if (scroll == null || scroll.viewport == null) return;
        var paper = new GameObject("History parchment", typeof(RectTransform), typeof(RawImage)); paper.transform.SetParent(scroll.viewport, false); paper.transform.SetAsFirstSibling();
        var paperRect = paper.GetComponent<RectTransform>(); paperRect.anchorMin = Vector2.zero; paperRect.anchorMax = Vector2.one; paperRect.offsetMin = paperRect.offsetMax = Vector2.zero;
        var background = paper.GetComponent<RawImage>(); background.texture = texture; background.raycastTarget = false;
        background.uvRect = new Rect(140f / texture.width, 560f / texture.height, 80f / texture.width, 35f / texture.height);
    }

    void OnDestroy() { foreach (var material in tabMaterials) if (material != null) Destroy(material); }


    private void OnEnable()
    {
        PlanetHistorySync.Changed += LoadHistory;
        SetupFilters(); loadedHistory = null; SelectMode(0);
        LoadUserInfo();
        LoadHistory();
    }
    private void OnDisable() {
        PlanetHistorySync.Changed -= LoadHistory;
        activeRequest?.Abort(); StopAllCoroutines(); activeRequest = null;
        loading = false; reloadRequested = false;
    }

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
        // Completed outbox writes may arrive while a read is still in flight.
        // Finish that read, then refresh once for all queued changes.
        if (loading) { reloadRequested = true; return; }
        loading = true;
        SetStatus("Đang tải lịch sử…");
        StartCoroutine(LoadRoutine());
    }

    IEnumerator LoadRoutine()
    {
        do {
            reloadRequested = false;
            yield return GetHistory();
        } while (reloadRequested && isActiveAndEnabled);
        loading = false;
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

        for (int attempt = 0; attempt < 2; attempt++) {
        UnityWebRequest request = UnityWebRequest.Get(url);
        using (request) {
        activeRequest = request;
        request.timeout = 45;

        request.certificateHandler = new BypassCertificate();
            yield return request.SendWebRequest();
        activeRequest = null;


        if (request.result != UnityWebRequest.Result.Success)
        {
            bool temporary = request.result == UnityWebRequest.Result.ConnectionError || request.responseCode == 429 || request.responseCode >= 500;
            if (temporary && attempt == 0) {
                SetStatus("Kết nối lịch sử chậm hoặc gián đoạn. Đang thử lại…");
                yield return new WaitForSecondsRealtime(2);
                continue;
            }
            string error = "Chưa tải được lịch sử từ Google. " + request.error + "\nBạn có thể bấm Thử lại.";
            SetStatus(error, true);
            Debug.LogError(error);

            yield break;
        }


        string body = request.downloadHandler.text;
        if (AppsScriptEndpoint.TryReadFailure(body, out var apiError)) {
            SetStatus(apiError, true);
            Debug.LogError("History API: " + apiError);
            yield break;
        }


        HistoryResponse response = null;
        try { response = JsonUtility.FromJson<HistoryResponse>(body); }
        catch (System.Exception) { Debug.LogError("API lịch sử không trả JSON hợp lệ."); }


        if (response == null || !response.success)
        {
            SetStatus("API lịch sử trả dữ liệu không hợp lệ.", true);
            Debug.LogError("History API trả dữ liệu không hợp lệ.");

            yield break;
        }


        loadedHistory = response;
        RenderHistory();
        yield break;
        }
        }
    }

    void SetStatus(string message, bool canRetry = false)
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
            labelRect.anchorMin = new Vector2(.06f, .32f); labelRect.anchorMax = new Vector2(.94f, .85f);
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            statusText = label.GetComponent<TextMeshProUGUI>();
            if (totalText != null) statusText.font = totalText.font;
            statusText.fontSize = 28; statusText.enableAutoSizing = true;
            statusText.fontSizeMin = 18; statusText.fontSizeMax = 28;
            statusText.color = new Color(.3f, .18f, .08f);
            statusText.alignment = TextAlignmentOptions.Center;
            statusText.richText = false; statusText.raycastTarget = false;
            retryButton = new GameObject("Retry history", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            retryButton.transform.SetParent(go.transform, false);
            var retryRect = retryButton.GetComponent<RectTransform>();
            retryRect.anchorMin = new Vector2(.32f, .1f); retryRect.anchorMax = new Vector2(.68f, .27f); retryRect.offsetMin = retryRect.offsetMax = Vector2.zero;
            retryButton.GetComponent<UnityEngine.UI.Image>().color = new Color(.2f, .43f, .7f);
            retryButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(LoadHistory);
            var retryLabel = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)); retryLabel.transform.SetParent(retryButton.transform, false);
            var retryLabelRect = retryLabel.GetComponent<RectTransform>(); retryLabelRect.anchorMin = Vector2.zero; retryLabelRect.anchorMax = Vector2.one; retryLabelRect.offsetMin = retryLabelRect.offsetMax = Vector2.zero;
            var retryText = retryLabel.GetComponent<TextMeshProUGUI>(); retryText.font = statusText.font; retryText.text = "Thử lại"; retryText.fontSize = 26; retryText.alignment = TextAlignmentOptions.Center; retryText.color = Color.white; retryText.raycastTarget = false;
        }
        statusText.text = message;
        if (retryButton != null) retryButton.SetActive(canRetry);
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
            child.gameObject.SetActive(false);
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
