using UnityEngine;
using UnityEngine.UI;

public class FootballNav : MonoBehaviour
{
    [Header("2 nút")]
    public Button btnThoatGame;
    public Button btnTrangChu;

    [Header("Màn đích")]
    public GameObject gameRoot;   // Football_Game4
    public GameObject menuGame;   // menu_game (màn chọn game)

    void Awake()
    {
        btnThoatGame.onClick.AddListener(() => Go(MapMarker.Current));   // về map môn đang chọn
        btnTrangChu.onClick.AddListener(() => Go(menuGame));             // về màn chọn game
    }

    void Go(GameObject target)
    {
        btnThoatGame.gameObject.SetActive(false);
        btnTrangChu.gameObject.SetActive(false);
        gameRoot.SetActive(false);
        if (target) target.SetActive(true);
    }
}