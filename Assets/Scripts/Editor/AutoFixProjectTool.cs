using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class AutoFixProjectTool : EditorWindow
{
    [MenuItem("Tools/Tự động Cập nhật UI (Fix Lỗi & Thêm Role)")]
    public static void RunAutoFix()
    {
        int fixedPauseButtons = FixPauseButtons();
        int fixedHomePage = FixHomePageUI();

        if (fixedPauseButtons == 0 && fixedHomePage == 0)
        {
            Debug.Log("<color=yellow>Không tìm thấy phần tử nào cần cập nhật trong Scene hiện tại. Hãy chắc chắn bạn đang mở đúng Scene (HomePage hoặc beantong).</color>");
        }
        else
        {
            Debug.Log($"<color=green>Hoàn tất cập nhật! Đã sửa {fixedPauseButtons} nút Tạm Dừng và {fixedHomePage} màn hình HomePage.</color>");
        }
    }

    private static int FixPauseButtons()
    {
        int count = 0;
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        
        foreach (GameObject btn in allObjects)
        {
            if (btn.scene.isLoaded && btn.name.Contains("TamDung") && !EditorUtility.IsPersistent(btn))
            {
                Undo.RecordObject(btn, "Fix Pause Button");

                Canvas canvas = btn.GetComponentInParent<Canvas>();
                
                // 1. Đảm bảo nằm trong Canvas Overlay
                if (canvas == null)
                {
                    GameObject canvasGo = new GameObject("PauseCanvas");
                    Undo.RegisterCreatedObjectUndo(canvasGo, "Create Pause Canvas");
                    
                    canvas = canvasGo.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvas.sortingOrder = 30000; // Đảm bảo nổi lên trên
                    
                    canvasGo.AddComponent<CanvasScaler>();
                    canvasGo.AddComponent<GraphicRaycaster>();
                    
                    Undo.SetTransformParent(btn.transform, canvasGo.transform, "Move Button to Canvas");
                }
                else
                {
                    Undo.RecordObject(canvas, "Fix Canvas Mode");
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvas.sortingOrder = 30000;
                }

                // Đưa Canvas chứa nút xuống dưới cùng để vẽ sau cùng (nổi lên trên)
                canvas.transform.SetAsLastSibling();

                // 2. Đổi SpriteRenderer thành Image nếu cần
                SpriteRenderer sr = btn.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    Image img = btn.GetComponent<Image>();
                    if (img == null) img = Undo.AddComponent<Image>(btn);
                    img.sprite = sr.sprite;
                    img.color = sr.color;
                    img.SetNativeSize();
                    Undo.DestroyObjectImmediate(sr);
                }

                // 3. Ép góc trên phải
                RectTransform rt = btn.GetComponent<RectTransform>();
                if (rt == null) rt = Undo.AddComponent<RectTransform>(btn);
                
                rt.anchorMin = new Vector2(1, 1);
                rt.anchorMax = new Vector2(1, 1);
                rt.pivot = new Vector2(1, 1);
                rt.anchoredPosition = new Vector2(-50, -50);

                EditorUtility.SetDirty(btn);
                count++;
            }
        }
        return count;
    }

    private static int FixHomePageUI()
    {
        int count = 0;
        HomeMenuController homeController = Object.FindAnyObjectByType<HomeMenuController>();
        if (homeController == null) return 0;

        GameObject txtNameObj = GameObject.Find("txtName");
        if (txtNameObj != null && homeController.txtName == null)
        {
            Undo.RecordObject(homeController, "Assign txtName");
            homeController.txtName = txtNameObj.GetComponent<TextMeshProUGUI>();
            EditorUtility.SetDirty(homeController);
            count++;
        }

        GameObject roleObj = GameObject.Find("role");
        if (roleObj == null) roleObj = GameObject.Find("txtRole");

        // Nếu người khác chưa có object hiển thị Role, tự động tạo bằng cách duplicate txtName
        if (roleObj == null && txtNameObj != null)
        {
            roleObj = Instantiate(txtNameObj, txtNameObj.transform.parent);
            roleObj.name = "role"; // Đặt tên là role cho chuẩn với code hiện tại
            Undo.RegisterCreatedObjectUndo(roleObj, "Create Role UI");

            RectTransform roleRt = roleObj.GetComponent<RectTransform>();
            if (roleRt != null)
            {
                // Dịch xuống dưới 50 pixel so với tên
                roleRt.anchoredPosition = new Vector2(roleRt.anchoredPosition.x, roleRt.anchoredPosition.y - 50f);
            }
            
            TextMeshProUGUI roleText = roleObj.GetComponent<TextMeshProUGUI>();
            if (roleText != null)
            {
                roleText.text = "Role";
                roleText.color = Color.white;
                roleText.outlineWidth = 0f;
                roleText.alignment = TextAlignmentOptions.Center;
                roleText.horizontalAlignment = HorizontalAlignmentOptions.Center;
                roleText.verticalAlignment = VerticalAlignmentOptions.Middle;
            }
            count++;
        }

        if (roleObj != null && homeController.txtRole == null)
        {
            Undo.RecordObject(homeController, "Assign txtRole");
            homeController.txtRole = roleObj.GetComponent<TextMeshProUGUI>();
            EditorUtility.SetDirty(homeController);
            count++;
        }

        return count;
    }
}
