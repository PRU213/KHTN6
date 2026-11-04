#if UNITY_EDITOR
using System;
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

/// <summary>Clicks the completion button and exercises the real practice HTTP client against a local fixture.</summary>
[InitializeOnLoad]
public static class EssayTutorNavigationSmokeTests
{
    const string Key="EssayTutorNavigationSmokeTests.Running";
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static EssayTutorUI ui;
    static FootballEssayController practice;
    static TcpListener listener;
    static string endpoint, serverError;
    static int stage, frames, requests;
    static double deadline;
    static EssayTutorNavigationSmokeTests()
    {
        EditorApplication.playModeStateChanged+=change=>{if(change==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)) Start();};
        if(SessionState.GetBool(Key,false)&&EditorApplication.isPlaying) EditorApplication.delayCall+=Start;
    }
    public static void RunBatch()
    {
        SessionState.SetBool(Key,true);
        if(EditorApplication.isPlaying){Start();return;}
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        EditorApplication.isPlaying=true;
    }
    static void Start()
    {
        if(listener!=null)return;
        listener=new TcpListener(IPAddress.Loopback,0); listener.Start();
        endpoint="http://127.0.0.1:"+((IPEndPoint)listener.LocalEndpoint).Port;
        Task.Run(()=>Serve());
        stage=0;frames=0;deadline=EditorApplication.timeSinceStartup+30;
        EditorApplication.update-=Tick;EditorApplication.update+=Tick;
    }
    static async Task Serve()
    {
        try{
            for(int i=0;i<2;i++)using(var client=await listener.AcceptTcpClientAsync())using(var stream=client.GetStream()){
                string headers="";var one=new byte[1];
                while(!headers.EndsWith("\r\n\r\n")){if(await stream.ReadAsync(one,0,1)!=1)throw new Exception("Incomplete HTTP headers");headers+=(char)one[0];}
                if(!headers.StartsWith("POST /football/next "))throw new Exception("Unexpected route: "+headers.Split('\r')[0]);
                int length=0;foreach(var line in headers.Split('\n'))if(line.StartsWith("Content-Length:",StringComparison.OrdinalIgnoreCase))length=int.Parse(line.Substring(15).Trim());
                var body=new byte[length];for(int read=0;read<length;){int n=await stream.ReadAsync(body,read,length-read);if(n==0)throw new Exception("Incomplete HTTP body");read+=n;}
                string payload=Encoding.UTF8.GetString(body);
                if(!payload.Contains("nav-token"))throw new Exception("Wrong practice ticket");
                bool last=payload.Contains("\"round\":5");
                string json=last?"{\"token\":\"nav-token\",\"state\":\"finished\",\"round\":5,\"score\":20,\"rawScore\":4,\"tutorCompleted\":true,\"tutorId\":\"lesson-5\",\"text\":\"FINAL QUESTION\"}"
                    :"{\"token\":\"nav-token\",\"state\":\"answering\",\"round\":2,\"total\":5,\"text\":\"NEXT QUESTION\",\"remaining\":120}";
                var data=Encoding.UTF8.GetBytes(json);
                var head=Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: "+data.Length+"\r\nConnection: close\r\n\r\n");
                await stream.WriteAsync(head,0,head.Length);await stream.WriteAsync(data,0,data.Length);Interlocked.Increment(ref requests);
            }
        }catch(Exception error){serverError=error.Message;}
    }
    static object Field(object target,string name)=>target.GetType().GetField(name,Flags).GetValue(target);
    static void Set(object target,string name,object value)=>target.GetType().GetField(name,Flags).SetValue(target,value);
    static void Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name,Flags).Invoke(target,args);
    static void Setup(int round)
    {
        if(ui==null)ui=(EssayTutorUI)typeof(EssayTutorUI).GetMethod("Get",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
        ui.gameObject.SetActive(true);
        if(EventSystem.current==null)new GameObject("Fixture events",typeof(EventSystem),typeof(StandaloneInputModule));
        practice=new GameObject("Practice navigation fixture").AddComponent<FootballEssayController>();
        Set(practice,"fallbackFont",ui.GetComponentInChildren<TMP_Text>(true).font);Set(practice,"url",endpoint);
        Call(practice,"Build");Set(practice,"token","nav-token");Set(practice,"historyQueued",true);Set(practice,"autoTutorKey","nav-token:"+round);
        var stateType=typeof(FootballEssayController).GetNestedType("State",BindingFlags.NonPublic);
        var state=JsonUtility.FromJson("{\"token\":\"nav-token\",\"state\":\"review\",\"round\":"+round+",\"rawScore\":4,\"needsTutor\":true,\"tutorSupported\":true,\"tutorId\":\"lesson-"+round+"\",\"text\":\"Đề học lại\"}",stateType);
        Call(practice,"Apply",state);
        var record=new EssayTutorUI.Record{id="lesson-"+round,subject="Physics",status="learning",question="Đề",total=3};
        Call(ui,"Render",record);
        if(((Button)Field(ui,"continueButton")).gameObject.activeSelf)throw new Exception("Continue shown before mastery");
        record.status="completed";record.step=3;record.solution="Lời giải tổng kết";
        Call(ui,"Render",record);
        var button=(Button)Field(ui,"continueButton");
        if(!button.gameObject.activeSelf||!button.interactable)throw new Exception("Completed lesson has no usable continuation");
        string expected=round==5?"Xem tổng kết":"Câu tiếp theo";
        if(button.GetComponentInChildren<TMP_Text>().text!=expected)throw new Exception("Wrong continuation label");
        if(practice.ContinueTutorial("another-lesson"))throw new Exception("Continued an unrelated lesson");
        frames=0;deadline=EditorApplication.timeSinceStartup+20;
    }
    static void ClickContinue()
    {
        var button=(Button)Field(ui,"continueButton");Canvas.ForceUpdateCanvases();
        var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,button.transform.position)};
        var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
        if(hits.Count==0||!hits[0].gameObject.transform.IsChildOf(button.transform))throw new Exception("Continuation button is covered");
        ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);
        if(ui.gameObject.activeSelf)throw new Exception("Tutor overlay did not close after continuing");
    }
    static void Tick()
    {
        if(!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
        try{
            if(serverError!=null)throw new Exception(serverError);
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Timed out waiting for next question");
            if(stage==0){Setup(1);stage=1;return;}
            if(stage==1){if(++frames<3)return;ClickContinue();stage=2;return;}
            if(stage==2){
                var state=Field(practice,"latest");if((int)state.GetType().GetField("round").GetValue(state)!=2)return;
                if(!((TMP_Text)Field(practice,"question")).text.Contains("NEXT QUESTION"))throw new Exception("Next question is not displayed");
                Set(practice,"token",null);UnityEngine.Object.Destroy(practice.gameObject);Setup(5);stage=3;return;
            }
            if(stage==3){if(++frames<3)return;ClickContinue();stage=4;return;}
            if(stage==4){
                var state=Field(practice,"latest");if((string)state.GetType().GetField("state").GetValue(state)!="finished")return;
                if(!((TMP_Text)Field(practice,"resultText")).text.Contains("HOÀN THÀNH"))throw new Exception("Final summary not displayed");
                ui.gameObject.SetActive(true);Call(ui,"Render",new EssayTutorUI.Record{id="older-lesson",subject="Physics",status="completed",total=3,step=3,solution="Tổng kết"});
                if(((Button)Field(ui,"continueButton")).GetComponentInChildren<TMP_Text>().text!="Về nhật ký")throw new Exception("History offers next for an unrelated practice");
                Set(ui,"teacher",true);Call(ui,"SetControls");if(((Button)Field(ui,"continueButton")).gameObject.activeSelf)throw new Exception("Teacher can advance student practice");
                if(requests!=2)throw new Exception("Continuation sent duplicate or missing requests");
                var output=Path.Combine(Directory.GetCurrentDirectory(),"outputs");Directory.CreateDirectory(output);
                File.WriteAllText(Path.Combine(output,"essay-tutor-navigation-smoke.txt"),"PASS: completion button click advances real practice HTTP client to displayed question 2 despite stale cached mastery; last question shows final summary; hidden before completion and for teacher; old history does not advance unrelated practice.\n");
                Finish(0);
            }
        }catch(Exception error){Debug.LogException(error);Finish(1);}
    }
    static void Finish(int code){EditorApplication.update-=Tick;SessionState.SetBool(Key,false);if(practice!=null)Set(practice,"token",null);listener?.Stop();listener=null;EditorApplication.Exit(code);}
}
#endif
