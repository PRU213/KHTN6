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

    readonly Dictionary<RectTransform, Vector2> originalPositions = new Dictionary<RectTransform, Vector2>();
    readonly Dictionary<GameObject, bool> originalVisibility = new Dictionary<GameObject, bool>();

    void OnEnable()
    {
        RestoreLayout();
        bool supportsEssay = GameSession.SelectedGame == "Football" || GameSession.SelectedGame == "Sheep" || GameSession.SelectedGame == "Cave";
        if (!supportsEssay && multipleChoiceToggle != null) multipleChoiceToggle.isOn = true;
        if (essayToggle != null) {
            // Temporarily hide Planet essays; restore the option for practice games.
            essayToggle.gameObject.SetActive(GameSession.SelectedGame != "Planet");
            essayToggle.interactable = supportsEssay;
            if (!supportsEssay) essayToggle.isOn = false;
        }
        if (multipleChoiceToggle != null && (essayToggle == null || !essayToggle.isOn)) multipleChoiceToggle.isOn = true;
        if (GameSession.SelectedGame == "Planet") ApplyDuelLayout();
    }

    void ApplyDuelLayout()
    {
        // These titles are direct children of the shared settings panel in HomePage.
        SetDuelVisibility(transform.Find("title2"));
        if (multipleChoiceToggle != null) SetDuelVisibility(multipleChoiceToggle.transform);
        if (essayToggle != null) SetDuelVisibility(essayToggle.transform);
        MoveForDuel(transform.Find("title1"), -100f);
        if (easyToggle != null) MoveForDuel(easyToggle.transform, -100f);
        if (mediumToggle != null) MoveForDuel(mediumToggle.transform, -100f);
        if (hardToggle != null) MoveForDuel(hardToggle.transform, -100f);
        if (startButton != null) MoveForDuel(startButton.transform, 160f);
    }

    void SetDuelVisibility(Transform target)
    {
        if (target == null) return;
        originalVisibility[target.gameObject] = target.gameObject.activeSelf;
        target.gameObject.SetActive(false);
    }

    void MoveForDuel(Transform target, float offsetY)
    {
        var rect = target as RectTransform;
        if (rect == null) return;
        originalPositions[rect] = rect.anchoredPosition;
        rect.anchoredPosition += new Vector2(0f, offsetY);
    }

    void OnDisable()
    {
        RestoreLayout();
    }

    void RestoreLayout()
    {
        foreach (var entry in originalPositions)
            if (entry.Key != null) entry.Key.anchoredPosition = entry.Value;
        foreach (var entry in originalVisibility)
            if (entry.Key != null) entry.Key.SetActive(entry.Value);
        originalPositions.Clear();
        originalVisibility.Clear();
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
    }

    void EnsureQuestionType()
    {
        if (multipleChoiceToggle != null && !multipleChoiceToggle.isOn && (essayToggle == null || !essayToggle.isOn))
            multipleChoiceToggle.isOn = true;
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
        // 2. LƯU HÌNH THỨC ĐÃ CHỌN
        // ============================

        bool supportsEssay = GameSession.SelectedGame == "Football" || GameSession.SelectedGame == "Sheep" || GameSession.SelectedGame == "Cave";
        GameSession.QuestionType = supportsEssay && essayToggle != null && essayToggle.isOn ? "Tự luận" : "Trắc nghiệm";


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
