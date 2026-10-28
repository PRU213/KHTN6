using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;

/// <summary>Chapter learning branch. Built at runtime so existing scene references stay intact.</summary>
public partial class PracticeLearningUI : MonoBehaviour
{
    [Serializable] public class Lesson {
        public string id, monId, chapterId, chapterTitle, title, objective, simulationType;
        public string equipment, predictionQuestion, instructions, conclusionQuestion, referenceAnswer;
        public string imageUrl, instructionImageUrl, safety, configJson;
        public int durationMinutes;
    }
    [Serializable] public class Step { public string id, title, action, observation, completionCondition, incorrectFeedback; }
    [Serializable] class Reply { public bool success; public string message, chapterTitle; public Lesson[] lessons; public Lesson lesson; public Step[] steps; }
    [Serializable] public class MeasureConfig { public float startCm, endCm, answer, tolerance = .1f; public string unit; }
    static PracticeLearningUI instance;
    static TMP_FontAsset font;
    readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();
    readonly List<UnityWebRequest> requests = new List<UnityWebRequest>();
    RectTransform root, page, stage;
    string subject, chapter, chapterTitle;
    Action theory, nextChapter;
    Lesson lesson;
    Step[] steps;
    MeasureConfig config;
    int generation, stepIndex;
    TMP_Text feedback, stepText, measureInstruction;
    TMP_InputField answer;
    RectTransform ruler, leftMarker, rightMarker;
    Sprite board, button, woodLabel, books, laboratory;
    const float FooterY = -432;
    static readonly Color Ink = new Color(.22f, .13f, .06f);

    public static void Open(string subject, string chapter, Action theory, Action nextChapter = null)
    {
        if (instance != null) Destroy(instance.gameObject);
        var go = new GameObject("Virtual practice", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        instance = go.AddComponent<PracticeLearningUI>();
        instance.subject = subject; instance.chapter = chapter; instance.theory = theory;
        instance.nextChapter = nextChapter;
        instance.Build(); instance.Choose();
    }
    void Build()
    {
        var canvas = GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 260;
        var scaler = GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920,1080); scaler.matchWidthOrHeight = .5f;
        if (font == null) {
            var source = Resources.Load<Font>("ScienceChatFont");
            font = source != null ? TMP_FontAsset.CreateFontAsset(source) : TMP_Settings.defaultFontAsset;
            if (font != null) font.isMultiAtlasTexturesEnabled = true;
        }
        root = (RectTransform)transform;
        var bg = Box(root, "Forest", 0,0,1920,1080,Color.white);
        bg.anchorMin = Vector2.zero; bg.anchorMax = Vector2.one; bg.sizeDelta = Vector2.zero;
        bg.GetComponent<Image>().sprite = Skin("Background", Vector4.zero);
        stage = new GameObject("Practice layout",typeof(RectTransform)).GetComponent<RectTransform>();
        stage.SetParent(root,false); stage.sizeDelta = new Vector2(1920,1080);
        board = Skin("WideBoard", new Vector4(160,125,160,125));
        woodLabel = Skin("WoodLabel",new Vector4(100,35,100,35));
        books = Skin("Books",Vector4.zero);
        // Select the visible artwork instead of squeezing its transparent padding into a button.
        button = Skin("Button",Vector4.zero,new Rect(.03f,.185f,.945f,.59f));
        laboratory = Skin("HomeIcons",Vector4.zero,new Rect(.158f,.055f,.125f,.225f));
        FitStage();
    }
    void LateUpdate() { FitStage(); }
    void FitStage()
    {
        if (stage == null) return;
        float scale = Mathf.Min(root.rect.width/1920f,root.rect.height/1080f);
        stage.localScale = Vector3.one * Mathf.Max(.01f,scale);
    }
    Sprite Skin(string name, Vector4 border, Rect? normalized = null)
    {
        var t = Resources.Load<Texture2D>("Practice/" + name);
        if (t == null) return null;
        Rect uv = normalized ?? new Rect(0,0,1,1);
        var s = Sprite.Create(t, new Rect(uv.x*t.width,uv.y*t.height,uv.width*t.width,uv.height*t.height), new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,border);
        owned.Add(s); return s;
    }
    void ResetPage(string title, Action back, string backLabel = "‹  Quay lại")
    {
        generation++;
        foreach (var request in requests) request.Abort();
        StopAllCoroutines();
        foreach (var request in requests) request.Dispose();
        requests.Clear();
        for (int i=owned.Count-1;i>=0;i--) {
            if (!(owned[i] is Texture2D)) continue;
            Destroy(owned[i]); owned.RemoveAt(i);
        }
        if (page != null) { page.gameObject.SetActive(false); Destroy(page.gameObject); }
        page = new GameObject("Practice page",typeof(RectTransform)).GetComponent<RectTransform>();
        page.SetParent(stage,false); page.sizeDelta = new Vector2(1920,1080);
        Panel(page,"Parchment",0,-18,1760,880,1.55f);
        var heading = Box(page,"Wooden heading",0,438,1300,155,Color.white);
        heading.GetComponent<Image>().sprite = woodLabel;
        heading.GetComponent<Image>().type = Image.Type.Sliced;
        var titleLabel = Label(heading,title,0,0,1050,90,46,TextAlignmentOptions.Center);
        titleLabel.fontStyle = FontStyles.Bold;
        titleLabel.enableAutoSizing = true; titleLabel.fontSizeMin = 30; titleLabel.fontSizeMax = 46;
        if(back!=null) Btn(page,backLabel,-665,FooterY,280,82,back,true);
    }
    void Choose()
    {
        ResetPage("KHÁM PHÁ KIẾN THỨC", () => Destroy(gameObject));
        Label(page,subject + " • Chương " + ChapterNumber(),0,310,1400,50,32,TextAlignmentOptions.Center).fontStyle=FontStyles.Bold;
        var chapterLabel = Label(page,chapterTitle ?? "Đang tải tên chương...",0,250,1400,50,28,TextAlignmentOptions.Center);
        chapterLabel.enableAutoSizing = true; chapterLabel.fontSizeMin = 20; chapterLabel.fontSizeMax = 28;
        if (string.IsNullOrEmpty(chapterTitle)) StartCoroutine(Fetch("&chapterId=" + UnityWebRequest.EscapeURL(chapter), result => {
            if (result.success && !string.IsNullOrWhiteSpace(result.chapterTitle)) {
                chapterTitle = result.chapterTitle;
                chapterLabel.text = chapterTitle;
            }
            else chapterLabel.text = "Không tải được tên chương. Em có thể quay lại để thử lại.";
        }));
        ModeCard(-370,books,"LÍ THUYẾT","Đọc kiến thức, rồi thử sức với trò chơi.",() => { var next = theory; gameObject.SetActive(false); Destroy(gameObject); next?.Invoke(); });
        ModeCard(370,laboratory,"THỰC HÀNH ẢO","Tự thao tác, quan sát và khám phá.",List);
    }
    void ModeCard(float x,Sprite illustration,string title,string description,Action action)
    {
        var card=Panel(page,title,x,-65,650,535,2.8f);
        // Leave a safe inset from the decorative wood and leaf border.
        Art(card,illustration,0,80,410,240);
        Btn(card,title,0,-95,490,84,action);
        Label(card,description,0,-180,470,56,25,TextAlignmentOptions.Center);
    }
    string ChapterNumber() { int n; return int.TryParse(chapter.Substring(chapter.LastIndexOf('_')+1),out n) ? n.ToString() : chapter; }
    void List()
    {
        ResetPage("PHÒNG THỰC HÀNH",Choose);
        Label(page,subject + " • Chương " + ChapterNumber(),0,315,1400,50,32,TextAlignmentOptions.Center).fontStyle=FontStyles.Bold;
        var state = Label(page,"Đang tải bài thực hành...",0,255,1400,50,26,TextAlignmentOptions.Center);
        Btn(page,"Tải lại",665,FooterY,280,82,List,true);
        StartCoroutine(Fetch("practiceList&chapterId=" + UnityWebRequest.EscapeURL(chapter), result => {
            if (!result.success) { state.text = result.message; return; }
            var lessons = result.lessons ?? new Lesson[0];
            state.text = lessons.Length == 0 ? "Chương này chưa có bài thực hành được duyệt." : lessons.Length + " bài sẵn sàng • Chọn một bài để khám phá";
            // Scroll keeps all approved lessons reachable, including chapters with more than three lessons.
            var view = Box(page,"Lessons",0,-65,1510,580,new Color(1,1,1,0));
            view.gameObject.AddComponent<RectMask2D>();
            var scroll = view.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false;
            var content = new GameObject("Content",typeof(RectTransform)).GetComponent<RectTransform>(); content.SetParent(view,false);
            content.anchorMin = new Vector2(0,1); content.anchorMax = new Vector2(1,1); content.pivot = new Vector2(.5f,1);
            content.sizeDelta = new Vector2(0,Mathf.Max(580,Mathf.CeilToInt(lessons.Length/3f) * 580));
            scroll.viewport = view; scroll.content = content; scroll.movementType = ScrollRect.MovementType.Clamped;
            for (int i=0;i<lessons.Length;i++) {
                var item = lessons[i];
                int row=i/3, column=i%3, columns=Mathf.Min(3,lessons.Length-row*3);
                var card=Panel(content,"Lesson "+item.id,0,0,485,550,2.8f);
                card.anchorMin=card.anchorMax=new Vector2(.5f,1);
                card.anchoredPosition=new Vector2((column-(columns-1)*.5f)*500,-285-row*580);
                Picture(card,item.imageUrl,item.id,false,0,125,350,190);
                var name=Label(card,item.title,0,-42,350,105,28,TextAlignmentOptions.Center);
                name.fontStyle=FontStyles.Bold; name.enableAutoSizing=true; name.fontSizeMin=23; name.fontSizeMax=28;
                Label(card,item.durationMinutes+" phút • Thực hành ảo",0,-127,350,32,23,TextAlignmentOptions.Center);
                Btn(card,"Khám phá  ›",0,-187,330,70,()=>Detail(item.id));
            }
        }));
    }
    IEnumerator Fetch(string query, Action<Reply> done)
    {
        int ticket = generation;
        var req = UnityWebRequest.Get(AppsScriptEndpoint.Url + "?action=" + query); req.timeout = 30; requests.Add(req);
        yield return req.SendWebRequest();
        Reply result = null;
        if (req.result != UnityWebRequest.Result.Success) result = new Reply { message = "Không tải được dữ liệu: " + req.error };
        else {
            try { result = JsonUtility.FromJson<Reply>(req.downloadHandler.text); }
            catch (Exception) { result = new Reply { message = "Máy chủ không trả JSON hợp lệ. Kiểm tra bản triển khai Apps Script." }; }
        }
        requests.Remove(req); req.Dispose();
        if (ticket == generation) done(result ?? new Reply { message = "Dữ liệu trống." });
    }
    void Detail(string id)
    {
        ResetPage("CHUẨN BỊ THỰC HÀNH",List);
        var state = Label(page,"Đang tải nội dung...",0,80,1350,180,28,TextAlignmentOptions.Center);
        StartCoroutine(Fetch("practiceDetail&lessonId=" + UnityWebRequest.EscapeURL(id), result => {
            if (!result.success || result.lesson == null) { state.text = result.message; Btn(page,"Thử lại",665,FooterY,280,82,()=>Detail(id)); return; }
            lesson = result.lesson; steps = result.steps ?? new Step[0]; state.gameObject.SetActive(false);
            Label(page,lesson.title,0,305,1430,72,34,TextAlignmentOptions.Center).fontStyle=FontStyles.Bold;
            ScrollText(page,"Preparation text","Mục tiêu\n" + lesson.objective + "\n\nDụng cụ\n" + lesson.equipment + "\n\nEm dự đoán thế nào?\n" + lesson.predictionQuestion,-395,30,650,400,27);
            Label(page,"Thực hành trên mô hình ảo. " + lesson.safety,0,-285,1430,80,23,TextAlignmentOptions.Center);
            Picture(page,lesson.imageUrl,lesson.id,false,385,35,680,400);
            string problem;
            bool supported = CanStartLesson(out problem);
            if (supported) Btn(page,"Bắt đầu  ›",665,FooterY,280,82,StartLesson);
            else Label(page,problem,385,-193,680,85,24,TextAlignmentOptions.Center);
        }));
    }
    public static bool ValidConfig(string json, out MeasureConfig value)
    {
        value = null;
        try { value = JsonUtility.FromJson<MeasureConfig>(json); } catch (Exception) { return false; }
        return value != null && !float.IsNaN(value.startCm) && !float.IsNaN(value.endCm)
            && value.startCm >= 0 && value.endCm > value.startCm && value.endCm <= 15
            && value.tolerance >= 0 && value.tolerance <= .5f && value.unit == "cm"
            && Mathf.Abs(value.answer - (value.endCm-value.startCm)) <= .001f;
    }
    void Measure()
    {
        ResetPage(lesson.title,()=>{
            if(stepIndex>0) {stepIndex--;UpdateStep();} else Detail(lesson.id);
        });
        stepText = Label(page,"",0,310,1430,48,32,TextAlignmentOptions.Center);
        stepText.fontStyle=FontStyles.Bold;
        measureInstruction=Label(page,"",0,250,1430,68,27,TextAlignmentOptions.Center);
        // Pencil ends are fixed relative to the ruler's horizontal scale; move the ruler vertically to align.
        var table = Box(page,"Measurement table",0,35,1410,260,new Color(.87f,.77f,.52f,.65f));
        float origin = -480, unit = 60;
        Box(table,"Pencil",origin+(config.startCm+config.endCm)*unit/2,65,(config.endCm-config.startCm)*unit,25,new Color(.95f,.58f,.08f));
        Label(table,"Vật cần đo",0,108,600,40,22,TextAlignmentOptions.Center);
        ruler = Box(table,"Ruler",0,-75,1000,65,new Color(1,.96f,.7f));
        var drag = ruler.gameObject.AddComponent<PracticePointerDrag>();
        drag.move = p => { if(stepIndex==0) ruler.anchoredPosition = new Vector2(0,Mathf.Clamp(p.y,-80,35)); };
        for(int i=0;i<=15;i++) {
            float x=origin+i*unit;
            Box(ruler,"Tick",x,17,2,25,Ink);
            Label(ruler,i.ToString(),x,-10,45,35,20,TextAlignmentOptions.Center);
        }
        Label(ruler,"cm",470,-10,50,35,18,TextAlignmentOptions.Center);
        leftMarker = Marker(table,"A",origin,unit);
        rightMarker = Marker(table,"B",origin+15*unit,unit);
        if(stepIndex>0) ruler.anchoredPosition=new Vector2(0,20);
        if(stepIndex==2) {
            leftMarker.anchoredPosition=new Vector2(origin+config.startCm*unit,65);
            rightMarker.anchoredPosition=new Vector2(origin+config.endCm*unit,65);
        }
        answer = Input(page,0,-160,600,65);
        feedback = Label(page,"",0,-250,1390,105,25,TextAlignmentOptions.Center);
        Btn(page,"Kiểm tra bước",610,FooterY,390,82,Check);
        UpdateStep();
    }
    RectTransform Marker(RectTransform table, string name, float x, float unit)
    {
        var marker = Box(table,name,x,65,42,65,new Color(.1f,.6f,.88f,.85f));
        var label = Label(marker,name,0,0,100,55,20,TextAlignmentOptions.Center);
        var drag = marker.gameObject.AddComponent<PracticePointerDrag>();
        drag.move = p => {
            if(stepIndex != 1) return;
            float snapped = Mathf.Round(Mathf.Clamp((p.x+480)/unit,0,15)*10)/10;
            marker.anchoredPosition = new Vector2(-480+snapped*unit,65);
            label.text = name + "\n" + snapped.ToString("0.#",CultureInfo.InvariantCulture);
        };
        return marker;
    }
    void UpdateStep()
    {
        // The available interactions change per step. Keep instructions aligned
        // with those controls even when an older Sheet still describes them differently.
        string[] titles = { "Đặt thước", "Đọc hai đầu vật", "Tính chiều dài" };
        string[] actions = {
            "Kéo phần nền thước lên sát mép dưới vật. Chưa cần di chuyển mốc A và B.",
            "Kéo mốc A tới đầu trái và mốc B tới đầu phải của vật; đọc hai vị trí trên thước.",
            "Lấy số đọc ở B trừ số đọc ở A. Nhập kết quả bằng cm (chỉ nhập số)."
        };
        stepText.text = "Bước " + (stepIndex+1) + "/3 • " + titles[stepIndex];
        measureInstruction.text=actions[stepIndex];
        answer.gameObject.SetActive(stepIndex==2);
        if(stepIndex==2) answer.placeholder.GetComponent<TMP_Text>().text="Chiều dài (cm) — chỉ nhập số";
        feedback.text = stepIndex==0 ? "Kéo thước lên sát dưới vật, rồi kiểm tra." : stepIndex==1 ? "Kéo mốc A và B tới đúng hai đầu vật để đọc số đo." : "Nhập chiều dài vật (cm). Lưu ý vị trí đầu vật khác vạch 0.";
    }
    void Check()
    {
        bool ok;
        if(stepIndex==0) ok = Mathf.Abs(ruler.anchoredPosition.y-20)<=12;
        else if(stepIndex==1) ok = Mathf.Abs((leftMarker.anchoredPosition.x+480)/60-config.startCm)<=config.tolerance+.001f
            && Mathf.Abs((rightMarker.anchoredPosition.x+480)/60-config.endCm)<=config.tolerance+.001f;
        else {
            float number;
            ok = float.TryParse(answer.text.Trim().Replace(',','.'),NumberStyles.Float,CultureInfo.InvariantCulture,out number)
                && !float.IsNaN(number) && Mathf.Abs(number-config.answer)<=config.tolerance+.001f;
        }
        if(!ok) {
            feedback.text = stepIndex==0 ? "Thước chưa sát vật. Kéo nền thước để mép trên sát mép dưới vật."
                : stepIndex==1 ? "Hai mốc chưa đúng vị trí. Đặt A ở đầu trái và B ở đầu phải của vật."
                : "Chiều dài chưa đúng. Lấy số đọc ở B trừ số đọc ở A; chỉ nhập số, đơn vị cm.";
            return;
        }
        if(stepIndex<2) { stepIndex++; UpdateStep(); } else Result();
    }
    void Result()
    {
        ResetPage("HOÀN THÀNH THỰC HÀNH",()=>{
            if(experiment==null) Measure();
            else if(experiment.Back()) ShowExperiment();
        });
        // Crop only the transparent padding; keep the supplied red close artwork.
        var closeSprite=Skin("Close",Vector4.zero,new Rect(.1f,.125f,.78125f,.78125f));
        if(closeSprite!=null) {
            var close=Box(page,"Thoát thực hành",820,400,148,148,Color.white);
            var image=close.GetComponent<Image>();image.sprite=closeSprite;image.preserveAspect=true;
            var closeButton=close.gameObject.AddComponent<Button>();
            closeButton.targetGraphic=image;closeButton.onClick.AddListener(ShowCompletionExit);
        } else Btn(page,"×",820,400,148,148,ShowCompletionExit,true);
        Label(page,experiment==null ? "Em đã đo được " + config.answer.ToString("0.#",CultureInfo.InvariantCulture) + " cm!" : "Em đã hoàn thành cả 3 bước thực hành!",0,300,1400,70,38,TextAlignmentOptions.Center).fontStyle=FontStyles.Bold;
        ScrollText(page,"Conclusion text",lesson.conclusionQuestion + "\n\n" + lesson.referenceAnswer,-395,65,650,290,29);
        Picture(page,lesson.instructionImageUrl,lesson.id,true,385,45,680,420);
        Btn(page,"Hỏi AI thêm",-300,-185,350,80,()=>ScienceChatController.ForReview(this).OpenReview(
            "practice:"+lesson.id,lesson.title,"Bài thực hành: "+lesson.title+"\nMục tiêu: "+lesson.objective+"\nCấu hình: "+lesson.configJson+"\nQuan sát: "+(experiment==null?"Đo chiều dài":string.Join("\n",experiment.Journal))+"\nKết luận: "+lesson.referenceAnswer,new Color(.2f,.6f,.3f)));
        Btn(page,"Làm lại",665,FooterY,280,82,StartLesson);
        Label(page,"Kết quả thực hành hiện chưa được lưu vào lịch sử trận đấu.",0,-300,1400,55,22,TextAlignmentOptions.Center);
    }
    void ShowCompletionExit()
    {
        if(page.Find("Completion exit")!=null) return;
        var overlay=Box(page,"Completion exit",0,0,1920,1080,new Color(0,0,0,.55f));
        Panel(overlay,"Exit choices",0,0,1240,760,1.9f);
        Label(overlay,"Em muốn học tiếp như thế nào?",0,260,1050,70,36,TextAlignmentOptions.Center).fontStyle=FontStyles.Bold;
        Btn(overlay,"Chọn bài thực hành khác",0,135,850,90,List);
        Btn(overlay,nextChapter!=null?"Học chương tiếp":"Về màn chọn chương",0,15,850,90,()=>{
            var next=nextChapter;
            gameObject.SetActive(false);
            Destroy(gameObject);
            next?.Invoke();
        });
        Btn(overlay,"Trang chủ",0,-105,850,90,GoHomeFromPractice);
        Label(overlay,"Về Trang chủ để chọn môn học khác.",0,-180,1050,50,25,TextAlignmentOptions.Center);
        Btn(overlay,"Ở lại xem kết quả",0,-270,500,75,()=>{
            overlay.gameObject.SetActive(false);
            Destroy(overlay.gameObject);
        },true);
    }
    void GoHomeFromPractice()
    {
        // Include inactive panels and other loaded scenes: this runtime overlay
        // need not belong to the same scene as the existing home screen.
        foreach(var home in FindObjectsByType<HomeMenuController>(FindObjectsInactive.Include)) {
            if(home.homeCanvas==null || !home.gameObject.scene.IsValid() || !home.gameObject.scene.isLoaded) continue;
            home.BackToHome();
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }
        // Some preview/prefab setups have no HomeMenuController. Use the same
        // HomePage destination as login and the other home navigation buttons.
        if(Application.CanStreamedLevelBeLoaded("HomePage")) {
            UnityEngine.SceneManagement.SceneManager.LoadScene("HomePage");
            return;
        }
        var overlay=page.Find("Completion exit");
        if(overlay!=null && overlay.Find("Home navigation error")==null) {
            var error=Label(overlay,"Chưa mở được Trang chủ. Hãy kiểm tra scene HomePage trong Build Settings.",0,-350,1080,55,23,TextAlignmentOptions.Center);
            error.name="Home navigation error";
        }
        Debug.LogWarning("Không tìm thấy màn Trang chủ và scene HomePage chưa được thêm vào Build Settings.");
    }
    void Picture(Transform parent,string url,string lessonId,bool instruction,float x,float y,float w,float h)
    {
        var host=Box(parent,"Illustration "+lessonId,x,y,w,h,new Color(1,.98f,.92f));
        host.gameObject.AddComponent<RectMask2D>();
        var imageRect=new GameObject("Picture",typeof(RectTransform),typeof(RawImage)).GetComponent<RectTransform>();
        imageRect.SetParent(host,false); imageRect.sizeDelta=host.sizeDelta;
        var raw=imageRect.GetComponent<RawImage>(); raw.raycastTarget=false;
        // Bundled illustrations give immediate feedback even when Google Drive is slow or unavailable.
        Texture2D bundled=null;
        if(System.Text.RegularExpressions.Regex.IsMatch(lessonId??"","^TH_(PHY|CHM)_[0-9]{2}_[0-9]{2}$"))
            bundled=Resources.Load<Texture2D>("Practice/Lessons/"+lessonId+(instruction?"_huongdan":"_daidien"));
        ApplyPicture(raw,bundled,host.sizeDelta);
        var state=Label(host,bundled!=null?"":"Đang tải ảnh...",0,0,w-24,h-24,22,TextAlignmentOptions.Center);
        if(string.IsNullOrWhiteSpace(url)) {
            if(bundled==null) state.text="Bài này chưa có ảnh minh họa.";
            return;
        }
        StartCoroutine(LoadPicture(url,raw,state,host.sizeDelta,bundled!=null));
    }
    static void ApplyPicture(RawImage image,Texture2D texture,Vector2 bounds)
    {
        image.texture=texture; image.enabled=texture!=null;
        if(texture==null) return;
        float ratio=Mathf.Min(bounds.x/texture.width,bounds.y/texture.height);
        image.rectTransform.sizeDelta=new Vector2(texture.width*ratio,texture.height*ratio);
    }
    IEnumerator LoadPicture(string url,RawImage raw,TMP_Text state,Vector2 bounds,bool hasBundled)
    {
        int ticket=generation;
        var request=UnityWebRequestTexture.GetTexture(url); request.timeout=25; requests.Add(request);
        yield return request.SendWebRequest();
        if(ticket==generation && raw!=null) {
            Texture2D texture=null;
            if(request.result==UnityWebRequest.Result.Success) {
                try {texture=DownloadHandlerTexture.GetContent(request);} catch(Exception) { }
            }
            if(texture!=null) {
                owned.Add(texture); state.gameObject.SetActive(false);
                ApplyPicture(raw,texture,bounds);
            } else if(hasBundled) {
                state.text="Ảnh minh họa đi kèm"; state.fontSize=17;
                state.rectTransform.sizeDelta=new Vector2(bounds.x-20,28);
                state.rectTransform.anchoredPosition=new Vector2(0,-bounds.y/2+17);
            } else state.text="Chưa tải được ảnh. Em có thể quay lại và thử tải lại bài.";
        }
        requests.Remove(request); request.Dispose();
    }
    RectTransform Box(Transform parent,string name,float x,float y,float w,float h,Color color)
    {
        var rt=new GameObject(name,typeof(RectTransform),typeof(Image)).GetComponent<RectTransform>(); rt.SetParent(parent,false);
        rt.sizeDelta=new Vector2(w,h);rt.anchoredPosition=new Vector2(x,y);rt.GetComponent<Image>().color=color;return rt;
    }
    RectTransform Panel(Transform parent,string name,float x,float y,float w,float h,float borderScale)
    {
        var rect=Box(parent,name,x,y,w,h,Color.white);
        var image=rect.GetComponent<Image>();image.sprite=board;image.type=Image.Type.Sliced;image.pixelsPerUnitMultiplier=borderScale;
        return rect;
    }
    void Art(Transform parent,Sprite sprite,float x,float y,float w,float h)
    {
        var rect=Box(parent,"Illustration",x,y,w,h,Color.white);
        var image=rect.GetComponent<Image>(); image.sprite=sprite; image.preserveAspect=true;image.raycastTarget=false;
    }
    TMP_Text Label(Transform parent,string text,float x,float y,float w,float h,int size,TextAlignmentOptions align=TextAlignmentOptions.TopLeft)
    {
        var rt=new GameObject("Text",typeof(RectTransform)).GetComponent<RectTransform>();rt.SetParent(parent,false);rt.sizeDelta=new Vector2(w,h);rt.anchoredPosition=new Vector2(x,y);
        var t=rt.gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.text=text;t.fontSize=size;t.color=Ink;t.alignment=align;t.raycastTarget=false;t.richText=false;
        t.enableAutoSizing=true;t.fontSizeMax=size;t.fontSizeMin=Mathf.Min(size,20);
        t.overflowMode=TextOverflowModes.Ellipsis;
        return t;
    }
    void ScrollText(Transform parent,string name,string text,float x,float y,float w,float h,int size)
    {
        var viewport=Box(parent,name,x,y,w,h,Color.clear);
        viewport.gameObject.AddComponent<RectMask2D>();
        var scroll=viewport.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity=28;
        var body=Label(viewport,text,0,0,w-24,h,size,TextAlignmentOptions.TopLeft);
        body.enableAutoSizing=false;body.overflowMode=TextOverflowModes.Overflow;
        body.lineSpacing=5;body.paragraphSpacing=8;
        float contentHeight=Mathf.Max(h,body.GetPreferredValues(text,w-24,float.PositiveInfinity).y+16);
        var rect=body.rectTransform;
        rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);rect.pivot=new Vector2(.5f,1);
        rect.anchoredPosition=Vector2.zero;rect.sizeDelta=new Vector2(w-24,contentHeight);
        scroll.viewport=viewport;scroll.content=rect;
    }
    Button Btn(Transform parent,string text,float x,float y,float w,float h,Action action,bool wooden=false)
    {
        var rt=Box(parent,text,x,y,w,h,button!=null ? Color.white : new Color(.3f,.7f,.25f));
        var im=rt.GetComponent<Image>();im.sprite=wooden?woodLabel:button;im.type=wooden?Image.Type.Sliced:Image.Type.Simple;
        if(wooden) im.pixelsPerUnitMultiplier=1.9f;
        var b=rt.gameObject.AddComponent<Button>();b.onClick.AddListener(()=>action());
        var t=Label(rt,text,0,0,w-40,h-16,31,TextAlignmentOptions.Center);t.fontStyle=FontStyles.Bold;
        t.enableAutoSizing=true;t.fontSizeMin=24;t.fontSizeMax=31;
        if(!wooden) {t.color=Color.white;t.outlineColor=new Color32(12,51,103,255);t.outlineWidth=.16f;}
        var colors=b.colors;colors.highlightedColor=new Color(1,.95f,.75f);colors.pressedColor=new Color(.8f,.88f,1);b.colors=colors;
        return b;
    }
    TMP_InputField Input(Transform parent,float x,float y,float w,float h)
    {
        var rt=Box(parent,"Answer",x,y,w,h,Color.white);
        // TMP initializes its caret and text callbacks in OnEnable. Assign all
        // references before enabling a dynamically-created input field.
        rt.gameObject.SetActive(false);
        var field=rt.gameObject.AddComponent<TMP_InputField>();
        var viewport=new GameObject("Text Area",typeof(RectTransform),typeof(RectMask2D)).GetComponent<RectTransform>();
        viewport.SetParent(rt,false);
        viewport.anchorMin=Vector2.zero;viewport.anchorMax=Vector2.one;
        viewport.offsetMin=new Vector2(15,5);viewport.offsetMax=new Vector2(-15,-5);
        var t=Label(viewport,"",0,0,w-30,h-10,28,TextAlignmentOptions.MidlineLeft);
        var placeholder=Label(viewport,"Nhập số đo…",0,0,w-30,h-10,26,TextAlignmentOptions.MidlineLeft);
        placeholder.color=new Color(.4f,.4f,.4f);
        field.textViewport=viewport;field.textComponent=t as TextMeshProUGUI;
        field.placeholder=placeholder;field.targetGraphic=rt.GetComponent<Image>();
        field.characterLimit=20;field.lineType=TMP_InputField.LineType.SingleLine;
        field.contentType=TMP_InputField.ContentType.Standard;
        field.readOnly=false;field.interactable=true;
        field.customCaretColor=true;field.caretColor=Ink;field.caretWidth=2;
        field.SetTextWithoutNotify("");
        rt.gameObject.SetActive(true);
        return field;
    }
    void OnDestroy()
    {
        foreach(var req in requests) {req.Abort();req.Dispose();}requests.Clear();
        foreach(var asset in owned) if(asset!=null) Destroy(asset);
        if(instance==this) instance=null;
    }
}

/// <summary>Convert pointer positions using the actual Canvas camera and scale.</summary>
public class PracticePointerDrag : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    public Action<Vector2> move;
    public void OnBeginDrag(PointerEventData data) { OnDrag(data); }
    public void OnDrag(PointerEventData data)
    {
        Vector2 point;
        if(RectTransformUtility.ScreenPointToLocalPointInRectangle(transform.parent as RectTransform,data.position,data.pressEventCamera,out point)) move?.Invoke(point);
    }
}
