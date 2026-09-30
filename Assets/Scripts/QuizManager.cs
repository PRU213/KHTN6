using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class QuizManager : MonoBehaviour
{
    [System.Serializable]
    public class Question
    {
        [TextArea] public string text;
        public string[] answers = new string[4];   // A, B, C, D
        [Range(0, 3)] public int correctIndex;     // 0=A, 1=B, 2=C, 3=D
    }

    [Header("UI")]
    public GameObject qsPanel;          // QsPanel1
    public TMP_Text questionText;       // QuestionText
    public Button[] answerButtons;      // 4 nút A B C D
    public TMP_Text[] answerTexts;      // chữ trên 4 nút (cùng thứ tự)

    [Header("Cấu hình")]
    public int passesPerQuiz = 5;       // qua bao nhiêu cột thì hiện câu hỏi
    public List<Question> questions = new List<Question>();

    [Header("Sự kiện")]
    public UnityEvent onCorrect;        // ví dụ: cộng điểm
    public UnityEvent onWrong;          // ví dụ: trừ tim / game over

    int passCount;
    bool isAsking;
    readonly List<int> pool = new List<int>();

    void Awake()
    {
        for (int i = 0; i < answerButtons.Length; i++)
        {
            int index = i; // giữ đúng giá trị i cho từng nút
            answerButtons[i].onClick.AddListener(() => OnAnswer(index));
        }
        qsPanel.SetActive(false);
    }

    // Gọi hàm này mỗi lần người chơi vượt qua 1 cột / 1 chặng
    public void OnPassedObstacle()
    {
        if (isAsking) return;
        passCount++;
        if (passCount >= passesPerQuiz)
        {
            passCount = 0;
            ShowQuestion();
        }
    }

    void ShowQuestion()
    {
        if (questions.Count == 0) return;

        // Lấy ngẫu nhiên không lặp cho đến khi hết câu
        if (pool.Count == 0)
            for (int i = 0; i < questions.Count; i++) pool.Add(i);
        int pick = Random.Range(0, pool.Count);
        Question q = questions[pool[pick]];
        pool.RemoveAt(pick);

        questionText.text = q.text;
        for (int i = 0; i < answerButtons.Length; i++)
        {
            answerTexts[i].text = q.answers[i];
            answerButtons[i].image.color = Color.white;
            answerButtons[i].interactable = true;
        }

        currentCorrect = q.correctIndex;
        isAsking = true;
        qsPanel.SetActive(true);
        Time.timeScale = 0f; // tạm dừng game
    }

    int currentCorrect;

    void OnAnswer(int index)
    {
        if (!isAsking) return;
        foreach (var b in answerButtons) b.interactable = false;
        StartCoroutine(ResolveRoutine(index));
    }

    IEnumerator ResolveRoutine(int index)
    {
        bool correct = index == currentCorrect;
        answerButtons[index].image.color = correct ? Color.green : Color.red;
        if (!correct) answerButtons[currentCorrect].image.color = Color.green;

        yield return new WaitForSecondsRealtime(1f); // timeScale=0 nên phải dùng Realtime

        qsPanel.SetActive(false);
        isAsking = false;
        Time.timeScale = 1f; // chạy tiếp

        if (correct) onCorrect?.Invoke();
        else onWrong?.Invoke();
    }
}