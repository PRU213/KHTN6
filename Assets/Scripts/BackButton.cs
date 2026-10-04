using UnityEngine;

public class BackButton : MonoBehaviour
{
    public GameObject menuScreen; // menu_game (1)

    public void GoBack()
    {
        menuScreen.SetActive(false);
        if (RememberScreen.previous != null)
            RememberScreen.previous.SetActive(true);
    }
}