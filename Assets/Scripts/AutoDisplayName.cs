using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class AutoDisplayName : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void InitializeOnLoad()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        // Chạy một lần cho scene đầu tiên
        ApplyNameToScene(SceneManager.GetActiveScene());
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyNameToScene(scene);
    }

    static void ApplyNameToScene(Scene scene)
    {
        if (!scene.isLoaded) return;
        
        string fullName = PlayerPrefs.GetString("Fullname", "Người chơi");
        string role = PlayerPrefs.GetString("Role", "Student");

        foreach (GameObject rootGo in scene.GetRootGameObjects())
        {
            Transform[] transforms = rootGo.GetComponentsInChildren<Transform>(true); // true = tìm cả object đang bị ẩn
            foreach (Transform t in transforms)
            {
                if (t.name == "txtFullname" || t.name == "txtName")
                {
                    SetText(t, fullName);
                }
                else if (t.name == "txtRole" || t.name == "role")
                {
                    SetText(t, role);
                }
            }
        }
    }
    
    static void SetText(Transform t, string textVal)
    {
        // Hỗ trợ cả Text thường (Legacy)
        Text legacyText = t.GetComponent<Text>();
        if (legacyText != null)
        {
            legacyText.text = textVal;
        }

        // Hỗ trợ cả TextMeshPro
        TextMeshProUGUI tmpText = t.GetComponent<TextMeshProUGUI>();
        if (tmpText != null)
        {
            tmpText.text = textVal;
        }
    }
}
