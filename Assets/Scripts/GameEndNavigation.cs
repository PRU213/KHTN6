using UnityEngine;
using UnityEngine.UI;

public class GameEndNavigation : MonoBehaviour
{
    [Header("2 nút hiện khi kết thúc")]
    public Button btnThoatGame;
    public Button btnTrangChu;

    [Header("Panel đích")]
    public GameObject gameRoot;        // chính Football_Game4
    public GameObject mapMonHoc;       // Physical_map_game (map của môn đang chọn)
    public GameObject menuGame;        // menu_game (màn chọn game, ảnh 5)

    void Start()
    {
        btnThoatGame.onClick.AddListener(OnThoatGame);
        btnTrangChu.onClick.AddListener(OnTrangChu);
        HideButtons();
    }

    // Gọi hàm này khi game kết thúc (lúc hiện "Hoàn thành!...")
    public void ShowEndButtons()
    {
        btnThoatGame.gameObject.SetActive(true);
        btnTrangChu.gameObject.SetActive(true);
    }

    public void HideButtons()
    {
        btnThoatGame.gameObject.SetActive(false);
        btnTrangChu.gameObject.SetActive(false);
    }

    void OnThoatGame()
    {
        HideButtons();
        Time.timeScale = 1f;
        if (GameData.Instance != null) GameData.Instance.isTimerRunning = false;
        if (gameRoot != null) gameRoot.SetActive(false);
        if (mapMonHoc != null) mapMonHoc.SetActive(true);
    }

    void OnTrangChu()
    {
        HideButtons();
        Time.timeScale = 1f;
        if (GameData.Instance != null) GameData.Instance.isTimerRunning = false;
        if (gameRoot != null) gameRoot.SetActive(false);
        if (menuGame != null) menuGame.SetActive(true);
    }
}
