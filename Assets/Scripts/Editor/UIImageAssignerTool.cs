using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.Linq;

public class UIImageAssignerTool : EditorWindow
{
    [MenuItem("Tools/Khôi phục ảnh bị Missing (Auto Assign)")]
    public static void ShowWindow()
    {
        GetWindow<UIImageAssignerTool>("Khôi phục Ảnh");
    }

    private bool forceReplace = false;

    private void OnGUI()
    {
        GUILayout.Label("Công cụ Cứu Hộ Ảnh UI Bị Mất", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox("Khi merge code bị lỗi 'Missing (Mono Script)' hoặc mất tham chiếu ảnh, công cụ này sẽ quét toàn bộ Scene.\n\n" +
            "Nó sẽ lấy tên của Object (vd: 'btn_login') và tự động tìm trong Project bức ảnh có tên tương ứng để gắn lại vào Image/SpriteRenderer.", MessageType.Info);

        GUILayout.Space(10);
        forceReplace = EditorGUILayout.Toggle("Ghi đè cả các ảnh đang có", forceReplace);
        GUILayout.Space(10);

        if (GUILayout.Button("🛠 BẮT ĐẦU QUÉT VÀ GÁN LẠI ẢNH", GUILayout.Height(50)))
        {
            AssignImages();
        }
    }

    private void AssignImages()
    {
        int assignCount = 0;

        // Xử lý UI Image
        Image[] allImages = FindObjectsOfType<Image>(true);
        foreach (Image img in allImages)
        {
            if (!forceReplace && img.sprite != null && img.sprite.name != "UISprite" && img.sprite.name != "Background") continue;

            Sprite s = FindSpriteByName(img.gameObject.name);
            if (s != null && img.sprite != s)
            {
                img.sprite = s;
                EditorUtility.SetDirty(img);
                assignCount++;
            }
        }

        // Xử lý SpriteRenderer
        SpriteRenderer[] allRenderers = FindObjectsOfType<SpriteRenderer>(true);
        foreach (SpriteRenderer sr in allRenderers)
        {
            if (!forceReplace && sr.sprite != null) continue;

            Sprite s = FindSpriteByName(sr.gameObject.name);
            if (s != null && sr.sprite != s)
            {
                sr.sprite = s;
                EditorUtility.SetDirty(sr);
                assignCount++;
            }
        }

        if (assignCount > 0)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("Thành công", $"Tuyệt vời! Đã tìm và gán lại thành công {assignCount} ảnh bị mất trong Scene!", "Tuyệt quá");
        }
        else
        {
            EditorUtility.DisplayDialog("Hoàn tất", "Không tìm thấy ảnh nào cần gán hoặc tên Object không khớp với tên ảnh nào trong Project.", "OK");
        }
    }

    private Sprite FindSpriteByName(string name)
    {
        // 1. Tìm chính xác tên
        string[] guids = AssetDatabase.FindAssets(name + " t:Sprite");
        
        // 2. Thử xóa hậu tố như " (1)" hoặc số " 1" (vd: "Cloud 1" -> "Cloud")
        if (guids.Length == 0 && name.Contains(" "))
        {
            string cleanName = name.Substring(0, name.LastIndexOf(" ")).Trim();
            guids = AssetDatabase.FindAssets(cleanName + " t:Sprite");
        }
        
        // 3. Thử xóa _ (vd: btn_login_hover)
        if (guids.Length == 0 && name.Contains("_"))
        {
            string cleanName = name.Replace("_", " ");
            guids = AssetDatabase.FindAssets(cleanName + " t:Sprite");
        }

        if (guids.Length > 0)
        {
            // Ưu tiên trùng tên chính xác (ko phân biệt hoa thường)
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (s != null && s.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                {
                    return s;
                }
            }

            // Nếu không, lấy ảnh đầu tiên tìm được
            string firstPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<Sprite>(firstPath);
        }
        return null;
    }
}
