using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KHTN6.Teacher;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem.UI;
#endif

/// <summary>Builds real uGUI scenes from the supplied artist layouts. No runtime screenshot overlay.</summary>
[InitializeOnLoad]
public static class TeacherSceneBuilder
{
    const string Root = "Assets/TeacherUI/";
    const string CommandFile = "Temp/TeacherUI.command";
    const string ResultFile = "tools/teacher_ui/EditorResult.txt";
    static double nextPoll;
    static TMP_FontAsset regular, bold;
    static readonly string[] Layouts = { "01_Chon_chuc_nang", "02_Trac_nghiem", "03_Tu_luan", "04_Tien_do" };
    static readonly string[] Scenes = { "GiaoVien", "GiaoVienTracNghiem", "GiaoVienTuLuan", "GiaoVienTienDo" };
    [Serializable] class Layout { public Element[] elements; }
    [Serializable] class Element
    {
        public string name, type, sprite, text, color, alignment, fontWeight;
        public float[] rect;
        public float fontSize;
        public bool sliced, wrap;
    }
    [Serializable] class Manifest { public AssetEntry[] assets; }
    [Serializable] class AssetEntry { public string file; public float[] border; }

    static TeacherSceneBuilder() { EditorApplication.update += PollCommand; }

    static void PollCommand()
    {
        if (EditorApplication.timeSinceStartup < nextPoll || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        nextPoll = EditorApplication.timeSinceStartup + 1;
        if (!File.Exists(CommandFile)) return;
        var command = File.ReadAllText(CommandFile).Trim();
        File.Delete(CommandFile);
        try
        {
            if (command == "build") BuildAll();
            else if (command == "repair-radios") RepairAnswerToggles();
            else if (command == "test")
            {
                if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play Mode before running tests.");
                TeacherUiSmokeTests.Run();
                WriteResult("Running teacher UI smoke tests; see TestReport.json and TestReport.md.");
            }
            else if (command == "play") { EditorApplication.isPlaying = true; WriteResult("Entering Play Mode"); }
            else if (command == "stop") { EditorApplication.isPlaying = false; WriteResult("Stopped Play Mode"); }
            else if (command.StartsWith("open:"))
            {
                string scene = command.Substring(5);
                if (!Scenes.Contains(scene)) throw new InvalidOperationException("Unknown teacher scene");
                if (EditorApplication.isPlaying) SceneManager.LoadScene(scene);
                else EditorSceneManager.OpenScene("Assets/Scenes/" + scene + ".unity");
                WriteResult("Opened " + scene);
            }
            else if (command == "status")
            {
                var view = UnityEngine.Object.FindAnyObjectByType<TeacherCanvasView>();
                WriteResult("Scene=" + SceneManager.GetActiveScene().name + "; playing=" + EditorApplication.isPlaying +
                    "; Canvas=" + (view != null) + (view == null ? "" : "; controls=" + view.GetComponentsInChildren<Selectable>(true).Length));
            }
            else if (command == "capture" && EditorApplication.isPlaying)
            {
                Directory.CreateDirectory("tools/teacher_ui/previews");
                ScreenCapture.CaptureScreenshot("tools/teacher_ui/previews/" + SceneManager.GetActiveScene().name + ".png");
                WriteResult("Captured " + SceneManager.GetActiveScene().name);
            }
        }
        catch (Exception ex) { WriteResult("ERROR: " + ex); Debug.LogException(ex); }
    }

    static void WriteResult(string value)
    {
        Directory.CreateDirectory("tools/teacher_ui");
        File.WriteAllText(ResultFile, DateTime.Now.ToString("s") + "\n" + value);
    }

    [MenuItem("Tools/KHTN6/Sua nut chon dap an dung")]
    public static void RepairAnswerToggles()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before repairing the scene.");
        const string path = "Assets/Scenes/GiaoVienTracNghiem.unity";
        var scene = SceneManager.GetSceneByPath(path);
        bool openedHere = !scene.isLoaded;
        if (openedHere) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        try
        {
            var toggles = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Toggle>(true))
                .Where(toggle => toggle.name.StartsWith("option_") && toggle.name.EndsWith("_Correct")).ToArray();
            if (toggles.Length != 4) throw new InvalidOperationException("Expected four correct-answer toggles.");
            foreach (var toggle in toggles)
            {
                var target = toggle.GetComponent<Image>();
                target.raycastTarget = true;
                toggle.targetGraphic = target;
                if (toggle.graphic != null && toggle.graphic != target) toggle.graphic.raycastTarget = false;
                EditorUtility.SetDirty(target);
                EditorUtility.SetDirty(toggle);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            WriteResult("SUCCESS: Four correct-answer toggles receive pointer clicks; existing layout preserved.");
        }
        finally { if (openedHere) EditorSceneManager.CloseScene(scene, true); }
    }

    [MenuItem("Tools/KHTN6/Dung 4 man giao vien")]
    public static void BuildAll()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before rebuilding scenes.");
        EditorSceneManager.SaveOpenScenes();
        Directory.CreateDirectory("tools/teacher_ui");
        if (!File.Exists("tools/teacher_ui/GiaoVien.before.unity"))
            File.Copy("Assets/Scenes/GiaoVien.unity", "tools/teacher_ui/GiaoVien.before.unity");
        ImportSprites();
        regular = FontAsset("DejaVuSans");
        bold = FontAsset("DejaVuSans-Bold");
        var buildScenes = EditorBuildSettings.scenes.ToList();
        for (int i = 0; i < Scenes.Length; i++)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildCanvas(i);
            string path = "Assets/Scenes/" + Scenes[i] + ".unity";
            EditorSceneManager.SaveScene(scene, path);
            int index = buildScenes.FindIndex(s => s.path == path);
            if (index >= 0) buildScenes[index] = new EditorBuildSettingsScene(path, true);
            else buildScenes.Add(new EditorBuildSettingsScene(path, true));
        }
        EditorBuildSettings.scenes = buildScenes.ToArray();
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene("Assets/Scenes/GiaoVien.unity");
        WriteResult("SUCCESS: Created 4 editable Canvas scenes and registered them in Build Settings.");
        Debug.Log("[TeacherUI] Four teacher Canvas scenes are ready.");
    }

    static void ImportSprites()
    {
        var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(Root + "ImportManifest.json").TrimStart('\uFEFF'));
        foreach (var entry in manifest.assets)
        {
            string path = Root + entry.file;
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) { AssetDatabase.ImportAsset(path); importer = AssetImporter.GetAtPath(path) as TextureImporter; }
            if (importer == null) throw new FileNotFoundException(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.spriteBorder = new Vector4(entry.border[0], entry.border[1], entry.border[2], entry.border[3]);
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            // Smooth minification at Canvas scales such as 0.75 and 1.25 without blocky nearest-neighbour edges.
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings); settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }
    }

    static TMP_FontAsset FontAsset(string name)
    {
        string path = Root + "Fonts/" + name + " SDF96.asset";
        var asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if (asset != null) return asset;
        var source = AssetDatabase.LoadAssetAtPath<Font>(Root + "Fonts/" + name + ".ttf");
        asset = TMP_FontAsset.CreateFontAsset(source, 96, 10, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic);
        asset.name = name + " SDF96";
        asset.isMultiAtlasTexturesEnabled = true;
        AssetDatabase.CreateAsset(asset, path);
        string characters = string.Concat(Enumerable.Range(32, 95).Select(c => (char)c)) + string.Concat(Enumerable.Range(0x00C0, 0x180 - 0x00C0).Select(c => (char)c)) + string.Concat(Enumerable.Range(0x1EA0, 0x1F00 - 0x1EA0).Select(c => (char)c)) + "Đđ•·–—…←✓✕";
        asset.TryAddCharacters(characters, out string unused);
        foreach (var texture in asset.atlasTextures) { texture.name = name + " Atlas"; texture.filterMode = FilterMode.Bilinear; AssetDatabase.AddObjectToAsset(texture, asset); }
        if (asset.material.HasProperty("_Sharpness")) asset.material.SetFloat("_Sharpness", .15f);
        AssetDatabase.AddObjectToAsset(asset.material, asset);
        EditorUtility.SetDirty(asset);
        return asset;
    }

    static void BuildCanvas(int page)
    {
        var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        var camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black; camera.orthographic = true;
        cameraObject.transform.position = new Vector3(0, 0, -10);
        var canvasObject = new GameObject("TeacherCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(TeacherCanvasView));
        var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.pixelPerfect = true;
        var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
        var view = canvasObject.GetComponent<TeacherCanvasView>(); view.screen = (TeacherScreen)page; view.regularFont = regular; view.boldFont = bold;
        view.rowNormal = Sprite("Sprites/UI/row_normal.png"); view.rowSelected = Sprite("Sprites/UI/row_selected.png");
        view.iconPerson = Sprite("Sprites/Icons/icon_person.png"); view.progressTrack = Sprite("Sprites/Controls/progress_track.png");
        view.progressFill = Sprite("Sprites/Controls/progress_fill.png"); view.tagGreen = Sprite("Sprites/UI/tag_green.png"); view.tagAmber = Sprite("Sprites/UI/tag_amber.png");
        var group = canvasObject.AddComponent<ToggleGroup>(); group.allowSwitchOff = false;
        var layout = JsonUtility.FromJson<Layout>(File.ReadAllText(Root + "Layouts/" + Layouts[page] + ".json").TrimStart('\uFEFF'));
        var fields = layout.elements.Where(e => e.type == "TMP_InputField" || e.type == "TMP_Dropdown").ToArray();
        int labelIndex = 0;
        foreach (var e in layout.elements)
        {
            if (page == 3 && (Inside(e.rect, new float[] {94,489,400,389}) || Inside(e.rect, new float[] {530,674,1290,216}))) continue;
            if (e.type == "TextMeshProUGUI" && fields.Any(f => Inside(e.rect, f.rect))) continue;
            if (e.type == "Image" && fields.Any(f => f.type == "TMP_Dropdown" && Inside(e.rect, f.rect))) continue;
            if (e.type == "TextMeshProUGUI" && e.rect[1] == 994) continue;
            if (e.type == "TMP_InputField") { CreateInput(canvasObject.transform, e); continue; }
            if (e.type == "TMP_Dropdown") { CreateDropdown(canvasObject.transform, e); continue; }
            if (e.type == "Toggle")
            {
                var toggleImage = Image(canvasObject.transform, e.name, e.rect, "Sprites/Controls/radio_off.png", false);
                // A Toggle needs a raycastable graphic; otherwise its click falls through to the answer input.
                toggleImage.raycastTarget = true;
                var toggle = toggleImage.gameObject.AddComponent<Toggle>(); toggle.targetGraphic = toggleImage;
                var check = Image(toggleImage.transform, "Selected", new float[]{0,0,40,40}, "Sprites/Controls/radio_on.png", false);
                check.raycastTarget = false; toggle.graphic = check; toggle.group = group; toggle.isOn = e.name == "option_a_Correct";
                continue;
            }
            if (e.type == "TextMeshProUGUI")
            {
                string name = LabelName(e, page, labelIndex++);
                string value = e.text;
                if (name == "StudentName") value = "Chọn học sinh";
                if (name == "UserID") value = "";
                if (name == "StudentCount") value = "Đang tải danh sách…";
                if (name == "TopicsCount" || name == "CompletedCount" || name == "AttemptsCount") value = "—";
                if (name == "ProgressSummary") value = "Chọn học sinh để xem tiến độ học tập.";
                var label = Text(canvasObject.transform, name, value, e.rect, e.fontSize, e.color, e.fontWeight == "Bold", e.alignment);
                label.textWrappingMode = e.wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
                if (name == "AccountName")
                {
                    label.enableAutoSizing = true; label.fontSizeMin = 24; label.fontSizeMax = 32;
                    label.textWrappingMode = TextWrappingModes.NoWrap; label.overflowMode = TextOverflowModes.Ellipsis;
                }
                continue;
            }
            if (e.type == "SolidColor")
            {
                var solid = Image(canvasObject.transform, e.name, e.rect, null, false); ColorUtility.TryParseHtmlString(e.color, out var color); solid.color = color; continue;
            }
            var graphic = Image(canvasObject.transform, e.type == "Button" ? ButtonName(e.name) : e.name, e.rect, e.sprite, e.sliced);
            if (e.type == "Button") ConfigureButton(graphic.gameObject.AddComponent<Button>(), graphic);
        }
        Text(canvasObject.transform, "StatusMessage", "", new float[]{page == 0 ? 520 : 76, 973, page == 0 ? 1260 : 1190, 87}, 22, "#fff6dc");
        if (page == 2)
        {
            var preview = Rect(canvasObject.transform, "ImagePreview", new float[]{1514,542,292,166}).gameObject.AddComponent<RawImage>();
            preview.raycastTarget = false; preview.color = Color.white; preview.gameObject.SetActive(false); view.imagePreview = preview;
            var remove = Image(canvasObject.transform, "RemoveImage", new float[]{1768,538,42,42}, "Sprites/Icons/icon_close.png", false);
            ConfigureButton(remove.gameObject.AddComponent<Button>(), remove); remove.gameObject.SetActive(false);
        }
        if (page == 3)
        {
            view.studentContent = ScrollArea(canvasObject.transform, "StudentList", new float[]{94,489,400,389}, 374);
            view.progressContent = ScrollArea(canvasObject.transform, "ProgressList", new float[]{530,674,1310,216}, 1290);
            var refresh = Image(canvasObject.transform, "RefreshProgress", new float[]{1300,980,230,72}, "Sprites/UI/button_dark.png", true);
            ConfigureButton(refresh.gameObject.AddComponent<Button>(), refresh);
            Text(refresh.transform, "Caption", "Làm mới", new float[]{10,10,210,52}, 24, "#fff6dc", true, "center");
        }
        var controllerType = typeof(TeacherCanvasView).Assembly.GetType("KHTN6.Teacher.TeacherScreenController");
        if (controllerType == null) throw new InvalidOperationException("TeacherScreenController has not compiled yet.");
        canvasObject.AddComponent(controllerType);
        var eventObject = new GameObject("EventSystem", typeof(EventSystem));
        #if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        var module = eventObject.AddComponent<InputSystemUIInputModule>(); module.AssignDefaultActions();
        #else
        eventObject.AddComponent<StandaloneInputModule>();
        #endif
        Selection.activeGameObject = canvasObject;
    }

    static bool Inside(float[] a, float[] b) => a[0] >= b[0] && a[1] >= b[1] && a[0] + a[2] <= b[0] + b[2] + 1 && a[1] + a[3] <= b[1] + b[3] + 8;
    static string ButtonName(string name)
    {
        switch (name)
        {
            case "Cài đặt": return "SettingsButton"; case "Xếp hạng": return "RankButton"; case "Thoát": return "LogoutButton";
            case "Làm mới": return "ResetButton"; case "Lưu câu hỏi": return "SaveButton"; case "Quay về tổng quan": return "HomeButton";
            default: return name;
        }
    }
    static string LabelName(Element e, int page, int index)
    {
        if (e.text == "Giáo viên") return "AccountName";
        if (e.text == "Chưa chọn ảnh") return "UploadHint";
        if (page == 3)
        {
            if (e.rect[0] == 530 && e.rect[1] == 297) return "StudentName";
            if (e.name == "UserID") return "UserID";
            if (e.rect[1] == 899) return "StudentCount";
            if (e.rect[1] == 415 && e.rect[0] == 841) return "TopicsCount";
            if (e.rect[1] == 415 && e.rect[0] == 1271) return "CompletedCount";
            if (e.rect[1] == 415 && e.rect[0] == 1721) return "AttemptsCount";
            if (e.rect[1] == 912) return "ProgressSummary";
        }
        return "Label_" + index;
    }
    static Sprite Sprite(string file) => string.IsNullOrEmpty(file) ? null : AssetDatabase.LoadAssetAtPath<Sprite>(Root + file);
    static RectTransform Rect(Transform parent, string name, float[] rect)
    {
        var result = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); result.SetParent(parent, false);
        result.anchorMin = result.anchorMax = new Vector2(0, 1); result.pivot = new Vector2(0, 1);
        result.anchoredPosition = new Vector2(rect[0], -rect[1]); result.sizeDelta = new Vector2(rect[2], rect[3]); return result;
    }
    static void Stretch(RectTransform rect, float left = 0, float top = 0, float right = 0, float bottom = 0)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = new Vector2(left, bottom); rect.offsetMax = new Vector2(-right, -top);
    }
    static Image Image(Transform parent, string name, float[] rect, string sprite, bool sliced)
    {
        var image = Rect(parent, name, rect).gameObject.AddComponent<Image>(); image.sprite = Sprite(sprite);
        image.type = sliced && image.sprite != null ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
        image.preserveAspect = !sliced && !string.IsNullOrEmpty(sprite) && (sprite.Contains("/Icons/") || sprite.Contains("/Characters/"));
        image.raycastTarget = false; return image;
    }
    static TextMeshProUGUI Text(Transform parent, string name, string value, float[] rect, float size, string color, bool heavy = false, string alignment = "left")
    {
        var text = Rect(parent, name, rect).gameObject.AddComponent<TextMeshProUGUI>(); text.font = heavy ? bold : regular;
        text.text = value; text.fontSize = size; text.enableAutoSizing = false; text.raycastTarget = false;
        text.richText = false; text.textWrappingMode = TextWrappingModes.Normal; text.overflowMode = TextOverflowModes.Truncate;
        text.alignment = alignment == "center" ? TextAlignmentOptions.Center : alignment == "right" ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
        if (ColorUtility.TryParseHtmlString(color, out var parsed)) text.color = parsed;
        return text;
    }
    static void ConfigureButton(Button button, Graphic graphic)
    {
        graphic.raycastTarget = true; button.targetGraphic = graphic;
        var colors = button.colors; colors.highlightedColor = new Color(1,.96f,.8f); colors.pressedColor = new Color(.7f,.85f,.8f); colors.disabledColor = new Color(.55f,.55f,.55f,.8f); button.colors = colors;
    }
    static void CreateInput(Transform parent, Element e)
    {
        var image = Image(parent, e.name, e.rect, e.sprite, true); image.raycastTarget = true;
        var input = image.gameObject.AddComponent<TMP_InputField>(); input.targetGraphic = image;
        var viewport = Rect(image.transform, "Text Area", new float[]{0,0,e.rect[2],e.rect[3]});
        float left = e.name.StartsWith("option_") ? 65 : e.name == "StudentSearch" ? 60 : 16;
        Stretch(viewport, left, 9, 16, 7); viewport.gameObject.AddComponent<RectMask2D>();
        var value = Text(viewport, "Text", "", new float[]{0,0,e.rect[2] - left -16,e.rect[3]-16}, 23, "#203d3e"); Stretch(value.rectTransform);
        var hint = Text(viewport, "Placeholder", Placeholder(e.name), new float[]{0,0,100,30}, 21, "#77877b"); Stretch(hint.rectTransform);
        input.textViewport = viewport; input.textComponent = value; input.placeholder = hint; input.caretColor = new Color(.12f,.24f,.24f); input.customCaretColor = true;
        input.selectionColor = new Color(.2f,.65f,.6f,.3f); input.richText = false; input.characterLimit = 20000;
        bool multiline = new[]{"question","Question","explanation","DapAnThamKhao","HuongDanChamAI"}.Contains(e.name);
        input.lineType = multiline ? TMP_InputField.LineType.MultiLineNewline : TMP_InputField.LineType.SingleLine;
        value.alignment = multiline ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.MidlineLeft;
        hint.alignment = value.alignment;
        bool readOnly = new[]{"id","CauHoiID","GiaoVienID","NgayTao"}.Contains(e.name); input.readOnly = readOnly;
        if (e.name == "points") input.contentType = TMP_InputField.ContentType.IntegerNumber;
        if (e.name == "DiemToiDa") input.contentType = TMP_InputField.ContentType.DecimalNumber;
        if (e.name == "points" || e.name == "DiemToiDa") input.text = "10";
        if (e.name == "NguonTao") input.text = "Giáo viên biên soạn";
        if (readOnly) input.text = e.name == "GiaoVienID" ? "Chưa đăng nhập" : "Tự động khi lưu";
    }
    static string Placeholder(string name)
    {
        switch(name) {
            case "id": case "CauHoiID": case "NgayTao": return "Tự động khi lưu";
            case "StudentSearch": return "Tìm tên hoặc mã học sinh";
            case "question": case "Question": return "Nhập nội dung câu hỏi…";
            case "DapAnThamKhao": return "Nhập đáp án tham khảo…";
            case "HuongDanChamAI": return "Nhập tiêu chí và thang điểm chấm…";
            case "explanation": return "Nhập lời giải thích (không bắt buộc)…";
            default: return name.StartsWith("option_") ? "Nhập phương án " + name.Substring(7).ToUpperInvariant() : "";
        }
    }
    static void CreateDropdown(Transform parent, Element e)
    {
        var image = Image(parent, e.name, e.rect, e.sprite, true); image.raycastTarget = true;
        var dropdown = image.gameObject.AddComponent<TMP_Dropdown>(); dropdown.targetGraphic = image;
        var caption = Text(image.transform, "Caption", "Đang tải…", new float[]{16,4,e.rect[2]-72,e.rect[3]-8}, 23, "#203d3e"); caption.alignment = TextAlignmentOptions.MidlineLeft;
        var arrow = Image(image.transform, "Arrow", new float[]{e.rect[2]-43, (e.rect[3]-30)/2,30,30}, "Sprites/Icons/icon_chevron_ink.png", false);
        var template = Image(image.transform, "Template", new float[]{0,e.rect[3]+3,e.rect[2],270}, "Sprites/UI/field_cream.png", true);
        template.raycastTarget = true;
        var scroll = template.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 32;
        var viewport = Rect(template.transform, "Viewport", new float[]{8,8,e.rect[2]-16,254}); viewport.gameObject.AddComponent<RectMask2D>();
        var content = Rect(viewport, "Content", new float[]{0,0,e.rect[2]-16,64});
        var itemBackground = Image(content, "Item", new float[]{0,0,e.rect[2]-16,64}, "Sprites/UI/row_selected.png", true);
        var item = itemBackground.gameObject.AddComponent<Toggle>(); itemBackground.raycastTarget = true; item.targetGraphic = itemBackground;
        var check = Image(item.transform, "Checkmark", new float[]{8,21,22,22}, "Sprites/Icons/icon_check.png", false); item.graphic = check;
        var itemLabel = Text(item.transform, "Item Label", "", new float[]{38,4,e.rect[2]-66,56}, 22, "#203d3e"); itemLabel.alignment = TextAlignmentOptions.MidlineLeft;
        var colors = item.colors; colors.normalColor = new Color(1,1,1,0); colors.highlightedColor = new Color(.6f,.85f,.8f,.5f); colors.selectedColor = colors.highlightedColor; item.colors = colors;
        scroll.viewport = viewport; scroll.content = content;
        dropdown.template = template.rectTransform; dropdown.captionText = caption; dropdown.itemText = itemLabel;
        dropdown.options = new List<TMP_Dropdown.OptionData>{new TMP_Dropdown.OptionData("Đang tải…")}; dropdown.RefreshShownValue();
        template.gameObject.SetActive(false);
    }
    static RectTransform ScrollArea(Transform parent, string name, float[] rect, float contentWidth)
    {
        var root = Rect(parent, name, rect); var raycast = root.gameObject.AddComponent<Image>(); raycast.color = Color.clear; raycast.raycastTarget = true;
        var scroll = root.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.vertical = true; scroll.scrollSensitivity = 35; scroll.movementType = ScrollRect.MovementType.Clamped;
        var viewport = Rect(root, "Viewport", new float[]{0,0,contentWidth,rect[3]}); viewport.gameObject.AddComponent<RectMask2D>();
        var content = Rect(viewport, "Content", new float[]{0,0,contentWidth,rect[3]});
        var track = Image(root, "Scrollbar", new float[]{rect[2]-14,0,14,rect[3]}, "Sprites/UI/scroll_track.png", false);
        track.raycastTarget = true; var scrollbar = track.gameObject.AddComponent<Scrollbar>(); scrollbar.direction = Scrollbar.Direction.BottomToTop;
        var sliding = Rect(track.transform, "Sliding Area", new float[]{0,0,14,rect[3]}); Stretch(sliding);
        var handle = Image(sliding, "Handle", new float[]{0,0,14,70}, "Sprites/UI/scroll_thumb.png", false); handle.raycastTarget = true;
        scrollbar.handleRect = handle.rectTransform; scrollbar.targetGraphic = handle; scroll.viewport = viewport; scroll.content = content; scroll.verticalScrollbar = scrollbar;
        return content;
    }
}
