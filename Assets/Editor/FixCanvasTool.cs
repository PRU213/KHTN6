using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class FixCanvasTool
{
    [MenuItem("Tools/3. Chỉnh Cam Khít Màn (Fix Màn Hình)")]
    public static void FixCanvasScale()
    {
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        if (canvases.Length == 0)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy Canvas (Giao diện) nào trong màn hình hiện tại!", "OK");
            return;
        }

        int count = 0;
        foreach (Canvas canvas in canvases)
        {
            // Ép luôn hiển thị đè lên toàn bộ màn hình (khít nhất)
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                // Tự động kéo dãn theo tỉ lệ màn hình của người chơi
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080); // Chuẩn Full HD
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f; // Cân bằng cả chiều ngang lẫn dọc
                count++;
            }
        }

        EditorUtility.DisplayDialog("Thành công!", 
            $"Đã chỉnh sửa {count} Canvas.\n\n" +
            "Giao diện của bạn bây giờ sẽ tự động co giãn KHÍT HOÀN TOÀN với màn hình Game (Full HD) mà không bị lệch hay tràn viền.", "OK");
    }
}
