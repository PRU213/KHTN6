using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuController : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void InitializeOnLoad()
    {
        GameObject go = new GameObject("PauseMenuController_Global");
        go.AddComponent<PauseMenuController>();
        DontDestroyOnLoad(go);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        AttachAllButtons();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AttachAllButtons();

        if (mode == LoadSceneMode.Additive && (scene.name == "TamDung" || scene.name == "XacNhan"))
        {
            // Disable extra cameras and audio listeners in the additive scene to avoid conflicts with the main scene
            foreach (GameObject go in scene.GetRootGameObjects())
            {
                Camera cam = go.GetComponentInChildren<Camera>();
                if (cam != null)
                {
                    cam.gameObject.SetActive(false);
                }
                
                // Also disable duplicate EventSystems to prevent warnings
                UnityEngine.EventSystems.EventSystem es = go.GetComponentInChildren<UnityEngine.EventSystems.EventSystem>();
                if (es != null)
                {
                    es.gameObject.SetActive(false);
                }
            }
        }
    }

    private void AttachAllButtons()
    {
        AttachButtonListener("btnTamDung", OnTamDungClicked);
        AttachButtonListener("TiepTuc", OnTiepTucClicked);
        AttachButtonListener("VeTrangChu", OnVeTrangChuClicked);
        AttachButtonListener("Co", OnCoClicked);
        AttachButtonListener("Khong", OnKhongClicked);
        
        // Force biology button to work
        AttachButtonListener("Biology", () => {
            SceneManager.LoadScene("ChonBai");
        });
    }

    private void AttachButtonListener(string buttonName, UnityEngine.Events.UnityAction action)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene s = SceneManager.GetSceneAt(i);
            if (s.isLoaded)
            {
                foreach (GameObject rootGo in s.GetRootGameObjects())
                {
                    Transform[] transforms = rootGo.GetComponentsInChildren<Transform>(true);
                    foreach (Transform t in transforms)
                    {
                        if (t.name == buttonName)
                        {
                            Button btn = t.GetComponent<Button>();
                            if (btn == null)
                            {
                                btn = t.gameObject.AddComponent<Button>();
                            }
                            btn.onClick.RemoveAllListeners();
                            btn.onClick.AddListener(action);
                        }
                    }
                }
            }
        }
    }

    private void OnTamDungClicked()
    {
        // Don't open multiple pause menus
        if (SceneManager.GetSceneByName("TamDung").isLoaded) return;
        
        Time.timeScale = 0;
        SceneManager.LoadScene("TamDung", LoadSceneMode.Additive);
    }

    private void OnTiepTucClicked()
    {
        Time.timeScale = 1;
        SceneManager.UnloadSceneAsync("TamDung");
    }

    private void OnVeTrangChuClicked()
    {
        if (SceneManager.GetSceneByName("XacNhan").isLoaded) return;
        
        SceneManager.LoadScene("XacNhan", LoadSceneMode.Additive);
        SceneManager.UnloadSceneAsync("TamDung");
    }

    private void OnCoClicked()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene("HomePage", LoadSceneMode.Single);
    }

    private void OnKhongClicked()
    {
        Time.timeScale = 1;
        SceneManager.UnloadSceneAsync("XacNhan");
    }
}
