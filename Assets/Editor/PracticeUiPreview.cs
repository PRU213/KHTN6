#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Preview uses the real runtime UI and read-only API. It does not change the open scene or login data.
[InitializeOnLoad]
public static class PracticeUiPreview
{
    const string Pending = "KHTN6.PracticeUiPreview.Pending";
    static PracticeUiPreview() { EditorApplication.playModeStateChanged += Changed; }
    [MenuItem("Tools/Practice UI/Preview Physics Chapter 1 %&F8")]
    public static void Preview()
    {
        if (EditorApplication.isPlaying) { Show(); return; }
        SessionState.SetBool(Pending,true);
        EditorApplication.isPlaying = true;
    }
    static void Changed(PlayModeStateChange state)
    {
        if(state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending,false)) return;
        SessionState.SetBool(Pending,false);
        EditorApplication.delayCall += Show;
    }
    static void Show()
    {
        PracticeLearningUI.Open("Vật lý","CH_PHY_01",()=>Debug.Log("Practice preview: theory selected."));
        var gameView=typeof(Editor).Assembly.GetType("UnityEditor.GameView");
        if(gameView!=null) EditorWindow.GetWindow(gameView).Focus();
    }
    [MenuItem("Tools/Practice UI/Capture Game View %&F9")]
    static void Capture()
    {
        if(!EditorApplication.isPlaying) return;
        const string folder="outputs/practice-ui-review";
        System.IO.Directory.CreateDirectory(folder);
        string path=folder+"/practice-"+System.DateTime.Now.ToString("yyyyMMdd-HHmmss")+".png";
        ScreenCapture.CaptureScreenshot(path);
        Debug.Log("Practice screenshot: "+path);
    }
}
#endif
