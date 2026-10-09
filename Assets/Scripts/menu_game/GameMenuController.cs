using UnityEngine;

public class GameMenuController : MonoBehaviour
{
    [Header("Menu Game")]
    public GameObject menuGame;

    [Header("Theory Screens")]
    public GameObject theoryPhysics;
    public GameObject theoryChemistry;

    // Dùng để nhớ môn hiện tại
    private string currentSubject;

    // Khi mở menu game từ môn Vật lý
    public void OpenFromPhysics()
    {
        currentSubject = "Physics";

        if (theoryPhysics != null)
            theoryPhysics.SetActive(false);

        if (theoryChemistry != null)
            theoryChemistry.SetActive(false);

        if (menuGame != null)
            menuGame.SetActive(true);
    }

    // Khi mở menu game từ môn Hóa
    public void OpenFromChemistry()
    {
        currentSubject = "Chemistry";

        if (theoryPhysics != null)
            theoryPhysics.SetActive(false);

        if (theoryChemistry != null)
            theoryChemistry.SetActive(false);

        if (menuGame != null)
            menuGame.SetActive(true);
    }

    // Nút X
    public void CloseMenuGame()
    {
        if (menuGame != null)
            menuGame.SetActive(false);

        if (currentSubject == "Physics")
        {
            if (theoryPhysics != null)
                theoryPhysics.SetActive(true);
        }
        else if (currentSubject == "Chemistry")
        {
            if (theoryChemistry != null)
                theoryChemistry.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Không xác định được môn hiện tại!");
        }
    }
}
