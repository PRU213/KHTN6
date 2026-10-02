using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SetupChonBaiUI : EditorWindow
{
    [MenuItem("Tools/Tạo Khung Danh Sách (Scroll View) Chuẩn")]
    public static void CreateStandardScrollView()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Không tìm thấy Canvas trong Scene hiện tại!");
            return;
        }

        // Tạo Standard Scroll View của Unity
        GameObject scrollView = DefaultControls.CreateScrollView(new DefaultControls.Resources());
        scrollView.name = "List_ScrollView";
        scrollView.transform.SetParent(canvas.transform, false);
        
        RectTransform rt = scrollView.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(400, 500); // Kích thước mặc định
        rt.anchoredPosition = Vector2.zero;

        // Cấu hình Content
        ScrollRect sr = scrollView.GetComponent<ScrollRect>();
        RectTransform content = sr.content;
        
        VerticalLayoutGroup vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.childControlHeight = false;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 10;
        vlg.padding = new RectOffset(10, 10, 10, 10);
        
        ContentSizeFitter csf = content.gameObject.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Xóa List cũ nếu có
        Transform oldList = canvas.transform.Find("List");
        if (oldList != null && oldList != scrollView.transform)
        {
            Undo.DestroyObjectImmediate(oldList.gameObject);
        }

        // Cập nhật tên của ScrollView thành List để script quản lý tìm thấy
        scrollView.name = "List";

        // Chọn nó trong editor để người dùng dễ chỉnh sửa
        Selection.activeGameObject = scrollView;
        
        Debug.Log("Tạo Scroll View thành công! Hãy kéo thả vị trí và kích thước của nó cho phù hợp.");
    }
}
