using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEditor.SceneManagement;

public class AssignImageTool : EditorWindow
{
    private Sprite selectedSprite;

    [MenuItem("Tools/Gắn Ảnh Cho Khung và Dropdown")]
    public static void ShowWindow()
    {
        GetWindow<AssignImageTool>("Gắn Ảnh Tự Động");
    }

    void OnGUI()
    {
        GUILayout.Label("Chọn ảnh bạn muốn dùng làm khung/nền", EditorStyles.boldLabel);
        
        selectedSprite = (Sprite)EditorGUILayout.ObjectField("Ảnh (Sprite)", selectedSprite, typeof(Sprite), false);

        if (GUILayout.Button("Bắt đầu gắn tự động"))
        {
            if (selectedSprite == null)
            {
                EditorUtility.DisplayDialog("Lỗi", "Bạn chưa chọn ảnh nào cả!", "OK");
                return;
            }

            ApplyToChonBai();
            ApplyToXepHang();
            
            EditorUtility.DisplayDialog("Thành công", "Đã gắn ảnh vào 2 màn Chọn Bài và Xếp Hạng!", "OK");
        }
    }

    private void ApplyToChonBai()
    {
        // 1. Mở Scene Chọn Bài
        string scenePath = "Assets/Scenes/Biology/ChonBai.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        
        ChonBaiManager manager = FindObjectOfType<ChonBaiManager>();
        if (manager != null)
        {
            // Gắn ảnh cho các nút động
            manager.itemSprite = selectedSprite;
            
            // Gắn ảnh cho khung nền (List)
            if (manager.listContainer != null)
            {
                Image listImg = manager.listContainer.GetComponent<Image>();
                if (listImg != null)
                {
                    listImg.sprite = selectedSprite;
                    listImg.type = Image.Type.Sliced;
                }
            }
            
            EditorUtility.SetDirty(manager.gameObject);
        }

        EditorSceneManager.SaveScene(scene);
    }

    private void ApplyToXepHang()
    {
        // 2. Mở Scene Xếp Hạng
        string scenePath = "Assets/Scenes/Biology/XepHang.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        
        // Tìm Dropdown
        Dropdown dropdown = FindObjectOfType<Dropdown>();
        if (dropdown != null)
        {
            // Gắn nền Dropdown chính (khi đang đóng)
            Image mainImg = dropdown.GetComponent<Image>();
            if (mainImg != null)
            {
                mainImg.sprite = selectedSprite;
                mainImg.type = Image.Type.Sliced;
            }

            // Gắn nền của khung xổ xuống (Template)
            if (dropdown.template != null)
            {
                Image templateImg = dropdown.template.GetComponent<Image>();
                if (templateImg != null)
                {
                    templateImg.sprite = selectedSprite;
                    templateImg.type = Image.Type.Sliced;
                }
                
                // Gắn nền cho từng ô thả xuống (Item Background)
                Transform itemBg = dropdown.template.Find("Viewport/Content/Item/Item Background");
                if (itemBg != null)
                {
                    Image itemBgImg = itemBg.GetComponent<Image>();
                    if (itemBgImg != null)
                    {
                        itemBgImg.sprite = selectedSprite;
                        itemBgImg.type = Image.Type.Sliced;
                    }
                }
            }
            
            // Tìm và gắn nền cho UI 'Chuong' cha (nếu có Image)
            Transform chuong = dropdown.transform.parent;
            if (chuong != null)
            {
                Image chuongImg = chuong.GetComponent<Image>();
                if (chuongImg != null)
                {
                    chuongImg.sprite = selectedSprite;
                    chuongImg.type = Image.Type.Sliced;
                }
            }
            
            EditorUtility.SetDirty(dropdown.gameObject);
        }

        EditorSceneManager.SaveScene(scene);
    }
}
