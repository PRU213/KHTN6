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

        // Tắt tất cả các màn hình phụ, bản đồ và minigame, chỉ mở duy nhất HomePage
        DeactivateAllSubScreens();
        
        if (homeCanvas != null) homeCanvas.SetActive(true);

        if (txtName != null)
        {
            string fullName = !string.IsNullOrEmpty(GameSession.Fullname) 
                ? GameSession.Fullname 
                : PlayerPrefs.GetString("Fullname", "Người chơi");
            txtName.text = fullName;
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

        if (btnQuit != null)
            btnQuit.onClick.AddListener(QuitGame);

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
            GameObject obj = GameObject.Find("btn_Thoat");
            
            if (obj != null) btnQuit = obj.GetComponent<Button>();
        }
    }

    public void QuitGame()
    {
        Debug.Log("Thoat Game!");
        Application.Quit();
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
