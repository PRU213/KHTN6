using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;

public class Bean1Setup : MonoBehaviour
{
    [MenuItem("Tools/1. Setup Tất Cả (Bấm vào đây)")]
    public static void RunSetup()
    {
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

        // Thêm các cảnh vào Build Settings nếu chưa có
        AddSceneToBuildSettings("Assets/Scenes/Biology/bean_1.unity");
        AddSceneToBuildSettings("Assets/Scenes/Biology/bean_1_question.unity");
        AddSceneToBuildSettings("Assets/Scenes/Biology/Bean_2_Biology.unity");
        AddSceneToBuildSettings("Assets/Scenes/Biology/bean_tong.unity");
        AddSceneToBuildSettings("Assets/Scenes/Biology/beantong_question.unity");

        // === BEAN_1 ===
        var scene1 = EditorSceneManager.OpenScene("Assets/Scenes/Biology/bean_1.unity", OpenSceneMode.Single);
        string res1 = "Loi mo bean_1";
        if (scene1.IsValid())
        {
            res1 = SetupBean1();
            EditorSceneManager.SaveScene(scene1);
        }

        // === BEAN_2_BIOLOGY (câu hỏi bean_1) ===
        var scene2 = EditorSceneManager.OpenScene("Assets/Scenes/Biology/Bean_2_Biology.unity", OpenSceneMode.Single);
        string res2 = "Loi mo Bean_2_Biology";
        if (scene2.IsValid())
        {
            res2 = SetupBean1Question();
            EditorSceneManager.SaveScene(scene2);
        }

        // === BEAN_TONG ===
        var scene3 = EditorSceneManager.OpenScene("Assets/Scenes/Biology/bean_tong.unity", OpenSceneMode.Single);
        string res3 = "Loi mo bean_tong";
        if (scene3.IsValid())
        {
            res3 = SetupBeanTong();
            EditorSceneManager.SaveScene(scene3);
        }

        // === BEANTONG_QUESTION (scene câu hỏi đã có sẵn) ===
        var scene4 = EditorSceneManager.OpenScene("Assets/Scenes/Biology/beantong_question.unity", OpenSceneMode.Single);
        string res4 = "Loi mo beantong_question";
        if (scene4.IsValid())
        {
            res4 = SetupBeanTongQuestion();
            EditorSceneManager.SaveScene(scene4);
        }

        // === VIDEO GIOI THIEU ===
        string res5 = "Khong co scene video_gioithieu";
        if (System.IO.File.Exists("Assets/Scenes/Biology/video_gioithieu.unity"))
        {
            AddSceneToBuildSettings("Assets/Scenes/Biology/video_gioithieu.unity");
            var scene5 = EditorSceneManager.OpenScene("Assets/Scenes/Biology/video_gioithieu.unity", OpenSceneMode.Single);
            if (scene5.IsValid())
            {
                res5 = SetupVideoGioiThieu();
                EditorSceneManager.SaveScene(scene5);
            }
        }

        EditorUtility.DisplayDialog("Ket qua Setup",
            $"bean_1: {res1}\nBean_2_Biology: {res2}\nbean_tong: {res3}\nbeantong_question: {res4}\nvideo_gioithieu: {res5}", "OK");
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (var s in scenes)
        {
            if (s.path == scenePath) return;
        }
        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    // =========================================================
    // SETUP BEAN_1 (giữ nguyên, không thay đổi)
    // =========================================================
    private static string SetupBean1()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) return "- Không tìm thấy Canvas!";

        int plat = 0, lad = 0, ene = 0, dart = 0, moving = 0;
        GameObject playerObj = null;

        Image[] allImages = canvas.GetComponentsInChildren<Image>(true);
        foreach (Image img in allImages)
        {
            RectTransform rt = img.rectTransform;
            float w = rt.sizeDelta.x;
            float h = rt.sizeDelta.y;
            string objName = img.gameObject.name.ToLower();

            if (rt.anchorMin == Vector2.zero && rt.anchorMax == Vector2.one) continue;

            if (objName.Contains("dart") || objName.Contains("phitieu") || objName.Contains("phi_tieu") || objName.Contains("phi tieu"))
            { SetupDart(img, dart); dart++; continue; }

            if (objName.Contains("moving") || objName.Contains("gachdi") || objName.Contains("gach_di") || objName.Contains("gach di"))
            { SetupMoving(img, moving); moving++; continue; }

            if (objName.Contains("cloud"))
            {
                CloudMove cm = img.GetComponent<CloudMove>();
                if (cm == null) cm = img.gameObject.AddComponent<CloudMove>();
                EditorUtility.SetDirty(cm);
                continue;
            }

            if (objName.Contains("portal") || objName.Contains("cong"))
            {
                if (!img.GetComponent<Portal>()) img.gameObject.AddComponent<Portal>();
                img.gameObject.name = "Portal";
                continue;
            }

            if (objName == "player" || objName == "player_1" || img.GetComponent<Animator>() != null)
            {
                if (!img.GetComponent<Animator>()) img.gameObject.AddComponent<Animator>();
                if (!img.GetComponent<PlayerController>()) img.gameObject.AddComponent<PlayerController>();
                img.gameObject.name = "Player";
                playerObj = img.gameObject;
                continue;
            }

            if (w > 45 && w < 115 && h > 90 && h < 360)
            {
                if (!img.GetComponent<LadderZone>()) img.gameObject.AddComponent<LadderZone>();
                img.gameObject.name = "Ladder_" + lad++;
                continue;
            }

            if (objName.Contains("dat") || objName.Contains("nen_dat") || (w > 75 && w < 700 && h > 55 && h < 250))
            {
                Platform p = img.GetComponent<Platform>();
                if (p == null) p = img.gameObject.AddComponent<Platform>();
                EditorUtility.SetDirty(p);
                img.gameObject.name = "Platform_" + plat++;
                continue;
            }

            if (objName.Contains("monster") || objName.Contains("monser") || objName.Contains("quai") || objName.Contains("quái") || (w > 35 && w < 90 && h > 35 && h < 120))
            {
                if (img.GetComponent<Platform>() || img.GetComponent<LadderZone>()) continue;
                if (!img.GetComponent<Enemy>()) img.gameObject.AddComponent<Enemy>();
                img.gameObject.name = "Enemy_" + ene++;
                continue;
            }
        }

        if (Object.FindAnyObjectByType<GameData>() == null)
            new GameObject("GameData").AddComponent<GameData>();

        if (playerObj) SetupClimb(playerObj);

        return $"({plat}) Đất, ({lad}) Thang, ({ene}) Quái, ({dart}) Phi tiêu, ({moving}) Gạch.";
    }

    // =========================================================
    // SETUP BEAN_TONG (riêng, tách biệt hoàn toàn)
    // =========================================================
    private static string SetupBeanTong()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) return "- Không tìm thấy Canvas!";

        int plat = 0, ene = 0, cloudCount = 0;
        GameObject playerObj = null;

        Image[] allImages = canvas.GetComponentsInChildren<Image>(true);
        foreach (Image img in allImages)
        {
            RectTransform rt = img.rectTransform;
            float w = rt.sizeDelta.x;
            float h = rt.sizeDelta.y;
            string objName = img.gameObject.name.ToLower();

            // Bỏ qua ảnh nền
            if (rt.anchorMin == Vector2.zero && rt.anchorMax == Vector2.one) continue;

            // === MONSTER (quái vật - tên chứa "monster" hoặc "monser") ===
            if (objName.Contains("monster") || objName.Contains("monser") || objName.Contains("quai") || objName.Contains("quái"))
            {
                // Xóa Enemy cũ nếu có
                Enemy oldEnemy = img.GetComponent<Enemy>();
                if (oldEnemy != null) Object.DestroyImmediate(oldEnemy);

                if (!img.GetComponent<EnemyTong>()) img.gameObject.AddComponent<EnemyTong>();
                EditorUtility.SetDirty(img.gameObject);
                ene++;
                continue;
            }

            // === MÂY (cloud - đứng trên được + trôi ngang) ===
            if (objName.Contains("cloud"))
            {
                // Gán CloudMove (trôi ngang)
                CloudMove cm = img.GetComponent<CloudMove>();
                if (cm == null) cm = img.gameObject.AddComponent<CloudMove>();

                // Gán Platform (nhân vật đứng trên được)
                Platform p = img.GetComponent<Platform>();
                if (p == null) p = img.gameObject.AddComponent<Platform>();
                p.surfaceOffset = 20f;

                EditorUtility.SetDirty(cm);
                EditorUtility.SetDirty(p);
                cloudCount++;
                continue;
            }

            // === PLAYER ===
            if (objName == "player" || objName == "player_1" || img.GetComponent<Animator>() != null)
            {
                if (!img.GetComponent<Animator>()) img.gameObject.AddComponent<Animator>();
                if (!img.GetComponent<PlayerController>()) img.gameObject.AddComponent<PlayerController>();
                img.gameObject.name = "Player";
                playerObj = img.gameObject;
                continue;
            }

            // === ĐẤT NỀN / LÁ CÂY (platform) ===
            // than_* (thân cây) bỏ qua vì nó là trang trí
            if (objName.Contains("than_")) continue;

            // background bỏ qua
            if (objName.Contains("background")) continue;

            // Những image còn lại có kích thước phù hợp → Platform (lá cây, đất)
            if (w > 75 && h > 55)
            {
                Platform p = img.GetComponent<Platform>();
                if (p == null) p = img.gameObject.AddComponent<Platform>();
                EditorUtility.SetDirty(p);
                if (!img.gameObject.name.StartsWith("Platform_"))
                    img.gameObject.name = "Platform_" + plat;
                plat++;
                continue;
            }
        }

        // Tăng lực nhảy cho bean_tong
        if (playerObj != null)
        {
            PlayerController pc = playerObj.GetComponent<PlayerController>();
            if (pc != null) pc.jumpForce = 1200f;
        }

        // Camera Follow
        Camera mainCam = Camera.main;
        if (mainCam != null && !mainCam.GetComponent<CameraFollow>())
            mainCam.gameObject.AddComponent<CameraFollow>();

        if (Object.FindAnyObjectByType<GameData>() == null)
            new GameObject("GameData").AddComponent<GameData>();

        if (playerObj) SetupClimb(playerObj);

        return $"({plat}) Đất/Lá, ({ene}) Quái, ({cloudCount}) Mây.";
    }

    // =========================================================
    // SETUP BEANTONG_QUESTION (scene câu hỏi đã có sẵn)
    // =========================================================
    private static string SetupBeanTongQuestion()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) return "- Không tìm thấy Canvas!";

        // Xóa QuestionManager cũ nếu có
        QuestionManager oldQM = canvas.GetComponent<QuestionManager>();
        if (oldQM != null) Object.DestroyImmediate(oldQM);

        // Gán QuestionManagerTong riêng cho bean_tong
        if (canvas.GetComponent<QuestionManagerTong>() == null)
            canvas.gameObject.AddComponent<QuestionManagerTong>();

        if (Object.FindAnyObjectByType<GameData>() == null)
            new GameObject("GameData").AddComponent<GameData>();

        return "OK (QuestionManagerTong).";
    }

    // =========================================================
    // HELPERS (không thay đổi)
    // =========================================================
    private static void SetupDart(Image img, int index)
    {
        DartTrap trap = img.GetComponent<DartTrap>();
        if (trap == null) trap = img.gameObject.AddComponent<DartTrap>();
        trap.dartSpeed = 200f;
        trap.triggerHeightRange = 200f;
        trap.cooldown = 4f;
        trap.maxFlyDistance = 2500f;
        if (!img.gameObject.name.StartsWith("Dart_")) img.gameObject.name = "Dart_" + index;
        EditorUtility.SetDirty(trap);
    }

    private static void SetupMoving(Image img, int index)
    {
        Platform plat = img.GetComponent<Platform>();
        if (plat == null) plat = img.gameObject.AddComponent<Platform>();
        MovingPlatform mp = img.GetComponent<MovingPlatform>();
        if (mp == null) mp = img.gameObject.AddComponent<MovingPlatform>();
        mp.speed = 80f; mp.waitTime = 0.8f; mp.edgePadding = 10f;
        if (!img.gameObject.name.StartsWith("Moving_")) img.gameObject.name = "Moving_" + index;
        EditorUtility.SetDirty(mp); EditorUtility.SetDirty(plat);
    }

    private static string SetupBean1Question()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) return "- Không tìm thấy Canvas!";
        if (canvas.GetComponent<QuestionManager>() == null)
            canvas.gameObject.AddComponent<QuestionManager>();
        if (Object.FindAnyObjectByType<GameData>() == null)
            new GameObject("GameData").AddComponent<GameData>();
        return "OK (QuestionManager).";
    }

    private static string SetupClimb(GameObject go)
    {
        Animator a = go.GetComponent<Animator>();
        if (!a || !a.runtimeAnimatorController) return "";
        var c = a.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
        if (!c) return "";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Sprites/Bean_1_science/Player/climbplayer.anim");
        if (!clip) return "";

        bool hp = false;
        foreach (var pr in c.parameters) if (pr.name == "IsClimbing") hp = true;
        if (!hp) c.AddParameter("IsClimbing", AnimatorControllerParameterType.Bool);

        var sm = c.layers[0].stateMachine;
        UnityEditor.Animations.AnimatorState cs = null;
        foreach (var s in sm.states) if (s.state.name == "Climb") { cs = s.state; break; }
        if (cs == null) cs = sm.AddState("Climb");
        cs.motion = clip;

        bool hi = false;
        foreach (var t in sm.anyStateTransitions) if (t.destinationState == cs) hi = true;
        if (!hi) { var t = sm.AddAnyStateTransition(cs); t.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "IsClimbing"); t.hasFixedDuration = false; t.duration = 0; t.canTransitionToSelf = false; }

        bool ho = false;
        foreach (var t in cs.transitions) if (t.destinationState == sm.defaultState) ho = true;
        if (!ho && sm.defaultState != null) { var t = cs.AddTransition(sm.defaultState); t.AddCondition(UnityEditor.Animations.AnimatorConditionMode.IfNot, 0, "IsClimbing"); t.hasFixedDuration = false; t.duration = 0; }

        EditorUtility.SetDirty(c);
        AssetDatabase.SaveAssets();
        return "OK";
    }

    // =========================================================
    // SETUP VIDEO GIOI THIEU
    // =========================================================
    private static string SetupVideoGioiThieu()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            mainCam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
        }

        // Tắt Canvas dư thừa nếu có
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (var c in canvases)
        {
            Object.DestroyImmediate(c.gameObject);
        }

        UnityEngine.Video.VideoPlayer vp = mainCam.GetComponent<UnityEngine.Video.VideoPlayer>();
        if (vp == null)
        {
            vp = mainCam.gameObject.AddComponent<UnityEngine.Video.VideoPlayer>();
        }

        vp.playOnAwake = true;
        vp.renderMode = UnityEngine.Video.VideoRenderMode.CameraNearPlane;
        vp.aspectRatio = UnityEngine.Video.VideoAspectRatio.FitInside; // Đảm bảo full HD
        vp.isLooping = false;

        // Gán clip
        var clip = AssetDatabase.LoadAssetAtPath<UnityEngine.Video.VideoClip>("Assets/Sprites/beantong/hat_dau_vuon_len_thao_nguyen.mp4");
        if (clip != null)
        {
            vp.clip = clip;
        }

        if (mainCam.GetComponent<VideoIntroManager>() == null)
        {
            mainCam.gameObject.AddComponent<VideoIntroManager>();
        }

        return "OK (VideoPlayer + VideoIntroManager)";
    }
}
