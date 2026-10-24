using UnityEngine;

public class HomeHistoryController : MonoBehaviour
{
    [Header("Screens")]
    public GameObject homePage;
    public GameObject historyPage;
    public GameObject rankPage;

    private void Start()
    {
        if (homePage == null) homePage = GameObject.Find("HomePage");
        if (historyPage == null) historyPage = GameObject.Find("History");
        if (rankPage == null) rankPage = GameObject.Find("Rank");

        ShowHome();
    }

    // =========================
    // HOME
    // =========================
    public void ShowHome()
    {
        if (homePage != null)
            homePage.SetActive(true);

        // Tự động tìm object đang active nếu chưa gán
        GameObject activeHistory = historyPage != null ? historyPage : GameObject.Find("History");
        if (activeHistory != null)
            activeHistory.SetActive(false);

        GameObject activeRank = rankPage != null ? rankPage : GameObject.Find("Rank");
        if (activeRank != null)
            activeRank.SetActive(false);
    }

    // =========================
    // HISTORY
    // =========================
    public void OpenHistory()
    {
        if (homePage != null)
            homePage.SetActive(false);

        if (historyPage != null)
            historyPage.SetActive(true);

        if (rankPage != null)
            rankPage.SetActive(false);
    }

    // =========================
    // RANK
    // =========================
    public void OpenRank()
    {
        if (homePage != null)
            homePage.SetActive(false);

        if (historyPage != null)
            historyPage.SetActive(false);

        if (rankPage != null)
            rankPage.SetActive(true);
    }
}