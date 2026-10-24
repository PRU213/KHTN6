using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChooseGameSettings : MonoBehaviour
{
    [Header("Độ khó")]
    public Toggle easyToggle;
    public Toggle mediumToggle;
    public Toggle hardToggle;

    [Header("Hình thức làm bài")]
    public Toggle multipleChoiceToggle;
    public Toggle essayToggle;

    [Header("Buttons")]
    public Button startButton;
    public Button backButton;

    [Header("Screens")]
    public GameSelectScreen gameSelectScreen;
    public GameObject chooseGamePanel;

    void OnEnable()
    {
        bool supportsEssay = GameSession.SelectedGame == "Planet" || GameSession.SelectedGame == "Football" || GameSession.SelectedGame == "Sheep" || GameSession.SelectedGame == "Cave";
        if (!supportsEssay && multipleChoiceToggle != null) multipleChoiceToggle.isOn = true;
        if (essayToggle != null) {
            essayToggle.interactable = supportsEssay;
            if (!supportsEssay) essayToggle.isOn = false;
        }
        if (multipleChoiceToggle != null && (essayToggle == null || !essayToggle.isOn)) multipleChoiceToggle.isOn = true;
    }

    private void Awake()
    {
        // The scene's two toggles may not share a ToggleGroup.
        if (multipleChoiceToggle != null) multipleChoiceToggle.onValueChanged.AddListener(value => {
            if (value && essayToggle != null) essayToggle.isOn = false;
            EnsureQuestionType();
        });
        if (essayToggle != null) essayToggle.onValueChanged.AddListener(value => {
            if (value && multipleChoiceToggle != null) multipleChoiceToggle.isOn = false;
            EnsureQuestionType();
        });
        if (startButton != null)
        {
            startButton.onClick.AddListener(StartGame);
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(BackToGameSelect);
        }
<<<<<<< Updated upstream
=======
    }

    void EnsureQuestionType()
    {
        if (multipleChoiceToggle != null && !multipleChoiceToggle.isOn && (essayToggle == null || !essayToggle.isOn))
            multipleChoiceToggle.isOn = true;
>>>>>>> Stashed changes
    }

    public void StartGame()
    {
        // ============================
        // 1. LƯU ĐỘ KHÓ
        // ============================

        GameSession.Difficulties.Clear();

        if (easyToggle != null && easyToggle.isOn)
        {
            GameSession.Difficulties.Add("Dễ");
        }

        if (mediumToggle != null && mediumToggle.isOn)
        {
            GameSession.Difficulties.Add("Trung bình");
        }

        if (hardToggle != null && hardToggle.isOn)
        {
            GameSession.Difficulties.Add("Khó");
        }

        // Phải chọn ít nhất 1 độ khó
        if (GameSession.Difficulties.Count == 0)
        {
            Debug.LogWarning("Bạn phải chọn ít nhất một độ khó!");
            return;
        }


        // ============================
<<<<<<< Updated upstream
        // 2. HIỆN TẠI CHỈ LÀM TRẮC NGHIỆM
        // ============================

        GameSession.QuestionType = "Trắc nghiệm";
=======
        // 2. LƯU HÌNH THỨC ĐÃ CHỌN
        // ============================

        bool supportsEssay = GameSession.SelectedGame == "Planet" || GameSession.SelectedGame == "Football" || GameSession.SelectedGame == "Sheep" || GameSession.SelectedGame == "Cave";
        GameSession.QuestionType = supportsEssay && essayToggle != null && essayToggle.isOn ? "Tự luận" : "Trắc nghiệm";
>>>>>>> Stashed changes


        // ============================
        // 3. DEBUG
        // ============================

        Debug.Log("Môn: " + GameSession.Subject);

        Debug.Log(
            "Độ khó: "
            + string.Join(", ", GameSession.Difficulties)
        );

        Debug.Log(
            "Game: "
            + GameSession.SelectedGame
        );

        Debug.Log(
            "Hình thức: "
            + GameSession.QuestionType
        );


        // ============================
        // 4. MỞ GAME ĐÃ CHỌN
        // ============================

        if (gameSelectScreen != null)
        {
            gameSelectScreen.OpenSelectedGame();
        }
        else
        {
            Debug.LogError("Chưa gán GameSelectScreen!");
        }
    }

    public void BackToGameSelect()
    {
        if (chooseGamePanel != null)
        {
            chooseGamePanel.SetActive(false);
        }

        if (gameSelectScreen != null)
        {
            gameSelectScreen.ReturnToSelection();
        }
    }
}