using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

/// <summary>Shared Football/Sheep/Cave practice. Server owns questions, deadlines, hints and grades.</summary>
public class FootballEssayController : MonoBehaviour
{
    [Serializable] class Settings { public string serverUrl = "http://localhost:8787"; }
    [Serializable] class Command {
        public string token, subject, chapter, username, name, text, imageBase64, mimeType, game;
        public string[] difficulties; public int round, level;
    }
    [Serializable] class State {
        public string token, state, error, text, subject, chapter, difficulty, feedback, transcription, sampleAnswer, suggestedAnswer;
        public string[] hints; public int round, total, score, maxScore, earned, rawScore, hintPenalty; public float remaining, duration; public bool hintPending, paused;
    }
    TMP_FontAsset fallbackFont;
    bool sheepSkin, caveSkin;
    bool ParchmentSkin => sheepSkin || caveSkin;
    string practiceGame = "Football";
    string practiceUsername;
    bool historyQueued;
    GameSelectScreen selector;
    GameObject hostPanel, overlay;
    RectTransform page, resultPanel, hintPanel, photoPanel, cameraPanel, errorPanel;
    TMP_Text question, scoreboard, context, status, resultText, hintText, errorText, cameraText, photoHint;
    TMP_InputField input;
    RawImage preview, cameraPreview;
    Button submit, hintButton, next, retry, hintNext, capture;
    ScrollRect results;
    Texture2D attached;
    WebCamTexture webcam;
    string imageBase64, token, url, notice;
    State latest;
    bool commandBusy, hintBusy, picking;
    int generation, revision, round = -1, cameraVersion;
    float receivedAt;
    RectTransform exitConfirmation;
    Button stayButton;
    bool exitPending;
    CancellationTokenSource pickerCancellation;
    readonly HashSet<UnityWebRequest> requests = new HashSet<UnityWebRequest>();
    Color Ink => ParchmentSkin ? new Color(.25f, .12f, .035f) : new Color(.035f, .07f, .38f);

    public void Open(FootballQuizController source)
    {
        OpenPractice(source, source.questionText, "Football");
    }
    public void Open(SheepQuizController source)
    {
        OpenPractice(source, source.questionText, "Sheep");
    }
    public void Open(CaveQuizController source)
    {
        OpenPractice(source, source.questionText, "Cave");
    }
    void OpenPractice(MonoBehaviour source, TMP_Text sourceQuestion, string selectedGame)
    {
        practiceGame = selectedGame; sheepSkin = selectedGame == "Sheep"; caveSkin = selectedGame == "Cave";
        fallbackFont = sourceQuestion != null ? sourceQuestion.font : TMP_Settings.defaultFontAsset;
        hostPanel = source.gameObject;
        foreach (var candidate in FindObjectsByType<GameSelectScreen>(FindObjectsInactive.Include)) {
            if (candidate.games == null) continue;
            foreach (var entry in candidate.games) if (entry.gameName == practiceGame && entry.gamePanel != null && source.transform.IsChildOf(entry.gamePanel.transform)) {
                selector = candidate; hostPanel = entry.gamePanel;
            }
        }
        var settings = Resources.Load<TextAsset>("PlanetDuelSettings");
        url = (settings == null ? "http://localhost:8787" : JsonUtility.FromJson<Settings>(settings.text).serverUrl).TrimEnd('/');
        if (overlay == null) Build();
        overlay.SetActive(true); Fit(); StartPractice();
    }
    void Build()
    {
        overlay = new GameObject(practiceGame + " Essay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = overlay.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 310;
        var scaler = overlay.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1672, 941); scaler.matchWidthOrHeight = .5f;
        Box("Backdrop", overlay.transform, 0, 0, 1, 1, new Color(.015f, .06f, .12f));
        page = Rect(practiceGame + "Page", overlay.transform, .5f, .5f, .5f, .5f); page.sizeDelta = new Vector2(1672, 941);
        var skin = page.gameObject.AddComponent<RawImage>(); skin.texture = Resources.Load<Texture2D>(practiceGame + "EssaySkin");
        // The supplied artwork is kept intact; runtime panels cover only its sample text.
        var q = caveSkin ? Box("LiveQuestion", page, .105f, .302f, .635f, .415f, new Color(1, .94f, .81f))
            : sheepSkin ? Box("LiveQuestion", page, .306f, .321f, .838f, .425f, new Color(1, .94f, .81f))
            : Box("LiveQuestion", page, .125f, .475f, .772f, .612f, new Color(.88f, .96f, 1));
        question = Text(q, "Đang tải câu hỏi…", 30, 0, 0, 1, 1, Ink);
        var field = caveSkin ? Box("Answer", page, .079f, .582f, .644f, .777f, new Color(1, .975f, .94f))
            : sheepSkin ? Box("Answer", page, .307f, .557f, .921f, .751f, new Color(1, .975f, .92f))
            : Box("Answer", page, .074f, .737f, .774f, .829f, Color.white);
        field.gameObject.SetActive(false);
        var area = Box("TextArea", field, 0, 0, 1, 1, Color.clear); area.GetComponent<Image>().raycastTarget = false;
        area.offsetMin = new Vector2(14, ParchmentSkin ? 32 : 8); area.offsetMax = new Vector2(-14, -8); area.gameObject.AddComponent<RectMask2D>();
        var answer = Text(area, "", 25, 0, 0, 1, 1, Ink); answer.enableAutoSizing = false; answer.alignment = TextAlignmentOptions.TopLeft;
        var placeholder = Text(area, "Nhập câu trả lời… (Enter để xuống dòng)", 24, 0, 0, 1, 1, new Color(.45f, .49f, .55f)); placeholder.alignment = TextAlignmentOptions.TopLeft;
        input = field.gameObject.AddComponent<TMP_InputField>(); input.textViewport = area; input.textComponent = answer; input.placeholder = placeholder;
        input.targetGraphic = field.GetComponent<Image>(); input.contentType = TMP_InputField.ContentType.Standard; input.lineType = TMP_InputField.LineType.MultiLineNewline;
        // Keep the input and its label in plain-text mode so TMP does not
        // insert visible <u> tags around Vietnamese IME composition text.
        input.richText = false;
        EssayInputState.Configure(input);
        input.characterLimit = 2000; input.customCaretColor = true; input.caretColor = Ink; input.caretWidth = 2; field.gameObject.SetActive(true);
        if (ParchmentSkin) {
            var counter = Text(field, "0/2000", 21, .72f, .82f, .985f, .99f, Ink); counter.alignment = TextAlignmentOptions.BottomRight;
            input.onValueChanged.AddListener(value => counter.text = value.Length + "/2000");
        }
        if (caveSkin) {
            Click("Close", page, .918f, .003f, .998f, .15f, "", Close, Color.clear);
            Click("Photos", page, .171f, .831f, .444f, .958f, "", () => { if (Editable()) photoPanel.gameObject.SetActive(true); }, Color.clear);
            submit = Click("Submit", page, .46f, .831f, .74f, .958f, "", Submit, Color.clear);
            hintButton = Click("Hint", page, .752f, .68f, .954f, .792f, "", ShowHints, Color.clear);
            scoreboard = Text(page, "Câu 0/5\n0/50 điểm", 23, .124f, .068f, .241f, .122f, Ink);
        } else if (sheepSkin) {
            Click("Close", page, .90f, .018f, .988f, .145f, "", Close, Color.clear);
            Click("Photos", page, .346f, .801f, .61f, .916f, "", () => { if (Editable()) photoPanel.gameObject.SetActive(true); }, Color.clear);
            submit = Click("Submit", page, .63f, .801f, .906f, .916f, "", Submit, Color.clear);
            hintButton = Click("Hint", page, .86f, .36f, .996f, .485f, "", ShowHints, Color.clear);
            scoreboard = Text(page, "Câu 0/5\n0/50 điểm", 24, .112f, .121f, .244f, .182f, Ink);
        } else {
            Click("Close", page, .90f, .005f, .998f, .17f, "", Close, Color.clear);
            Click("Photos", page, .22f, .872f, .505f, .985f, "", () => { if (Editable()) photoPanel.gameObject.SetActive(true); }, Color.clear);
            submit = Click("Submit", page, .525f, .872f, .78f, .985f, "", Submit, Color.clear);
            hintButton = Click("Hint", page, .81f, .61f, .987f, .725f, "", ShowHints, Color.clear);
            scoreboard = Text(page, "Câu 0/5\n0/50 điểm", 25, .815f, .22f, .95f, .335f, Color.white);
        }
        var info = caveSkin ? Box("LessonInfo", page, .28f, .008f, .67f, .068f, new Color(.17f, .075f, .025f, .94f))
            : sheepSkin ? Box("LessonInfo", page, .025f, .205f, .25f, .25f, new Color(.17f, .075f, .025f, .94f))
            : Box("LessonInfo", page, .025f, .22f, .30f, .34f, new Color(.025f, .16f, .43f, .94f));
        context = Text(info, "", 23, .04f, .03f, .96f, .97f, Color.white);
        var feedback = caveSkin ? Box("Status", page, .19f, .966f, .74f, .998f, new Color(.17f, .075f, .025f, .94f))
            : sheepSkin ? Box("Status", page, .025f, .255f, .25f, .30f, new Color(.17f, .075f, .025f, .94f))
            : Box("Status", page, .035f, .345f, .30f, .397f, new Color(.02f, .15f, .35f, .94f));
        status = Text(feedback, "", 20, .03f, 0, .97f, 1, Color.white);
        var imageArea = caveSkin ? Box("AttachedPhoto", page, .80f, .165f, .975f, .285f, new Color(.17f, .075f, .025f, .9f))
            : sheepSkin ? Box("AttachedPhoto", page, .035f, .31f, .235f, .42f, new Color(.17f, .075f, .025f, .9f))
            : Box("AttachedPhoto", page, .825f, .755f, .975f, .96f, new Color(.025f, .14f, .3f, .9f));
        photoHint = Text(imageArea, "Ảnh bài làm\n(chưa đính kèm)", 20, 0, 0, 1, 1, Color.white);
        preview = Preview(imageArea);
        BuildResults(); BuildHints(); BuildPhotoMenu();
        errorPanel = Modal("Error");
        errorText = Text(errorPanel, "", 30, .07f, .12f, .93f, .65f, Color.white);
        retry = Click("Retry", errorPanel, .08f, .73f, .48f, .91f, "Thử lại", StartPractice);
        Click("Back", errorPanel, .52f, .73f, .92f, .91f, "Chọn game", Close);
        errorPanel.gameObject.SetActive(false);
        var exitShade = Box("ExitConfirmation", page, 0, 0, 1, 1, new Color(0, 0, 0, .65f));
        exitConfirmation = exitShade;
        var exitCard = Box("Card", exitShade, .18f, .3f, .82f, .7f, new Color(.015f, .105f, .29f, 1));
        Text(exitCard, "Bạn có muốn thoát game không?", 32, .05f, .1f, .95f, .5f, Color.white);
        stayButton = Click("Stay", exitCard, .07f, .65f, .47f, .9f, "Ở lại", () => StartCoroutine(SetExitPause(false)));
        Click("Leave", exitCard, .53f, .65f, .93f, .9f, "Thoát game", LeavePractice);
        exitConfirmation.gameObject.SetActive(false);
    }
    void BuildResults()
    {
        resultPanel = Modal("Results", .08f, .18f, .92f, .97f);
        Text(resultPanel, "KẾT QUẢ TỰ LUẬN", 34, .08f, .025f, .92f, .11f, Color.white);
        var view = Box("Viewport", resultPanel, .05f, .13f, .95f, .69f, ParchmentSkin ? new Color(1, .97f, .89f) : new Color(.94f, .98f, 1)); view.gameObject.AddComponent<RectMask2D>();
        results = view.gameObject.AddComponent<ScrollRect>(); results.horizontal = false; results.viewport = view; results.movementType = ScrollRect.MovementType.Clamped;
        var content = Box("Content", view, 0, 0, 1, 0, Color.clear); content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(.5f, 1);
        content.GetComponent<Image>().raycastTarget = false;
        var layout = content.gameObject.AddComponent<VerticalLayoutGroup>(); layout.padding = new RectOffset(24, 24, 20, 20); layout.childControlWidth = true; layout.childControlHeight = true; layout.childForceExpandHeight = false;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        resultText = Text(content, "", 26, 0, 0, 1, 1, Ink); resultText.enableAutoSizing = false; resultText.alignment = TextAlignmentOptions.TopLeft;
        results.content = content;
        Click("AskMore", resultPanel, .22f, .71f, .78f, .81f, "Hỏi AI thêm", () => {
            if (latest == null || (latest.state != "review" && latest.state != "finished")) return;
            ScienceChatController.ForReview(this).OpenReview(token + ":" + round, "Câu " + round + " • " + latest.text,
                resultText.text, ParchmentSkin ? new Color(.24f, .12f, .055f) : new Color(.015f, .105f, .29f));
        });
        Click("ExitPractice", resultPanel, .05f, .84f, .48f, .96f, "Thoát luyện tập", Close,
            ParchmentSkin ? new Color(.55f, .22f, .08f) : new Color(.15f, .27f, .44f));
        next = Click("Next", resultPanel, .52f, .84f, .95f, .96f, "Câu tiếp theo", Next);
        resultPanel.gameObject.SetActive(false);
    }
    void BuildHints()
    {
        hintPanel = Modal("AIHints", .14f, .24f, .86f, .81f);
        Text(hintPanel, "AI GỢI Ý • LẦN 1: -1 • LẦN 2: -2 ĐIỂM", 32, .05f, .04f, .95f, .17f, Color.white);
        hintText = Text(hintPanel, "", 26, .07f, .21f, .93f, .72f, Color.white); hintText.alignment = TextAlignmentOptions.TopLeft;
        hintNext = Click("MoreHint", hintPanel, .07f, .78f, .54f, .94f, "Gợi ý tiếp", () => StartCoroutine(HintRoutine()));
        Click("CloseHint", hintPanel, .59f, .78f, .93f, .94f, "Làm bài", () => hintPanel.gameObject.SetActive(false));
        hintPanel.gameObject.SetActive(false);
    }
    void BuildPhotoMenu()
    {
        photoPanel = Modal("PhotoOptions", .22f, .26f, .78f, .82f);
        Text(photoPanel, "ẢNH BÀI LÀM", 32, .05f, .03f, .95f, .17f, Color.white);
        Click("File", photoPanel, .08f, .23f, .92f, .39f, "Chọn ảnh từ máy", () => { photoPanel.gameObject.SetActive(false); if (Editable()) StartCoroutine(PickPhoto()); });
        Click("Camera", photoPanel, .08f, .43f, .92f, .59f, "Chụp bằng webcam", () => { photoPanel.gameObject.SetActive(false); if (Editable()) StartCoroutine(OpenCamera()); });
        Click("Remove", photoPanel, .08f, .63f, .47f, .80f, "Xóa ảnh", () => { ClearPhoto(); notice = "Đã xóa ảnh."; photoPanel.gameObject.SetActive(false); });
        Click("Done", photoPanel, .53f, .63f, .92f, .80f, "Quay lại", () => photoPanel.gameObject.SetActive(false));
        photoPanel.gameObject.SetActive(false);
    }
    RectTransform Box(string name, Transform parent, float left, float top, float right, float bottom, Color color)
    {
        var rect = Rect(name, parent, left, top, right, bottom); rect.gameObject.AddComponent<Image>().color = color; return rect;
    }
    RectTransform Rect(string name, Transform parent, float left, float top, float right, float bottom)
    {
        var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform; rect.anchorMin = new Vector2(left, 1 - bottom); rect.anchorMax = new Vector2(right, 1 - top); rect.offsetMin = rect.offsetMax = Vector2.zero; return rect;
    }
    TMP_Text Text(Transform parent, string value, int size, float left, float top, float right, float bottom, Color color)
    {
        var rect = Rect("Label", parent, left, top, right, bottom);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        // Use the chat's complete Vietnamese font when available.
        var font = Resources.Load<Font>("ScienceChatFont");
        if (sharedFont == null && font != null) { sharedFont = TMP_FontAsset.CreateFontAsset(font); sharedFont.isMultiAtlasTexturesEnabled = true; }
        text.font = sharedFont != null ? sharedFont : fallbackFont; text.text = value; text.fontSize = size;
        text.enableAutoSizing = true; text.fontSizeMin = 16; text.fontSizeMax = size; text.alignment = TextAlignmentOptions.Center;
        text.color = color; text.raycastTarget = false; text.richText = false; return text;
    }
    static TMP_FontAsset sharedFont;
    Button Click(string name, Transform parent, float left, float top, float right, float bottom, string label, UnityEngine.Events.UnityAction action, Color? color = null)
    {
        var rect = Box(name, parent, left, top, right, bottom, color ?? new Color(.02f, .4f, .88f));
        var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = rect.GetComponent<Image>();
        if (!string.IsNullOrEmpty(label)) Text(rect, label, 27, .03f, .03f, .97f, .97f, Color.white);
        button.onClick.AddListener(action); return button;
    }
    RectTransform Modal(string name, float left = .16f, float top = .25f, float right = .84f, float bottom = .80f)
    {
        var rect = Box(name, page, left, top, right, bottom, ParchmentSkin ? new Color(.19f, .085f, .025f, .99f) : new Color(.015f, .105f, .29f, .99f));
        var edge = rect.gameObject.AddComponent<Outline>(); edge.effectColor = ParchmentSkin ? new Color(.95f, .68f, .2f) : new Color(.2f, .8f, 1); edge.effectDistance = new Vector2(3, -3); return rect;
    }
    RawImage Preview(Transform parent)
    {
        var go = new GameObject("Preview", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter)); go.transform.SetParent(parent, false);
        var image = go.GetComponent<RawImage>(); image.color = Color.clear; image.raycastTarget = false;
        go.GetComponent<AspectRatioFitter>().aspectMode = AspectRatioFitter.AspectMode.FitInParent; return image;
    }
    void Fit() { if (page != null && overlay != null) { var rect = (RectTransform)overlay.transform; page.localScale = Vector3.one * Mathf.Min(rect.rect.width / 1672, rect.rect.height / 941); } }
    bool Editable() { return !exitPending && latest != null && !latest.paused && latest.state == "answering" && !commandBusy && Remaining() > 0; }
    float Remaining() { return latest == null ? 0 : Mathf.Max(0, latest.remaining - (latest.paused ? 0 : Time.realtimeSinceStartup - receivedAt)); }
    void Update()
    {
        if (overlay == null || !overlay.activeSelf) return;
        Fit(); bool editable = Editable(); input.interactable = editable; submit.interactable = editable; hintButton.interactable = editable && !hintBusy;
        if (latest != null) scoreboard.text = "Câu " + latest.round + "/5 • " + latest.score + "/50\n" + (latest.state == "answering" ? Mathf.CeilToInt(Remaining()) + " giây" : latest.state == "grading" || commandBusy ? "Đang xử lý…" : "Đã nộp bài");
        status.text = commandBusy ? "Đang xử lý…" : !string.IsNullOrEmpty(notice) ? notice : editable ? "Nhập chữ hoặc chọn ảnh để làm bài." : "Đang lấy trạng thái…";
        if (!editable) { StopCamera(); CancelPicker(); if (photoPanel != null) photoPanel.gameObject.SetActive(false); }
        if (webcam != null && webcam.width > 16) {
            cameraPreview.rectTransform.localEulerAngles = new Vector3(0, 0, -webcam.videoRotationAngle);
            cameraPreview.uvRect = webcam.videoVerticallyMirrored ? new Rect(0, 1, 1, -1) : new Rect(0, 0, 1, 1);
            cameraPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)webcam.width / webcam.height;
            capture.interactable = editable; cameraText.text = "Còn " + Mathf.CeilToInt(Remaining()) + " giây • Chụp rõ toàn bộ bài làm";
        }
    }
    void StartPractice()
    {
        var chat = GetComponent<ScienceChatController>(); if (chat != null) chat.ClearReviews();
        historyQueued = false;
        practiceUsername = string.IsNullOrWhiteSpace(GameSession.Username) ? PlayerPrefs.GetString("Username", "") : GameSession.Username;
        ReleaseSession(); generation++; revision++; StopAllCoroutines(); AbortRequests(); latest = null; round = -1; commandBusy = true; hintBusy = false;
        notice = "Đang tải 5 câu đúng môn và chương…"; question.text = notice; ClearPhoto(); StopCamera(); CancelPicker();
        EssayInputState.Clear(input);
        errorPanel.gameObject.SetActive(false); resultPanel.gameObject.SetActive(false); hintPanel.gameObject.SetActive(false);
        context.text = GameSession.Subject + "\n" + GameSession.Chapter;
        StartCoroutine(StartRoutine(generation));
    }
    IEnumerator StartRoutine(int version)
    {
        State state = null; string error = null;
        yield return Request("start", new Command { game = practiceGame, subject = GameSession.Subject, chapter = GameSession.Chapter, username = GameSession.Username, name = GameSession.Fullname, difficulties = GameSession.Difficulties.ToArray() }, (s, e) => { state = s; error = e; });
        if (version != generation) yield break; commandBusy = false;
        if (error != null || state == null) { Error(error ?? "Không tải được câu hỏi."); yield break; }
        token = state.token; Apply(state); StartCoroutine(Poll(version));
    }
    IEnumerator Poll(int version)
    {
        while (version == generation && !string.IsNullOrEmpty(token)) {
            yield return new WaitForSecondsRealtime(1);
            if (commandBusy || exitPending) continue;
            int expected = revision; State state = null; string error = null;
            yield return Request("state", new Command { token = token }, (s, e) => { state = s; error = e; });
            if (version != generation) yield break;
            if (expected != revision) continue;
            if (error != null) { notice = error; if (Time.realtimeSinceStartup - receivedAt > 15) { Error(error); yield break; } continue; }
            if (state != null) Apply(state);
        }
    }
    IEnumerator Request(string path, Command body, Action<State, string> done)
    {
        using (var request = new UnityWebRequest(url + "/football/" + path, "POST")) {
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(body))); request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json"); request.timeout = path == "start" ? 45 : path == "submit" ? 30 : path == "hint" ? 20 : 5; requests.Add(request);
            yield return request.SendWebRequest(); requests.Remove(request);
            State state = null; try { state = JsonUtility.FromJson<State>(request.downloadHandler.text); } catch (Exception) { }
            string error = state != null ? state.error : null;
            if (request.result != UnityWebRequest.Result.Success || state == null) error = error ?? "Chưa kết nối được " + practiceGame + " tự luận. Hãy khởi động lại PlanetServer và kiểm tra Apps Script.";
            done(state, error);
        }
    }
    void Apply(State state)
    {
        if (latest != null && state.round < latest.round) return;
        bool changed = round != state.round; string previous = latest == null ? "" : latest.state;
        latest = state; receivedAt = Time.realtimeSinceStartup;
        if (!historyQueued && state.round == 5 && (state.state == "review" || state.state == "finished")) {
            historyQueued = true;
            PlanetHistorySync.Enqueue(new PlanetMatchRecord {
                matchId = "practice-" + state.token, username = practiceUsername, opponent = "Máy AI",
                mode = "Luyện tập", questionType = "essay", score = state.score, maxScore = 50,
                subject = state.subject, chapter = state.chapter,
                result = state.score >= 25 ? "THẮNG" : "THUA"
            });
        }
        if (changed) { round = state.round; EssayInputState.Clear(input); notice = null; ClearPhoto(); hintPanel.gameObject.SetActive(false); }
        question.text = state.difficulty + " • " + state.text;
        if (state.state == "review" || state.state == "finished") {
            resultPanel.gameObject.SetActive(true); resultPanel.SetAsLastSibling(); hintPanel.gameObject.SetActive(false);
            resultText.text = (state.state == "finished" ? "HOÀN THÀNH: " + state.score + "/50 ĐIỂM\n\n" : "CÂU " + state.round + ": " + state.earned + "/10 ĐIỂM\n\n")
                + "Điểm bài làm: " + state.rawScore + "/10\nTrừ gợi ý: " + state.hintPenalty + " điểm\nĐiểm nhận: " + state.earned + "/10 (tối thiểu 0)\n\n"
                + state.text + "\n\nNHẬN XÉT CỦA AI\n" + state.feedback + "\n\nBÀI LÀM AI ĐỌC ĐƯỢC\n" + state.transcription
                + "\n\nĐÁP ÁN THAM KHẢO\n" + state.sampleAnswer + "\n\nBÀI GIẢI GỢI Ý CỦA AI\n" + state.suggestedAnswer
                + "\n\nGợi ý đã dùng: " + (state.hints == null ? 0 : state.hints.Length) + "/2 • Tổng mức trừ: " + state.hintPenalty + " điểm\nHỏi AI thêm sau khi nộp: miễn phí.";
            next.GetComponentInChildren<TMP_Text>().text = state.state == "finished" ? "Luyện tập lại" : state.round == 5 ? "Xem tổng kết" : "Câu tiếp theo";
            if (changed || previous != state.state) { Canvas.ForceUpdateCanvases(); LayoutRebuilder.ForceRebuildLayoutImmediate(results.content); results.verticalNormalizedPosition = 1; }
        } else { resultPanel.gameObject.SetActive(false); if (changed && !commandBusy) { input.Select(); input.ActivateInputField(); } }
    }
    void Submit()
    {
        if (!Editable()) return;
        if (string.IsNullOrWhiteSpace(input.text) && string.IsNullOrEmpty(imageBase64)) { notice = "Hãy nhập bài hoặc đính kèm ảnh trước khi nộp."; return; }
        StartCoroutine(CommandRoutine("submit", new Command { token = token, round = round, text = input.text.Trim(), imageBase64 = imageBase64, mimeType = string.IsNullOrEmpty(imageBase64) ? null : "image/jpeg" }));
    }
    void Next() { if (commandBusy || latest == null) return; if (latest.state == "finished") StartPractice(); else StartCoroutine(CommandRoutine("next", new Command { token = token, round = round })); }
    IEnumerator CommandRoutine(string path, Command body)
    {
        commandBusy = true; revision++; int version = generation; State state = null; string error = null; notice = null;
        yield return Request(path, body, (s, e) => { state = s; error = e; });
        if (version != generation) yield break; commandBusy = false;
        if (state != null && !string.IsNullOrEmpty(state.state)) Apply(state);
        if (error != null) notice = error;
    }
    void ShowHints()
    {
        if (!Editable() || hintBusy) return;
        hintPanel.gameObject.SetActive(true); hintPanel.SetAsLastSibling();
        RenderHints();
    }
    void RenderHints()
    {
        var hints = latest == null || latest.hints == null ? new string[0] : latest.hints;
        var lines = new List<string>(); for (int i = 0; i < hints.Length; i++) lines.Add("Gợi ý " + (i + 1) + "/2: " + hints[i]);
        hintText.text = hints.Length == 0 ? (hintBusy ? "AI đang gợi ý hướng làm…" : "Gợi ý 1 trừ 1 điểm. Gợi ý 2 trừ thêm 2 điểm.\nChỉ tính khi nhận gợi ý thành công; xem lại không trừ thêm.\nĐiểm cuối câu không thấp hơn 0.") : string.Join("\n\n", lines) + "\n\nTổng mức trừ khi chấm câu này: " + latest.hintPenalty + " điểm.";
        hintNext.GetComponentInChildren<TMP_Text>().text = hintBusy ? "Đang lấy gợi ý…" : hints.Length == 0 ? "Gợi ý 1 (-1 điểm)" : "Gợi ý 2 (-2 điểm)";
        hintNext.gameObject.SetActive(hints.Length < 2); hintNext.interactable = !hintBusy && Editable();
    }
    IEnumerator HintRoutine()
    {
        if (!Editable() || hintBusy || (latest.hints != null && latest.hints.Length >= 2)) yield break;
        hintBusy = true; int version = generation, requestedRound = round; State state = null; string error = null; RenderHints();
        yield return Request("hint", new Command { token = token, round = round, level = (latest.hints == null ? 0 : latest.hints.Length) + 1 }, (s, e) => { state = s; error = e; });
        if (version != generation) yield break; hintBusy = false;
        if (round != requestedRound) yield break;
        if (state != null && !string.IsNullOrEmpty(state.state)) Apply(state);
        RenderHints(); if (error != null) hintText.text = error;
    }
    void Error(string message) { notice = message; errorText.text = message; errorPanel.gameObject.SetActive(true); errorPanel.SetAsLastSibling(); }
    void Close()
    {
        if (exitPending) return;
        exitPending = true; StopCamera(); CancelPicker();
        exitConfirmation.gameObject.SetActive(true); exitConfirmation.SetAsLastSibling();
        StartCoroutine(SetExitPause(true));
    }
    IEnumerator SetExitPause(bool pause)
    {
        stayButton.interactable = false;
        // Let a submission or initial session request finish before pausing.
        // Hint generation can continue while the answer deadline is paused.
        while (commandBusy) yield return null;
        revision++;
        State state = null; string error = null;
        if (!string.IsNullOrEmpty(token)) {
            yield return Request(pause ? "pause" : "resume", new Command { token = token }, (s, e) => { state = s; error = e; });
            if (state != null && !string.IsNullOrEmpty(state.state)) Apply(state);
        }
        stayButton.interactable = true;
        exitConfirmation.SetAsLastSibling();
        if (error != null) { Error(error); exitConfirmation.SetAsLastSibling(); yield break; }
        if (!pause) { exitPending = false; exitConfirmation.gameObject.SetActive(false); }
    }
    void LeavePractice() { exitPending = false; exitConfirmation.gameObject.SetActive(false); ReleaseSession(); if (selector != null) selector.ReturnFromGame(hostPanel); else hostPanel.SetActive(false); }
    void ReleaseSession()
    {
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(url)) return;
        var request = new UnityWebRequest(url + "/football/leave", "POST"); request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(new Command { token = token })));
        request.downloadHandler = new DownloadHandlerBuffer(); request.SetRequestHeader("Content-Type", "application/json"); request.timeout = 5;
        request.SendWebRequest().completed += _ => request.Dispose(); token = null;
    }
    void AbortRequests() { foreach (var request in requests) { request.Abort(); request.Dispose(); } requests.Clear(); }
    void OnDisable() { exitPending = false; if (exitConfirmation != null) exitConfirmation.gameObject.SetActive(false); generation++; revision++; StopAllCoroutines(); AbortRequests(); ReleaseSession(); StopCamera(); CancelPicker(); ClearPhoto(); if (overlay != null) overlay.SetActive(false); }
    void OnDestroy() { if (overlay != null) Destroy(overlay); }

    // Photo routines share Planet's isolated Windows picker; webcam is opened only on request.
    IEnumerator PickPhoto()
    {
        if (!Editable() || picking) yield break;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        picking = true; var cancellation = new CancellationTokenSource(); pickerCancellation = cancellation;
        int version = generation, requestedRound = round;
        var task = PlanetEssayFileDialog.OpenAsync(PlayerPrefs.GetString("PlanetEssay.ImageFolder", Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)), cancellation.Token);
        notice = "Đang chọn ảnh trong cửa sổ Windows…";
        while (!task.IsCompleted) yield return null;
        if (pickerCancellation != cancellation) { cancellation.Dispose(); yield break; }
        pickerCancellation = null; picking = false; cancellation.Dispose();
        if (version != generation || round != requestedRound || !Editable() || task.IsCanceled) yield break;
        if (task.IsFaulted) { notice = "Không mở được ảnh. Bạn thử lại nhé."; yield break; }
        if (task.Result == null) { notice = null; yield break; }
        try { SetPhoto(task.Result.Bytes); PlayerPrefs.SetString("PlanetEssay.ImageFolder", Path.GetDirectoryName(task.Result.Path)); }
        catch { notice = "Không đọc được ảnh đã chọn."; }
#else
        notice = "Chọn file hiện hỗ trợ Windows.";
        yield break;
#endif
    }
    void SetPhoto(byte[] bytes)
    {
        var texture = new Texture2D(2, 2, TextureFormat.RGB24, false);
        if (bytes.Length > 2 * 1024 * 1024 || !texture.LoadImage(bytes)) { Destroy(texture); notice = "Ảnh không hợp lệ hoặc lớn hơn 2 MB."; return; }
        ClearPhoto(); attached = texture; imageBase64 = Convert.ToBase64String(bytes); preview.texture = texture; preview.color = Color.white; photoHint.gameObject.SetActive(false);
        preview.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height; notice = "Đã đính kèm ảnh. Bấm Nộp bài để chấm.";
    }
    void ClearPhoto() { imageBase64 = null; if (preview != null) { preview.texture = null; preview.color = Color.clear; } if (photoHint != null) photoHint.gameObject.SetActive(true); if (attached != null) Destroy(attached); attached = null; }
    void CancelPicker() { if (pickerCancellation != null) { pickerCancellation.Cancel(); pickerCancellation = null; } picking = false; }
    IEnumerator OpenCamera()
    {
        if (!Editable()) yield break;
        StopCamera(); int version = cameraVersion;
        yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
        if (version != cameraVersion || !Editable()) yield break;
        if (!Application.HasUserAuthorization(UserAuthorization.WebCam) || WebCamTexture.devices.Length == 0) { notice = "Chưa mở được webcam. Bạn có thể chọn ảnh từ máy."; yield break; }
        if (cameraPanel == null) {
            cameraPanel = Modal("Webcam", .12f, .17f, .88f, .94f);
            cameraText = Text(cameraPanel, "Đang mở webcam…", 28, .04f, .02f, .96f, .13f, Color.white);
            cameraPreview = Preview(Box("CameraView", cameraPanel, .08f, .15f, .92f, .79f, Color.black));
            capture = Click("Capture", cameraPanel, .1f, .83f, .49f, .96f, "Chụp bài", Capture);
            Click("CancelCamera", cameraPanel, .54f, .83f, .93f, .96f, "Quay lại", StopCamera);
        }
        cameraPanel.gameObject.SetActive(true); cameraPanel.SetAsLastSibling();
        try { webcam = new WebCamTexture(WebCamTexture.devices[0].name, 1600, 1200, 15); webcam.Play(); cameraPreview.texture = webcam; cameraPreview.color = Color.white; }
        catch { StopCamera(); notice = "Không mở được webcam. Hãy kiểm tra quyền camera Windows."; }
    }
    void Capture()
    {
        if (!Editable() || webcam == null || webcam.width <= 16) return;
        int w = webcam.width, h = webcam.height, angle = webcam.videoRotationAngle;
        int outW = angle == 90 || angle == 270 ? h : w, outH = angle == 90 || angle == 270 ? w : h;
        var source = webcam.GetPixels32(); var pixels = new Color32[source.Length];
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) {
            int sy = webcam.videoVerticallyMirrored ? h - 1 - y : y, dx = x, dy = y;
            if (angle == 90) { dx = y; dy = w - 1 - x; } else if (angle == 180) { dx = w - 1 - x; dy = h - 1 - y; } else if (angle == 270) { dx = h - 1 - y; dy = x; }
            pixels[dy * outW + dx] = source[sy * w + x];
        }
        var photo = new Texture2D(outW, outH, TextureFormat.RGB24, false);
        try { photo.SetPixels32(pixels); photo.Apply();
            float scale = Mathf.Min(1, 1600f / Mathf.Max(outW, outH));
            var rt = RenderTexture.GetTemporary(Mathf.Max(1, Mathf.RoundToInt(outW * scale)), Mathf.Max(1, Mathf.RoundToInt(outH * scale)));
            var previous = RenderTexture.active; Texture2D resized = null;
            try { Graphics.Blit(photo, rt); RenderTexture.active = rt; resized = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false); resized.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); resized.Apply(); SetPhoto(resized.EncodeToJPG(85)); }
            finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(rt); if (resized != null) Destroy(resized); }
        } catch { notice = "Chưa chụp được ảnh. Bạn thử lại nhé."; }
        finally { Destroy(photo); StopCamera(); }
    }
    void StopCamera() { cameraVersion++; if (webcam != null) { webcam.Stop(); Destroy(webcam); webcam = null; } if (cameraPreview != null) cameraPreview.texture = null; if (cameraPanel != null) cameraPanel.gameObject.SetActive(false); }
}

// TMP reads the OS composition even when an input is not focused. Keep idle
// answer fields read-only so an unfinished syllable cannot appear in a new game.
internal static class EssayInputState
{
    public static void Configure(TMP_InputField field)
    {
        field.readOnly = true;
        field.onSelect.AddListener(_ => { field.readOnly = false; field.ForceLabelUpdate(); });
        field.onDeselect.AddListener(_ => { field.readOnly = true; field.ForceLabelUpdate(); });
    }

    public static void Clear(TMP_InputField field)
    {
        // Deselect first: TMP may commit the pending composition on focus loss.
        // Clearing afterwards also removes that commit from the previous round.
        var events = UnityEngine.EventSystems.EventSystem.current;
        if (events != null && events.currentSelectedGameObject == field.gameObject)
            events.SetSelectedGameObject(null);
        field.DeactivateInputField(true);
        field.readOnly = true;
        field.text = "";
        field.caretPosition = 0;
        // The text setter skips updates when the value was already empty.
        field.ForceLabelUpdate();
    }
}
