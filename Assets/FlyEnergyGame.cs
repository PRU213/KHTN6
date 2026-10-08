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

    // Chậm hơn một chút để dễ chơi
    public float pillarSpeed = 250f;

    // Khoảng cách giữa các cặp cột xa hơn
    public float pillarSpacing = 800f;

    // Random lên xuống nhẹ hơn
    public float yJitter = 70f;

    // Giúp cột trên + dưới được nhận đúng thành một cặp
    public float groupTolerance = 120f;

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
    public string baseUrl = "https://script.google.com/macros/s/AKfycbxxE2R2ZoitgM647aQqnebUcG90lhIlodU0DcyiaZuKkLaVWl6oopI-TkeNM8_KKDWhUw/exec";


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

    // true khi panel câu hỏi đang mở
    bool questionOpen = false;

    Color[] originalColors;

    GameObject tapArea;

    bool built;


    // =========================================================
    // AWAKE
    // =========================================================
    // =========================================================
    // AUTO FIND QUESTION UI
    // =========================================================

    Transform FindDeepChild(Transform parent, string targetName)
    {
        if (parent == null)
            return null;

        foreach (Transform child in parent)
        {
            if (
                child.name.Equals(
                    targetName,
                    System.StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return child;
            }

            Transform result =
                FindDeepChild(
                    child,
                    targetName
                );

            if (result != null)
            {
                return result;
            }
        }

        return null;
    }


    void FindQuestionReferences()
    {
        // =====================================================
        // TÌM AREA
        // =====================================================

        if (area == null)
        {
            area =
                transform.parent
                as RectTransform;
        }


        if (area == null)
        {
            Debug.LogError(
                "FLY: Không tìm thấy AREA!"
            );

            return;
        }


        // =====================================================
        // TÌM QUESTION PANEL
        // =====================================================

        if (questionPanel == null)
        {
            Transform panel =
                FindDeepChild(
                    area,
                    "QsPanel1"
                );

            if (panel != null)
            {
                questionPanel =
                    panel.gameObject;
            }
        }


        // =====================================================
        // TÌM QUESTION TEXT
        // =====================================================

        if (
            questionText == null
            &&
            questionPanel != null
        )
        {
            Transform qt =
                FindDeepChild(
                    questionPanel.transform,
                    "QuestionText"
                );

            if (qt != null)
            {
                questionText =
                    qt.GetComponent<TMP_Text>();
            }
        }


        // =====================================================
        // TỰ TÌM 4 BUTTON NẾU THIẾU
        // =====================================================

        if (
            answerButtons == null
            ||
            answerButtons.Length < 4
        )
        {
            answerButtons =
                new Button[4];


            for (int i = 0; i < 4; i++)
            {
                string buttonName =
                    "a" + (i + 1);


                Transform t =
                    questionPanel != null
                        ? FindDeepChild(
                            questionPanel.transform,
                            buttonName
                        )
                        : null;


                if (t != null)
                {
                    answerButtons[i] =
                        t.GetComponent<Button>();
                }
            }
        }


        // =====================================================
        // TỰ LẤY TEXT BÊN TRONG TỪNG BUTTON
        // =====================================================

        if (
            answerTexts == null
            ||
            answerTexts.Length < 4
        )
        {
            answerTexts =
                new TMP_Text[4];
        }


        for (int i = 0; i < 4; i++)
        {
            if (
                answerTexts[i] == null
                &&
                answerButtons != null
                &&
                i < answerButtons.Length
                &&
                answerButtons[i] != null
            )
            {
                answerTexts[i] =
                    answerButtons[i]
                        .GetComponentInChildren<
                            TMP_Text
                        >(true);
            }
        }


        // =====================================================
        // DEBUG
        // =====================================================

        Debug.Log(
            "===== FLY UI REF CHECK ====="
            + "\nObject: "
            + gameObject.name
            + "\nPanel: "
            + (
                questionPanel != null
                    ? questionPanel.name
                    : "NULL"
            )
            + "\nQuestionText: "
            + (
                questionText != null
                    ? questionText.name
                    : "NULL"
            )
            + "\nButtons: "
            + (
                answerButtons != null
                    ? answerButtons.Length
                    : 0
            )
            + "\nAnswerTexts: "
            + (
                answerTexts != null
                    ? answerTexts.Length
                    : 0
            )
        );
    }


    void Awake()
    {
        // =====================================================
        // AREA
        // =====================================================

        if (area == null)
        {
            area =
                transform.parent
                as RectTransform;
        }


        // =====================================================
        // STUDENT
        // =====================================================

        if (student == null)
        {
            student =
                FindChildByName(
                    "student"
                );
        }


        // =====================================================
        // STONES
        // =====================================================

        if (
            stones == null
            ||
            stones.Length == 0
        )
        {
            List<RectTransform> list =
                new List<RectTransform>();


            if (area != null)
            {
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
                        RectTransform rt =
                            t as RectTransform;

                        if (rt != null)
                        {
                            list.Add(rt);
                        }
                    }
                }
            }


            stones =
                list.ToArray();
        }


        // =====================================================
        // TỰ TÌM UI CÂU HỎI
        // =====================================================

        FindQuestionReferences();


        // =====================================================
        // BUTTON COLORS + EVENTS
        // =====================================================

        int buttonCount =
            answerButtons != null
                ? answerButtons.Length
                : 0;


        originalColors =
            new Color[buttonCount];


        for (
            int i = 0;
            i < buttonCount;
            i++
        )
        {
            int idx = i;


            if (answerButtons[i] == null)
            {
                continue;
            }


            Image img =
                answerButtons[i]
                .GetComponent<Image>();


            originalColors[i] =
                img != null
                    ? img.color
                    : Color.white;


            // tránh listener bị add trùng
            answerButtons[i]
                .onClick
                .RemoveAllListeners();


            answerButtons[i]
                .onClick
                .AddListener(
                    () =>
                        OnAnswer(idx)
                );
        }


        // =====================================================
        // RESTART
        // =====================================================

        if (restartButton != null)
        {
            restartButton
                .onClick
                .RemoveAllListeners();


            restartButton
                .onClick
                .AddListener(
                    Restart
                );
        }


        // =====================================================
        // EXIT
        // =====================================================

        if (exitButton != null)
        {
            exitButton
                .onClick
                .RemoveAllListeners();


            exitButton
                .onClick
                .AddListener(
                    ExitGame
                );
        }


        HideEndButtons();
    }


    // =========================================================
    // EXIT
    // =========================================================

    public void ExitGame()
    {
        onExit?.Invoke();
    }

    void HideEndButtons()
    {
        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(false);
        }

        if (exitButton != null)
        {
            exitButton.gameObject.SetActive(false);
        }
    }


    // =========================================================
    // ENABLE
    // =========================================================

    void OnEnable()
    {
        Time.timeScale = 1f;

        StopAllCoroutines();

        // Không cho nút hiện khi vừa vào game
        HideEndButtons();

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


        HideEndButtons();


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
        questionOpen = false;
        locked = false;

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


        HideEndButtons();


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


        energy -= energyPerFlap;

        // Không cho nhỏ hơn 0
        energy = Mathf.Clamp(
            energy,
            0,
            maxEnergy
        );

        velY = flapVelocity;

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
        // =====================================================
        // TRẠNG THÁI CÂU HỎI
        // =====================================================

        state = State.Asking;
        locked = false;
        questionOpen = true;




        // =====================================================
        // VALIDATE
        // =====================================================

        if (
            questions == null
            ||
            questions.Count == 0
        )
        {
            Debug.LogError(
                "FLY: Không có câu hỏi!"
            );

            questionOpen = false;
            SetQuestionUI(false);

            return;
        }


        if (questionPanel == null)
        {
            Debug.LogError(
                "FLY: Không tìm thấy QsPanel1!"
            );

            questionOpen = false;

            return;
        }


        if (questionText == null)
        {
            Debug.LogError(
                "FLY: Không tìm thấy QuestionText!"
            );

            questionOpen = false;
            SetQuestionUI(false);

            return;
        }


        if (
            answerButtons == null
            ||
            answerButtons.Length < 4
        )
        {
            Debug.LogError(
                "FLY: Không đủ 4 Answer Button!"
            );

            questionOpen = false;
            SetQuestionUI(false);

            return;
        }


        if (
            answerTexts == null
            ||
            answerTexts.Length < 4
        )
        {
            Debug.LogError(
                "FLY: Không đủ 4 Answer Text!"
            );

            questionOpen = false;
            SetQuestionUI(false);

            return;
        }


        // =====================================================
        // TẠO THỨ TỰ CÂU HỎI
        // =====================================================

        if (
            order.Count == 0
            ||
            orderIdx >= order.Count
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


        // =====================================================
        // LẤY CÂU HỎI
        // =====================================================

        int questionIndex =
            order[orderIdx++];


        if (
            questionIndex < 0
            ||
            questionIndex >= questions.Count
        )
        {
            Debug.LogError(
                "FLY: Question Index lỗi: "
                + questionIndex
            );

            questionOpen = false;

            return;
        }


        currentQ =
            questions[questionIndex];


        if (currentQ == null)
        {
            Debug.LogError(
                "FLY: currentQ NULL!"
            );

            questionOpen = false;

            return;
        }


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


        // =====================================================
        // CÂU HỎI
        // =====================================================

        questionText.text =
            currentQ.question ?? "";


        // =====================================================
        // ĐÁP ÁN
        // =====================================================

        string[] opts =
        {
        currentQ.optionA ?? "",
        currentQ.optionB ?? "",
        currentQ.optionC ?? "",
        currentQ.optionD ?? ""
    };


        for (
            int i = 0;
            i < 4;
            i++
        )
        {
            if (answerButtons[i] == null)
            {
                Debug.LogError(
                    "FLY: Button "
                    + i
                    + " đang NULL!"
                );

                continue;
            }


            // Hiện button
            answerButtons[i]
                .gameObject
                .SetActive(true);


            // Cho phép click
            answerButtons[i]
                .interactable = true;


            // Đảm bảo listener đúng (phòng trường hợp mất listener)
            int idx = i;
            answerButtons[i]
                .onClick
                .RemoveAllListeners();
            answerButtons[i]
                .onClick
                .AddListener(
                    () => OnAnswer(idx)
                );


            // Bật raycast
            Image img =
                answerButtons[i]
                    .GetComponent<Image>();


            if (img != null)
            {
                img.raycastTarget = true;


                if (
                    originalColors != null
                    &&
                    i < originalColors.Length
                )
                {
                    img.color =
                        originalColors[i];
                }
            }


            // Tìm text nếu bị mất reference
            if (answerTexts[i] == null)
            {
                answerTexts[i] =
                    answerButtons[i]
                        .GetComponentInChildren<
                            TMP_Text
                        >(true);
            }


            if (answerTexts[i] != null)
            {
                answerTexts[i].text =
                    opts[i];
            }
            else
            {
                Debug.LogError(
                    "FLY: Không tìm thấy text của Button "
                    + i
                );
            }
        }


        // =====================================================
        // GIẢI THÍCH
        // =====================================================

        if (explanationText != null)
        {
            explanationText.text = "";
        }


        // =====================================================
        // TẮT TAP AREA
        // =====================================================

        if (tapArea != null)
        {
            tapArea.SetActive(false);
        }


        // =====================================================
        // HIỆN PANEL
        // =====================================================

        questionPanel.SetActive(true);

        questionText
            .gameObject
            .SetActive(true);


        // Đưa panel lên trên cùng
        questionPanel
            .transform
            .SetAsLastSibling();


        CanvasGroup cg =
            questionPanel
                .GetComponent<CanvasGroup>();


        if (cg != null)
        {
            cg.alpha = 1f;
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }


        Debug.Log(
            "FLY: Đã mở panel câu hỏi"
            + " | state = "
            + state
            + " | questionOpen = "
            + questionOpen
        );
    }


    // =========================================================
    // ANSWER
    // =========================================================

    void OnAnswer(int chosen)
    {
        Debug.Log(
            "FLY CLICK ANSWER: "
            + chosen
            + " | state = "
            + state
            + " | locked = "
            + locked
            + " | questionOpen = "
            + questionOpen
        );


        // =====================================================
        // PANEL CÂU HỎI PHẢI ĐANG MỞ
        // =====================================================

        bool panelIsOpen =
            questionPanel != null
            &&
            questionPanel.activeInHierarchy;


        if (
            !questionOpen
            &&
            !panelIsOpen
        )
        {
            Debug.LogWarning(
                "FLY: Câu hỏi hiện không mở!"
            );

            return;
        }


        // Nếu panel đang mở thì ép về Asking
        state = State.Asking;
        questionOpen = true;


        // =====================================================
        // KHÓA CLICK NHIỀU LẦN
        // =====================================================

        if (locked)
        {
            Debug.LogWarning(
                "FLY: Đáp án đang bị khóa!"
            );

            return;
        }


        // =====================================================
        // KIỂM TRA CÂU HỎI
        // =====================================================

        if (currentQ == null)
        {
            Debug.LogWarning(
                "FLY: currentQ NULL → tải lại câu hỏi"
            );

            locked = false;
            ShowNextQuestion();

            return;
        }


        if (
            chosen < 0
            ||
            chosen >= 4
        )
        {
            Debug.LogError(
                "FLY: chosen không hợp lệ: "
                + chosen
            );

            return;
        }


        locked = true;


        // =====================================================
        // KHÓA 4 BUTTON
        // =====================================================

        if (answerButtons != null)
        {
            for (
                int i = 0;
                i < answerButtons.Length;
                i++
            )
            {
                if (answerButtons[i] != null)
                {
                    answerButtons[i]
                        .interactable = false;
                }
            }
        }


        // =====================================================
        // TÌM ĐÁP ÁN ĐÚNG
        // =====================================================

        int correctIndex =
            ParseLetter(
                currentQ.correct
            );


        Debug.Log(
            "FLY ANSWER CHECK"
            + " | chosen = "
            + chosen
            + " | correct = "
            + correctIndex
            + " | raw = "
            + currentQ.correct
        );


        if (
            correctIndex < 0
            ||
            correctIndex >= 4
        )
        {
            Debug.LogError(
                "FLY: correctIndex không hợp lệ!"
            );


            locked = false;


            for (
                int i = 0;
                i < answerButtons.Length;
                i++
            )
            {
                if (answerButtons[i] != null)
                {
                    answerButtons[i]
                        .interactable = true;
                }
            }


            return;
        }


        bool isCorrect =
            chosen == correctIndex;


        // =====================================================
        // MÀU ĐÁP ÁN
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


        answeredCount++;


        // =====================================================
        // ĐÚNG
        // =====================================================

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
            score += 100;

            correctCount++;


            Debug.Log(
                "FLY: TRẢ LỜI ĐÚNG"
                + " | +"
                + gain
                + " Energy"
                + " | Energy = "
                + energy
            );


            onCorrect?.Invoke();
        }

        // =====================================================
        // SAI
        // =====================================================

        else
        {
            Debug.Log(
                "FLY: TRẢ LỜI SAI"
            );


            onWrong?.Invoke();
        }


        // =====================================================
        // GIẢI THÍCH
        // =====================================================

        if (explanationText != null)
        {
            explanationText.text =
                currentQ.explanation ?? "";
        }


        UpdateUI();


        // =====================================================
        // CHỜ RỒI CHUYỂN TIẾP
        // =====================================================

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


        if (explanationText != null)
        {
            explanationText.text = "";
        }


        // =====================================================
        // NẾU VẪN THIẾU NĂNG LƯỢNG
        // -> HỎI TIẾP
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
            locked = false;
            questionOpen = true;

            ShowNextQuestion();

            yield break;
        }


        // =====================================================
        // ĐỦ NĂNG LƯỢNG
        // -> ĐÓNG CÂU HỎI
        // =====================================================

        questionOpen = false;
        locked = false;


        SetQuestionUI(false);


        Say("");


        if (tapArea != null)
        {
            tapArea.SetActive(true);

            tapArea
                .transform
                .SetAsLastSibling();
        }


        state =
            State.Playing;


        Debug.Log(
            "FLY: Đã trả lời xong"
            + " | Energy = "
            + energy
            + " | state = Ready"
        );
    }


    // =========================================================
    // FINISH
    // =========================================================

    void Finish(string head)
    {
        state = State.Over;
        questionOpen = false;
        locked = true;

        if (tapArea != null)
        {
            tapArea.SetActive(false);
        }

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


        // =====================================================
        // HIỆN NÚT TRANG CHỦ
        // =====================================================

        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(true);

            // Đưa lên trên cùng UI
            restartButton.transform.SetAsLastSibling();

            RectTransform rt =
                restartButton.GetComponent<RectTransform>();

            if (rt != null)
            {
                rt.localScale = Vector3.one;
            }

            Debug.Log(
                "FLY: Đã bật nút Trang chủ."
            );
        }
        else
        {
            Debug.LogError(
                "FLY: Restart Button chưa được gán!"
            );
        }


        // =====================================================
        // HIỆN NÚT THOÁT GAME
        // =====================================================

        if (exitButton != null)
        {
            exitButton.gameObject.SetActive(true);

            // Đưa lên trên cùng UI
            exitButton.transform.SetAsLastSibling();

            RectTransform rt =
                exitButton.GetComponent<RectTransform>();

            if (rt != null)
            {
                rt.localScale = Vector3.one;
            }

            Debug.Log(
                "FLY: Đã bật nút Thoát game."
            );
        }
        else
        {
            Debug.LogError(
                "FLY: Exit Button chưa được gán!"
            );
        }


        onFinished?.Invoke();
    }


    // =========================================================
    // BUILD GROUPS
    // =========================================================

    void BuildGroups()
    {
        groups.Clear();

        studentStart = student.localPosition;

        List<RectTransform> list =
            new List<RectTransform>();

        foreach (RectTransform s in stones)
        {
            if (s != null)
            {
                list.Add(s);
            }
        }

        // Sắp xếp Stone từ trái sang phải
        list.Sort(
            (a, b) =>
                a.localPosition.x.CompareTo(
                    b.localPosition.x
                )
        );


        // =====================================================
        // GOM CÁC CỘT CÓ X GẦN NHAU THÀNH MỘT GROUP
        // =====================================================

        foreach (RectTransform s in list)
        {
            float x = s.localPosition.x;

            Group bestGroup = null;
            float bestDistance = float.MaxValue;


            foreach (Group g in groups)
            {
                float distance =
                    Mathf.Abs(x - g.startX);

                if (
                    distance <= groupTolerance
                    &&
                    distance < bestDistance
                )
                {
                    bestDistance = distance;
                    bestGroup = g;
                }
            }


            // Không tìm thấy group phù hợp
            // -> tạo group mới
            if (bestGroup == null)
            {
                bestGroup = new Group
                {
                    startX = x,
                    x = x,
                    dy = 0f,
                    passed = false
                };

                groups.Add(bestGroup);
            }


            bestGroup.items.Add(s);

            bestGroup.offsetX.Add(
                x - bestGroup.startX
            );

            bestGroup.baseY.Add(
                s.localPosition.y
            );
        }


        // =====================================================
        // SẮP XẾP GROUP TỪ TRÁI SANG PHẢI
        // =====================================================

        groups.Sort(
            (a, b) =>
                a.startX.CompareTo(
                    b.startX
                )
        );


        // =====================================================
        // KHOẢNG CÁCH CÁC GROUP
        // =====================================================

        if (pillarSpacing > 0f)
        {
            spacing = pillarSpacing;
        }
        else if (groups.Count > 1)
        {
            float total =
                groups[groups.Count - 1].startX
                - groups[0].startX;

            spacing =
                total /
                (groups.Count - 1);
        }
        else
        {
            spacing = 500f;
        }


        if (spacing < 200f)
        {
            spacing = 500f;
        }


        // DEBUG
        Debug.Log(
            "FLY: Có "
            + groups.Count
            + " nhóm cột. Spacing = "
            + spacing
        );


        for (int i = 0; i < groups.Count; i++)
        {
            Debug.Log(
                "Group "
                + i
                + " có "
                + groups[i].items.Count
                + " Stone"
            );
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