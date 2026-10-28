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
/// Công cụ khởi tạo Scene riêng biệt cho Popup Loading (chờ AI chấm): Assets/Scenes/Biology/PopupLoading.unity
/// Chạy từ menu: Tools > Sinh Hoc > Tạo Scene PopupLoading riêng
/// </summary>
public static class PopupLoadingSceneCreator
{
    public const string SCENE_PATH = "Assets/Scenes/Biology/PopupLoading.unity";

    [InitializeOnLoadMethod]
    private static void AutoCreateIfNotExists()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(SCENE_PATH))
            {
                CreatePopupScene();
            }
            else
            {
                EnsureSceneInBuildSettings(SCENE_PATH);
            }
        };
    }

    [MenuItem("Tools/Sinh Hoc/Tạo Scene PopupLoading riêng")]
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
        cam.depth = 12;
        camObj.tag = "Untagged";

        // 3. EventSystem
        GameObject esObj = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        // 4. Canvas
        GameObject canvasObj = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 95; // Hiển thị trên màn chơi TuLuan (0), dưới PopupKetQua (100)

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // Controller gắn trên Canvas
        PopupLoadingController controller = canvasObj.AddComponent<PopupLoadingController>();

        // Load Font tiếng Việt
        TMP_FontAsset fontRegular = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TeacherUI/Fonts/DejaVuSans SDF96.asset");
        TMP_FontAsset fontBold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TeacherUI/Fonts/DejaVuSans-Bold SDF96.asset");
        controller.fontRegular = fontRegular;
        controller.fontBold = fontBold;

        // 5. Background Overlay (Lớp tối che toàn màn hình, chặn tương tác click khi đang chờ)
        GameObject overlayObj = new GameObject("BackgroundOverlay", typeof(RectTransform), typeof(Image));
        overlayObj.transform.SetParent(canvasObj.transform, false);
        RectTransform rtOverlay = overlayObj.GetComponent<RectTransform>();
        rtOverlay.anchorMin = Vector2.zero;
        rtOverlay.anchorMax = Vector2.one;
        rtOverlay.offsetMin = Vector2.zero;
        rtOverlay.offsetMax = Vector2.zero;

        Image imgOverlay = overlayObj.GetComponent<Image>();
        imgOverlay.color = new Color(0f, 0f, 0f, 0.65f);
        imgOverlay.raycastTarget = true;

        // 6. Box Khung thông báo ở giữa
        GameObject boxObj = new GameObject("Box", typeof(RectTransform), typeof(Image));
        boxObj.transform.SetParent(canvasObj.transform, false);
        RectTransform rtBox = boxObj.GetComponent<RectTransform>();
        rtBox.anchorMin = new Vector2(0.5f, 0.5f);
        rtBox.anchorMax = new Vector2(0.5f, 0.5f);
        rtBox.pivot = new Vector2(0.5f, 0.5f);
        rtBox.anchoredPosition = Vector2.zero;
        rtBox.sizeDelta = new Vector2(895f, 330f);

        Image imgBox = boxObj.GetComponent<Image>();
        imgBox.raycastTarget = true;

        Sprite frameSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/SinhTuLuan/khung.png");
        if (frameSprite != null)
        {
            imgBox.sprite = frameSprite;
            imgBox.type = Image.Type.Sliced;
        }
        else
        {
            imgBox.color = new Color(0.14f, 0.10f, 0.08f, 0.95f);
        }

        // 7. Biểu tượng Spinner xoay (Dùng Image sprite thay vì ký tự Text Unicode để không bị lỗi font ô vuông)
        GameObject spinnerObj = new GameObject("Spinner", typeof(RectTransform), typeof(Image));
        spinnerObj.transform.SetParent(boxObj.transform, false);
        RectTransform rtSpinner = spinnerObj.GetComponent<RectTransform>();
        rtSpinner.anchorMin = new Vector2(0.5f, 0.5f);
        rtSpinner.anchorMax = new Vector2(0.5f, 0.5f);
        rtSpinner.pivot = new Vector2(0.5f, 0.5f);
        rtSpinner.anchoredPosition = new Vector2(0, 55);
        rtSpinner.sizeDelta = new Vector2(50, 50);

        Image imgSpin = spinnerObj.GetComponent<Image>();
        Sprite gearSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/TeacherUI/Sprites/Icons/icon_gear.png")
                         ?? AssetDatabase.LoadAssetAtPath<Sprite>("Assets/TeacherUI/Sprites/Icons/icon_refresh.png");
        if (gearSprite != null)
        {
            imgSpin.sprite = gearSprite;
            imgSpin.color = new Color(0.93f, 0.86f, 0.68f, 1f); // Màu vàng kem hài hòa nền gỗ
        }
        else
        {
            imgSpin.color = new Color(1f, 0.85f, 0.2f, 1f);
        }
        imgSpin.raycastTarget = false;
        controller.spinner = rtSpinner;
        controller.spinnerSprite = gearSprite;

        // 8. Text tiêu đề chính: "AI đang chấm bài..."
        GameObject textObj = new GameObject("txtLoading", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(boxObj.transform, false);
        RectTransform rtText = textObj.GetComponent<RectTransform>();
        rtText.anchorMin = new Vector2(0.5f, 0.5f);
        rtText.anchorMax = new Vector2(0.5f, 0.5f);
        rtText.pivot = new Vector2(0.5f, 0.5f);
        rtText.anchoredPosition = new Vector2(0, 7);
        rtText.sizeDelta = new Vector2(700, 45);

        TextMeshProUGUI tmpLoading = textObj.GetComponent<TextMeshProUGUI>();
        if (fontBold != null) tmpLoading.font = fontBold;
        tmpLoading.text = "AI đang chấm bài...";
        tmpLoading.fontSize = 30;
        tmpLoading.fontStyle = FontStyles.Bold;
        tmpLoading.alignment = TextAlignmentOptions.Center;
        tmpLoading.color = new Color(0.24f, 0.16f, 0.10f, 1f); // Màu nâu đậm sắc nét trên nền giấy sáng
        tmpLoading.raycastTarget = false;
        controller.txtLoading = tmpLoading;

        // 9. Text phụ chú: "Vui lòng chờ trong giây lát..."
        GameObject subObj = new GameObject("txtSub", typeof(RectTransform), typeof(TextMeshProUGUI));
        subObj.transform.SetParent(boxObj.transform, false);
        RectTransform rtSub = subObj.GetComponent<RectTransform>();
        rtSub.anchorMin = new Vector2(0.5f, 0.5f);
        rtSub.anchorMax = new Vector2(0.5f, 0.5f);
        rtSub.pivot = new Vector2(0.5f, 0.5f);
        rtSub.anchoredPosition = new Vector2(0, -28);
        rtSub.sizeDelta = new Vector2(700, 35);

        TextMeshProUGUI tmpSub = subObj.GetComponent<TextMeshProUGUI>();
        if (fontBold != null) tmpSub.font = fontBold;
        else if (fontRegular != null) tmpSub.font = fontRegular;
        tmpSub.text = "Vui lòng chờ trong giây lát...";
        tmpSub.fontSize = 20;
        tmpSub.fontStyle = FontStyles.Normal; // Normal để không lỗi dấu tiếng Việt
        tmpSub.alignment = TextAlignmentOptions.Center;
        tmpSub.color = new Color(0.35f, 0.25f, 0.18f, 0.9f); // Màu nâu vừa
        tmpSub.raycastTarget = false;
        controller.txtSub = tmpSub;

        // 10. Lưu Scene
        Directory.CreateDirectory(Path.GetDirectoryName(SCENE_PATH));
        EditorSceneManager.SaveScene(scene, SCENE_PATH);
        Debug.Log("[PopupLoadingSceneCreator] Đã tạo thành công Scene: " + SCENE_PATH);

        // 11. Thêm Scene vào Build Settings
        EnsureSceneInBuildSettings(SCENE_PATH);

        if (!Application.isBatchMode)
        {
            EditorUtility.DisplayDialog(
                "Tạo Scene thành công",
                $"Đã tạo thành công Scene Popup Chờ AI chấm riêng biệt tại:\n{SCENE_PATH}\n\n" +
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
        Debug.Log("[PopupLoadingSceneCreator] Đã thêm " + scenePath + " vào Build Settings.");
    }
}
#endif
