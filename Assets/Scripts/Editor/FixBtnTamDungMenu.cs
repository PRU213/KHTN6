using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class FixBtnTamDungMenu
{
    [MenuItem("Tools/Sửa Lỗi Nút Tạm Dừng")]
    public static void FixButton()
    {
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        int fixedCount = 0;

        foreach (GameObject btn in allObjects)
        {
            if (btn.name.Contains("TamDung") && !EditorUtility.IsPersistent(btn))
            {
                Undo.RecordObject(btn, "Fix Pause Button");

                Canvas canvas = btn.GetComponentInParent<Canvas>();
                
                // Nếu chưa có Canvas, tạo mới
                if (canvas == null)
                {
                    GameObject canvasGo = new GameObject("PauseCanvas");
                    Undo.RegisterCreatedObjectUndo(canvasGo, "Create Pause Canvas");
                    
                    canvas = canvasGo.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvas.sortingOrder = 30000;
                    
                    canvasGo.AddComponent<CanvasScaler>();
                    canvasGo.AddComponent<GraphicRaycaster>();
                    
                    Undo.SetTransformParent(btn.transform, canvasGo.transform, "Move Button to Canvas");
                }
                else
                {
                    Undo.RecordObject(canvas, "Fix Canvas Render Mode");
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvas.sortingOrder = 30000;
                }

                // Sửa SpriteRenderer thành Image
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

                // Cố định góc
                RectTransform rt = btn.GetComponent<RectTransform>();
                if (rt == null) rt = Undo.AddComponent<RectTransform>(btn);
                
                rt.anchorMin = new Vector2(1, 1);
                rt.anchorMax = new Vector2(1, 1);
                rt.pivot = new Vector2(1, 1);
                rt.anchoredPosition = new Vector2(-50, -50);

                EditorUtility.SetDirty(btn);
                fixedCount++;
            }
        }
        
        if (fixedCount > 0)
        {
            Debug.Log($"<color=green>Đã sửa thành công {fixedCount} nút Tạm Dừng!</color>");
        }
        else
        {
            Debug.LogWarning("Không tìm thấy nút nào có tên chứa chữ 'TamDung' trong Scene này.");
        }
    }
}
