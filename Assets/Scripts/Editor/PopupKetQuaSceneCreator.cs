#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Công cụ khởi tạo Scene riêng biệt cho Popup Kết quả: Assets/Scenes/Biology/PopupKetQua.unity
/// Chạy từ menu: Tools > Sinh Hoc > Tạo Scene PopupKetQua riêng
/// </summary>
public static class PopupKetQuaSceneCreator
{
    public const string SCENE_PATH = "Assets/Scenes/Biology/PopupKetQua.unity";

    [InitializeOnLoadMethod]
    private static void AutoCreateIfNotExists()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(SCENE_PATH))
            {
                CreatePopupScene();
            }
        };
    }

    [MenuItem("Tools/Sinh Hoc/Tạo Scene PopupKetQua riêng")]
    public static void CreatePopupScene()
    {
        // 1. Tạo scene mới
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 2. Camera UI
        GameObject camObj = new GameObject("Main Camera", typeof(Camera));
        Camera cam = camObj.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Depth;
        cam.cullingMask = 1 << 5; // UI layer
        cam.orthographic = true;
        cam.depth = 10;
        camObj.tag = "MainCamera";

        // 3. EventSystem
        GameObject esObj = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        // 4. Canvas
        GameObject canvasObj = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // Hiển thị trên cùng khi load additive

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // Controller gắn trên Canvas
        PopupKetQuaController controller = canvasObj.AddComponent<PopupKetQuaController>();

        // 5. Background Overlay (Lớp tối che màn hình)
        GameObject overlayObj = new GameObject("BackgroundOverlay", typeof(RectTransform), typeof(Image));
        overlayObj.transform.SetParent(canvasObj.transform, false);
        RectTransform rtOverlay = overlayObj.GetComponent<RectTransform>();
        rtOverlay.anchorMin = Vector2.zero;
        rtOverlay.anchorMax = Vector2.one;
        rtOverlay.offsetMin = Vector2.zero;
        rtOverlay.offsetMax = Vector2.zero;

        Image imgOverlay = overlayObj.GetComponent<Image>();
        imgOverlay.color = new Color(0f, 0f, 0f, 0.75f);
        imgOverlay.raycastTarget = true;

        // 6. Frame Popup chính (Khung hình gỗ fantasy)
        GameObject frameObj = new GameObject("Frame", typeof(RectTransform), typeof(Image));
        frameObj.transform.SetParent(canvasObj.transform, false);
        RectTransform rtFrame = frameObj.GetComponent<RectTransform>();
        rtFrame.anchorMin = new Vector2(0.5f, 0.5f);
        rtFrame.anchorMax = new Vector2(0.5f, 0.5f);
        rtFrame.pivot = new Vector2(0.5f, 0.5f);
        rtFrame.anchoredPosition = Vector2.zero;
        rtFrame.sizeDelta = new Vector2(860, 620);

        Image imgFrame = frameObj.GetComponent<Image>();
        imgFrame.raycastTarget = true;

        // Gán sprite khung.png nếu có
        Sprite frameSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/SinhTuLuan/khung.png");
        if (frameSprite != null)
        {
            imgFrame.sprite = frameSprite;
        }

        // 7. Tiêu đề
        GameObject tieuDeObj = new GameObject("TieuDe", typeof(RectTransform), typeof(TextMeshProUGUI));
        tieuDeObj.transform.SetParent(frameObj.transform, false);
        RectTransform rtTieuDe = tieuDeObj.GetComponent<RectTransform>();
        rtTieuDe.anchorMin = new Vector2(0.5f, 1f);
        rtTieuDe.anchorMax = new Vector2(0.5f, 1f);
        rtTieuDe.pivot = new Vector2(0.5f, 1f);
        rtTieuDe.anchoredPosition = new Vector2(0, -45);
        rtTieuDe.sizeDelta = new Vector2(600, 60);

        TextMeshProUGUI tmpTieuDe = tieuDeObj.GetComponent<TextMeshProUGUI>();
        tmpTieuDe.text = "KẾT QUẢ CHẤM BÀI";
        tmpTieuDe.fontSize = 42;
        tmpTieuDe.fontStyle = FontStyles.Bold;
        tmpTieuDe.alignment = TextAlignmentOptions.Center;
        tmpTieuDe.color = Color.black;
        controller.txtTieuDe = tmpTieuDe;

        // 8. txtKetQua ("ĐẠT" / "CHƯA ĐẠT")
        GameObject kqObj = new GameObject("txtKetQua", typeof(RectTransform), typeof(TextMeshProUGUI));
        kqObj.transform.SetParent(frameObj.transform, false);
        RectTransform rtKq = kqObj.GetComponent<RectTransform>();
        rtKq.anchorMin = new Vector2(0.5f, 0.5f);
        rtKq.anchorMax = new Vector2(0.5f, 0.5f);
        rtKq.pivot = new Vector2(0.5f, 0.5f);
        rtKq.anchoredPosition = new Vector2(0, 110);
        rtKq.sizeDelta = new Vector2(400, 50);

        TextMeshProUGUI tmpKq = kqObj.GetComponent<TextMeshProUGUI>();
        tmpKq.text = "ĐẠT";
        tmpKq.fontSize = 36;
        tmpKq.fontStyle = FontStyles.Bold;
        tmpKq.alignment = TextAlignmentOptions.Center;
        tmpKq.color = new Color(0.12f, 0.75f, 0.2f);
        controller.txtKetQua = tmpKq;

        // 9. txtDiem
        GameObject diemObj = new GameObject("txtDiem", typeof(RectTransform), typeof(TextMeshProUGUI));
        diemObj.transform.SetParent(frameObj.transform, false);
        RectTransform rtDiem = diemObj.GetComponent<RectTransform>();
        rtDiem.anchorMin = new Vector2(0.5f, 0.5f);
        rtDiem.anchorMax = new Vector2(0.5f, 0.5f);
        rtDiem.pivot = new Vector2(0.5f, 0.5f);
        rtDiem.anchoredPosition = new Vector2(0, 50);
        rtDiem.sizeDelta = new Vector2(500, 50);

        TextMeshProUGUI tmpDiem = diemObj.GetComponent<TextMeshProUGUI>();
        tmpDiem.text = "Điểm số: 8.5 / 10";
        tmpDiem.fontSize = 34;
        tmpDiem.fontStyle = FontStyles.Bold;
        tmpDiem.alignment = TextAlignmentOptions.Center;
        tmpDiem.color = Color.black;
        controller.txtDiem = tmpDiem;

        // 10. txtNhanXet
        GameObject nxObj = new GameObject("txtNhanXet", typeof(RectTransform), typeof(TextMeshProUGUI));
        nxObj.transform.SetParent(frameObj.transform, false);
        RectTransform rtNx = nxObj.GetComponent<RectTransform>();
        rtNx.anchorMin = new Vector2(0.5f, 0.5f);
        rtNx.anchorMax = new Vector2(0.5f, 0.5f);
        rtNx.pivot = new Vector2(0.5f, 0.5f);
        rtNx.anchoredPosition = new Vector2(0, -35);
        rtNx.sizeDelta = new Vector2(720, 110);

        TextMeshProUGUI tmpNx = nxObj.GetComponent<TextMeshProUGUI>();
        tmpNx.text = "Bài làm phân tích đúng trọng tâm, trình bày rõ ràng các nội dung chính của câu hỏi.";
        tmpNx.fontSize = 26;
        tmpNx.alignment = TextAlignmentOptions.Center;
        tmpNx.color = Color.black;
        tmpNx.textWrappingMode = TextWrappingModes.Normal;
        controller.txtNhanXet = tmpNx;

        // 11. txtMockNotice
        GameObject mockObj = new GameObject("txtMockNotice", typeof(RectTransform), typeof(TextMeshProUGUI));
        mockObj.transform.SetParent(frameObj.transform, false);
        RectTransform rtMock = mockObj.GetComponent<RectTransform>();
        rtMock.anchorMin = new Vector2(0.5f, 0.5f);
        rtMock.anchorMax = new Vector2(0.5f, 0.5f);
        rtMock.pivot = new Vector2(0.5f, 0.5f);
        rtMock.anchoredPosition = new Vector2(0, -115);
        rtMock.sizeDelta = new Vector2(720, 35);

        TextMeshProUGUI tmpMock = mockObj.GetComponent<TextMeshProUGUI>();
        tmpMock.text = "[KẾT QUẢ THỬ NGHIỆM MOCK - CHƯA KẾT NỐI AI CHẤM THẬT]";
        tmpMock.fontSize = 20;
        tmpMock.fontStyle = FontStyles.Italic;
        tmpMock.alignment = TextAlignmentOptions.Center;
        tmpMock.color = new Color(0.4f, 0.4f, 0.4f, 0.85f);
        controller.txtMockNotice = tmpMock;

        // 12. btnDong
        GameObject btnObj = new GameObject("btnDong", typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(frameObj.transform, false);
        RectTransform rtBtn = btnObj.GetComponent<RectTransform>();
        rtBtn.anchorMin = new Vector2(0.5f, 0.5f);
        rtBtn.anchorMax = new Vector2(0.5f, 0.5f);
        rtBtn.pivot = new Vector2(0.5f, 0.5f);
        rtBtn.anchoredPosition = new Vector2(0, -195);
        rtBtn.sizeDelta = new Vector2(240, 70);

        Image imgBtn = btnObj.GetComponent<Image>();
        imgBtn.color = new Color(0.85f, 0.68f, 0.42f, 1f); // Nâu vàng gỗ ấm áp

        Button btn = btnObj.GetComponent<Button>();
        btn.targetGraphic = imgBtn;
        controller.btnDong = btn;

        // Text bên trong nút Đóng
        GameObject btnTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        btnTextObj.transform.SetParent(btnObj.transform, false);
        RectTransform rtBtnText = btnTextObj.GetComponent<RectTransform>();
        rtBtnText.anchorMin = Vector2.zero;
        rtBtnText.anchorMax = Vector2.one;
        rtBtnText.offsetMin = Vector2.zero;
        rtBtnText.offsetMax = Vector2.zero;

        TextMeshProUGUI tmpBtnText = btnTextObj.GetComponent<TextMeshProUGUI>();
        tmpBtnText.text = "Đóng";
        tmpBtnText.fontSize = 32;
        tmpBtnText.fontStyle = FontStyles.Bold;
        tmpBtnText.alignment = TextAlignmentOptions.Center;
        tmpBtnText.color = Color.black;

        // 13. Lưu Scene
        Directory.CreateDirectory(Path.GetDirectoryName(SCENE_PATH));
        EditorSceneManager.SaveScene(scene, SCENE_PATH);
        Debug.Log("[PopupKetQuaSceneCreator] Đã tạo thành công Scene: " + SCENE_PATH);

        // 14. Thêm Scene vào Build Settings
        EnsureSceneInBuildSettings(SCENE_PATH);

        if (!Application.isBatchMode)
        {
            EditorUtility.DisplayDialog(
                "Tạo Scene thành công",
                $"Đã tạo thành công Scene Popup Kết quả riêng biệt tại:\n{SCENE_PATH}\n\n" +
                "Scene đã được thêm vào Build Settings.\n" +
                "Bạn có thể mở scene này trong Unity bất kỳ lúc nào để căn chỉnh trực quan!",
                "OK"
            );
        }
    }

    public static void EnsureSceneInBuildSettings(string scenePath)
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

        EditorBuildSettingsScene[] newScenes = new EditorBuildSettingsScene[scenes.Length + 1];
        for (int i = 0; i < scenes.Length; i++)
        {
            newScenes[i] = scenes[i];
        }
        newScenes[newScenes.Length - 1] = new EditorBuildSettingsScene(scenePath, true);
        EditorBuildSettings.scenes = newScenes;
        Debug.Log("[PopupKetQuaSceneCreator] Đã thêm " + scenePath + " vào Build Settings.");
    }
}
#endif
