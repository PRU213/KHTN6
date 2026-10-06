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

        // Danh sách các màn chơi chính
        string[] gameScenes = {
            "Assets/Scenes/Biology/bean_1.unity",
            "Assets/Scenes/Biology/Bean_2_Biology.unity",
            "Assets/Scenes/Biology/Bean_3_Biology.unity"
        };

        // Danh sách các màn câu hỏi tương ứng
        string[] questionScenes = {
            "Assets/Scenes/Biology/bean_1_question.unity",
            "Assets/Scenes/Biology/Bean_2_Biology_Question.unity",
            "Assets/Scenes/Biology/Bean_3_Biology_Question.unity"
        };

        string results = "";

        // Setup các màn chơi chính
        for (int i = 0; i < gameScenes.Length; i++)
        {
            if (!System.IO.File.Exists(gameScenes[i])) continue;
            AddSceneToBuildSettings(gameScenes[i]);
            var scene = EditorSceneManager.OpenScene(gameScenes[i], OpenSceneMode.Single);
            if (scene.IsValid())
            {
                string res = SetupBean1();
                results += $"\n- {System.IO.Path.GetFileNameWithoutExtension(gameScenes[i])}: {res}";
                EditorSceneManager.SaveScene(scene);
            }
        }

        // Setup các màn câu hỏi
        for (int i = 0; i < questionScenes.Length; i++)
        {
            if (!System.IO.File.Exists(questionScenes[i])) continue;
            AddSceneToBuildSettings(questionScenes[i]);
            var scene = EditorSceneManager.OpenScene(questionScenes[i], OpenSceneMode.Single);
            if (scene.IsValid())
            {
                string res = SetupBean1Question();
                results += $"\n- {System.IO.Path.GetFileNameWithoutExtension(questionScenes[i])}: {res}";
                EditorSceneManager.SaveScene(scene);
            }
        }

        // === BEAN_TONG ===
        if (System.IO.File.Exists("Assets/Scenes/Biology/bean_tong.unity"))
        {
            AddSceneToBuildSettings("Assets/Scenes/Biology/bean_tong.unity");
            var scene3 = EditorSceneManager.OpenScene("Assets/Scenes/Biology/bean_tong.unity", OpenSceneMode.Single);
            if (scene3.IsValid())
            {
                string res3 = SetupBeanTong();
                results += $"\n- bean_tong: {res3}";
                EditorSceneManager.SaveScene(scene3);
            }
        }

        // === BEANTONG_QUESTION ===
        if (System.IO.File.Exists("Assets/Scenes/Biology/beantong_question.unity"))
        {
            AddSceneToBuildSettings("Assets/Scenes/Biology/beantong_question.unity");
            var scene4 = EditorSceneManager.OpenScene("Assets/Scenes/Biology/beantong_question.unity", OpenSceneMode.Single);
            if (scene4.IsValid())
            {
                string res4 = SetupBeanTongQuestion();
                results += $"\n- beantong_question: {res4}";
                EditorSceneManager.SaveScene(scene4);
            }
        }

        // === VIDEO GIOI THIEU ===
        if (System.IO.File.Exists("Assets/Scenes/Biology/video_gioithieu.unity"))
        {
            AddSceneToBuildSettings("Assets/Scenes/Biology/video_gioithieu.unity");
            var scene5 = EditorSceneManager.OpenScene("Assets/Scenes/Biology/video_gioithieu.unity", OpenSceneMode.Single);
            if (scene5.IsValid())
            {
                string res5 = SetupVideoGioiThieu();
                results += $"\n- video_gioithieu: {res5}";
                EditorSceneManager.SaveScene(scene5);
            }
        }

        EditorUtility.DisplayDialog("Ket qua Setup", "Thành công!\n" + results, "OK");
    }

    [MenuItem("Tools/2. Setup Riêng Bean 2 (Fix lỗi)")]
    public static void RunSetupBean2()
    {
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

        string scenePath = "Assets/Scenes/Biology/Bean_2_Biology.unity";
        if (!System.IO.File.Exists(scenePath))
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy Bean_2_Biology!", "OK");
            return;
        }

        AddSceneToBuildSettings(scenePath);
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        
        // 1. Sửa lỗi xóa nhầm QuestionManager do phiên bản trước gắn nhầm
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas != null)
        {
            QuestionManager qm = canvas.GetComponent<QuestionManager>();
            if (qm != null) Object.DestroyImmediate(qm);
            
            // Xóa các object đáp án sinh ra do lỗi
            for (int i = canvas.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = canvas.transform.GetChild(i);
                if (child.name.StartsWith("đáp án") || child.name == "bảng" || child.name.StartsWith("tim"))
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }
        }

    // 2. Chạy setup chuẩn
        string res = SetupBean1();
        EditorSceneManager.SaveScene(scene);

        EditorUtility.DisplayDialog("Setup Bean 2", "Đã fix xong lỗi hiện đáp án và setup leo trèo/di chuyển!\nKết quả: " + res, "OK");
    }

    [MenuItem("Tools/3. Setup Riêng Bean 3")]
    public static void RunSetupBean3()
    {
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

        string scenePath = "Assets/Scenes/Biology/Bean_3_Biology.unity";
        if (!System.IO.File.Exists(scenePath))
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy Bean_3_Biology!", "OK");
            return;
        }

        AddSceneToBuildSettings(scenePath);
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        
        // 1. Dọn dẹp lỗi UI (nếu có)
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas != null)
        {
            QuestionManager qm = canvas.GetComponent<QuestionManager>();
            if (qm != null) Object.DestroyImmediate(qm);
            
            for (int i = canvas.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = canvas.transform.GetChild(i);
                if (child.name.StartsWith("đáp án") || child.name == "bảng" || child.name.StartsWith("tim"))
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }
        }

        // 2. Chạy setup chuẩn
        string res = SetupBean1();
        EditorSceneManager.SaveScene(scene);

        EditorUtility.DisplayDialog("Setup Bean 3", "Đã setup thành công môi trường cho Bean 3!\nKết quả: " + res, "OK");
    }

    [MenuItem("Tools/4. Liên Kết Các Màn Chơi (Tự động)")]
    public static void RunSetupLinks()
    {
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

        // 0. HomePage -> luatchoisinhhoc (chỉ khi click nút Sinh học), và nối Lý/Hóa sang SampleScene
        if (System.IO.File.Exists("Assets/Scenes/HomePage.unity"))
        {
            AddSceneToBuildSettings("Assets/Scenes/HomePage.unity");
            var scene0 = EditorSceneManager.OpenScene("Assets/Scenes/HomePage.unity", OpenSceneMode.Single);
            
            // Nút Sinh
            GameObject bioObj = GameObject.Find("Biology");
            if (bioObj != null)
            {
                if (bioObj.GetComponent<UnityEngine.UI.Button>() == null) bioObj.AddComponent<UnityEngine.UI.Button>();
                var bst = bioObj.GetComponent<ButtonSceneTransition>();
                if (bst == null) bst = bioObj.AddComponent<ButtonSceneTransition>();
                bst.targetScene = "luatchoisinhhoc";
            }

            // Nút Lý
            GameObject phyObj = GameObject.Find("Physic");
            if (phyObj != null)
            {
                if (phyObj.GetComponent<UnityEngine.UI.Button>() == null) phyObj.AddComponent<UnityEngine.UI.Button>();
                var gto = phyObj.GetComponent<GoToSampleScenePanel>();
                if (gto == null) gto = phyObj.AddComponent<GoToSampleScenePanel>();
                gto.subjectName = "Physic";
            }

            // Nút Hóa
            GameObject chemObj = GameObject.Find("Chemistry");
            if (chemObj != null)
            {
                if (chemObj.GetComponent<UnityEngine.UI.Button>() == null) chemObj.AddComponent<UnityEngine.UI.Button>();
                var gto = chemObj.GetComponent<GoToSampleScenePanel>();
                if (gto == null) gto = chemObj.AddComponent<GoToSampleScenePanel>();
                gto.subjectName = "Chemistry";
            }
            
            EditorSceneManager.SaveScene(scene0);
        }

        // 0.5. Cấu hình SampleScene
        if (System.IO.File.Exists("Assets/Scenes/SampleScene.unity"))
        {
            AddSceneToBuildSettings("Assets/Scenes/SampleScene.unity");
            var sceneSample = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            
            Camera cam = Object.FindAnyObjectByType<Camera>();
            if (cam != null)
            {
                var mgr = cam.GetComponent<SampleScenePanelManager>();
                if (mgr == null) mgr = cam.gameObject.AddComponent<SampleScenePanelManager>();
                
                GameObject pMap = GameObject.Find("Physical_map_game");
                GameObject cMap = GameObject.Find("Chemistry_map_game");
                GameObject pTheory = GameObject.Find("theory_physic");
                GameObject cTheory = GameObject.Find("theory_chemistry");

                if (pMap != null) mgr.physicsMap = pMap;
                if (cMap != null) mgr.chemistryMap = cMap;
                if (pTheory != null) mgr.theoryPhysics = pTheory;
                if (cTheory != null) mgr.theoryChemistry = cTheory;
            }
            EditorSceneManager.SaveScene(sceneSample);
        }

        // 1. Màn luật chơi sinh học -> bean_1
        if (System.IO.File.Exists("Assets/Scenes/Biology/luatchoisinhhoc.unity"))
        {
            AddSceneToBuildSettings("Assets/Scenes/Biology/luatchoisinhhoc.unity");
            AddSceneToBuildSettings("Assets/Scenes/Biology/bean_1.unity"); // Phải thêm bean_1 vào Build Settings
            var scene1 = EditorSceneManager.OpenScene("Assets/Scenes/Biology/luatchoisinhhoc.unity", OpenSceneMode.Single);
            
            // Xóa script click màn hình cũ
            Camera cam = Object.FindAnyObjectByType<Camera>();
            if (cam != null)
            {
                var oldSt = cam.GetComponent<SceneTransitionOnClick>();
                if (oldSt != null) Object.DestroyImmediate(oldSt);
            }

            // Tìm nút start
            GameObject startBtn = GameObject.Find("start");
            if (startBtn != null)
            {
                if (startBtn.GetComponent<UnityEngine.UI.Button>() == null) startBtn.AddComponent<UnityEngine.UI.Button>();
                var bst = startBtn.GetComponent<ButtonSceneTransition>();
                if (bst == null) bst = startBtn.AddComponent<ButtonSceneTransition>();
                bst.targetScene = "bean_1";
            }

            // Tìm nút backhome
            GameObject homeBtn = GameObject.Find("backhome");
            if (homeBtn != null)
            {
                if (homeBtn.GetComponent<UnityEngine.UI.Button>() == null) homeBtn.AddComponent<UnityEngine.UI.Button>();
                var bst = homeBtn.GetComponent<ButtonSceneTransition>();
                if (bst == null) bst = homeBtn.AddComponent<ButtonSceneTransition>();
                bst.targetScene = "HomePage";
            }

            EditorSceneManager.SaveScene(scene1);
        }

        // 2. Màn video giới thiệu -> bean_tong
        if (System.IO.File.Exists("Assets/Scenes/Biology/video_gioithieu.unity"))
        {
            AddSceneToBuildSettings("Assets/Scenes/Biology/video_gioithieu.unity");
            AddSceneToBuildSettings("Assets/Scenes/Biology/bean_tong.unity");
            AddSceneToBuildSettings("Assets/Scenes/Biology/victory.unity");
            var scene2 = EditorSceneManager.OpenScene("Assets/Scenes/Biology/video_gioithieu.unity", OpenSceneMode.Single);
            var vp = Object.FindAnyObjectByType<UnityEngine.Video.VideoPlayer>();
            if (vp != null && vp.GetComponent<VideoEndTransition>() == null)
            {
                var vet = vp.gameObject.AddComponent<VideoEndTransition>();
                vet.nextSceneName = "bean_tong";
            }
            else
            {
                Camera cam = Object.FindAnyObjectByType<Camera>();
                if (cam != null && cam.GetComponent<VideoEndTransition>() == null)
                {
                    var vet = cam.gameObject.AddComponent<VideoEndTransition>();
                    vet.nextSceneName = "bean_tong";
                }
            }
            EditorSceneManager.SaveScene(scene2);
        }

        // 3. Màn victory -> HomePage
        if (System.IO.File.Exists("Assets/Scenes/Biology/victory.unity"))
        {
            AddSceneToBuildSettings("Assets/Scenes/Biology/victory.unity");
            var sceneVic = EditorSceneManager.OpenScene("Assets/Scenes/Biology/victory.unity", OpenSceneMode.Single);
            
            // Xóa script click màn hình cũ
            Camera cam = Object.FindAnyObjectByType<Camera>();
            if (cam != null)
            {
                var oldSt = cam.GetComponent<SceneTransitionOnClick>();
                if (oldSt != null) Object.DestroyImmediate(oldSt);
            }

            // Tìm nút backhome
            GameObject homeBtn = GameObject.Find("backhome");
            if (homeBtn != null)
            {
                if (homeBtn.GetComponent<UnityEngine.UI.Button>() == null) homeBtn.AddComponent<UnityEngine.UI.Button>();
                var bst = homeBtn.GetComponent<ButtonSceneTransition>();
                if (bst == null) bst = homeBtn.AddComponent<ButtonSceneTransition>();
                bst.targetScene = "HomePage";
            }

            // Tìm nút start (nếu có)
            GameObject startBtn = GameObject.Find("start");
            if (startBtn != null)
            {
                if (startBtn.GetComponent<UnityEngine.UI.Button>() == null) startBtn.AddComponent<UnityEngine.UI.Button>();
                var bst = startBtn.GetComponent<ButtonSceneTransition>();
                if (bst == null) bst = startBtn.AddComponent<ButtonSceneTransition>();
                bst.targetScene = "bean_1";
            }

            EditorSceneManager.SaveScene(sceneVic);
        }

        EditorUtility.DisplayDialog("Liên kết", "Đã thiết lập xong luồng chuyển cảnh với các nút start / backhome!", "OK");
    }

    [MenuItem("Tools/5. Sửa Lỗi Lún Chân (Cho Màn Hiện Tại)")]
    public static void FixFootOffset()
    {
        int count = 0;
        
        // Sửa Platform
        Platform[] platforms = Object.FindObjectsByType<Platform>(FindObjectsSortMode.None);
        foreach (var p in platforms)
        {
            if (p.surfaceOffset > 10f)
            {
                p.surfaceOffset = 5f;
                EditorUtility.SetDirty(p);
                count++;
            }
        }

        // Sửa Vertical Moving Platform (bị lún)
        VerticalMovingPlatform[] vmps = Object.FindObjectsByType<VerticalMovingPlatform>(FindObjectsSortMode.None);
        foreach (var vmp in vmps)
        {
            Platform p = vmp.GetComponent<Platform>();
            if (p != null)
            {
                p.surfaceOffset = 5f;
                EditorUtility.SetDirty(p);
            }
        }
        
        // Sửa Moving Platform
        MovingPlatform[] mps = Object.FindObjectsByType<MovingPlatform>(FindObjectsSortMode.None);
        foreach (var mp in mps)
        {
            Platform p = mp.GetComponent<Platform>();
            if (p != null)
            {
                p.surfaceOffset = 5f;
                EditorUtility.SetDirty(p);
            }
        }

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("Sửa lỗi lún chân", $"Đã nâng vạch vàng lên sát mép trên cho {count} bệ đỡ.\n\nHãy Play thử để xem nhân vật đã đứng trên bề mặt chưa!", "OK");
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
    // SETUP GAME SCENE (Chung cho bean_1, bean_2, bean_3...)
    // =========================================================
    private static string SetupBean1()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) return "- Không tìm thấy Canvas!";

        // Xóa QuestionManager vì đây là GAME SCENE
        QuestionManager qm = canvas.GetComponent<QuestionManager>();
        if (qm != null) Object.DestroyImmediate(qm);
        // Xóa các object đáp án sinh ra do lỗi copy nhầm
        for (int i = canvas.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = canvas.transform.GetChild(i);
            if (child.name.StartsWith("đáp án") || child.name == "bảng" || child.name.StartsWith("tim"))
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }

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

            if (objName.Contains("dart") || objName.Contains("phitieu") || objName.Contains("phi_tieu") || objName.Contains("phi tieu") || objName.Contains("tendo") || objName.Contains("tên độc") || objName.Contains("tênđộc") || objName.Contains("tên"))
            { SetupDart(img, dart); dart++; continue; }

            if (objName.Contains("moving") || objName.Contains("gachdi") || objName.Contains("gach_di") || objName.Contains("gach di"))
            {
                SetupMoving(img, moving);
                MovingPlatform mp = img.GetComponent<MovingPlatform>();
                if (mp != null) { mp.defaultTravelDistance = 400f; }
                moving++; continue;
            }

            if (objName.Contains("cloud"))
            {
                CloudMove cm = img.GetComponent<CloudMove>();
                if (cm == null) cm = img.gameObject.AddComponent<CloudMove>();
                EditorUtility.SetDirty(cm);
                continue;
            }

            if (objName.Contains("portal") || objName.Contains("cong") || objName.Contains("cổng"))
            {
                if (!img.GetComponent<Portal>()) img.gameObject.AddComponent<Portal>();
                continue;
            }

            if (objName == "player" || objName == "player_1" || img.GetComponent<Animator>() != null)
            {
                Animator anim = img.GetComponent<Animator>();
                if (!anim) anim = img.gameObject.AddComponent<Animator>();
                
                // Gắn luôn cute2.controller của bean_1 sang cho player theo yêu cầu
                var controller = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>("Assets/Animation/cute2.controller");
                if (controller != null) anim.runtimeAnimatorController = controller;

                if (!img.GetComponent<PlayerController>()) img.gameObject.AddComponent<PlayerController>();
                playerObj = img.gameObject;
                continue;
            }

            if (objName.Contains("gonang") || objName.Contains("go_nang") || objName.Contains("gỗ_nâng") || objName.Contains("gỗ nâng"))
            {
                VerticalMovingPlatform vmp = img.GetComponent<VerticalMovingPlatform>();
                if (vmp == null) vmp = img.gameObject.AddComponent<VerticalMovingPlatform>();
                vmp.speed = 80f; vmp.waitTime = 1f; vmp.defaultTravelDistance = 200f;
                EditorUtility.SetDirty(vmp);
                moving++;
                continue;
            }

            if (objName.Contains("monster") || objName.Contains("monser") || objName.Contains("quai") || objName.Contains("quái") || objName.Contains("enemy") || objName.Contains("slime") || objName.Contains("virus"))
            {
                if (!img.GetComponent<Enemy>()) img.gameObject.AddComponent<Enemy>();
                ene++;
                continue;
            }

            if (objName.Contains("dayleo") || objName.Contains("day_leo") || objName.Contains("ladder") || objName.Contains("lader") || objName.Contains("thang") || (w > 45 && w < 115 && h > 90 && h < 360))
            {
                if (!img.GetComponent<LadderZone>()) img.gameObject.AddComponent<LadderZone>();
                lad++;
                continue;
            }

            if (objName.Contains("dat") || objName.Contains("nen_dat") || objName.Contains("platform") || objName.Contains("daycau") || objName.Contains("day_cau") || objName.Contains("beo") || objName.Contains("bèo") || objName.Contains("gỗ") || objName.Contains("đảo") || objName.Contains("cầu") || (w > 75 && w < 700 && h > 55 && h < 250))
            {
                Platform p = img.GetComponent<Platform>();
                if (p == null) p = img.gameObject.AddComponent<Platform>();
                EditorUtility.SetDirty(p);
                plat++;
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
