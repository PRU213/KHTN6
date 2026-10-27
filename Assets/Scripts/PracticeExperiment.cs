using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

[Serializable]
public class PracticeExperimentConfig
{
    public float massG, answer, beforeMl, afterMl, leftN, initialSpeed, gravityNPerKg, speed, initialC, roomC, input, startC, meltingC, nitrogenPercent, oxygenPercent, otherPercent;
    public float[] weightsG, forceLevelsN, rightLevelsN, massesG, weightsN, frictionLevels, dragN, afterC, useful, waste, rotationDegrees, temperaturesC, particleSizes, sieveSizes;
    public string unit, pressure, solute, solvent, recover;
    public string[] surfaces, shapes, situations, actions, chain, phases, order, required, labels, errors, states, oxygenLevels, sources, parts, matches, fuels, notFuels, items, mixture;
    public bool elasticRange, friction, storage, illustrativeData, sunFixed, notToScale, eclipse, virtualOnly, particleModel, pureWater, dryAir, qualitative, labelDriven, soluble, idealized;
}

// Deterministic, data-only experiment state. Scene changes and elapsed frames cannot complete a task.
public class PracticeExperiment
{
    public class ActionItem {
        public string Label, Key; public float Value; public bool Toggle, Required=true;
        public ActionItem(string label,string key,float value=1,bool toggle=false,bool required=true){Label=label;Key=key;Value=value;Toggle=toggle;Required=required;}
    }
    public class NumberItem { public string Label; public float Expected, Tolerance; public NumberItem(string label,float expected,float tolerance=.05f){Label=label;Expected=expected;Tolerance=tolerance;} }
    public class MatchItem { public string Label, Answer; public MatchItem(string label,string answer){Label=label;Answer=answer;} }
    public class SliderItem { public string Label, Key; public float Min,Max,Initial; public float[] Samples; public float? Target; public bool Whole; }
    public class Task {
        public string Title, Instruction;
        public List<ActionItem> Actions=new List<ActionItem>(); public List<NumberItem> Numbers=new List<NumberItem>(); public List<MatchItem> Matches=new List<MatchItem>();
        public SliderItem Slider; public string[] Choices; public int CorrectChoice=-1; public Func<PracticeExperiment,bool> Extra;
    }
    public readonly string Type;
    public readonly PracticeExperimentConfig C;
    public readonly Task[] Tasks;
    public int Stage {get;private set;}
    public bool Completed {get {return Stage>=Tasks.Length;}}
    public Task Current {get {return Tasks[Math.Min(Stage,Tasks.Length-1)];}}
    public readonly Dictionary<string,float> Values=new Dictionary<string,float>();
    public readonly HashSet<int> Visited=new HashSet<int>();
    public readonly HashSet<float> Samples=new HashSet<float>();
    public readonly Dictionary<int,string> Answers=new Dictionary<int,string>();
    public readonly Dictionary<int,string> Placements=new Dictionary<int,string>();
    public readonly List<string> Journal=new List<string>();
    public int Choice {get;private set;}=-1;
    public string LastAction {get;private set;}="";
    class StepState {
        public Dictionary<string,float> Values;
        public int[] Visited; public float[] Samples;
        public Dictionary<int,string> Answers,Placements;
        public int Choice; public string LastAction;
    }
    readonly Stack<StepState> previousSteps=new Stack<StepState>();
    void SaveStep(){previousSteps.Push(new StepState{
        Values=new Dictionary<string,float>(Values),Visited=Visited.ToArray(),Samples=Samples.ToArray(),
        Answers=new Dictionary<int,string>(Answers),Placements=new Dictionary<int,string>(Placements),
        Choice=Choice,LastAction=LastAction});}
    public bool Back(){
        if(Stage==0||previousSteps.Count==0)return false;
        var saved=previousSteps.Pop();Stage--;
        Values.Clear();foreach(var item in saved.Values)Values[item.Key]=item.Value;
        Visited.Clear();Visited.UnionWith(saved.Visited);Samples.Clear();Samples.UnionWith(saved.Samples);
        Answers.Clear();foreach(var item in saved.Answers)Answers[item.Key]=item.Value;
        Placements.Clear();foreach(var item in saved.Placements)Placements[item.Key]=item.Value;
        Choice=saved.Choice;LastAction=saved.LastAction;
        if(Journal.Count>Stage)Journal.RemoveRange(Stage,Journal.Count-Stage);
        return true;
    }
    public float Get(string key,float fallback=0){float v;return Values.TryGetValue(key,out v)?v:fallback;}
    public static string N(float n){return n.ToString("0.##",CultureInfo.InvariantCulture);}
    static bool Finite(float n){return !float.IsNaN(n)&&!float.IsInfinity(n);}
    static bool Positive(float[] values,int min=1){return values!=null&&values.Length>=min&&values.All(v=>Finite(v)&&v>0);}
    static bool Names(string[] values,int count){return values!=null&&values.Length==count&&values.All(v=>!string.IsNullOrWhiteSpace(v))&&values.Distinct().Count()==count;}
    static bool Same(string[] a,params string[] b){return a!=null&&a.SequenceEqual(b);}
    public static readonly string[] SupportedTypes={"CanKhoiLuong","DoTheTich","LucBienDang","HaiLucNguocChieu","TrongLuc","MaSatBeMat","LucCanChatLong","MaSatDoiSong","ChuyenHoaNangLuong","TruyenNhiet","NangLuongHaoPhi","NgayDem","PhaMatTrang","HeMatTroi","TrangBiBaoHo","NhanCanhBao","TimLoiAnToan","BaTheCuaChat","NongChay","BayHoiVaSoi","ThanhPhanKhongKhi","OxygenSuChay","BaoVeKhongKhi","ChonVatLieu","NhienLieu","BaoQuanThucPham","LocHonHop","BayHoiThuMuoi","RayHonHop"};
    public static bool Validate(string type,PracticeExperimentConfig c,out string error)
    {
        error="Cấu hình bài chưa phù hợp với mô phỏng.";if(c==null)return false;bool ok=false;
        switch(type){
            case "CanKhoiLuong":ok=c.massG>0&&Finite(c.massG)&&Positive(c.weightsG)&&c.weightsG.Length<=6&&Math.Abs(c.weightsG.Sum()-c.massG)<.01f&&c.unit=="g";break;
            case "DoTheTich":ok=Finite(c.beforeMl)&&Finite(c.afterMl)&&c.beforeMl>0&&c.afterMl>c.beforeMl&&Math.Abs(c.answer-(c.afterMl-c.beforeMl))<.01f;break;
            case "LucBienDang":ok=c.elasticRange&&c.forceLevelsN!=null&&c.forceLevelsN.Length==3&&c.forceLevelsN[0]==0&&c.forceLevelsN[1]>0&&c.forceLevelsN[2]>c.forceLevelsN[1]&&Finite(c.forceLevelsN[2]);break;
            case "HaiLucNguocChieu":ok=!c.friction&&c.initialSpeed==0&&c.leftN>0&&Positive(c.rightLevelsN,2)&&c.rightLevelsN.Length==2&&c.rightLevelsN[0]==c.leftN&&c.rightLevelsN[1]>c.leftN;break;
            case "TrongLuc":ok=Positive(c.massesG,2)&&c.massesG.Length==2&&c.gravityNPerKg>0&&Finite(c.gravityNPerKg);break;
            case "MaSatBeMat":ok=Names(c.surfaces,3)&&Positive(c.frictionLevels,3)&&c.frictionLevels.Length==3&&c.frictionLevels.Distinct().Count()==3&&c.initialSpeed>0&&Finite(c.initialSpeed);break;
            case "LucCanChatLong":ok=Names(c.shapes,3)&&Positive(c.dragN,3)&&c.dragN.Length==3&&c.dragN.Distinct().Count()==3&&c.speed>0;break;
            case "MaSatDoiSong":ok=Same(c.situations,"de_giay","phanh_xe","ban_le")&&Same(c.actions,"tang","tang","giam");break;
            case "ChuyenHoaNangLuong":ok=!c.storage&&Same(c.chain,"co_nang","dien_nang","anh_sang_va_nhiet");break;
            case "TruyenNhiet":ok=Positive(c.afterC,2)&&c.afterC.Length==2&&c.initialC>c.afterC.Max()&&c.roomC<c.afterC.Min()&&Finite(c.initialC)&&c.afterC[0]!=c.afterC[1];break;
            case "NangLuongHaoPhi":ok=c.input>0&&Positive(c.useful,2)&&Positive(c.waste,2)&&c.useful.Length==2&&c.waste.Length==2&&c.useful[0]!=c.useful[1]&&Math.Abs(c.useful[0]+c.waste[0]-c.input)<.01f&&Math.Abs(c.useful[1]+c.waste[1]-c.input)<.01f;break;
            case "NgayDem":ok=c.sunFixed&&c.rotationDegrees!=null&&c.rotationDegrees.SequenceEqual(new[]{0f,90f,180f,270f,360f});break;
            case "PhaMatTrang":ok=!c.eclipse&&Same(c.phases,"trang_moi","ban_nguyet","trang_tron");break;
            case "HeMatTroi":ok=Same(c.order,"Thuy","Kim","TraiDat","Hoa","Moc","Tho","ThienVuong","HaiVuong");break;
            case "TrangBiBaoHo":ok=Same(c.required,"kinh_bao_ho","ao_choang","giay_kin","toc_gon");break;
            case "NhanCanhBao":ok=Same(c.labels,"de_chay","an_mon","doc");break;
            case "TimLoiAnToan":ok=Same(c.errors,"do_an","coc_khong_nhan","chai_khong_day");break;
            case "BaTheCuaChat":ok=Same(c.states,"ran","long","khi")&&c.particleModel;break;
            case "NongChay":ok=c.pureWater&&c.pressure=="1 atm"&&c.startC<c.meltingC&&c.meltingC==0&&Finite(c.startC);break;
            case "BayHoiVaSoi":ok=Positive(c.temperaturesC,2)&&c.temperaturesC.Length==2&&c.temperaturesC[0]<100&&c.temperaturesC[1]==100&&c.pressure=="1 atm";break;
            case "ThanhPhanKhongKhi":ok=c.dryAir&&c.nitrogenPercent>0&&c.oxygenPercent>0&&c.otherPercent>=0&&Math.Abs(c.nitrogenPercent+c.oxygenPercent+c.otherPercent-100)<.01f;break;
            case "OxygenSuChay":ok=Same(c.oxygenLevels,"khong_co","khong_khi","giau_oxygen");break;
            case "BaoVeKhongKhi":ok=Same(c.sources,"xe_no_may","dot_rac","bui");break;
            case "ChonVatLieu":ok=Same(c.parts,"than_noi","tay_cam","cua_so")&&Same(c.matches,"kim_loai","cach_nhiet_phu_hop","kinh");break;
            case "NhienLieu":ok=Same(c.fuels,"cui","than","khi_thien_nhien")&&Same(c.notFuels,"cat","kinh");break;
            case "BaoQuanThucPham":ok=Same(c.items,"goi_kho","goi_can_mat","goi_qua_han")&&c.labelDriven;break;
            case "LocHonHop":ok=Same(c.mixture,"cat","nuoc")&&!c.soluble;break;
            case "BayHoiThuMuoi":ok=c.solute=="muoi"&&c.solvent=="nuoc"&&c.recover=="muoi";break;
            case "RayHonHop":ok=Positive(c.particleSizes,2)&&c.particleSizes.Length==2&&c.particleSizes[0]<c.particleSizes[1]&&Positive(c.sieveSizes,3)&&c.sieveSizes.Length==3&&c.sieveSizes.Any(v=>v>c.particleSizes[0]&&v<c.particleSizes[1]);break;
        }
        if(ok)error=null;return ok;
    }
    public PracticeExperiment(string type,PracticeExperimentConfig config){string e;if(!Validate(type,config,out e))throw new ArgumentException(e+" ("+type+")");Type=type;C=config;Tasks=Build();BeginStage();}
    void BeginStage(){Visited.Clear();Samples.Clear();Answers.Clear();Placements.Clear();Choice=-1;if(Current.Slider!=null)Values[Current.Slider.Key]=Current.Slider.Initial;}
    public void Act(int index){if(Completed||index<0||index>=Current.Actions.Count)return;var a=Current.Actions[index];Values[a.Key]=a.Toggle?(Get(a.Key)==0?1:0):a.Value;Visited.Add(index);LastAction=a.Label;if(Current.Slider!=null&&a.Key==Current.Slider.Key)RecordSample();if(a.Key=="containerLiquid")Values["sample"]=1;if(a.Key=="containerGas")Values["sample"]=2;}
    public void SetSlider(float value){var s=Current.Slider;if(Completed||s==null||!Finite(value))return;Values[s.Key]=Math.Max(s.Min,Math.Min(s.Max,s.Whole?(float)Math.Round(value):value));}
    public void RecordSample(){var s=Current.Slider;if(s==null)return;foreach(float v in s.Samples??new float[0])if(Math.Abs(Get(s.Key)-v)<.01f)Samples.Add(v);LastAction="Đã ghi nhận: "+N(Get(s.Key));}
    public void SetNumber(int i,string text){if(!Completed&&i>=0&&i<Current.Numbers.Count)Answers[i]=text??"";}
    public void Place(int i,string v){if(!Completed&&i>=0&&i<Current.Matches.Count&&Current.Matches.Any(m=>m.Answer==v))Placements[i]=v;}
    public void Choose(int i){if(!Completed&&Current.Choices!=null&&i>=0&&i<Current.Choices.Length)Choice=i;}
    public bool Ready(out string hint){
        hint="";if(Completed)return true;
        for(int i=0;i<Current.Actions.Count;i++)if(Current.Actions[i].Required&&!Visited.Contains(i)){hint="Bấm “"+Current.Actions[i].Label+"” trước, rồi làm theo hướng dẫn.";return false;}
        var s=Current.Slider;if(s!=null){if((s.Samples??new float[0]).Any(v=>!Samples.Contains(v))){hint="Kéo tới từng mốc yêu cầu và bấm Ghi nhận.";return false;}if(s.Target.HasValue&&Math.Abs(Get(s.Key)-s.Target.Value)>.01f){hint="Đưa thanh điều chỉnh về giá trị yêu cầu.";return false;}}
        for(int i=0;i<Current.Numbers.Count;i++){string text;float n;if(!Answers.TryGetValue(i,out text)||!float.TryParse(text.Trim().Replace(',','.'),NumberStyles.Float,CultureInfo.InvariantCulture,out n)||!Finite(n)||Math.Abs(n-Current.Numbers[i].Expected)>Current.Numbers[i].Tolerance){hint="Số đo chưa đúng. Hãy đọc lại mô hình và đơn vị.";return false;}}
        for(int i=0;i<Current.Matches.Count;i++){string v;if(!Placements.TryGetValue(i,out v)||v!=Current.Matches[i].Answer){hint="Các thẻ chưa ghép đúng hết. Hãy đối chiếu với điều vừa quan sát.";return false;}}
        if(Current.CorrectChoice>=0&&Choice!=Current.CorrectChoice){hint="Nhận xét chưa phù hợp với kết quả quan sát.";return false;}
        if(Current.Extra!=null&&!Current.Extra(this)){hint="Mô hình chưa đạt điều kiện. Hãy kiểm tra dụng cụ đang chọn.";return false;}return true;
    }
    public bool Advance(out string hint){if(Completed){hint="Bài đã hoàn thành.";return false;}if(!Ready(out hint))return false;SaveStep();Journal.Add(Current.Title+": "+Observation());Stage++;if(!Completed)BeginStage();return true;}
    public float WeightSum(){float sum=0;for(int i=0;i<(C.weightsG??new float[0]).Length;i++)sum+=Get("weight"+i)*C.weightsG[i];return sum;}
    // Use a graduated capacity (100 mL for the chapter-one lesson), never
    // derive an arbitrary scale such as 91 mL from the displaced water level.
    public float CylinderCapacityMl(){return Math.Max(100,(float)Math.Ceiling(C.afterMl/100f)*100);}
    public float Temperature(){float p=Get("time");return p<30?C.startC+(C.meltingC-C.startC)*p/30:p<=70?C.meltingC:C.meltingC+(p-70)/3;}
    static Task T(string title,string instruction,params ActionItem[] actions){return new Task{Title=title,Instruction=instruction,Actions=actions.ToList()};}
    static Task Quiz(string title,string instruction,string correct,params string[] other){return new Task{Title=title,Instruction=instruction,Choices=other.Concat(new[]{correct}).ToArray(),CorrectChoice=other.Length};}
    static Task Match(string title,string instruction,string[] labels,string[] answers){return new Task{Title=title,Instruction=instruction,Matches=labels.Select((s,i)=>new MatchItem(s,answers[i])).ToList()};}
    static Task Explore(string title,string instruction,string key,string[] labels,float[] values=null){return T(title,instruction,labels.Select((s,i)=>new ActionItem(s,key,values==null?i:values[i])).ToArray());}
    static Task Slide(string title,string instruction,string key,string label,float min,float max,float initial,float[] samples,float? target=null){return new Task{Title=title,Instruction=instruction,Slider=new SliderItem{Key=key,Label=label,Min=min,Max=max,Initial=initial,Samples=samples,Target=target,Whole=true}};}
    Task[] Build(){Task a,b,c;switch(Type){
case "CanKhoiLuong":
 a=T("Đặt vật lên cân","Bấm Đặt vật để đưa vật mẫu lên đĩa trái.",new ActionItem("Đặt vật lên đĩa trái","object"));
 b=T("Làm cân thăng bằng","Bấm nút khối lượng để thêm quả cân; bấm lại để nhấc ra. Điều chỉnh đến khi cân nằm ngang.",C.weightsG.Select((v,i)=>new ActionItem(N(v)+" g","weight"+i,1,true,false)).ToArray());b.Extra=s=>Math.Abs(s.WeightSum()-C.massG)<.01f;
 c=T("Đọc khối lượng","Cộng khối lượng các quả cân trên đĩa phải.");c.Numbers.Add(new NumberItem("Khối lượng vật (g)",C.massG));break;
case "DoTheTich":
 a=T("Đọc mực nước đầu","Bấm Đặt mắt ngang mực nước, đọc thang chia trên ống đong rồi nhập số đo (mL).",new ActionItem("Đặt mắt ngang mực nước","eye"));a.Numbers.Add(new NumberItem("Mực nước đầu (mL)",C.beforeMl));
 b=T("Đặt đá vào ống đong","Bấm Thả đá chìm trong nước, quan sát mực nước dâng rồi nhập số đo mới (mL).",new ActionItem("Thả đá chìm trong nước","stone"));b.Numbers.Add(new NumberItem("Mực nước sau (mL)",C.afterMl));
 c=T("Tính thể tích đá","Lấy số đọc sau trừ số đọc đầu.");c.Numbers.Add(new NumberItem("Thể tích đá (cm³)",C.afterMl-C.beforeMl));break;
case "LucBienDang":
 a=Explore("Lò xo ban đầu","Chọn lực bằng 0, quan sát chiều dài ban đầu.","force",new[]{"Lực 0 N"},new[]{0f});
 b=Slide("Thử các mức lực","Kéo qua các mốc "+string.Join(", ",C.forceLevelsN.Select(N))+" N và ghi nhận tại mỗi mốc.","force","Lực kéo (N)",0,C.forceLevelsN.Max(),0,C.forceLevelsN);
 c=Quiz("Thả lực và kết luận","Đưa lực về 0 rồi chọn nhận xét.","Lực có thể làm vật biến dạng.","Lực không làm đổi hình dạng.");c.Slider=new SliderItem{Key="force",Label="Lực kéo (N)",Min=0,Max=C.forceLevelsN.Max(),Initial=C.forceLevelsN.Max(),Target=0,Whole=true};break;
case "HaiLucNguocChieu":
 a=Explore("Hai lực cân bằng","Giữ lực trái, đặt lực phải bằng lực trái.","right",new[]{"Lực phải "+N(C.rightLevelsN[0])+" N"},new[]{C.rightLevelsN[0]});
 b=Explore("Tạo chênh lệch","Tăng lực đẩy sang phải và quan sát xe.","right",new[]{"Lực phải "+N(C.rightLevelsN[1])+" N"},new[]{C.rightLevelsN[1]});
 c=Quiz("So sánh hai trường hợp","Xe ban đầu đứng yên; bỏ qua ma sát. Chọn kết luận đúng.","Lực bằng nhau: đứng yên; lực phải lớn hơn: tăng tốc sang phải.","Hai lực bằng nhau vẫn làm xe tự tăng tốc.","Xe luôn đi về phía lực nhỏ hơn.");break;
case "TrongLuc":
 a=Explore("Treo vật thứ nhất","Chọn vật rồi đọc lực kế.","sample",new[]{N(C.massesG[0])+" g"});a.Numbers.Add(new NumberItem("Số chỉ lực kế (N)",C.massesG[0]*C.gravityNPerKg/1000));
 b=Explore("Thay vật thứ hai","Thay vật tại cùng nơi rồi đọc lực kế.","sample",new[]{N(C.massesG[1])+" g"},new[]{1f});b.Numbers.Add(new NumberItem("Số chỉ lực kế (N)",C.massesG[1]*C.gravityNPerKg/1000));
 c=Match("Ghép khối lượng và trọng lượng","Ghép hai vật với số chỉ tương ứng.",C.massesG.Select(v=>N(v)+" g").ToArray(),C.massesG.Select(v=>N(v*C.gravityNPerKg/1000)+" N").ToArray());break;
case "MaSatBeMat":
 a=T("Giữ điều kiện so sánh","Dùng cùng một xe và cùng vận tốc đầu trên cả ba mặt đường.",new ActionItem("Cố định xe và vận tốc","setup"));
 b=Explore("Thả xe trên từng mặt","Chọn từng bề mặt; quan sát xe giảm tốc và quãng đường dừng.","sample",C.surfaces.Select(v=>"Thả xe: "+Name(v)).ToArray());
 var ranked=C.surfaces.Select((v,i)=>new{v,mu=C.frictionLevels[i]}).OrderBy(v=>v.mu).Select(v=>Name(v.v)).ToArray();c=Match("Xếp mức ma sát","Ghép bề mặt theo lực ma sát trong mô hình.",new[]{"Nhỏ nhất","Trung bình","Lớn nhất"},ranked);break;
case "LucCanChatLong":
 a=T("Thiết lập so sánh","Giữ tốc độ kéo, độ sâu và diện tích cản lớn nhất như nhau.",new ActionItem("Cố định điều kiện","setup"));
 b=Explore("Kéo từng hình qua nước","Quan sát số chỉ lực kế khi đổi hình.","sample",C.shapes.Select(Name).ToArray());
 int least=Array.IndexOf(C.dragN,C.dragN.Min());c=Quiz("Chọn vật ít cản","Hình nào cần lực kéo nhỏ nhất trong mô hình?",Name(C.shapes[least]),C.shapes.Where((v,i)=>i!=least).Select(Name).ToArray());break;
case "MaSatDoiSong":
 a=Explore("Quan sát ba tình huống","Mở đủ ba tình huống để xác định mục đích sử dụng.","sample",new[]{"Đế giày: cần bám đường","Phanh xe: cần hãm xe","Bản lề: cần quay nhẹ"});
 b=Match("Ghép giải pháp","Ghép hành động vào từng tình huống.",new[]{"Đế giày","Phanh xe","Bản lề"},new[]{"Tăng độ bám","Tăng lực hãm","Bôi trơn"});
 c=Match("Có ích hay có hại?","Đánh giá theo mục đích vừa nêu.",new[]{"Đế giày bám đường","Phanh hãm xe","Bản lề khó quay"},new[]{"Có ích","Có ích","Có hại"});break;
case "ChuyenHoaNangLuong":
 a=Match("Nối chuỗi thiết bị","Ghép thiết bị vào vị trí từ nguồn đến nơi sử dụng.",new[]{"1. Nguồn chuyển động","2. Tạo điện","3. Phát sáng"},new[]{"Tay quay","Máy phát","Đèn"});
 b=Slide("Quay máy phát","Thử tốc độ 1 và 2; ghi nhận độ sáng ở mỗi mức.","speed","Tốc độ quay",0,2,0,new[]{1f,2f});
 c=Match("Dừng và ghép năng lượng","Dừng tay quay, rồi ghép dạng năng lượng tương ứng.",new[]{"Tay quay","Dòng điện","Đèn"},new[]{"Cơ năng","Điện năng","Ánh sáng và nhiệt"});c.Slider=new SliderItem{Key="speed",Label="Tốc độ quay",Min=0,Max=2,Initial=2,Target=0,Whole=true};break;
case "TruyenNhiet":
 a=T("Chuẩn bị hai cốc","Cùng loại cốc, lượng nước và nhiệt độ ban đầu.",new ActionItem("Hai cốc "+N(C.initialC)+" °C","setup"));
 b=Slide("Bọc cách nhiệt và chờ","Bọc cốc B, đưa thời gian tới 100% rồi ghi nhận.","time","Thời gian mô phỏng (%)",0,100,0,new[]{100f});b.Actions.Add(new ActionItem("Bọc cách nhiệt cốc B","insulation"));
 c=Quiz("So sánh nhiệt độ","Cách nhiệt làm chậm truyền nhiệt. Cốc nào giữ ấm tốt hơn?",C.afterC[1]>C.afterC[0]?"Cốc B":"Cốc A",C.afterC[1]>C.afterC[0]?"Cốc A":"Cốc B","Hai cốc luôn bằng nhau");break;
case "NangLuongHaoPhi":
 a=T("Cấp cùng năng lượng","Cấp cho mỗi đèn cùng một lượng đầu vào.",new ActionItem("Cấp "+N(C.input)+" đơn vị","power"));
 b=Explore("Đọc hai sơ đồ","Mở sơ đồ đầu ra của cả hai đèn.","sample",new[]{"Xem đèn A","Xem đèn B"});
 c=Quiz("Chọn đèn ít hao phí","Đèn nào có phần ánh sáng có ích lớn hơn với cùng đầu vào?",C.useful[1]>C.useful[0]?"Đèn B":"Đèn A",C.useful[1]>C.useful[0]?"Đèn A":"Đèn B","Chỉ nhìn màu vỏ đèn");break;
case "NgayDem":
 a=T("Chọn điểm trên Trái Đất","Đánh dấu một điểm; Mặt Trời ở bên trái.",new ActionItem("Đánh dấu vị trí","marked"));
 b=Slide("Quay Trái Đất","Quay qua 0°, 90°, 180°, 270°, 360° và ghi nhận tại mỗi mốc.","angle","Góc tự quay (°)",0,360,0,C.rotationDegrees);
 c=Match("Ghép ngày và đêm","Ghép trạng thái của nơi được chiếu sáng và nơi khuất sáng.",new[]{"Vùng được chiếu sáng","Vùng khuất sáng"},new[]{"Ngày","Đêm"});break;
case "PhaMatTrang":
 a=T("Nhìn từ Trái Đất","Bật góc nhìn quan sát Mặt Trăng từ Trái Đất.",new ActionItem("Góc nhìn từ Trái Đất","view"));
 b=Slide("Đổi vị trí Mặt Trăng","Xem 0°, 90°, 180°, 270° và ghi nhận mỗi vị trí.","angle","Góc quỹ đạo (°)",0,360,0,new[]{0f,90f,180f,270f});
 c=Match("Ghép các pha","Ghép pha với phần sáng nhìn thấy.",new[]{"Gần như không thấy phần sáng","Thấy nửa đĩa sáng","Thấy cả đĩa sáng"},new[]{"Trăng mới","Bán nguyệt","Trăng tròn"});break;
case "HeMatTroi":
 a=Explore("Nhận diện hành tinh","Mở tên cả tám hành tinh.","sample",C.order.Select(Name).ToArray());
 b=Match("Sắp thứ tự từ Mặt Trời","Kéo thẻ vào thứ tự từ gần đến xa.",Enumerable.Range(1,8).Select(i=>"Vị trí "+i).ToArray(),C.order.Select(Name).ToArray());
 c=Quiz("Vị trí Trái Đất","Chọn vị trí đúng trong thứ tự quỹ đạo.","Thứ ba, giữa Kim và Hỏa","Thứ hai, giữa Thủy và Kim","Thứ tư, giữa Hỏa và Mộc");break;
case "TrangBiBaoHo":
 a=T("Đọc yêu cầu","Cần kính bảo hộ, áo choàng, giày kín và tóc gọn.",new ActionItem("Mở yêu cầu trang bị","setup"));
 b=T("Trang bị cho nhân vật","Bấm từng món để mặc hoặc tháo. Chọn đủ trang bị phù hợp.",new ActionItem("Kính bảo hộ","gear0",1,true,false),new ActionItem("Áo choàng","gear1",1,true,false),new ActionItem("Giày kín","gear2",1,true,false),new ActionItem("Tóc gọn","gear3",1,true,false),new ActionItem("Dép hở chân","badGear",1,true,false));b.Extra=s=>Enumerable.Range(0,4).All(i=>s.Get("gear"+i)>0)&&s.Get("badGear")==0;
 c=Quiz("Kiểm tra trước khi vào phòng","Khi chưa rõ yêu cầu, em cần làm gì?","Hỏi giáo viên và kiểm tra đủ trang bị","Tự thử hóa chất để tìm hiểu","Bỏ kính nếu thấy vướng");break;
case "NhanCanhBao":
 a=Explore("Đọc nhãn cảnh báo","Mở đủ ba nhãn để nhận biết nguy cơ.","sample",new[]{"Dễ cháy","Ăn mòn","Độc"});
 b=Match("Ghép hành vi","Chọn hành vi phù hợp với từng nguy cơ.",new[]{"Dễ cháy","Ăn mòn","Độc"},new[]{"Tránh nguồn lửa","Tránh tiếp xúc trực tiếp","Không nếm/ngửi trực tiếp"});
 c=Quiz("Đọc nhãn để làm gì?","Chọn cách xử lý khi chưa hiểu nhãn.","Đọc nhãn và hỏi giáo viên khi chưa rõ","Đoán bằng màu dung dịch","Ngửi trực tiếp để nhận biết");break;
case "TimLoiAnToan":
 a=T("Quan sát bàn","Quan sát đồ ăn, cốc không nhãn và chai chưa đậy.",new ActionItem("Quan sát toàn bàn","setup"));
 b=T("Đánh dấu lỗi","Chọn vật cần xử lý; bấm lại để bỏ đánh dấu.",new ActionItem("Đồ ăn trên bàn","error0",1,true,false),new ActionItem("Cốc không nhãn","error1",1,true,false),new ActionItem("Chai không đậy","error2",1,true,false),new ActionItem("Kính bảo hộ sạch","error3",1,true,false));b.Extra=s=>s.Get("error0")==1&&s.Get("error1")==1&&s.Get("error2")==1&&s.Get("error3")==0;
 c=Match("Chọn cách xử lý","Ghép hành động phù hợp, không tự dùng chất chưa rõ.",new[]{"Đồ ăn","Cốc không nhãn","Chai không đậy"},new[]{"Đưa ra khu vực phù hợp","Báo giáo viên","Báo giáo viên"});break;
case "BaTheCuaChat":
 a=Explore("Xem ba thể","Quan sát cách sắp xếp và chuyển động của hạt.","sample",new[]{"Rắn","Lỏng","Khí"});
 b=T("Đổi bình chứa","Thử chất lỏng và chất khí trong bình hẹp hơn.",new ActionItem("Lỏng trong bình hẹp","containerLiquid"),new ActionItem("Khí trong bình hẹp","containerGas"));
 c=Match("Ghép tính chất","Ghép hình dạng và thể tích của từng thể.",new[]{"Rắn","Lỏng","Khí"},new[]{"Hình dạng, thể tích xác định","Thể tích xác định, hình theo bình","Chiếm không gian bình"});break;
case "NongChay":
 a=T("Đọc nhiệt độ đầu","Bấm nhiệt kế để quan sát, nhập số đọc vào ô bên phải rồi kiểm tra.",new ActionItem("Quan sát nhiệt kế","setup"));a.Numbers.Add(new NumberItem("Nhiệt độ đầu (°C)",C.startC));
 b=Slide("Cấp nhiệt ảo","Ghi nhận ở 0%, 50%, 100%: trước, trong và sau nóng chảy.","time","Tiến trình cấp nhiệt (%)",0,100,0,new[]{0f,50f,100f});
 c=Match("Đọc đường nhiệt độ","Ghép đoạn đồ thị với hiện tượng.",new[]{"Đoạn ngang ở 0 °C","Đoạn tăng sau khi tan hết"},new[]{"Đá đang nóng chảy","Nước lỏng ấm lên"});break;
case "BayHoiVaSoi":
 a=Explore("Xem bay hơi","Quan sát mặt nước ở nhiệt độ dưới nhiệt độ sôi.","sample",new[]{"Xem "+N(C.temperaturesC[0])+" °C"});
 b=Explore("Xem nước sôi","Quan sát bọt hơi trong khối nước ở 1 atm.","sample",new[]{"Xem 100 °C"},new[]{1f});
 c=Match("Phân biệt hiện tượng","Ghép hiện tượng với nơi hình thành hơi.",new[]{"Bay hơi","Sôi"},new[]{"Ở mặt thoáng","Trong khối nước và mặt thoáng"});break;
case "ThanhPhanKhongKhi":
 a=T("Bảng 100 ô","Mỗi ô biểu diễn khoảng 1% thể tích không khí khô.",new ActionItem("Mở chú giải 100 ô","setup"));
 b=T("Phân bố các khí","Nhập số ô cho ba nhóm; tổng phải bằng 100.");b.Numbers.Add(new NumberItem("Nitrogen (%)",C.nitrogenPercent));b.Numbers.Add(new NumberItem("Oxygen (%)",C.oxygenPercent));b.Numbers.Add(new NumberItem("Khí khác (%)",C.otherPercent));
 c=Quiz("Thành phần lớn nhất","Chọn nhận xét đúng về không khí khô.","Nitrogen nhiều nhất; oxygen khoảng một phần năm","Oxygen chiếm gần toàn bộ","Khí khác chiếm một nửa");break;
case "OxygenSuChay":
 a=T("Giữ cùng nhiên liệu","Các buồng chỉ khác mức oxygen; giữ các điều kiện khác giống nhau.",new ActionItem("Chọn cùng mẫu nhiên liệu","setup"));
 b=Explore("Quan sát ba buồng","Chạy từng buồng trong mô hình ảo.","sample",new[]{"Không có oxygen","Không khí","Giàu oxygen"});
 c=Match("Phân biệt vai trò","Ghép nhiên liệu và oxygen với vai trò.",new[]{"Nhiên liệu","Oxygen"},new[]{"Chất cháy","Hỗ trợ sự cháy"});break;
case "BaoVeKhongKhi":
 a=Explore("Tìm nguồn ô nhiễm","Đánh dấu ba nguồn trong mô hình.","sample",new[]{"Xe nổ máy lâu","Đốt rác","Bụi xây dựng"});
 b=Match("Giảm phát thải tại nguồn","Ghép hành động với từng nguồn.",new[]{"Xe nổ máy lâu","Đốt rác","Bụi xây dựng"},new[]{"Giảm nổ máy không cần thiết","Không đốt rác","Kiểm soát bụi"});
 c=Quiz("Đánh giá giải pháp","Chọn nhận xét về việc bảo vệ không khí.","Cần phối hợp nhiều biện pháp","Một cây xanh xử lý mọi ô nhiễm","Chỉ cần chuyển khói sang nơi khác");break;
case "ChonVatLieu":
 a=Explore("Đọc yêu cầu bộ phận","Mở yêu cầu sử dụng của ba bộ phận.","sample",new[]{"Thân nồi: truyền nhiệt","Tay cầm: hạn chế truyền nhiệt","Cửa sổ: truyền ánh sáng"});
 b=Match("Ghép vật liệu","Chọn vật liệu theo mục đích sử dụng.",new[]{"Thân nồi","Tay cầm","Cửa sổ"},new[]{"Kim loại","Vật liệu cách nhiệt phù hợp","Kính"});
 c=Match("Giải thích lựa chọn","Ghép tính chất cần thiết.",new[]{"Thân nồi","Tay cầm","Cửa sổ"},new[]{"Dẫn nhiệt tốt","Hạn chế truyền nhiệt","Trong suốt"});break;
case "NhienLieu":
 a=Explore("Xem các thẻ vật chất","Mở đủ năm thẻ trước khi phân loại.","sample",new[]{"Củi","Than","Khí thiên nhiên","Cát","Kính"});
 b=Match("Phân loại nhiên liệu","Ghép mỗi vật với nhóm trong bài.",new[]{"Củi","Than","Khí thiên nhiên","Cát","Kính"},new[]{"Nhiên liệu","Nhiên liệu","Nhiên liệu","Không phải nhiên liệu","Không phải nhiên liệu"});
 c=Match("Dùng tiết kiệm","Chọn hành động phù hợp trong hai tình huống.",new[]{"Khi không sử dụng","Khi đang sử dụng"},new[]{"Tắt thiết bị","Giảm thất thoát năng lượng"});break;
case "BaoQuanThucPham":
 a=Explore("Đọc nhãn mẫu","Mở ba nhãn về hạn dùng và bảo quản.","sample",new[]{"Gói A: còn hạn, nơi khô","Gói B: còn hạn, giữ mát","Gói C: đã quá hạn"});
 b=Match("Chọn nơi bảo quản","Ghép theo nhãn, không đoán bằng vẻ ngoài.",new[]{"Gói A","Gói B","Gói C"},new[]{"Kệ khô","Tủ mát","Tách riêng, báo người lớn"});
 c=Quiz("Gói quá hạn hoặc hỏng","Em cần xử lý thế nào?","Tách riêng và báo người lớn","Nếm thử để kiểm tra","Đổi nhãn rồi tiếp tục dùng");break;
case "LocHonHop":
 a=T("Lắp bộ lọc","Đặt giấy lọc vào phễu và cốc nhận bên dưới.",new ActionItem("Đặt giấy lọc","filter"),new ActionItem("Đặt cốc nhận","cup"));
 b=T("Rót hỗn hợp ảo","Cho hỗn hợp qua bộ lọc, quan sát hai phần.",new ActionItem("Rót cát và nước","pour"));
 c=Match("Xác định hai phần","Ghép vật chất với vị trí sau lọc.",new[]{"Trên giấy lọc","Trong cốc nhận"},new[]{"Cát","Nước"});break;
case "BayHoiThuMuoi":
 a=T("Thử giấy lọc ảo","Muối đã tan không được giữ lại trên giấy lọc thông thường.",new ActionItem("Cho dung dịch qua giấy lọc","filter"));
 b=Slide("Cho nước bay hơi","Ghi nhận lúc bắt đầu và khi nước đã bay hơi hết trong mô hình.","time","Tiến trình bay hơi (%)",0,100,0,new[]{0f,100f});
 c=Match("Thu lại muối","Ghép phần còn lại và phần rời khay.",new[]{"Còn trong khay","Rời khay vào môi trường"},new[]{"Muối","Nước ở dạng hơi"});break;
case "RayHonHop":
 a=T("So kích thước hạt","Hạt bột nhỏ hơn hạt gạo. Quan sát ba cỡ lỗ rây.",new ActionItem("Phóng đại hạt và lỗ rây","setup"));
 b=Explore("Chọn và thử rây","Thử đủ ba cỡ, cuối cùng giữ rây tách được bột và gạo.","mesh",C.sieveSizes.Select(v=>"Cỡ lỗ "+N(v)).ToArray(),C.sieveSizes);b.Extra=s=>s.Get("mesh")>C.particleSizes[0]&&s.Get("mesh")<C.particleSizes[1];
 c=Match("Kiểm tra hai phần","Ghép hai phần thu được.",new[]{"Ở trên rây","Ở khay dưới"},new[]{"Gạo","Bột"});break;
default:throw new ArgumentException("Unknown experiment "+Type);
}return new[]{a,b,c};}

public string Observation(){int i=(int)Get("sample");switch(Type){
case "CanKhoiLuong":return "Vật mẫu: "+(Get("object")>0?"đã đặt":"chưa đặt")+". Tổng quả cân: "+N(WeightSum())+" g. "+(Math.Abs(WeightSum()-C.massG)<.01f?"Cân thăng bằng.":"Hai đĩa chưa cân bằng.");
case "DoTheTich":return "Mực nước: "+N(Get("stone")>0?C.afterMl:C.beforeMl)+" mL. 1 mL = 1 cm³.";
case "LucBienDang":return "Lực kéo: "+N(Get("force"))+" N. Độ giãn tăng theo lực trong giới hạn đàn hồi của mô hình.";
case "HaiLucNguocChieu":return "Lực trái: "+N(C.leftN)+" N; lực phải: "+N(Get("right",C.leftN))+" N. Hợp lực: "+N(Get("right",C.leftN)-C.leftN)+" N sang phải.";
case "TrongLuc":return "Vật "+N(C.massesG[Math.Min(i,1)])+" g → lực kế "+N(C.massesG[Math.Min(i,1)]/1000*C.gravityNPerKg)+" N.";
case "MaSatBeMat":return "Mặt "+Name(C.surfaces[Math.Min(i,2)])+" • Cùng vận tốc đầu "+N(C.initialSpeed)+" m/s. Quãng đường dừng minh họa: "+N(C.initialSpeed*C.initialSpeed/(2*C.frictionLevels[Math.Min(i,2)]*10))+" m.";
case "LucCanChatLong":return Name(C.shapes[Math.Min(i,2)])+": lực kéo "+N(C.dragN[Math.Min(i,2)])+" N ở cùng tốc độ "+N(C.speed)+" m/s.";
case "ChuyenHoaNangLuong":return Get("speed")>0?"Cơ năng → điện năng → ánh sáng và nhiệt.":"Dừng quay: đèn tắt vì mô hình không tích trữ năng lượng.";
case "TruyenNhiet":return "Cốc A: "+N(C.initialC+(C.afterC[0]-C.initialC)*Get("time")/100)+" °C; cốc B: "+N(C.initialC+((Get("insulation")>0?C.afterC[1]:C.afterC[0])-C.initialC)*Get("time")/100)+" °C. Số liệu minh họa, cùng thời gian.";
case "NangLuongHaoPhi":return "Cùng đầu vào "+N(C.input)+": A có "+N(C.useful[0])+" có ích / "+N(C.waste[0])+" hao phí; B có "+N(C.useful[1])+" / "+N(C.waste[1])+".";
case "NgayDem":return "Góc tự quay "+N(Get("angle"))+"°. Điểm đánh dấu ở vùng "+(Math.Cos(Get("angle")*Math.PI/180)<0?"sáng: ngày.":"tối: đêm.")+" Nhìn từ phía cực; không đúng tỉ lệ.";
case "PhaMatTrang":return "Vị trí "+N(Get("angle"))+"° • Phần sáng nhìn từ Trái Đất thay đổi; không phải bóng Trái Đất che Mặt Trăng.";
case "NongChay":return "Nhiệt độ: "+N(Temperature())+" °C • "+(Get("time")<30?"Đá đang ấm lên.":Get("time")<=70?"Đá và nước cùng tồn tại.":"Đá tan hết, nước lỏng ấm lên.")+" (Nước tinh khiết, 1 atm).";
case "BayHoiVaSoi":return Get("sample")==0?"Bay hơi ở mặt thoáng tại "+N(C.temperaturesC[0])+" °C.":"100 °C, 1 atm: bọt hơi hình thành trong khối nước và thoát ra.";
case "ThanhPhanKhongKhi":return "Không khí khô: nitrogen "+N(C.nitrogenPercent)+"%, oxygen "+N(C.oxygenPercent)+"%, khí khác "+N(C.otherPercent)+"%.";
case "OxygenSuChay":return new[]{"Không có oxygen: không duy trì cháy.","Trong không khí: nhiên liệu cháy khi đủ điều kiện.","Giàu oxygen: cháy mạnh hơn trong mô hình định tính."}[Math.Min(i,2)];
case "BaTheCuaChat":return new[]{"Rắn: hình dạng và thể tích xác định; hạt dao động quanh vị trí.","Lỏng: thể tích xác định; hình dạng theo phần bình chứa.","Khí: lan ra chiếm không gian bình; hạt cách xa và chuyển động."}[Math.Min(i,2)];
case "LocHonHop":return Get("pour")>0?"Cát giữ trên giấy lọc; nước đi qua vào cốc. Nước lọc chưa chắc uống được.":"Lắp giấy lọc và cốc nhận trước khi rót hỗn hợp ảo.";
case "BayHoiThuMuoi":return "Nước còn "+N(100-Get("time"))+"% lượng ban đầu. Muối không bay hơi theo nước ở điều kiện mô phỏng.";
case "RayHonHop":float mesh=Get("mesh",C.sieveSizes[0]);return "Cỡ lỗ "+N(mesh)+"; hạt nhỏ "+N(C.particleSizes[0])+", hạt lớn "+N(C.particleSizes[1])+" (đơn vị mô hình). "+(mesh<=C.particleSizes[0]?"Cả hai ở trên.":mesh>=C.particleSizes[1]?"Cả hai đi qua.":"Bột qua rây, gạo ở trên.");
default:return LastAction.Length>0?LastAction:"Quan sát mô hình, thực hiện thao tác bên phải.";
}}
public static string Name(string key){switch(key){case "kinh":return "Kính";case "go":return "Gỗ";case "tham":return "Thảm";case "mat_tu":return "Mặt tù";case "cau":return "Hình cầu";case "thuon":return "Hình thuôn";case "Thuy":return "Thủy";case "Kim":return "Kim";case "TraiDat":return "Trái Đất";case "Hoa":return "Hỏa";case "Moc":return "Mộc";case "Tho":return "Thổ";case "ThienVuong":return "Thiên Vương";case "HaiVuong":return "Hải Vương";default:return key;}}
}
