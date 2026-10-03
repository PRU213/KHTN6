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

    void OpenMap(GameObject map)
    {
        if (map == null)
        {
            Debug.LogWarning("Chưa gán map cho môn này!");
            return;
        }

        homeCanvas.SetActive(false);
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

        homeCanvas.SetActive(true);
    }
}