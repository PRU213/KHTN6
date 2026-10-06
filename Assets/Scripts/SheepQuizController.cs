using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;

public class SheepQuizController : MonoBehaviour
{
    // =========================================================
    // UI
    // =========================================================

    [Header("Khung câu hỏi")]
    public TMP_Text questionText;

    [Header("4 đáp án, theo thứ tự A, B, C, D")]
    public Button[] answerButtons;

    // TMP nội dung đáp án, không phải chữ A/B/C/D
    public TMP_Text[] answerTexts;


    // =========================================================
    // UI TÙY CHỌN
    // =========================================================

    [Header("Tùy chọn")]
    public TMP_Text scoreText;
    public TMP_Text explanationText;


    // =========================================================
    // GOOGLE SHEET
    // =========================================================

    [Header("Google Apps Script")]
    public string baseUrl = "DÁN_LINK_EXEC_VÀO_ĐÂY";


    // =========================================================
    // THIẾT LẬP GAME
    // =========================================================

    [Header("Thiết lập lượt chơi")]

    [Tooltip("Số câu tối đa trong một lượt. 0 = lấy toàn bộ câu phù hợp.")]
    public int maxQuestions = 10;

    [Tooltip("Sai một câu thì kết thúc game")]
    public bool wrongAnswerEndsGame = false;


    // =========================================================
    // MÀU
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

    private float shownScore;
    private Coroutine countRoutine;


    // =========================================================
    // AWAKE
    // =========================================================

    void Awake()
    {
        originalColors =
            new Color[answerButtons.Length];

        for (
            int i = 0;
            i < answerButtons.Length;
            i++
        )
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
    // ON ENABLE
    // =========================================================

    void OnEnable()
    {
        StopAllCoroutines();

        StartCoroutine(
            Begin()
        );
    }


    // =========================================================
    // LOAD QUESTIONS
    // =========================================================

    IEnumerator Begin()
    {
        locked = true;

        SetAnswersVisible(false);


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
        // DEBUG SESSION
        // =====================================================

        Debug.Log(
            "========== SHEEP SESSION ==========\n"
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
            + "]\n"
            + "SelectedGame = ["
            + GameSession.SelectedGame
            + "]"
        );


        // =====================================================
        // SUBJECT
        // =====================================================

        string mon =
            GameSession.Subject;


        if (
            string.IsNullOrWhiteSpace(mon)
        )
        {
            questionText.text =
                "Chưa xác định môn học!";

            Debug.LogError(
                "GameSession.Subject đang trống!"
            );

            yield break;
        }


        // =====================================================
        // DIFFICULTIES
        // =====================================================

        if (
            GameSession.Difficulties == null
            ||
            GameSession.Difficulties.Count == 0
        )
        {
            questionText.text =
                "Chưa chọn độ khó!";

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
        // URL
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
            "SHEEP URL: "
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
                questionText.text =
                    "Lỗi kết nối: "
                    + req.error;

                Debug.LogError(
                    "SHEEP REQUEST ERROR: "
                    + req.error
                );

                yield break;
            }


            Debug.Log(
                "SHEEP JSON: "
                + req.downloadHandler.text
            );


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
                questionText.text =
                    "Không có câu hỏi phù hợp";

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
                questionText.text =
                    "Không có câu hỏi phù hợp với lựa chọn.";

                Debug.LogError(
                    "Sheep BuildQuestionSet trả về 0 câu."
                );

                yield break;
            }


            // =================================================
            // SCORE
            // =================================================

            maxRawScore = 0;

            foreach (
                QuestionData q
                in questions
            )
            {
                maxRawScore +=
                    GetPoints(q);
            }


            index = 0;
            rawScore = 0;
            shownScore = 0;
            correctCount = 0;

            locked = false;


            Debug.Log(
                "Sheep lấy được "
                + questions.Count
                + " câu."
            );


            foreach (
                QuestionData q
                in questions
            )
            {
                Debug.Log(
                    "SHEEP POOL => "
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


        string ch =
            GameSession.Chapter;


        Debug.Log(
            "Sheep filter chapter = ["
            + ch
            + "]"
        );


        foreach (
            QuestionData q
            in all
        )
        {
            if (q == null)
                continue;


            // =================================================
            // SUBJECT
            // =================================================

            bool subjectOK =
                string.Equals(
                    q.monId?.Trim(),
                    GameSession.Subject?.Trim(),
                    System.StringComparison
                        .OrdinalIgnoreCase
                );


            if (!subjectOK)
                continue;


            // =================================================
            // CHAPTER
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
                continue;


            // =================================================
            // DIFFICULTY
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
                continue;


            pool.Add(q);
        }


        // =====================================================
        // RANDOM
        // =====================================================

        Shuffle(pool);


        // =====================================================
        // LIMIT
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


        Debug.Log(
            "SHEEP ĐANG HIỂN THỊ => "
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


        if (questionText != null)
        {
            questionText.text =
                q.question;
        }


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
        // ĐÚNG
        // =====================================================

        if (isCorrect)
        {
            rawScore +=
                GetPoints(q);

            correctCount++;

            onCorrect?.Invoke();
        }

        // =====================================================
        // SAI
        // =====================================================

        else
        {
            onWrong?.Invoke();
        }


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
        yield return
            new WaitForSeconds(
                delayNext
            );


        if (
            !wasCorrect
            &&
            wrongAnswerEndsGame
        )
        {
            GameOver();
            yield break;
        }


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
                + GetScoreOutOf100()
                + "/100";
        }


        Debug.Log(
            "SHEEP GAME OVER"
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
                + "Bạn được "
                + score100
                + "/100 điểm"
                + "\nĐúng "
                + correctCount
                + "/"
                + questions.Count
                + " câu";
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
    // SCORE OUT OF 100
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
    // POINTS
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
    // PARSE A B C D
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


        return
            char.ToUpper(
                s.Trim()[0]
            )
            - 'A';
    }


    // =========================================================
    // BUTTON COLOR
    // =========================================================

    void SetButtonColor(
        int i,
        Color c
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


        if (img != null)
        {
            img.color = c;
        }
    }


    // =========================================================
    // SHOW / HIDE ANSWERS
    // =========================================================

    void SetAnswersVisible(
        bool show
    )
    {
        foreach (
            Button b
            in answerButtons
        )
        {
            if (b != null)
            {
                b.gameObject
                    .SetActive(show);
            }
        }
    }


    // =========================================================
    // SCORE UI
    // =========================================================

    void UpdateScore()
    {
        if (scoreText == null)
            return;


        int targetScore =
            GetScoreOutOf100();


        if (
            countRoutine != null
        )
        {
            StopCoroutine(
                countRoutine
            );
        }


        countRoutine =
            StartCoroutine(
                CountTo(
                    targetScore
                )
            );
    }


    // =========================================================
    // COUNT SCORE ANIMATION
    // =========================================================

    IEnumerator CountTo(
        int target
    )
    {
        while (
            Mathf.Abs(
                shownScore - target
            )
            > 0.01f
        )
        {
            shownScore =
                Mathf.MoveTowards(
                    shownScore,
                    target,
                    120f
                    * Time.deltaTime
                );


            scoreText.text =
                Mathf
                    .RoundToInt(
                        shownScore
                    )
                    .ToString();


            yield return null;
        }


        shownScore = target;

        scoreText.text =
            target.ToString();
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


            QuestionData tmp =
                list[i];


            list[i] =
                list[j];


            list[j] =
                tmp;
        }
    }
}