using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;

public class FootballQuizController : MonoBehaviour
{
    [Header("Khung câu hỏi")]
    public TMP_Text questionText;          // chữ trong khung xanh trên cùng

    [Header("4 đáp án, theo thứ tự A, B, C, D")]
    public Button[] answerButtons;         // 4 nút A B C D
    public TMP_Text[] answerTexts;         // chữ trong từng nút (KHÔNG phải chữ A/B/C/D)

    [Header("Tùy chọn")]
    public TMP_Text scoreText;             // hiện điểm (có thể để trống)
    public TMP_Text explanationText;       // hiện giải thích (có thể để trống)

    [Header("Sút bóng")]
    public KickBall kickBall;              // kéo object đang gắn script KickBall vào

    [Header("Dữ liệu")]
    public string baseUrl = "DÁN_LINK_EXEC_VÀO_ĐÂY";
    public string subject = "Vật lý";      // lọc theo cột monID (dùng khi test riêng game)
    public string chapter = "Chương 1";    // lọc theo cột Chương/Chủ đề (để trống = lấy cả môn)

    [Header("Cơ cấu câu hỏi mỗi lượt")]
    public int easyCount = 3;              // câu Dễ (10 điểm)
    public int mediumCount = 2;            // câu Trung bình (20 điểm)
    public int hardCount = 1;              // câu Khó (30 điểm)
    public int targetPoints = 100;         // tổng điểm mong muốn

    [Header("Màu phản hồi")]
    public Color correctColor = new Color(0.3f, 0.85f, 0.3f);
    public Color wrongColor = new Color(0.95f, 0.3f, 0.3f);
    public float delayNext = 1.8f;

    [Header("Sự kiện (gắn animation sút bóng, thủ môn...)")]
    public UnityEvent onCorrect;
    public UnityEvent onWrong;
    public UnityEvent onFinished;

    List<QuestionData> questions = new List<QuestionData>();
    Color[] originalColors;
    int index;
    int score;          // tổng điểm
    int maxScore;       // tổng điểm tối đa của bộ câu hỏi lượt này
    int correctCount;   // số câu đúng
    bool locked;

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
        StartCoroutine(LoadQuestions());
    }

    IEnumerator LoadQuestions()
    {
        SetAnswersVisible(false);
        questionText.text = "Đang tải câu hỏi...";
        if (explanationText) explanationText.text = "";
        if (scoreText) scoreText.text = "";

        // Ưu tiên môn do màn lý thuyết gửi sang; nếu trống thì dùng ô Subject (để test riêng game)
        string mon = string.IsNullOrEmpty(GameSession.Subject) ? subject : GameSession.Subject;
        string url = baseUrl + "?action=questions&mon=" + UnityWebRequest.EscapeURL(mon);
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                questionText.text = "Lỗi kết nối: " + req.error;
                yield break;
            }

            QuestionList list = JsonUtility.FromJson<QuestionList>(req.downloadHandler.text);
            if (list == null || !list.success || list.questions == null || list.questions.Length == 0)
            {
                questionText.text = "Không có câu hỏi";
                Debug.Log(req.downloadHandler.text);
                yield break;
            }

            questions = BuildQuestionSet(list.questions);
            if (questions.Count == 0)
            {
                questionText.text = "Không có câu hỏi cho chương này";
                yield break;
            }

            maxScore = 0;
            foreach (var q in questions) maxScore += GetPoints(q);

            index = 0;
            score = 0;
            correctCount = 0;
            ShowQuestion();
        }
    }

    // Chọn câu hỏi: lọc theo chương, lấy đúng số câu Dễ/TB/Khó, bù theo điểm nếu thiếu
    List<QuestionData> BuildQuestionSet(QuestionData[] all)
    {
        // Chương ưu tiên lấy từ màn lý thuyết; nếu trống thì dùng ô Chapter (test riêng game)
        string ch = string.IsNullOrEmpty(GameSession.Chapter) ? chapter : GameSession.Chapter;

        // 1) Lọc theo chương
        var pool = new List<QuestionData>();
        foreach (var q in all)
        {
            if (string.IsNullOrEmpty(ch) ||
                (q.topic != null && q.topic.Trim().StartsWith(ch.Trim(), System.StringComparison.OrdinalIgnoreCase)))
                pool.Add(q);
        }

        // 2) Chia theo độ khó
        var easy = new List<QuestionData>();
        var medium = new List<QuestionData>();
        var hard = new List<QuestionData>();
        foreach (var q in pool)
        {
            string d = (q.difficulty ?? "").Trim().ToLower();
            if (d == "dễ") easy.Add(q);
            else if (d == "trung bình") medium.Add(q);
            else if (d == "khó") hard.Add(q);
        }

        Shuffle(easy); Shuffle(medium); Shuffle(hard);

        // 3) Lấy đúng số lượng từng loại
        var result = new List<QuestionData>();
        Take(easy, easyCount, result);
        Take(medium, mediumCount, result);
        Take(hard, hardCount, result);

        // 4) Thiếu câu (chương thiếu một độ khó) thì bù theo điểm, không vượt targetPoints
        int total = 0;
        foreach (var q in result) total += GetPoints(q);

        if (total < targetPoints)
        {
            var rest = new List<QuestionData>();
            foreach (var q in pool) if (!result.Contains(q)) rest.Add(q);
            Shuffle(rest);
            foreach (var q in rest)
            {
                int p = GetPoints(q);
                if (total + p <= targetPoints)
                {
                    result.Add(q);
                    total += p;
                    if (total == targetPoints) break;
                }
            }
        }

        // 5) Xáo thứ tự để dễ/khó không xếp theo cục
        Shuffle(result);
        return result;
    }

    void Take(List<QuestionData> source, int n, List<QuestionData> dest)
    {
        for (int i = 0; i < n && i < source.Count; i++) dest.Add(source[i]);
    }

    int GetPoints(QuestionData q)
    {
        return q.points > 0 ? q.points : 10;   // điểm theo sheet, thiếu thì mặc định 10
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
            score += GetPoints(q);
            correctCount++;
            onCorrect?.Invoke();
        }
        else onWrong?.Invoke();

        if (kickBall)
        {
            if (isCorrect) kickBall.Goal();   // đúng: bóng vào gôn
            else kickBall.Miss();             // sai: bóng bay trượt
        }

        if (explanationText) explanationText.text = q.explanation;
        UpdateScore();
        StartCoroutine(NextAfterDelay());
    }

    IEnumerator NextAfterDelay()
    {
        if (kickBall)
        {
            yield return null;                          // cho KickBall kịp bắt đầu
            while (kickBall.IsBusy) yield return null;  // đợi bóng sút xong
            yield return new WaitForSeconds(0.5f);      // thêm chút để đọc giải thích
        }
        else
        {
            yield return new WaitForSeconds(delayNext);
        }
        index++;
        if (index >= questions.Count) Finish();
        else ShowQuestion();
    }

    void Finish()
    {
        SetAnswersVisible(false);
        questionText.text = "Hoàn thành! Bạn được " + score + "/" + maxScore + " điểm (đúng " + correctCount + "/" + questions.Count + " câu)";
        if (explanationText) explanationText.text = "";
        onFinished?.Invoke();
    }

    // ---------- tiện ích ----------
    int ParseLetter(string s)
    {
        if (string.IsNullOrEmpty(s)) return -1;
        char c = char.ToUpper(s.Trim()[0]);
        return c - 'A';
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
        if (scoreText) scoreText.text = "" + score;
    }

    void Shuffle(List<QuestionData> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var tmp = list[i]; list[i] = list[j]; list[j] = tmp;
        }
    }
}