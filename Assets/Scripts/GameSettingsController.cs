using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Device preferences are independent of login credentials and match history.
public class GameSettingsController : MonoBehaviour
{
    const float DesignWidth = 1920;
    const float DesignHeight = 1080;
    const string VolumeKey = "Settings.Volume";
    const string BrightnessKey = "Settings.Brightness";
    const string MutedKey = "Settings.Muted";
    const string AudibleVolumeKey = "Settings.LastAudibleVolume";
    static GameSettingsController instance;
    GameObject screen;
    Image shade;
    TMP_FontAsset font;
    Sprite roundedSprite, circleSprite;
    Texture2D roundedTexture, circleTexture;
    Slider volumeSlider, brightnessSlider;
    TMP_InputField nickname;
    TextMeshProUGUI volumeLabel, brightnessLabel, message;
    float savedVolume, savedBrightness;
    float currentVolume;
    bool muted;
    bool mutedAtOpen;
    public static event Action<bool> SoundStateChanged;
    public static bool IsSilent => instance != null
        ? instance.muted || instance.currentVolume <= .0001f
        : PlayerPrefs.GetInt(MutedKey, 0) == 1 || PlayerPrefs.GetFloat(VolumeKey, .7f) <= .0001f;
    string accountAtOpen;
    GameObject previousSelection;

    static string Account => !string.IsNullOrWhiteSpace(GameSession.Username)
        ? GameSession.Username : PlayerPrefs.GetString("Username", "");
    static string NicknameKey(string account) => "Settings.Nickname." + account;
    public static string DisplayName {
        get {
            string alias = PlayerPrefs.GetString(NicknameKey(Account), "");
            if (!string.IsNullOrWhiteSpace(alias)) return alias;
            return !string.IsNullOrWhiteSpace(GameSession.Fullname)
                ? GameSession.Fullname : PlayerPrefs.GetString("Fullname", "Người chơi");
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize() { EnsureInstance(); }

    static void EnsureInstance()
    {
        if (instance != null) return;
        var go = new GameObject("Game Settings");
        instance = go.AddComponent<GameSettingsController>();
        DontDestroyOnLoad(go);
    }

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        savedVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, .7f));
        muted = PlayerPrefs.GetInt(MutedKey, 0) == 1;
        savedBrightness = Mathf.Clamp(PlayerPrefs.GetFloat(BrightnessKey, .8f), .2f, 1f);
        var overlay = CanvasRoot("Game Brightness", 32000);
        shade = Box(overlay.transform, "Brightness", 0, 0, 1672, 941, Color.black);
        shade.rectTransform.anchorMin = Vector2.zero;
        shade.rectTransform.anchorMax = Vector2.one;
        shade.rectTransform.offsetMin = shade.rectTransform.offsetMax = Vector2.zero;
        shade.raycastTarget = false;
        Apply(savedVolume, savedBrightness);
        SceneManager.sceneLoaded += SceneLoaded;
    }

    void SceneLoaded(Scene scene, LoadSceneMode mode) { if (screen != null && screen.activeSelf) Close(); }

    public static void Open()
    {
        EnsureInstance();
        instance.Show();
    }

    public static void BindSoundButton(Button button)
    {
        EnsureInstance();
        if (button.GetComponent<SoundToggleView>() == null) button.gameObject.AddComponent<SoundToggleView>();
    }

    public static void ToggleSound()
    {
        EnsureInstance();
        if (IsSilent) {
            instance.muted = false;
            // A saved slider value of zero must still be recoverable with the quick toggle.
            if (instance.savedVolume <= .0001f) {
                instance.savedVolume = Mathf.Clamp(PlayerPrefs.GetFloat(AudibleVolumeKey, .7f), .01f, 1f);
                PlayerPrefs.SetFloat(VolumeKey, instance.savedVolume);
            }
        } else instance.muted = true;
        PlayerPrefs.SetInt(MutedKey, instance.muted ? 1 : 0);
        PlayerPrefs.Save();
        instance.Apply(instance.savedVolume, instance.savedBrightness);
    }

    void Show()
    {
        if (screen == null) Build();
        if (screen.activeSelf) return;
        accountAtOpen = Account;
        mutedAtOpen = muted;
        previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        volumeSlider.SetValueWithoutNotify(savedVolume);
        brightnessSlider.SetValueWithoutNotify(savedBrightness);
        nickname.SetTextWithoutNotify(DisplayName);
        message.text = "";
        UpdatePreview();
        screen.SetActive(true);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(nickname.gameObject);
    }

    void Apply(float volume, float brightness)
    {
        currentVolume = volume;
        AudioListener.volume = muted ? 0 : volume;
        SoundStateChanged?.Invoke(IsSilent);
        // Desktop brightness affects only the game, including overlay UI.
        shade.color = new Color(0, 0, 0, (1f - brightness) * .8f);
    }

    void UpdatePreview()
    {
        Apply(volumeSlider.value, brightnessSlider.value);
        volumeLabel.text = Mathf.RoundToInt(volumeSlider.value * 100) + "%";
        brightnessLabel.text = Mathf.RoundToInt(brightnessSlider.value * 100) + "%";
    }

    void Save()
    {
        if (Account != accountAtOpen) { Close(); return; }
        string value = nickname.text.Trim();
        if (value.Length < 2 || value.Length > 24 || Array.Exists(value.ToCharArray(), char.IsControl)) {
            message.text = "Bí danh cần từ 2 đến 24 ký tự, không chứa ký tự xuống dòng.";
            return;
        }
        savedVolume = volumeSlider.value;
        savedBrightness = brightnessSlider.value;
        PlayerPrefs.SetFloat(VolumeKey, savedVolume);
        PlayerPrefs.SetFloat(BrightnessKey, savedBrightness);
        PlayerPrefs.SetInt(MutedKey, muted ? 1 : 0);
        if (savedVolume > .0001f) PlayerPrefs.SetFloat(AudibleVolumeKey, savedVolume);
        PlayerPrefs.SetString(NicknameKey(accountAtOpen), value);
        PlayerPrefs.Save();
        mutedAtOpen = muted;
        AutoDisplayName.Refresh();
        message.text = "Đã lưu cài đặt!";
        nickname.SetTextWithoutNotify(value);
    }

    public void Close()
    {
        if (screen != null && screen.activeSelf) muted = mutedAtOpen;
        Apply(savedVolume, savedBrightness);
        if (screen != null) screen.SetActive(false);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(previousSelection);
    }

    void Build()
    {
        var sourceFont = Resources.Load<Font>("ScienceChatFont");
        if (sourceFont != null) {
            font = TMP_FontAsset.CreateFontAsset(sourceFont);
            font.isMultiAtlasTexturesEnabled = true;
        }
        roundedSprite = MakeShape(16, out roundedTexture);
        circleSprite = MakeShape(32, out circleTexture);
        screen = CanvasRoot("Settings Page", 1100);
        // A single landscape background fills the screen; no duplicated artwork or modal dimming.
        var background = new GameObject("Space Background", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
        background.transform.SetParent(screen.transform, false);
        background.GetComponent<RawImage>().texture = Resources.Load<Texture2D>("SettingsBackground");
        background.GetComponent<RawImage>().raycastTarget = true;
        var backgroundFit = background.GetComponent<AspectRatioFitter>();
        backgroundFit.aspectRatio = DesignWidth / DesignHeight;
        backgroundFit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        var page = new GameObject("Safe Layout", typeof(RectTransform), typeof(AspectRatioFitter));
        page.transform.SetParent(screen.transform, false);
        var fitter = page.GetComponent<AspectRatioFitter>();
        fitter.aspectRatio = DesignWidth / DesignHeight;
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        var content = new GameObject("Design", typeof(RectTransform));
        content.transform.SetParent(page.transform, false);
        var design = content.GetComponent<RectTransform>();
        design.anchorMin = design.anchorMax = new Vector2(.5f, .5f);
        design.sizeDelta = new Vector2(DesignWidth, DesignHeight);
        page.AddComponent<SettingsPageSizing>().design = design;
        var root = content.transform;

        var back = ActionButton(root, "Back", 100, 68, 230, 64,
            new Color(.95f, .98f, 1), "‹  Quay lại", 25, Close);
        back.GetComponentInChildren<TextMeshProUGUI>().color = new Color(.08f, .2f, .49f);
        var titleShadow = Label(root, "Title Shadow", 550, 103, 820, 94, "CÀI ĐẶT", 74);
        titleShadow.fontStyle = FontStyles.Bold;
        titleShadow.color = new Color(.08f, .2f, .5f);
        var title = Label(root, "Title", 550, 97, 820, 94, "CÀI ĐẶT", 74);
        title.fontStyle = FontStyles.Bold;
        title.color = new Color(1, .88f, .38f);
        Label(root, "Subtitle", 500, 193, 920, 40, "Điều chỉnh trải nghiệm theo cách của bạn", 27).color = Color.white;

        var shadow = RoundBox(root, "Panel Shadow", 360, 267, 1200, 590, new Color(.13f, .26f, .6f, .2f));
        shadow.raycastTarget = false;
        RoundBox(root, "Panel Border", 360, 252, 1200, 590, new Color(.28f, .71f, 1));
        RoundBox(root, "Panel", 366, 258, 1188, 578, new Color(.94f, .98f, 1));
        var blue = new Color(.04f, .5f, .96f);
        var gold = new Color(1, .66f, .08f);
        BuildRow(root, 290, "Âm lượng", "Âm nhạc và hiệu ứng trong game", 0, blue);
        BuildRow(root, 464, "Độ sáng", "Điều chỉnh độ sáng hiển thị trong game", 1, gold);
        BuildRow(root, 638, "Bí danh", "Tên hiển thị của bạn · 2–24 ký tự", 2, blue);
        volumeLabel = Percent(root, "Volume Percent", 327);
        brightnessLabel = Percent(root, "Brightness Percent", 501);
        volumeSlider = MakeSlider(root, "Volume", 940, 332, blue, 0);
        brightnessSlider = MakeSlider(root, "Brightness", 940, 506, gold, .2f);

        RoundBox(root, "Nickname Border", 930, 677, 540, 70, new Color(.69f, .8f, .93f));
        var field = RoundBox(root, "Nickname", 933, 680, 534, 64, Color.white);
        nickname = field.gameObject.AddComponent<TMP_InputField>();
        var viewport = new GameObject("Text Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewport.transform.SetParent(field.transform, false);
        Place(viewport.GetComponent<RectTransform>(), 20, 6, 494, 52);
        var text = Label(viewport.transform, "Nickname Text", 0, 0, 494, 52, "", 28);
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.richText = false;
        nickname.textViewport = viewport.GetComponent<RectTransform>();
        nickname.textComponent = text;
        nickname.characterLimit = 24;
        nickname.lineType = TMP_InputField.LineType.SingleLine;
        nickname.targetGraphic = field;
        nickname.selectionColor = new Color(.2f, .65f, 1, .3f);
        nickname.caretColor = new Color(.08f, .25f, .6f);
        nickname.customCaretColor = true;
        var saveShadow = RoundBox(root, "Save Shadow", 750, 889, 420, 86, new Color(.69f, .34f, .07f));
        saveShadow.raycastTarget = false;
        RoundBox(root, "Save Border", 746, 872, 428, 92, new Color(1, .89f, .4f));
        var save = ActionButton(root, "Save Settings", 752, 878, 416, 80,
            new Color(1, .68f, .08f), "LƯU CÀI ĐẶT", 32, Save);
        save.GetComponentInChildren<TextMeshProUGUI>().color = new Color(.25f, .13f, .03f);
        message = Label(root, "Status", 360, 985, 1200, 52, "", 26);
        message.color = new Color(.08f, .15f, .4f);
        screen.SetActive(false);
    }

    void BuildRow(Transform parent, float y, string title, string description, int symbol, Color accent)
    {
        RoundBox(parent, title + " Row Border", 402, y, 1116, 148, new Color(.79f, .87f, .96f));
        RoundBox(parent, title + " Row", 404, y + 2, 1112, 144, Color.white);
        var badge = RoundBox(parent, title + " Icon Badge", 436, y + 34, 80, 80,
            Color.Lerp(accent, Color.white, .9f));
        badge.sprite = circleSprite;
        badge.type = Image.Type.Simple;
        var icon = new GameObject(title + " Icon", typeof(RectTransform), typeof(SettingsSymbol));
        icon.transform.SetParent(parent, false);
        Place(icon.GetComponent<RectTransform>(), 453, y + 51, 46, 46);
        var drawing = icon.GetComponent<SettingsSymbol>();
        drawing.symbol = symbol;
        drawing.color = accent;
        drawing.raycastTarget = false;
        var label = Label(parent, title + " Label", 544, y + 34, 360, 46, title, 32);
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.fontStyle = FontStyles.Bold;
        var hint = Label(parent, title + " Description", 544, y + 82, 365, 34, description, 18);
        hint.alignment = TextAlignmentOptions.MidlineLeft;
        hint.color = new Color(.4f, .48f, .62f);
    }

    TextMeshProUGUI Percent(Transform parent, string name, float y)
    {
        RoundBox(parent, name + " Badge", 1377, y, 104, 58, new Color(.92f, .96f, 1));
        var label = Label(parent, name, 1377, y, 104, 58, "", 28);
        label.fontStyle = FontStyles.Bold;
        return label;
    }

    Slider MakeSlider(Transform parent, string name, float x, float y, Color color, float minimum)
    {
        var track = Box(parent, name, x, y, 402, 48, Color.clear);
        var slider = track.gameObject.AddComponent<Slider>();
        RoundBox(track.transform, "Track", 0, 15, 402, 18, new Color(.83f, .88f, .94f));
        var fillArea = Box(track.transform, "Fill Area", 0, 15, 402, 18, Color.clear);
        fillArea.raycastTarget = false;
        var fill = RoundBox(fillArea.transform, "Fill", 0, 0, 0, 0, color);
        // Both fill and thumb share the same travel range, so they stay aligned.
        var handleArea = Box(track.transform, "Handle Area", 0, 2, 402, 44, Color.clear);
        handleArea.raycastTarget = false;
        var handle = Box(handleArea.transform, "Handle", 0, 0, 44, 0, color);
        handle.sprite = circleSprite;
        handle.rectTransform.pivot = new Vector2(.5f, .5f);
        var center = Box(handle.transform, "Thumb Center", 5, 5, 34, 34, Color.white);
        center.sprite = circleSprite;
        center.raycastTarget = false;
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.minValue = minimum;
        slider.maxValue = 1;
        slider.onValueChanged.AddListener(value => {
            if (name == "Volume" && value > .0001f) muted = false;
            UpdatePreview();
        });
        return slider;
    }

    Image RoundBox(Transform parent, string name, float x, float y, float w, float h, Color color)
    {
        var image = Box(parent, name, x, y, w, h, color);
        image.sprite = roundedSprite;
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 1;
        return image;
    }

    Button ActionButton(Transform parent, string name, float x, float y, float w, float h,
        Color color, string caption, int size, UnityEngine.Events.UnityAction action)
    {
        var image = RoundBox(parent, name, x, y, w, h, color);
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.highlightedColor = new Color(.93f, .97f, 1);
        colors.pressedColor = new Color(.8f, .88f, .98f);
        button.colors = colors;
        button.onClick.AddListener(action);
        Label(image.transform, name + " Caption", 0, 0, w, h, caption, size).fontStyle = FontStyles.Bold;
        return button;
    }

    // Self-contained UI shapes avoid missing editor-only built-in sprite resources in players.
    static Sprite MakeShape(float radius, out Texture2D texture)
    {
        const int size = 64;
        texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "Settings Rounded Shape";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++) {
            float dx = Mathf.Max(Mathf.Abs(x + .5f - 32) - (32 - radius), 0);
            float dy = Mathf.Max(Mathf.Abs(y + .5f - 32) - (32 - radius), 0);
            pixels[y * size + x] = new Color(1, 1, 1, Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy)));
        }
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100,
            0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
    }

    GameObject CanvasRoot(string name, int order)
    {
        var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.transform.SetParent(transform, false);
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = order;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(DesignWidth, DesignHeight);
        scaler.matchWidthOrHeight = .5f;
        return go;
    }

    static void Place(RectTransform rt, float x, float y, float width, float height)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(width, height);
    }

    static Image Box(Transform parent, string name, float x, float y, float w, float h, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Place(go.GetComponent<RectTransform>(), x, y, w, h);
        var image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    TextMeshProUGUI Label(Transform parent, string name, float x, float y, float w, float h, string value, int size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        Place(go.GetComponent<RectTransform>(), x, y, w, h);
        var label = go.GetComponent<TextMeshProUGUI>();
        if (font != null) label.font = font;
        label.text = value;
        label.fontSize = size;
        label.color = new Color(.02f, .04f, .23f);
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return label;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= SceneLoaded;
        if (font != null) Destroy(font);
        if (roundedSprite != null) Destroy(roundedSprite);
        if (circleSprite != null) Destroy(circleSprite);
        if (roundedTexture != null) Destroy(roundedTexture);
        if (circleTexture != null) Destroy(circleTexture);
        if (instance == this) { AudioListener.volume = 1; instance = null; }
    }
}

public class SettingsPageSizing : MonoBehaviour
{
    public RectTransform design;
    void LateUpdate()
    {
        if (design != null) design.localScale = Vector3.one * (((RectTransform)transform).rect.width / design.sizeDelta.x);
    }
}

// Decorates the existing wooden button; the original art is restored when sound is on.
public class SoundToggleView : MonoBehaviour
{
    Button button;
    GameObject indicator;

    void OnEnable()
    {
        button = GetComponent<Button>();
        if (indicator == null) BuildIndicator();
        button.onClick.RemoveListener(GameSettingsController.ToggleSound);
        button.onClick.AddListener(GameSettingsController.ToggleSound);
        GameSettingsController.SoundStateChanged += Refresh;
        Refresh(GameSettingsController.IsSilent);
    }

    void OnDisable()
    {
        if (button != null) button.onClick.RemoveListener(GameSettingsController.ToggleSound);
        GameSettingsController.SoundStateChanged -= Refresh;
    }

    void Refresh(bool silent) { if (indicator != null) indicator.SetActive(silent); }

    void BuildIndicator()
    {
        indicator = new GameObject("Sound Off Indicator", typeof(RectTransform));
        indicator.transform.SetParent(transform, false);
        Stretch(indicator.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        var slash = new GameObject("Muted Speaker Slash", typeof(RectTransform), typeof(SettingsSymbol), typeof(Outline));
        slash.transform.SetParent(indicator.transform, false);
        Stretch(slash.GetComponent<RectTransform>(), new Vector2(.24f, .43f), new Vector2(.79f, .91f));
        var graphic = slash.GetComponent<SettingsSymbol>();
        graphic.symbol = 3;
        graphic.color = new Color(1, .16f, .16f);
        graphic.raycastTarget = false;
        var outline = slash.GetComponent<Outline>();
        outline.effectColor = new Color(.3f, .04f, .02f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);
        var badge = new GameObject("Muted Label Background", typeof(RectTransform), typeof(Image));
        badge.transform.SetParent(indicator.transform, false);
        Stretch(badge.GetComponent<RectTransform>(), new Vector2(.1f, .11f), new Vector2(.9f, .39f));
        badge.GetComponent<Image>().color = new Color(.54f, .16f, .05f);
        badge.GetComponent<Image>().raycastTarget = false;
        var caption = new GameObject("Muted Label", typeof(RectTransform), typeof(Text), typeof(Shadow));
        caption.transform.SetParent(badge.transform, false);
        Stretch(caption.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        var label = caption.GetComponent<Text>();
        label.font = Resources.Load<Font>("ScienceChatFont");
        label.text = "Đã tắt";
        label.fontSize = 22;
        label.fontStyle = FontStyle.Bold;
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 12;
        label.resizeTextMaxSize = 22;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = new Color(1, .95f, .76f);
        label.raycastTarget = false;
        caption.GetComponent<Shadow>().effectDistance = new Vector2(1, -1);
    }

    static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}

// Small vector icons remain crisp at different Game view resolutions.
public class SettingsSymbol : MaskableGraphic
{
    public int symbol;
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        if (symbol == 3) {
            Stroke(mesh, new Vector2(.12f, .1f), new Vector2(.88f, .9f), .14f);
        } else if (symbol == 0) {
            Quad(mesh, new Vector2(.08f, .36f), new Vector2(.27f, .36f),
                new Vector2(.27f, .64f), new Vector2(.08f, .64f));
            Quad(mesh, new Vector2(.25f, .36f), new Vector2(.53f, .15f),
                new Vector2(.53f, .85f), new Vector2(.25f, .64f));
            for (int arc = 0; arc < 2; arc++) {
                float radius = .24f + arc * .18f;
                for (int i = 0; i < 12; i++) {
                    float a = (-55 + i * 110f / 12) * Mathf.Deg2Rad;
                    float b = (-55 + (i + 1) * 110f / 12) * Mathf.Deg2Rad;
                    Stroke(mesh, new Vector2(.56f + radius * Mathf.Cos(a), .5f + radius * Mathf.Sin(a)),
                        new Vector2(.56f + radius * Mathf.Cos(b), .5f + radius * Mathf.Sin(b)), .045f);
                }
            }
        } else if (symbol == 1) {
            Disk(mesh, new Vector2(.5f, .5f), .23f);
            for (int i = 0; i < 8; i++) {
                float a = i * Mathf.PI / 4;
                var direction = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                Stroke(mesh, Vector2.one * .5f + direction * .33f,
                    Vector2.one * .5f + direction * .47f, .055f);
            }
        } else {
            Disk(mesh, new Vector2(.5f, .74f), .19f);
            Disk(mesh, new Vector2(.5f, .26f), .27f);
            Quad(mesh, new Vector2(.23f, .02f), new Vector2(.77f, .02f),
                new Vector2(.77f, .26f), new Vector2(.23f, .26f));
        }
    }

    void Vertex(VertexHelper mesh, Vector2 p)
    {
        var rect = rectTransform.rect;
        mesh.AddVert(new Vector3(rect.xMin + p.x * rect.width, rect.yMin + p.y * rect.height), color, Vector2.zero);
    }

    void Quad(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        int index = mesh.currentVertCount;
        Vertex(mesh, a); Vertex(mesh, b); Vertex(mesh, c); Vertex(mesh, d);
        mesh.AddTriangle(index, index + 1, index + 2);
        mesh.AddTriangle(index, index + 2, index + 3);
    }

    void Stroke(VertexHelper mesh, Vector2 a, Vector2 b, float width)
    {
        var direction = (b - a).normalized;
        var offset = new Vector2(-direction.y, direction.x) * width * .5f;
        Quad(mesh, a - offset, b - offset, b + offset, a + offset);
    }

    void Disk(VertexHelper mesh, Vector2 center, float radius)
    {
        int index = mesh.currentVertCount;
        Vertex(mesh, center);
        const int segments = 32;
        for (int i = 0; i <= segments; i++) {
            float angle = i * 2 * Mathf.PI / segments;
            Vertex(mesh, center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            if (i > 0) mesh.AddTriangle(index, index + i, index + i + 1);
        }
    }
}
