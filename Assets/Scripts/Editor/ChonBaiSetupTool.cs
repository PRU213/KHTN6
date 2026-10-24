#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Công cụ thiết lập và khôi phục tự động chuẩn 100% cho màn Chọn Bài (ChonBai.unity).
/// Chạy từ menu: Tools > Sinh Hoc > Setup ChonBai Scene
/// </summary>
public static class ChonBaiSetupTool
{
    public const string CHONBAI_SCENE_PATH = "Assets/Scenes/Biology/ChonBai.unity";
    public const string WEBAPP_URL = "https://script.google.com/macros/s/AKfycbxxE2R2ZoitgM647aQqnebUcG90lhIlodU0DcyiaZuKkLaVWl6oopI-TkeNM8_KKDWhUw/exec";
    public const string SHEET_CSV_URL = "https://docs.google.com/spreadsheets/d/1DcJwlN_fUDdcaQ6IfnlkMJApgWuEUqMhyZv9Kn8vfWo/export?format=csv&gid=1463734910";

    [MenuItem("Tools/Sinh Hoc/Setup ChonBai Scene")]
    [MenuItem("Tools/Setup ChonBai Scene")]
    public static void SetupChonBaiScene()
    {
        Debug.Log("[ChonBaiSetupTool] Bắt đầu thiết lập màn ChonBai...");

        // 1. Đảm bảo Scene ChonBai có trong Build Settings
        EnsureSceneInBuildSettings(CHONBAI_SCENE_PATH);

        // 2. Mở Scene ChonBai nếu chưa mở
        Scene scene = OpenChonBaiScene();
        if (!scene.IsValid())
        {
            Debug.LogError($"[ChonBaiSetupTool] Không thể mở scene tại: {CHONBAI_SCENE_PATH}");
            return;
        }

        // 3. Tìm Canvas chính của scene
        Canvas canvas = FindTargetCanvas(scene);
        if (canvas == null)
        {
            ShowDialogOrLog("Lỗi", "Không tìm thấy Canvas trong scene ChonBai!", true);
            return;
        }

        // Đảm bảo cấu hình Canvas
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>() ?? canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>() ?? canvas.gameObject.AddComponent<GraphicRaycaster>();
        raycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;

        // 4. Lấy / Gắn ChonBaiManager trên Canvas
        ChonBaiManager manager = canvas.GetComponent<ChonBaiManager>();
        if (manager == null)
        {
            manager = canvas.gameObject.AddComponent<ChonBaiManager>();
            Undo.RegisterCreatedObjectUndo(manager, "Add ChonBaiManager");
        }

        manager.monHocFilter = "Sinh học";
        manager.webAppUrl = WEBAPP_URL;
        manager.sheetCSVUrl = SHEET_CSV_URL;

        // 5. Quét và cấu hình các nút và thành phần UI trong Canvas
        Transform[] allTransforms = canvas.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in allTransforms)
        {
            string n = t.name;

            if (n == "List")
            {
                manager.listContainer = t;
                SetupListScrollView(t, canvas);
            }
            else if (n == "BatDau")
            {
                manager.batDauButton = EnsureButton(t);
            }
            else if (n == "De")
            {
                manager.btnDe = EnsureButton(t);
            }
            else if (n == "TrungBinh")
            {
                manager.btnTrungBinh = EnsureButton(t);
            }
            else if (n == "Kho")
            {
                manager.btnKho = EnsureButton(t);
            }
            else if (n == "TongHop")
            {
                manager.btnTongHop = EnsureButton(t);
            }
            else if (n == "TracNghiem")
            {
                manager.btnTracNghiem = EnsureButton(t);
            }
            else if (n == "TuLuan")
            {
                manager.btnTuLuan = EnsureButton(t);
            }
            else if (n == "HomePage")
            {
                manager.btnHomePage = EnsureButton(t);
            }
            else if (n == "ChonDang" || n == "KhungChonLoai" || n == "KhungDoKho" || n == "KhungChonBai" || n == "ChonChuong" || n == "ChonDoKho" || n == "Background")
            {
                // Tắt raycastTarget trên các khung nền trang trí để không bao giờ chắn click
                Image img = t.GetComponent<Image>();
                if (img != null)
                {
                    img.raycastTarget = false;
                    EditorUtility.SetDirty(img);
                }
            }
        }

        // 6. Đảm bảo EventSystem đầy đủ
        EnsureEventSystem(scene);

        // 7. Đánh dấu lưu Scene
        EditorUtility.SetDirty(manager);
        EditorUtility.SetDirty(canvas.gameObject);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[ChonBaiSetupTool] Thiết lập thành công và đã lưu Scene ChonBai!");
        ShowDialogOrLog("Hoàn tất", "Đã thiết lập chuẩn toàn bộ màn Chọn Bài (ChonBai.unity) thành công!\n\n- Các nút Bắt đầu, Độ khó, Dạng bài, Trang chủ đã được liên kết.\n- Khung cuộn danh sách (List, Viewport, Content) đã được cấu hình chuẩn RectMask2D.\n- Raycast của các khung trang trí đã được tối ưu chống chắn chuột.", false);
    }

    private static Button EnsureButton(Transform t)
    {
        Button btn = t.GetComponent<Button>();
        if (btn == null)
        {
            btn = t.gameObject.AddComponent<Button>();
            Undo.RegisterCreatedObjectUndo(btn, "Add Button " + t.name);
        }

        Image img = t.GetComponent<Image>();
        if (img != null)
        {
            img.enabled = true;
            img.raycastTarget = true;
            btn.targetGraphic = img;
            EditorUtility.SetDirty(img);
        }

        btn.interactable = true;
        btn.transition = Selectable.Transition.None;
        Navigation nav = btn.navigation;
        nav.mode = Navigation.Mode.None;
        btn.navigation = nav;

        EditorUtility.SetDirty(btn);
        return btn;
    }

    private static void SetupListScrollView(Transform listContainer, Canvas parentCanvas)
    {
        // Tắt raycastTarget trên ảnh nền của List
        Image listImg = listContainer.GetComponent<Image>();
        if (listImg != null)
        {
            listImg.raycastTarget = false;
            EditorUtility.SetDirty(listImg);
        }

        // Cấu hình ScrollRect
        ScrollRect scrollRect = listContainer.GetComponent<ScrollRect>();
        if (scrollRect == null)
        {
            scrollRect = listContainer.gameObject.AddComponent<ScrollRect>();
            Undo.RegisterCreatedObjectUndo(scrollRect, "Add ScrollRect");
        }

        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 40f;
        scrollRect.horizontalScrollbar = null;
        scrollRect.verticalScrollbar = null;
        scrollRect.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

        // Vô hiệu hóa các thanh scrollbar hỏng có sẵn trong scene
        Transform sbV = listContainer.Find("Scrollbar Vertical");
        if (sbV != null)
        {
            sbV.gameObject.SetActive(false);
            EditorUtility.SetDirty(sbV.gameObject);
        }
        Transform sbH = listContainer.Find("Scrollbar Horizontal");
        if (sbH != null)
        {
            sbH.gameObject.SetActive(false);
            EditorUtility.SetDirty(sbH.gameObject);
        }

        // Cấu hình Viewport
        Transform vpTransform = listContainer.Find("Viewport");
        if (vpTransform == null)
        {
            GameObject vpObj = new GameObject("Viewport", typeof(RectTransform));
            vpObj.transform.SetParent(listContainer, false);
            vpTransform = vpObj.transform;
            Undo.RegisterCreatedObjectUndo(vpObj, "Create Viewport");
        }

        RectTransform vpRt = vpTransform.GetComponent<RectTransform>();
        vpRt.anchorMin = Vector2.zero;
        vpRt.anchorMax = Vector2.one;
        vpRt.offsetMin = Vector2.zero;
        vpRt.offsetMax = Vector2.zero;
        vpRt.anchoredPosition = Vector2.zero;
        vpRt.sizeDelta = Vector2.zero;
        vpRt.pivot = new Vector2(0.5f, 0.5f);

        // Thay Mask bằng RectMask2D
        Mask oldMask = vpTransform.GetComponent<Mask>();
        if (oldMask != null)
        {
            Object.DestroyImmediate(oldMask);
        }

        RectMask2D rectMask = vpTransform.GetComponent<RectMask2D>();
        if (rectMask == null)
        {
            rectMask = vpTransform.gameObject.AddComponent<RectMask2D>();
            Undo.RegisterCreatedObjectUndo(rectMask, "Add RectMask2D");
        }
        rectMask.enabled = true;

        Image vpImg = vpTransform.GetComponent<Image>();
        if (vpImg != null)
        {
            vpImg.enabled = false;
            vpImg.raycastTarget = false;
            EditorUtility.SetDirty(vpImg);
        }

        scrollRect.viewport = vpRt;

        // Cấu hình Content
        Transform ct = vpTransform.Find("Content");
        if (ct == null) ct = listContainer.Find("Content");
        if (ct == null)
        {
            GameObject contentObj = new GameObject("Content", typeof(RectTransform));
            contentObj.transform.SetParent(vpTransform, false);
            ct = contentObj.transform;
            Undo.RegisterCreatedObjectUndo(contentObj, "Create Content");
        }
        else
        {
            ct.SetParent(vpTransform, false);
        }

        RectTransform cRt = ct.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0, 1);
        cRt.anchorMax = new Vector2(1, 1);
        cRt.pivot = new Vector2(0.5f, 1f);
        cRt.anchoredPosition = Vector2.zero;
        cRt.sizeDelta = new Vector2(0, 0);

        scrollRect.content = cRt;

        VerticalLayoutGroup vlg = ct.GetComponent<VerticalLayoutGroup>();
        if (vlg == null)
        {
            vlg = ct.gameObject.AddComponent<VerticalLayoutGroup>();
            Undo.RegisterCreatedObjectUndo(vlg, "Add VerticalLayoutGroup");
        }
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 12;
        vlg.padding = new RectOffset(10, 10, 10, 10);

        ContentSizeFitter csf = ct.GetComponent<ContentSizeFitter>();
        if (csf == null)
        {
            csf = ct.gameObject.AddComponent<ContentSizeFitter>();
            Undo.RegisterCreatedObjectUndo(csf, "Add ContentSizeFitter");
        }
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        EditorUtility.SetDirty(scrollRect);
        EditorUtility.SetDirty(vpTransform.gameObject);
        EditorUtility.SetDirty(ct.gameObject);
    }

    private static void EnsureEventSystem(Scene scene)
    {
        EventSystem es = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            es = root.GetComponentInChildren<EventSystem>(true);
            if (es != null) break;
        }

        if (es == null)
        {
            GameObject esObj = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            Undo.RegisterCreatedObjectUndo(esObj, "Create EventSystem");
            es = esObj.GetComponent<EventSystem>();
            EditorUtility.SetDirty(esObj);
        }

        InputSystemUIInputModule inputMod = es.GetComponent<InputSystemUIInputModule>();
        if (inputMod == null && es.GetComponent<StandaloneInputModule>() == null)
        {
            inputMod = es.gameObject.AddComponent<InputSystemUIInputModule>();
            EditorUtility.SetDirty(es.gameObject);
        }

        if (inputMod != null && inputMod.actionsAsset == null)
        {
            try
            {
                inputMod.AssignDefaultActions();
                EditorUtility.SetDirty(inputMod);
            }
            catch { }
        }
    }

    private static Canvas FindTargetCanvas(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Canvas[] canvases = root.GetComponentsInChildren<Canvas>(true);
            foreach (Canvas c in canvases)
            {
                if (c.name != "PersistentTimerCanvas" && c.name != "PersistentPauseCanvas")
                {
                    return c;
                }
            }
        }
        return null;
    }

    private static Scene OpenChonBaiScene()
    {
        for (int i = 0; i < EditorSceneManager.sceneCount; i++)
        {
            Scene s = EditorSceneManager.GetSceneAt(i);
            if (s.path == CHONBAI_SCENE_PATH)
            {
                return s;
            }
        }

        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        return EditorSceneManager.OpenScene(CHONBAI_SCENE_PATH, OpenSceneMode.Single);
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

        EditorBuildSettingsScene[] newScenes = new EditorBuildSettingsScene[scenes.Length + 1];
        scenes.CopyTo(newScenes, 0);
        newScenes[newScenes.Length - 1] = new EditorBuildSettingsScene(scenePath, true);
        EditorBuildSettings.scenes = newScenes;
        Debug.Log($"[ChonBaiSetupTool] Đã thêm {scenePath} vào EditorBuildSettings.");
    }

    private static void ShowDialogOrLog(string title, string message, bool isError)
    {
        if (Application.isBatchMode)
        {
            if (isError) Debug.LogError($"[{title}] {message}");
            else Debug.Log($"[{title}] {message}");
        }
        else
        {
            EditorUtility.DisplayDialog(title, message, "OK");
        }
    }
}
#endif
