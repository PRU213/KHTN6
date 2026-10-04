using System.Collections.Generic;
using UnityEngine;

public static class GameSession
{
    // =========================
    // MÔN HỌC & CHƯƠNG
    // =========================

    public static string Subject = "";
    public static string Chapter = "";


    // =========================
    // CẤU HÌNH GAME ĐƯỢC CHỌN
    // =========================

    // Game người chơi vừa chọn trong menu_game
    // Ví dụ: "Football", "Fly", "Sheep", "Planet", "Cave"
    public static string SelectedGame = "";

    // Các độ khó được chọn
    // Có thể chọn nhiều:
    // Dễ + Trung bình
    // Trung bình + Khó
    // hoặc cả 3
    public static List<string> Difficulties = new List<string>();

    // Hình thức làm bài:
    // "Trắc nghiệm" hoặc "Tự luận"
    public static string QuestionType = "";


    // =========================
    // TRẠNG THÁI GAME
    // =========================

    // Máu của người chơi
    public static int hp = 3;

    // Scene game cần quay lại sau khi trả lời câu hỏi
    public static string gameplaySceneName = "SampleScene";

    // Scene câu hỏi
    public static string questionSceneName = "QuestionScene";

    // Vị trí câu hỏi hiện tại
    private static int currentQuestionIndex = 0;


    // =========================
    // HẠT ĐẬU (BEAN)
    // =========================

    public static bool hasBean = false;
    public static bool beanSpawned = false;


    // =========================
    // QUÁI VẬT
    // =========================

    public static int killCount = 0;

    private static HashSet<string> deadEnemies =
        new HashSet<string>();

    private static string currentEncounterEnemyId = "";


    // =========================
    // VỊ TRÍ PLAYER
    // =========================

    public static Vector2 startPosition = Vector2.zero;
    public static Vector2 returnPosition = Vector2.zero;


    // =========================
    // ENEMY
    // =========================

    /// <summary>
    /// Kiểm tra quái vật đã bị tiêu diệt chưa.
    /// </summary>
    public static bool IsEnemyDead(string enemyId)
    {
        return deadEnemies.Contains(enemyId);
    }


    /// <summary>
    /// Bắt đầu encounter:
    /// lưu enemyId và vị trí player.
    /// </summary>
    public static void StartEncounter(
        string enemyId,
        Vector2 playerPosition
    )
    {
        currentEncounterEnemyId = enemyId;
        returnPosition = playerPosition;
    }


    // =========================
    // QUESTION
    // =========================

    /// <summary>
    /// Lấy index câu hỏi tiếp theo.
    /// </summary>
    public static int GetNextQuestionIndex()
    {
        int index = currentQuestionIndex;

        currentQuestionIndex++;

        return index;
    }


    /// <summary>
    /// Reset index câu hỏi.
    /// Dùng khi bắt đầu một lượt game mới.
    /// </summary>
    public static void ResetQuestionIndex()
    {
        currentQuestionIndex = 0;
    }


    // =========================
    // ANSWER
    // =========================

    /// <summary>
    /// Người chơi trả lời đúng.
    /// </summary>
    public static void OnAnswerCorrect()
    {
        if (!string.IsNullOrEmpty(currentEncounterEnemyId))
        {
            deadEnemies.Add(currentEncounterEnemyId);

            killCount++;

            currentEncounterEnemyId = "";
        }
    }


    /// <summary>
    /// Người chơi trả lời sai.
    /// </summary>
    public static void OnAnswerWrong()
    {
        if (hp > 0)
        {
            hp--;
        }
    }


    // =========================
    // KIỂM TRA ĐỘ KHÓ
    // =========================

    /// <summary>
    /// Kiểm tra độ khó này có được người chơi chọn hay không.
    /// </summary>
    public static bool IsDifficultySelected(string difficulty)
    {
        return Difficulties.Contains(difficulty);
    }


    // =========================
    // RESET GAMEPLAY
    // =========================

    /// <summary>
    /// Reset dữ liệu gameplay khi chơi lại.
    ///
    /// KHÔNG reset Subject, SelectedGame,
    /// Difficulties và QuestionType.
    ///
    /// Vì khi vào game, chúng ta vẫn cần
    /// các thông tin đó để lọc câu hỏi.
    /// </summary>
    public static void ResetGameplay()
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


    // =========================
    // RESET TOÀN BỘ
    // =========================

    /// <summary>
    /// Reset toàn bộ session.
    /// Dùng khi quay về Home hoặc bắt đầu
    /// một phiên chơi hoàn toàn mới.
    /// </summary>
    public static void ResetAll()
    {
        // Môn/chương
        Subject = "";
        Chapter = "";

        // Game
        SelectedGame = "";

        // Filter câu hỏi
        Difficulties.Clear();
        QuestionType = "";

        // Gameplay
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