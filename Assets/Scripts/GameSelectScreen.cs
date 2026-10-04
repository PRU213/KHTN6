using UnityEngine;
using UnityEngine.UI;

public class GameSelectScreen : MonoBehaviour
{
    [System.Serializable]
    public class GameEntry
    {
        [Header("Tên game lưu vào GameSession")]
        public string gameName;

        [Header("Nút chọn game")]
        public Button button;

        [Header("Panel game thật")]
        public GameObject gamePanel;
    }

    [Header("Danh sách game")]
    public GameEntry[] games;

    [Header("Nút quay lại")]
    public Button backButton;

    [Header("Màn chọn độ khó / hình thức")]
    public GameObject chooseGamePanel;

    private GameObject returnPanel;

    void Awake()
    {
        foreach (var g in games)
        {
            GameEntry entry = g;

            if (entry.button != null)
            {
                entry.button.onClick.AddListener(
                    () => SelectGame(entry)
                );
            }
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(Back);
        }
    }

    /// <summary>
    /// Mở màn chọn game.
    /// fromPanel thường là theoryPanel.
    /// </summary>
    public void Open(GameObject fromPanel)
    {
        returnPanel = fromPanel;

        if (returnPanel != null)
        {
            returnPanel.SetActive(false);
        }

        gameObject.SetActive(true);
    }

    /// <summary>
    /// Khi người chơi chọn Football / Fly / Sheep...
    ///
    /// KHÔNG mở game ngay.
    /// Chỉ lưu game đã chọn rồi mở màn choosegame.
    /// </summary>
    void SelectGame(GameEntry entry)
    {
        if (entry == null)
            return;

        // Nếu quên nhập tên game trong Inspector
        if (string.IsNullOrEmpty(entry.gameName))
        {
            Debug.LogError(
                "GameEntry chưa có gameName!"
            );

            return;
        }

        // Lưu game đã chọn
        GameSession.SelectedGame = entry.gameName;

        Debug.Log(
            "Đã chọn game: "
            + GameSession.SelectedGame
        );

        // Tắt menu_game
        gameObject.SetActive(false);

        // Mở màn chọn độ khó / hình thức
        if (chooseGamePanel != null)
        {
            chooseGamePanel.SetActive(true);
        }
        else
        {
            Debug.LogError(
                "Chưa gán chooseGamePanel trong GameSelectScreen!"
            );
        }
    }

    /// <summary>
    /// Quay lại màn trước đó, ví dụ theory_physic.
    /// </summary>
    void Back()
    {
        gameObject.SetActive(false);

        if (returnPanel != null)
        {
            returnPanel.SetActive(true);
        }
    }

    /// <summary>
    /// Dùng nếu game muốn quay trở lại menu chọn game.
    /// </summary>
    public void ReturnFromGame(GameObject game)
    {
        if (game != null)
        {
            game.SetActive(false);
        }

        gameObject.SetActive(true);
    }

    /// <summary>
    /// Tìm GameObject của game đã chọn.
    /// ChooseGameSettings có thể gọi hàm này
    /// sau khi người chơi bấm Bắt đầu.
    /// </summary>
    public GameObject GetSelectedGamePanel()
    {
        foreach (var g in games)
        {
            if (
                g.gameName == GameSession.SelectedGame
                && g.gamePanel != null
            )
            {
                return g.gamePanel;
            }
        }

        Debug.LogError(
            "Không tìm thấy gamePanel cho game: "
            + GameSession.SelectedGame
        );

        return null;
    }

    /// <summary>
    /// Mở game đã được lưu trong GameSession.
    /// Có thể gọi từ màn choosegame.
    /// </summary>
    public void OpenSelectedGame()
    {
        GameObject selectedGame =
            GetSelectedGamePanel();

        if (selectedGame == null)
            return;

        if (chooseGamePanel != null)
        {
            chooseGamePanel.SetActive(false);
        }

        selectedGame.SetActive(true);
    }
}