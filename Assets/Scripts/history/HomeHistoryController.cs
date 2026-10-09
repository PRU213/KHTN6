using UnityEngine;

public class HomeHistoryController : MonoBehaviour
{
    [Header("Screens")]
    public GameObject homePage;
    public GameObject historyPage;
    public GameObject rankPage;

    private void Start()
    {
        ShowHome();
    }

    // =========================
    // HOME
    // =========================
    public void ShowHome()
    {
        if (homePage != null)
            homePage.SetActive(true);

        if (historyPage != null)
            historyPage.SetActive(false);

        if (rankPage != null)
            rankPage.SetActive(false);
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