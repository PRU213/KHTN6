using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Dữ liệu chung giữa các scene. Không bị hủy khi chuyển scene.
/// Lưu: máu nhân vật, quái nào đã bị tiêu diệt, quái nào đang giao chiến.
/// </summary>
public class GameData : MonoBehaviour
{
    public static GameData Instance { get; private set; }

    [Header("=== MÁU NHÂN VẬT ===")]
    public int maxHealth = 3;
    public int currentHealth = 3;

    /// <summary>Tên con quái vừa chạm vào (đang chờ trả lời câu hỏi)</summary>
    [HideInInspector] public string currentEnemyName = "";

    /// <summary>Danh sách tên các con quái đã bị tiêu diệt (trả lời đúng)</summary>
    [HideInInspector] public List<string> defeatedEnemies = new List<string>();

    /// <summary>Vị trí nhân vật trước khi chuyển cảnh</summary>
    [HideInInspector] public Vector2 lastPlayerPosition;
    [HideInInspector] public bool hasSavedPosition = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>Mất 1 máu. Trả về true nếu còn sống.</summary>
    public bool TakeDamage()
    {
        currentHealth--;
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            return false; // Chết
        }
        return true; // Còn sống
    }

    /// <summary>Đánh dấu con quái hiện tại đã bị tiêu diệt</summary>
    public void DefeatCurrentEnemy()
    {
        if (!string.IsNullOrEmpty(currentEnemyName) && !defeatedEnemies.Contains(currentEnemyName))
        {
            defeatedEnemies.Add(currentEnemyName);
        }
        currentEnemyName = "";
    }

    /// <summary>Kiểm tra con quái có bị tiêu diệt chưa</summary>
    public bool IsEnemyDefeated(string enemyName)
    {
        return defeatedEnemies.Contains(enemyName);
    }
}
