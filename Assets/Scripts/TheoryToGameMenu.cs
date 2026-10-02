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

    [Header("Môn của màn này")]
    public string subjectName = "Hóa học";   // đúng như cột monID trong sheet câu hỏi

    void Start()
    {
        if (playButton != null)
        {
            playButton.onClick.AddListener(OpenGameMenu);
        }
    }

    void OpenGameMenu()
    {
        GameSession.Subject = subjectName;   // báo cho game biết đang chơi môn nào

        if (theoryScreen != null)
            theoryScreen.SetActive(false);

        if (gameMenu != null)
            gameMenu.SetActive(true);
    }
}