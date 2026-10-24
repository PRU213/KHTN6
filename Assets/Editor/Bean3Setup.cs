using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Tool riêng để setup Bean_3_Biology.
/// Menu: Tools -> 7. Setup Riêng Bean 3 (Mới)
///
/// Phân loại object theo TÊN:
///   - "đất", "cầu"                  → Platform (đứng được)
///   - "trang trí"                    → WallBlock (chặn đi ngang, không đứng lên)
///   - "chỉ dẫn"                      → bỏ qua (đi xuyên)
///   - "thang"                        → LadderZone (leo được)
///   - "monster"                      → Enemy (chạm → qua màn câu hỏi)
///   - "bẫy"                          → TrapZone (chạm → chết)
///   - "Dart"                         → DartTrap (phóng khi gần)
///   - "moving" (không phải moving_1) → VerticalMovingPlatform (lên xuống)
///   - "moving_1"                     → MovingPlatform (sang phải tới cổng)
///   - "cổng"                         → Portal
///   - "player"                       → PlayerController + Animator cute2
///   - "background"                   → bỏ qua
/// </summary>
public class Bean3Setup
{
    [MenuItem("Tools/7. Setup Riêng Bean 3 (Mới)")]
    public static void RunSetupBean3()
    {
        // Mở scene Bean_3_Biology
        string scenePath = "Assets/Scenes/Biology/Bean_3_Biology.unity";
        if (!System.IO.File.Exists(scenePath))
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy " + scenePath, "OK");
            return;
        }

        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // Thêm vào Build Settings
        AddSceneToBuildSettings(scenePath);
        AddSceneToBuildSettings("Assets/Scenes/Biology/bean_1_question.unity");

        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy Canvas trong scene!", "OK");
            return;
        }

        // Camera
        Camera cam = Object.FindAnyObjectByType<Camera>();
        if (cam != null)
        {
            if (cam.GetComponent<CameraFollow>() == null)
                cam.gameObject.AddComponent<CameraFollow>();
        }

        // GameData
        if (Object.FindAnyObjectByType<GameData>() == null)
        {
            GameObject gdObj = new GameObject("GameData");
            gdObj.AddComponent<GameData>();
        }

        Image[] allImages = canvas.GetComponentsInChildren<Image>(true);

        int platforms = 0, ladders = 0, enemies = 0, darts = 0, traps = 0, walls = 0, movings = 0;
        GameObject playerObj = null;

        // Tìm trước tọa độ của đất_chạm và đất bay
        float datChamX = 0f;
        float datBayY = 0f;
        foreach (Image img in allImages)
        {
            string n = img.gameObject.name.ToLower().Trim();
            if (n.Contains("đất_chạm") || n.Contains("dat_cham") || n.Contains("datcham"))
            {
                datChamX = img.rectTransform.anchoredPosition.x - (img.rectTransform.sizeDelta.x / 2f) - 60f; 
            }
            if (n.Contains("đất bay") || n.Contains("dat bay") || n.Contains("đất_bay"))
            {
                datBayY = img.rectTransform.anchoredPosition.y - (img.rectTransform.sizeDelta.y / 2f) - 60f;
            }
        }

        foreach (Image img in allImages)
        {
            string objName = img.gameObject.name.ToLower().Trim();
            RectTransform rt = img.rectTransform;

            // Bỏ qua background
            if (objName.Contains("background") || objName.Contains("bg"))
                continue;

            // Bỏ qua chỉ dẫn (đi xuyên)
            if (objName.Contains("chỉ dẫn") || objName.Contains("chi dan") || objName.Contains("chidan"))
                continue;

            // === CỔNG ===
            if (objName.Contains("cổng") || objName.Contains("cong") || objName.Contains("portal"))
            {
                if (img.GetComponent<Portal>() == null)
                    img.gameObject.AddComponent<Portal>();
                continue;
            }

            // === PLAYER ===
            if (objName == "player" || objName == "player_1")
            {
                Animator anim = img.GetComponent<Animator>();
                if (anim == null) anim = img.gameObject.AddComponent<Animator>();

                var controller = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>("Assets/Animation/cute2.controller");
                if (controller != null) anim.runtimeAnimatorController = controller;

                if (img.GetComponent<PlayerController>() == null)
                    img.gameObject.AddComponent<PlayerController>();

                playerObj = img.gameObject;
                continue;
            }

            // === MONSTER ===
            if (objName.Contains("monster") || objName.Contains("quái") || objName.Contains("enemy"))
            {
                if (img.GetComponent<Enemy>() == null)
                    img.gameObject.AddComponent<Enemy>();
                enemies++;
                continue;
            }

            // === BẪY ===
            if (objName.Contains("bẫy") || objName.Contains("trap"))
            {
                if (img.GetComponent<TrapZone>() == null)
                    img.gameObject.AddComponent<TrapZone>();
                traps++;
                continue;
            }

            // === DART ===
            if (objName.Contains("dart"))
            {
                if (img.GetComponent<DartTrap>() == null)
                    img.gameObject.AddComponent<DartTrap>();
                darts++;
                continue;
            }

            // === THANG ===
            if (objName.Contains("thang") || objName.Contains("ladder") || objName.Contains("lader"))
            {
                if (img.GetComponent<LadderZone>() == null)
                    img.gameObject.AddComponent<LadderZone>();
                ladders++;
                continue;
            }

            // === TRANG TRÍ (chặn đi ngang) ===
            if (objName.Contains("trang trí") || objName.Contains("trang tri") || objName.Contains("trangtri"))
            {
                if (img.GetComponent<WallBlock>() == null)
                    img.gameObject.AddComponent<WallBlock>();
                
                var oldTrap = img.GetComponent<TrapZone>();
                if (oldTrap != null) Object.DestroyImmediate(oldTrap);

                walls++;
                continue;
            }

            // === MOVING_1 (đi ngang sang phải đến gần cổng) ===
            if (objName == "moving_1" || objName == "moving (1)" || objName == "moving (2)" || objName == "moving_2")
            {
                Platform p = img.GetComponent<Platform>();
                if (p == null) p = img.gameObject.AddComponent<Platform>();
                p.surfaceOffset = 5f;

                // Xóa script dọc nếu có
                var oldVmp = img.GetComponent<VerticalMovingPlatform>();
                if (oldVmp != null) Object.DestroyImmediate(oldVmp);

                MovingPlatform mp = img.GetComponent<MovingPlatform>();
                if (mp == null) mp = img.gameObject.AddComponent<MovingPlatform>();
                mp.speed = 120f;
                mp.useCustomBounds = true;
                mp.customMinX = img.rectTransform.anchoredPosition.x;
                
                if (datChamX != 0f) 
                    mp.customMaxX = datChamX;
                else 
                    mp.customMaxX = img.rectTransform.anchoredPosition.x + 900f; // Đi thẳng tới cổng 900 pixel

                EditorUtility.SetDirty(p);
                EditorUtility.SetDirty(mp);
                movings++;
                continue;
            }

            // === MOVING (lên xuống) ===
            if (objName.Contains("moving"))
            {
                Platform p = img.GetComponent<Platform>();
                if (p == null) p = img.gameObject.AddComponent<Platform>();
                p.surfaceOffset = 5f;

                // Xóa script ngang nếu có
                var oldMp = img.GetComponent<MovingPlatform>();
                if (oldMp != null) Object.DestroyImmediate(oldMp);

                VerticalMovingPlatform vmp = img.GetComponent<VerticalMovingPlatform>();
                if (vmp == null) vmp = img.gameObject.AddComponent<VerticalMovingPlatform>();
                vmp.speed = 120f;
                vmp.waitTime = 1f;
                vmp.useCustomBounds = true;
                vmp.customMinY = img.rectTransform.anchoredPosition.y;
                
                if (datBayY != 0f)
                    vmp.customMaxY = datBayY;
                else
                    vmp.customMaxY = img.rectTransform.anchoredPosition.y + 400f; // Bay lên bục trên 400 pixel

                EditorUtility.SetDirty(p);
                EditorUtility.SetDirty(vmp);
                movings++;
                continue;
            }

            // === ĐẤT, CẦU (platform - đứng được) ===
            if (objName.Contains("đất") || objName.Contains("dat") || objName.Contains("cầu") || objName.Contains("cau"))
            {
                Platform p = img.GetComponent<Platform>();
                if (p == null) p = img.gameObject.AddComponent<Platform>();
                p.surfaceOffset = 5f;
                
                // Xóa script di chuyển nếu trước đó nó từng là moving nhưng nay đã đổi tên thành đất
                var oldMp = img.GetComponent<MovingPlatform>();
                if (oldMp != null) Object.DestroyImmediate(oldMp);
                
                var oldVmp = img.GetComponent<VerticalMovingPlatform>();
                if (oldVmp != null) Object.DestroyImmediate(oldVmp);

                var oldTrap = img.GetComponent<TrapZone>();
                if (oldTrap != null) Object.DestroyImmediate(oldTrap);

                EditorUtility.SetDirty(p);
                platforms++;
                continue;
            }
        }

        EditorSceneManager.SaveScene(scene);

        string report = $"Setup Bean 3 xong!\n\n" +
            $"✅ Platform (đất, cầu): {platforms}\n" +
            $"🪜 Thang: {ladders}\n" +
            $"👾 Monster: {enemies}\n" +
            $"🏹 Dart: {darts}\n" +
            $"💀 Bẫy: {traps}\n" +
            $"🧱 Tường chặn (trang trí): {walls}\n" +
            $"🔄 Moving: {movings}\n" +
            $"🎮 Player: {(playerObj != null ? "OK" : "KHÔNG TÌM THẤY")}";

        EditorUtility.DisplayDialog("Setup Bean 3", report, "OK");
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
}
