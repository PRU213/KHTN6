using System.Collections.Generic;
using UnityEngine;

public static class GameSession
{
    // =========================
    // THÔNG TIN TÀI KHOẢN & VAI TRÒ (SESSION)
    // =========================

    public static string Role = "";
    public static string Username = "";
    public static string Fullname = "";

    /// <summary>
    /// Kiểm tra người dùng hiện tại có phải là Giáo viên không.
    /// </summary>
    public static bool IsTeacher()
    {
        return !string.IsNullOrEmpty(Role) &&
               (Role.Equals("Teacher", System.StringComparison.OrdinalIgnoreCase) ||
                Role.Equals("GiaoVien", System.StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Kiểm tra người dùng hiện tại có phải là Học sinh không.
    /// </summary>
    public static bool IsStudent()
    {
        return !IsTeacher();
    }

    /// <summary>
    /// Xóa thông tin phiên người dùng khi đăng xuất.
    /// </summary>
    public static void ClearUserSession()
    {
        Role = "";
        Username = "";
        Fullname = "";
    }


    // =========================
    // MÔN HỌC & CHƯƠNG
    // =========================

    private static string _subject = "";
    public static string Subject
    {
        get => NormalizeSubject(_subject);
        set => _subject = NormalizeSubject(value);
    }
    public static string Chapter = "";

    /// <summary>
    /// Chuẩn hóa tên môn học sang định dạng chuẩn trong Google Sheet ("Vật lý", "Hóa học", "Sinh học").
    /// </summary>
    public static string NormalizeSubject(string sub)
    {
        if (string.IsNullOrWhiteSpace(sub)) return "";
        string s = sub.Trim().ToLowerInvariant();
        if (s.Contains("vật") || s.Contains("vat") || s.Contains("lý") || s.Contains("ly") || s.Contains("physic"))
            return "Vật lý";
        if (s.Contains("hóa") || s.Contains("hoá") || s.Contains("hoa") || s.Contains("chem"))
            return "Hóa học";
        if (s.Contains("sinh") || s.Contains("bio"))
            return "Sinh học";
        return sub.Trim();
    }

    /// <summary>
    /// So khớp 2 tên môn học (chấp nhận cả tiếng Anh, tiếng Việt không dấu, có dấu).
    /// </summary>
    public static bool IsMatchingSubject(string mon1, string mon2)
    {
        if (string.IsNullOrWhiteSpace(mon1) || string.IsNullOrWhiteSpace(mon2)) return true;
        string norm1 = NormalizeSubject(mon1);
        string norm2 = NormalizeSubject(mon2);
        return string.Equals(norm1, norm2, System.StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// So khớp chương giữa câu hỏi trong sheet và chương đang chọn trong session.
    /// </summary>
    public static bool IsMatchingChapter(string questionTopic, string sessionChapter)
    {
        if (string.IsNullOrWhiteSpace(sessionChapter)) return true;
        string ch = sessionChapter.Trim();
        if (ch.IndexOf("Tất cả", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            ch.IndexOf("Tat ca", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            ch.Equals("All", System.StringComparison.OrdinalIgnoreCase))
            return true;

        if (string.IsNullOrWhiteSpace(questionTopic)) return true;
        string topic = questionTopic.Trim();

        var mSession = System.Text.RegularExpressions.Regex.Match(ch, @"(?:Chương|Chuong|Chapter)\s*(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        var mTopic = System.Text.RegularExpressions.Regex.Match(topic, @"(?:Chương|Chuong|Chapter)\s*(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (mSession.Success && mTopic.Success)
        {
            return mSession.Groups[1].Value == mTopic.Groups[1].Value;
        }

        return topic.StartsWith(ch, System.StringComparison.OrdinalIgnoreCase) ||
               ch.StartsWith(topic, System.StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// So khớp độ khó giữa câu hỏi và danh sách độ khó được chọn.
    /// </summary>
    public static bool IsMatchingDifficulty(string questionDiff, List<string> selectedDiffs)
    {
        if (selectedDiffs == null || selectedDiffs.Count == 0) return true;
        foreach (var d in selectedDiffs)
        {
            if (string.IsNullOrWhiteSpace(d)) continue;
            if (d.IndexOf("Tổng hợp", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                d.IndexOf("Tong hop", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                d.Equals("All", System.StringComparison.OrdinalIgnoreCase))
                return true;

            if (string.Equals(questionDiff?.Trim(), d?.Trim(), System.StringComparison.OrdinalIgnoreCase))
                return true;

            if (d.IndexOf("Dễ", System.StringComparison.OrdinalIgnoreCase) >= 0 && questionDiff.IndexOf("Dễ", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (d.IndexOf("Trung", System.StringComparison.OrdinalIgnoreCase) >= 0 && questionDiff.IndexOf("Trung", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (d.IndexOf("Khó", System.StringComparison.OrdinalIgnoreCase) >= 0 && questionDiff.IndexOf("Khó", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
        }
        return false;
    }


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
    /// Reset toàn bộ session gameplay.
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
