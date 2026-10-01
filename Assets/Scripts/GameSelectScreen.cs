using UnityEngine;
using UnityEngine.UI;

public class GameSelectScreen : MonoBehaviour
{
    [System.Serializable]
    public class GameEntry
    {
        public Button button;
        public GameObject gamePanel;
    }

    public GameEntry[] games;
    public Button backButton;

    GameObject returnPanel;

    void Awake()
    {
        foreach (var g in games)
        {
            var entry = g;
            if (entry.button) entry.button.onClick.AddListener(() => OpenGame(entry));
        }
        if (backButton) backButton.onClick.AddListener(Back);
    }

    public void Open(GameObject fromPanel)
    {
        returnPanel = fromPanel;
        if (returnPanel) returnPanel.SetActive(false);
        gameObject.SetActive(true);
    }

    void OpenGame(GameEntry entry)
    {
        if (!entry.gamePanel) return;
        gameObject.SetActive(false);
        entry.gamePanel.SetActive(true);
    }

    void Back()
    {
        gameObject.SetActive(false);
        if (returnPanel) returnPanel.SetActive(true);
    }

    public void ReturnFromGame(GameObject game)
    {
        if (game) game.SetActive(false);
        gameObject.SetActive(true);
    }
}