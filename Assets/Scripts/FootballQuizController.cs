using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;

public class FootballQuizController : MonoBehaviour
{
    // =========================================================
    // UI
    // =========================================================

    [Header("Khung câu hỏi")]
    public TMP_Text questionText;

    [Header("4 đáp án theo thứ tự A, B, C, D")]
    public Button[] answerButtons;

    // TMP chứa NỘI DUNG đáp án
    // KHÔNG phải TMP chỉ chứa chữ A/B/C/D
    public TMP_Text[] answerTexts;


    // =========================================================
    // UI TÙY CHỌN
    // =========================================================

    [Header("Tùy chọn")]
    public TMP_Text scoreText;
    public TMP_Text explanationText;


    // =========================================================
    // FOOTBALL
    // =========================================================

    [Header("Sút bóng")]
    public KickBall kickBall;


    // =========================================================
    // GOOGLE SHEET
    // =========================================================

    [Header("Google Apps Script")]
    public string baseUrl = "DÁN_LINK_EXEC_VÀO_ĐÂY";


    // =========================================================
    // GAME SETTINGS
    // =========================================================

    [Header("Thiết lập lượt chơi")]

    [Tooltip("Số câu tối đa trong một lượt. 0 = lấy toàn bộ câu phù hợp.")]
    public int maxQuestions = 10;

    [Tooltip("Sai một câu thì kết thúc game")]
    public bool wrongAnswerEndsGame = true;


    // =========================================================
    // MÀU PHẢN HỒI
    // =========================================================

    [Header("Màu phản hồi")]

    public Color correctColor =
        new Color(0.3f, 0.85f, 0.3f);

    public Color wrongColor =
        new Color(0.95f, 0.3f, 0.3f);

    public float delayNext = 1.8f;


    // =========================================================
    // EVENTS
    // =========================================================

    [Header("Sự kiện")]

    public UnityEvent onCorrect;
    public UnityEvent onWrong;
    public UnityEvent onFinished;


    // =========================================================
    // RUNTIME
    // =========================================================

    private List<QuestionData> questions =
        new List<QuestionData>();

    private Color[] originalColors;

    private int index;

    private int rawScore;
    private int maxRawScore;
    private int correctCount;

    private bool locked;


    // =========================================================
    // AWAKE
    // =========================================================

    void Awake()
    {
        wrongAnswerEndsGame = false;

        originalColors =
            new Color[answerButtons.Length];

        for (int i = 0;
             i < answerButtons.Length;
             i++)
        {
            int idx = i;

            if (answerButtons[i] == null)
                continue;

            Image img =
                answerButtons[i]
                    .GetComponent<Image>();

            originalColors[i] =
                img != null
                    ? img.color
                    : Color.white;

            answerButtons[i]
                .onClick
                .AddListener(
                    () => OnAnswer(idx)
                );
        }
    }


    // =========================================================
    // MỖI KHI FOOTBALL ĐƯỢC BẬT
    // =========================================================

    void OnEnable()
    {
        StopAllCoroutines();

        StartCoroutine(
            LoadQuestions()
        );
    }


    // =========================================================
    // LOAD QUESTIONS
    // =========================================================

    IEnumerator LoadQuestions()
    {
        locked = true;

        SetAnswersVisible(false);


        // =====================================================
        // DEBUG SESSION
        // =====================================================

        Debug.Log(
            "========== FOOTBALL SESSION ==========\n"
            + "Subject = ["
            + GameSession.Subject
            + "]\n"
            + "Chapter = ["
            + GameSession.Chapter
            + "]\n"
            + "Difficulty = ["
            + string.Join(
                ", ",
                GameSession.Difficulties
            )
            + "]\n"
            + "QuestionType = ["
            + GameSession.QuestionType
            + "]"
        );


        // =====================================================
        // UI LOADING
        // =====================================================

        if (questionText != null)
        {
            questionText.text =
                "Đang tải câu hỏi...";
        }

        if (explanationText != null)
        {
            explanationText.text = "";
        }

        if (scoreText != null)
        {
            scoreText.text = "0";
        }


        // =====================================================
        // KIỂM TRA SUBJECT
        // =====================================================

        string mon =
            GameSession.Subject;


        if (
            string.IsNullOrWhiteSpace(mon)
        )
        {
            if (questionText != null)
            {
                questionText.text =
                    "Chưa xác định môn học!";
            }

            Debug.LogError(
                "GameSession.Subject đang trống!"
            );

            yield break;
        }


        // =====================================================
        // KIỂM TRA DIFFICULTY
        // =====================================================

        if (
            GameSession.Difficulties == null
            ||
            GameSession.Difficulties.Count == 0
        )
        {
            if (questionText != null)
            {
                questionText.text =
                    "Chưa chọn độ khó!";
            }

            Debug.LogError(
                "GameSession.Difficulties đang trống!"
            );

            yield break;
        }


        string difficultyString =
            string.Join(
                ",",
                GameSession.Difficulties
            );


        // =====================================================
        // TẠO URL
        // =====================================================

        string url =
            baseUrl
            + "?action=questions"
            + "&subject="
            + UnityWebRequest.EscapeURL(mon)
            + "&difficulty="
            + UnityWebRequest.EscapeURL(
                difficultyString
            );


        Debug.Log(
            "FOOTBALL URL: "
            + url
        );


        // =====================================================
        // REQUEST
        // =====================================================

        using (
            UnityWebRequest req =
                UnityWebRequest.Get(url)
        )
        {
            yield return
                req.SendWebRequest();


            if (
                req.result !=
                UnityWebRequest.Result.Success
            )
            {
                if (questionText != null)
                {
                    questionText.text =
                        "Lỗi kết nối: "
                        + req.error;
                }

                Debug.LogError(
                    "FOOTBALL REQUEST ERROR: "
                    + req.error
                );

                yield break;
            }


            Debug.Log(
                "FOOTBALL JSON: "
                + req.downloadHandler.text
            );


            // =================================================
            // PARSE JSON
            // =================================================

            QuestionList list =
                JsonUtility
                    .FromJson<QuestionList>(
                        req.downloadHandler.text
                    );


            if (
                list == null
                ||
                !list.success
                ||
                list.questions == null
                ||
                list.questions.Length == 0
            )
            {
                if (questionText != null)
                {
                    questionText.text =
                        "Không có câu hỏi phù hợp";
                }

                Debug.LogError(
                    "Không có câu hỏi phù hợp.\n"
                    + req.downloadHandler.text
                );

                yield break;
            }


            // =================================================
            // BUILD QUESTION SET
            // =================================================

            questions =
                BuildQuestionSet(
                    list.questions
                );


            if (
                questions == null
                ||
                questions.Count == 0
            )
            {
                if (questionText != null)
                {
                    questionText.text =
                        "Không có câu hỏi phù hợp với môn/chương/độ khó đã chọn.";
                }

                Debug.LogError(
                    "BuildQuestionSet trả về 0 câu."
                );

                yield break;
            }


            // =================================================
            // TOTAL SCORE
            // =================================================

            maxRawScore = 0;

            foreach (
                QuestionData q in questions
            )
            {
                maxRawScore +=
                    GetPoints(q);
            }


            // =================================================
            // RESET GAME
            // =================================================

            index = 0;
            rawScore = 0;
            correctCount = 0;
            locked = false;


            Debug.Log(
                "Football lấy được "
                + questions.Count
                + " câu."
            );


            foreach (
                QuestionData q in questions
            )
            {
                Debug.Log(
                    "POOL => "
                    + q.id
                    + " | "
                    + q.monId
                    + " | "
                    + q.topic
                    + " | "
                    + q.difficulty
                    + " | "
                    + q.question
                );
            }


            ShowQuestion();
        }
    }


    // =========================================================
    // BUILD QUESTION SET
    // =========================================================

    List<QuestionData> BuildQuestionSet(
        QuestionData[] all
    )
    {
        List<QuestionData> pool =
            new List<QuestionData>();


        // =====================================================
        // CHAPTER
        // =====================================================

        string ch =
            GameSession.Chapter;


        Debug.Log(
            "Football filter chapter = ["
            + ch
            + "]"
        );


        // =====================================================
        // FILTER
        // =====================================================

        foreach (
            QuestionData q in all
        )
        {
            if (q == null)
                continue;


            // =================================================
            // SUBJECT CHECK
            // =================================================

            bool subjectOK =
                string.Equals(
                    q.monId?.Trim(),
                    GameSession.Subject?.Trim(),
                    System.StringComparison
                        .OrdinalIgnoreCase
                );


            if (!subjectOK)
            {
                continue;
            }


            // =================================================
            // CHAPTER CHECK
            // =================================================

            bool chapterOK = true;


            if (
                !string.IsNullOrWhiteSpace(ch)
            )
            {
                chapterOK =
                    !string.IsNullOrWhiteSpace(
                        q.topic
                    )
                    &&
                    q.topic
                        .Trim()
                        .StartsWith(
                            ch.Trim(),
                            System.StringComparison
                                .OrdinalIgnoreCase
                        );
            }


            if (!chapterOK)
            {
                continue;
            }


            // =================================================
            // DIFFICULTY CHECK
            // =================================================

            bool difficultyOK = false;


            foreach (
                string selected
                in GameSession.Difficulties
            )
            {
                if (
                    string.Equals(
                        q.difficulty?.Trim(),
                        selected?.Trim(),
                        System.StringComparison
                            .OrdinalIgnoreCase
                    )
                )
                {
                    difficultyOK = true;
                    break;
                }
            }


            if (!difficultyOK)
            {
                continue;
            }


            pool.Add(q);
        }


        // =====================================================
        // ĐẢM BẢO ĐỦ SỐ CÂU (VÍ DỤ 10 CÂU)
        // =====================================================

        if (maxQuestions > 0 && pool.Count < maxQuestions)
        {
            foreach (QuestionData q in all)
            {
                if (q == null) continue;

                bool subjectOK = string.Equals(
                    q.monId?.Trim(),
                    GameSession.Subject?.Trim(),
                    System.StringComparison.OrdinalIgnoreCase
                );

                if (subjectOK && !pool.Contains(q))
                {
                    pool.Add(q);
                    if (pool.Count >= maxQuestions)
                        break;
                }
            }
        }

        // =====================================================
        // RANDOM
        // =====================================================

        Shuffle(pool);


        // =====================================================
        // GIỚI HẠN SỐ CÂU
        // =====================================================

        if (
            maxQuestions > 0
            &&
            pool.Count > maxQuestions
        )
        {
            pool =
                pool.GetRange(
                    0,
                    maxQuestions
                );
        }


        return pool;
    }


    // =========================================================
    // SHOW QUESTION
    // =========================================================

    void ShowQuestion()
    {
        if (
            questions == null
            ||
            questions.Count == 0
        )
        {
            return;
        }


        if (
            index >= questions.Count
        )
        {
            Finish();
            return;
        }


        QuestionData q =
            questions[index];


        locked = false;


        // =====================================================
        // DEBUG CHÍNH XÁC CÂU ĐANG HIỂN THỊ
        // =====================================================

        Debug.Log(
            "FOOTBALL ĐANG HIỂN THỊ => "
            + q.id
            + " | môn: "
            + q.monId
            + " | chương: "
            + q.topic
            + " | độ khó: "
            + q.difficulty
            + " | câu: "
            + q.question
        );


        // =====================================================
        // QUESTION
        // =====================================================

        if (questionText != null)
        {
            questionText.text =
                q.question;
        }


        // =====================================================
        // ANSWER TEXTS
        // =====================================================

        string[] opts =
        {
            q.optionA,
            q.optionB,
            q.optionC,
            q.optionD
        };


        for (
            int i = 0;
            i < answerButtons.Length;
            i++
        )
        {
            if (
                i < answerTexts.Length
                &&
                answerTexts[i] != null
            )
            {
                answerTexts[i].text =
                    i < opts.Length
                        ? opts[i]
                        : "";
            }


            if (
                answerButtons[i] != null
            )
            {
                Image img =
                    answerButtons[i]
                        .GetComponent<Image>();


                if (img != null)
                {
                    img.color =
                        originalColors[i];
                }
            }
        }


        if (
            explanationText != null
        )
        {
            explanationText.text = "";
        }


        UpdateScore();

        SetAnswersVisible(true);
    }


    // =========================================================
    // ANSWER
    // =========================================================

    void OnAnswer(
        int chosen
    )
    {
        if (locked)
            return;


        if (
            index < 0
            ||
            index >= questions.Count
        )
        {
            return;
        }


        locked = true;


        QuestionData q =
            questions[index];


        int correctIndex =
            ParseLetter(
                q.correct
            );


        bool isCorrect =
            chosen == correctIndex;


        // =====================================================
        // COLOR
        // =====================================================

        SetButtonColor(
            correctIndex,
            correctColor
        );


        if (!isCorrect)
        {
            SetButtonColor(
                chosen,
                wrongColor
            );
        }


        // =====================================================
        // CORRECT
        // =====================================================

        if (isCorrect)
        {
            rawScore +=
                GetPoints(q);

            correctCount++;

            onCorrect?.Invoke();
        }
        else
        {
            onWrong?.Invoke();
        }


        // =====================================================
        // KICK BALL
        // =====================================================

        if (
            kickBall != null
        )
        {
            if (isCorrect)
            {
                kickBall.Goal();
            }
            else
            {
                kickBall.Miss();
            }
        }


        // =====================================================
        // EXPLANATION
        // =====================================================

        if (
            explanationText != null
        )
        {
            explanationText.text =
                q.explanation;
        }


        UpdateScore();


        StartCoroutine(
            NextAfterDelay(
                isCorrect
            )
        );
    }


    // =========================================================
    // NEXT
    // =========================================================

    IEnumerator NextAfterDelay(
        bool wasCorrect
    )
    {
        if (
            kickBall != null
        )
        {
            yield return null;


            while (
                kickBall.IsBusy
            )
            {
                yield return null;
            }


            yield return
                new WaitForSeconds(
                    0.5f
                );
        }
        else
        {
            yield return
                new WaitForSeconds(
                    delayNext
                );
        }


        // =====================================================
        // WRONG = GAME OVER (Đã tắt để người chơi làm đủ 10 câu)
        // =====================================================

        /*
        if (
            !wasCorrect
            &&
            wrongAnswerEndsGame
        )
        {
            GameOver();

            yield break;
        }
        */


        // =====================================================
        // NEXT
        // =====================================================

        index++;


        if (
            index >= questions.Count
        )
        {
            Finish();
        }
        else
        {
            ShowQuestion();
        }
    }


    // =========================================================
    // GAME OVER
    // =========================================================

    void GameOver()
    {
        locked = true;


        SetAnswersVisible(false);


        if (
            questionText != null
        )
        {
            questionText.text =
                "Bạn đã trả lời sai!\n"
                + "Điểm: "
                + rawScore
                + "/"
                + maxRawScore
                + " điểm";
        }


        Debug.Log(
            "FOOTBALL GAME OVER"
        );


        onFinished?.Invoke();
    }


    // =========================================================
    // FINISH
    // =========================================================

    void Finish()
    {
        locked = true;


        SetAnswersVisible(false);


        int score100 =
            GetScoreOutOf100();


        if (
            questionText != null
        )
        {
            questionText.text =
                "Hoàn thành!\n"
+ "Bạn được " + rawScore + "/" + maxRawScore + " điểm"
+ "\nĐúng " + correctCount + "/" + questions.Count + " câu";
        }


        if (
            explanationText != null
        )
        {
            explanationText.text = "";
        }


        UpdateScore();


        onFinished?.Invoke();
    }


    // =========================================================
    // SCORE 0 - 100
    // =========================================================

    int GetScoreOutOf100()
    {
        if (
            maxRawScore <= 0
        )
        {
            return 0;
        }


        float percent =
            (float)rawScore
            /
            maxRawScore;


        return Mathf.RoundToInt(
            percent * 100f
        );
    }


    // =========================================================
    // POINT
    // =========================================================

    int GetPoints(
        QuestionData q
    )
    {
        return q.points > 0
            ? q.points
            : 10;
    }


    // =========================================================
    // A=0 B=1 C=2 D=3
    // =========================================================

    int ParseLetter(
        string s
    )
    {
        if (
            string.IsNullOrWhiteSpace(s)
        )
        {
            return -1;
        }


        char c =
            char.ToUpper(
                s.Trim()[0]
            );


        return c - 'A';
    }


    // =========================================================
    // BUTTON COLOR
    // =========================================================

    void SetButtonColor(
        int i,
        Color color
    )
    {
        if (
            i < 0
            ||
            i >= answerButtons.Length
        )
        {
            return;
        }


        if (
            answerButtons[i] == null
        )
        {
            return;
        }


        Image img =
            answerButtons[i]
                .GetComponent<Image>();


        if (
            img != null
        )
        {
            img.color =
                color;
        }
    }


    // =========================================================
    // SHOW/HIDE ANSWERS
    // =========================================================

    void SetAnswersVisible(
        bool show
    )
    {
        foreach (
            Button button
            in answerButtons
        )
        {
            if (
                button != null
            )
            {
                button
                    .gameObject
                    .SetActive(show);
            }
        }
    }


    // =========================================================
    // SCORE
    // =========================================================

    void UpdateScore()
    {
        if (
            scoreText != null
        )
        {
            scoreText.text =
                rawScore
                .ToString();
        }
    }


    // =========================================================
    // SHUFFLE
    // =========================================================

    void Shuffle(
        List<QuestionData> list
    )
    {
        for (
            int i =
                list.Count - 1;
            i > 0;
            i--
        )
        {
            int j =
                Random.Range(
                    0,
                    i + 1
                );


            QuestionData temp =
                list[i];


            list[i] =
                list[j];


            list[j] =
                temp;
        }
    }
}