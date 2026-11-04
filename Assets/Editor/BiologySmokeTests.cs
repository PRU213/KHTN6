#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Biology-only regression checks. No submissions, credentials or photos are sent to a server.</summary>
[InitializeOnLoad]
public static class BiologySmokeTests
{
    const string Key = "BiologySmokeTests.Running", Command = "Temp/BiologyCheck.command", Account = "biology-ui-fixture";
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    [Serializable] class Report { public string completed; public List<string> passed = new List<string>(), failed = new List<string>(); }
    [Serializable] class Pref { public string key, value; public bool exists; }
    [Serializable] class Prefs { public List<Pref> items = new List<Pref>(); }
    static Report report;
    static IEnumerator suite;
    static int frame = -1;
    static double deadline;
    static BiologySmokeTests()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += state => {
            if (!SessionState.GetBool(Key, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode) {
                BeginSuite();
            }
            if (state == PlayModeStateChange.EnteredEditMode) Restore();
        };
    }

    [MenuItem("Tools/KHTN6/Sinh hoc/Kiem tra nap cau hoi va hoc lai")]
    public static void Run()
    {
        if (EditorApplication.isPlaying || SessionState.GetBool(Key, false)) return;
        report = new Report(); ValidateBank();
        SessionState.SetBool(Key + ".finishing", false);
        var prefs = new Prefs();
        foreach (string key in new[] { "SelectedChapter", "SelectedDifficulty", "Biology.PendingEssayTutor." + Account })
            prefs.items.Add(new Pref { key = key, exists = PlayerPrefs.HasKey(key), value = PlayerPrefs.GetString(key, "") });
        SessionState.SetString(Key + ".prefs", JsonUtility.ToJson(prefs));
        SessionState.SetString(Key + ".scene", AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
        SessionState.SetString(Key + ".report", JsonUtility.ToJson(report)); SessionState.SetBool(Key, true);
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Biology/bean_1_question.unity");
        EditorApplication.isPlaying = true;
    }

    static void Tick()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        // Script refreshes may occur between the play/edit transition callbacks.
        if (SessionState.GetBool(Key, false)) {
            if (!EditorApplication.isPlayingOrWillChangePlaymode) Restore();
            else if (EditorApplication.isPlaying && suite == null && !SessionState.GetBool(Key + ".finishing", false)) BeginSuite();
        }
        if (File.Exists(Command)) {
            string command = File.ReadAllText(Command).Trim(); File.Delete(Command);
            if (command == "run") Run();
        }
        if (suite == null || !EditorApplication.isPlaying || frame == Time.frameCount) return;
        frame = Time.frameCount;
        try {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Biology smoke timeout.");
            if (!suite.MoveNext()) Finish();
        } catch (Exception error) { Check(false, "Runner: " + error); Finish(); }
    }

    static void BeginSuite()
    {
        if (suite != null) return;
        report = JsonUtility.FromJson<Report>(SessionState.GetString(Key + ".report", "{}"));
        GameSession.Username = Account; GameSession.Subject = "Sinh học";
        PlayerPrefs.SetString("SelectedChapter", "Chương 1"); PlayerPrefs.SetString("SelectedDifficulty", "Dễ");
        if (GameData.Instance == null) new GameObject("Biology fixture bank").AddComponent<GameData>();
        GameData.Instance.allQuestions = Fixture();
        SceneManager.sceneLoaded -= PrepareScene; SceneManager.sceneLoaded += PrepareScene;
        suite = Suite(); deadline = EditorApplication.timeSinceStartup + 150;
    }

    static void PrepareScene(Scene scene, LoadSceneMode mode)
    {
        foreach (var root in scene.GetRootGameObjects()) foreach (var controller in root.GetComponentsInChildren<TuLuanController>(true))
            controller.loadQuestionsFromGoogleSheets = false;
    }
    static List<QuestionData> Fixture() => new List<QuestionData> {
        new QuestionData { id="BIO_FIX_1", monId="Sinh học", topic="Chương 1: Tế bào", difficulty="Dễ", question="BIOLOGY FIXTURE QUESTION", optionA="Wrong A",optionB="Right B",optionC="Wrong C",optionD="Wrong D",correct="B",explanation="B is correct",points=10 },
        new QuestionData { id="BIO_FIX_2", monId="Sinh học", topic="Chương 2: Cơ thể", difficulty="Khó", question="OTHER BIOLOGY CHAPTER", optionA="A",optionB="B",optionC="C",optionD="D",correct="D",points=10 },
        new QuestionData { id="PHY_FIX", monId="Vật lý", topic="Chương 1", difficulty="Dễ", question="UNRELATED PHYSICS", optionA="A",optionB="B",optionC="C",optionD="D",correct="A",points=10 }
    };
    static void ValidateBank()
    {
        var bank = Fixture();
        Check(BiologyQuestionBank.Select(bank, "Chương 1", "Dễ").Single().id == "BIO_FIX_1", "Filter exact subject/chapter/difficulty");
        Check(BiologyQuestionBank.Select(bank, "Chương 7", "Dễ").Count == 0, "Empty filter never takes a different chapter");
        Check(BiologyQuestionBank.Select(bank, "Tất cả", "Tổng hợp").Count == 2, "All chapters excludes Physics");
        bank[0].correct = "invalid";
        Check(BiologyQuestionBank.Select(bank, "Chương 1", "Dễ").Count == 0, "Invalid correct answer never defaults to A");
        string csv = "option_d,id,monID,question,option_b,correct_option,option_a,Chương / Chủ đề,explanation,difficulty,option_c,points\r\nD,BIO_CSV,Sinh học,\"Câu có dấu phẩy, và xuống\ndòng\",B,C,A,Chương 1,\"Giải thích \"\"đúng\"\"\",Dễ,C,12\r\n";
        var q = BiologyQuestionBank.ParseCsv(csv).Single();
        Check(q.optionA == "A" && q.optionB == "B" && q.optionC == "C" && q.optionD == "D" && BiologyQuestionBank.CorrectIndex(q) == 2, "Header mapping preserves A/B/C/D despite moved columns");
        Check(q.question.Contains("\n") && q.explanation == "Giải thích \"đúng\"" && q.points == 12, "Quoted CSV and points preserved");
        Check(TuLuanController.RequiresBiologyRemediation(4.99f) && !TuLuanController.RequiresBiologyRemediation(5) && !TuLuanController.RequiresBiologyRemediation(6), "Biology boundaries: below 5 required, 5 and 6 retain pass");
        ValidateGradingProtocol();
    }

    static void ValidateGradingProtocol()
    {
        TuLuanGradingResult grade; string status, error;
        const string completed = "{\"success\":true,\"submissionId\":\"BIO_FIX\",\"status\":\"Completed\",\"completed\":true,\"score\":0,\"feedback\":\"Cần bổ sung các thành phần tế bào\"}";
        Check(BiologyGradingStatus.ReadJson(completed,"BIO_FIX",out grade,out status,out error) && grade.score==0 && !grade.isMock, "Genuine zero score with feedback is a valid completed grade");
        Check(!BiologyGradingStatus.ReadJson(completed.Replace("\"score\":0","\"score\":null"),"BIO_FIX",out grade,out status,out error) && status=="Invalid", "Missing score never silently becomes zero");
        Check(!BiologyGradingStatus.ReadJson(completed.Replace("Completed","Pending"),"BIO_FIX",out grade,out status,out error), "Pending status cannot apply a numeric grade");
        Check(!BiologyGradingStatus.ReadJson(completed,"OTHER_ID",out grade,out status,out error), "A different submission cannot supply this grade");
        Check(!BiologyGradingStatus.ReadJson(completed.Replace("\"score\":0","\"score\":1e9"),"BIO_FIX",out grade,out status,out error) && status=="Invalid", "Out-of-range exponent is rejected rather than read as one");
        Check(!BiologyGradingStatus.ReadJson("{\"success\":true,\"submissionId\":\"BIO_FIX\",\"status\":\"Failed\",\"message\":\"script.external_request\"}","BIO_FIX",out grade,out status,out error) && status=="Failed" && error.Contains("cấp quyền"), "AI permission failure terminates waiting immediately");
        var rows=new List<string[]> {new[]{"NhanXetAI","TrangThaiCham","SubmissionID","DiemAI"}, new[]{"Đúng trọng tâm","Completed","BIO_FIX","6"}};
        Check(BiologyGradingStatus.ReadRows(rows,"BIO_FIX",out grade,out status,out error) && grade.score==6 && grade.reviewText=="Đúng trọng tâm", "Grading uses named Sheet columns after column reordering");
        rows.Add(rows[1]);
        Check(!BiologyGradingStatus.ReadRows(rows,"BIO_FIX",out grade,out status,out error) && status=="Invalid", "Duplicate Sheet submission IDs cannot supply an arbitrary grade");
    }

    static IEnumerator Suite()
    {
        var prefs = JsonUtility.FromJson<Prefs>(SessionState.GetString(Key + ".prefs", "{}"));
        if (!prefs.items.Any(p => p.key == "SelectedQuestionType")) {
            prefs.items.Add(new Pref { key="SelectedQuestionType",exists=PlayerPrefs.HasKey("SelectedQuestionType"),value=PlayerPrefs.GetString("SelectedQuestionType","") });
            SessionState.SetString(Key + ".prefs", JsonUtility.ToJson(prefs));
        }
        foreach (string scene in new[] { "bean_1_question", "Bean_2_Biology_Question", "Bean_3_Biology_Question", "beantong_question" }) {
            // Reload the first scene too: EnteredPlayMode may follow its initial Start.
            SceneManager.LoadScene(scene);
            for (int i = 0; i < 5; i++) yield return null;
            MonoBehaviour manager = UnityEngine.Object.FindFirstObjectByType<QuestionManager>();
            if (manager == null) manager = UnityEngine.Object.FindFirstObjectByType<QuestionManagerTong>();
            Check(manager != null, scene + " loads its Biology manager");
            var questions = (Array)Field(manager, "questions");
            Check(questions.Length == 1 && (string)questions.GetValue(0).GetType().GetField("questionText").GetValue(questions.GetValue(0)) == "BIOLOGY FIXTURE QUESTION", scene + " displays only selected bank");
            var answers = (Text[])Field(manager, "answerTexts");
            Check(answers.All(text => text != null) && answers[1].text.Contains("Right B") && answers[2].text.Contains("Wrong C"), scene + " answer slots match Sheet order");
            var buttons = (Button[])Field(manager, "answerButtons");
            bool clicked = Click(buttons[1].gameObject);
            Check(clicked && ((Text)Field(manager, "feedbackText")).text.Contains("Chính xác"), scene + " B pointer click is graded correctly");
        }
        using (var request = UnityWebRequest.Get(BiologyQuestionBank.CsvUrl)) {
            request.timeout = 25; var loading = request.SendWebRequest(); while (!loading.isDone) yield return null;
            Check(request.result == UnityWebRequest.Result.Success, "Linked Questions Sheet responds");
            if (request.result == UnityWebRequest.Result.Success) {
                var real = BiologyQuestionBank.ParseCsv(request.downloadHandler.text);
                Check(real.Count > 0 && real.All(q => BiologyQuestionBank.IsBiology(q.monId)), "Live Sheet supplies validated Biology records");
            }
        }
        SceneManager.LoadScene("ChonBai"); for (int i = 0; i < 4; i++) yield return null;
        var chooser = UnityEngine.Object.FindFirstObjectByType<ChonBaiManager>();
        double waitUntil = EditorApplication.timeSinceStartup + 30;
        while ((bool)Field(chooser,"biologyBankLoading") && EditorApplication.timeSinceStartup < waitUntil) yield return null;
        var bank = (List<QuestionData>)Field(chooser,"allQuestions");
        Check(bank.Count > 0 && bank.All(q => BiologyQuestionBank.IsBiology(q.monId) && !q.id.StartsWith("BIO_DEF_")), "Biology chooser loads real Sheet records without generated placeholders");
        Set(chooser,"selectedChapter","Chương 999"); Set(chooser,"selectedDifficulty","Dễ");
        typeof(ChonBaiManager).GetMethod("StartGame",Private).Invoke(chooser,null); yield return null;
        Check(SceneManager.GetActiveScene().name == "ChonBai", "Empty chooser filter cannot start a quiz from another chapter");
        var notice = (GameObject)Field(chooser,"biologyBankNotice");
        Check(notice.activeInHierarchy && Click(notice.GetComponentInChildren<Button>().gameObject) && !notice.activeSelf, "Empty chooser filter shows a dismissible message");
        chooser.StopAllCoroutines();
        var first = bank.First();
        GameData.Instance.allQuestions = bank;
        PlayerPrefs.SetString("SelectedChapter",first.topic); PlayerPrefs.SetString("SelectedDifficulty",first.difficulty);
        SceneManager.LoadScene("bean_1_question"); for (int i = 0; i < 4; i++) yield return null;
        var liveManager = UnityEngine.Object.FindFirstObjectByType<QuestionManager>();
        var displayed = ((Text)Field(liveManager,"questionText")).text;
        var liveQuestion = bank.FirstOrDefault(q => q.question == displayed);
        var liveAnswers = (Text[])Field(liveManager,"answerTexts");
        Check(liveQuestion != null && liveAnswers[0].text.Contains(liveQuestion.optionA) && liveAnswers[1].text.Contains(liveQuestion.optionB)
            && liveAnswers[2].text.Contains(liveQuestion.optionC) && liveAnswers[3].text.Contains(liveQuestion.optionD), "Live Biology question and all four answers stay on the same Sheet row");
        Capture("biology-live-quiz"); yield return null; yield return null;
        PlayerPrefs.SetString("SelectedChapter","Chương 1"); PlayerPrefs.SetString("SelectedDifficulty","Dễ");
        SceneManager.LoadScene("TuLuan"); for (int i = 0; i < 6; i++) yield return null;
        var controller = UnityEngine.Object.FindFirstObjectByType<TuLuanController>();
        var guide = UnityEngine.Object.FindFirstObjectByType<BiologyEssayGuide>();
        Check(controller != null && guide != null && guide.GetComponentsInChildren<TMP_Text>().Any(t => t.text.Contains("Dưới 5 điểm")), "Biology essay opens its instructions");
        Capture("biology-instructions"); yield return null; yield return null;
        var guideButton = guide.GetComponentsInChildren<Button>().Single();
        Check(Click(guideButton.gameObject), "Instructions button receives pointer clicks");
        PopupLoadingController.Show("AI đang chấm bài...", "Đang nhận điểm và nhận xét.");
        PopupLoadingController.Hide();
        for(int i=0;i<12;i++) yield return null;
        Check(!SceneManager.GetSceneByName("PopupLoading").isLoaded, "Hide while loading cannot leave an orphaned spinner scene");
        PopupLoadingController.Show("AI đang chấm bài...", "Đang nhận điểm và nhận xét.");
        for(int i=0;i<12;i++) yield return null;
        Check(PopupLoadingController.Instance != null && PopupLoadingController.Instance.txtSub.text=="Đang nhận điểm và nhận xét.", "Loading retains its specific progress message");
        Time.timeScale=0;
        Set(controller,"isSubmitting",true); Set(controller,"submissionCoroutine",null);
        yield return null; yield return null;
        Check(!controller.IsSubmitting && controller.btnNopBai.interactable, "Interrupted grading recovers even with paused game time");
        for(int i=0;i<8;i++) yield return null;
        var gradingNotice=UnityEngine.Object.FindFirstObjectByType<BiologySubmissionNotice>();
        Check(gradingNotice != null && !SceneManager.GetSceneByName("PopupLoading").isLoaded, "Interrupted grading closes loading and keeps a persistent retry notice");
        Capture("biology-grading-retry"); yield return null; yield return null;
        var backToAnswer=gradingNotice.GetComponentsInChildren<Button>().Single(b=>b.name=="Về bài làm");
        Check(Click(backToAnswer.gameObject) && !gradingNotice.gameObject.activeSelf, "Grading error lets the student return to the preserved answer");
        Time.timeScale=1;
        const string realResultFile="outputs/biology/real-grading-retry.json";
        if(File.Exists(realResultFile)) {
            var saved=JsonUtility.FromJson<EssaySubmissionSaveResponse>(File.ReadAllText(realResultFile));
            if(saved.success && saved.completed) {
                bool readFinished=false, readSuccess=false; TuLuanGradingResult liveGrade=null;
                controller.StartCoroutine(controller.PollGradingResultRoutine(saved.submissionId,(ok,grade,error)=>{readSuccess=ok;liveGrade=grade;readFinished=true;}));
                double readDeadline=EditorApplication.timeSinceStartup+30;
                while(!readFinished && EditorApplication.timeSinceStartup<readDeadline) yield return null;
                Check(readSuccess && liveGrade != null && liveGrade.score==saved.score && liveGrade.reviewText==saved.feedback && !liveGrade.isMock, "Unity reads the actual completed AI grade and comments for the repaired submission");
                if(readSuccess) {
                    typeof(TuLuanController).GetMethod("AcceptBiologyGrade",Private).Invoke(controller,new object[]{new TuLuanSubmissionData{submissionId=saved.submissionId,isSavedToSheet=true},liveGrade});
                    for(int i=0;i<5;i++) yield return null;
                    var livePopup=PopupKetQuaController.Instance;
                    Check(livePopup != null && livePopup.txtDiem.text.Contains(saved.score.ToString("0.0")) && livePopup.txtNhanXet.text.Contains(saved.feedback), "Actual AI score and comments are visible in the Biology result popup");
                    Check(!controller.HasPendingBiologyTutor && !controller.IsSubmitting, "Actual six-point grade passes without copying or a stuck submission");
                    Capture("biology-real-ai-result"); yield return null; yield return null;
                    if(SceneManager.GetSceneByName("PopupKetQua").isLoaded) SceneManager.UnloadSceneAsync("PopupKetQua");
                    for(int i=0;i<5;i++) yield return null;
                }
            }
        }
        Set(controller, "currentSubmissionId", "BIOLOGY_UI_SUBMISSION");
        GameData.Instance.currentHealth = 3;
        var result = new TuLuanGradingResult { score=4, maxScore=10, reviewText="Fixture", status="CHƯA ĐẠT", isMock=true };
        typeof(TuLuanController).GetMethod("AcceptBiologyGrade",Private).Invoke(controller,new object[]{new TuLuanSubmissionData{submissionId="BIOLOGY_UI_SUBMISSION"},result}); controller.ShowResultPopup(result);
        for (int i = 0; i < 4; i++) yield return null;
        Check(GameData.Instance.currentHealth == 2 && controller.HasPendingBiologyTutor && TuLuanController.HasSavedBiologyTutor, "Low score loses one heart once and persists its obligation");
        Check(controller.BindBiologyTutorial("bio-lesson", "biology:BIOLOGY_UI_SUBMISSION"), "Tutor continuation binds only the matching submission");
        Check(!controller.BindBiologyTutorial("wrong", "biology:some-other-submission"), "Other submission cannot claim this Biology question");
        Set(controller, "biologyTutorSubmissionId", "");
        var previous = Field(controller, "currentQuestion"); controller.LoadNextQuestion();
        Check(ReferenceEquals(previous, Field(controller, "currentQuestion")), "Pending Biology learning blocks next question without a server confirmation");
        foreach (var popup in SceneManager.GetAllScenes().Where(s => s.name == "PopupKetQua")) SceneManager.UnloadSceneAsync(popup);
        for (int i = 0; i < 4; i++) yield return null;
        guide.gameObject.SetActive(false);
        var ui = (EssayTutorUI)typeof(EssayTutorUI).GetMethod("Get", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
        ui.gameObject.SetActive(true);
        typeof(EssayTutorUI).GetField("username", Private).SetValue(ui, Account);
        var record = new EssayTutorUI.Record { id="bio-lesson", sourceId="biology:BIOLOGY_UI_SUBMISSION",subject="Biology",chapter="Chương 1: Tế bào",question="Tế bào là gì?",score=4,finalScore=4,status="learning",step=0,total=3,prompt="Cơ thể sống gồm những đơn vị nào?",
            penalty=new EssayTutorUI.Penalty { status="deferred" } };
        Render(ui, record);
        Check(((TMP_InputField)Field(ui, "input")).interactable && !((Button)Field(ui, "continueButton")).gameObject.activeSelf, "Biology AI step accepts answers and blocks continuation");
        Capture("biology-ai-guidance"); yield return null; yield return null;
        record.status="completed"; record.step=3; record.solution="Tế bào là đơn vị cơ sở của sự sống.";
        record.penalty=new EssayTutorUI.Penalty { status="copying",biologyChapter=true,chapterContent="Mọi cơ thể sống gồm tế bào.\nMàng tế bào, chất tế bào và nhân.",redoQuestion=record.question,redoSolution=record.solution,content="Đủ chương và đáp án",required=1,count=0 };
        Render(ui, record);
        Check(ui.GetComponentsInChildren<TMP_Text>().Any(t => t.text.Contains("NỘI DUNG CHƯƠNG CẦN CHÉP")) && !((Button)Field(ui, "continueButton")).gameObject.activeSelf, "Copying displays full chapter and keeps the next-question gate");
        Capture("biology-copy-chapter"); yield return null; yield return null;
        Set(controller, "biologyTutorSubmissionId", "BIOLOGY_UI_SUBMISSION");
        record.penalty.status="completed"; record.penalty.count=1; Render(ui, record);
        var continueButton=(Button)Field(ui,"continueButton");
        Check(continueButton.gameObject.activeSelf && continueButton.interactable && continueButton.GetComponentInChildren<TMP_Text>().text == "Tiếp tục Sinh học", "Server-completed guide and copying expose Biology continuation");
        var legacy=new EssayTutorUI.Record {id="physics",sourceId="practice:physics:0",subject="Physics",status="completed",solution="P=20N",total=3,step=3};
        Render(ui,legacy);
        Check(continueButton.GetComponentInChildren<TMP_Text>().text == "Về trang chủ", "Unrelated practice continuation keeps its original behavior");
    }

    static object Field(object target, string name) => target.GetType().GetField(name, Private).GetValue(target);
    static void Set(object target,string name,object value) => target.GetType().GetField(name,Private).SetValue(target,value);
    static void Render(EssayTutorUI ui,EssayTutorUI.Record record) => typeof(EssayTutorUI).GetMethod("Render",Private).Invoke(ui,new object[]{record});
    static void Check(bool success,string name) { (success?report.passed:report.failed).Add(name); }
    static void Capture(string name) { Directory.CreateDirectory("outputs/biology"); ScreenCapture.CaptureScreenshot("outputs/biology/"+name+".png"); }
    static bool Click(GameObject expected)
    {
        Canvas.ForceUpdateCanvases();
        var rect=(RectTransform)expected.transform;
        var data=new PointerEventData(EventSystem.current) {position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center)),button=PointerEventData.InputButton.Left};
        var hits=new List<RaycastResult>(); EventSystem.current.RaycastAll(data,hits);
        if(hits.Count==0||ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject)!=expected)return false;
        ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,data,ExecuteEvents.pointerDownHandler);
        ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,data,ExecuteEvents.pointerUpHandler);
        ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,data,ExecuteEvents.pointerClickHandler);return true;
    }
    static void Finish()
    {
        SessionState.SetBool(Key + ".finishing", true);
        suite=null; SceneManager.sceneLoaded-=PrepareScene; report.completed=DateTime.Now.ToString("s");
        Directory.CreateDirectory("outputs/biology"); File.WriteAllText("outputs/biology/unity-report.json",JsonUtility.ToJson(report,true));
        Debug.Log("Biology checks: "+report.passed.Count+" passed, "+report.failed.Count+" failed.");
        EditorApplication.isPlaying=false;
    }
    static void Restore()
    {
        var prefs=JsonUtility.FromJson<Prefs>(SessionState.GetString(Key+".prefs","{}"));
        foreach(var p in prefs.items) {if(p.exists)PlayerPrefs.SetString(p.key,p.value);else PlayerPrefs.DeleteKey(p.key);}
        PlayerPrefs.Save();
        string scene=SessionState.GetString(Key+".scene","");
        EditorSceneManager.playModeStartScene=string.IsNullOrEmpty(scene)?null:AssetDatabase.LoadAssetAtPath<SceneAsset>(scene);
        SessionState.SetBool(Key,false);
    }
}
#endif
