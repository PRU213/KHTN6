using UnityEngine;

public class GameExitButton : MonoBehaviour
{
    [Header("Game hiện tại")]
    public GameObject currentGame;

    [Header("UI Manager")]
    public GameExitPopup exitManager;

    public void OpenExitPopup()
    {
        if (exitManager == null)
        {
            Debug.LogError("Chưa gán GameExitPopup!");
            return;
        }

        if (currentGame == null)
        {
            Debug.LogError("Chưa gán Current Game!");
            return;
        }

        exitManager.OpenExitPopup(currentGame);
    }
}