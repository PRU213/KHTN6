#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Công cụ thiết lập tự động cho Scene TuLuan.
/// Chạy từ menu: Tools > Sinh Hoc > Setup TuLuan Scene
/// </summary>
public static class TuLuanSetupTool
{
    private const string TULUAN_SCENE_PATH = "Assets/Scenes/Biology/TuLuan.unity";

    [MenuItem("Tools/Sinh Hoc/Setup TuLuan Scene")]
    public static void SetupTuLuanScene()
    {
        // 1. Thêm TuLuan vào Build Settings nếu chưa có
        EnsureSceneInBuildSettings(TULUAN_SCENE_PATH);

        // 2. Mở Scene TuLuan
        bool sceneWasOpen = false;
        for (int i = 0; i < EditorSceneManager.sceneCount; i++)
        {
            if (EditorSceneManager.GetSceneAt(i).path == TULUAN_SCENE_PATH)
            {
                sceneWasOpen = true;
                break;
            }
        }

        if (!sceneWasOpen)
        {
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            EditorSceneManager.OpenScene(TULUAN_SCENE_PATH, OpenSceneMode.Single);
        }

        // 3. Tìm Canvas
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy Canvas trong scene TuLuan!", "OK");
            }
            else
            {
                Debug.LogError("[TuLuanSetupTool] Không tìm thấy Canvas trong scene TuLuan!");
            }
            return;
        }

        // 4. Thêm / lấy TuLuanController trên Canvas
        TuLuanController controller = canvas.GetComponent<TuLuanController>();
        if (controller == null)
        {
            controller = canvas.gameObject.AddComponent<TuLuanController>();
            Undo.RegisterCreatedObjectUndo(controller, "Add TuLuanController");
        }

        // 5. Chỉ liên kết component sẵn có, KHÔNG tự cấu hình lại TMP/ScrollRect.
        // Ô nhập chưa có TMP_InputField sẽ được bổ sung RuntimeInput khi Play.
        controller.AutoBindUI();

        // 6. Cấu hình btnTaiAnh và Anh trong KhungAnh
        Transform khAnh = canvas.transform.Find("KhungAnh");
        if (khAnh != null)
        {
            Transform tBtn = khAnh.Find("btnTaiAnh");
            if (tBtn != null)
            {
                Button btn = tBtn.GetComponent<Button>();
                if (btn == null) btn = tBtn.gameObject.AddComponent<Button>();
                btn.targetGraphic = tBtn.GetComponent<Image>();
                controller.btnTaiAnh = btn;
                EditorUtility.SetDirty(btn);
            }

            // Đồng bộ tên GameObject ảnh: hỗ trợ cả "Anh" và "btnTai"
            Transform tAnh = khAnh.Find("Anh");
            if (tAnh == null) tAnh = khAnh.Find("btnTai");
            if (tAnh != null)
            {
                tAnh.name = "Anh"; // Đổi tên cho khớp chuẩn Hierarchy đề bài
                Image img = tAnh.GetComponent<Image>();
                if (img != null)
                {
                    img.preserveAspect = true;
                    controller.imgAnh = img;
                    EditorUtility.SetDirty(img);
                }
                EditorUtility.SetDirty(tAnh.gameObject);
            }
        }

        // 7. Cấu hình btnNopBai
        Transform tNop = canvas.transform.Find("btnNopBai");
        if (tNop != null)
        {
            Button btn = tNop.GetComponent<Button>();
            if (btn == null) btn = tNop.gameObject.AddComponent<Button>();
            btn.targetGraphic = tNop.GetComponent<Image>();
            controller.btnNopBai = btn;
            EditorUtility.SetDirty(btn);
        }

        // 7b. Cấu hình nút Tạm Dừng btnTamDung
        controller.SetupPauseButton(canvas);
        Transform tPause = canvas.transform.Find("btnTamDung");
        if (tPause != null)
        {
            Button btnP = tPause.GetComponent<Button>();
            controller.btnTamDung = btnP;
            EditorUtility.SetDirty(tPause.gameObject);
            if (btnP != null) EditorUtility.SetDirty(btnP);
        }

        // 8. Không gọi AutoBindUI lần hai và không tái cấu trúc TMP lúc Edit/Play.

        // 9. Lưu scene
        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(canvas.gameObject);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        EditorSceneManager.SaveScene(canvas.gameObject.scene);

        Debug.Log("[TuLuanSetupTool] Thiết lập hoàn tất cho Scene TuLuan!");
        if (!Application.isBatchMode)
        {
            EditorUtility.DisplayDialog(
                "Setup hoàn tất",
                "Đã cấu hình thành công các thành phần cho Scene TuLuan:\n\n" +
                "✓ Đã thêm TuLuanController vào Canvas\n" +
                "✓ Đã giữ nguyên cấu trúc TMP và ScrollRect hiện tại\n" +
                "✓ Ô nhập được liên kết an toàn khi Play\n" +
                "✓ Chữ đề bài, bài làm và câu trả lời màu đen\n" +
                "✓ Con trỏ nhấp nháy màu đen sắc nét (4px)\n" +
                "✓ Đã kết nối btnTaiAnh & Image Anh (giữ tỷ lệ preserveAspect)\n" +
                "✓ Đã kết nối btnNopBai\n" +
                "✓ Đã thêm nút Tạm Dừng (btnTamDung) góc trên bên phải\n" +
                "✓ Đã thêm Popup kết quả (Mock Result kiểm thử)\n" +
                "✓ Đã thêm TuLuan vào Build Settings",
                "OK"
            );
        }
    }

    private static void EnsureSceneInBuildSettings(string scenePath)
    {
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        for (int i = 0; i < scenes.Length; i++)
        {
            if (scenes[i].path == scenePath)
            {
                if (!scenes[i].enabled)
                {
                    scenes[i].enabled = true;
                    EditorBuildSettings.scenes = scenes;
                }
                return;
            }
        }

        // Thêm mới
        EditorBuildSettingsScene[] newScenes = new EditorBuildSettingsScene[scenes.Length + 1];
        for (int i = 0; i < scenes.Length; i++)
        {
            newScenes[i] = scenes[i];
        }
        newScenes[newScenes.Length - 1] = new EditorBuildSettingsScene(scenePath, true);
        EditorBuildSettings.scenes = newScenes;
        Debug.Log("[TuLuanSetupTool] Đã thêm " + scenePath + " vào Build Settings.");
    }
}
#endif
