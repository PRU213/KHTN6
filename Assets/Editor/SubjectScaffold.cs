using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SubjectScaffold
{
    [MenuItem("Tools/6. Tạo Màn Hình Lý Hóa & Liên Kết (Mới)")]
    public static void CreateSubjectScenes()
    {
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

        // Danh sách các scene cần tạo
        string[] newScenes = {
            "Assets/Scenes/Physics/physical_map_game.unity",
            "Assets/Scenes/Physics/theory_physic.unity",
            "Assets/Scenes/Physics/physic_games.unity",
            "Assets/Scenes/Chemistry/chemistry_map_game.unity",
            "Assets/Scenes/Chemistry/theory_chemistry.unity",
            "Assets/Scenes/Chemistry/chemistry_games.unity"
        };

        // Tạo thư mục nếu chưa có
        if (!System.IO.Directory.Exists("Assets/Scenes/Physics"))
            System.IO.Directory.CreateDirectory("Assets/Scenes/Physics");
        if (!System.IO.Directory.Exists("Assets/Scenes/Chemistry"))
            System.IO.Directory.CreateDirectory("Assets/Scenes/Chemistry");

        // Tạo từng scene và thiết lập UI cơ bản
        foreach (var path in newScenes)
        {
            if (!System.IO.File.Exists(path))
            {
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                
                // Tạo Canvas
                GameObject canvasObj = new GameObject("Canvas");
                Canvas canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();

                // Tạo EventSystem
                if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
                {
                    GameObject esObj = new GameObject("EventSystem");
                    esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                    esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                }

                // Tiêu đề
                GameObject titleObj = new GameObject("Title");
                titleObj.transform.SetParent(canvasObj.transform, false);
                Text titleText = titleObj.AddComponent<Text>();
                titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                titleText.text = "Đây là màn: " + System.IO.Path.GetFileNameWithoutExtension(path);
                titleText.fontSize = 40;
                titleText.alignment = TextAnchor.MiddleCenter;
                titleText.color = Color.black;
                RectTransform rtTitle = titleObj.GetComponent<RectTransform>();
                rtTitle.sizeDelta = new Vector2(800, 100);
                rtTitle.anchoredPosition = new Vector2(0, 200);

                // Nút chức năng tùy scene
                if (path.Contains("map_game"))
                {
                    CreateButton(canvasObj, "theory", "Học Lý Thuyết", new Vector2(-150, 0), path.Contains("physical") ? "theory_physic" : "theory_chemistry");
                    CreateButton(canvasObj, "games", "Chọn Game", new Vector2(150, 0), path.Contains("physical") ? "physic_games" : "chemistry_games");
                }
                
                // Nút backhome ở mọi màn
                CreateButton(canvasObj, "backhome", "Về Trang Chủ", new Vector2(0, -200), "HomePage");

                // Đặt Camera background color
                Camera cam = Object.FindAnyObjectByType<Camera>();
                if (cam != null)
                {
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.backgroundColor = new Color(0.9f, 0.9f, 0.9f);
                }

                EditorSceneManager.SaveScene(scene, path);
            }
            AddSceneToBuildSettings(path);
        }

        // Cập nhật HomePage
        if (System.IO.File.Exists("Assets/Scenes/HomePage.unity"))
        {
            var scene0 = EditorSceneManager.OpenScene("Assets/Scenes/HomePage.unity", OpenSceneMode.Single);
            
            LinkButton("Physic", "physical_map_game");
            LinkButton("Chemistry", "chemistry_map_game");
            LinkButton("Biology", "luatchoisinhhoc"); // Đảm bảo biology vẫn đúng

            EditorSceneManager.SaveScene(scene0);
        }

        EditorUtility.DisplayDialog("Thành công", "Đã tạo xong các màn hình cho Lý, Hóa và liên kết từ HomePage!", "OK");
    }

    private static void LinkButton(string objName, string targetScene)
    {
        GameObject btnObj = GameObject.Find(objName);
        if (btnObj != null)
        {
            if (btnObj.GetComponent<Button>() == null) btnObj.AddComponent<Button>();
            var bst = btnObj.GetComponent<ButtonSceneTransition>();
            if (bst == null) bst = btnObj.AddComponent<ButtonSceneTransition>();
            bst.targetScene = targetScene;
        }
    }

    private static void CreateButton(GameObject canvas, string name, string textStr, Vector2 pos, string targetScene)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(canvas.transform, false);
        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0.2f, 0.6f, 1f);
        Button btn = btnObj.AddComponent<Button>();

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200, 80);
        rt.anchoredPosition = pos;

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        Text txt = txtObj.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.text = textStr;
        txt.fontSize = 24;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;

        var bst = btnObj.AddComponent<ButtonSceneTransition>();
        bst.targetScene = targetScene;
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (var s in scenes)
        {
            if (s.path == scenePath) return; // Đã có
        }
        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
