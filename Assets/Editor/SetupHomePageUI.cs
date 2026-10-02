using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

public class SetupHomePageUI : EditorWindow
{
    [MenuItem("Tools/Cài Đặt Nút Cho HomePage")]
    public static void SetupHomePage()
    {
        // Find Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Không tìm thấy Canvas!");
            return;
        }

        // Add HomeMenuController if missing
        HomeMenuController controller = canvas.GetComponent<HomeMenuController>();
        if (controller == null)
        {
            controller = canvas.gameObject.AddComponent<HomeMenuController>();
        }

        // Find biology, chemistry, physics images
        Transform biology = FindChildRecursive(canvas.transform, "Biology");
        if (biology != null)
        {
            Button btn = biology.GetComponent<Button>();
            if (btn == null) btn = biology.gameObject.AddComponent<Button>();
            controller.btnBiology = btn;
            Debug.Log("Đã thiết lập nút Biology");
        }

        Transform physics = FindChildRecursive(canvas.transform, "Physics");
        if (physics != null)
        {
            Button btn = physics.GetComponent<Button>();
            if (btn == null) btn = physics.gameObject.AddComponent<Button>();
            controller.btnPhysics = btn;
            Debug.Log("Đã thiết lập nút Physics");
        }

        Transform chemistry = FindChildRecursive(canvas.transform, "Chemistry");
        if (chemistry != null)
        {
            Button btn = chemistry.GetComponent<Button>();
            if (btn == null) btn = chemistry.gameObject.AddComponent<Button>();
            controller.btnChemistry = btn;
            Debug.Log("Đã thiết lập nút Chemistry");
        }

        EditorUtility.SetDirty(canvas.gameObject);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Cài đặt thành công! Bạn hãy lưu Scene lại (Ctrl + S).");
    }

    private static Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                return child;
            Transform found = FindChildRecursive(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
