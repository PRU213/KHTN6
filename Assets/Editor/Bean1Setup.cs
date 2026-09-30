using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;

/// <summary>
/// Tool Setup bản cũ an toàn nhất (Không sửa Canvas, dựa theo kích thước cũ).
/// </summary>
public class Bean1Setup : EditorWindow
{
    [MenuItem("Tools/1. Setup Tất Cả (Bấm vào đây)")]
    public static void RunSetup()
    {
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

        AddSceneToBuildSettings("Assets/Scenes/Biology/bean_1.unity");
        AddSceneToBuildSettings("Assets/Scenes/Biology/bean_1_question.unity");

        var scene1 = EditorSceneManager.OpenScene("Assets/Scenes/Biology/bean_1.unity", OpenSceneMode.Single);
        string res1 = "Lỗi mở bean_1";
        if (scene1.IsValid())
        {
            res1 = SetupBean1();
            EditorSceneManager.SaveScene(scene1);
        }

        var scene2 = EditorSceneManager.OpenScene("Assets/Scenes/Biology/bean_1_question.unity", OpenSceneMode.Single);
        string res2 = "Lỗi mở bean_1_question";
        if (scene2.IsValid())
        {
            res2 = SetupBean1Question();
            EditorSceneManager.SaveScene(scene2);
        }

        EditorSceneManager.OpenScene("Assets/Scenes/Biology/bean_1.unity", OpenSceneMode.Single);

        EditorUtility.DisplayDialog("Thành công!", 
            "Đã setup xong xuôi cả 2 màn!\n\n" + 
            "Chi tiết:\n" + res1 + "\n" + res2 + "\n\n" + 
            "Giờ bạn chỉ cần bấm PLAY để chơi!", "OK");
    }

    private static void AddSceneToBuildSettings(string path)
    {
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (var s in scenes)
        {
            if (s.path == path) { s.enabled = true; return; }
        }
        scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static string SetupBean1()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) return "- Không tìm thấy Canvas trong màn chính!";

        int plat = 0, lad = 0, ene = 0;
        GameObject playerObj = null;

        Image[] allImages = canvas.GetComponentsInChildren<Image>(true);
        foreach (Image img in allImages)
        {
            RectTransform rt = img.rectTransform;
            float w = rt.sizeDelta.x;
            float h = rt.sizeDelta.y;

            if (rt.anchorMin == Vector2.zero && rt.anchorMax == Vector2.one) continue;

            if (img.GetComponent<Animator>() != null)
            {
                if (!img.GetComponent<PlayerController>()) img.gameObject.AddComponent<PlayerController>();
                img.gameObject.name = "Player";
                playerObj = img.gameObject;
                continue;
            }

            if (w > 45 && w < 65 && h > 90 && h < 160)
            {
                if (!img.GetComponent<LadderZone>()) img.gameObject.AddComponent<LadderZone>();
                img.gameObject.name = "Ladder_" + lad++;
                continue;
            }

            if (w > 75 && w < 450 && h > 55 && h < 150)
            {
                if (!img.GetComponent<Platform>()) img.gameObject.AddComponent<Platform>();
                img.gameObject.name = "Platform_" + plat++;
                continue;
            }

            if (w > 35 && w < 75 && h > 35 && h < 100)
            {
                if (img.GetComponent<Platform>() || img.GetComponent<LadderZone>()) continue;
                if (!img.GetComponent<Enemy>()) img.gameObject.AddComponent<Enemy>();
                img.gameObject.name = "Enemy_" + ene++;
                continue;
            }
        }

        if (Object.FindAnyObjectByType<GameData>() == null)
        {
            new GameObject("GameData").AddComponent<GameData>();
        }

        if (playerObj) SetupClimb(playerObj);

        return $"- Màn chính: Đã gắn ({plat}) Đất, ({lad}) Thang, ({ene}) Quái vật.";
    }

    private static string SetupBean1Question()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) return "- Không tìm thấy Canvas màn câu hỏi!";

        if (canvas.GetComponent<QuestionManager>() == null)
            canvas.gameObject.AddComponent<QuestionManager>();

        if (Object.FindAnyObjectByType<GameData>() == null)
        {
            new GameObject("GameData").AddComponent<GameData>();
        }

        return "- Màn câu hỏi: Đã gắn QuestionManager.";
    }

    private static string SetupClimb(GameObject go)
    {
        Animator a = go.GetComponent<Animator>();
        if (!a || !a.runtimeAnimatorController) return "";
        var c = a.runtimeAnimatorController as AnimatorController;
        if (!c) return "";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Sprites/Bean_1_science/Player/climbplayer.anim");
        if (!clip) return "";

        bool hp = false;
        foreach (var pr in c.parameters) if (pr.name == "IsClimbing") hp = true;
        if (!hp) c.AddParameter("IsClimbing", AnimatorControllerParameterType.Bool);

        var sm = c.layers[0].stateMachine;
        AnimatorState cs = null;
        foreach (var s in sm.states) if (s.state.name == "Climb") { cs = s.state; break; }
        if (cs == null) cs = sm.AddState("Climb");
        cs.motion = clip;

        bool hi = false;
        foreach (var t in sm.anyStateTransitions) if (t.destinationState == cs) hi = true;
        if (!hi) { var t = sm.AddAnyStateTransition(cs); t.AddCondition(AnimatorConditionMode.If, 0, "IsClimbing"); t.hasFixedDuration = false; t.duration = 0; t.canTransitionToSelf = false; }

        bool ho = false;
        foreach (var t in cs.transitions) if (t.destinationState == sm.defaultState) ho = true;
        if (!ho && sm.defaultState != null) { var t = cs.AddTransition(sm.defaultState); t.AddCondition(AnimatorConditionMode.IfNot, 0, "IsClimbing"); t.hasFixedDuration = false; t.duration = 0; }

        EditorUtility.SetDirty(c);
        AssetDatabase.SaveAssets();
        return "OK";
    }
}
