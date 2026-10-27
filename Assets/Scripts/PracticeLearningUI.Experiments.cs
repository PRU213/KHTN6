using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public partial class PracticeLearningUI
{
    PracticeExperiment experiment;
    PracticeExperimentDiagram diagram;
    string pickedCard;
    bool CanStartLesson(out string error)
    {
        error="Bài cần đủ ba bước thực hành.";
        if(steps==null||steps.Length!=3)return false;
        if(lesson.simulationType=="DoChieuDai") {error="Cấu hình đo chiều dài chưa hợp lệ.";return ValidConfig(lesson.configJson,out config);}
        try {return PracticeExperiment.Validate(lesson.simulationType,JsonUtility.FromJson<PracticeExperimentConfig>(lesson.configJson),out error);}
        catch(Exception) {error="Cấu hình mô phỏng không đọc được. Kiểm tra dữ liệu bài trong Sheet.";return false;}
    }
    void StartLesson()
    {
        string problem;if(!CanStartLesson(out problem)){Debug.LogWarning(problem);return;}
        if(lesson.simulationType=="DoChieuDai") {experiment=null;stepIndex=0;Measure();return;}
        experiment=new PracticeExperiment(lesson.simulationType,JsonUtility.FromJson<PracticeExperimentConfig>(lesson.configJson));
        ShowExperiment();
    }
    void ShowExperiment()
    {
        ResetPage(lesson.title,()=>{
            if(experiment.Back()) ShowExperiment(); else Detail(lesson.id);
        });pickedCard=null;
        var task=experiment.Current;
        Label(page,"Bước "+(experiment.Stage+1)+"/3 • "+task.Title,0,310,1460,48,34,TextAlignmentOptions.Center).fontStyle=FontStyles.Bold;
        var instruction=Label(page,task.Instruction,0,255,1460,64,27,TextAlignmentOptions.Center);
        instruction.enableAutoSizing=true;instruction.fontSizeMin=22;instruction.fontSizeMax=27;
        var area=Box(page,"Experiment diagram",-285,-5,900,350,new Color(.92f,.96f,.96f));
        var graphic=new GameObject("Interactive model",typeof(RectTransform)).GetComponent<RectTransform>();graphic.SetParent(area,false);graphic.sizeDelta=area.sizeDelta;
        diagram=graphic.gameObject.AddComponent<PracticeExperimentDiagram>();diagram.raycastTarget=false;diagram.Session=experiment;diagram.Refresh();
        Label(page,DiagramLegend(),-285,202,900,34,22,TextAlignmentOptions.Center);
        var observation=Label(page,experiment.Observation(),-285,-238,900,85,24,TextAlignmentOptions.Center);
        // Keep feedback below the controls, inside the paper and clear of
        // both the wooden footer and the answer cards on the left.
        feedback=Label(page,"Thao tác rồi bấm Kiểm tra bước.",515,-300,530,70,24,TextAlignmentOptions.Center);
        Btn(page,"Kiểm tra bước",610,FooterY,390,82,()=>{
            string hint;
            if(!experiment.Advance(out hint)){feedback.text=hint;feedback.color=new Color(.65f,.17f,.08f);return;}
            if(experiment.Completed)Result();else ShowExperiment();
        });
        // A scrollable control column prevents labels and controls from colliding on longer lessons.
        var view=Box(page,"Experiment controls",515,-20,530,470,new Color(1,1,1,.2f));view.gameObject.AddComponent<RectMask2D>();
        var scroll=view.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;
        var content=new GameObject("Controls",typeof(RectTransform)).GetComponent<RectTransform>();content.SetParent(view,false);
        content.anchorMin=new Vector2(0,1);content.anchorMax=new Vector2(1,1);content.pivot=new Vector2(.5f,1);
        scroll.content=content;scroll.viewport=view;
        var updates=new List<Action>();
        Action refresh=()=>{observation.text=experiment.Observation();foreach(var update in updates)update();diagram.Refresh();};
        if(experiment.Type=="TrongLuc") {
            Label(area,"N",40,65,60,30,25,TextAlignmentOptions.Center).fontStyle=FontStyles.Bold;
            for(int force=0;force<=5;force++)
                Label(area,force.ToString(),40,diagram.ForceMeterScaleY(force),45,26,22,TextAlignmentOptions.Center);
        }
        if(experiment.Type=="DoTheTich") {
            float capacity=experiment.CylinderCapacityMl();
            Label(area,"mL",135,153,70,30,22,TextAlignmentOptions.Center);
            for(int tick=0;tick<=10;tick++)
                Label(area,PracticeExperiment.N(capacity*tick/10),140,-130+tick*26,70,26,20,TextAlignmentOptions.MidlineLeft);
        }
        float y=-12;
        for(int i=0;i<task.Actions.Count;i++) {
            int index=i;var action=task.Actions[i];y-=38;
            var control=Btn(content,action.Label,0,y,490,65,()=>{experiment.Act(index);refresh();feedback.text="Đã thực hiện: "+action.Label+". "+(task.Numbers.Count>0?"Nhập số đo vào ô bên phải rồi kiểm tra.":"Tiếp tục các thao tác của bước này.");feedback.color=Ink;});Top(control.transform);
            updates.Add(()=>control.image.color=action.Toggle?(experiment.Get(action.Key)>0?new Color(.65f,1,.65f):Color.white):(experiment.Visited.Contains(index)?new Color(.65f,1,.65f):Color.white));
            y-=40;
        }
        if(task.Slider!=null) {
            var spec=task.Slider;y-=28;
            var readout=Label(content,"",0,y,490,40,26,TextAlignmentOptions.Center);Top(readout.transform);y-=45;
            var bar=Box(content,"Adjust "+spec.Key,0,y,460,36,new Color(.72f,.77f,.74f));Top(bar);
            var fill=Box(bar,"Fill",0,0,460,14,new Color(.26f,.65f,.37f));fill.anchorMin=new Vector2(0,.5f);fill.anchorMax=new Vector2(1,.5f);fill.sizeDelta=new Vector2(0,14);
            var handleArea=new GameObject("Handle area",typeof(RectTransform)).GetComponent<RectTransform>();handleArea.SetParent(bar,false);handleArea.anchorMin=Vector2.zero;handleArea.anchorMax=Vector2.one;handleArea.sizeDelta=new Vector2(-28,0);
            var handle=Box(handleArea,"Handle",0,0,30,50,new Color(1,.74f,.2f));
            var slider=bar.gameObject.AddComponent<Slider>();slider.fillRect=fill;slider.handleRect=handle;slider.targetGraphic=handle.GetComponent<Image>();slider.minValue=spec.Min;slider.maxValue=spec.Max;slider.wholeNumbers=spec.Whole;slider.SetValueWithoutNotify(experiment.Get(spec.Key));
            slider.onValueChanged.AddListener(value=>{experiment.SetSlider(value);refresh();});
            updates.Add(()=>readout.text=spec.Label+": "+PracticeExperiment.N(experiment.Get(spec.Key)));
            y-=67;
            if(spec.Samples!=null&&spec.Samples.Length>0){
                var record=Btn(content,"Ghi nhận",0,y,270,60,()=>{experiment.RecordSample();refresh();feedback.text="Đã ghi nhận "+experiment.Samples.Count+"/"+spec.Samples.Length+" mốc.";});Top(record.transform);y-=55;
                var samples=Label(content,"Mốc: "+string.Join(" • ",spec.Samples.Select(PracticeExperiment.N)),0,y,490,55,22,TextAlignmentOptions.Center);Top(samples.transform);y-=30;
            }
        }
        for(int i=0;i<task.Numbers.Count;i++) {
            int index=i;y-=28;var label=Label(content,task.Numbers[i].Label,0,y,480,40,25);Top(label.transform);y-=53;
            var input=Input(content,0,y,460,60);Top(input.transform);
            string savedAnswer;if(experiment.Answers.TryGetValue(index,out savedAnswer)) input.SetTextWithoutNotify(savedAnswer);
            input.onValueChanged.AddListener(text=>{experiment.SetNumber(index,text);refresh();});y-=42;
        }
        if(task.Choices!=null)for(int i=0;i<task.Choices.Length;i++) {
            int index=i;y-=48;var option=Btn(content,task.Choices[i],0,y,490,88,()=>{experiment.Choose(index);refresh();});Top(option.transform);
            option.GetComponentInChildren<TMP_Text>().fontSizeMin=21;
            updates.Add(()=>option.image.color=experiment.Choice==index?new Color(.65f,1,.65f):Color.white);y-=51;
        }
        if(task.Matches.Count>0) {
            observation.gameObject.SetActive(false);
            var options=task.Matches.Select(m=>m.Answer).Distinct().OrderBy(v=>v,StringComparer.Ordinal).ToArray();
            Label(page,"Chọn hoặc kéo thẻ vào ô bên phải",-285,-195,900,28,23,TextAlignmentOptions.Center);
            int columns=options.Length>4?4:2;float width=options.Length>4?216:430;
            for(int i=0;i<options.Length;i++) {
                int row=i/columns, rowColumns=Mathf.Min(columns,options.Length-row*columns);
                string payload=options[i];float x=-285+(i%columns-(rowColumns-1)*.5f)*(width+12), yy=-238-row*54;
                var token=Btn(page,payload,x,yy,width,48,()=>{pickedCard=payload;feedback.text="Đã chọn: "+payload+". Bấm vào ô cần ghép.";});
                var txt=token.GetComponentInChildren<TMP_Text>();txt.fontSizeMin=18;txt.fontSizeMax=23;
                var drag=token.gameObject.AddComponent<PracticeAnswerToken>();drag.Value=payload;drag.Pick=()=>pickedCard=payload;
            }
            for(int i=0;i<task.Matches.Count;i++) {
                int index=i;y-=54;
                var label=Label(content,task.Matches[i].Label,-180,y,150,104,23,TextAlignmentOptions.MidlineLeft);Top(label.transform);
                Action<string> place=value=>{if(string.IsNullOrEmpty(value)){feedback.text="Chọn thẻ đáp án ở phía dưới mô hình trước nhé.";return;}experiment.Place(index,value);refresh();};
                var target=Btn(content,"Chọn thẻ…",90,y,330,104,()=>place(pickedCard),true);Top(target.transform);
                var text=target.GetComponentInChildren<TMP_Text>();
                // Text stays inside the plain centre, away from wood and leaves.
                text.rectTransform.sizeDelta=new Vector2(210,62);
                text.fontSizeMin=19;text.fontSizeMax=22;text.fontStyle=FontStyles.Normal;
                var drop=target.gameObject.AddComponent<PracticeAnswerTarget>();drop.Accept=place;
                updates.Add(()=>{string value;text.text=experiment.Placements.TryGetValue(index,out value)?value:"Chọn thẻ…";});y-=64;
            }
        }
        content.sizeDelta=new Vector2(0,Mathf.Max(470,-y+20));
        if(experiment.Type=="NongChay") {
            // Coordinate labels share the diagram's 900 x 350 reference space.
            Label(area,"°C",-355,145,80,30,22,TextAlignmentOptions.Center);
            float low=Mathf.Min(-10,experiment.C.startC), high=15;
            for(float degree=low;degree<=high;degree+=5) {
                float yy=-115+(degree-low)/(high-low)*230;
                Label(area,PracticeExperiment.N(degree),-400,yy,55,28,21,TextAlignmentOptions.Center);
            }
            Label(area,"°C",-30,140,55,28,21,TextAlignmentOptions.Center);
            Label(area,"0",-25,0,40,28,21,TextAlignmentOptions.Center);
            Label(area,PracticeExperiment.N(experiment.C.startC),-30,-60,50,28,21,TextAlignmentOptions.Center);
            Label(area,"10",-30,110,45,28,21,TextAlignmentOptions.Center);
            foreach(int percent in new[]{0,30,70,100})Label(area,percent.ToString(),percent*3.4f,-125,65,28,21,TextAlignmentOptions.Center);
            Label(area,"Tiến trình cấp nhiệt (%)",180,-155,370,28,21,TextAlignmentOptions.Center);
            var reading=Label(area,"",-230,145,220,30,24,TextAlignmentOptions.Center);
            updates.Add(()=>reading.text=PracticeExperiment.N(experiment.Temperature())+" °C");
            if(experiment.Stage==0) {
                var hit=Box(area,"Bấm nhiệt kế",-355,0,95,290,Color.clear);
                var observe=hit.gameObject.AddComponent<Button>();
                observe.onClick.AddListener(()=>{experiment.Act(0);refresh();feedback.text="Đã quan sát nhiệt kế. Nhập nhiệt độ vào ô bên phải rồi bấm Kiểm tra bước.";feedback.color=Ink;});
            }
        }
        refresh();
    }
    static void Top(Transform transform){var rt=(RectTransform)transform;rt.anchorMin=rt.anchorMax=new Vector2(.5f,1);}
    string DiagramLegend()
    {
        switch(experiment.Type){
            case "CanKhoiLuong":return "Vật mẫu ở đĩa trái • Quả cân ở đĩa phải";
            case "TrongLuc":return "Lực kế • Đơn vị N • Đọc vạch ngang tại đầu dưới lò xo";
            case "DoTheTich":return "Ống đong "+PracticeExperiment.N(experiment.CylinderCapacityMl())+" mL • Mỗi khoảng chia: "+PracticeExperiment.N(experiment.CylinderCapacityMl()/10)+" mL";
            case "TruyenNhiet":return "Cốc A: không bọc • Cốc B: bọc cách nhiệt";
            case "NangLuongHaoPhi":return "Đèn A (trên), B (dưới) • Vàng: có ích • Đỏ: hao phí";
            case "OxygenSuChay":return "Từ trái sang phải: Không oxygen • Không khí • Giàu oxygen";
            case "ThanhPhanKhongKhi":return "Xanh: nitrogen • Vàng: oxygen • Xám: khí khác";
            case "NhanCanhBao":return "Từ trái sang phải: Dễ cháy • Ăn mòn • Độc (minh họa)";
            case "HeMatTroi":return "Mặt Trời ở trái • Kích thước và khoảng cách không đúng tỉ lệ";
            case "PhaMatTrang":return "Bên trái: Quỹ đạo nhìn từ trên • Bên phải: Nhìn từ Trái Đất";
            case "NongChay":return "Bên trái: Nước đá / nước • Bên phải: Đồ thị nhiệt độ – thời gian";
            case "NhienLieu":return "Từ trái sang phải: Củi • Than • Khí thiên nhiên • Cát • Kính";
            case "BaoQuanThucPham":return "Gói A: nơi khô • Gói B: giữ mát • Gói C: quá hạn";
            default:return "MÔ HÌNH THỰC HÀNH ẢO";
        }
    }
}

public class PracticeAnswerToken : MonoBehaviour,IBeginDragHandler,IDragHandler,IEndDragHandler
{
    public string Value; public Action Pick;
    RectTransform rect;Vector2 start;CanvasGroup group;
    public void OnBeginDrag(PointerEventData e){rect=(RectTransform)transform;start=rect.anchoredPosition;Pick?.Invoke();group=gameObject.AddComponent<CanvasGroup>();group.blocksRaycasts=false;group.alpha=.8f;}
    public void OnDrag(PointerEventData e){Vector2 point;if(rect!=null&&RectTransformUtility.ScreenPointToLocalPointInRectangle(rect.parent as RectTransform,e.position,e.pressEventCamera,out point))rect.anchoredPosition=point;}
    public void OnEndDrag(PointerEventData e){if(rect!=null)rect.anchoredPosition=start;if(group!=null)Destroy(group);}
}
public class PracticeAnswerTarget : MonoBehaviour,IDropHandler
{
    public Action<string> Accept;
    public void OnDrop(PointerEventData e){var token=e.pointerDrag!=null?e.pointerDrag.GetComponent<PracticeAnswerToken>():null;if(token!=null)Accept?.Invoke(token.Value);}
}
