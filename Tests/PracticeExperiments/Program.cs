using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

class Program
{
    static int checks;
    static void Check(bool condition,string message){checks++;if(!condition)throw new Exception(message);}
    static JsonSerializerOptions Options=new JsonSerializerOptions{IncludeFields=true};
    static Dictionary<string,PracticeExperimentConfig> configs=new Dictionary<string,PracticeExperimentConfig>();
    static PracticeExperiment Make(string type)=>new PracticeExperiment(type,configs[type]);
    static void CompleteStep(PracticeExperiment e)
    {
        var t=e.Current;
        for(int i=0;i<t.Actions.Count;i++)if(t.Actions[i].Key!="badGear"&&t.Actions[i].Key!="error3")e.Act(i);
        if(e.Type=="RayHonHop"&&e.Stage==1){int i=Array.FindIndex(e.C.sieveSizes,x=>x>e.C.particleSizes[0]&&x<e.C.particleSizes[1]);e.Act(i);}
        if(t.Slider!=null){foreach(float n in t.Slider.Samples??Array.Empty<float>()){e.SetSlider(n);e.RecordSample();}if(t.Slider.Target.HasValue)e.SetSlider(t.Slider.Target.Value);}
        for(int i=0;i<t.Numbers.Count;i++)e.SetNumber(i,PracticeExperiment.N(t.Numbers[i].Expected));
        for(int i=0;i<t.Matches.Count;i++)e.Place(i,t.Matches[i].Answer);
        if(t.CorrectChoice>=0)e.Choose(t.CorrectChoice);
    }
    static void Main()
    {
        using(var data=JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"fixtures.json")))) {
            foreach(var item in data.RootElement.GetProperty("items").EnumerateArray()){
                var lesson=item.GetProperty("lesson");string type=lesson.GetProperty("simulationType").GetString();
                if(type=="DoChieuDai")continue;
                configs[type]=JsonSerializer.Deserialize<PracticeExperimentConfig>(lesson.GetProperty("configJson").GetString(),Options);
                Check(item.GetProperty("steps").GetArrayLength()==3,type+" must have 3 authored steps");
            }
        }
        Check(configs.Count==29,"All 29 additional lessons present");
        Check(PracticeExperiment.SupportedTypes.OrderBy(x=>x).SequenceEqual(configs.Keys.OrderBy(x=>x)),"No missing or extra simulation type");
        foreach(var entry in configs){
            string error;Check(PracticeExperiment.Validate(entry.Key,entry.Value,out error),entry.Key+": "+error);
            var e=Make(entry.Key);Check(e.Tasks.Length==3,"Three tasks "+entry.Key);
            for(int stage=0;stage<3;stage++){
                Check(!e.Ready(out error),entry.Key+" step "+stage+" cannot complete before interaction");
                Check(!e.Advance(out error),entry.Key+" cannot skip unfinished step");
                CompleteStep(e);
                Check(e.Ready(out error),entry.Key+" complete task "+stage+": "+error);
                if(e.Current.Numbers.Count>0){e.SetNumber(0,"NaN");Check(!e.Ready(out error),"NaN rejected");e.SetNumber(0,"Infinity");Check(!e.Ready(out error),"Infinity rejected");e.SetNumber(0,PracticeExperiment.N(e.Current.Numbers[0].Expected+1));Check(!e.Ready(out error),"Wrong numeric answer rejected");e.SetNumber(0,PracticeExperiment.N(e.Current.Numbers[0].Expected));}
                if(e.Current.Matches.Select(m=>m.Answer).Distinct().Count()>1){e.Place(0,e.Current.Matches.First(m=>m.Answer!=e.Current.Matches[0].Answer).Answer);Check(!e.Ready(out error),"Incorrect matching rejected");e.Place(0,e.Current.Matches[0].Answer);}
                if(e.Current.CorrectChoice>=0){e.Choose(0);Check(!e.Ready(out error),"Incorrect conclusion rejected");e.Choose(e.Current.CorrectChoice);}
                Check(e.Advance(out error),entry.Key+" can advance after correct work");
            }
            Check(e.Completed&&e.Journal.Count==3,entry.Key+" completed with observations");Check(!e.Advance(out error),"Cannot append duplicate result");
            var fresh=Make(entry.Key);Check(!fresh.Completed&&fresh.Stage==0&&fresh.Journal.Count==0,"Replay resets progress");
        }
        var balance=Make("CanKhoiLuong");balance.Act(0);balance.Advance(out _);balance.Act(0);Check(!balance.Ready(out _),"One weight cannot balance 150 g");balance.Act(1);Check(balance.WeightSum()==150&&balance.Ready(out _),"100 + 50 g balances 150 g");balance.Act(1);Check(!balance.Ready(out _),"Removing a weight invalidates balance");
        var volume=Make("DoTheTich");Check(volume.Tasks[0].Numbers[0].Expected==40&&volume.Tasks[1].Numbers[0].Expected==70&&volume.Tasks[2].Numbers[0].Expected==30,"Displacement is after minus before");
        var gravity=Make("TrongLuc");Check(gravity.Tasks[0].Numbers[0].Expected==1&&gravity.Tasks[1].Numbers[0].Expected==2,"g to kg conversion for force");
        var melting=Make("NongChay");CompleteStep(melting);melting.Advance(out _);melting.SetSlider(0);Check(melting.Temperature()==-5,"Ice starts at -5");melting.SetSlider(30);Check(melting.Temperature()==0,"Melting begins at 0");melting.SetSlider(60);Check(melting.Temperature()==0,"Temperature plateau during melting");melting.SetSlider(100);Check(melting.Temperature()==10,"Liquid warms after melting");
        var spring=Make("LucBienDang");CompleteStep(spring);spring.Advance(out _);spring.SetSlider(999);Check(spring.Get("force")==2,"Slider clamps to config range");spring.RecordSample();Check(!spring.Ready(out _),"Cannot skip intermediate observations");spring.SetSlider(float.NaN);Check(spring.Get("force")==2,"NaN slider update ignored");
        var ppe=Make("TrangBiBaoHo");CompleteStep(ppe);ppe.Advance(out _);CompleteStep(ppe);ppe.Act(4);Check(!ppe.Ready(out _),"Unsafe footwear cannot pass even with required equipment");
        string problem;Check(!PracticeExperiment.Validate("Unknown",new PracticeExperimentConfig(),out problem),"Unknown type rejected");
        var bad=JsonSerializer.Deserialize<PracticeExperimentConfig>(JsonSerializer.Serialize(configs["NangLuongHaoPhi"],Options),Options);bad.waste[0]=1;Check(!PracticeExperiment.Validate("NangLuongHaoPhi",bad,out problem),"Energy totals must conserve input");
        Console.WriteLine("PASS: "+checks+" checks across all 29 additional experiments.");
    }
}
