#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Offline Play Mode integration checks for duel review, local capture and tutor continuation.</summary>
[InitializeOnLoad]
public static class PlanetReviewSmokeTests
{
    const string Key = "PlanetReviewSmokeTests.Running";
    const string Account = "planet-review-smoke-fixture";
    const string PrefKey = "Planet.PendingReview." + Account;
    const string RemotePrefKey = "Planet.PendingRemoteReview." + Account;
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    static EssayTutorUI tutor;
    static PlanetDuelController duel;
    static BlastRoundManager game;
    static TcpListener listener;
    static string endpoint, serverError;
    static int stage, frames, requests, healthRequests;
    static double deadline;
    static bool localSynced;
    static PlanetDuelController.ReviewQuestion[] entries;

    static PlanetReviewSmokeTests()
    {
        EditorApplication.playModeStateChanged += change => {
            if (change == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key, false)) Start();
        };
        if (SessionState.GetBool(Key, false) && EditorApplication.isPlaying) EditorApplication.delayCall += Start;
    }
    public static void RunBatch()
    {
        SessionState.SetBool(Key, true);
        if (EditorApplication.isPlaying) { Start(); return; }
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorApplication.isPlaying = true;
    }
    static void Start()
    {
        if (listener != null) return;
        listener = new TcpListener(IPAddress.Loopback, 0); listener.Start();
        endpoint = "http://127.0.0.1:" + ((IPEndPoint)listener.LocalEndpoint).Port;
        Task.Run(Serve);
        stage = frames = 0; deadline = EditorApplication.timeSinceStartup + 60;
        EditorApplication.update -= Tick; EditorApplication.update += Tick;
    }
    static object Field(object target, string name) => target.GetType().GetField(name, Flags).GetValue(target);
    static void Set(object target, string name, object value) => target.GetType().GetField(name, Flags).SetValue(target, value);
    static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Flags).Invoke(target, args);
    static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    static TMP_Text Text(Transform parent, TMP_FontAsset font, string name)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false);
        var text = go.GetComponent<TMP_Text>(); text.font = font; return text;
    }
    static Image Image(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
        return go.GetComponent<Image>();
    }
    static void Setup()
    {
        PlayerPrefs.DeleteKey(PrefKey); PlayerPrefs.DeleteKey(RemotePrefKey); GameSession.Username = Account;
        // The isolated DLL-only harness has no package-script GUID for TMP Settings.
        // A normal Unity project loads this asset from its installed UGUI package.
        if (TMP_Settings.instance == null) typeof(TMP_Settings).GetField("s_Instance", BindingFlags.Static | BindingFlags.NonPublic)
            .SetValue(null, ScriptableObject.CreateInstance<TMP_Settings>());
        tutor = (EssayTutorUI)typeof(EssayTutorUI).GetMethod("Get", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
        Set(tutor, "url", endpoint);
        typeof(EssayTutorUI).GetField("accessToken", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, "fixture-review-access");
        typeof(EssayTutorUI).GetField("tokenAccount", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, Account);
        tutor.gameObject.SetActive(false);
        var font = tutor.GetComponentInChildren<TMP_Text>(true).font;
        var canvas = new GameObject("Planet review fixture", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvas.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        var root = (RectTransform)canvas.transform;
        var host = new GameObject("Duel fixture"); host.SetActive(false); host.transform.SetParent(root, false);
        game = host.AddComponent<BlastRoundManager>(); game.enableOnlineDuel = false; game.useDemoData = false;
        game.guns = new[] { (RectTransform)Image(root, "Gun0").transform, (RectTransform)Image(root, "Gun1").transform };
        game.blobImages = new Image[4]; game.blobTexts = new TMP_Text[4];
        for (int i = 0; i < 4; i++) { game.blobImages[i] = Image(root, "Planet" + i); game.blobTexts[i] = Text(game.blobImages[i].transform, font, "Answer"); }
        game.questionText = Text(root, font, "Question"); game.timerText = Text(root, font, "Timer");
        game.scoreTexts = new[] { Text(root, font, "Score0"), Text(root, font, "Score1") };
        game.resultPanel = Image(root, "Result").gameObject; game.bulletParent = root;
        duel = host.AddComponent<PlanetDuelController>();
        Set(duel, "game", game); Set(duel, "root", root); Set(duel, "url", endpoint); Set(duel, "matchUsername", Account);
        host.SetActive(true); Call(duel, "BuildScreens");
        Set(duel, "historyMatchId", "fixture-match");
        entries = new PlanetDuelController.ReviewQuestion[15];
        for (int i = 0; i < 15; i++) entries[i] = new PlanetDuelController.ReviewQuestion {
            round = i + 1, text = "Câu thử nghiệm " + (i + 1), difficulty = "Dễ", subject = i == 1 ? "Biology" : "Physics",
            questionType = "multipleChoice", status = i == 1 ? "unanswered" : i == 0 ? "wrong" : "correct",
            needsTutor = i < 2, tutorId = i < 2 ? "fixture-lesson-" + i : "", sourceId = "planet:fixture-match:0:" + (i + 1), options = new[] { "20 N", "30 N", "40 N", "50 N" }, choices = i == 1 ? new int[0] : new[] { 1 }
        };
        entries[13].status = "ungraded"; entries[13].questionType = "essay";
        Set(duel, "latest", new PlanetDuelController.State { state = "finished", matchId = "fixture-match", reviewQuestions = entries, reviewPending = 2 });
        Call(duel, "Screen", "finished", "KẾT QUẢ", "Bạn: 100 — Máy: 90", "Tìm trận mới");
        Call(duel, "UpdateReviewGate"); Call(duel, "OpenReviewPicker");
        if (EventSystem.current == null) new GameObject("Fixture events", typeof(EventSystem), typeof(StandaloneInputModule));
    }
    static void VerifyList()
    {
        var picker = (RectTransform)Field(duel, "reviewPicker");
        var scroll = picker.GetComponentInChildren<ScrollRect>();
        Canvas.ForceUpdateCanvases(); LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
        Require(scroll.content.childCount == 15, "Review lost questions beyond row 9");
        Require(scroll.content.rect.height > scroll.viewport.rect.height, "15 questions are not scrollable");
        Require(scroll.content.Find("Question15") != null, "Question 15 is missing");
        string first = scroll.content.Find("Question1").GetComponentInChildren<TMP_Text>().text;
        string second = scroll.content.Find("Question2").GetComponentInChildren<TMP_Text>().text;
        string third = scroll.content.Find("Question3").GetComponentInChildren<TMP_Text>().text;
        Require(first.Contains("CẦN SỬA") && first.Contains("B. 30 N"), "Wrong choice is not visible");
        Require(second.Contains("CHƯA TRẢ LỜI"), "Unanswered Biology is not identified");
        Require(third.Contains("ĐÚNG"), "Correct question is not identified");
        Require(scroll.content.Find("Question14").GetComponentInChildren<TMP_Text>().text.Contains("CHỜ KIỂM TRA"), "Ungraded work is presented as correct");
        Require((bool)Call(duel, "HasPendingReviews"), "New match unlocked with wrong answers");
        Require(((Button)Field(duel, "primary")).GetComponentInChildren<TMP_Text>().text.Contains("Học lại"), "Primary action bypasses remediation");
        Require(!duel.MatchesTutorial("not-this-match"), "Unrelated lesson matched duel");
    }
    static void CompleteLesson(int index)
    {
        tutor.gameObject.SetActive(true);
        Call(tutor, "Render", new EssayTutorUI.Record { id = entries[index].tutorId, subject = entries[index].subject, question = "Bài tính", status = "learning", total = 3 });
        Require(!((Button)Field(tutor, "continueButton")).gameObject.activeSelf, "Continue shown before mastery");
        Call(tutor, "Render", new EssayTutorUI.Record { id = entries[index].tutorId, subject = entries[index].subject, question = "Bài tính", status = "completed", total = 3, step = 3, solution = "Tổng kết" });
        var button = (Button)Field(tutor, "continueButton");
        Require(button.gameObject.activeSelf && button.interactable, "Completed duel lesson has no continuation");
        Require(button.GetComponentInChildren<TMP_Text>().text == "Về các câu sau trận", "Wrong duel continuation destination");
    }
    static void ClickTutorContinue()
    {
        var button = (Button)Field(tutor, "continueButton");
        Canvas.ForceUpdateCanvases();
        var pointer = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(null, button.transform.position) };
        var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
        Require(hits.Count > 0 && hits[0].gameObject.transform.IsChildOf(button.transform), "Tutor continuation button is covered: " + (hits.Count == 0 ? "no raycast hits" : hits[0].gameObject.name) + " at " + pointer.position + ", screen " + Screen.width + "x" + Screen.height);
        ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
    }
    static void VerifyLocal()
    {
        var source = new Question { subject = "Physics", difficulty = "Dễ", text = "Một vật 2 kg có trọng lượng bao nhiêu?", options = new[] { "10 N", "20 N", "30 N", "40 N" }, correct = 1 };
        Question seen = null; int[] seenChoices = null; bool correct = false;
        game.questionsPerGame = 1; game.RoundReviewed = (round, question, choices, answered) => { seen = question; seenChoices = choices; correct = answered; };
        game.StartGame(new List<Question> { source });
        var shuffled = ((List<Question>)Field(game, "questions"))[0];
        int wrong = (shuffled.correct + 1) % 4;
        game.ChooseLocal(wrong); Set(game, "stunUntil", new float[2]); game.ChooseLocal(shuffled.correct);
        Require(seen != null && seen.subject == "Physics" && seen.difficulty == "Dễ", "Shuffle lost subject/difficulty metadata");
        Require(correct && seenChoices.Length == 2 && seen.options[seenChoices[1]] == "20 N", "Local choices no longer match shuffled options");
        game.StopMatch();
        Set(duel, "historyMatchId", "local-11111111111111111111111111111111");
        Call(duel, "LocalRoundReviewed", 1, seen, new[] { seen.correct }, true);
        Require(!PlayerPrefs.HasKey(PrefKey), "Perfect local answer created a mandatory sync gate");
        ((List<PlanetDuelController.ReviewQuestion>)Field(duel, "localReviews")).Clear();
        Call(duel, "LocalRoundReviewed", 1, seen, seenChoices, true);
        Require(PlayerPrefs.HasKey(PrefKey), "Local remediation was not persisted");
        Set(duel, "pendingLocalReview", null); Set(duel, "latest", null);
        Require((bool)Call(duel, "RestoreLocalReviews"), "Local remediation did not survive reopening");
        var batch = (PlanetDuelController.LocalReviewBatch)Field(duel, "pendingLocalReview");
        Require(batch.reviewQuestions[0].needsTutor, "Wrong then correct skipped mandatory learning");
        Require(batch.reviewQuestions[0].options[batch.reviewQuestions[0].choices[1]] == "20 N", "Persisted choices changed answer order");
        Require((bool)Call(duel, "HasPendingReviews"), "Restored local work did not block another match");
        EssayTutorUI.LoadPlanetReviews(batch, (rows, pending) => { localSynced = rows.Length == 1 && pending == 1; });
    }
    static void VerifyCachedRemoteGate()
    {
        Call(duel, "RememberRemoteReviews", 2, false);
        Require(PlayerPrefs.GetInt(RemotePrefKey, 0) == 2, "Pending remote lessons were not cached");
        Set(duel, "latest", null); Set(duel, "mode", "error"); Set(duel, "local", false);
        Call(duel, "StartLocalBot");
        Require((string)Field(duel, "mode") == "reviewRequired", "Offline bot bypassed pending remote review");
        Require(!(bool)Field(duel, "local"), "Offline bot started while remote work remains");
        var blocked = (PlanetDuelController.State)Field(duel, "latest");
        Require(blocked.reviewPending == 2, "Restart lost cached pending remote lesson count");
        Call(duel, "BeginSearch");
        Require((string)Field(duel, "mode") == "reviewRequired", "New search bypassed cached remote review");
        Call(duel, "RememberRemoteReviews", 0, false);
        Require(PlayerPrefs.GetInt(RemotePrefKey, 0) == 2, "Single-match completion cleared other pending remote lessons");
        Call(duel, "RememberRemoteReviews", 0, true);
        Require(!PlayerPrefs.HasKey(RemotePrefKey), "Completed remote lessons left an offline lock");
    }
    static async Task Serve()
    {
        try {
            for (int n = 0; n < 4; n++) using (var client = await listener.AcceptTcpClientAsync()) using (var stream = client.GetStream()) {
                string headers = ""; var one = new byte[1];
                while (!headers.EndsWith("\r\n\r\n")) { if (await stream.ReadAsync(one, 0, 1) != 1) throw new Exception("Incomplete HTTP headers"); headers += (char)one[0]; }
                if (headers.StartsWith("GET /health ")) {
                    int healthNumber = Interlocked.Increment(ref healthRequests);
                    byte[] capability = Encoding.UTF8.GetBytes(healthNumber == 1 ? "{\"ok\":true,\"tutorVersion\":2}" : "{\"ok\":true,\"duelReviewVersion\":1}");
                    byte[] capabilityHead = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: " + capability.Length + "\r\nConnection: close\r\n\r\n");
                    await stream.WriteAsync(capabilityHead, 0, capabilityHead.Length); await stream.WriteAsync(capability, 0, capability.Length);
                    n--; continue;
                }
                string route = n < 3 ? "/review/pending" : "/review/local";
                Require(headers.StartsWith("POST " + route + " "), "Wrong review endpoint: " + headers.Split('\r')[0]);
                Require(headers.Contains("Bearer fixture-review-access"), "Review request missing account authentication");
                int length = 0; foreach (string line in headers.Split('\n')) if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase)) length = int.Parse(line.Substring(15).Trim());
                var body = new byte[length]; for (int read = 0; read < length;) { int count = await stream.ReadAsync(body, read, length - read); if (count == 0) throw new Exception("Incomplete request"); read += count; }
                string payload = Encoding.UTF8.GetString(body);
                Require(payload.Contains(Account), "Review request account mismatch");
                string json;
                if (n < 3) json = "{\"state\":\"finished\",\"reviewPending\":" + (n == 0 ? 1 : 0) + ",\"reviewQuestions\":[{\"round\":1,\"sourceId\":\"planet:fixture-match:0:1\",\"tutorId\":\"fixture-lesson-0\",\"needsTutor\":true,\"tutorCompleted\":true},{\"round\":2,\"sourceId\":\"planet:fixture-match:0:2\",\"tutorId\":\"fixture-lesson-1\",\"needsTutor\":true,\"tutorCompleted\":" + (n == 0 ? "false" : "true") + "}]}";
                else {
                    Require(payload.Contains("local-11111111111111111111111111111111") && payload.Contains("20 N") && payload.Contains("\"choices\":["), "Local sync lost match/options/attempts");
                    json = "{\"state\":\"finished\",\"reviewPending\":1,\"reviewQuestions\":[{\"round\":1,\"needsTutor\":true,\"tutorId\":\"local-lesson\"}]}";
                }
                byte[] data = Encoding.UTF8.GetBytes(json), head = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: " + data.Length + "\r\nConnection: close\r\n\r\n");
                await stream.WriteAsync(head, 0, head.Length); await stream.WriteAsync(data, 0, data.Length); Interlocked.Increment(ref requests);
            }
        } catch (Exception error) { serverError = error.Message; }
    }
    static void Tick()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        try {
            if (serverError != null) throw new Exception(serverError);
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Planet smoke test timed out at stage " + stage);
            if (stage == 0) { Setup(); stage = 1; return; }
            if (stage == 1) {
                if (++frames < 5) return; VerifyList();
                EssayTutorUI.LoadPlanetReviews(null, (rows, pending) => { throw new Exception("Legacy server must not complete review loading"); });
                stage = 10; return;
            }
            if (stage == 10) {
                if (healthRequests < 1 || (bool)Field(tutor, "busy")) return;
                var login = (RectTransform)Field(tutor, "loginPanel");
                Require(login == null || !login.gameObject.activeSelf, "Old review API incorrectly asks for the password again");
                Require(requests == 0, "Review request was sent to an unsupported server");
                bool explanation = false;
                foreach (var label in tutor.GetComponentsInChildren<TMP_Text>())
                    if (label.text.Contains("không phải mật khẩu") && label.text.Contains("PlanetServer")) explanation = true;
                Require(explanation, "Legacy server explanation is not visible");
                tutor.gameObject.SetActive(false); CompleteLesson(0); stage = 11; frames = 0; return;
            }
            if (stage == 11) { if (++frames < 3) return; ClickTutorContinue(); stage = 2; return; }
            if (stage == 2) {
                if (requests < 1 || tutor.gameObject.activeSelf) return;
                var state = (PlanetDuelController.State)Field(duel, "latest");
                if (!state.reviewQuestions[0].tutorCompleted) return;
                Require((bool)Call(duel, "HasPendingReviews"), "One completed lesson unlocked other wrong questions");
                CompleteLesson(1); stage = 12; frames = 0; return;
            }
            if (stage == 12) { if (++frames < 3) return; ClickTutorContinue(); stage = 3; return; }
            if (stage == 3) {
                if (requests < 2 || tutor.gameObject.activeSelf) return;
                var state = (PlanetDuelController.State)Field(duel, "latest");
                if (!state.reviewQuestions[1].tutorCompleted) return;
                Require(!(bool)Call(duel, "HasPendingReviews"), "All lessons completed but next match remains locked");
                Require(((Button)Field(duel, "primary")).GetComponentInChildren<TMP_Text>().text == "Tìm trận mới", "Primary action failed to unlock");
                state.reviewQuestions[0].tutorId = "";
                Call(duel, "LoadSavedReviews"); stage = 4; return;
            }
            if (stage == 4) {
                if (requests < 3 || tutor.gameObject.activeSelf) return;
                var state = (PlanetDuelController.State)Field(duel, "latest");
                Require(state.reviewQuestions[0].tutorId == "fixture-lesson-0", "Retry did not recover missing tutor ID by source ID");
                Require(state.reviewQuestions.Length == 15, "Retry duplicated recovered question");
                VerifyCachedRemoteGate(); VerifyLocal(); stage = 5; return;
            }
            if (stage == 5 && localSynced && requests == 4) {
                string result = "PASS: legacy server shows version error without password loop; 15 scrollable questions; wrong/unanswered/correct/ungraded status and own choices; per-lesson completion; authenticated server refresh; missing-ID recovery; mandatory gate including cached remote offline fallback; shuffled local attempts; perfect local no gate; persistence; local authenticated sync.";
                Directory.CreateDirectory("outputs"); File.WriteAllText("outputs/planet-review-smoke.txt", result); Debug.Log("PLANET_REVIEW_SMOKE: " + result); Finish(0);
            }
        } catch (Exception error) {
            Directory.CreateDirectory("outputs"); File.WriteAllText("outputs/planet-review-smoke.txt", "FAIL: " + error); Debug.LogException(error); Finish(1);
        }
    }
    static void Finish(int code)
    {
        PlayerPrefs.DeleteKey(PrefKey); PlayerPrefs.DeleteKey(RemotePrefKey); PlayerPrefs.Save();
        listener?.Stop(); listener = null; EditorApplication.update -= Tick; SessionState.SetBool(Key, false); EditorApplication.Exit(code);
    }
}
#endif
