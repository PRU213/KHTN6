#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>Offline runtime layout/serialization checks. Does not contact AI or change saved scenes.</summary>
[InitializeOnLoad]
public static class EssayTutorSmokeTests
{
    const string Key = "EssayTutorSmokeTests.Running";
    static int frames;
    static EssayTutorUI ui;
    static EssayTutorSmokeTests()
    {
        EditorApplication.playModeStateChanged += OnPlay;
        if (SessionState.GetBool(Key, false)) EditorApplication.update += Tick;
    }
    public static void RunBatch()
    {
        Debug.Log("ESSAY_TUTOR_SMOKE: starting");
        SessionState.SetBool(Key, true);
        if (EditorApplication.isPlaying) { frames = 0; EditorApplication.update -= Tick; EditorApplication.update += Tick; return; }
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorApplication.isPlaying = true;
    }
    static void OnPlay(PlayModeStateChange change)
    {
        if (!SessionState.GetBool(Key, false) || change != PlayModeStateChange.EnteredPlayMode) return;
        frames = 0; EditorApplication.update -= Tick; EditorApplication.update += Tick;
    }
    static void Tick()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        if (++frames < 5) return;
        try {
            if (ui == null) {
                ui = (EssayTutorUI)typeof(EssayTutorUI).GetMethod("Get", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
                var record = new EssayTutorUI.Record { id = "fixture", username = "Học sinh thử nghiệm", subject = "Physics", chapter = "Lực",
                    question = "Một vật có khối lượng 2 kg. Tính trọng lượng của vật.", score = 4, status = "learning", step = 0, total = 5, checking = true,
                    explanation = "Trọng lượng được tính bằng P = 10 × m. P là trọng lượng (N), m là khối lượng (kg).",
                    prompt = "Nếu biết khối lượng m theo kg, em dùng công thức nào để tính trọng lượng P?",
                    events = new[] { new EssayTutorUI.Turn { step = 1, prompt = "Em nhớ công thức tính trọng lượng không?", answer = "Em chưa biết", correct = false,
                        explanation = "Ta dùng P = 10 × m. P đo bằng N, m đo bằng kg.", at = "2026-10-29T08:00:00Z" } } };
                typeof(EssayTutorUI).GetMethod("Render", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(ui, new object[] { record });
                var earlyRequestType = typeof(EssayTutorUI).GetNestedType("Request", BindingFlags.NonPublic);
                typeof(EssayTutorUI).GetMethod("ShowLogin", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(ui,
                    new[] { (object)"state", Activator.CreateInstance(earlyRequestType) });
                if (EventSystem.current == null) new GameObject("Offline input events", typeof(EventSystem), typeof(StandaloneInputModule));
                frames = 5; return;
            }
            if (frames < 12) return;
            Canvas.ForceUpdateCanvases();
            VerifyMandatoryResults();
            var input = ui.GetComponentInChildren<TMP_InputField>();
            if (input == null || !input.interactable || input.textViewport == null || input.textComponent == null) throw new Exception("Answer field is not configured.");
            var responseType = typeof(EssayTutorUI).GetNestedType("Response", BindingFlags.NonPublic);
            var response = (EssayTutorUI.Record)JsonUtility.FromJson("{\"id\":\"saved\",\"status\":\"learning\",\"step\":2,\"events\":[{\"answer\":\"đúng\",\"correct\":true}]}", responseType);
            if (response.id != "saved" || response.step != 2 || response.events.Length != 1) throw new Exception("Inherited response fields did not deserialize.");
            var content = ui.GetComponentInChildren<ScrollRect>().content;
            if (content.rect.height <= 0) throw new Exception("Journal has no scrollable layout.");
            var requestType = typeof(EssayTutorUI).GetNestedType("Request", BindingFlags.NonPublic);
            typeof(EssayTutorUI).GetMethod("ShowLogin", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(ui, new[] { (object)"state", Activator.CreateInstance(requestType) });
            var password = (TMP_InputField)typeof(EssayTutorUI).GetField("password", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ui);
            var events = EventSystem.current;
            if (events == null) events = new GameObject("Offline input events", typeof(EventSystem), typeof(StandaloneInputModule)).GetComponent<EventSystem>();
            Canvas.ForceUpdateCanvases();
            var pointer = new PointerEventData(events) { position = RectTransformUtility.WorldToScreenPoint(null, password.transform.position) };
            var hits = new System.Collections.Generic.List<RaycastResult>(); events.RaycastAll(pointer, hits);
            if (hits.Count == 0 || !hits[0].gameObject.transform.IsChildOf(password.transform)) throw new Exception("Password field cannot receive clicks; first hit: " + (hits.Count == 0 ? "none" : hits[0].gameObject.name));
            ExecuteEvents.Execute(password.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(password.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            events.SetSelectedGameObject(password.gameObject);
            if (password.readOnly) throw new Exception("Selecting password leaves it read-only.");
            password.ProcessEvent(new Event { type = EventType.KeyDown, keyCode = KeyCode.X, character = 'x' });
            if (password.text != "x") throw new Exception("Focused password does not accept keyboard characters.");
            events.SetSelectedGameObject(null);
            typeof(EssayTutorUI).GetMethod("ShowLogin", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(ui, new[] { (object)"state", Activator.CreateInstance(requestType) });
            events.SetSelectedGameObject(password.gameObject);
            if (password.readOnly || password.text.Length != 0) throw new Exception("Reopening password cannot be edited or retains prior input.");
            password.interactable = false;
            typeof(EssayTutorUI).GetMethod("Cancel", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(ui, null);
            if (!password.interactable) throw new Exception("Closing authentication leaves the password field locked.");
            ((RectTransform)typeof(EssayTutorUI).GetField("loginPanel", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ui)).gameObject.SetActive(false);
            VerifyAnswerAfterResponse(events);
            var tokenField = typeof(EssayTutorUI).GetField("accessToken", BindingFlags.NonPublic | BindingFlags.Static);
            tokenField.SetValue(null, "offline-fixture");
            var loginScene = UnityEngine.SceneManagement.SceneManager.CreateScene("SignIn");
            typeof(EssayTutorUI).GetMethod("ResetAuthenticationOnLogin", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null,
                new object[] { loginScene, UnityEngine.SceneManagement.LoadSceneMode.Single });
            if (tokenField.GetValue(null) != null) throw new Exception("Journal token survived returning to sign-in.");
            var camera = new GameObject("Capture", typeof(Camera)).GetComponent<Camera>();
            camera.orthographic = true; camera.orthographicSize = 5.4f; camera.transform.position = new Vector3(0, 0, -10);
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
            var texture = new RenderTexture(1920, 1080, 24); camera.targetTexture = texture;
            var canvas = ui.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 5;
            Canvas.ForceUpdateCanvases(); camera.Render();
            var old = RenderTexture.active; RenderTexture.active = texture;
            var image = new Texture2D(1920, 1080, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); image.Apply(); RenderTexture.active = old;
            var directory = Path.Combine(Directory.GetCurrentDirectory(), "outputs"); Directory.CreateDirectory(directory);
            File.WriteAllBytes(Path.Combine(directory, "essay-tutor-preview.png"), image.EncodeToPNG());
            File.WriteAllText(Path.Combine(directory, "essay-tutor-smoke.txt"), "PASS: answer click and typing after repeated AI responses and lost runtime focus callbacks; password click/typing/reopen; mandatory next-question gate for Football/Sheep/Cave; teacher/completed input remains locked.\n");
            Finish(0);
        } catch (Exception error) {
            Debug.LogException(error); Finish(1);
        }
    }
    static void VerifyAnswerAfterResponse(EventSystem events)
    {
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
        var type = typeof(EssayTutorUI);
        var answer = (TMP_InputField)type.GetField("input", flags).GetValue(ui);
        // Editor script reloads do not preserve listeners registered from code.
        answer.onSelect.RemoveAllListeners(); answer.onDeselect.RemoveAllListeners();
        var clear = typeof(FootballEssayController).Assembly.GetType("EssayInputState").GetMethod("Clear");
        var record = new EssayTutorUI.Record { id="answer-fixture", subject="Physics", status="learning", question="Đề thử nhập", prompt="Trả lời bước này", total=3 };
        for (int turn=0; turn<3; turn++) {
            type.GetField("busy", flags).SetValue(ui, true);
            type.GetMethod("SetControls", flags).Invoke(ui, null);
            if (answer.interactable) throw new Exception("Answer remains interactive while AI is processing.");
            events.SetSelectedGameObject(null);
            clear.Invoke(null, new object[] { answer });
            type.GetField("busy", flags).SetValue(ui, false);
            record.revision=turn; record.checking=turn>0;
            type.GetMethod("Render", flags).Invoke(ui, new object[]{ record });
            Canvas.ForceUpdateCanvases();
            var pointer=new PointerEventData(events){position=RectTransformUtility.WorldToScreenPoint(null,answer.transform.position)};
            var hits=new System.Collections.Generic.List<RaycastResult>(); events.RaycastAll(pointer,hits);
            if(hits.Count==0 || !hits[0].gameObject.transform.IsChildOf(answer.transform)) throw new Exception("Answer is covered after AI response.");
            ExecuteEvents.Execute(answer.gameObject,pointer,ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(answer.gameObject,pointer,ExecuteEvents.pointerClickHandler);
            events.SetSelectedGameObject(answer.gameObject);
            if(answer.readOnly) throw new Exception("Answer stays read-only after AI response " + turn);
            answer.ProcessEvent(new Event{type=EventType.KeyDown,keyCode=KeyCode.A,character='a'});
            if(answer.text!="a") throw new Exception("Answer does not accept a keyboard character after AI response.");
        }
        type.GetField("teacher",flags).SetValue(ui,true);
        type.GetMethod("Render",flags).Invoke(ui,new object[]{record});
        if(answer.gameObject.activeSelf || answer.interactable) throw new Exception("Teacher can edit student answers.");
        type.GetField("teacher",flags).SetValue(ui,false); record.status="completed";
        type.GetMethod("Render",flags).Invoke(ui,new object[]{record});
        if(answer.gameObject.activeSelf || answer.interactable) throw new Exception("Completed lesson still accepts answers.");
        record.status="learning"; clear.Invoke(null,new object[]{answer});
        type.GetMethod("Render",flags).Invoke(ui,new object[]{record});
    }
    static void VerifyMandatoryResults()
    {
        const BindingFlags instanceFlags = BindingFlags.NonPublic | BindingFlags.Instance;
        var type = typeof(FootballEssayController);
        var stateType = type.GetNestedType("State", BindingFlags.NonPublic);
        foreach (string game in new[] { "Football", "Sheep", "Cave" }) {
            var host = new GameObject("Offline " + game + " results");
            var practice = host.AddComponent<FootballEssayController>();
            type.GetField("practiceGame", instanceFlags).SetValue(practice, game);
            type.GetField("sheepSkin", instanceFlags).SetValue(practice, game == "Sheep");
            type.GetField("caveSkin", instanceFlags).SetValue(practice, game == "Cave");
            type.GetField("fallbackFont", instanceFlags).SetValue(practice, ui.GetComponentInChildren<TMP_Text>().font);
            type.GetMethod("Build", instanceFlags).Invoke(practice, null);
            var state = JsonUtility.FromJson("{\"token\":\"fixture\",\"state\":\"review\",\"subject\":\"Physics\",\"round\":1,\"rawScore\":4,\"earned\":4,\"text\":\"Một lò xo dài 10 cm. Treo 100 g thì dài 12 cm. Treo tổng cộng 200 g thì dài bao nhiêu?\",\"transcription\":\"45\"}", stateType);
            type.GetMethod("Apply", instanceFlags).Invoke(practice, new[] { state });
            var next = (Button)type.GetField("next", instanceFlags).GetValue(practice);
            if (next.interactable) throw new Exception(game + ": wrong work can still advance.");
            type.GetField("commandBusy", instanceFlags).SetValue(practice, true); // Prevent network; the click must still expose the cause.
            type.GetMethod("OpenTutor", instanceFlags).Invoke(practice, null);
            var message = (TMP_Text)type.GetField("resultNotice", instanceFlags).GetValue(practice);
            if (!message.gameObject.activeInHierarchy || !message.text.Contains("Render")) throw new Exception(game + ": legacy server error is hidden behind result board.");
            type.GetField("commandBusy", instanceFlags).SetValue(practice, false);
            stateType.GetField("tutorCompleted").SetValue(state, true);
            stateType.GetField("tutorSupported").SetValue(state, true);
            type.GetMethod("Apply", instanceFlags).Invoke(practice, new[] { state });
            if (!next.interactable) throw new Exception(game + ": completed tutorial did not unlock next question.");
            UnityEngine.Object.Destroy(host);
        }
    }
    static void Finish(int code) { EditorApplication.update -= Tick; SessionState.SetBool(Key, false); EditorApplication.Exit(code); }
}
#endif
