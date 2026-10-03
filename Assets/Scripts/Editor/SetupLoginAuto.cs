using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using System.Linq;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public class SetupLoginAuto
{
    static SetupLoginAuto()
    {
        EditorApplication.delayCall += DoSetup;
    }

    static void DoSetup()
    {
        if (EditorPrefs.GetBool("SetupLoginAutoDone6", false)) return;

        bool isModified = false;

        // 1. Setup Build Settings
        var scenes = EditorBuildSettings.scenes.ToList();
        string[] requiredScenes = { "Assets/Scenes/SampleScene.unity", "Assets/Scenes/HomePage.unity" };
        bool buildSettingsChanged = false;
        
        foreach (string sc in requiredScenes)
        {
            if (!scenes.Any(s => s.path == sc))
            {
                scenes.Add(new EditorBuildSettingsScene(sc, true));
                buildSettingsChanged = true;
            }
        }
        
        if (buildSettingsChanged)
        {
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("Đã thêm Scene vào Build Settings.");
        }

        // 2. Setup Scene
        if (SceneManager.GetActiveScene().name == "SampleScene")
        {
            LoginController loginController = Object.FindObjectOfType<LoginController>(true);
            
            if (loginController != null)
            {
                GoogleSheetAPI api = Object.FindObjectOfType<GoogleSheetAPI>(true);
                if (api == null)
                {
                    api = loginController.gameObject.AddComponent<GoogleSheetAPI>();
                    isModified = true;
                    Debug.Log("Đã thêm GoogleSheetAPI vào " + loginController.name);
                }

                if (loginController.api == null)
                {
                    loginController.api = api;
                    isModified = true;
                }

                // Find ALL GameObjects named HienMK and AnMK
                var allTransforms = Resources.FindObjectsOfTypeAll<Transform>().Where(t => t.gameObject.scene.isLoaded).ToList();
                Transform hienMkTrans = allTransforms.FirstOrDefault(t => t.name == "HienMK");
                Transform anMkTrans = allTransforms.FirstOrDefault(t => t.name == "AnMK");

                if (hienMkTrans != null)
                {
                    Button hbtn = hienMkTrans.GetComponent<Button>();
                    if (hbtn == null) hbtn = hienMkTrans.gameObject.AddComponent<Button>();
                    
                    Image himg = hienMkTrans.GetComponent<Image>();
                    if (himg != null) himg.raycastTarget = true;

                    if (loginController.btnHienMK != hbtn)
                    {
                        loginController.btnHienMK = hbtn;
                        isModified = true;
                        Debug.Log("Đã nối nút HienMK (và thêm Button component nếu cần).");
                    }
                }

                if (anMkTrans != null)
                {
                    Button abtn = anMkTrans.GetComponent<Button>();
                    if (abtn == null) abtn = anMkTrans.gameObject.AddComponent<Button>();
                    
                    Image aimg = anMkTrans.GetComponent<Image>();
                    if (aimg != null) aimg.raycastTarget = true;

                    if (loginController.btnAnMK != abtn)
                    {
                        loginController.btnAnMK = abtn;
                        isModified = true;
                        Debug.Log("Đã nối nút AnMK (và thêm Button component nếu cần).");
                    }
                }

                Button btnLogin = allTransforms.FirstOrDefault(t => t.name == "btn_login")?.GetComponent<Button>();
                
                if (btnLogin != null)
                {
                    // Find ThongBao
                    TextMeshProUGUI errorTxt = null;
                    
                    // Search for "ThongBao" anywhere in the active scene
                    var thongBaoObjects = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>().Where(t => t.name == "ThongBao" && t.gameObject.scene.isLoaded);
                    if (thongBaoObjects.Any())
                    {
                        errorTxt = thongBaoObjects.First();
                    }

                    if (errorTxt != null)
                    {
                        // Căn giữa text
                        if (errorTxt.alignment != TextAlignmentOptions.Center)
                        {
                            errorTxt.alignment = TextAlignmentOptions.Center;
                            isModified = true;
                            Debug.Log("Đã căn giữa text ThongBao.");
                        }

                        if (loginController.errorText != errorTxt)
                        {
                            loginController.errorText = errorTxt;
                            isModified = true;
                            Debug.Log("Đã nối Text ThongBao với LoginController.");
                        }
                    }
                    
                    // Remove auto-created ErrorText if exists
                    Transform autoErrT = btnLogin.transform.parent.Find("ErrorText");
                    if (autoErrT != null)
                    {
                        Object.DestroyImmediate(autoErrT.gameObject);
                        isModified = true;
                        Debug.Log("Đã xóa ErrorText cũ.");
                    }

                    // Link OnClick
                    int count = btnLogin.onClick.GetPersistentEventCount();
                    bool hasEvent = false;
                    for (int i = 0; i < count; i++)
                    {
                        if (btnLogin.onClick.GetPersistentMethodName(i) == "OnLoginClicked" &&
                            btnLogin.onClick.GetPersistentTarget(i) == (Object)loginController)
                        {
                            hasEvent = true;
                            break;
                        }
                    }

                    if (!hasEvent)
                    {
                        UnityEditor.Events.UnityEventTools.AddPersistentListener(btnLogin.onClick, new UnityAction(loginController.OnLoginClicked));
                        isModified = true;
                        Debug.Log("Đã nối nút Login với script.");
                    }
                }
            }

            if (isModified)
            {
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
                Debug.Log("Đã tự động lưu Scene hoàn tất!");
            }
        }

        EditorPrefs.SetBool("SetupLoginAutoDone5", true);
    }
}
