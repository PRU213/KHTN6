#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using KHTN6.Teacher;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Play Mode smoke checks for the four teacher Canvases. Clicks start with a real
/// EventSystem raycast, so this catches controls hidden behind another graphic.
/// Does not authenticate, write to Google Sheets, log out, or change preferences.
/// </summary>
public static class TeacherUiSmokeTests
{
    [Serializable] public class Check { public string name, status, detail; }
    [Serializable] public class Report
    {
        public string startedUtc, completedUtc, unityVersion;
        public int passed, failed, skipped;
        public List<Check> checks = new List<Check>();
    }

    public static bool IsRunning { get { return routine != null; } }
    const string QuizId = "KHTN6_UI_TEST_QUIZ";
    const string EssayId = "KHTN6_UI_TEST_ESSAY";
    const string BatchKey = "TeacherUiSmokeTests.Batch";
    const string BatchStartedKey = "TeacherUiSmokeTests.BatchStarted";
    const string BatchDeadlineKey = "TeacherUiSmokeTests.BatchDeadline";
    const string DisabledEndpoint = "invalid-local-test-endpoint";
    static readonly Stack<IEnumerator> stack = new Stack<IEnumerator>();
    static readonly Dictionary<string, byte[]> fileSnapshots = new Dictionary<string, byte[]>();
    static readonly List<string> exceptions = new List<string>();
    static IEnumerator routine;
    static Report report;
    static int lastFrame;
    static bool waitSucceeded;
    static float originalVolume;
    static string ImageFixture { get { return Path.Combine(Application.temporaryCachePath, "KHTN6_UI_TEST_IMAGE.png"); } }

    /// <summary>CLI: Unity -batchmode -projectPath ... -executeMethod TeacherUiSmokeTests.RunBatch -logFile ... (without -quit).</summary>
    public static void RunBatch()
    {
        SessionState.SetBool(BatchKey, true);
        SessionState.SetBool(BatchStartedKey, false);
        SessionState.SetString(BatchDeadlineKey, DateTime.UtcNow.AddSeconds(300).Ticks.ToString());
        ResumeBatch();
        EditorSceneManager.OpenScene("Assets/Scenes/GiaoVien.unity", OpenSceneMode.Single);
        EditorApplication.isPlaying = true;
    }

    [InitializeOnLoadMethod]
    static void ResumeBatch()
    {
        if (!SessionState.GetBool(BatchKey, false)) return;
        EditorApplication.update -= BatchPump;
        EditorApplication.update += BatchPump;
    }

    static void BatchPump()
    {
        long deadline;
        if (!long.TryParse(SessionState.GetString(BatchDeadlineKey, "0"), out deadline) || DateTime.UtcNow.Ticks > deadline)
        {
            if (report == null) report = new Report { startedUtc = DateTime.UtcNow.ToString("o"), unityVersion = Application.unityVersion };
            Add("Batch / 300 second watchdog", false, "Unity did not finish the smoke suite within 300 seconds.");
            Finish(); return;
        }
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || !EditorApplication.isPlaying) return;
        if (!IsRunning && !SessionState.GetBool(BatchStartedKey, false))
        { SessionState.SetBool(BatchStartedKey, true); Run(); }
    }

    [MenuItem("Tools/Teacher UI/Run Play Mode Smoke Tests")]
    public static void Run()
    {
        if (IsRunning) return;
        if (!EditorApplication.isPlaying)
        { Debug.LogWarning("Enter Play Mode on GiaoVien before running teacher UI smoke tests."); return; }
        report = new Report { startedUtc = DateTime.UtcNow.ToString("o"), unityVersion = Application.unityVersion };
        exceptions.Clear(); fileSnapshots.Clear(); stack.Clear();
        originalVolume = AudioListener.volume;
        foreach (var id in new[] { QuizId, EssayId })
        {
            Snapshot(Path.Combine(Application.persistentDataPath, "TeacherDrafts", id + ".json"));
            Snapshot(Path.Combine(Application.persistentDataPath, "TeacherExports", id + ".csv"));
        }
        Snapshot(ImageFixture);
        routine = Suite(); stack.Push(routine); lastFrame = -1;
        Application.logMessageReceived += OnLog;
        EditorApplication.update += Tick;
        Debug.Log("Teacher UI smoke tests started; Google Sheet writes are disabled for test records.");
    }

    static void Tick()
    {
        if (!EditorApplication.isPlaying)
        { Add("Test session", false, "Play Mode ended before the checks completed."); Finish(); return; }
        if (lastFrame == Time.frameCount) return;
        lastFrame = Time.frameCount;
        try
        {
            while (stack.Count > 0)
            {
                var current = stack.Peek();
                if (!current.MoveNext()) { stack.Pop(); continue; }
                var nested = current.Current as IEnumerator;
                if (nested != null) { stack.Push(nested); continue; }
                return;
            }
            Finish();
        }
        catch (Exception error)
        { Add("Test runner", false, error.GetType().Name + ": " + error.Message); Finish(); }
    }

    static IEnumerator Suite()
    {
        ConfirmSaveProtocol();
        if (SceneManager.GetActiveScene().name != "GiaoVien") SceneManager.LoadScene("GiaoVien");
        yield return Frames(3);
        Add("Home / authored Canvas", View != null && View.screen == TeacherScreen.Home);
        Click("SettingsButton", "Home / settings button"); yield return Frames(2);
        var settings = View.transform.Find("TeacherSettings");
        Add("Home / settings opens", settings != null && settings.gameObject.activeInHierarchy);
        Click("Close", "Home / settings close button"); yield return Frames(2);
        Add("Home / settings closes", settings != null && !settings.gameObject.activeSelf);

        Click("Navigate_02_Trac_nghiem", "Home / quiz navigation");
        yield return WaitFor(() => View != null && View.screen == TeacherScreen.Quiz, 8);
        Add("Quiz / scene opens", waitSucceeded);
        if (waitSucceeded) yield return Quiz();
        yield return Home("Quiz");

        Click("Navigate_03_Tu_luan", "Home / essay navigation");
        yield return WaitFor(() => View != null && View.screen == TeacherScreen.Essay, 8);
        Add("Essay / scene opens", waitSucceeded);
        if (waitSucceeded) yield return Essay();
        yield return Home("Essay");

        Click("Navigate_04_Tien_do", "Home / progress navigation");
        yield return WaitFor(() => View != null && View.screen == TeacherScreen.Progress, 8);
        Add("Progress / scene opens", waitSucceeded);
        if (waitSucceeded) yield return Progress();
        yield return Home("Progress");
        Add("Runtime / no teacher exceptions", exceptions.Count == 0, string.Join("; ", exceptions));
        Skip("Google Sheet writes", "Not attempted: tests use an invalid local endpoint and restore their exact local draft/export files.");
        Skip("Native image picker dialog", "The OS dialog is not automated; image decoding, preview, removal and invalid-file behavior are tested separately.");
    }

    static IEnumerator Quiz()
    {
        yield return WaitFor(() => Ready("SaveButton"), 85);
        Add("Quiz / live catalog ready", waitSucceeded && View.Dropdown("monID").options.Count > 0 && HasCatalog());
        if (!waitSucceeded || !HasCatalog()) { Skip("Quiz / data-dependent validation", "Linked Sheet catalog did not load."); yield break; }
        if (!DisableRemoteWrites("Quiz")) yield break;
        Add("Quiz / all 12 fields", new[] { "id", "question", "option_a", "option_b", "option_c", "option_d", "explanation", "points" }.All(n => View.Input(n) != null)
            && View.Dropdown("monID") != null && View.Dropdown("Chương / Chủ đề") != null
            && View.Dropdown("difficulty") != null && CorrectToggles().Length == 4);
        Add("Quiz / initial values", View.Input("points").text == "10" && CorrectToggles().All(t => !t.isOn) && View.Input("id").readOnly);
        foreach (string letter in new[] { "a", "b", "c", "d" })
        {
            var toggle = CorrectToggles().Single(t => t.name == "option_" + letter + "_Correct");
            bool reached = PointerClick(toggle.gameObject);
            yield return Frames(1);
            Add("Quiz / answer " + letter.ToUpperInvariant() + " pointer selection", reached && toggle.isOn && CorrectToggles().Count(t => t.isOn) == 1,
                "The top Canvas hit must route to this Toggle, with exactly one answer selected.");
        }
        // Text entry has its own target to the right of the answer selector.
        var answer = View.Input("option_a");
        bool typed = PointerClick(answer.gameObject, new Vector2(.70f, .5f));
        yield return Frames(2);
        answer.text = "";
        foreach (char character in "Answer A") answer.ProcessEvent(new Event { type = EventType.KeyDown, character = character });
        yield return Frames(1);
        Add("Quiz / answer text input independent of selector", typed && answer.text == "Answer A" && CorrectToggles().Single(t => t.name == "option_d_Correct").isOn);

        yield return SelectDropdown("difficulty", 2, "Quiz / difficulty dropdown");
        yield return SelectDropdown("monID", Math.Min(1, View.Dropdown("monID").options.Count - 1), "Quiz / subject dropdown");
        yield return SelectDropdown("Chương / Chủ đề", Math.Min(1, View.Dropdown("Chương / Chủ đề").options.Count - 1), "Quiz / chapter dropdown");
        ClickQuiet("SettingsButton"); yield return Frames(2);
        string endpointBefore = View.Input("TeacherEndpoint").text;
        string usernameBefore = View.Input("TeacherUsername").text;
        Set("TeacherEndpoint", "KHTN6_UI_TEST_UNSAVED_ENDPOINT"); Set("TeacherUsername", "KHTN6_UI_TEST_UNSAVED_USERNAME");
        ClickQuiet("Close"); yield return Frames(1);
        Click("ResetButton", "Quiz / reset pointer"); yield return Frames(1);
        Add("Quiz / reset clears answers", View.Input("option_a").text == "" && CorrectToggles().All(t => !t.isOn) && View.Input("points").text == "10");
        ClickQuiet("SettingsButton"); yield return Frames(2);
        Add("Quiz / reset preserves hidden settings inputs", View.Input("TeacherEndpoint").text == "KHTN6_UI_TEST_UNSAVED_ENDPOINT"
            && View.Input("TeacherUsername").text == "KHTN6_UI_TEST_UNSAVED_USERNAME", "Temporary fields are not connected or saved to preferences.");
        Set("TeacherEndpoint", endpointBefore); Set("TeacherUsername", usernameBefore);
        ClickQuiet("Close"); yield return Frames(1);
        ClickQuiet("SaveButton"); yield return Frames(1);
        Add("Quiz / missing question validation", Status.Contains("nội dung câu hỏi"));
        Set("question", "KHTN6 UI smoke test question"); Set("option_a", "A"); Set("option_b", "B"); Set("option_c", "C"); Set("option_d", "D");
        ClickQuiet("SaveButton"); yield return Frames(1);
        Add("Quiz / missing correct answer validation", Status.Contains("đáp án đúng"));
        Set("points", "0"); ClickQuiet("SaveButton"); yield return Frames(1);
        Add("Quiz / invalid points validation", Status.Contains("Điểm phải"));
        Set("points", "10"); Set("id", QuizId); Set("explanation", "Local automated test, never sent to Sheet.");
        PointerClick(CorrectToggles().Single(t => t.name == "option_b_Correct").gameObject);
        ClickQuiet("SaveButton"); yield return Frames(2);
        string path = Path.Combine(Application.persistentDataPath, "TeacherDrafts", QuizId + ".json");
        var saved = File.Exists(path) ? JsonUtility.FromJson<TeacherQuestion>(File.ReadAllText(path)) : null;
        Add("Quiz / local save preserves correct_option B", saved != null && saved.correct_option == "B" && saved.question == "KHTN6 UI smoke test question" && saved.id == QuizId);
        Add("Quiz / save states local-only result", Status.Contains("bản nháp") && Status.Contains("chưa ghi Google Sheet") && Ready("SaveButton"));
        ClickQuiet("ResetButton"); yield return Frames(1);
    }

    static IEnumerator Essay()
    {
        yield return WaitFor(() => Ready("SaveButton"), 85);
        Add("Essay / live catalog ready", waitSucceeded && HasCatalog());
        if (!waitSucceeded || !HasCatalog()) { Skip("Essay / data-dependent validation", "Linked Sheet catalog did not load."); yield break; }
        if (!DisableRemoteWrites("Essay")) yield break;
        Add("Essay / all 13 fields", new[] { "CauHoiID", "Question", "DapAnThamKhao", "HuongDanChamAI", "DiemToiDa", "GiaoVienID", "NguonTao", "NgayTao" }.All(n => View.Input(n) != null)
            && new[] { "monID", "Chương/Chủ đề", "difficulty", "TrangThai" }.All(n => View.Dropdown(n) != null) && View.imagePreview != null && View.Button("PickImage") != null);
        Add("Essay / defaults and managed IDs", View.Input("DiemToiDa").text == "10" && View.Input("NguonTao").text == "Giáo viên biên soạn"
            && View.Input("CauHoiID").readOnly && View.Input("GiaoVienID").readOnly && View.Input("NgayTao").readOnly && View.Input("NgayTao").text.Length > 0);
        Add("Essay / empty image state", !View.imagePreview.gameObject.activeSelf && !View.Button("RemoveImage").gameObject.activeSelf);
        yield return EssayImage();
        ClickQuiet("SaveButton"); yield return Frames(1);
        Add("Essay / missing question validation", Status.Contains("nội dung câu hỏi"));
        Set("Question", "KHTN6 UI smoke test essay"); ClickQuiet("SaveButton"); yield return Frames(1);
        Add("Essay / missing reference answer validation", Status.Contains("đáp án tham khảo"));
        Set("DapAnThamKhao", "Reference answer"); ClickQuiet("SaveButton"); yield return Frames(1);
        Add("Essay / missing grading instructions validation", Status.Contains("hướng dẫn chấm AI"));
        Set("HuongDanChamAI", "Award 10 points for a complete explanation.");
        Set("DiemToiDa", "-1"); ClickQuiet("SaveButton"); yield return Frames(1);
        Add("Essay / invalid points validation", Status.Contains("Điểm phải"));
        Set("DiemToiDa", "9.5"); Set("CauHoiID", EssayId);
        yield return SelectDropdown("TrangThai", 2, "Essay / status dropdown");
        ClickQuiet("SaveButton"); yield return Frames(2);
        string path = Path.Combine(Application.persistentDataPath, "TeacherDrafts", EssayId + ".json");
        var saved = File.Exists(path) ? JsonUtility.FromJson<TeacherEssay>(File.ReadAllText(path)) : null;
        Add("Essay / local save carries grading and decimal score", saved != null && saved.CauHoiID == EssayId && saved.DiemToiDa == "9.5"
            && saved.HuongDanChamAI == "Award 10 points for a complete explanation." && saved.TrangThai == "Bản nháp" && saved.HinhAnh == "");
        ClickQuiet("ResetButton"); yield return Frames(1);
        Add("Essay / reset restores defaults", View.Input("Question").text == "" && View.Input("DiemToiDa").text == "10" && View.Input("NguonTao").text == "Giáo viên biên soạn");
    }

    static IEnumerator EssayImage()
    {
        Vector2 previewSize = View.imagePreview.rectTransform.sizeDelta;
        Vector2 previewPosition = View.imagePreview.rectTransform.anchoredPosition;
        var fixture = new Texture2D(32, 16, TextureFormat.RGBA32, false);
        fixture.SetPixels(Enumerable.Repeat(new Color(.2f, .7f, .4f, 1), 32 * 16).ToArray()); fixture.Apply();
        Directory.CreateDirectory(Path.GetDirectoryName(ImageFixture));
        File.WriteAllBytes(ImageFixture, fixture.EncodeToPNG()); UnityEngine.Object.Destroy(fixture);
        var load = typeof(TeacherScreenController).GetMethod("LoadImageFile", BindingFlags.Instance | BindingFlags.NonPublic);
        if (load == null) { Add("Essay / image test seam", false, "LoadImageFile is unavailable."); yield break; }
        load.Invoke(View.GetComponent<TeacherScreenController>(), new object[] { ImageFixture }); yield return Frames(2);
        var texture = View.imagePreview.texture;
        Vector2 fitted = View.imagePreview.rectTransform.sizeDelta;
        Add("Essay / image decode and aspect-correct preview", texture != null && View.imagePreview.gameObject.activeSelf && Mathf.Abs(fitted.x / fitted.y - 2) < .001f
            && fitted.x <= previewSize.x + .01f && fitted.y <= previewSize.y + .01f && Private<string>("imageData").StartsWith("data:image/png;base64,"));
        bool rejected = false;
        try { load.Invoke(View.GetComponent<TeacherScreenController>(), new object[] { ImageFixture + ".missing" }); }
        catch (TargetInvocationException error) { rejected = error.InnerException is IOException; }
        Add("Essay / invalid file preserves previous image", rejected && View.imagePreview.texture == texture && View.imagePreview.gameObject.activeSelf);
        bool removed = ClickQuiet("RemoveImage"); yield return Frames(2);
        Add("Essay / remove image pointer restores empty state", removed && !View.imagePreview.gameObject.activeSelf && View.imagePreview.texture == null
            && Private<string>("imageData") == "" && View.imagePreview.rectTransform.sizeDelta == previewSize && View.imagePreview.rectTransform.anchoredPosition == previewPosition);
    }

    static IEnumerator Progress()
    {
        yield return WaitFor(() => Ready("RefreshProgress"), 110);
        Add("Progress / live Sheet load finishes", waitSucceeded && Status.Contains("Đã cập nhật"));
        if (!waitSucceeded) { Skip("Progress / records and filters", "Network loading timed out."); yield break; }
        var students = Private<List<TeacherStudent>>("students");
        var progress = Private<List<TeacherProgress>>("progress");
        Add("Progress / student list count", students != null && students.Count > 0 && View.Label("StudentCount").text.StartsWith(students.Count + " "));
        var first = View.studentContent.GetComponentsInChildren<Button>().FirstOrDefault();
        if (first != null)
        {
            bool clicked = PointerClick(first.gameObject); yield return Frames(1);
            var selected = Private<TeacherStudent>("selectedStudent");
            Add("Progress / student row pointer selection", clicked && selected != null && first.name == "Student_" + selected.UserID && View.Label("UserID").text.Contains(selected.UserID));
            var records = progress.Where(p => p.UserID.Equals(selected.UserID, StringComparison.OrdinalIgnoreCase)).ToList();
            Add("Progress / selected student record count", View.Label("TopicsCount").text == records.Count.ToString());
            var row = View.progressContent.Cast<Transform>().FirstOrDefault(t => t.name.StartsWith("Progress_"));
            if (row != null)
                Add("Progress / nine Sheet fields represented", View.Label("UserID") != null && new[] { "TienDoID", "ChuongChuDe", "SoLanLam", "DiemCaoNhat", "DiemGanNhat", "TyLeDung", "TrangThai", "LanHocCuoi" }.All(n => row.Find(n) != null));
            else Skip("Progress / nine Sheet fields on a populated row", "The initially selected student has no progress rows.");
        }
        var studentScroll = View.studentContent.GetComponentInParent<ScrollRect>();
        bool canScrollStudents = studentScroll != null && studentScroll.content.rect.height > studentScroll.viewport.rect.height;
        if (canScrollStudents) { studentScroll.verticalNormalizedPosition = 0; yield return Frames(2); }
        var search = View.Input("StudentSearch");
        bool searchClick = PointerClick(search.gameObject, new Vector2(.7f, .5f));
        search.text = "KHTN6_UI_TEST_NO_SUCH_STUDENT_7263"; yield return Frames(2);
        Add("Progress / search empty state", searchClick && View.Label("StudentCount").text == "0 học sinh" && View.Label("StudentName").text == "Chọn học sinh" && View.studentContent.Find("Empty") != null);
        search.text = ""; yield return Frames(2);
        Add("Progress / search restores list", View.studentContent.GetComponentsInChildren<Button>().Length == students.Count);
        if (canScrollStudents) Add("Progress / filtering resets student scroll to top", studentScroll.verticalNormalizedPosition > .99f);
        else Skip("Progress / student scroll reset", "Sheet has fewer rows than the visible student list.");
        var classFilter = View.Dropdown("ClassFilter");
        if (classFilter.options.Count > 1)
        {
            yield return SelectDropdown("ClassFilter", 1, "Progress / class dropdown");
            int count = students.Count(s => string.Equals(s.Class, classFilter.options[1].text, StringComparison.OrdinalIgnoreCase));
            Add("Progress / class filtering", View.Label("StudentCount").text == count + " học sinh");
            classFilter.value = 0; yield return Frames(2);
        }
        else Add("Progress / absent class data disables filter", !classFilter.interactable);
        foreach (var filterName in new[] { "ChapterFilter", "StatusFilter" })
        {
            var filter = View.Dropdown(filterName);
            if (filter.options.Count < 2) { Skip("Progress / " + filterName, "Sheet has no values for this filter."); continue; }
            yield return SelectDropdown(filterName, 1, "Progress / " + filterName + " dropdown");
            var selected = Private<TeacherStudent>("selectedStudent");
            int expected = progress.Count(p => selected != null && p.UserID.Equals(selected.UserID, StringComparison.OrdinalIgnoreCase)
                && string.Equals(filterName == "ChapterFilter" ? p.ChuongChuDe : p.TrangThai, filter.options[1].text, StringComparison.OrdinalIgnoreCase));
            int actual = View.progressContent.Cast<Transform>().Count(t => t.gameObject.activeSelf && t.name.StartsWith("Progress_"));
            Add("Progress / " + filterName + " result count", actual == expected);
            filter.value = 0; yield return Frames(2);
        }
        var catalogBeforeRefresh = Private<TeacherCatalog>("catalog");
        bool refreshClicked = ClickQuiet("RefreshProgress");
        yield return WaitFor(() => Ready("RefreshProgress"), 110);
        Add("Progress / refresh reloads catalog and progress", refreshClicked && waitSucceeded && Status.Contains("Đã cập nhật")
            && !ReferenceEquals(catalogBeforeRefresh, Private<TeacherCatalog>("catalog")), "An explicit refresh must replace the cached catalog.");
    }

    static IEnumerator Home(string from)
    {
        if (View != null && View.screen != TeacherScreen.Home && View.Button("BackButton") != null && View.Button("BackButton").interactable)
        {
            Click("BackButton", from + " / back pointer");
            yield return WaitFor(() => View != null && View.screen == TeacherScreen.Home, 8);
            Add(from + " / back returns home", waitSucceeded);
        }
        if (View == null || View.screen != TeacherScreen.Home)
        { SceneManager.LoadScene("GiaoVien"); yield return Frames(3); }
    }

    static IEnumerator SelectDropdown(string name, int index, string check)
    {
        var dropdown = View.Dropdown(name);
        bool opened = dropdown != null && PointerClick(dropdown.gameObject);
        yield return Frames(3);
        var list = dropdown != null ? dropdown.transform.Find("Dropdown List") : null;
        var items = list != null ? list.GetComponentsInChildren<Toggle>().Where(t => t.gameObject.activeInHierarchy).ToArray() : new Toggle[0];
        bool selected = false;
        if (opened && index < items.Length)
        {
            var item = items[index];
            var scroll = list.GetComponent<ScrollRect>();
            if (scroll != null && scroll.content.rect.height > scroll.viewport.rect.height)
            { scroll.verticalNormalizedPosition = 1f - (items.Length < 2 ? 0 : index / (float)(items.Length - 1)); Canvas.ForceUpdateCanvases(); }
            selected = PointerClick(item.gameObject);
        }
        yield return Frames(2);
        Add(check, opened && selected && dropdown.value == index, "Opened and selected through Canvas raycasts.");
        if (dropdown != null) dropdown.Hide();
        yield return WaitFor(() => dropdown == null || dropdown.transform.Find("Dropdown List") == null, 2);
    }

    static bool PointerClick(GameObject expected, Vector2? normalizedPoint = null)
    {
        if (expected == null || !expected.activeInHierarchy || EventSystem.current == null) return false;
        var selectable = expected.GetComponent<Selectable>();
        if (selectable != null && !selectable.IsInteractable()) return false;
        Canvas.ForceUpdateCanvases();
        var rect = expected.transform as RectTransform;
        var p = normalizedPoint ?? new Vector2(.5f, .5f);
        var local = new Vector2(Mathf.Lerp(rect.rect.xMin, rect.rect.xMax, p.x), Mathf.Lerp(rect.rect.yMin, rect.rect.yMax, p.y));
        var canvas = expected.GetComponentInParent<Canvas>();
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, rect.TransformPoint(local));
        var data = new PointerEventData(EventSystem.current) { position = screen, pressPosition = screen, button = PointerEventData.InputButton.Left, pointerId = -1, clickCount = 1, eligibleForClick = true };
        var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(data, hits);
        if (hits.Count == 0) return false;
        var target = hits[0].gameObject;
        data.pointerCurrentRaycast = hits[0]; data.pointerPressRaycast = hits[0];
        if (ExecuteEvents.GetEventHandler<IPointerClickHandler>(target) != expected) return false;
        ExecuteEvents.ExecuteHierarchy(target, data, ExecuteEvents.pointerEnterHandler);
        data.pointerPress = ExecuteEvents.ExecuteHierarchy(target, data, ExecuteEvents.pointerDownHandler);
        data.rawPointerPress = target;
        ExecuteEvents.ExecuteHierarchy(target, data, ExecuteEvents.pointerUpHandler);
        ExecuteEvents.ExecuteHierarchy(target, data, ExecuteEvents.pointerClickHandler);
        return true;
    }

    static TeacherCanvasView View { get { return UnityEngine.Object.FindFirstObjectByType<TeacherCanvasView>(); } }
    static string Status { get { return View.Label("StatusMessage").text; } }
    static Toggle[] CorrectToggles() { return View.GetComponentsInChildren<Toggle>(true).Where(t => t.name.EndsWith("_Correct", StringComparison.Ordinal)).ToArray(); }
    static bool Ready(string name) { return View != null && View.Button(name) != null && View.Button(name).interactable; }
    static bool HasCatalog() { var catalog = Private<TeacherCatalog>("catalog"); return catalog != null && catalog.Subjects.Count > 0; }
    static bool DisableRemoteWrites(string screen)
    {
        var service = View.GetComponent<TeacherSheetService>();
        service.teacherApiUrl = DisabledEndpoint;
        var validMethod = typeof(TeacherSheetService).GetMethod("ValidEndpoint", BindingFlags.Instance | BindingFlags.NonPublic);
        bool safe = service.ApiUrl == DisabledEndpoint && validMethod != null && !(bool)validMethod.Invoke(service, null);
        Add(screen + " / local save transport guard", safe, "Save is executed only if the service resolves the invalid override and rejects it as a remote endpoint.");
        return safe;
    }
    static void ConfirmSaveProtocol()
    {
        var replyType = typeof(TeacherSheetService).GetNestedType("ApiReply", BindingFlags.NonPublic);
        var confirm = typeof(TeacherSheetService).GetMethod("ConfirmsSaved", BindingFlags.Static | BindingFlags.NonPublic);
        if (replyType == null || confirm == null) { Add("Save protocol / strict confirmation helper", false, "ConfirmsSaved(ApiReply, id, sheet) is unavailable."); return; }
        Func<string, bool> accepts = json => (bool)confirm.Invoke(null, new[] { JsonUtility.FromJson(json, replyType), QuizId, "Questions" });
        Add("Save protocol / exact acknowledgement accepted", accepts("{\"success\":true,\"saved\":true,\"id\":\"" + QuizId + "\",\"sheet\":\"Questions\"}"));
        Add("Save protocol / generic or partial success rejected", !accepts("{\"message\":\"thành công\"}") && !accepts("{\"success\":true}")
            && !accepts("{\"saved\":true}") && !accepts("{\"success\":true,\"saved\":true}"));
        Add("Save protocol / wrong ID or sheet rejected", !accepts("{\"success\":true,\"saved\":true,\"id\":\"OTHER\",\"sheet\":\"Questions\"}")
            && !accepts("{\"success\":true,\"saved\":true,\"id\":\"" + QuizId + "\",\"sheet\":\"TuLuan\"}"));
    }
    static T Private<T>(string name) { return (T)typeof(TeacherScreenController).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(View.GetComponent<TeacherScreenController>()); }
    static void Set(string name, string text) { View.Input(name).text = text; }
    static bool ClickQuiet(string name) { return View != null && View.Button(name) != null && PointerClick(View.Button(name).gameObject); }
    static void Click(string name, string check) { Add(check, ClickQuiet(name)); }
    static IEnumerator Frames(int count) { for (int i = 0; i < count; i++) yield return null; }
    static IEnumerator WaitFor(Func<bool> predicate, double seconds)
    {
        double deadline = EditorApplication.timeSinceStartup + seconds;
        while (!predicate() && EditorApplication.timeSinceStartup < deadline) yield return null;
        waitSucceeded = predicate();
        yield return Frames(2);
    }
    static void Add(string name, bool pass, string detail = "") { report.checks.Add(new Check { name = name, status = pass ? "PASS" : "FAIL", detail = detail }); }
    static void Skip(string name, string detail) { report.checks.Add(new Check { name = name, status = "SKIP", detail = detail }); }
    static void Snapshot(string path) { fileSnapshots[path] = File.Exists(path) ? File.ReadAllBytes(path) : null; }
    static void OnLog(string message, string trace, LogType type)
    {
        if ((type == LogType.Exception || type == LogType.Error) && (trace.Contains("Teacher") || message.Contains("Teacher")))
            exceptions.Add(message.Split('\n')[0]);
    }
    static void Finish()
    {
        EditorApplication.update -= Tick; Application.logMessageReceived -= OnLog;
        routine = null; stack.Clear(); AudioListener.volume = originalVolume;
        foreach (var snapshot in fileSnapshots)
        {
            try
            {
                if (snapshot.Value != null) File.WriteAllBytes(snapshot.Key, snapshot.Value);
                else if (File.Exists(snapshot.Key)) File.Delete(snapshot.Key);
            }
            catch (Exception error) { Add("Cleanup / exact test file", false, error.Message); }
        }
        report.completedUtc = DateTime.UtcNow.ToString("o");
        report.passed = report.checks.Count(c => c.status == "PASS"); report.failed = report.checks.Count(c => c.status == "FAIL"); report.skipped = report.checks.Count(c => c.status == "SKIP");
        string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "tools", "teacher_ui")); Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "TestReport.json"), JsonUtility.ToJson(report, true), new UTF8Encoding(false));
        var md = new StringBuilder("# Teacher UI Play Mode smoke test\n\n");
        md.AppendLine(report.passed + " passed, " + report.failed + " failed, " + report.skipped + " skipped.\n");
        md.AppendLine("Clicks use EventSystem raycasts and pointer down/up/click events. Text validation uses TMP input fields. Local save records are restored/removed by exact test ID; remote writes and logout are excluded.\n");
        md.AppendLine("| Check | Result | Detail |\n|---|---|---|");
        foreach (var check in report.checks) md.AppendLine("| " + check.name + " | " + check.status + " | " + (check.detail ?? "").Replace("|", "/").Replace("\n", " ") + " |");
        File.WriteAllText(Path.Combine(directory, "TestReport.md"), md.ToString(), new UTF8Encoding(false));
        Debug.Log("Teacher UI smoke tests finished: " + report.passed + " passed, " + report.failed + " failed, " + report.skipped + " skipped. Report: " + directory);
        if (SessionState.GetBool(BatchKey, false))
        {
            SessionState.EraseBool(BatchKey); SessionState.EraseBool(BatchStartedKey); SessionState.EraseString(BatchDeadlineKey);
            EditorApplication.update -= BatchPump;
            EditorApplication.Exit(report.failed > 0 ? 1 : 0);
        }
    }
}
#endif
