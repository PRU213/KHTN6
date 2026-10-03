#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor tool: Tự động gắn EnemyEncounter vào tất cả Enemy trong Bean_2_Biology
/// và thêm các scene cần thiết vào Build Settings.
///
/// Chạy từ menu: Tools > Bean2Biology > Setup Enemy Encounters
/// </summary>
public static class Bean2BiologySetup
{
    private const string GAMEPLAY_SCENE_PATH = "Assets/Scenes/Biology/Bean_2_Biology.unity";
    private const string QUESTION_SCENE_PATH = "Assets/Scenes/Biology/Bean_2_Biology_Question.unity";

    [MenuItem("Tools/Bean2Biology/Setup Enemy Encounters")]
    public static void SetupEnemyEncounters()
    {
        // ─── 1. Thêm scenes vào Build Settings ───────────────────────────────
        AddScenesToBuildSettings();

        // ─── 2. Mở scene gameplay (nếu chưa mở) ──────────────────────────────
        bool sceneWasOpen = false;
        for (int i = 0; i < EditorSceneManager.sceneCount; i++)
        {
            if (EditorSceneManager.GetSceneAt(i).path == GAMEPLAY_SCENE_PATH)
            {
                sceneWasOpen = true;
                break;
            }
        }

        if (!sceneWasOpen)
        {
            bool save = EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            EditorSceneManager.OpenScene(GAMEPLAY_SCENE_PATH, OpenSceneMode.Single);
        }

        // ─── 3. Tìm Nv (player) RectTransform ────────────────────────────────
        GameObject nvGo = GameObject.Find("Nv");
        if (nvGo == null)
        {
            EditorUtility.DisplayDialog(
                "Setup thất bại",
                "Không tìm thấy GameObject tên 'Nv' trong scene Bean_2_Biology.\n" +
                "Hãy mở scene Bean_2_Biology và chạy lại.",
                "OK"
            );
            return;
        }

        RectTransform playerRect = nvGo.GetComponent<RectTransform>();
        if (playerRect == null)
        {
            EditorUtility.DisplayDialog(
                "Setup thất bại",
                "'Nv' không có RectTransform.",
                "OK"
            );
            return;
        }

        // ─── 4. Tìm tất cả Enemy và gắn EnemyEncounter ───────────────────────
        string[] enemyNames = { "Enemy1", "Enemy2", "Enemy3", "Enemy4", "Enemy5", "Enemy6" };
        int attached = 0;
        int skipped  = 0;

        foreach (string eName in enemyNames)
        {
            GameObject enemyGo = GameObject.Find(eName);
            if (enemyGo == null)
            {
                Debug.Log($"[Bean2BiologySetup] Không tìm thấy '{eName}' — bỏ qua.");
                continue;
            }

            EnemyEncounter existing = enemyGo.GetComponent<EnemyEncounter>();
            if (existing != null)
            {
                // Script đã tồn tại — chỉ cập nhật playerRect nếu chưa set
                if (existing.playerRect == null)
                    existing.playerRect = playerRect;

                EditorUtility.SetDirty(enemyGo);
                skipped++;
                Debug.Log($"[Bean2BiologySetup] '{eName}' đã có EnemyEncounter — cập nhật playerRect.");
                continue;
            }

            // Gắn component mới
            EnemyEncounter enc = enemyGo.AddComponent<EnemyEncounter>();
            enc.enemyId    = eName;          // Dùng tên làm ID
            enc.playerRect = playerRect;

            EditorUtility.SetDirty(enemyGo);
            attached++;
            Debug.Log($"[Bean2BiologySetup] Đã gắn EnemyEncounter vào '{eName}' (id='{eName}').");
        }

        // ─── 5. Lưu scene ─────────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(
            EditorSceneManager.GetSceneByPath(GAMEPLAY_SCENE_PATH)
        );
        EditorSceneManager.SaveOpenScenes();

        // ─── 6. Báo kết quả ───────────────────────────────────────────────────
        EditorUtility.DisplayDialog(
            "Setup hoàn tất!",
            $"✅ Đã gắn EnemyEncounter: {attached} Enemy\n" +
            $"⏭ Đã có sẵn (cập nhật playerRect): {skipped} Enemy\n\n" +
            $"Scenes đã thêm vào Build Settings:\n" +
            $"• {GAMEPLAY_SCENE_PATH}\n" +
            $"• {QUESTION_SCENE_PATH}",
            "OK"
        );
    }

    // ─── Thêm scene vào Build Settings nếu chưa có ───────────────────────────
    private static void AddScenesToBuildSettings()
    {
        string[] scenePaths = { GAMEPLAY_SCENE_PATH, QUESTION_SCENE_PATH };

        var buildScenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(
            EditorBuildSettings.scenes
        );

        foreach (string path in scenePaths)
        {
            bool alreadyIn = false;
            foreach (var s in buildScenes)
            {
                if (s.path == path)
                {
                    alreadyIn = true;
                    break;
                }
            }

            if (!alreadyIn)
            {
                buildScenes.Add(new EditorBuildSettingsScene(path, true));
                Debug.Log($"[Bean2BiologySetup] Thêm vào Build Settings: {path}");
            }
            else
            {
                // Đảm bảo scene được enable
                for (int i = 0; i < buildScenes.Count; i++)
                {
                    if (buildScenes[i].path == path && !buildScenes[i].enabled)
                    {
                        buildScenes[i] = new EditorBuildSettingsScene(path, true);
                        Debug.Log($"[Bean2BiologySetup] Enable scene trong Build Settings: {path}");
                    }
                }
            }
        }

        EditorBuildSettings.scenes = buildScenes.ToArray();
    }
}
#endif
