#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Offline rendering and continuation checks; never sends images to a real server.</summary>
[InitializeOnLoad]
public static class CopyPenaltySmokeTests
{
    const string Key = "CopyPenaltySmokeTests.Running";
    const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;
    static int frames;
    static CopyPenaltySmokeTests()
    {
        EditorApplication.playModeStateChanged += state => {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key, false)) EditorApplication.update += Tick;
        };
    }
    public static void RunBatch()
    {
        SessionState.SetBool(Key, true);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorApplication.isPlaying = true;
    }
    static object Field(EssayTutorUI ui, string name) => typeof(EssayTutorUI).GetField(name, Flags).GetValue(ui);
    static void Render(EssayTutorUI ui, EssayTutorUI.Record record) => typeof(EssayTutorUI).GetMethod("Render", Flags).Invoke(ui, new object[] { record });
    static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    static void Tick()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isCompiling || ++frames < 8) return;
        int exit = 0;
        try {
            var ui = (EssayTutorUI)typeof(EssayTutorUI).GetMethod("Get", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
            VerifyGameEntry(ui);
            var record = new EssayTutorUI.Record { id = "copy-fixture", username = "student", subject = "Physics", question = "Một vật có khối lượng 2 kg. Tính trọng lượng.", status = "learning", total = 3,
                penalty = new EssayTutorUI.Penalty { status = "deferred", count = 0 } };
            Render(ui, record);
            Require(((TMP_InputField)Field(ui, "input")).interactable, "Deferred copying must not lock learning answers.");
            record.status = "completed"; record.step = 3; record.solution = "P = 10m = 20 N.";
            Render(ui, record);
            Require(!((Button)Field(ui, "continueButton")).gameObject.activeSelf, "Copying must block tutorial continuation.");
            record.penalty = new EssayTutorUI.Penalty { status = "copying", kind = "calculation", content = "P = 10m; P: trọng lượng (N); m: khối lượng (kg).", required = 10, count = 7, feedback = "Đã đọc được 7 bản chép đúng. Còn thiếu 3 bản." };
            Render(ui, record);
            typeof(EssayTutorUI).GetField("username", Flags).SetValue(ui, "copy-draft-fixture");
            var sample = new Texture2D(320, 180); var pixels = new Color[320 * 180]; Array.Fill(pixels, Color.white); sample.SetPixels(pixels); sample.Apply();
            typeof(EssayTutorUI).GetMethod("AddCopyImage", Flags).Invoke(ui, new object[] { sample.EncodeToJPG() }); UnityEngine.Object.Destroy(sample);
            typeof(EssayTutorUI).GetMethod("ClearCopyImages", Flags).Invoke(ui, null);
            typeof(EssayTutorUI).GetMethod("RestoreCopyDraft", Flags).Invoke(ui, new object[] { record.id });
            Require(((System.Collections.ICollection)Field(ui, "copyImages")).Count == 1, "Selected photos must survive clearing runtime state and reopening the saved draft.");
            typeof(EssayTutorUI).GetMethod("DeleteCopyDraft", Flags).Invoke(ui, new object[] { record.id });
            Render(ui, record);
            Require(ui.GetComponentsInChildren<RawImage>().Length == 1, "A selected copy page must render a preview.");
            Require(Array.Exists(ui.GetComponentsInChildren<Button>(), b => b.name == "Gửi toàn bộ ảnh để AI kiểm tra"), "Student upload control missing.");
            Canvas.ForceUpdateCanvases();
            var camera = new GameObject("Copy preview camera", typeof(Camera)).GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
            var target = new RenderTexture(1920, 1080, 24); camera.targetTexture = target;
            var canvas = ui.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 5;
            Canvas.ForceUpdateCanvases(); camera.Render(); var previous = RenderTexture.active; RenderTexture.active = target;
            var screenshot = new Texture2D(1920, 1080, TextureFormat.RGB24, false); screenshot.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); screenshot.Apply(); RenderTexture.active = previous;
            Directory.CreateDirectory("outputs"); File.WriteAllBytes("outputs/copy-penalty-preview.png", screenshot.EncodeToPNG());
            typeof(EssayTutorUI).GetField("teacher", Flags).SetValue(ui, true); Render(ui, record);
            Require(!Array.Exists(ui.GetComponentsInChildren<Button>(), b => b.name == "Gửi toàn bộ ảnh để AI kiểm tra"), "Teacher must not upload on behalf of a student.");
            record.penalty.status = "completed"; record.penalty.count = 10; Render(ui, record);
            Require(((TMP_Text)Field(ui, "status")).text.Contains("Đã chép phạt xong"), "Completed copying status missing.");
            File.WriteAllText("outputs/copy-penalty-smoke.txt", "PASS: penalty preserves learning answers but blocks continuation until copying passes; copy template/count/feedback, photo preview, upload controls, teacher read-only and completed status render offline.\n");
        } catch (Exception error) { Debug.LogException(error); exit = 1; }
        SessionState.SetBool(Key, false); EditorApplication.update -= Tick; EditorApplication.Exit(exit);
    }
    static void VerifyGameEntry(EssayTutorUI ui)
    {
        var begin = typeof(EssayTutorUI).GetMethod("BeginPlayCheck", Flags);
        var complete = typeof(EssayTutorUI).GetMethod("CompletePlayCheck", Flags);
        var page = (RectTransform)Field(ui, "page");
        bool questionsOpened = false, nestedOpened = false;
        Action openQuestions = () => { questionsOpened = true; EssayTutorUI.CheckBeforePlay(() => nestedOpened = true); };
        begin.Invoke(ui, new object[] { openQuestions });
        Require(!page.gameObject.activeSelf, "Starting a game must not show the learning page before any pending work is known.");
        Require(((RectTransform)Field(ui, "playGate")).gameObject.activeSelf, "Game entry must use a separate compact loading/error panel.");
        typeof(EssayTutorUI).GetMethod("ShowReviewServiceError", Flags).Invoke(ui, new object[] { "Máy chủ cần cập nhật." });
        Require(!page.gameObject.activeSelf && ((TMP_Text)Field(ui, "playGateMessage")).text.Contains("cập nhật"), "A server-version error must stay on the entry panel instead of opening learning.");
        complete.Invoke(ui, new object[] { new EssayTutorUI.Record[0] });
        Require(questionsOpened && nestedOpened, "No pending work must open questions; game OnEnable must reuse the same approval synchronously.");
        ui.gameObject.SetActive(true); questionsOpened = false;
        begin.Invoke(ui, new object[] { openQuestions });
        complete.Invoke(ui, new object[] { new[] { new EssayTutorUI.Record { id = "pending", status = "completed", penalty = new EssayTutorUI.Penalty { status = "copying" } } } });
        Require(!questionsOpened && page.gameObject.activeSelf, "Actual pending copying must show remediation and block a new game.");
        ui.gameObject.SetActive(false); ui.gameObject.SetActive(true);
    }
}
#endif
