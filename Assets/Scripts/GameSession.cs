public static class GameSession
{
    // Môn học và chương
    public static string Subject = "";
    public static string Chapter = "";

    // Máu của người chơi
    public static int hp = 3;

    // Scene game cần quay lại sau khi trả lời câu hỏi
    public static string gameplaySceneName = "SampleScene";

    // Vị trí câu hỏi hiện tại
    private static int currentQuestionIndex = 0;

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
        // Trả lời đúng thì không mất máu.
        // QuestionController sẽ chuyển người chơi về game.
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
    }
}