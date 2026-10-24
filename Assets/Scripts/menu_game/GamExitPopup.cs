using UnityEngine;

public class GameExitPopup : MonoBehaviour
{
    [Header("Popup dùng chung")]
    public GameObject exitPopup;

    [Header("Menu chọn game")]
    public GameObject menuGame;

    private GameObject currentGame;

    // Game nào bấm X thì truyền game đó vào đây
    public void OpenExitPopup(GameObject game)
    {
        currentGame = game;

        Debug.Log("Mở popup từ game: " + game.name);

        if (exitPopup != null)
            exitPopup.SetActive(true);
        else
            Debug.LogError("ExitPopup chưa được gán!");

        Time.timeScale = 0f;
    }

    // Bấm KHÔNG
    public void CancelExit()
    {
        if (exitPopup != null)
            exitPopup.SetActive(false);

        Time.timeScale = 1f;
    }

    // Bấm CÓ
    public void ConfirmExit()
    {
        Time.timeScale = 1f;

        if (exitPopup != null)
            exitPopup.SetActive(false);

        if (currentGame != null)
        {
            Debug.Log("Thoát game: " + currentGame.name);

            currentGame.SetActive(false);
            currentGame = null;
        }

        if (menuGame != null)
            menuGame.SetActive(true);
    }
}