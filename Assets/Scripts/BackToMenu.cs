using UnityEngine;

public class BackToMenu : MonoBehaviour
{
    public GameObject currentGame;
    public GameObject menuPanel;

    public void GoBack()
    {
        currentGame.SetActive(false);
        menuPanel.SetActive(true);
    }
}