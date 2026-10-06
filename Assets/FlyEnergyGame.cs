using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;

public class FlyEnergyGame : MonoBehaviour
{
    // =========================================================
    // KHU VỰC CHƠI
    // =========================================================

    [Header("Khu vực chơi & nhân vật (để trống = tự tìm)")]
    public RectTransform area;
    public RectTransform student;
    public RectTransform[] stones;

    public float groundHeight = 140f;


    // =========================================================
    // BAY
    // =========================================================

    [Header("Bay")]
    public float gravity = 2200f;
    public float flapVelocity = 700f;
    public float pillarSpeed = 300f;

    public float pillarSpacing = 0f;
    public float yJitter = 120f;
    public float groupTolerance = 40f;

    [Range(0f, 0.45f)]
    public float studentShrink = 0.3f;

    [Range(0f, 0.45f)]
    public float stoneShrink = 0.05f;


    // =========================================================
    // NĂNG LƯỢNG
    // =========================================================

    [Header("Năng lượng & điểm")]
    public int maxEnergy = 100;
    public int startEnergy = 60;
    public int energyPerFlap = 10;
    public int askBelowEnergy = 15;
    public int pointsPerPillar = 10;


    // =========================================================
    // QUESTION UI
    // =========================================================

    [Header("Câu hỏi")]
    public GameObject questionPanel;
    public TMP_Text questionText;

    public Button[] answerButtons;
    public TMP_Text[] answerTexts;

    public TMP_Text explanationText;


    // =========================================================
    // UI
    // =========================================================

    [Header("Hiển thị")]
    public TMP_Text scoreText;
    public TMP_Text energyText;

    public Image energyFill;
    public Slider energySlider;

    public TMP_Text hintText;

    public Button restartButton;
    public Button exitButton;


    // =========================================================
    // GOOGLE SHEET
    // =========================================================

    [Header("Google Apps Script")]
    public string baseUrl = "DÁN_LINK_EXEC_VÀO_ĐÂY";


    [Header("Thiết lập câu hỏi")]

    [Tooltip("Số câu tối đa lấy cho game. 0 = lấy toàn bộ câu phù hợp.")]
    public int maxQuestions = 10;


    // =========================================================
    // MÀU
    // =========================================================

    [Header("Màu phản hồi")]

    public Color correctColor =
        new Color(0.3f, 0.85f, 0.3f);

    public Color wrongColor =
        new Color(0.95f, 0.3f, 0.3f);

    public float delayNext = 1.2f;


    // =========================================================
    // EVENTS
    // =========================================================

    [Header("Sự kiện")]
    public UnityEvent onCorrect;
    public UnityEvent onWrong;
    public UnityEvent onFinished;
    public UnityEvent onExit;


    // =========================================================
    // STATE
    // =========================================================

    enum State
    {
        Loading,
        Ready,
        Playing,
        Asking,
        Over
    }

    State state = State.Loading;


    // =========================================================
    // PILLAR GROUP
    // =========================================================

    class Group
    {
        public List<RectTransform> items =
            new List<RectTransform>();

        public List<float> offsetX =
            new List<float>();

        public List<float> baseY =
            new List<float>();

        public float startX;
        public float x;
        public float dy;

        public bool passed;
    }


    // =========================================================
    // RUNTIME
    // =========================================================

    List<Group> groups =
        new List<Group>();

    float spacing = 600f;

    Vector3 studentStart;

    float velY;

    int energy;
    int score;

    int correctCount;
    int answeredCount;


    List<QuestionData> questions =
        new List<QuestionData>();

    List<int> order =
        new List<int>();

    int orderIdx;

    QuestionData currentQ;

    bool locked;

    Color[] originalColors;

    GameObject tapArea;

    bool built;


    // =========================================================
    // AWAKE
    // =========================================================

    void Awake()
    {
        // Tự tìm area
        if (!area)
        {
            area =
                transform.parent
                as RectTransform;
        }


        // Tự tìm student
        if (!student)
        {
            student =
                FindChildByName(
                    "student"
                );
        }


        // Tự tìm Stones
        if (
            stones == null
            ||
            stones.Length == 0
        )
        {
            var list =
                new List<RectTransform>();


            foreach (
                Transform t
                in area
            )
            {
                if (
                    t.name
                    .ToLower()
                    .StartsWith("stone")
                )
                {
                    list.Add(
                        (RectTransform)t
                    );
                }
            }


            stones =
                list.ToArray();
        }


        // Lưu màu button
        originalColors =
            new Color[
                answerButtons.Length
            ];


        for (
            int i = 0;
            i < answerButtons.Length;
            i++
        )
        {
            int idx = i;


            if (
                answerButtons[i] == null
            )
            {
                continue;
            }


            Image img =
                answerButtons[i]
                .GetComponent<Image>();


            originalColors[i] =
                img
                ? img.color
                : Color.white;


            answerButtons[i]
                .onClick
                .AddListener(
                    () =>
                        OnAnswer(idx)
                );
        }


        if (restartButton)
        {
            restartButton
                .onClick
                .AddListener(
                    Restart
                );
        }


        if (exitButton)
        {
            exitButton
                .onClick
                .AddListener(
                    ExitGame
                );
        }
    }


    // =========================================================
    // EXIT
    // =========================================================

    public void ExitGame()
    {
        onExit?.Invoke();
    }


    // =========================================================
    // ENABLE
    // =========================================================

    void OnEnable()
    {
        Time.timeScale = 1f;

        StopAllCoroutines();

        StartCoroutine(
            Begin()
        );
    }


    void OnDisable()
    {
        StopAllCoroutines();

        locked = false;

        if (
            state != State.Loading
        )
        {
            state = State.Over;
        }
    }


    // =========================================================
    // BEGIN
    // =========================================================

    IEnumerator Begin()
    {
        state =
            State.Loading;


        SetQuestionUI(false);


        if (restartButton)
        {
            restartButton
                .gameObject
                .SetActive(false);
        }


        if (exitButton)
        {
            exitButton
                .gameObject
                .SetActive(false);
        }


        EnsureTapArea();


        tapArea.SetActive(false);


        Say(
            "Đang tải câu hỏi..."
        );


        // =====================================================
        // DEBUG SESSION
        // =====================================================

        Debug.Log(
            "========== FLY SESSION ==========\n"
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

        if (
            string.IsNullOrWhiteSpace(
                GameSession.Subject
            )
        )
        {
            Say(
                "Chưa xác định môn học!"
            );

            Debug.LogError(
                "FLY: GameSession.Subject đang trống!"
            );

            yield break;
        }


        // =====================================================
        // DIFFICULTY
        // =====================================================

        if (
            GameSession.Difficulties == null
            ||
            GameSession.Difficulties.Count
                == 0
        )
        {
            Say(
                "Chưa chọn độ khó!"
            );

            Debug.LogError(
                "FLY: chưa chọn độ khó!"
            );

            yield break;
        }


        // =====================================================
        // QUESTION TYPE
        // =====================================================

        if (
            GameSession.QuestionType
            != "Trắc nghiệm"
        )
        {
            Say(
                "Fly hiện chỉ hỗ trợ Trắc nghiệm."
            );

            yield break;
        }


        // =====================================================
        // URL
        // =====================================================

        string difficultyString =
            string.Join(
                ",",
                GameSession.Difficulties
            );


        string url =
            baseUrl
            + "?action=questions"
            + "&subject="
            + UnityWebRequest
                .EscapeURL(
                    GameSession.Subject
                )
            + "&difficulty="
            + UnityWebRequest
                .EscapeURL(
                    difficultyString
                );


        Debug.Log(
            "FLY URL: "
            + url
        );


        // =====================================================
        // API REQUEST
        // =====================================================

        using (
            UnityWebRequest req =
                UnityWebRequest
                .Get(url)
        )
        {
            yield return
                req.SendWebRequest();


            if (
                req.result !=
                UnityWebRequest
                    .Result
                    .Success
            )
            {
                Say(
                    "Lỗi kết nối: "
                    + req.error
                );


                Debug.LogError(
                    "FLY REQUEST ERROR: "
                    + req.error
                );


                yield break;
            }


            Debug.Log(
                "FLY JSON: "
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
                Say(
                    "Không có câu hỏi phù hợp."
                );


                yield break;
            }


            // =================================================
            // FILTER QUESTIONS
            // =================================================

            questions =
                BuildQuestionSet(
                    list.questions
                );


            if (
                questions.Count == 0
            )
            {
                Say(
                    "Không có câu hỏi phù hợp với chương đã chọn."
                );


                yield break;
            }
        }


        // =====================================================
        // CHECK REFERENCES
        // =====================================================

        if (
            area == null
            ||
            student == null
            ||
            stones == null
            ||
            stones.Length == 0
        )
        {
            Say(
                "Thiếu Area / Student / Stone trong Inspector"
            );


            Debug.LogError(
                "FlyEnergyGame: thiếu tham chiếu"
            );


            yield break;
        }


        // =====================================================
        // DEBUG QUESTIONS
        // =====================================================

        Debug.Log(
            "Fly lấy được "
            + questions.Count
            + " câu."
        );


        foreach (
            QuestionData q
            in questions
        )
        {
            Debug.Log(
                "FLY POOL => "
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


        // =====================================================
        // BUILD PILLARS
        // =====================================================

        if (!built)
        {
            BuildGroups();

            built = true;
        }


        ResetRun();
    }


    // =========================================================
    // FILTER QUESTIONS
    // =========================================================

    List<QuestionData> BuildQuestionSet(
        QuestionData[] all
    )
    {
        List<QuestionData> pool =
            new List<QuestionData>();


        string chapter =
            GameSession.Chapter;


        foreach (
            QuestionData q
            in all
        )
        {
            if (q == null)
                continue;


            // SUBJECT
            bool subjectOK =
                string.Equals(
                    q.monId?.Trim(),
                    GameSession
                        .Subject
                        ?.Trim(),
                    System.StringComparison
                        .OrdinalIgnoreCase
                );


            if (!subjectOK)
                continue;


            // CHAPTER
            bool chapterOK =
                true;


            if (
                !string.IsNullOrWhiteSpace(
                    chapter
                )
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
                            chapter.Trim(),
                            System.StringComparison
                                .OrdinalIgnoreCase
                        );
            }


            if (!chapterOK)
                continue;


            // DIFFICULTY
            bool difficultyOK =
                false;


            foreach (
                string selected
                in GameSession
                    .Difficulties
            )
            {
                if (
                    string.Equals(
                        q.difficulty
                            ?.Trim(),
                        selected
                            ?.Trim(),
                        System.StringComparison
                            .OrdinalIgnoreCase
                    )
                )
                {
                    difficultyOK =
                        true;

                    break;
                }
            }


            if (!difficultyOK)
                continue;


            pool.Add(q);
        }


        Shuffle(pool);


        if (
            maxQuestions > 0
            &&
            pool.Count
                > maxQuestions
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
    // RESET RUN
    // =========================================================

    void ResetRun()
    {
        energy =
            Mathf.Clamp(
                startEnergy,
                0,
                maxEnergy
            );


        score = 0;

        correctCount = 0;

        answeredCount = 0;

        velY = 0f;


        order.Clear();

        orderIdx = 0;


        student.localPosition =
            studentStart;

        student.localRotation =
            Quaternion.identity;


        foreach (
            Group g
            in groups
        )
        {
            g.x =
                g.startX;

            g.dy = 0f;

            g.passed = false;

            Place(g);
        }


        if (restartButton)
        {
            restartButton
                .gameObject
                .SetActive(false);
        }


        if (exitButton)
        {
            exitButton
                .gameObject
                .SetActive(false);
        }


        SetQuestionUI(false);


        UpdateUI();


        Say(
            "Chạm để bay!"
        );


        tapArea
            .SetActive(true);


        tapArea
            .transform
            .SetAsLastSibling();


        state =
            State.Ready;
    }


    // =========================================================
    // RESTART
    // =========================================================

    public void Restart()
    {
        if (
            state ==
            State.Over
        )
        {
            ResetRun();
        }
    }


    // =========================================================
    // FLAP
    // =========================================================

    public void Flap()
    {
        if (
            state ==
            State.Ready
        )
        {
            state =
                State.Playing;

            Say("");
        }


        if (
            state !=
            State.Playing
        )
            return;


        if (
            energy
            < energyPerFlap
        )
            return;


        energy -=
            energyPerFlap;


        velY =
            flapVelocity;


        UpdateUI();
    }


    // =========================================================
    // UPDATE
    // =========================================================

    void Update()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        if (
            Input.GetKeyDown(
                KeyCode.Space
            )
        )
        {
            Flap();
        }
#endif


        if (
            state ==
            State.Ready
        )
        {
            return;
        }


        if (
            state !=
            State.Playing
        )
        {
            return;
        }


        float dt =
            Time.deltaTime;


        // =====================================================
        // STUDENT
        // =====================================================

        velY -=
            gravity * dt;


        Vector3 p =
            student.localPosition;


        p.y +=
            velY * dt;


        float ceil =
            area.rect.height
            * 0.5f;


        if (
            p.y > ceil
        )
        {
            p.y = ceil;

            velY =
                Mathf.Min(
                    velY,
                    0f
                );
        }


        student.localPosition =
            p;


        student.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                Mathf.Clamp(
                    velY * 0.03f,
                    -30f,
                    25f
                )
            );


        Rect sRect =
            LocalRect(
                student,
                studentShrink
            );


        float floor =
            -area.rect.height
            * 0.5f
            + groundHeight;


        if (
            sRect.yMin
            < floor
        )
        {
            Finish(
                "Rơi xuống đất rồi! "
            );

            return;
        }


        // =====================================================
        // PILLARS
        // =====================================================

        float leftEdge =
            -area.rect.width
            * 0.5f
            - 100f;


        foreach (
            Group g
            in groups
        )
        {
            g.x -=
                pillarSpeed * dt;


            Place(g);


            float right =
                GroupRight(g);


            if (
                !g.passed
                &&
                right < sRect.xMin
            )
            {
                g.passed = true;

                score +=
                    pointsPerPillar;


                UpdateUI();
            }


            if (
                right < leftEdge
            )
            {
                float maxX =
                    float.MinValue;


                foreach (
                    Group g2
                    in groups
                )
                {
                    maxX =
                        Mathf.Max(
                            maxX,
                            g2.x
                        );
                }


                g.x =
                    maxX
                    + spacing;


                g.dy =
                    Random.Range(
                        -yJitter,
                        yJitter
                    );


                g.passed =
                    false;


                Place(g);
            }
        }


        // =====================================================
        // COLLISION
        // =====================================================

        foreach (
            Group g
            in groups
        )
        {
            foreach (
                RectTransform s
                in g.items
            )
            {
                if (
                    !s.gameObject
                    .activeInHierarchy
                )
                    continue;


                if (
                    LocalRect(
                        s,
                        stoneShrink
                    )
                    .Overlaps(
                        sRect
                    )
                )
                {
                    Finish(
                        "Va vào cột rồi! "
                    );

                    return;
                }
            }
        }


        // =====================================================
        // LOW ENERGY -> QUESTION
        // =====================================================

        if (
            energy
            <
            Mathf.Max(
                askBelowEnergy,
                energyPerFlap
            )
        )
        {
            BeginAsking();
        }
    }


    // =========================================================
    // ASK
    // =========================================================

    void BeginAsking()
    {
        state =
            State.Asking;


        tapArea
            .SetActive(false);


        Say("");


        ShowNextQuestion();
    }


    // =========================================================
    // NEXT QUESTION
    // =========================================================

    void ShowNextQuestion()
    {
        if (
            order.Count == 0
            ||
            orderIdx >=
            order.Count
        )
        {
            order.Clear();


            for (
                int i = 0;
                i < questions.Count;
                i++
            )
            {
                order.Add(i);
            }


            Shuffle(order);


            orderIdx = 0;
        }


        currentQ =
            questions[
                order[
                    orderIdx++
                ]
            ];


        locked = false;


        Debug.Log(
            "FLY ĐANG HIỂN THỊ => "
            + currentQ.id
            + " | môn: "
            + currentQ.monId
            + " | chương: "
            + currentQ.topic
            + " | độ khó: "
            + currentQ.difficulty
            + " | câu: "
            + currentQ.question
        );


        questionText.text =
            currentQ.question;


        string[] opts =
        {
            currentQ.optionA,
            currentQ.optionB,
            currentQ.optionC,
            currentQ.optionD
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
                answerTexts[i]
            )
            {
                answerTexts[i].text =
                    opts[i];
            }


            Image img =
                answerButtons[i]
                .GetComponent<Image>();


            if (img)
            {
                img.color =
                    originalColors[i];
            }
        }


        if (
            explanationText
        )
        {
            explanationText.text = "";
        }


        SetQuestionUI(true);
    }


    // =========================================================
    // ANSWER
    // =========================================================

    void OnAnswer(
        int chosen
    )
    {
        if (
            state !=
                State.Asking
            ||
            locked
        )
        {
            return;
        }


        locked = true;


        int correctIndex =
            ParseLetter(
                currentQ.correct
            );


        bool isCorrect =
            chosen ==
            correctIndex;


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


        answeredCount++;


        if (isCorrect)
        {
            int gain =
                GetPoints(
                    currentQ
                );


            energy =
                Mathf.Min(
                    maxEnergy,
                    energy + gain
                );


            correctCount++;


            onCorrect?.Invoke();
        }
        else
        {
            onWrong?.Invoke();
        }


        if (
            explanationText
        )
        {
            explanationText.text =
                currentQ.explanation;
        }


        UpdateUI();


        StartCoroutine(
            AfterAnswer()
        );
    }


    // =========================================================
    // AFTER ANSWER
    // =========================================================

    IEnumerator AfterAnswer()
    {
        yield return
            new WaitForSeconds(
                delayNext
            );


        if (
            explanationText
        )
        {
            explanationText.text = "";
        }


        if (
            energy
            <
            Mathf.Max(
                askBelowEnergy,
                energyPerFlap
            )
        )
        {
            ShowNextQuestion();

            yield break;
        }


        SetQuestionUI(false);


        Say(
            "Chạm để bay tiếp!"
        );


        tapArea
            .SetActive(true);


        tapArea
            .transform
            .SetAsLastSibling();


        state =
            State.Ready;
    }


    // =========================================================
    // FINISH
    // =========================================================

    void Finish(
        string head
    )
    {
        state =
            State.Over;


        tapArea
            .SetActive(false);


        SetQuestionUI(false);


        Say(
            head
            + "Bạn được "
            + score
            + " điểm"
            + " (đúng "
            + correctCount
            + "/"
            + answeredCount
            + " câu)"
        );


        if (restartButton)
        {
            restartButton
                .gameObject
                .SetActive(true);

            restartButton
                .transform
                .SetAsLastSibling();
        }


        if (exitButton)
        {
            exitButton
                .gameObject
                .SetActive(true);

            exitButton
                .transform
                .SetAsLastSibling();
        }


        onFinished?.Invoke();
    }


    // =========================================================
    // BUILD GROUPS
    // =========================================================

    void BuildGroups()
    {
        groups.Clear();


        studentStart =
            student.localPosition;


        var list =
            new List<RectTransform>();


        foreach (
            RectTransform s
            in stones
        )
        {
            if (s)
            {
                list.Add(s);
            }
        }


        list.Sort(
            (a, b) =>
                a.localPosition.x
                .CompareTo(
                    b.localPosition.x
                )
        );


        Group cur = null;


        foreach (
            RectTransform s
            in list
        )
        {
            float x =
                s.localPosition.x;


            if (
                cur == null
                ||
                x - cur.startX
                    > groupTolerance
            )
            {
                cur =
                    new Group
                    {
                        startX = x,
                        x = x
                    };


                groups.Add(cur);
            }


            cur.items.Add(s);

            cur.offsetX.Add(
                x - cur.startX
            );

            cur.baseY.Add(
                s.localPosition.y
            );
        }


        spacing =
            pillarSpacing;


        if (
            spacing <= 0f
        )
        {
            spacing =
                groups.Count > 1
                ?
                (
                    groups[
                        groups.Count - 1
                    ].startX
                    -
                    groups[0]
                        .startX
                )
                /
                (
                    groups.Count - 1
                )
                :
                600f;
        }


        if (
            spacing < 50f
        )
        {
            spacing = 600f;
        }
    }


    // =========================================================
    // PLACE
    // =========================================================

    void Place(
        Group g
    )
    {
        for (
            int i = 0;
            i < g.items.Count;
            i++
        )
        {
            Vector3 lp =
                g.items[i]
                    .localPosition;


            lp.x =
                g.x
                + g.offsetX[i];


            lp.y =
                g.baseY[i]
                + g.dy;


            g.items[i]
                .localPosition =
                lp;
        }
    }


    // =========================================================
    // RIGHT EDGE
    // =========================================================

    float GroupRight(
        Group g
    )
    {
        float right =
            float.MinValue;


        foreach (
            RectTransform s
            in g.items
        )
        {
            float r =
                s.localPosition.x
                +
                s.rect.xMax
                *
                s.localScale.x;


            if (
                r > right
            )
            {
                right = r;
            }
        }


        return right;
    }


    // =========================================================
    // RECT
    // =========================================================

    Rect LocalRect(
        RectTransform rt,
        float shrink
    )
    {
        Vector3 p =
            rt.localPosition;


        Vector3 sc =
            rt.localScale;


        Rect r =
            rt.rect;


        float w =
            r.width
            * Mathf.Abs(sc.x);


        float h =
            r.height
            * Mathf.Abs(sc.y);


        float x =
            p.x
            + r.x * sc.x;


        float y =
            p.y
            + r.y * sc.y;


        return new Rect(
            x + w * shrink,
            y + h * shrink,
            w * (1f - 2f * shrink),
            h * (1f - 2f * shrink)
        );
    }


    // =========================================================
    // TAP AREA
    // =========================================================

    void EnsureTapArea()
    {
        if (
            tapArea != null
        )
        {
            return;
        }


        tapArea =
            new GameObject(
                "TapArea",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button)
            );


        RectTransform rt =
            tapArea
                .GetComponent<
                    RectTransform
                >();


        rt.SetParent(
            area,
            false
        );


        rt.anchorMin =
            Vector2.zero;


        rt.anchorMax =
            Vector2.one;


        rt.offsetMin =
        rt.offsetMax =
            Vector2.zero;


        tapArea
            .GetComponent<Image>()
            .color =
            new Color(
                1f,
                1f,
                1f,
                0f
            );


        Button btn =
            tapArea
                .GetComponent<Button>();


        btn.transition =
            Selectable
                .Transition
                .None;


        btn.onClick
            .AddListener(
                Flap
            );
    }


    // =========================================================
    // FIND CHILD
    // =========================================================

    RectTransform FindChildByName(
        string childName
    )
    {
        if (!area)
            return null;


        foreach (
            Transform t
            in area
        )
        {
            if (
                t.name
                .ToLower()
                ==
                childName
                    .ToLower()
            )
            {
                return
                    (RectTransform)t;
            }
        }


        return null;
    }


    // =========================================================
    // QUESTION UI
    // =========================================================

    void SetQuestionUI(
        bool show
    )
    {
        if (
            questionPanel
        )
        {
            questionPanel
                .SetActive(show);
        }


        if (
            questionText
        )
        {
            questionText
                .gameObject
                .SetActive(show);
        }


        foreach (
            Button b
            in answerButtons
        )
        {
            if (b)
            {
                b.gameObject
                    .SetActive(show);
            }
        }


        if (
            !show
            &&
            explanationText
        )
        {
            explanationText.text = "";
        }
    }


    // =========================================================
    // SAY
    // =========================================================

    void Say(
        string msg
    )
    {
        if (
            hintText
        )
        {
            hintText
                .gameObject
                .SetActive(
                    !string.IsNullOrEmpty(
                        msg
                    )
                );


            hintText.text =
                msg;
        }
        else if (
            questionText
            &&
            !string.IsNullOrEmpty(
                msg
            )
        )
        {
            questionText
                .gameObject
                .SetActive(true);


            questionText.text =
                msg;
        }
    }


    // =========================================================
    // UPDATE UI
    // =========================================================

    void UpdateUI()
    {
        if (
            scoreText
        )
        {
            scoreText.text =
                score.ToString();
        }


        if (
            energyText
        )
        {
            energyText.text =
                energy
                + "/"
                + maxEnergy;
        }


        if (
            energyFill
        )
        {
            energyFill
                .fillAmount =
                maxEnergy > 0
                ?
                (float)energy
                /
                maxEnergy
                :
                0f;
        }


        if (
            energySlider
        )
        {
            energySlider
                .interactable =
                false;


            energySlider.minValue =
                0;


            energySlider.maxValue =
                maxEnergy;


            energySlider.value =
                energy;
        }
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
    // SHUFFLE
    // =========================================================

    void Shuffle<T>(
        List<T> list
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


            T tmp =
                list[i];


            list[i] =
                list[j];


            list[j] =
                tmp;
        }
    }


    // =========================================================
    // A B C D
    // =========================================================

    int ParseLetter(
        string s
    )
    {
        if (
            string.IsNullOrWhiteSpace(
                s
            )
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


        if (img)
        {
            img.color = c;
        }
    }
}