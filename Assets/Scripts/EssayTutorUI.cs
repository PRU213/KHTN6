using System;
using System.Collections;
using System.Collections.Generic;
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
        public int finalScore; public Penalty penalty;
        public string sourceId, blockedBy;
    }
    [Serializable] public class Penalty { public string status, kind, content, feedback, createdAt, completedAt, redoQuestion, redoSolution; public int required, count; public bool redoPassed; }
    [Serializable] public class CopyImage { public string base64, mimeType; }
    [Serializable] class CopyDraft { public string id, username; public CopyImage[] images; }
    [Serializable] public class QuizMistake { public string id; public int chosen; }
    [Serializable] public class QuizBatch { public string id, game, subject; public List<QuizMistake> questions = new List<QuizMistake>(); }
    [Serializable] class QuizJournal { public List<QuizBatch> batches = new List<QuizBatch>(); }
    [Serializable] class Request {
        public string id, username, answer, requestId, password, matchId; public int revision;
        public PlanetDuelController.ReviewQuestion[] reviewQuestions;
        public CopyImage[] images;
        public QuizBatch[] quizBatches;
    }
    [Serializable] class Response : Record {
        public string error, token; public bool teacher; public Record[] records;
        public PlanetDuelController.ReviewQuestion[] reviewQuestions; public int reviewPending;
        public int duelReviewVersion, copyPenaltyVersion;
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
    Action playAllowed;
    static int approvedLaunchDepth;
    RectTransform playGate;
    TMP_Text playGateMessage;
    string playAccount, copyRecordId;
    readonly List<CopyImage> copyImages = new List<CopyImage>();
    readonly List<Texture2D> copyTextures = new List<Texture2D>();
    WebCamTexture copyCamera;
    System.Threading.CancellationTokenSource copyPicker;
    Request retryCopy;
    static readonly Color Ink = new Color(.08f, .17f, .28f);
    static readonly Color Paper = new Color(.96f, .98f, 1);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void InitializeAuthentication()
    {
        approvedLaunchDepth = 0;
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
        ui.playAllowed = null;
        ui.current = new Record { id = id }; ui.listMode = false; ui.Clear(); ui.retryAnswer = null; ui.gameObject.SetActive(true);
        ui.StartCoroutine(ui.Load("start", new Request { id = id, username = ui.username }));
    }
    public static void OpenHistory(string account, bool readOnly = false)
    {
        var ui = Get(); ui.Cancel(); ui.username = account; ui.teacher = readOnly; ui.current = null; ui.listMode = true; ui.Clear(); ui.retryAnswer = null;
        ui.planetReviewLoaded = null;
        ui.playAllowed = null;
        ui.gameObject.SetActive(true); ui.StartCoroutine(ui.Load("list", new Request { username = account }));
    }
    public static void CheckBeforePlay(Action allowed)
    {
        // Activating the game invokes its OnEnable synchronously. Reuse this approval
        // only within that activation; replay buttons must still perform a fresh check.
        if (approvedLaunchDepth > 0) { allowed?.Invoke(); return; }
        var ui = Get();
        if (ui.busy || ui.playAllowed != null) return;
        ui.BeginPlayCheck(allowed);
        ui.LoadHistoryOrQuiz();
    }
    void BeginPlayCheck(Action allowed)
    {
        var ui = this;
        ui.Cancel(); ui.teacher = false; ui.username = CurrentUsername; ui.playAccount = ui.username;
        ui.playAllowed = allowed; ui.planetReviewLoaded = null; ui.current = null; ui.listMode = true;
        ui.Clear(); ui.gameObject.SetActive(true);
        SetPlayGateVisible(true);
        playGateMessage.text = "Đang kiểm tra điều kiện bắt đầu trò chơi…";
    }
    void SetPlayGateVisible(bool visible)
    {
        page.gameObject.SetActive(!visible);
        if (playGate == null) {
            playGate = Box(transform, "Game entry check", .20f, .24f, .80f, .76f, new Color(.035f, .14f, .23f));
            Label(Box(playGate, "Heading", .05f, .78f, .95f, .96f, Color.clear), "MỞ TRÒ CHƠI", 34, Color.white);
            playGateMessage = Label(Box(playGate, "Message", .07f, .25f, .93f, .76f, Color.clear), "", 26, Color.white);
            ButtonAt(playGate, "Thử lại", .12f, .05f, .46f, .20f, Reload);
            ButtonAt(playGate, "Quay lại", .54f, .05f, .88f, .20f, () => gameObject.SetActive(false));
        }
        playGate.gameObject.SetActive(visible);
        if (visible) playGate.SetAsLastSibling();
    }
    void CompletePlayCheck(Record[] records)
    {
        bool blocked = Array.Exists(records, r => r.penalty != null && r.penalty.status != "completed");
        var allowed = playAllowed; playAllowed = null;
        if (!blocked && playAccount == CurrentUsername) {
            gameObject.SetActive(false);
            approvedLaunchDepth++;
            try { allowed?.Invoke(); } finally { approvedLaunchDepth--; }
        } else {
            SetPlayGateVisible(false); RenderList(records);
            if (blocked) status.text = "Tài khoản còn nhiệm vụ chưa hoàn thành. Xử lý bài cũ trước khi mở lượt mới.";
        }
    }
    static string QuizKey => "Quiz.PendingReviews." + CurrentUsername.Trim().ToLowerInvariant();
    static QuizJournal ReadQuizJournal()
    {
        var json = PlayerPrefs.GetString(QuizKey, "");
        return string.IsNullOrEmpty(json) ? new QuizJournal() : JsonUtility.FromJson<QuizJournal>(json);
    }
    public static void RememberQuizMistake(string batchId, string game, QuestionData question, int chosen)
    {
        var journal = ReadQuizJournal(); var batch = journal.batches.Find(b => b.id == batchId);
        if (batch == null) { batch = new QuizBatch { id = batchId, game = game, subject = GameSession.Subject }; journal.batches.Add(batch); }
        if (!batch.questions.Exists(q => q.id == question.id)) batch.questions.Add(new QuizMistake { id = question.id, chosen = chosen });
        PlayerPrefs.SetString(QuizKey, JsonUtility.ToJson(journal)); PlayerPrefs.Save();
    }
    public static void FinishQuizReviews()
    {
        if (ReadQuizJournal().batches.Count == 0) return;
        var ui = Get(); ui.Cancel(); ui.playAllowed = null; ui.teacher = false; ui.username = CurrentUsername;
        ui.current = null; ui.listMode = true; ui.planetReviewLoaded = null; ui.Clear(); ui.gameObject.SetActive(true); ui.LoadHistoryOrQuiz();
    }
    void LoadHistoryOrQuiz()
    {
        var batches = ReadQuizJournal().batches;
        StartCoroutine(Load(batches.Count == 0 ? "list" : "quiz-review", new Request { username = username, quizBatches = batches.ToArray() }));
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
        Label(Box(page, "Footer", .03f, .01f, .97f, .055f, Color.clear), "Điểm bài nộp giữ nguyên • Hoàn thành học lại và chép phạt (nếu có) mới được chuyển câu", 21, Color.white);
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
        loginPanel.SetParent(playAllowed != null ? playGate : page, false);
        var reason = loginPanel.Find("Reason").GetComponentInChildren<TMP_Text>(true);
        reason.text = playAllowed != null ? "Xác minh tài khoản trước khi chơi\nKiểm tra bài còn cần hoàn thành của em." : "Đăng nhập nhật ký học lại\nXác minh tài khoản để bảo vệ quá trình học của em.";
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
        SetPlayGateVisible(false);
        CloseCopyCameraPanel();
        current = record; listMode = false; Clear();
        title.text = teacher ? "NHẬT KÝ HỌC LẠI • " + username : "CÙNG THẦY AI LÀM LẠI";
        status.text = Status(record) + " • Đã qua " + record.step + "/" + record.total + " bước";
        Paragraph(Subject(record.subject) + " • " + record.chapter + "\n" + record.question + "\nĐiểm kiến thức: " + record.score + "/10 • Sau trừ gợi ý: " + record.finalScore + "/10");
        if (!string.IsNullOrEmpty(record.studentAnswer)) Paragraph("BÀI LÀM / LỰA CHỌN BAN ĐẦU\n" + record.studentAnswer);
        foreach (var turn in record.events ?? new Turn[0]) {
            Paragraph("Bước " + turn.step + (turn.checking ? " • Kiểm tra lại" : "") + " • " + LocalDate(turn.at) + "\n" + turn.prompt
                + "\nEm: " + turn.answer + "\n" + (turn.correct ? "Đúng, em đã hiểu bước này." : turn.answer == "Em chưa biết" ? "GỢI Ý CHO EM\n" + turn.explanation : "Chưa đúng. " + turn.explanation));
        }
        if (record.status == "completed") Paragraph("EM ĐÃ HOÀN THÀNH HỌC LẠI\n\nLỜI GIẢI TỔNG KẾT\n" + record.solution);
        else if (!string.IsNullOrEmpty(record.prompt)) Paragraph("BƯỚC " + (record.step + 1) + "/" + record.total + (record.checking ? " • KIỂM TRA LẠI" : "")
            + (record.checking ? "\n\nGỢI Ý CÁCH LÀM\n" + record.explanation : "") + "\n\nEM THỬ TRẢ LỜI\n" + record.prompt);
        else Paragraph("Học sinh chưa bắt đầu học lại câu này.");
        RenderCopy(record);
        SetControls(); StartCoroutine(ScrollTo(0));
    }
    static string Subject(string subject) => subject == "Biology" ? "Sinh học" : subject == "Chemistry" ? "Hóa học" : "Vật lý";
    static string Status(Record r) => r.penalty != null && r.penalty.status == "completed" ? "Đã chép phạt xong"
        : r.penalty != null && r.penalty.status == "deferred" && r.status == "completed" ? "Đã học lại • Cần chép phạt trước khi chuyển câu"
        : r.penalty != null ? (r.status == "completed" ? "Đang chép phạt" : "Đang học lại • Có nhiệm vụ chép phạt")
        : r.status == "completed" ? "Đã hoàn thành" : r.status == "learning" ? "Đang học lại" : "Chưa học lại";

    void CopyButton(string label, Action action)
    {
        var button = ButtonAt(content, label, 0, 0, 1, 1, () => { if (!busy && !teacher) action(); });
        button.gameObject.AddComponent<LayoutElement>().preferredHeight = 62;
    }
    void RenderCopy(Record record)
    {
        var p = record.penalty;
        if (p == null) return;
        Paragraph("CHÉP PHẠT • " + Status(record) + "\nHoàn thành cả chép tay và làm lại đầy đủ trước khi chuyển câu.\n"
            + (p.required > 0 ? "Đã đạt " + p.count + "/" + p.required + " bản chép đầy đủ.\nMẪU PHẢI CHÉP TAY:\n" + p.content : "Nội dung chép sẽ mở ngay sau khi học lại xong câu này.")
            + (p.required > 0 ? "\nPHẦN RIÊNG: Chép lại đề và lời giải hoàn chỉnh 1 lần (ngoài " + p.required + " lần chép ở trên).\nĐỀ: " + p.redoQuestion + "\nLỜI GIẢI: " + p.redoSolution + "\nPhần làm lại: " + (p.redoPassed ? "Đạt" : "Chưa đạt") : "")
            + "\n" + p.feedback + (string.IsNullOrEmpty(p.completedAt) ? "" : "\nHoàn thành: " + LocalDate(p.completedAt)));
        if (p.status == "completed") { DeleteCopyDraft(record.id); return; }
        if (teacher || record.status != "completed") return;
        if (string.IsNullOrEmpty(p.content)) {
            CopyButton("Mở nội dung chép phạt", () => StartCoroutine(Load("copy-start", new Request { id = record.id, username = username })));
            return;
        }
        if (copyRecordId != record.id) { ClearCopyImages(); copyRecordId = record.id; RestoreCopyDraft(record.id); }
        Paragraph("Viết tay rồi nộp toàn bộ các trang cùng một lần (tối đa 6 ảnh). Khi nộp bổ sung, gửi lại đủ các trang; hệ thống không cộng giữa các lần nộp.\nĐã chọn " + copyImages.Count + " ảnh.");
        foreach (var texture in copyTextures) {
            var holder = new GameObject("Trang chép tay", typeof(RectTransform), typeof(LayoutElement)); holder.transform.SetParent(content, false);
            holder.GetComponent<LayoutElement>().preferredHeight = 350;
            var go = new GameObject("Ảnh chép tay", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter)); go.transform.SetParent(holder.transform, false);
            go.GetComponent<RawImage>().texture = texture;
            var ratio = go.GetComponent<AspectRatioFitter>(); ratio.aspectMode = AspectRatioFitter.AspectMode.FitInParent; ratio.aspectRatio = (float)texture.width / texture.height;
        }
        CopyButton("Thêm ảnh từ máy", () => StartCoroutine(PickCopyImage()));
        CopyButton("Chụp ảnh bằng camera", () => StartCoroutine(OpenCopyCamera()));
        CopyButton("Xóa các ảnh đã chọn", () => { DeleteCopyDraft(current.id); ClearCopyImages(); Render(current); });
        CopyButton("Gửi toàn bộ ảnh để AI kiểm tra", () => {
            if (copyImages.Count == 0) { status.text = "Hãy chọn hoặc chụp ảnh bài chép tay."; return; }
            if (retryCopy == null) retryCopy = new Request { id = current.id, username = username, requestId = Guid.NewGuid().ToString("N"), images = copyImages.ToArray() };
            StartCoroutine(Load("copy-submit", retryCopy));
        });
    }
    void ClearCopyImages()
    {
        foreach (var texture in copyTextures) if (texture != null) Destroy(texture);
        copyTextures.Clear(); copyImages.Clear(); retryCopy = null;
    }
    string CopyDraftPath(string id)
    {
        using (var sha = System.Security.Cryptography.SHA256.Create()) {
            var key = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(username.Trim().ToLowerInvariant() + "|" + id))).Replace("-", "");
            return System.IO.Path.Combine(Application.persistentDataPath, "CopyDrafts", key + ".json");
        }
    }
    void SaveCopyDraft()
    {
        try {
            var path = CopyDraftPath(current.id); System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            System.IO.File.WriteAllText(path + ".tmp", JsonUtility.ToJson(new CopyDraft { id = current.id, username = username, images = copyImages.ToArray() }));
            if (System.IO.File.Exists(path)) System.IO.File.Replace(path + ".tmp", path, null); else System.IO.File.Move(path + ".tmp", path);
        } catch (Exception) { Debug.LogWarning("Chưa lưu được bản nháp ảnh chép phạt trên thiết bị; giữ ảnh gốc trước khi thoát."); }
    }
    void RestoreCopyDraft(string id)
    {
        try {
            var path = CopyDraftPath(id); if (!System.IO.File.Exists(path)) return;
            var draft = JsonUtility.FromJson<CopyDraft>(System.IO.File.ReadAllText(path));
            if (draft == null || draft.id != id || !string.Equals(draft.username, username, StringComparison.OrdinalIgnoreCase) || draft.images == null || draft.images.Length > 6) return;
            foreach (var photo in draft.images) {
                var bytes = Convert.FromBase64String(photo.base64); if (bytes.Length > 2 * 1024 * 1024) throw new FormatException();
                var texture = new Texture2D(2, 2);
                if (!texture.LoadImage(bytes)) { Destroy(texture); throw new FormatException(); }
                copyImages.Add(photo); copyTextures.Add(texture);
            }
        } catch (Exception) { ClearCopyImages(); Debug.LogWarning("Chưa đọc được bản nháp chép phạt. Có thể chọn lại ảnh gốc."); }
    }
    void DeleteCopyDraft(string id)
    {
        if (teacher) return;
        try { var path = CopyDraftPath(id); if (System.IO.File.Exists(path)) System.IO.File.Delete(path); }
        catch (Exception) { Debug.LogWarning("Chưa xóa được bản nháp ảnh đã hoàn thành."); }
    }
    void AddCopyImage(byte[] bytes)
    {
        if (copyImages.Count >= 6) { status.text = "Tối đa 6 ảnh trong một bài nộp."; return; }
        if (bytes.Length > 2 * 1024 * 1024) { status.text = "Mỗi ảnh tối đa 2 MB."; return; }
        var texture = new Texture2D(2, 2);
        if (!texture.LoadImage(bytes)) { Destroy(texture); status.text = "Không đọc được ảnh."; return; }
        copyTextures.Add(texture); copyImages.Add(new CopyImage { base64 = Convert.ToBase64String(bytes), mimeType = "image/jpeg" }); retryCopy = null;
        SaveCopyDraft();
        Render(current);
    }
    IEnumerator PickCopyImage()
    {
        if (copyPicker != null || busy) yield break;
        var recordId = current.id;
        var cancellation = new System.Threading.CancellationTokenSource(); copyPicker = cancellation;
        var task = PlanetEssayFileDialog.OpenAsync("", cancellation.Token);
        while (!task.IsCompleted) yield return null;
        if (copyPicker != cancellation) { cancellation.Dispose(); yield break; }
        copyPicker = null; cancellation.Dispose();
        if (task.IsFaulted) { status.text = "Không mở được ảnh. Hãy thử lại."; yield break; }
        if (!task.IsCanceled && task.Result != null && current != null && current.id == recordId && !teacher) AddCopyImage(task.Result.Bytes);
    }
    IEnumerator OpenCopyCamera()
    {
        if (copyCamera != null || busy) yield break;
        yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
        if (!Application.HasUserAuthorization(UserAuthorization.WebCam) || WebCamTexture.devices.Length == 0) { status.text = "Chưa truy cập được camera."; yield break; }
        copyCamera = new WebCamTexture(WebCamTexture.devices[0].name, 1600, 1200); copyCamera.Play();
        var panel = Box(page, "Camera chép tay", .05f, .22f, .95f, .78f, Color.black);
        var cameraView = new GameObject("Preview", typeof(RectTransform), typeof(RawImage)); cameraView.transform.SetParent(panel, false);
        var preview = cameraView.GetComponent<RawImage>(); preview.texture = copyCamera;
        preview.rectTransform.anchorMin = new Vector2(.02f, .18f); preview.rectTransform.anchorMax = new Vector2(.98f, .98f);
        preview.rectTransform.offsetMin = preview.rectTransform.offsetMax = Vector2.zero;
        ButtonAt(panel, "Chụp", .1f, .02f, .48f, .16f, () => {
            if (copyCamera == null || !copyCamera.didUpdateThisFrame || copyCamera.width < 32) return;
            var image = new Texture2D(copyCamera.width, copyCamera.height); image.SetPixels32(copyCamera.GetPixels32()); image.Apply();
            var bytes = image.EncodeToJPG(90); Destroy(image); StopCopyCamera(); Destroy(panel.gameObject); AddCopyImage(bytes);
        });
        ButtonAt(panel, "Hủy", .52f, .02f, .9f, .16f, () => { StopCopyCamera(); Destroy(panel.gameObject); });
        while (copyCamera != null && panel != null) {
            preview.rectTransform.localEulerAngles = new Vector3(0, 0, -copyCamera.videoRotationAngle);
            preview.uvRect = copyCamera.videoVerticallyMirrored ? new Rect(0, 1, 1, -1) : new Rect(0, 0, 1, 1);
            yield return null;
        }
    }
    void StopCopyCamera() { if (copyCamera != null) { copyCamera.Stop(); Destroy(copyCamera); copyCamera = null; } }
    void CloseCopyCameraPanel() { StopCopyCamera(); var panel = page != null ? page.Find("Camera chép tay") : null; if (panel != null) { panel.gameObject.SetActive(false); Destroy(panel.gameObject); } }
    void ReleaseCopyControls() { playAllowed = null; CloseCopyCameraPanel(); copyPicker?.Cancel(); copyPicker = null; }
    void RenderList(Record[] records)
    {
        SetPlayGateVisible(false);
        CloseCopyCameraPanel();
        listMode = true; Clear(); title.text = "NHẬT KÝ HỌC LẠI • " + username;
        status.text = teacher ? "Chọn mục để xem các bước và từng lần trả lời của học sinh." : "Chọn câu để tiếp tục đúng bước đã lưu.";
        if (records == null || records.Length == 0) Paragraph("Chưa có câu cần học lại trên máy chủ này.");
        foreach (var item in records ?? new Record[0]) {
            var r = item;
            Paragraph(Subject(r.subject) + " • " + r.chapter + " • " + Status(r) + " • " + r.step + "/" + r.total + " bước\n"
                + r.question + "\nĐiểm ban đầu: " + r.score + "/10 • Cập nhật: " + LocalDate(r.updatedAt));
            var button = ButtonAt(content, teacher ? "Xem quá trình" : r.status == "completed" ? "Xem tổng kết" : "Học lại / Tiếp tục", 0, 0, 1, 1,
                () => { if (!busy) { retryAnswer = null; StartCoroutine(Load(teacher ? "state" : "start", new Request { id = r.id, username = username })); } });
            if (!teacher && !string.IsNullOrEmpty(r.blockedBy)) {
                button.interactable = false; button.GetComponentInChildren<TMP_Text>().text = "Hoàn thành học lại và chép phạt câu trước";
            }
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
        bool completed = !teacher && !listMode && current != null && current.status == "completed" && (current.penalty == null || current.penalty.status == "completed");
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
        if (busy || teacher || listMode || current == null || current.status != "completed" || (current.penalty != null && current.penalty.status != "completed") || !gameObject.activeInHierarchy) return;
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
        else if (listMode || current == null) {
            if (teacher) StartCoroutine(Load("list", new Request { username = username })); else LoadHistoryOrQuiz();
        }
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
        if (!teacher && !string.Equals(body.username, CurrentUsername, StringComparison.OrdinalIgnoreCase)) { gameObject.SetActive(false); yield break; }
        deferredPath = path; deferredBody = body;
        bool planet = path.StartsWith("/review/");
        if (planet || playAllowed != null || path == "quiz-review" || path.StartsWith("copy-")) {
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
                if (planet && capability.duelReviewVersion < 1) {
                    ShowReviewServiceError(ReviewServerUpdateMessage);
                    yield break;
                }
                if (!planet && capability.copyPenaltyVersion < 2) {
                    ShowReviewServiceError("Cần cập nhật PlanetServer hỗ trợ chép phạt trước khi bắt đầu lượt mới. Bấm Tải lại sau khi máy chủ đã cập nhật.");
                    yield break;
                }
            }
        }
        if (string.IsNullOrEmpty(accessToken) || tokenAccount != CurrentUsername) { ShowLogin(path, body); yield break; }
        if (loginPanel != null) loginPanel.gameObject.SetActive(false);
        busy = true; SetControls(); status.text = "Đang tải và lưu tiến trình…";
        if (playAllowed != null) playGateMessage.text = "Đang kiểm tra bài còn cần hoàn thành trước khi vào trò chơi…";
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
            if (request.result == UnityWebRequest.Result.Success && response != null && (planet ? response.reviewQuestions != null : path == "list" || path == "quiz-review" ? response.records != null : !string.IsNullOrEmpty(response.id))) {
                if (planet) {
                    var done = planetReviewLoaded; planetReviewLoaded = null;
                    gameObject.SetActive(false); done?.Invoke(response.reviewQuestions, response.reviewPending);
                }
                else if (path == "list" || path == "quiz-review") {
                    if (path == "quiz-review") { PlayerPrefs.DeleteKey(QuizKey); PlayerPrefs.Save(); }
                    if (playAllowed != null) CompletePlayCheck(response.records);
                    else RenderList(response.records);
                }
                else { retryAnswer = null; if (path == "copy-submit") retryCopy = null; EssayInputState.Clear(input); Render(response); }
            } else {
                status.text = response != null && !string.IsNullOrEmpty(response.error) ? response.error : "Chưa kết nối được máy chủ học lại. Bấm Tải lại hoặc gửi lại câu trả lời.";
                if (playAllowed != null) playGateMessage.text = status.text;
                SetControls();
            }
        }
    }
    const string ReviewServerUpdateMessage = "Máy chủ chưa hỗ trợ xem câu sai đối kháng. Đây là lỗi phiên bản máy chủ, không phải mật khẩu. Cần cập nhật PlanetServer có duelReviewVersion=1, rồi bấm Tải lại. Bài cần sửa vẫn được giữ.";
    void ShowReviewServiceError(string message)
    {
        if (loginPanel != null) loginPanel.gameObject.SetActive(false);
        if (playAllowed != null) {
            busy = false; playGateMessage.text = message; return;
        }
        busy = false; Clear(); status.text = "Chưa mở được phần học lại đối kháng";
        Paragraph(message); SetControls();
    }
    void Cancel() { StopAllCoroutines(); pending?.Abort(); pending?.Dispose(); pending = null; busy = false; if (password != null) { password.interactable = true; password.text = ""; } }
    void OnDisable() { Cancel(); ReleaseCopyControls(); if (playGate != null) playGate.gameObject.SetActive(false); if (page != null) page.gameObject.SetActive(true); }
    void OnDestroy() { Cancel(); ReleaseCopyControls(); ClearCopyImages(); SceneManager.sceneLoaded -= SceneLoaded; if (instance == this) instance = null; }

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
