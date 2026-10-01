using System.Collections.Generic;
using UnityEngine;

// Lưu trữ trạng thái giữa các màn chơi (Gameplay và Câu hỏi)
public static class GameSession
{
    public static string gameplaySceneName = "Bean_2_Biology";
    public static string questionSceneName = "Bean_2_Biology_Question";
    
    // Vị trí người chơi trước khi chạm trán quái vật
    public static Vector2 returnPosition = Vector2.zero;
    
    // Vị trí bắt đầu của người chơi khi mới vào game
    public static Vector2 startPosition = Vector2.zero;
    
    public static string currentEnemyId = "";
    public static HashSet<string> killedEnemies = new HashSet<string>();
    
    public static int killCount = 0;
    public static int totalEnemies = 6;
    
    public static bool hasBean = false;
    public static bool beanSpawned = false;
    
    public static int hp = 3;
    public static int questionIndex = 0;

    // Bắt đầu chạm trán với quái vật
    public static void StartEncounter(string enemyId, Vector2 playerPos)
    {
        currentEnemyId = enemyId;
        returnPosition = playerPos;
        hp = 3; // Reset lại số tim (mạng) khi vào câu hỏi
    }

    // Xử lý khi trả lời đúng
    public static void OnAnswerCorrect()
    {
        if (!string.IsNullOrEmpty(currentEnemyId))
        {
            killedEnemies.Add(currentEnemyId);
            killCount++;
        }
    }

    // Xử lý khi trả lời sai (Trả về số máu còn lại)
    public static int OnAnswerWrong()
    {
        hp--;
        if (hp < 0) hp = 0;
        return hp;
    }

    // Kiểm tra xem quái vật đã bị tiêu diệt chưa
    public static bool IsEnemyDead(string id)
    {
        return killedEnemies.Contains(id);
    }

    // Reset lại toàn bộ trạng thái khi Game Over
    public static void ResetAll()
    {
        returnPosition = Vector2.zero;
        startPosition = Vector2.zero;
        currentEnemyId = "";
        killedEnemies.Clear();
        killCount = 0;
        hasBean = false;
        beanSpawned = false;
        hp = 3;
        questionIndex = 0;
    }

    // Lấy câu hỏi tiếp theo
    public static int GetNextQuestionIndex()
    {
        int currentIndex = questionIndex;
        questionIndex++;
        return currentIndex;
    }
}
