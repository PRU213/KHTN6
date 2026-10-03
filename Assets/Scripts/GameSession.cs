using System.Collections.Generic;
using UnityEngine;

public static class GameSession
{
    // Môn học và chương
    public static string Subject = "";
    public static string Chapter = "";

    // Máu của người chơi
    public static int hp = 3;

    // Scene game cần quay lại sau khi trả lời câu hỏi
    public static string gameplaySceneName = "SampleScene";

    // Scene câu hỏi (dùng khi gặp quái vật)
    public static string questionSceneName = "QuestionScene";

    // Vị trí câu hỏi hiện tại
    private static int currentQuestionIndex = 0;

    // ===== Hạt đậu (Bean) =====
    public static bool hasBean = false;
    public static bool beanSpawned = false;

    // ===== Quái vật (Enemy encounter) =====
    public static int killCount = 0;
    private static HashSet<string> deadEnemies = new HashSet<string>();
    private static string currentEncounterEnemyId = "";

    // ===== Vị trí player khi chuyển scene =====
    public static Vector2 startPosition = Vector2.zero;
    public static Vector2 returnPosition = Vector2.zero;

    /// <summary>
    /// Kiểm tra quái vật đã bị tiêu diệt chưa.
    /// </summary>
    public static bool IsEnemyDead(string enemyId)
    {
        return deadEnemies.Contains(enemyId);
    }

    /// <summary>
    /// Bắt đầu encounter: lưu enemyId và vị trí player để quay lại sau.
    /// </summary>
    public static void StartEncounter(string enemyId, Vector2 playerPosition)
    {
        currentEncounterEnemyId = enemyId;
        returnPosition = playerPosition;
    }

    /// <summary>
    /// Lấy câu hỏi tiếp theo.
    /// </summary>
    public static int GetNextQuestionIndex()
    {
        int index = currentQuestionIndex;
        currentQuestionIndex++;
        return index;
    }

    /// <summary>
    /// Xử lý khi người chơi trả lời đúng.
    /// </summary>
    public static void OnAnswerCorrect()
    {
        // Đánh dấu quái vật hiện tại đã chết
        if (!string.IsNullOrEmpty(currentEncounterEnemyId))
        {
            deadEnemies.Add(currentEncounterEnemyId);
            killCount++;
            currentEncounterEnemyId = "";
        }
    }

    /// <summary>
    /// Xử lý khi người chơi trả lời sai.
    /// </summary>
    public static void OnAnswerWrong()
    {
        if (hp > 0)
        {
            hp--;
        }
    }

    /// <summary>
    /// Reset dữ liệu khi hết máu hoặc bắt đầu lượt chơi mới.
    /// </summary>
    public static void ResetAll()
    {
        hp = 3;
        currentQuestionIndex = 0;
        hasBean = false;
        beanSpawned = false;
        killCount = 0;
        deadEnemies.Clear();
        currentEncounterEnemyId = "";
        startPosition = Vector2.zero;
        returnPosition = Vector2.zero;
    }
}