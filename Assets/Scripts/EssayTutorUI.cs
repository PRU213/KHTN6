using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Guided practice and Planet remediation. Server owns mastery and history.</summary>
public sealed class EssayTutorUI : MonoBehaviour
{
    [Serializable] public class Turn { public int step; public bool checking, correct; public string prompt, answer, explanation, at; }
    [Serializable] public class Record {
        public string id, username, subject, chapter, question, studentAnswer, status, prompt, explanation, solution, createdAt, updatedAt;
        public int step, total, revision, score; public bool checking; public Turn[] events;
    }
    [Serializable] class Request {
        public string id, username, answer, requestId, password, matchId; public int revision;
        public PlanetDuelController.ReviewQuestion[] reviewQuestions;
    }
    [Serializable] class Response : Record {
        public string error, token; public bool teacher; public Record[] records;
        public PlanetDuelController.ReviewQuestion[] reviewQuestions; public int reviewPending;
        public int duelReviewVersion;
    }
    [Serializable] class Settings { public string serverUrl; }
    static EssayTutorUI instance;
    static TMP_FontAsset font;
    static string accessToken, tokenAccount;
    RectTransform loginPanel;
    TMP_InputField password;
    TMP_Text loginStatus;
    string deferredPath;
    Request deferredBody;
    RectTransform page, content;
    TMP_Text title, status;
    TMP_InputField input;
    Button send, unknown, refresh, continueButton;
    ScrollRect scroll;
    UnityWebRequest pending;
    Record current;
    Request retryAnswer;
    string username, url;
    bool teacher, busy, listMode;
    Action<PlanetDuelController.ReviewQuestion[], int> planetReviewLoaded;
    static readonly Color Ink = new Color(.08f, .17f, .28f);
    static readonly Color Paper = new Color(.96f, .98f, 1);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void InitializeAuthentication()
    {
        accessToken = tokenAccount = null;
        SceneManager.sceneLoaded -= ResetAuthenticationOnLogin;
        SceneManager.sceneLoaded += ResetAuthenticationOnLogin;
    }
    static void ResetAuthenticationOnLogin(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "SampleScene" || scene.name == "SignIn" || string.IsNullOrWhiteSpace(CurrentUsername))
            accessToken = tokenAccount = null;
    }

    public static string CurrentUsername => string.IsNullOrWhiteSpace(GameSession.Username) ? PlayerPrefs.GetString("Username", "") : GameSession.Username;

    static EssayTutorUI Get()
    {
        if (instance != null) return instance;
        var go = new GameObject("Essay guided learning", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        instance = go.AddComponent<EssayTutorUI>(); instance.Build();
        return instance;
    }
    public static void Open(string id)
    {
        var ui = Get(); ui.Cancel(); ui.teacher = false; ui.username = CurrentUsername;
        ui.planetReviewLoaded = null;
        ui.current = new Record { id = id }; ui.listMode = false; ui.Clear(); ui.retryAnswer = null; ui.gameObject.SetActive(true);
        ui.StartCoroutine(ui.Load("start", new Request { id = id, username = ui.username }));
    }
    public static void OpenHistory(string account, bool readOnly = false)
    {
        var ui = Get(); ui.Cancel(); ui.username = account; ui.teacher = readOnly; ui.current = null; ui.listMode = true; ui.Clear(); ui.retryAnswer = null;
        ui.planetReviewLoaded = null;
        ui.gameObject.SetActive(true); ui.StartCoroutine(ui.Load("list", new Request { username = account }));
    }
    public static void LoadPlanetReviews(PlanetDuelController.LocalReviewBatch batch, Action<PlanetDuelController.ReviewQuestion[], int> done)
    {
        var ui = Get(); ui.Cancel(); ui.teacher = false; ui.username = CurrentUsername;
        ui.current = null; ui.listMode = true; ui.Clear(); ui.retryAnswer = null; ui.planetReviewLoaded = done;
        ui.gameObject.SetActive(true); ui.title.text = "CÂU SAI ĐỐI KHÁNG • HỌC LẠI TỪNG BƯỚC";
        ui.StartCoroutine(ui.Load(batch == null ? "/review/pending" : "/review/local", new Request {
            username = ui.username, matchId = batch?.matchId, reviewQuestions = batch?.reviewQuestions
        }));
    }
    public static void AttachHistoryButton(Transform parent, Func<string> account, bool readOnly)
    {
        if (parent == null || parent.Find("Essay tutor history") != null) return;
        var rect = Box(parent, "Essay tutor history", .64f, .015f, .89f, .065f, new Color(.08f, .35f, .55f));
        var button = rect.gameObject.AddComponent<Button>();
        button.onClick.AddListener(() => OpenHistory(account(), readOnly));
        Label(rect, "Nhật ký học lại", 23, Color.white);
    }
    void Build()
    {
        var settings = Resources.Load<TextAsset>("PlanetDuelSettings");
        url = settings == null ? "http://localhost:8787" : JsonUtility.FromJson<Settings>(settings.text).serverUrl;
        GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay; GetComponent<Canvas>().sortingOrder = 1500;
        var scaler = GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
        Box(transform, "Shade", 0, 0, 1, 1, new Color(.02f, .06f, .12f, .97f));
        page = Box(transform, "Tutor", .025f, .025f, .975f, .975f, new Color(.035f, .14f, .23f));
        title = Label(Box(page, "Title", .03f, .88f, .68f, .97f, Color.clear), "HỌC LẠI TỪNG BƯỚC", 36, Color.white);
        ButtonAt(page, "Đóng", .85f, .90f, .97f, .97f, () => gameObject.SetActive(false));
        refresh = ButtonAt(page, "Tải lại", .70f, .90f, .83f, .97f, Reload);
        ButtonAt(page, "Nhật ký", .03f, .80f, .18f, .87f, () => OpenHistory(username, teacher));
        status = Label(Box(page, "Status", .20f, .80f, .97f, .87f, Color.clear), "", 23, Color.white);
        var viewport = Box(page, "Viewport", .03f, .22f, .97f, .78f, Paper);
        viewport.gameObject.AddComponent<RectMask2D>();
        scroll = viewport.gameObject.AddComponent<ScrollRect>(); scroll.viewport = viewport; scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped;
        content = Box(viewport, "Content", 0, 1, 1, 1, Color.clear); content.pivot = new Vector2(.5f, 1);
        var layout = content.gameObject.AddComponent<VerticalLayoutGroup>(); layout.padding = new RectOffset(24, 24, 20, 20);
        layout.spacing = 15; layout.childControlHeight = true; layout.childControlWidth = true; layout.childForceExpandHeight = false;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize; scroll.content = content;
        var field = Box(page, "Answer", .03f, .07f, .69f, .20f, Paper); field.gameObject.SetActive(false);
        var area = Box(field, "TextArea", .025f, .06f, .975f, .94f, Color.clear); area.gameObject.AddComponent<RectMask2D>();
        input = field.gameObject.AddComponent<TMP_InputField>(); input.targetGraphic = field.GetComponent<Image>(); input.textViewport = area;
        input.textComponent = Label(area, "", 27, Ink); input.textComponent.alignment = TextAlignmentOptions.TopLeft;
        var placeholder = Label(area, "Viết câu trả lời của em cho bước hiện tại…", 25, Color.gray); placeholder.alignment = TextAlignmentOptions.TopLeft;
        input.placeholder = placeholder; input.characterLimit = 1200; input.richText = false;
        input.lineType = TMP_InputField.LineType.MultiLineNewline; input.readOnly = false; field.gameObject.SetActive(true);
        send = ButtonAt(page, "Trả lời", .71f, .135f, .97f, .20f, () => Answer(input.text));
        unknown = ButtonAt(page, "Em chưa biết", .71f, .07f, .97f, .125f, () => Answer("Em chưa biết"));
        continueButton = ButtonAt(page, "Câu tiếp theo", .32f, .08f, .68f, .18f, ContinueAfterLearning);
        continueButton.gameObject.SetActive(false);
        Label(Box(page, "Footer", .03f, .01f, .97f, .055f, Color.clear), "Điểm bài nộp được giữ nguyên • Lời giải tổng kết mở sau khi hoàn thành các bước", 21, Color.white);
        SceneManager.sceneLoaded += SceneLoaded;
    }
    void ShowLogin(string path, Request body)
    {
        deferredPath = path; deferredBody = body;
        if (loginPanel == null) {
            loginPanel = Box(page, "Journal sign in", 0, 0, 1, .89f, new Color(.035f, .14f, .23f, .99f));
            Label(Box(loginPanel, "Reason", .12f, .65f, .88f, .85f, Color.clear), "Đăng nhập nhật ký học lại\nXác minh tài khoản để bảo vệ quá trình học của em.", 32, Color.white);
            var field = Box(loginPanel, "Password", .25f, .40f, .75f, .51f, Paper); field.gameObject.SetActive(false);
            var area = Box(field, "TextArea", .03f, .08f, .97f, .92f, Color.clear);
            password = field.gameObject.AddComponent<TMP_InputField>(); password.targetGraphic = field.GetComponent<Image>(); password.textViewport = area;
            password.textComponent = Label(area, "", 28, Ink); password.textComponent.alignment = TextAlignmentOptions.MidlineLeft;
            var placeholder = Label(area, "Mật khẩu tài khoản hiện tại", 25, Color.gray); placeholder.alignment = TextAlignmentOptions.MidlineLeft;
            password.placeholder = placeholder; password.contentType = TMP_InputField.ContentType.Password; password.characterLimit = 200;
            password.richText = false;
            password.lineType = TMP_InputField.LineType.SingleLine;
            EssayInputState.Configure(password);
            field.gameObject.SetActive(true);
            ButtonAt(loginPanel, "Đăng nhập", .35f, .26f, .65f, .36f, () => { if (!busy) StartCoroutine(SignIn()); });
            loginStatus = Label(Box(loginPanel, "Login status", .12f, .06f, .88f, .22f, Color.clear), "", 25, Color.white);
        }
        busy = false; SetControls(); loginPanel.gameObject.SetActive(true); loginPanel.SetAsLastSibling();
        password.interactable = true;
        loginStatus.text = "Tài khoản: " + CurrentUsername;
        EssayInputState.Clear(password); password.Select(); password.ActivateInputField();
    }
    IEnumerator SignIn()
    {
        if (string.IsNullOrWhiteSpace(password.text)) { loginStatus.text = "Hãy nhập mật khẩu."; yield break; }
        busy = true; SetControls(); password.interactable = false; loginStatus.text = "Đang xác minh tài khoản…";
        using (var request = new UnityWebRequest(url.TrimEnd('/') + "/tutor/login", "POST")) {
            pending = request;
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(new Request { username = CurrentUsername, password = password.text.Trim() })));
            request.downloadHandler = new DownloadHandlerBuffer(); request.SetRequestHeader("Content-Type", "application/json"); request.timeout = 30;
            yield return request.SendWebRequest();
            Response response = null;
            try { response = JsonUtility.FromJson<Response>(request.downloadHandler.text); } catch (ArgumentException) { }
            pending = null; busy = false; password.interactable = true;
            if (request.result == UnityWebRequest.Result.Success && response != null && !string.IsNullOrEmpty(response.token)) {
                accessToken = response.token; tokenAccount = CurrentUsername;
                teacher = response.teacher; // Role comes from the server, never from the UI toggle.
                if (!teacher) username = CurrentUsername;
                EssayInputState.Clear(password);
                if (!teacher) deferredBody.username = username;
                // A teacher may only inspect state, never start/answer on the student's behalf.
                if (teacher && deferredPath.StartsWith("/review/")) {
                    loginStatus.text = "Giáo viên chỉ xem nhật ký; hãy dùng tài khoản học sinh để sửa bài đối kháng.";
                    SetControls(); yield break;
                }
                loginPanel.gameObject.SetActive(false);
                if (teacher && deferredPath != "list") deferredPath = "state";
                yield return Load(deferredPath, deferredBody);
            } else loginStatus.text = response != null && !string.IsNullOrEmpty(response.error) ? response.error : "Chưa xác minh được tài khoản. Hãy thử lại.";
            SetControls();
        }
    }
    void SceneLoaded(Scene scene, LoadSceneMode mode) { gameObject.SetActive(false); }
    void Clear()
    {
        foreach (Transform child in content) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
    }
    void Paragraph(string value)
    {
        var go = new GameObject("Text", typeof(RectTransform)); go.transform.SetParent(content, false);
        var label = Label((RectTransform)go.transform, value, 26, Ink); label.alignment = TextAlignmentOptions.TopLeft;
        // The layout sizes the actual text element using TMP's preferred height.
        label.transform.SetParent(content, false); Destroy(go);
    }
    void Render(Record record)
    {
        current = record; listMode = false; Clear();
        title.text = teacher ? "NHẬT KÝ HỌC LẠI • " + username : "CÙNG THẦY AI LÀM LẠI";
        status.text = Status(record) + " • Đã qua " + record.step + "/" + record.total + " bước";
        Paragraph(Subject(record.subject) + " • " + record.chapter + "\n" + record.question + "\nĐiểm bài nộp: " + record.score + "/10");
        if (!string.IsNullOrEmpty(record.studentAnswer)) Paragraph("BÀI LÀM / LỰA CHỌN BAN ĐẦU\n" + record.studentAnswer);
        foreach (var turn in record.events ?? new Turn[0]) {
            Paragraph("Bước " + turn.step + (turn.checking ? " • Kiểm tra lại" : "") + " • " + LocalDate(turn.at) + "\n" + turn.prompt
                + "\nEm: " + turn.answer + "\n" + (turn.correct ? "Đúng, em đã hiểu bước này." : turn.answer == "Em chưa biết" ? "GỢI Ý CHO EM\n" + turn.explanation : "Chưa đúng. " + turn.explanation));
        }
        if (record.status == "completed") Paragraph("EM ĐÃ HOÀN THÀNH HỌC LẠI\n\nLỜI GIẢI TỔNG KẾT\n" + record.solution);
        else if (!string.IsNullOrEmpty(record.prompt)) Paragraph("BƯỚC " + (record.step + 1) + "/" + record.total + (record.checking ? " • KIỂM TRA LẠI" : "")
            + (record.checking ? "\n\nGỢI Ý CÁCH LÀM\n" + record.explanation : "") + "\n\nEM THỬ TRẢ LỜI\n" + record.prompt);
        else Paragraph("Học sinh chưa bắt đầu học lại câu này.");
        SetControls(); StartCoroutine(ScrollTo(0));
    }
    static string Subject(string subject) => subject == "Biology" ? "Sinh học" : subject == "Chemistry" ? "Hóa học" : "Vật lý";
    static string Status(Record r) => r.status == "completed" ? "Đã hoàn thành" : r.status == "learning" ? "Đang học lại" : "Chưa học lại";
    void RenderList(Record[] records)
    {
        listMode = true; Clear(); title.text = "NHẬT KÝ HỌC LẠI • " + username;
        status.text = teacher ? "Chọn mục để xem các bước và từng lần trả lời của học sinh." : "Chọn câu để tiếp tục đúng bước đã lưu.";
        if (records == null || records.Length == 0) Paragraph("Chưa có câu cần học lại trên máy chủ này.");
        foreach (var item in records ?? new Record[0]) {
            var r = item;
            Paragraph(Subject(r.subject) + " • " + r.chapter + " • " + Status(r) + " • " + r.step + "/" + r.total + " bước\n"
                + r.question + "\nĐiểm ban đầu: " + r.score + "/10 • Cập nhật: " + LocalDate(r.updatedAt));
            var button = ButtonAt(content, teacher ? "Xem quá trình" : r.status == "completed" ? "Xem tổng kết" : "Học lại / Tiếp tục", 0, 0, 1, 1,
                () => { if (!busy) { retryAnswer = null; StartCoroutine(Load(teacher ? "state" : "start", new Request { id = r.id, username = username })); } });
            var element = button.gameObject.AddComponent<LayoutElement>(); element.preferredHeight = 58;
        }
        SetControls(); StartCoroutine(ScrollTo(1));
    }
    static string LocalDate(string value) => DateTime.TryParse(value, out var date) ? date.ToLocalTime().ToString("dd/MM/yyyy HH:mm") : value;
    IEnumerator ScrollTo(float position) { yield return null; Canvas.ForceUpdateCanvases(); LayoutRebuilder.ForceRebuildLayoutImmediate(content); scroll.verticalNormalizedPosition = position; }
    void SetControls()
    {
        bool editable = !busy && !teacher && !listMode && current != null && current.status == "learning";
        input.gameObject.SetActive(!teacher && !listMode && current != null && current.status == "learning");
        // Clear() and editor reloads can leave readOnly set even when interactable
        // is restored. Learning state owns both flags, independently of focus callbacks.
        input.readOnly = !editable;
        input.interactable = editable; send.gameObject.SetActive(input.gameObject.activeSelf); unknown.gameObject.SetActive(input.gameObject.activeSelf);
        send.interactable = unknown.interactable = editable; refresh.interactable = !busy;
        bool completed = !teacher && !listMode && current != null && current.status == "completed";
        continueButton.gameObject.SetActive(completed);
        if (completed) {
            var practice = FindPracticeForCurrentLesson();
            var duel = FindDuelForCurrentLesson();
            continueButton.GetComponentInChildren<TMP_Text>().text = practice != null ? practice.TutorialContinuationLabel : duel != null ? "Về các câu sau trận" : "Về nhật ký";
            continueButton.interactable = !busy && (practice == null || practice.CanContinueTutorial(current.id));
        }
    }
    PlanetDuelController FindDuelForCurrentLesson()
    {
        if (current == null) return null;
        foreach (var duel in FindObjectsByType<PlanetDuelController>(FindObjectsInactive.Exclude))
            if (duel.MatchesTutorial(current.id)) return duel;
        return null;
    }
    FootballEssayController FindPracticeForCurrentLesson()
    {
        if (current == null) return null;
        foreach (var practice in FindObjectsByType<FootballEssayController>(FindObjectsInactive.Exclude))
            if (practice.MatchesTutorial(current.id)) return practice;
        return null;
    }
    void ContinueAfterLearning()
    {
        if (busy || teacher || listMode || current == null || current.status != "completed" || !gameObject.activeInHierarchy) return;
        var practice = FindPracticeForCurrentLesson();
        if (practice == null) {
            var duel = FindDuelForCurrentLesson();
            if (duel != null) { gameObject.SetActive(false); duel.ContinueTutorial(current.id); }
            else OpenHistory(username, false);
            return;
        }
        if (practice.ContinueTutorial(current.id)) gameObject.SetActive(false);
    }
    void Reload()
    {
        if (busy) return;
        if (planetReviewLoaded != null && deferredPath != null && deferredPath.StartsWith("/review/")) StartCoroutine(Load(deferredPath, deferredBody));
        else if (listMode || current == null) StartCoroutine(Load("list", new Request { username = username }));
        else StartCoroutine(Load(teacher ? "state" : "start", new Request { id = current.id, username = username }));
    }
    void Answer(string answer)
    {
        if (busy || teacher || current == null || current.status != "learning" || string.IsNullOrWhiteSpace(answer)) return;
        if (username != CurrentUsername) { gameObject.SetActive(false); return; }
        if (retryAnswer == null || retryAnswer.answer != answer.Trim() || retryAnswer.revision != current.revision)
            retryAnswer = new Request { id = current.id, username = username, answer = answer.Trim(), revision = current.revision, requestId = Guid.NewGuid().ToString("N") };
        StartCoroutine(Load("answer", retryAnswer));
    }
    IEnumerator Load(string path, Request body)
    {
        deferredPath = path; deferredBody = body;
        bool planet = path.StartsWith("/review/");
        if (planet) {
            busy = true; SetControls(); status.text = "Đang kiểm tra máy chủ học lại…";
            using (var health = UnityWebRequest.Get(url.TrimEnd('/') + "/health")) {
                pending = health; health.timeout = 45;
                yield return health.SendWebRequest();
                Response capability = null;
                try { capability = JsonUtility.FromJson<Response>(health.downloadHandler.text); } catch (ArgumentException) { }
                pending = null; busy = false;
                if (health.result != UnityWebRequest.Result.Success || capability == null) {
                    ShowReviewServiceError("Chưa kết nối được máy chủ học lại. Bấm Tải lại để thử lại; bài cần sửa vẫn được giữ.");
                    yield break;
                }
                if (capability.duelReviewVersion < 1) {
                    ShowReviewServiceError(ReviewServerUpdateMessage);
                    yield break;
                }
            }
        }
        if (string.IsNullOrEmpty(accessToken) || tokenAccount != CurrentUsername) { ShowLogin(path, body); yield break; }
        if (loginPanel != null) loginPanel.gameObject.SetActive(false);
        busy = true; SetControls(); status.text = "Đang tải và lưu tiến trình…";
        using (var request = new UnityWebRequest(url.TrimEnd('/') + (path.StartsWith("/") ? path : "/tutor/" + path), "POST")) {
            pending = request; request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(body)));
            request.downloadHandler = new DownloadHandlerBuffer(); request.SetRequestHeader("Content-Type", "application/json"); request.timeout = 60;
            request.SetRequestHeader("Authorization", "Bearer " + accessToken);
            yield return request.SendWebRequest();
            Response response = null;
            try { response = JsonUtility.FromJson<Response>(request.downloadHandler.text); } catch (ArgumentException) { }
            pending = null; busy = false;
            if (request.responseCode == 401) {
                // Old Arena servers treat unknown /review routes as expired match
                // sessions. Re-entering the correct password cannot repair that.
                if (planet && response != null && (response.error ?? "").Contains("Phiên đã hết hạn")) {
                    ShowReviewServiceError(ReviewServerUpdateMessage); yield break;
                }
                accessToken = null; ShowLogin(path, body); yield break;
            }
            if (request.result == UnityWebRequest.Result.Success && response != null && (planet ? response.reviewQuestions != null : path == "list" ? response.records != null : !string.IsNullOrEmpty(response.id))) {
                if (planet) {
                    var done = planetReviewLoaded; planetReviewLoaded = null;
                    gameObject.SetActive(false); done?.Invoke(response.reviewQuestions, response.reviewPending);
                }
                else if (path == "list") RenderList(response.records);
                else { retryAnswer = null; EssayInputState.Clear(input); Render(response); }
            } else {
                status.text = response != null && !string.IsNullOrEmpty(response.error) ? response.error : "Chưa kết nối được máy chủ học lại. Bấm Tải lại hoặc gửi lại câu trả lời.";
                SetControls();
            }
        }
    }
    const string ReviewServerUpdateMessage = "Máy chủ chưa hỗ trợ xem câu sai đối kháng. Đây là lỗi phiên bản máy chủ, không phải mật khẩu. Cần cập nhật PlanetServer có duelReviewVersion=1, rồi bấm Tải lại. Bài cần sửa vẫn được giữ.";
    void ShowReviewServiceError(string message)
    {
        if (loginPanel != null) loginPanel.gameObject.SetActive(false);
        busy = false; Clear(); status.text = "Chưa mở được phần học lại đối kháng";
        Paragraph(message); SetControls();
    }
    void Cancel() { StopAllCoroutines(); pending?.Abort(); pending?.Dispose(); pending = null; busy = false; if (password != null) { password.interactable = true; password.text = ""; } }
    void OnDisable() { Cancel(); }
    void OnDestroy() { Cancel(); SceneManager.sceneLoaded -= SceneLoaded; if (instance == this) instance = null; }

    static RectTransform Box(Transform parent, string name, float left, float bottom, float right, float top, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform; rect.anchorMin = new Vector2(left, bottom); rect.anchorMax = new Vector2(right, top); rect.offsetMin = rect.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = color; go.GetComponent<Image>().raycastTarget = color.a > 0; return rect;
    }
    static TMP_Text Label(RectTransform parent, string value, int size, Color color)
    {
        var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        var label = go.GetComponent<TextMeshProUGUI>();
        if (font == null) { var source = Resources.Load<Font>("ScienceChatFont"); if (source != null) { font = TMP_FontAsset.CreateFontAsset(source); font.isMultiAtlasTexturesEnabled = true; } }
        if (font != null) label.font = font;
        label.text = value; label.fontSize = size; label.color = color; label.richText = false; label.raycastTarget = false;
        label.alignment = TextAlignmentOptions.Center; return label;
    }
    static Button ButtonAt(Transform parent, string label, float left, float bottom, float right, float top, UnityEngine.Events.UnityAction action)
    {
        var rect = Box(parent, label, left, bottom, right, top, new Color(.08f, .36f, .53f));
        var button = rect.gameObject.AddComponent<Button>(); button.onClick.AddListener(action); Label(rect, label, 25, Color.white); return button;
    }
}
