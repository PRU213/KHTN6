using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class HomeMenuController : MonoBehaviour
{
    [Header("Thong tin Nguoi dung")]
    public Text txtName;
    public Text txtRole;

    [Header("Man trang chu")]
    public GameObject homeCanvas;

    [Header("Ban do tung man")]
    public GameObject physicsMap;
    public GameObject chemistryMap;
    public GameObject biologyMap;

    [Header("Cac nut man hoc")]
    public Button btnPhysics;
    public Button btnChemistry;
    public Button btnBiology;

    [Header("Nut quay lai")]
    public Button backPhysics;
    public Button backChemistry;
    public Button backBiology;

    [Header("Nut Thoat")]
    public Button btnQuit;
    GameObject logoutPopup;
    TMP_FontAsset logoutFont;
    bool loggingOut;
    readonly System.Collections.Generic.List<Button> logoutButtons = new System.Collections.Generic.List<Button>();

    void Awake()
    {
        // Tránh xung đột nếu có GameObject rỗng "HomeMenuController" trong scene
        HomeMenuController[] controllers = FindObjectsByType<HomeMenuController>(FindObjectsInactive.Exclude);
        if (controllers.Length > 1 && gameObject.name == "HomeMenuController" && homeCanvas == null)
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        AutoFindReferences();
        ScienceChatController.Install(homeCanvas);
        foreach (var selector in FindObjectsByType<GameSelectScreen>(FindObjectsInactive.Include)) selector.ConfigureDuel();

        // Tắt tất cả các màn hình phụ, bản đồ và minigame, chỉ mở duy nhất HomePage
        DeactivateAllSubScreens();
        
        if (homeCanvas != null) homeCanvas.SetActive(true);

        if (txtName != null)
        {
            string fullName = GameSettingsController.DisplayName;
            txtName.text = fullName;
            txtName.supportRichText = false;
            txtName.fontSize = 24;
            txtName.color = Color.black;
        }

        if (txtRole != null)
        {
            string role = !string.IsNullOrEmpty(GameSession.Role) 
                ? GameSession.Role 
                : PlayerPrefs.GetString("Role", "Student");
            txtRole.text = role;
            txtRole.fontSize = txtName != null ? txtName.fontSize : 24;
            txtRole.color = Color.white;
            txtRole.alignment = TextAnchor.MiddleCenter;
        }

        if (GameData.Instance != null)
        {
            GameData.Instance.isTimerRunning = false;
        }

        // The visible header button is btn_exit, not the inactive btn_Thoat.
        // Include hidden headers so the same action works after changing panels.
        foreach (var button in FindObjectsByType<Button>(FindObjectsInactive.Include)) {
            if (button.name == "btn_sound") GameSettingsController.BindSoundButton(button);
            if (button.name == "btn_setting") {
                button.onClick.RemoveListener(GameSettingsController.Open);
                button.onClick.AddListener(GameSettingsController.Open);
            }
            if (button.name != "btn_exit" && button != btnQuit) continue;
            button.onClick.RemoveListener(QuitGame);
            button.onClick.AddListener(QuitGame);
            logoutButtons.Add(button);
        }

        if (btnPhysics != null)
            btnPhysics.onClick.AddListener(() => OpenMap(physicsMap));

        if (btnChemistry != null)
            btnChemistry.onClick.AddListener(() => OpenMap(chemistryMap));

        if (btnBiology != null)
            btnBiology.onClick.AddListener(() => SceneManager.LoadScene("ChonBai"));

        if (backPhysics != null)
            backPhysics.onClick.AddListener(BackToHome);

        if (backChemistry != null)
            backChemistry.onClick.AddListener(BackToHome);

        if (backBiology != null)
            backBiology.onClick.AddListener(BackToHome);
    }

    void AutoFindReferences()
    {
        Transform root = transform.root;

        if (txtName == null)
        {
            GameObject obj = GameObject.Find("txtFullname");
            if (obj == null) obj = GameObject.Find("txtName");
            
            if (obj != null) 
            {
                txtName = obj.GetComponent<Text>();
                if (txtName == null) txtName = obj.GetComponentInChildren<Text>();
            }
        }

        if (txtRole == null)
        {
            GameObject obj = GameObject.Find("txtRole");
            if (obj == null) obj = GameObject.Find("role");
            
            if (obj != null) 
            {
                txtRole = obj.GetComponent<Text>();
                if (txtRole == null) txtRole = obj.GetComponentInChildren<Text>();
            }
        }

        if (homeCanvas == null)
        {
            GameObject obj = GameObject.Find("HomePage");
            if (obj != null) homeCanvas = obj;
            else
            {
                Transform found = root.Find("HomePage");
                if (found != null) homeCanvas = found.gameObject;
            }
        }

        if (physicsMap == null)
        {
            Transform found = root.Find("Physical_map_game");
            if (found != null) physicsMap = found.gameObject;
        }

        if (chemistryMap == null)
        {
            Transform found = root.Find("Chemistry_map_game");
            if (found != null) chemistryMap = found.gameObject;
        }

        if (biologyMap == null)
        {
            Transform found = root.Find("Biology_map_game");
            if (found != null) biologyMap = found.gameObject;
        }

        if (homeCanvas == null)
            Debug.LogWarning("HomeMenuController: Khong tim thay 'HomePage' trong scene!");

        if (btnPhysics == null)
        {
            GameObject obj = GameObject.Find("Physic");
            if (obj != null) btnPhysics = obj.GetComponent<Button>();
        }

        if (btnChemistry == null)
        {
            GameObject obj = GameObject.Find("Chemistry");
            if (obj != null) btnChemistry = obj.GetComponent<Button>();
        }

        if (btnBiology == null)
        {
            GameObject obj = GameObject.Find("Biology");
            if (obj != null) btnBiology = obj.GetComponent<Button>();
        }

        if (backPhysics == null)
        {
            GameObject obj = GameObject.Find("btn_back");
        }

        if (btnQuit == null)
        {
            GameObject obj = GameObject.Find("btn_exit");
            if (obj == null) obj = GameObject.Find("btn_Thoat");
            
            if (obj != null) btnQuit = obj.GetComponent<Button>();
        }
    }

    public void QuitGame()
    {
        if (loggingOut) return;
        if (logoutPopup == null) BuildLogoutPopup();
        logoutPopup.SetActive(true);
        var cancel = logoutPopup.transform.Find("Card/Cancel");
        if (cancel != null && UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(cancel.gameObject);
    }

    void BuildLogoutPopup()
    {
        logoutPopup = new GameObject("Logout confirmation", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = logoutPopup.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 1000;
        var scaler = logoutPopup.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
        LogoutBox("Dim", logoutPopup.transform, Vector2.zero, Vector2.one, new Color(0, 0, 0, .65f));
        var card = LogoutBox("Card", logoutPopup.transform, new Vector2(.28f, .31f), new Vector2(.72f, .69f), new Color(.25f, .12f, .045f));
        var edge = card.gameObject.AddComponent<Outline>();
        edge.effectColor = new Color(1, .72f, .2f); edge.effectDistance = new Vector2(3, -3);
        var font = Resources.Load<Font>("ScienceChatFont");
        if (font != null) { logoutFont = TMP_FontAsset.CreateFontAsset(font); logoutFont.isMultiAtlasTexturesEnabled = true; }
        LogoutLabel(card, "ĐĂNG XUẤT", new Vector2(.06f, .76f), new Vector2(.94f, .94f), 40, new Color(1, .85f, .4f));
        LogoutLabel(card, "Bạn có muốn đăng xuất không?", new Vector2(.06f, .38f), new Vector2(.94f, .73f), 38, Color.white);
        LogoutButton("Confirm", card, new Vector2(.10f, .10f), new Vector2(.46f, .30f), "Có", new Color(.75f, .34f, .035f), ConfirmLogout);
        LogoutButton("Cancel", card, new Vector2(.54f, .10f), new Vector2(.90f, .30f), "Không", new Color(.035f, .35f, .65f), () => logoutPopup.SetActive(false));
    }

    RectTransform LogoutBox(string objectName, Transform parent, Vector2 min, Vector2 max, Color color)
    {
        var go = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        go.layer = parent.gameObject.layer; go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>(); rect.anchorMin = min; rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = color; return rect;
    }

    void LogoutLabel(Transform parent, string value, Vector2 min, Vector2 max, int size, Color color)
    {
        var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.layer = parent.gameObject.layer; go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>(); rect.anchorMin = min; rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        var text = go.GetComponent<TextMeshProUGUI>(); text.font = logoutFont != null ? logoutFont : TMP_Settings.defaultFontAsset;
        text.text = value; text.fontSize = size; text.enableAutoSizing = true; text.fontSizeMin = 22;
        text.color = color; text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
    }

    void LogoutButton(string objectName, Transform parent, Vector2 min, Vector2 max, string label, Color color, UnityEngine.Events.UnityAction action)
    {
        var rect = LogoutBox(objectName, parent, min, max, color);
        var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = rect.GetComponent<Image>();
        button.onClick.AddListener(action);
        LogoutLabel(rect, label, new Vector2(.05f, .05f), new Vector2(.95f, .95f), 34, Color.white);
    }

    void ConfirmLogout()
    {
        if (loggingOut) return;
        if (!Application.CanStreamedLevelBeLoaded("SampleScene")) {
            Debug.LogError("Không tìm thấy màn đăng nhập SampleScene trong Build Settings.");
            return;
        }
        loggingOut = true;
        GameSession.ClearUserSession();
        GameSession.ResetAll();
        PlayerPrefs.DeleteKey("Username");
        PlayerPrefs.DeleteKey("Fullname");
        PlayerPrefs.DeleteKey("Role");
        PlayerPrefs.Save();
        Time.timeScale = 1f;
        if (GameData.Instance != null) Destroy(GameData.Instance.gameObject);
        SceneManager.LoadScene("SampleScene");
    }

    void OnDestroy()
    {
        foreach (var button in logoutButtons) if (button != null) button.onClick.RemoveListener(QuitGame);
        if (logoutPopup != null) Destroy(logoutPopup);
        if (logoutFont != null) Destroy(logoutFont);
    }

    void OpenMap(GameObject map)
    {
        if (map == null)
        {
            Debug.LogWarning("Chua gan map cho man nay!");
            return;
        }

        homeCanvas?.SetActive(false);
        map.SetActive(true);
    }

    void DeactivateAllSubScreens()
    {
        string[] screens = new string[]
        {
            "Cave_dao_2",
            "Planet_Game_3_blast",
            "Planet_Game_3_blast_ship",
            "Sheep_Game_1",
            "Fly_Game5",
            "Football_Game",
            "Noborimasu",
            "theory_physic",
            "theory_chemistry",
            "Physical_map_game",
            "Chemistry_map_game",
            "Biology_map_game",
            "choosegame",
            "menu_game",
            "Rank",
            "History",
            "ExitPopup"
        };

        foreach (string sName in screens)
        {
            GameObject obj = GameObject.Find(sName);
            if (obj != null && obj != homeCanvas)
            {
                obj.SetActive(false);
            }
        }
    }

    public void BackToHome()
    {
        if (physicsMap != null) physicsMap.SetActive(false);
        if (chemistryMap != null) chemistryMap.SetActive(false);
        if (biologyMap != null) biologyMap.SetActive(false);

        DeactivateAllSubScreens();

        if (homeCanvas != null)
            homeCanvas.SetActive(true);
    }
}
