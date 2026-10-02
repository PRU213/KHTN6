using UnityEngine;

public class GameplayInit : MonoBehaviour
{
    [SerializeField] private RectTransform player;
    [SerializeField] private GameObject beanIcon; // Icon HUD

    void Start()
    {
        // Lần đầu vào scene
        if (GameSession.startPosition == Vector2.zero)
        {
            if (player != null)
            {
                GameSession.startPosition = player.anchoredPosition;
            }
        }
        
        // Quay về từ màn hình câu hỏi
        if (GameSession.returnPosition != Vector2.zero)
        {
            if (player != null)
            {
                player.anchoredPosition = GameSession.returnPosition;
            }
            GameSession.returnPosition = Vector2.zero; // Clear vị trí sau khi dùng
        }

        // Cập nhật UI hạt đậu
        if (GameSession.hasBean && beanIcon != null)
        {
            beanIcon.SetActive(true);
        }
    }
}
