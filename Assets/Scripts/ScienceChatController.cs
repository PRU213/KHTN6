using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

/// <summary>Runtime overlay: keeps the existing HomePage scene and its artwork intact.</summary>
public class ScienceChatController : MonoBehaviour
{
    [Serializable] public class Message { public string role; public string text; }
    [Serializable] class Request { public string message; public List<Message> history; public string clientId, reviewContext; }
    [Serializable] class Reply { public string reply; public string error; }
    [Serializable] class Settings { public string serverUrl = "http://localhost:8787"; }
    List<Message> history = new List<Message>();
    readonly Dictionary<string, List<Message>> reviewChats = new Dictionary<string, List<Message>>();
    string reviewContext;
    bool reviewMode;
    TMP_Text reviewTitle;
    RectTransform page, content;
    GameObject overlay;
    TMP_InputField input;
    TMP_Text status;
    ScrollRect scroll;
    Button send;
    UnityWebRequest pending;
    bool busy;
    readonly string clientId = Guid.NewGuid().ToString("N");
    string serverUrl = "http://localhost:8787";
    static readonly Color Ink = new Color(.22f, .13f, .065f);
    static TMP_FontAsset chatFont;

    public static void Install(GameObject home)
    {
        if (home == null || home.GetComponent<ScienceChatController>() != null) return;
        home.AddComponent<ScienceChatController>().BuildHome(home);
    }

    void BuildHome(GameObject home)
    {
        var settings = Resources.Load<TextAsset>("ScienceChatSettings");
        if (settings != null) serverUrl = JsonUtility.FromJson<Settings>(settings.text).serverUrl;
        // XanhDuong is the existing white/cyan robot beside the teacher.
        var robot = home.transform.Find("XanhDuong") as RectTransform;
        if (robot == null) { Debug.LogWarning("Science chat: HomePage/XanhDuong was not found."); return; }
        var graphic = robot.GetComponent<Image>();
        if (graphic != null) graphic.raycastTarget = true;
        var button = robot.GetComponent<Button>() ?? robot.gameObject.AddComponent<Button>();
        button.onClick.AddListener(Open);
        var bubble = Rect("AI invitation", robot, 0, 1, 1, 1);
        bubble.sizeDelta = new Vector2(100, 65); bubble.anchoredPosition = new Vector2(0, 18);
        var bg = bubble.gameObject.AddComponent<Image>(); bg.color = new Color(1, .98f, .88f);
        var outline = bubble.gameObject.AddComponent<Outline>(); outline.effectColor = new Color(.1f, .65f, .9f); outline.effectDistance = new Vector2(3, -3);
        Label(bubble, "Hãy hỏi tớ đi!", 25, 0, 0, 1, 1);
        bubble.gameObject.AddComponent<Button>().onClick.AddListener(Open);
    }

    void Build()
    {
        if (reviewMode) { BuildReview(); return; }
        overlay = new GameObject("Science AI chat", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = overlay.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 300;
        var scaler = overlay.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
        var backdrop = Rect("Backdrop", overlay.transform, 0, 0, 1, 1).gameObject.AddComponent<Image>(); backdrop.color = new Color(.015f, .055f, .08f);
        page = Rect("Forest chat", overlay.transform, .5f, .5f, .5f, .5f);
        page.sizeDelta = new Vector2(1920, 1080);
        var skin = page.gameObject.AddComponent<RawImage>(); skin.texture = Resources.Load<Texture2D>("ScienceChatSkin");
        Label(page, "HỎI ĐÁP CÙNG AI", 50, .36f, .055f, .67f, .145f, new Color(1, .91f, .57f));
        Click("Quay lại", .075f, .045f, .16f, .105f, Close, 25);
        Click("", .92f, .025f, .98f, .12f, Close);
        Label(page, "Bạn đồng hành khoa học", 29, .53f, .19f, .79f, .25f);
        Label(page, "Cùng tớ khám phá\nkhoa học nhé!", 32, .13f, .19f, .29f, .29f);
        Suggestion("Vì sao có cầu vồng?", .60f, .66f);
        Suggestion("Cây tạo oxygen thế nào?", .69f, .75f);
        Suggestion("Lực ma sát là gì?", .78f, .84f);

        var view = TopRect("Messages", page, .405f, .285f, .93f, .70f);
        view.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, .015f);
        view.gameObject.AddComponent<RectMask2D>();
        scroll = view.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.viewport = view; scroll.movementType = ScrollRect.MovementType.Clamped;
        content = Rect("Content", view, 0, 1, 1, 1); content.pivot = new Vector2(.5f, 1);
        var layout = content.gameObject.AddComponent<VerticalLayoutGroup>(); layout.spacing = 16; layout.padding = new RectOffset(10, 10, 10, 10); layout.childControlHeight = true; layout.childControlWidth = true; layout.childForceExpandHeight = false;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = content;
        var field = TopRect("Question input", page, .42f, .748f, .785f, .805f);
        // TMP creates its caret and registers text callbacks in OnEnable.
        // Configure all references while inactive so its first enable is complete.
        field.gameObject.SetActive(false);
        var fieldGraphic = field.gameObject.AddComponent<Image>();
        fieldGraphic.color = new Color(1, 1, 1, .01f); fieldGraphic.raycastTarget = true;
        var textArea = Rect("Text Area", field, 0, 0, 1, 1);
        textArea.offsetMin = new Vector2(12, 3); textArea.offsetMax = new Vector2(-12, -3);
        textArea.gameObject.AddComponent<RectMask2D>();
        var text = Label(textArea, "", 27, 0, 0, 1, 1); text.alignment = TextAlignmentOptions.MidlineLeft;
        var placeholder = Label(textArea, "Nhập câu hỏi của bạn…", 26, 0, 0, 1, 1, new Color(.5f, .46f, .38f)); placeholder.alignment = TextAlignmentOptions.MidlineLeft;
        input = field.gameObject.AddComponent<TMP_InputField>();
        input.targetGraphic = fieldGraphic;
        input.textViewport = textArea; input.textComponent = text; input.placeholder = placeholder;
        input.readOnly = false; input.interactable = true;
        input.contentType = TMP_InputField.ContentType.Standard;
        input.richText = false;
        input.characterLimit = 1200; input.lineType = TMP_InputField.LineType.SingleLine;
        input.customCaretColor = true; input.caretColor = Ink; input.caretWidth = 2;
        input.onSubmit.AddListener(_ => Send());
        field.gameObject.SetActive(true);
        send = Click("Gửi", .815f, .745f, .915f, .812f, Send, 32);
        status = Label(page, "AI có thể nhầm lẫn. Hãy đối chiếu với bài học nhé.", 21, .415f, .825f, .92f, .855f);
        Click("Trò chuyện mới", .42f, .875f, .59f, .915f, ResetChat, 23);
        AddMessage("robot", "Chào bạn! Tớ có thể cùng bạn tìm hiểu Vật lí, Hóa học và Sinh học lớp 6. Bạn muốn khám phá điều gì nào?");
    }

    void Update()
    {
        if (page == null || !overlay.activeSelf) return;
        var root = (RectTransform)overlay.transform;
        page.localScale = Vector3.one * Mathf.Min(root.rect.width / 1920, root.rect.height / 1080);
    }
    void FocusInput()
    {
        if (input == null || !input.isActiveAndEnabled || !input.interactable) return;
        input.Select();
        input.ActivateInputField();
    }
    public void Open() { if (overlay == null) Build(); overlay.SetActive(true); Update(); FocusInput(); }
    public static ScienceChatController ForReview(Component owner)
    {
        return owner.GetComponent<ScienceChatController>() ?? owner.gameObject.AddComponent<ScienceChatController>();
    }
    public void ClearReviews()
    {
        Close(); reviewChats.Clear(); history = new List<Message>(); reviewContext = null;
    }
    public void OpenReview(string key, string title, string context, Color accent)
    {
        CancelRequest(); reviewMode = true;
        var settings = Resources.Load<TextAsset>("PlanetDuelSettings");
        if (settings != null) serverUrl = JsonUtility.FromJson<Settings>(settings.text).serverUrl;
        if (overlay == null) BuildReview(accent);
        if (!reviewChats.TryGetValue(key, out history)) { history = new List<Message>(); reviewChats[key] = history; }
        reviewContext = context.Length > 12000 ? context.Substring(0, 12000) : context;
        reviewTitle.text = title;
        foreach (Transform child in content) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
        AddMessage("robot", "Em muốn hiểu rõ phần nào của câu vừa làm? Tớ có thể giải thích chỗ còn thiếu, giải thích đáp án hoặc đưa ví dụ.");
        foreach (var message in history) AddMessage(message.role == "user" ? "user" : "robot", message.text);
        EssayInputState.Clear(input);
        overlay.SetActive(true); Update();
        status.text = "Hỏi thêm để hiểu bài • Nội dung trò chuyện không thay đổi điểm.";
    }
    void BuildReview(Color? accent = null)
    {
        overlay = new GameObject("Essay review chat", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = overlay.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 500;
        var scaler = overlay.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
        Rect("Shade", overlay.transform, 0, 0, 1, 1).gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .8f);
        page = Rect("Review", overlay.transform, .5f, .5f, .5f, .5f); page.sizeDelta = new Vector2(1780, 980);
        page.gameObject.AddComponent<Image>().color = accent ?? new Color(.055f, .09f, .2f);
        Label(page, "HỎI AI THÊM", 42, .05f, .035f, .8f, .11f, Color.white);
        Click("Đóng", .85f, .035f, .96f, .11f, Close, 26);
        reviewTitle = Label(page, "", 27, .05f, .13f, .95f, .23f, Color.white);
        SuggestReview("Giải thích chỗ em thiếu", .31f);
        SuggestReview("Giải thích đáp án dễ hiểu hơn", .47f);
        SuggestReview("Cho em một ví dụ thực tế", .63f);
        var view = TopRect("Messages", page, .30f, .26f, .95f, .73f);
        view.gameObject.AddComponent<Image>().color = new Color(.94f, .97f, 1);
        view.gameObject.AddComponent<RectMask2D>();
        scroll = view.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.viewport = view; scroll.movementType = ScrollRect.MovementType.Clamped;
        content = Rect("Content", view, 0, 1, 1, 1); content.pivot = new Vector2(.5f, 1);
        var layout = content.gameObject.AddComponent<VerticalLayoutGroup>(); layout.spacing = 14; layout.padding = new RectOffset(12, 12, 12, 12); layout.childControlHeight = true; layout.childControlWidth = true; layout.childForceExpandHeight = false;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize; scroll.content = content;
        var field = TopRect("Question", page, .30f, .77f, .81f, .85f); field.gameObject.SetActive(false);
        var graphic = field.gameObject.AddComponent<Image>(); graphic.color = Color.white;
        var area = Rect("TextArea", field, 0, 0, 1, 1); area.offsetMin = new Vector2(12, 6); area.offsetMax = new Vector2(-12, -6); area.gameObject.AddComponent<RectMask2D>();
        var text = Label(area, "", 27, 0, 0, 1, 1); text.alignment = TextAlignmentOptions.TopLeft;
        var placeholder = Label(area, "Nhập điều em chưa hiểu…", 25, 0, 0, 1, 1, Color.gray); placeholder.alignment = TextAlignmentOptions.TopLeft;
        input = field.gameObject.AddComponent<TMP_InputField>(); input.textViewport = area; input.textComponent = text; input.placeholder = placeholder; input.targetGraphic = graphic;
        input.contentType = TMP_InputField.ContentType.Standard; input.richText = false; input.characterLimit = 1200; input.lineType = TMP_InputField.LineType.MultiLineNewline;
        EssayInputState.Configure(input); field.gameObject.SetActive(true);
        send = Click("Gửi", .83f, .77f, .95f, .85f, Send, 30);
        status = Label(page, "", 22, .30f, .87f, .95f, .92f, Color.white);
        Click("Trò chuyện mới", .30f, .93f, .57f, .98f, ResetChat, 23);
    }
    void SuggestReview(string question, float top)
    {
        Click(question, .035f, top, .275f, top + .11f, () => { if (!busy) { input.text = question; Send(); } }, 25);
    }
    void Close() { CancelRequest(); if (overlay != null) overlay.SetActive(false); }
    void ResetChat()
    {
        if (busy) return;
        history.Clear(); foreach (Transform child in content) Destroy(child.gameObject);
        AddMessage("robot", "Bắt đầu khám phá mới nhé! Bạn muốn hỏi gì?"); status.text = "AI có thể nhầm lẫn. Hãy đối chiếu với bài học nhé.";
    }
    void Suggestion(string question, float top, float bottom) { Click(question, .115f, top, .25f, bottom, () => { if (!busy) { input.text = question; Send(); } }, 25); }
    void Send() { if (!busy && !string.IsNullOrWhiteSpace(input.text)) StartCoroutine(Ask(input.text.Trim())); }
    IEnumerator Ask(string question)
    {
        busy = true; send.interactable = false; input.interactable = false; status.text = "Tớ đang suy nghĩ…";
        AddMessage("user", question);
        var payload = new Request { message = question, history = new List<Message>(history), clientId = clientId, reviewContext = reviewContext };
        pending = new UnityWebRequest(serverUrl.TrimEnd('/') + "/chat", "POST");
        pending.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload)));
        // Server has a 45-second total deadline; allow time for its error response.
        pending.downloadHandler = new DownloadHandlerBuffer(); pending.SetRequestHeader("Content-Type", "application/json"); pending.timeout = 60;
        yield return pending.SendWebRequest();
        Reply result = null;
        try { result = JsonUtility.FromJson<Reply>(pending.downloadHandler.text); } catch (ArgumentException) { }
        if (pending.result == UnityWebRequest.Result.Success && result != null && !string.IsNullOrEmpty(result.reply))
        {
            history.Add(new Message { role = "user", text = question }); history.Add(new Message { role = "model", text = result.reply });
            while (history.Count > 12) history.RemoveRange(0, 2);
            AddMessage("robot", result.reply); input.text = ""; status.text = "AI có thể nhầm lẫn. Hãy đối chiếu với bài học nhé.";
        }
        else
        {
            AddMessage("error", result != null && !string.IsNullOrEmpty(result.error) ? result.error : "Tớ chưa kết nối được máy chủ. Hãy kiểm tra máy chủ và mạng rồi gửi lại nhé.");
            status.text = "Câu hỏi được giữ lại để bạn bấm Gửi thử lại.";
        }
        pending.Dispose(); pending = null; busy = false; send.interactable = true; input.interactable = true; FocusInput();
    }
    void CancelRequest()
    {
        if (!busy) return;
        StopAllCoroutines(); pending?.Abort(); pending?.Dispose(); pending = null; busy = false;
        if (send != null) send.interactable = true;
        if (input != null) input.interactable = true;
        if (status != null) status.text = "Đã dừng trả lời. Bạn có thể gửi lại câu hỏi.";
    }
    void OnDisable() { CancelRequest(); if (overlay != null) overlay.SetActive(false); }
    void OnDestroy() { CancelRequest(); if (overlay != null) Destroy(overlay); }
    void AddMessage(string role, string value)
    {
        var row = new GameObject("Message", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup)); row.transform.SetParent(content, false);
        row.GetComponent<Image>().color = role == "user" ? new Color(.81f, .93f, 1) : role == "error" ? new Color(1, .86f, .77f) : new Color(1, .98f, .88f);
        var layout = row.GetComponent<VerticalLayoutGroup>(); layout.padding = new RectOffset(20, 20, 14, 14); layout.childControlHeight = true; layout.childControlWidth = true; layout.childForceExpandHeight = false;
        var label = Label(row.transform, (role == "user" ? "Bạn" : "Bạn đồng hành AI") + "\n" + value, 28, 0, 0, 1, 1);
        label.alignment = TextAlignmentOptions.TopLeft; label.richText = false;
        Canvas.ForceUpdateCanvases(); LayoutRebuilder.ForceRebuildLayoutImmediate(content); scroll.verticalNormalizedPosition = 0;
        // Bound UI memory independently of the shorter context sent to Gemini.
        if (content.childCount > 40) Destroy(content.GetChild(0).gameObject);
    }
    Button Click(string label, float x, float y, float right, float bottom, UnityEngine.Events.UnityAction action, int size = 28)
    {
        var rect = TopRect(label + " button", page, x, y, right, bottom);
        rect.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, .01f);
        var button = rect.gameObject.AddComponent<Button>(); button.onClick.AddListener(action);
        Label(rect, label, size, 0, 0, 1, 1, new Color(1, .96f, .8f)); return button;
    }
    static TMP_Text Label(Transform parent, string value, int size, float x, float y, float right, float bottom, Color? color = null)
    {
        var rect = TopRect("Label", parent, x, y, right, bottom); var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (chatFont == null)
        {
            var source = Resources.Load<Font>("ScienceChatFont");
            if (source != null) { chatFont = TMP_FontAsset.CreateFontAsset(source); chatFont.isMultiAtlasTexturesEnabled = true; }
        }
        if (chatFont != null) text.font = chatFont;
        text.text = value; text.fontSize = size; text.color = color ?? Ink; text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false; text.richText = false;
        return text;
    }
    static RectTransform TopRect(string name, Transform parent, float x, float top, float right, float bottom) { return Rect(name, parent, x, 1 - bottom, right, 1 - top); }
    static RectTransform Rect(string name, Transform parent, float x, float y, float right, float top)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(x, y); rect.anchorMax = new Vector2(right, top); rect.offsetMin = rect.offsetMax = Vector2.zero; return rect;
    }
}
