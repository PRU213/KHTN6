using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HomeMenuController : MonoBehaviour
{
    [Header("Thong tin Nguoi dung")]
    public TextMeshProUGUI txtName;
    public TextMeshProUGUI txtRole;

    [Header("MA'n trang chu")]
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

    void Start()
    {
        AutoFindReferences();

        if (txtName != null)
        {
            string fullName = PlayerPrefs.GetString("Fullname", "Người chơi");
            txtName.text = fullName;
            txtName.fontSize = 24;
            txtName.color = Color.black;
        }

        if (txtRole != null)
        {
            string role = PlayerPrefs.GetString("Role", "Student");
            txtRole.text = role;
            txtRole.fontSize = txtName != null ? txtName.fontSize : 24;
            txtRole.color = Color.white;
            txtRole.outlineWidth = 0f;
            txtRole.horizontalAlignment = HorizontalAlignmentOptions.Center;
            txtRole.verticalAlignment = VerticalAlignmentOptions.Middle;
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
            btnBiology.onClick.AddListener(() => UnityEngine.SceneManagement.SceneManager.LoadScene("ChonBai"));

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
                txtName = obj.GetComponent<TextMeshProUGUI>();
            }
        }

        if (txtRole == null)
        {
            GameObject obj = GameObject.Find("txtRole");
            if (obj == null) obj = GameObject.Find("role");
            
            if (obj != null) 
            {
                txtRole = obj.GetComponent<TextMeshProUGUI>();
            }
        }

        if (homeCanvas == null)
        {
            Transform found = root.Find("HomePage");
            if (found != null) homeCanvas = found.gameObject;
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
            GameObject obj = GameObject.Find("btn_back"); // Might find the wrong one, but usually multiple
            // Safer: don't auto assign back buttons if they have same names, let the user assign them in inspector, or search within maps
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

    public void BackToHome()
    {
        if (physicsMap != null)
            physicsMap.SetActive(false);

        if (chemistryMap != null)
            chemistryMap.SetActive(false);

        if (biologyMap != null)
            biologyMap.SetActive(false);

        if (homeCanvas != null)
            homeCanvas.SetActive(true);
    }
}

