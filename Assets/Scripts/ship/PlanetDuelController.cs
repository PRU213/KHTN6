using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

/// <summary>Planet's queue, synchronized match and screens. No third party Unity packages required.</summary>
public class PlanetDuelController : MonoBehaviour
{
    [Serializable] public class Settings { public string serverUrl = "http://localhost:8787"; }
    [Serializable] public class Join {
        public string name, username, subject, questionType; public string[] difficulties; public Vector2[] guns, targets;
    }
    [Serializable] public class Command { public string token; public bool fire, playWithBot; public int answer = -1, round; }
    [Serializable] class EssayCommand { public string token, text, imageBase64, mimeType; public int round; }
    [Serializable] public class Shot { public int id, player; public float x, y, dx, dy; }
    [Serializable] public class State {
        public ReviewQuestion[] reviewQuestions;
        public string token, matchId, finishedAt, state, text, reason, error; public string[] names, options;
        public int maxScore;
        public int seat, round, total, correct, winner; public bool bot;
        public float waited, waitLimit, countdown, remaining; public int[] scores;
        public float[] angles; public Vector2[] targets; public bool[] shot, stunned; public Shot[] bullets;
        public string questionType, sampleAnswer, feedback; public int[] submissions; public int earned;
        public string difficulty, transcription, suggestedAnswer; public float duration, revealRemaining;
    }
    [Serializable] public class ReviewQuestion {
        public int round, earned;
        public string text, difficulty, sampleAnswer, feedback, transcription, suggestedAnswer;
    }
    [Serializable] public class Bank { public BankQuestion[] questions; }
    [Serializable] public class BankQuestion {
        public string subject, difficulty, text; public string[] options; public int correct;
    }

    BlastRoundManager game;
    RectTransform root, overlay, card, hud;
    TMP_Text heading, detail, badge, hudText;
    Button primary, secondary, shoot, exit;
    string token, url, mode;
    bool pendingFire, pendingBot, local, capturedTargets;
    int pendingAnswer = -1, pendingRound;
    float lastGood;
    int generation;
    Coroutine loop;
    UnityWebRequest activeRequest;
    State latest;
    readonly Dictionary<int, RectTransform> projectiles = new Dictionary<int, RectTransform>();
    readonly List<Behaviour> animations = new List<Behaviour>();
    readonly List<Behaviour> previouslyEnabled = new List<Behaviour>();
    readonly Vector2[] initialTargets = new Vector2[4];
    TMP_Text[] hints;
    RectTransform essayPanel;
    TMP_InputField essayInput;
    TMP_Text essayQuestion, essayStatus, essayClock, essayCounter;
    Button essaySubmit;
    UnityWebRequest essayRequest;
    bool essayMode, essaySubmitting;
    int essayRound = -1;
    string essayError;
    Button chooseImage, takePhoto, removeImage, capturePhoto;
    RawImage imagePreview, cameraPreview;
    RectTransform imageCard, cameraPanel, essayResults;
    TMP_Text resultBody, cameraStatus;
    ScrollRect resultScroll;
    Texture2D attachedImage;
    string attachedBase64;
    WebCamTexture webcam;
    int cameraGeneration;
    bool pickingImage;
    System.Threading.CancellationTokenSource imagePickerCancellation;
    int shownResultRound = -1;
    string historyMatchId, matchUsername;
    RectTransform exitConfirmation;
    RectTransform reviewPicker;
    Button askAfterMatch;

    public void Open(BlastRoundManager manager)
    {
        game = manager; root = game.guns[0].parent as RectTransform;
        essayMode = GameSession.QuestionType == "Tự luận";
        root.localScale = Vector3.one;
        if (overlay == null) BuildScreens();
        animations.Clear(); previouslyEnabled.Clear();
        foreach (var drift in root.GetComponentsInChildren<BlobDrift>(true)) animations.Add(drift);
        foreach (var mover in root.GetComponentsInChildren<BlackHoleMover>(true)) animations.Add(mover);
        foreach (var sway in root.GetComponentsInChildren<ShipSway>(true)) animations.Add(sway);
        foreach (var shooter in root.GetComponentsInChildren<ShipShooter>(true)) animations.Add(shooter);
        foreach (var animation in animations) { if (animation.enabled) previouslyEnabled.Add(animation); animation.enabled = false; }
        Canvas.ForceUpdateCanvases();
        if (!capturedTargets) {
            for (int i = 0; i < 4; i++) initialTargets[i] = root.InverseTransformPoint(game.blobImages[i].transform.position);
            capturedTargets = true;
        }
        hints = root.GetComponentsInChildren<TMP_Text>(true);
        game.MatchFinished = LocalFinished;
        BeginSearch();
    }

    void BuildScreens()
    {
        for (int i = 0; i < game.blobImages.Length; i++) {
            int answer = i;
            var planet = game.blobImages[i];
            planet.raycastTarget = true;
            var button = planet.GetComponent<Button>();
            if (button == null) button = planet.gameObject.AddComponent<Button>();
            button.targetGraphic = planet;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => ChooseAnswer(answer));
        }
        overlay = Box("PlanetDuelScreens", root, Vector2.zero, Vector2.one, new Color(.025f, .025f, .12f, .7f));
        card = Box("SpaceCard", overlay, new Vector2(.16f, .12f), new Vector2(.84f, .88f), new Color(.085f, .04f, .19f, .98f));
        var roundedFrame = game.resultPanel.GetComponent<Image>();
        if (roundedFrame != null) { card.GetComponent<Image>().sprite = roundedFrame.sprite; card.GetComponent<Image>().type = roundedFrame.type; }
        var outline = card.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(.55f, .23f, 1f, .95f); outline.effectDistance = new Vector2(3, -3);
        var frame = game.questionText.GetComponentInParent<Image>();
        if (frame != null) {
            var banner = Box("NeonPlanetBanner", card, new Vector2(.08f, .71f), new Vector2(.92f, .98f), new Color(.48f, .3f, .75f));
            banner.GetComponent<Image>().sprite = frame.sprite; banner.GetComponent<Image>().preserveAspect = true;
            banner.GetComponent<Image>().raycastTarget = false;
        }
        // Decorative ships reuse the actual Planet sprites.
        for (int i = 0; i < 2; i++) {
            var icon = Box("Ship" + i, card, new Vector2(.17f + i * .5f, .46f), new Vector2(.33f + i * .5f, .65f), Color.white);
            icon.GetComponent<Image>().sprite = game.guns[i].GetComponent<Image>().sprite;
            icon.GetComponent<Image>().preserveAspect = true;
            icon.GetComponent<Image>().raycastTarget = false;
        }
        heading = Label("Title", card, new Vector2(.12f, .78f), new Vector2(.88f, .91f), 42);
        badge = Label("MatchBadge", card, new Vector2(.06f, .65f), new Vector2(.94f, .76f), 24);
        detail = Label("Status", card, new Vector2(.08f, .25f), new Vector2(.92f, .45f), 28);
        primary = MakeButton("Primary", card, new Vector2(.13f, .07f), new Vector2(.48f, .21f), "", () => Primary());
        secondary = MakeButton("Secondary", card, new Vector2(.52f, .07f), new Vector2(.87f, .21f), "Về trang chủ", ConfirmExit);
        hud = Box("DuelHUD", root, Vector2.zero, Vector2.one, Color.clear);
        hud.GetComponent<Image>().raycastTarget = false;
        hudText = Label("Players", hud, new Vector2(.2f, .91f), new Vector2(.8f, .99f), 23);
        shoot = MakeButton("Fire", hud, new Vector2(.78f, .015f), new Vector2(.96f, .105f), "BẮN", () => { if (local) game.FireLocal(); else pendingFire = true; });
        exit = MakeButton("Exit", hud, new Vector2(.025f, .92f), new Vector2(.17f, .985f), "Rời trận", ConfirmExit);
        BuildEssayScreen();
        overlay.SetAsLastSibling();
        exitConfirmation = Box("ExitConfirmation", root, Vector2.zero, Vector2.one, new Color(0, 0, 0, .7f));
        var exitCard = Box("ExitCard", exitConfirmation, new Vector2(.2f, .3f), new Vector2(.8f, .7f), new Color(.085f, .04f, .19f, 1));
        Label("Question", exitCard, new Vector2(.06f, .5f), new Vector2(.94f, .9f), 32).text = "Bạn có muốn thoát game không?";
        Label("Warning", exitCard, new Vector2(.06f, .33f), new Vector2(.94f, .5f), 22).text = "Thời gian vẫn chạy. Rời trận đang đấu sẽ tính là bỏ cuộc.";
        MakeButton("Stay", exitCard, new Vector2(.07f, .08f), new Vector2(.47f, .28f), "Ở lại", () => exitConfirmation.gameObject.SetActive(false));
        MakeButton("Leave", exitCard, new Vector2(.53f, .08f), new Vector2(.93f, .28f), "Thoát game", ReturnToMenu);
        exitConfirmation.gameObject.SetActive(false);
        askAfterMatch = MakeButton("AskAfterMatch", card, new Vector2(.2f, .215f), new Vector2(.8f, .295f), "Xem lại câu / Hỏi AI thêm", OpenReviewPicker);
        askAfterMatch.gameObject.SetActive(false);
    }

    void BuildEssayScreen()
    {
        essayPanel = Box("PlanetEssay", root, new Vector2(.16f, .13f), new Vector2(.84f, .89f), new Color(.065f, .035f, .17f, .98f));
        var frame = game.resultPanel.GetComponent<Image>();
        if (frame != null) { essayPanel.GetComponent<Image>().sprite = frame.sprite; essayPanel.GetComponent<Image>().type = frame.type; }
        var edge = essayPanel.gameObject.AddComponent<Outline>(); edge.effectColor = new Color(.45f, .4f, 1); edge.effectDistance = new Vector2(3, -3);
        var title = Label("EssayTitle", essayPanel, new Vector2(.06f, .87f), new Vector2(.70f, .97f), 33);
        title.text = "ĐẤU TRƯỜNG TỰ LUẬN"; title.color = new Color(.65f, .9f, 1);
        essayClock = Label("EssayClock", essayPanel, new Vector2(.73f, .87f), new Vector2(.94f, .97f), 30);
        essayQuestion = Label("EssayQuestion", essayPanel, new Vector2(.06f, .68f), new Vector2(.94f, .86f), 30);
        essayQuestion.alignment = TextAlignmentOptions.MidlineLeft;
        var field = Box("EssayAnswer", essayPanel, new Vector2(.06f, .39f), new Vector2(.65f, .65f), new Color(.96f, .96f, 1));
        field.gameObject.SetActive(false);
        var area = Box("TextArea", field, Vector2.zero, Vector2.one, Color.clear);
        area.GetComponent<Image>().raycastTarget = false;
        area.offsetMin = new Vector2(18, 12); area.offsetMax = new Vector2(-18, -12);
        area.gameObject.AddComponent<RectMask2D>();
        var text = Label("AnswerText", area, Vector2.zero, Vector2.one, 26);
        text.enableAutoSizing = false; text.alignment = TextAlignmentOptions.TopLeft; text.color = new Color(.13f, .09f, .25f);
        var placeholder = Label("Placeholder", area, Vector2.zero, Vector2.one, 25);
        placeholder.alignment = TextAlignmentOptions.TopLeft; placeholder.color = new Color(.46f, .42f, .56f);
        placeholder.text = "Viết câu trả lời của bạn tại đây…\nBạn có thể xuống dòng để trình bày các ý.";
        essayInput = field.gameObject.AddComponent<TMP_InputField>();
        essayInput.targetGraphic = field.GetComponent<Image>(); essayInput.textViewport = area;
        essayInput.textComponent = text; essayInput.placeholder = placeholder;
        essayInput.contentType = TMP_InputField.ContentType.Standard;
        // Match the plain-text label during Vietnamese IME composition.
        essayInput.richText = false;
        EssayInputState.Configure(essayInput);
        essayInput.lineType = TMP_InputField.LineType.MultiLineNewline; essayInput.characterLimit = 2000;
        essayInput.customCaretColor = true; essayInput.caretColor = text.color; essayInput.caretWidth = 2;
        field.gameObject.SetActive(true);
        imageCard = Box("AttachedImage", essayPanel, new Vector2(.68f, .39f), new Vector2(.94f, .65f), new Color(.13f, .09f, .28f));
        Label("ImageHint", imageCard, Vector2.zero, Vector2.one, 22).text = "Ảnh bài làm\nJPG / PNG";
        imagePreview = MakePreview(imageCard);
        chooseImage = MakeButton("ChooseImage", essayPanel, new Vector2(.06f, .28f), new Vector2(.33f, .36f), "CHỌN ẢNH", ChooseEssayImage);
        takePhoto = MakeButton("TakePhoto", essayPanel, new Vector2(.36f, .28f), new Vector2(.65f, .36f), "CHỤP ẢNH", () => StartCoroutine(OpenCamera()));
        removeImage = MakeButton("RemoveImage", essayPanel, new Vector2(.68f, .28f), new Vector2(.94f, .36f), "XÓA ẢNH", () => { if (pickingImage) CancelImagePicker(); else ClearEssayImage(); });
        essayStatus = Label("EssayFeedback", essayPanel, new Vector2(.06f, .14f), new Vector2(.94f, .26f), 23);
        essayStatus.alignment = TextAlignmentOptions.TopLeft;
        essayCounter = Label("EssayCounter", essayPanel, new Vector2(.06f, .04f), new Vector2(.57f, .13f), 21);
        essayCounter.alignment = TextAlignmentOptions.MidlineLeft;
        essayInput.onValueChanged.AddListener(value => essayCounter.text = value.Length + " / 2.000 ký tự • Enter để xuống dòng");
        essaySubmit = MakeButton("SubmitEssay", essayPanel, new Vector2(.64f, .035f), new Vector2(.94f, .13f), "NỘP BÀI", SubmitEssay);
        BuildEssayResults();
        essayPanel.gameObject.SetActive(false);
    }

    RawImage MakePreview(Transform parent)
    {
        var go = new GameObject("Preview", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
        go.layer = root.gameObject.layer; go.transform.SetParent(parent, false);
        var image = go.GetComponent<RawImage>(); image.raycastTarget = false; image.color = Color.clear;
        go.GetComponent<AspectRatioFitter>().aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        return image;
    }
    void BuildEssayResults()
    {
        essayResults = Box("EssayResults", essayPanel, new Vector2(.06f, .035f), new Vector2(.94f, .86f), new Color(.055f, .03f, .13f));
        essayResults.gameObject.AddComponent<RectMask2D>();
        resultScroll = essayResults.gameObject.AddComponent<ScrollRect>(); resultScroll.horizontal = false;
        resultScroll.viewport = essayResults; resultScroll.movementType = ScrollRect.MovementType.Clamped;
        var body = Box("ResultContent", essayResults, new Vector2(0, 1), Vector2.one, Color.clear);
        body.GetComponent<Image>().raycastTarget = false; body.pivot = new Vector2(.5f, 1);
        var layout = body.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 18, 18); layout.childControlHeight = true; layout.childControlWidth = true; layout.childForceExpandHeight = false;
        body.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        resultBody = Label("ResultBody", body, Vector2.zero, Vector2.one, 25);
        resultBody.enableAutoSizing = false; resultBody.alignment = TextAlignmentOptions.TopLeft;
        resultScroll.content = body; essayResults.gameObject.SetActive(false);
    }

    bool CanEditEssay()
    {
        return essayMode && !essaySubmitting && latest != null && latest.state == "playing" && latest.remaining > 0
            && latest.submissions != null && latest.submissions[latest.seat] == 0;
    }
    void ChooseEssayImage()
    {
        if (!CanEditEssay() || pickingImage) return;
        StartCoroutine(ChooseEssayImageRoutine());
    }
    IEnumerator ChooseEssayImageRoutine()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        pickingImage = true;
        int version = generation, round = essayRound;
        var cancellation = new System.Threading.CancellationTokenSource();
        imagePickerCancellation = cancellation;
        string initialFolder = PlayerPrefs.GetString("PlanetEssay.ImageFolder", Environment.GetFolderPath(Environment.SpecialFolder.MyPictures));
        var selection = PlanetEssayFileDialog.OpenAsync(initialFolder, cancellation.Token);
        while (!selection.IsCompleted) yield return null;
        if (imagePickerCancellation != cancellation) { cancellation.Dispose(); yield break; }
        imagePickerCancellation = null; cancellation.Dispose();
        pickingImage = false;
        if (version != generation || round != essayRound || !CanEditEssay()) yield break;
        if (selection.IsCanceled) yield break;
        if (selection.IsFaulted) { essayError = "Không mở được hộp chọn ảnh. Bạn thử lại nhé."; yield break; }
        try {
            var selected = selection.Result;
            if (selected == null) yield break;
            PlayerPrefs.SetString("PlanetEssay.ImageFolder", Path.GetDirectoryName(selected.Path));
            var texture = new Texture2D(2, 2, TextureFormat.RGB24, false);
            if (!texture.LoadImage(selected.Bytes)) { Destroy(texture); essayError = "Không đọc được ảnh. Hãy chọn JPG hoặc PNG."; yield break; }
            ClearEssayImage(); attachedImage = texture; attachedBase64 = Convert.ToBase64String(selected.Bytes);
            imagePreview.texture = texture; imagePreview.color = Color.white;
            imagePreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;
            essayError = "Đã đính kèm ảnh. Bấm Nộp bài để gửi cho AI chấm.";
        } catch (Exception) { essayError = "Không mở được ảnh. Bạn thử chọn lại nhé."; }
#else
        essayError = "Chọn ảnh hiện hỗ trợ bản Windows. Bạn vẫn có thể nhập chữ hoặc dùng webcam.";
        yield break;
#endif
    }
    void CancelImagePicker()
    {
        if (imagePickerCancellation != null) { imagePickerCancellation.Cancel(); imagePickerCancellation = null; }
        pickingImage = false;
    }
    void AttachEssayImage(Texture source)
    {
        if (!CanEditEssay()) return;
        float scale = Mathf.Min(1f, 1600f / Mathf.Max(source.width, source.height));
        int width = Mathf.Max(1, Mathf.RoundToInt(source.width * scale)), height = Mathf.Max(1, Mathf.RoundToInt(source.height * scale));
        var target = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
        var previous = RenderTexture.active;
        Texture2D texture = null;
        try {
            Graphics.Blit(source, target); RenderTexture.active = target;
            texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0); texture.Apply();
            byte[] bytes = texture.EncodeToJPG(85);
            if (bytes.Length > 2 * 1024 * 1024) { essayError = "Ảnh sau nén lớn hơn 2 MB. Hãy chọn ảnh nhỏ hơn."; Destroy(texture); return; }
            ClearEssayImage(); attachedImage = texture; attachedBase64 = Convert.ToBase64String(bytes);
            imagePreview.texture = texture; imagePreview.color = Color.white;
            imagePreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)width / height;
            essayError = "Đã đính kèm ảnh. Bấm Nộp bài để gửi cho AI chấm.";
        } catch { if (texture != null) Destroy(texture); throw; }
        finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target); }
    }
    void ClearEssayImage()
    {
        attachedBase64 = null;
        if (imagePreview != null) { imagePreview.texture = null; imagePreview.color = Color.clear; }
        if (attachedImage != null) Destroy(attachedImage); attachedImage = null;
    }
    IEnumerator OpenCamera()
    {
        if (!CanEditEssay()) yield break;
        CloseCamera(); int version = cameraGeneration;
        yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
        if (version != cameraGeneration || !CanEditEssay()) yield break;
        if (!Application.HasUserAuthorization(UserAuthorization.WebCam)) { essayError = "Chưa có quyền webcam. Bạn có thể chọn ảnh từ máy."; yield break; }
        var devices = WebCamTexture.devices;
        if (devices.Length == 0) { essayError = "Không tìm thấy webcam. Bạn hãy chọn ảnh từ máy."; yield break; }
        if (cameraPanel == null) {
            cameraPanel = Box("EssayCamera", root, new Vector2(.12f, .07f), new Vector2(.88f, .91f), new Color(.035f, .02f, .1f, .99f));
            cameraStatus = Label("CameraStatus", cameraPanel, new Vector2(.05f, .85f), new Vector2(.95f, .97f), 28);
            var view = Box("CameraView", cameraPanel, new Vector2(.08f, .2f), new Vector2(.92f, .83f), Color.black);
            cameraPreview = MakePreview(view);
            capturePhoto = MakeButton("Capture", cameraPanel, new Vector2(.17f, .04f), new Vector2(.49f, .16f), "CHỤP BÀI", CaptureEssayPhoto);
            MakeButton("CloseCamera", cameraPanel, new Vector2(.53f, .04f), new Vector2(.85f, .16f), "QUAY LẠI", CloseCamera);
        }
        cameraPanel.gameObject.SetActive(true); cameraPanel.SetAsLastSibling();
        cameraStatus.text = "Đưa toàn bộ bài làm vào khung hình, giữ máy và giấy ổn định.";
        try { webcam = new WebCamTexture(devices[0].name, 1600, 1200, 15); webcam.Play(); cameraPreview.texture = webcam; cameraPreview.color = Color.white; }
        catch { CloseCamera(); essayError = "Không mở được webcam. Kiểm tra quyền camera của Windows hoặc chọn ảnh từ máy."; }
    }
    void CaptureEssayPhoto()
    {
        if (!CanEditEssay() || webcam == null || webcam.width <= 16) return;
        // Correct the same rotation and vertical flip used by the live preview.
        int width = webcam.width, height = webcam.height, angle = webcam.videoRotationAngle;
        var pixels = webcam.GetPixels32();
        int outWidth = angle == 90 || angle == 270 ? height : width, outHeight = angle == 90 || angle == 270 ? width : height;
        var corrected = new Color32[pixels.Length];
        for (int y = 0; y < height; y++) for (int x = 0; x < width; x++) {
            int sy = webcam.videoVerticallyMirrored ? height - 1 - y : y;
            int dx = x, dy = y;
            if (angle == 90) { dx = y; dy = width - 1 - x; }
            else if (angle == 180) { dx = width - 1 - x; dy = height - 1 - y; }
            else if (angle == 270) { dx = height - 1 - y; dy = x; }
            corrected[dy * outWidth + dx] = pixels[sy * width + x];
        }
        var photo = new Texture2D(outWidth, outHeight, TextureFormat.RGB24, false);
        try { photo.SetPixels32(corrected); photo.Apply(); AttachEssayImage(photo); }
        catch { essayError = "Chưa chụp được ảnh. Bạn thử lại hoặc chọn ảnh từ máy."; }
        finally { Destroy(photo); CloseCamera(); }
    }
    void CloseCamera()
    {
        cameraGeneration++;
        if (webcam != null) { webcam.Stop(); Destroy(webcam); webcam = null; }
        if (cameraPreview != null) cameraPreview.texture = null;
        if (cameraPanel != null) cameraPanel.gameObject.SetActive(false);
    }

    void ApplyEssay(State state)
    {
        bool playing = state.state == "playing", reveal = state.state == "reveal";
        essayPanel.gameObject.SetActive(playing || reveal);
        if (!playing || state.remaining <= 0) { CloseCamera(); CancelImagePicker(); }
        if (!playing && !reveal) return;
        essayQuestion.text = state.difficulty + " • " + state.text;
        bool newRound = essayRound != state.round;
        if (newRound) {
            essayRound = state.round; EssayInputState.Clear(essayInput); essayError = null; ClearEssayImage(); CloseCamera(); CancelImagePicker();
            essayCounter.text = "0 / 2.000 ký tự • Enter để xuống dòng";
        }
        int submission = state.submissions != null && state.submissions.Length > state.seat ? state.submissions[state.seat] : 0;
        bool editable = playing && state.remaining > 0 && submission == 0 && !essaySubmitting;
        if (!editable) { CloseCamera(); CancelImagePicker(); }
        essayInput.interactable = editable; essaySubmit.interactable = editable;
        chooseImage.interactable = editable && !pickingImage; takePhoto.interactable = editable && !pickingImage; removeImage.interactable = editable && (attachedImage != null || pickingImage);
        essayInput.gameObject.SetActive(!reveal); imageCard.gameObject.SetActive(!reveal);
        chooseImage.gameObject.SetActive(!reveal); takePhoto.gameObject.SetActive(!reveal); removeImage.gameObject.SetActive(!reveal);
        essayStatus.gameObject.SetActive(!reveal); essayCounter.gameObject.SetActive(!reveal); essaySubmit.gameObject.SetActive(!reveal);
        essayResults.gameObject.SetActive(reveal);
        if (reveal && shownResultRound != state.round) {
            shownResultRound = state.round;
            resultBody.text = "KẾT QUẢ CÂU " + state.round + " • " + state.earned + "/10 ĐIỂM\n\n"
                + state.text + "\n\nNHẬN XÉT CỦA AI\n" + state.feedback
                + "\n\nNỘI DUNG BÀI LÀM AI ĐỌC ĐƯỢC\n" + (string.IsNullOrEmpty(state.transcription) ? "Chưa có bài được chấm." : state.transcription)
                + "\n\nĐÁP ÁN THAM KHẢO\n" + state.sampleAnswer
                + (string.IsNullOrEmpty(state.suggestedAnswer) ? "" : "\n\nBÀI GIẢI GỢI Ý CỦA AI\n" + state.suggestedAnswer);
            Canvas.ForceUpdateCanvases(); LayoutRebuilder.ForceRebuildLayoutImmediate(resultScroll.content); resultScroll.verticalNormalizedPosition = 1;
        }
        if (newRound && editable) { essayInput.Select(); essayInput.ActivateInputField(); }
        ButtonText(chooseImage, pickingImage ? "ĐANG CHỌN…" : "CHỌN ẢNH");
        ButtonText(removeImage, pickingImage ? "HỦY CHỌN" : "XÓA ẢNH");
        ButtonText(essaySubmit, submission == 1 || essaySubmitting ? "ĐANG CHẤM…" : submission == 2 ? "ĐÃ NỘP" : "NỘP BÀI");
        essayClock.text = reveal ? "ĐỌC KẾT QUẢ: " + Mathf.CeilToInt(state.revealRemaining) + "s" : state.remaining > 0 ? TimeLabel(state.remaining) : "CHỜ KẾT QUẢ";
        essayStatus.text = reveal ? "+" + state.earned + "/10 • " + state.feedback + "\nTham khảo: " + state.sampleAnswer
            : submission == 1 || essaySubmitting ? "AI đang chấm bài của bạn…"
            : submission == 2 ? "Đã chấm xong bài. Đang chờ đối thủ hoặc hết giờ."
            : state.remaining <= 0 ? "Hết giờ làm bài. Đang tổng hợp kết quả…"
            : !string.IsNullOrEmpty(essayError) ? essayError
            : "Trình bày theo cách hiểu của bạn. AI chấm tối đa 10 điểm/câu theo nội dung, có điểm cho ý đúng.";
    }

    void SubmitEssay()
    {
        if (!essayMode || essaySubmitting || latest == null || latest.state != "playing" || latest.remaining <= 0) return;
        if (string.IsNullOrWhiteSpace(essayInput.text) && string.IsNullOrEmpty(attachedBase64)) { essayError = "Bạn hãy nhập câu trả lời hoặc chọn ảnh bài làm trước khi nộp nhé."; essayInput.Select(); essayInput.ActivateInputField(); return; }
        StartCoroutine(SubmitEssayRoutine(generation, latest.round, essayInput.text.Trim()));
    }
    IEnumerator SubmitEssayRoutine(int version, int round, string answer)
    {
        essaySubmitting = true; essayError = null; ApplyEssay(latest);
        using (var request = new UnityWebRequest(url + "/essay", "POST")) {
            essayRequest = request; request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(new EssayCommand { token = token, text = answer, round = round,
                imageBase64 = attachedBase64, mimeType = string.IsNullOrEmpty(attachedBase64) ? null : "image/jpeg" })));
            request.downloadHandler = new DownloadHandlerBuffer(); request.SetRequestHeader("Content-Type", "application/json"); request.timeout = 30;
            yield return request.SendWebRequest();
            essayRequest = null; essaySubmitting = false;
            if (version != generation) yield break;
            State response = null;
            try { response = JsonUtility.FromJson<State>(request.downloadHandler.text); } catch (Exception) { }
            if (request.result == UnityWebRequest.Result.Success && response != null && !string.IsNullOrEmpty(response.state)) {
                if (latest == null || response.round >= latest.round) Apply(response);
                if (!string.IsNullOrEmpty(response.error) && latest != null && latest.round == round) essayError = response.error;
            } else if (latest != null && latest.round == round && latest.state == "playing") {
                ApplyEssay(latest);
                essayError = response != null && !string.IsNullOrEmpty(response.error) ? response.error : "Chưa nhận được kết quả chấm. Đang kiểm tra lại với máy chủ…";
                essayStatus.text = essayError;
            }
        }
    }

    RectTransform Box(string name, Transform parent, Vector2 min, Vector2 max, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.layer = root.gameObject.layer;
        var rt = (RectTransform)go.transform; rt.SetParent(parent, false);
        rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = color; return rt;
    }
    TMP_Text Label(string name, Transform parent, Vector2 min, Vector2 max, float size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); go.layer = root.gameObject.layer;
        var rt = (RectTransform)go.transform; rt.SetParent(parent, false); rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = rt.offsetMax = Vector2.zero;
        var text = go.GetComponent<TextMeshProUGUI>(); text.font = game.questionText.font;
        text.fontSharedMaterial = game.questionText.fontSharedMaterial; text.fontSize = size;
        text.enableAutoSizing = true; text.fontSizeMin = 14; text.fontSizeMax = size;
        text.alignment = TextAlignmentOptions.Center; text.color = Color.white; text.raycastTarget = false;
        text.richText = false; return text;
    }
    Button MakeButton(string name, Transform parent, Vector2 min, Vector2 max, string text, UnityEngine.Events.UnityAction action)
    {
        var rt = Box(name, parent, min, max, new Color(.36f, .22f, .62f));
        var source = game.resultPanel.GetComponent<Image>();
        if (source != null) { rt.GetComponent<Image>().sprite = source.sprite; rt.GetComponent<Image>().type = source.type; }
        var button = rt.gameObject.AddComponent<Button>(); button.targetGraphic = rt.GetComponent<Image>();
        Label("Label", rt, new Vector2(.04f, .06f), new Vector2(.96f, .94f), 28).text = text;
        button.onClick.AddListener(action); return button;
    }
    void ButtonText(Button button, string text) { button.GetComponentInChildren<TMP_Text>().text = text; }
    void Screen(string state, string title, string message, string action)
    {
        if (askAfterMatch != null) askAfterMatch.gameObject.SetActive(essayMode && state == "finished" && latest != null && latest.reviewQuestions != null && latest.reviewQuestions.Length > 0);
        var detailRect = detail.rectTransform;
        detailRect.anchorMin = new Vector2(.08f, essayMode && state == "finished" ? .34f : .25f);
        mode = state; overlay.gameObject.SetActive(true); overlay.SetAsLastSibling();
        heading.text = title; detail.text = message; badge.text = essayMode ? "PLANET • TỰ LUẬN • AI CHẤM BÀI" : "PLANET • ĐỐI KHÁNG";
        primary.gameObject.SetActive(!string.IsNullOrEmpty(action)); ButtonText(primary, action);
        primary.interactable = state != "searching" || !pendingBot;
        ButtonText(secondary, state == "searching" || state == "countdown" ? "Hủy tìm" : "Về trang chủ");
    }

    void BeginSearch()
    {
        var chat = GetComponent<ScienceChatController>(); if (chat != null) chat.ClearReviews();
        if (reviewPicker != null) { Destroy(reviewPicker.gameObject); reviewPicker = null; }
        foreach (var drift in root.GetComponentsInChildren<BlobDrift>(true)) drift.enabled = false;
        StopConnection(); game.StopMatch(); ClearProjectiles(); game.resultPanel.SetActive(false);
        local = false; pendingFire = false; pendingBot = false; pendingAnswer = -1; latest = null; game.OnlineControlled = true;
        historyMatchId = null;
        matchUsername = string.IsNullOrWhiteSpace(GameSession.Username) ? PlayerPrefs.GetString("Username", "") : GameSession.Username;
        essayRound = -1; shownResultRound = -1; ClearEssayImage(); CloseCamera(); essayPanel.gameObject.SetActive(false);
        foreach (var image in game.blobImages) image.gameObject.SetActive(!essayMode);
        game.questionText.gameObject.SetActive(!essayMode);
        shoot.gameObject.SetActive(false); hud.gameObject.SetActive(true);
        for (int i = 0; i < 4; i++) {
            game.blobImages[i].rectTransform.position = root.TransformPoint(initialTargets[i]);
            game.blobImages[i].color = game.normalColor; game.blobTexts[i].text = "?";
        }
        foreach (var score in game.scoreTexts) score.text = "0";
        game.questionText.text = essayMode ? "Tự luận • 9 câu: mỗi môn 3 câu" : "Tổng hợp 3 môn • 15 câu: 5 Vật lý, 5 Hóa học, 5 Sinh học";
        game.timerText.text = "—";
        Screen("searching", "TÌM ĐỐI THỦ", "Đang kết nối…", "Chơi với máy ngay");
        var asset = Resources.Load<TextAsset>("PlanetDuelSettings");
        url = asset == null ? "http://localhost:8787" : JsonUtility.FromJson<Settings>(asset.text).serverUrl;
        url = url.TrimEnd('/');
        loop = StartCoroutine(Connect(generation));
    }

    IEnumerator Connect(int version)
    {
        Canvas.ForceUpdateCanvases();
        var body = new Join { name = GameSettingsController.DisplayName,
            username = matchUsername, subject = "Mixed", questionType = essayMode ? "essay" : "multipleChoice", difficulties = GameSession.Difficulties.ToArray(), guns = new Vector2[2], targets = new Vector2[4] };
        for (int i = 0; i < 2; i++) body.guns[i] = root.InverseTransformPoint(game.guns[i].position);
        Array.Copy(initialTargets, body.targets, 4);
        State response = null; string error = null;
        yield return Request("/join", JsonUtility.ToJson(body), (s, e) => { response = s; error = e; });
        if (version != generation) yield break;
        if (error != null || response == null) { ConnectionError(error); yield break; }
        token = response.token; lastGood = Time.realtimeSinceStartup; Apply(response);
        while (version == generation && !string.IsNullOrEmpty(token)) {
            yield return new WaitForSecondsRealtime(.08f);
            bool fire = pendingFire; pendingFire = false;
            int answer = pendingAnswer, round = pendingRound; pendingAnswer = -1;
            bool switchingToBot = pendingBot;
            yield return Request(switchingToBot ? "/bot" : "/poll", JsonUtility.ToJson(new Command { token = token, fire = fire, answer = answer, round = round }), (s, e) => { response = s; error = e; });
            if (version != generation) yield break;
            if (switchingToBot) {
                pendingBot = false;
                if (error != null || response == null || response.state == "searching") {
                    if (!essayMode) { StartLocalBot(); yield break; }
                    ConnectionError(error ?? "Máy chủ chưa xác nhận trận với máy. Hãy cập nhật và khởi động lại PlanetServer.");
                    yield break;
                }
                if (response.state != "countdown" && response.state != "playing" && response.state != "reveal" && response.state != "finished") {
                    ConnectionError("Chưa chuyển được sang trận với máy. Bạn thử kết nối lại nhé."); yield break;
                }
            }
            if (error != null || response == null) {
                shoot.interactable = false;
                hudText.text = "Mất kết nối — đang thử lại…";
                if (Time.realtimeSinceStartup - lastGood > 10f) { ConnectionError(error); yield break; }
                continue;
            }
            lastGood = Time.realtimeSinceStartup; shoot.interactable = true;
            Apply(response);
            if (response.state == "finished") yield break;
        }
    }

    IEnumerator Request(string path, string json, Action<State, string> done)
    {
        using (var request = new UnityWebRequest(url + path, "POST")) {
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer(); request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = path == "/join" && essayMode ? 20 : 5; activeRequest = request;
            yield return request.SendWebRequest(); activeRequest = null;
            State state = null;
            try { state = JsonUtility.FromJson<State>(request.downloadHandler.text); } catch (Exception) { }
            if (path == "/bot" && request.responseCode == 404) done(null, "Máy chủ đang chạy bản cũ, chưa có nút chơi máy ngay. Hãy tắt server cũ rồi chạy lại npm start trong PlanetServer.");
            else if (request.result != UnityWebRequest.Result.Success) done(null, state != null && !string.IsNullOrEmpty(state.error) ? state.error : "Không kết nối được máy chủ.");
            else done(state, state == null ? "Phản hồi máy chủ không hợp lệ." : state.error);
        }
    }

    void Apply(State state)
    {
        latest = state;
        if (state.state != "searching") pendingBot = false;
        if (essayMode) ApplyEssay(state);
        if (state.state == "searching") {
            Screen("searching", "TÌM ĐỐI THỦ", essayMode ? "Tự luận • 9 câu • Mỗi môn 3 câu\nDễ: 60s • Trung bình: 120s • Khó: 240s" : "Tổng hợp 15 câu • Mỗi môn 5 câu", pendingBot ? "Đang chuyển…" : "Chơi với máy ngay");
            detail.text += "\n" + TimeLabel(state.waited) + " / " + TimeLabel(state.waitLimit) + "\nBạn có thể chơi với máy ngay hoặc tiếp tục chờ.\nHết 2 phút sẽ tự ghép với Máy Planet.";
            return;
        }
        string you = state.names[state.seat], opponent = state.names[1 - state.seat];
        hudText.text = you + "  VS  " + opponent + "\nCâu " + state.round + "/" + state.total + (state.bot ? " • Đấu với máy" : " • Online");
        foreach (var hint in hints) {
            if (hint.name == "P1Keys") hint.text = state.seat == 0 ? "Bạn • Bấm hành tinh để chọn" : "Đối thủ";
            if (hint.name == "P2Keys") hint.text = state.seat == 1 ? "Bạn • Bấm hành tinh để chọn" : state.bot ? "Máy Planet" : "Đối thủ";
        }
        if (state.state == "countdown") {
            Screen("countdown", state.bot ? "ĐẤU VỚI MÁY" : "ĐÃ TÌM THẤY ĐỐI THỦ", you + "  VS  " + opponent + "\nBắt đầu sau " + Mathf.CeilToInt(state.countdown), null);
            badge.text = state.bot ? (essayMode ? "MÁY PLANET • ĐIỂM ĐỐI THỦ MÔ PHỎNG" : "MÁY PLANET SẴN SÀNG") : "HAI PHI THUYỀN ĐÃ SẴN SÀNG";
        } else if (state.state == "playing" || state.state == "reveal") {
            if (mode != "confirmExit") { mode = "playing"; overlay.gameObject.SetActive(false); }
            shoot.gameObject.SetActive(false);
        }
        game.questionText.text = state.text; game.timerText.text = Mathf.CeilToInt(state.remaining).ToString();
        for (int i = 0; i < 2; i++) {
            game.scoreTexts[i].text = state.scores[i].ToString(); game.guns[i].localRotation = Quaternion.Euler(0, 0, state.angles[i]);
            game.guns[i].GetComponent<Image>().color = state.stunned[i] ? game.stunColor : Color.white;
        }
        for (int i = 0; !essayMode && i < 4; i++) {
            game.blobTexts[i].text = state.options[i];
            if (state.state == "countdown") game.blobImages[i].rectTransform.position = root.TransformPoint(state.targets[i]);
            game.blobImages[i].color = state.correct == i ? game.correctColor : state.shot[i] ? game.wrongColor : game.normalColor;
        }
        RenderBullets(state.bullets);
        if (state.state == "finished") {
            if (string.IsNullOrEmpty(historyMatchId)) {
                historyMatchId = state.matchId;
                if (!string.IsNullOrEmpty(historyMatchId)) PlanetHistorySync.Enqueue(new PlanetMatchRecord {
                    matchId = historyMatchId, finishedAt = state.finishedAt, username = matchUsername, opponent = opponent,
                    result = state.winner < 0 ? "HÒA" : state.winner == state.seat ? "THẮNG" : "THUA",
                    score = state.scores[state.seat], maxScore = state.maxScore,
                    questionType = essayMode ? "essay" : "multipleChoice"
                });
                else Debug.LogError("Máy chủ Planet chưa có mã trận để lưu lịch sử. Hãy khởi động lại PlanetServer bản mới.");
            }
            shoot.gameObject.SetActive(false);
            string outcome = state.winner < 0 ? "HÒA NHAU!" : state.winner == state.seat ? "BẠN CHIẾN THẮNG!" : "CỐ GẮNG Ở TRẬN SAU!";
            Screen("finished", outcome, you + ": " + state.scores[state.seat] + "   —   " + opponent + ": " + state.scores[1 - state.seat] + "\n" + state.reason, "Tìm trận mới");
            badge.text = state.bot ? "KẾT QUẢ • ĐẤU VỚI MÁY" : "KẾT QUẢ • ĐẤU ONLINE";
        }
    }

    void RenderBullets(Shot[] shots)
    {
        var alive = new HashSet<int>();
        if (shots != null) foreach (var shot in shots) {
            alive.Add(shot.id);
            if (!projectiles.TryGetValue(shot.id, out var rt)) {
                rt = Box("OnlineBullet", root, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Color.white);
                rt.sizeDelta = game.bulletSize; var image = rt.GetComponent<Image>(); image.sprite = game.bulletSprite;
                image.raycastTarget = false; image.preserveAspect = true; projectiles.Add(shot.id, rt);
            }
            rt.position = root.TransformPoint(new Vector2(shot.x, shot.y));
            rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(shot.dy, shot.dx) * Mathf.Rad2Deg + game.bulletAngleOffset);
            rt.SetSiblingIndex(hud.GetSiblingIndex());
        }
        var removed = new List<int>();
        foreach (var pair in projectiles) if (!alive.Contains(pair.Key)) { Destroy(pair.Value.gameObject); removed.Add(pair.Key); }
        foreach (var id in removed) projectiles.Remove(id);
    }
    void ClearProjectiles() { foreach (var rt in projectiles.Values) if (rt != null) Destroy(rt.gameObject); projectiles.Clear(); }
    string TimeLabel(float seconds) { int s = Mathf.FloorToInt(seconds); return (s / 60).ToString("00") + ":" + (s % 60).ToString("00"); }

    void Update()
    {
        if (exitConfirmation != null && exitConfirmation.gameObject.activeSelf) exitConfirmation.SetAsLastSibling();
        if (mode == "finished" && !string.IsNullOrEmpty(historyMatchId))
            badge.text = PlanetHistorySync.Status(historyMatchId, matchUsername);
        if (webcam != null && cameraPreview != null && webcam.width > 16) {
            cameraPreview.rectTransform.localEulerAngles = new Vector3(0, 0, -webcam.videoRotationAngle);
            cameraPreview.uvRect = webcam.videoVerticallyMirrored ? new Rect(0, 1, 1, -1) : new Rect(0, 0, 1, 1);
            cameraPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)webcam.width / webcam.height;
            if (capturePhoto != null) capturePhoto.interactable = CanEditEssay();
        }
        if (!essayMode && !local && latest != null && latest.targets != null && (latest.state == "playing" || latest.state == "reveal"))
            for (int i = 0; i < 4; i++) game.blobImages[i].rectTransform.position = Vector3.Lerp(
                game.blobImages[i].rectTransform.position, root.TransformPoint(latest.targets[i]), 1f - Mathf.Exp(-25f * Time.unscaledDeltaTime));
        if (mode == "searching" && latest != null && latest.state == "searching") {
            float waited = latest.waited + Mathf.Min(5, Time.realtimeSinceStartup - lastGood);
            badge.text = "TÌM TRẬN • " + TimeLabel(Mathf.Min(waited, latest.waitLimit));
        }
    }
    void ChooseAnswer(int answer)
    {
        if (mode != "playing" || overlay.gameObject.activeSelf) return;
        if (local) { game.ChooseLocal(answer); return; }
        if (latest == null || latest.state != "playing" || latest.stunned[latest.seat] || latest.shot[answer] ||
            pendingAnswer >= 0 || Time.realtimeSinceStartup - lastGood > 2f) return;
        pendingAnswer = answer; pendingRound = latest.round;
    }
    void ConnectionError(string message)
    {
        StopConnection(); ClearProjectiles(); shoot.gameObject.SetActive(false);
        if (essayPanel != null) essayPanel.gameObject.SetActive(false);
        Screen("error", "CHƯA THỂ KẾT NỐI", (message ?? "Máy chủ chưa sẵn sàng.") + (essayMode ? "\nTự luận cần kết nối máy chủ để AI chấm bài." : "\nBạn có thể chơi với máy ngay hoặc quay lại để tìm trận sau."), essayMode ? "Kết nối lại" : "Chơi với máy");
    }
    void Primary()
    {
        if (mode == "searching") {
            if (pendingBot) return;
            pendingBot = true;
            primary.interactable = false; ButtonText(primary, "Đang chuyển…");
            return;
        }
        if (mode == "error") { if (essayMode) BeginSearch(); else StartLocalBot(); return; }
        BeginSearch();
    }
    void ConfirmExit()
    {
        CloseCamera(); CancelImagePicker();
        exitConfirmation.gameObject.SetActive(true); exitConfirmation.SetAsLastSibling();
    }
    void StartLocalBot()
    {
        StopConnection();
        historyMatchId = null;
        var bank = JsonUtility.FromJson<Bank>(Resources.Load<TextAsset>("PlanetDuelQuestions").text);
        var questions = new List<Question>();
        foreach (string subject in new[] { "Physics", "Chemistry", "Biology" }) {
            var pool = new List<BankQuestion>();
            foreach (var q in bank.questions) if (q.subject == subject &&
                (GameSession.Difficulties.Count == 0 ? q.difficulty == "Dễ" : GameSession.Difficulties.Contains(q.difficulty))) pool.Add(q);
            if (pool.Count < 5) { Screen("error", "CHƯA ĐỦ CÂU HỎI", "Mỗi môn cần ít nhất 5 câu hỏi.", null); return; }
            for (int i = 0; i < 5; i++) {
                int j = UnityEngine.Random.Range(i, pool.Count); var q = pool[j]; pool[j] = pool[i]; pool[i] = q;
                questions.Add(new Question { text = q.text, options = q.options, correct = q.correct });
            }
        }
        for (int i = 0; i < questions.Count; i++) { int j = UnityEngine.Random.Range(i, questions.Count); var q = questions[i]; questions[i] = questions[j]; questions[j] = q; }
        local = true; game.OnlineControlled = false; game.PlayWithBot = true;
        game.questionsPerGame = 15;
        game.roundTime = 20f;
        foreach (var image in game.blobImages) {
            var drift = image.GetComponent<BlobDrift>();
            if (drift == null) drift = image.gameObject.AddComponent<BlobDrift>();
            drift.wanderInterval = 2f;
            drift.enabled = true;
        }
        mode = "playing"; overlay.gameObject.SetActive(false); shoot.gameObject.SetActive(false);
        hudText.text = "Bạn VS Máy Planet • Tổng hợp 3 môn • 15 câu";
        foreach (var hint in hints) { if (hint.name == "P1Keys") hint.text = "Bạn • Bấm hành tinh để chọn"; if (hint.name == "P2Keys") hint.text = "Máy Planet"; }
        game.StartGame(questions);
    }
    static string SubjectKey(string value)
    {
        string s = (value ?? "").ToLowerInvariant();
        if (s.Contains("chem") || s.Contains("hóa") || s.Contains("hoá")) return "Chemistry";
        if (s.Contains("bio") || s.Contains("sinh")) return "Biology";
        return "Physics";
    }
    void LocalFinished(int p1, int p2)
    {
        if (string.IsNullOrEmpty(historyMatchId)) {
            historyMatchId = "local-" + Guid.NewGuid().ToString("N");
            PlanetHistorySync.Enqueue(new PlanetMatchRecord {
                matchId = historyMatchId, username = matchUsername, opponent = "Máy Planet",
                result = p1 == p2 ? "HÒA" : p1 > p2 ? "THẮNG" : "THUA", score = p1,
                maxScore = game.questionsPerGame * (10 + Mathf.RoundToInt(game.roundTime)), questionType = "multipleChoice"
            });
        }
        shoot.gameObject.SetActive(false);
        Screen("finished", p1 == p2 ? "HÒA NHAU!" : p1 > p2 ? "BẠN CHIẾN THẮNG!" : "CỐ GẮNG Ở TRẬN SAU!", "Bạn: " + p1 + "   —   Máy Planet: " + p2, "Tìm trận mới");
    }
    void ReturnToMenu()
    {
        StopConnection(); game.StopMatch(); ClearProjectiles();
        exitConfirmation.gameObject.SetActive(false);
        var homes = UnityEngine.Object.FindObjectsByType<HomeMenuController>(FindObjectsInactive.Include);
        root.gameObject.SetActive(false);
        if (homes.Length > 0) homes[0].BackToHome();
    }
    void OpenReviewPicker()
    {
        if (!essayMode || latest == null || latest.state != "finished" || latest.reviewQuestions == null) return;
        if (reviewPicker != null) Destroy(reviewPicker.gameObject);
        reviewPicker = Box("ReviewQuestions", root, Vector2.zero, Vector2.one, new Color(.035f, .02f, .09f, .99f));
        Label("Title", reviewPicker, new Vector2(.1f, .86f), new Vector2(.8f, .97f), 34).text = "CHỌN CÂU ĐỂ HỎI AI THÊM";
        MakeButton("Close", reviewPicker, new Vector2(.82f, .87f), new Vector2(.96f, .96f), "Đóng", () => reviewPicker.gameObject.SetActive(false));
        foreach (var entry in latest.reviewQuestions) {
            var questionReview = entry;
            int index = entry.round - 1, column = index % 3, row = index / 3;
            float left = .07f + column * .30f, top = .78f - row * .23f;
            MakeButton("Question" + entry.round, reviewPicker, new Vector2(left, top - .18f), new Vector2(left + .27f, top),
                "Câu " + entry.round + " • " + entry.earned + "/10\n" + entry.difficulty,
                () => ScienceChatController.ForReview(this).OpenReview(latest.matchId + ":" + questionReview.round,
                    "Câu " + questionReview.round + " • " + questionReview.text,
                    "Đề: " + questionReview.text + "\nĐiểm: " + questionReview.earned + "/10\nBài làm học sinh (AI đọc): " + questionReview.transcription
                        + "\nNhận xét: " + questionReview.feedback + "\nĐáp án tham khảo: " + questionReview.sampleAnswer + "\nGiải thích: " + questionReview.suggestedAnswer,
                    new Color(.085f, .04f, .19f)));
        }
        reviewPicker.SetAsLastSibling();
    }
    void StopConnection()
    {
        generation++;
        CloseCamera();
        CancelImagePicker();
        if (essayRequest != null) { essayRequest.Abort(); essayRequest = null; }
        essaySubmitting = false;
        if (activeRequest != null) { activeRequest.Abort(); activeRequest = null; }
        if (loop != null) { StopCoroutine(loop); loop = null; }
        if (!string.IsNullOrEmpty(token)) {
            // Detached request survives deactivation; server heartbeat expires abandoned sessions too.
            var request = new UnityWebRequest(url + "/leave", "POST");
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(new Command { token = token })));
            request.downloadHandler = new DownloadHandlerBuffer(); request.SetRequestHeader("Content-Type", "application/json"); request.timeout = 3;
            var operation = request.SendWebRequest(); operation.completed += _ => request.Dispose(); token = null;
        }
    }
    void OnDisable()
    {
        if (reviewPicker != null) reviewPicker.gameObject.SetActive(false);
        if (exitConfirmation != null) exitConfirmation.gameObject.SetActive(false);
        StopConnection(); ClearProjectiles();
        if (game != null) { game.StopMatch(); game.MatchFinished = null; game.OnlineControlled = true; }
        foreach (var animation in previouslyEnabled) if (animation != null) animation.enabled = true;
        if (overlay != null) overlay.gameObject.SetActive(false);
        if (hud != null) hud.gameObject.SetActive(false);
        if (essayPanel != null) essayPanel.gameObject.SetActive(false);
        ClearEssayImage();
        if (game != null) {
            foreach (var image in game.blobImages) if (image != null) image.gameObject.SetActive(true);
            if (game.questionText != null) game.questionText.gameObject.SetActive(true);
        }
    }
}

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
// A separate Windows process owns the dialog and decodes large images.
// It never receives a Unity HWND or calls back into Unity's window thread.
static class PlanetEssayFileDialog
{
    public sealed class Selection
    {
        public string Path; public byte[] Bytes;
    }
    public static string BuildScript(string initialDirectory)
    {
        // Encode path data separately; folder names cannot become PowerShell code.
        string folder = Convert.ToBase64String(Encoding.UTF8.GetBytes(initialDirectory ?? ""));
        return @"$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
$dialog = New-Object System.Windows.Forms.OpenFileDialog
$owner = New-Object System.Windows.Forms.Form
$source = $null; $bitmap = $null; $graphics = $null; $stream = $null
try {
    $dialog.Title = 'Chọn ảnh bài tự luận'
    $dialog.Filter = 'Ảnh bài làm (JPG, PNG)|*.jpg;*.jpeg;*.png'
    $dialog.Multiselect = $false; $dialog.CheckFileExists = $true; $dialog.RestoreDirectory = $true
    $folder = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('__FOLDER__'))
    if ([IO.Directory]::Exists($folder)) { $dialog.InitialDirectory = $folder }
    else { $dialog.InitialDirectory = [Environment]::GetFolderPath('MyPictures') }
    $owner.ShowInTaskbar = $false; $owner.TopMost = $true; $owner.Opacity = 0
    $owner.Width = 1; $owner.Height = 1
    $owner.Show()
    if ($dialog.ShowDialog($owner) -ne [Windows.Forms.DialogResult]::OK) { exit 0 }
    $selectedPath = $dialog.FileName
    if ((Get-Item -LiteralPath $selectedPath).Length -gt 10MB) { throw 'Ảnh gốc phải nhỏ hơn 10 MB.' }
    $source = [Drawing.Image]::FromFile($selectedPath)
    if ($source.Width -gt 12000 -or $source.Height -gt 12000) { throw 'Độ phân giải ảnh quá lớn.' }
    if ($source.PropertyIdList -contains 274) {
        $orientation = [BitConverter]::ToUInt16($source.GetPropertyItem(274).Value, 0)
        switch ($orientation) {
            2 { $source.RotateFlip([Drawing.RotateFlipType]::RotateNoneFlipX) }
            3 { $source.RotateFlip([Drawing.RotateFlipType]::Rotate180FlipNone) }
            4 { $source.RotateFlip([Drawing.RotateFlipType]::Rotate180FlipX) }
            5 { $source.RotateFlip([Drawing.RotateFlipType]::Rotate90FlipX) }
            6 { $source.RotateFlip([Drawing.RotateFlipType]::Rotate90FlipNone) }
            7 { $source.RotateFlip([Drawing.RotateFlipType]::Rotate270FlipX) }
            8 { $source.RotateFlip([Drawing.RotateFlipType]::Rotate270FlipNone) }
        }
    }
    $scale = [Math]::Min(1.0, 1600.0 / [Math]::Max($source.Width, $source.Height))
    $width = [Math]::Max(1, [int][Math]::Round($source.Width * $scale))
    $height = [Math]::Max(1, [int][Math]::Round($source.Height * $scale))
    $bitmap = New-Object Drawing.Bitmap($width, $height)
    $graphics = [Drawing.Graphics]::FromImage($bitmap)
    $graphics.Clear([Drawing.Color]::White)
    $graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.DrawImage($source, 0, 0, $width, $height)
    $stream = New-Object IO.MemoryStream
    $bitmap.Save($stream, [Drawing.Imaging.ImageFormat]::Jpeg)
    $bytes = $stream.ToArray()
    if ($bytes.Length -gt 2MB) { throw 'Ảnh sau nén lớn hơn 2 MB.' }
    [Console]::Out.WriteLine([Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($selectedPath)))
    [Console]::Out.WriteLine([Convert]::ToBase64String($bytes))
} catch {
    [Console]::Error.WriteLine('Không mở hoặc xử lý được ảnh đã chọn.'); exit 1
} finally {
    if ($graphics) { $graphics.Dispose() }; if ($bitmap) { $bitmap.Dispose() }
    if ($source) { $source.Dispose() }; if ($stream) { $stream.Dispose() }
    $dialog.Dispose(); $owner.Dispose()
}".Replace("__FOLDER__", folder);
    }
    public static System.Threading.Tasks.Task<Selection> OpenAsync(string initialDirectory, System.Threading.CancellationToken cancellation)
    {
        string script = BuildScript(initialDirectory);
        return System.Threading.Tasks.Task.Run(() => {
            cancellation.ThrowIfCancellationRequested();
            var start = new System.Diagnostics.ProcessStartInfo {
                FileName = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"WindowsPowerShell\v1.0\powershell.exe"),
                Arguments = "-NoLogo -NoProfile -STA -EncodedCommand " + Convert.ToBase64String(Encoding.Unicode.GetBytes(script)),
                UseShellExecute = false, CreateNoWindow = true, WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true, RedirectStandardError = true
            };
            using (var process = new System.Diagnostics.Process { StartInfo = start }) {
                process.Start();
                var output = process.StandardOutput.ReadToEndAsync();
                var errors = process.StandardError.ReadToEndAsync();
                while (!process.WaitForExit(100)) {
                    if (cancellation.IsCancellationRequested) {
                        try { process.Kill(); } catch (InvalidOperationException) { }
                        process.WaitForExit(); cancellation.ThrowIfCancellationRequested();
                    }
                }
                cancellation.ThrowIfCancellationRequested();
                string payload = output.GetAwaiter().GetResult();
                errors.GetAwaiter().GetResult();
                if (process.ExitCode != 0) throw new IOException("Không mở hoặc xử lý được ảnh đã chọn.");
                if (string.IsNullOrWhiteSpace(payload)) return null;
                string[] lines = payload.Trim().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length != 2 || lines[1].Length > 2800000) throw new IOException("Dữ liệu ảnh không hợp lệ.");
                byte[] bytes = Convert.FromBase64String(lines[1]);
                if (bytes.Length > 2 * 1024 * 1024 || bytes.Length < 3 || bytes[0] != 255 || bytes[1] != 216) throw new IOException("Ảnh JPEG không hợp lệ.");
                return new Selection { Path = Encoding.UTF8.GetString(Convert.FromBase64String(lines[0])), Bytes = bytes };
            }
        }, cancellation);
    }
}
#endif
