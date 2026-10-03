using UnityEngine;
using UnityEngine.UI;

public class HomeMenuController : MonoBehaviour
{
    [Header("Màn trang chủ")]
    public GameObject homeCanvas;

    [Header("Bản đồ từng môn")]
    public GameObject physicsMap;
    public GameObject chemistryMap;
    public GameObject biologyMap;

    [Header("Các nút môn học")]
    public Button btnPhysics;
    public Button btnChemistry;
    public Button btnBiology;

    [Header("Nút quay lại")]
    public Button backPhysics;
    public Button backChemistry;
    public Button backBiology;

    void Start()
    {
        AutoFindReferences();

        if (GameData.Instance != null)
        {
            GameData.Instance.isTimerRunning = false;
        }

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

    /// <summary>
    /// Tự động tìm và gán các GameObject theo tên nếu chưa được gán trong Inspector.
    /// </summary>
    void AutoFindReferences()
    {
        Transform root = transform.root;

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
            Debug.LogWarning("HomeMenuController: Không tìm thấy 'HomePage' trong scene!");
    }

    void OpenMap(GameObject map)
    {
        if (map == null)
        {
            Debug.LogWarning("Chưa gán map cho môn này!");
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