using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SheepQuizController : MonoBehaviour
{
    [Header("Khung câu hỏi")]
    public TMP_Text questionText;

    [Header("4 đáp án, theo thứ tự A, B, C, D")]
    public Button[] answerButtons;         // 4 nút answerA..answerD
    public TMP_Text[] answerTexts;         // chữ trong từng nút (KHÔNG phải chữ A/B/C/D)

    [Header("Tùy chọn")]
    public TMP_Text scoreText;             // có thể để trống
    public TMP_Text explanationText;       // có thể để trống

    [Header("Dữ liệu")]
    public string baseUrl = "DÁN_LINK_EXEC_VÀO_ĐÂY";
    public string subject = "Vật lý";      // chỉ dùng khi test riêng game
    public string chapter = "Chương 1";    // chỉ dùng khi test riêng game

    [Header("Cơ cấu câu hỏi mỗi lượt")]
    public int easyCount = 3;
    public int mediumCount = 2;
    public int hardCount = 1;
    public int targetPoints = 100;

    [Header("Màu phản hồi")]
    public Color correctColor = new Color(0.3f, 0.85f, 0.3f);
    public Color wrongColor = new Color(0.95f, 0.3f, 0.3f);
    public float delayNext = 1.8f;

    [Header("Sự kiện (gắn animation cho nhân vật, cừu...)")]
    public UnityEvent onCorrect;
    public UnityEvent onWrong;
    public UnityEvent onFinished;

    List<QuestionData> questions = new List<QuestionData>();
    Color[] originalColors;
    int index;
    int score;
    int maxScore;
    int correctCount;
    bool locked;

    float shownScore;
    Coroutine countRoutine;

    void Awake()
    {
        originalColors = new Color[answerButtons.Length];
        for (int i = 0; i < answerButtons.Length; i++)
        {
            int idx = i;
            var img = answerButtons[i].GetComponent<Image>();
            originalColors[i] = img ? img.color : Color.white;
            answerButtons[i].onClick.AddListener(() => OnAnswer(idx));
        }
    }

    void OnEnable()
    {
        StartCoroutine(Begin());
    }

    IEnumerator Begin()
    {
        SetAnswersVisible(false);
        questionText.text = "Đang tải câu hỏi...";
        if (explanationText) explanationText.text = "";
        if (scoreText) scoreText.text = "0";

        List<QuestionData> loaded = null;
        string error = null;
        bool finished = false;
        string mon = string.IsNullOrEmpty(GameSession.Subject) ? subject : GameSession.Subject;
        string ch = string.IsNullOrEmpty(GameSession.Chapter) ? chapter : GameSession.Chapter;

        yield return StartCoroutine(QuestionSetLoader.Load(
            baseUrl, subject, chapter,
            easyCount, mediumCount, hardCount, targetPoints,
            (set, err) => { loaded = set; error = err; finished = true; }));

        if (!finished || loaded == null)
        {
            questionText.text = string.IsNullOrEmpty(error) ? "Không tải được câu hỏi" : error;
            yield break;
        }

        questions = loaded;
        maxScore = 0;
        foreach (var q in questions) maxScore += QuestionSetLoader.GetPoints(q);

        index = 0;
        score = 0;
        shownScore = 0;
        correctCount = 0;
        ShowQuestion();
    }

    void ShowQuestion()
    {
        var q = questions[index];
        locked = false;

        questionText.text = q.question;
        string[] opts = { q.optionA, q.optionB, q.optionC, q.optionD };
        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (i < answerTexts.Length && answerTexts[i]) answerTexts[i].text = opts[i];
            var img = answerButtons[i].GetComponent<Image>();
            if (img) img.color = originalColors[i];
        }

        if (explanationText) explanationText.text = "";
        UpdateScore();
        SetAnswersVisible(true);
    }

    void OnAnswer(int chosen)
    {
        if (locked) return;
        locked = true;

        var q = questions[index];
        int correctIndex = ParseLetter(q.correct);
        bool isCorrect = chosen == correctIndex;

        SetButtonColor(correctIndex, correctColor);
        if (!isCorrect) SetButtonColor(chosen, wrongColor);

        if (isCorrect)
        {
            score += QuestionSetLoader.GetPoints(q);
            correctCount++;
            onCorrect?.Invoke();
        }
        else onWrong?.Invoke();

        if (explanationText) explanationText.text = q.explanation;
        UpdateScore();
        StartCoroutine(NextAfterDelay());
    }

    IEnumerator NextAfterDelay()
    {
        yield return new WaitForSeconds(delayNext);
        index++;
        if (index >= questions.Count) Finish();
        else ShowQuestion();
    }

    void Finish()
    {
        SetAnswersVisible(false);
        questionText.text = "Hoàn thành! Bạn được " + score + "/" + maxScore +
                            " điểm (đúng " + correctCount + "/" + questions.Count + " câu)";
        if (explanationText) explanationText.text = "";
        onFinished?.Invoke();
    }

    // ---------- tiện ích ----------
    int ParseLetter(string s)
    {
        if (string.IsNullOrEmpty(s)) return -1;
        return char.ToUpper(s.Trim()[0]) - 'A';
    }

    void SetButtonColor(int i, Color c)
    {
        if (i < 0 || i >= answerButtons.Length) return;
        var img = answerButtons[i].GetComponent<Image>();
        if (img) img.color = c;
    }

    void SetAnswersVisible(bool show)
    {
        foreach (var b in answerButtons) b.gameObject.SetActive(show);
    }

    void UpdateScore()
    {
        if (!scoreText) return;
        if (countRoutine != null) StopCoroutine(countRoutine);
        countRoutine = StartCoroutine(CountTo(score));
    }

    IEnumerator CountTo(int target)
    {
        while (Mathf.Abs(shownScore - target) > 0.01f)
        {
            shownScore = Mathf.MoveTowards(shownScore, target, 120f * Time.deltaTime);
            scoreText.text = Mathf.RoundToInt(shownScore).ToString();
            yield return null;
        }
        shownScore = target;
        scoreText.text = target.ToString();
    }
}