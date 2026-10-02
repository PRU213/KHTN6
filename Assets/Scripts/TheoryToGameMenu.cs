using UnityEngine;
using UnityEngine.UI;

public class TheoryToGameMenu : MonoBehaviour
{
    [Header("Màn lý thuyết")]
    public GameObject theoryScreen;

    [Header("Màn chọn game")]
    public GameObject gameMenu;

    [Header("Nút vào chơi")]
    public Button playButton;

    void Start()
    {
        if (playButton != null)
        {
            playButton.onClick.AddListener(OpenGameMenu);
        }
    }

    void OpenGameMenu()
    {
        if (theoryScreen != null)
            theoryScreen.SetActive(false);

        if (gameMenu != null)
            gameMenu.SetActive(true);
    }
}