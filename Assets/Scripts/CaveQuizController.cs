using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;

public class CaveQuizController : MonoBehaviour
{
    // =========================================================
    // UI
    // =========================================================

    [Header("Khung câu hỏi")]
    public TMP_Text questionText;

    [Header("4 đáp án, theo thứ tự A, B, C, D")]
    public Button[] answerButtons;
    public TMP_Text[] answerTexts;


    // =========================================================
    // UI TÙY CHỌN
    // =========================================================

    [Header("Tùy chọn")]
    public TMP_Text scoreText;
    public TMP_Text explanationText;


    // =========================================================
    // LƯỚI
    // =========================================================

    [Header("Lưới (matrix)")]
    public RectTransform cellTopLeft;
    public RectTransform cellBottomRight;

    public int columns = 8;
    public int rows = 6;


    // =========================================================
    // NHÂN VẬT / KHO BÁU / ĐÁ
    // =========================================================

    [Header("Nhân vật, kho báu, đá")]
    public RectTransform person;
    public RectTransform treasure;

    // Animator của Person
    public Animator personAnimator;

    public GameObject stoneTemplate;
    public GameObject questionMarker;

    public int obstacleCount = 6;
    public int gateCount = 2;

    public float moveSpeed = 600f;


    // =========================================================
    // GOOGLE SHEET
    // =========================================================

    [Header("Google Apps Script")]
    public string baseUrl = "DÁN_LINK_EXEC_VÀO_ĐÂY";


    // =========================================================
    // QUESTION SETTINGS
    // =========================================================

    [Header("Thiết lập câu hỏi")]

    [Tooltip("Số câu tối đa trong một lượt. 0 = lấy toàn bộ.")]
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


    // =========================================================
    // QUESTION RUNTIME
    // =========================================================

    private List<QuestionData> questions =
        new List<QuestionData>();

    private Color[] originalColors;

    private int score;
    private int maxScore;
    private int correctCount;
    private int answeredCount;
    private int currentQ;

    private bool locked;
    private bool canMove;

    private bool historySaved = false;

    // =========================================================
    // GRID RUNTIME
    // =========================================================

    private bool[,] stoneAt;
    private bool[,] clearedAt;

    private int[,] questionAt;

    private GameObject[,] markerAt;

    private Vector2Int personCell;
    private Vector2Int pendingCell;
    private Vector2Int treasureCell;

    private RectTransform cellsRoot;

    private List<GameObject> spawned =
        new List<GameObject>();


    // =========================================================
    // START POSITION
    // =========================================================

    private bool initialSaved;

    private Vector3 personStartPos;
    private Vector3 treasureStartPos;


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
    // ENABLE
    // =========================================================

    void OnEnable()
    {
        StopAllCoroutines();

        StartCoroutine(
            Begin()
        );
    }


    void OnDisable()
    {
        StopAllCoroutines();

        locked = false;
        canMove = false;

        // Nếu game bị tắt trong lúc Person đang chạy
        // thì ép animation quay về Idle
        if (personAnimator != null)
        {
            personAnimator.SetBool("isRunning", false);
        }
    }


    // =========================================================
    // BEGIN
    // =========================================================

    IEnumerator Begin()
    {
        historySaved = false;
        CleanUp();

        canMove = false;
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
        // DEBUG GAME SESSION
        // =====================================================

        Debug.Log(
            "========== CAVE SESSION ==========\n"
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
        // SUBJECT CHECK
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
                "CAVE: GameSession.Subject đang trống!"
            );

            yield break;
        }


        // =====================================================
        // DIFFICULTY CHECK
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
                "CAVE: GameSession.Difficulties đang trống!"
            );

            yield break;
        }


        // =====================================================
        // QUESTION TYPE
        // =====================================================

        if (
            GameSession.QuestionType !=
            "Trắc nghiệm"
        )
        {
            questionText.text =
                "Game này hiện hỗ trợ Trắc nghiệm.";

            Debug.LogWarning(
                "CAVE: QuestionType = "
                + GameSession.QuestionType
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
            + UnityWebRequest.EscapeURL(
                mon
            )
            + "&difficulty="
            + UnityWebRequest.EscapeURL(
                difficultyString
            );


        Debug.Log(
            "CAVE URL: "
            + url
        );


        // =====================================================
        // REQUEST GOOGLE SHEET
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
                    "CAVE REQUEST ERROR: "
                    + req.error
                );


                yield break;
            }


            Debug.Log(
                "CAVE JSON: "
                + req.downloadHandler.text
            );


            // =================================================
            // JSON
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
                questionText.text =
                    "Không có câu hỏi phù hợp";


                Debug.LogError(
                    "CAVE: API không có câu hỏi."
                );


                yield break;
            }


            // =================================================
            // FILTER
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
                    "CAVE: BuildQuestionSet = 0 câu."
                );


                yield break;
            }
        }


        // =====================================================
        // REFERENCES CHECK
        // =====================================================

        if (
            person == null
            ||
            treasure == null
            ||
            cellTopLeft == null
            ||
            cellBottomRight == null
        )
        {
            questionText.text =
                "Chưa gán đủ Person / Treasure / Cell Top Left / Cell Bottom Right";


            Debug.LogError(
                "CaveQuizController: thiếu tham chiếu Inspector"
            );


            yield break;
        }


        // =====================================================
        // SCORE RESET
        // =====================================================

        maxScore = 0;


        foreach (
            QuestionData q
            in questions
        )
        {
            maxScore +=
                GetPoints(q);
        }


        score = 0;
        correctCount = 0;
        answeredCount = 0;

        locked = false;


        Debug.Log(
            "Cave lấy được "
            + questions.Count
            + " câu."
        );


        foreach (
            QuestionData q
            in questions
        )
        {
            Debug.Log(
                "CAVE POOL => "
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
        // START GRID
        // =====================================================

        SetupGrid();

        ShowHint();

        canMove = true;
    }


    // =========================================================
    // FILTER QUESTION
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
            "Cave filter chapter = ["
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

            bool difficultyOK =
                false;


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


        // Random
        Shuffle(pool);


        // Giới hạn số câu
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
    // SETUP GRID
    // =========================================================

    void SetupGrid()
    {
        stoneAt =
            new bool[columns, rows];

        clearedAt =
            new bool[columns, rows];

        questionAt =
            new int[columns, rows];

        markerAt =
            new GameObject[
                columns,
                rows
            ];


        for (
            int x = 0;
            x < columns;
            x++
        )
        {
            for (
                int y = 0;
                y < rows;
                y++
            )
            {
                questionAt[x, y] = -1;
            }
        }


        DisableRaycast(
            person.gameObject
        );

        DisableRaycast(
            treasure.gameObject
        );


        if (stoneTemplate)
        {
            stoneTemplate
                .SetActive(false);
        }


        if (questionMarker)
        {
            questionMarker
                .SetActive(false);
        }


        // =====================================================
        // SAVE START POSITION
        // =====================================================

        if (!initialSaved)
        {
            personStartPos =
                person.position;

            treasureStartPos =
                treasure.position;

            initialSaved = true;
        }


        person.position =
            personStartPos;

        treasure.position =
            treasureStartPos;


        personCell =
            NearestCell(
                personStartPos
            );

        treasureCell =
            NearestCell(
                treasureStartPos
            );


        Vector3 start =
            CellPos(
                personCell
            );


        start.z =
            person.position.z;


        person.position =
            start;


        // =====================================================
        // FREE CELLS
        // =====================================================

        List<Vector2Int> free =
            new List<Vector2Int>();


        for (
            int x = 0;
            x < columns;
            x++
        )
        {
            for (
                int y = 0;
                y < rows;
                y++
            )
            {
                Vector2Int c =
                    new Vector2Int(
                        x,
                        y
                    );


                if (
                    c != personCell
                    &&
                    c != treasureCell
                )
                {
                    free.Add(c);
                }
            }
        }


        // =====================================================
        // RANDOM STONES
        // =====================================================

        int obstacles =
            Mathf.Clamp(
                obstacleCount,
                0,
                Mathf.Max(
                    0,
                    free.Count
                    - questions.Count
                )
            );


        bool ok = false;


        for (
            int attempt = 0;
            attempt < 50
            &&
            !ok;
            attempt++
        )
        {
            Shuffle(free);


            System.Array.Clear(
                stoneAt,
                0,
                stoneAt.Length
            );


            for (
                int i = 0;
                i < obstacles;
                i++
            )
            {
                stoneAt[
                    free[i].x,
                    free[i].y
                ] = true;
            }


            ok =
                CanReachTreasure();
        }


        if (!ok)
        {
            System.Array.Clear(
                stoneAt,
                0,
                stoneAt.Length
            );
        }


        // =====================================================
        // GATE
        // =====================================================

        List<Vector2Int> gate =
            BuildGate();


        // =====================================================
        // QUESTION CELLS
        // =====================================================

        List<Vector2Int> rest =
            new List<Vector2Int>();


        foreach (
            Vector2Int c
            in free
        )
        {
            if (
                !stoneAt[c.x, c.y]
                &&
                !gate.Contains(c)
            )
            {
                rest.Add(c);
            }
        }


        Shuffle(rest);


        List<Vector2Int> qCells =
            new List<Vector2Int>(
                gate
            );


        for (
            int i = 0;
            qCells.Count
                < questions.Count
            &&
            i < rest.Count;
            i++
        )
        {
            qCells.Add(
                rest[i]
            );
        }


        for (
            int i = 0;
            i < qCells.Count;
            i++
        )
        {
            Vector2Int c =
                qCells[i];


            questionAt[
                c.x,
                c.y
            ] = i;


            if (
                questionMarker
            )
            {
                markerAt[
                    c.x,
                    c.y
                ] =
                    Spawn(
                        questionMarker,
                        c
                    );
            }
        }


        // =====================================================
        // SPAWN STONES
        // =====================================================

        if (stoneTemplate)
        {
            for (
                int x = 0;
                x < columns;
                x++
            )
            {
                for (
                    int y = 0;
                    y < rows;
                    y++
                )
                {
                    if (
                        stoneAt[x, y]
                    )
                    {
                        Spawn(
                            stoneTemplate,
                            new Vector2Int(
                                x,
                                y
                            )
                        );
                    }
                }
            }
        }


        BuildCellButtons();
    }


    // =========================================================
    // BUILD GATE
    // =========================================================

    List<Vector2Int> BuildGate()
    {
        List<Vector2Int> gate =
            new List<Vector2Int>();


        int[,] dist =
            BfsDist();


        int distT =
            dist[
                treasureCell.x,
                treasureCell.y
            ];


        if (distT < 2)
            return gate;


        List<List<Vector2Int>>
            layers =
                new List<
                    List<Vector2Int>
                >();


        for (
            int d = 0;
            d <= distT;
            d++
        )
        {
            layers.Add(
                new List<Vector2Int>()
            );
        }


        for (
            int x = 0;
            x < columns;
            x++
        )
        {
            for (
                int y = 0;
                y < rows;
                y++
            )
            {
                int d =
                    dist[x, y];


                if (
                    d >= 0
                    &&
                    d <= distT
                )
                {
                    layers[d]
                        .Add(
                            new Vector2Int(
                                x,
                                y
                            )
                        );
                }
            }
        }


        int need =
            Mathf.Min(
                Mathf.Max(
                    1,
                    gateCount
                ),
                questions.Count
            );


        int best =
            int.MaxValue;


        for (
            int d = 1;
            d < distT;
            d++
        )
        {
            if (
                layers[d].Count
                >= need
            )
            {
                best =
                    Mathf.Min(
                        best,
                        layers[d].Count
                    );
            }
        }


        if (
            best ==
            int.MaxValue
        )
        {
            for (
                int d = 1;
                d < distT;
                d++
            )
            {
                best =
                    Mathf.Min(
                        best,
                        layers[d].Count
                    );
            }
        }


        List<int> picks =
            new List<int>();


        for (
            int d = 1;
            d < distT;
            d++
        )
        {
            if (
                layers[d].Count
                    <= best + 1
                &&
                layers[d].Count > 0
            )
            {
                picks.Add(d);
            }
        }


        if (
            picks.Count == 0
        )
        {
            return gate;
        }


        List<Vector2Int> layer =
            layers[
                picks[
                    Random.Range(
                        0,
                        picks.Count
                    )
                ]
            ];


        int gateSize =
            Mathf.Clamp(
                Mathf.Max(
                    1,
                    gateCount
                ),
                1,
                Mathf.Min(
                    layer.Count,
                    questions.Count
                )
            );


        for (
            int attempt = 0;
            attempt < 30;
            attempt++
        )
        {
            Shuffle(layer);


            for (
                int i = gateSize;
                i < layer.Count;
                i++
            )
            {
                stoneAt[
                    layer[i].x,
                    layer[i].y
                ] = true;
            }


            if (
                CanReachTreasure()
            )
            {
                for (
                    int i = 0;
                    i < gateSize;
                    i++
                )
                {
                    gate.Add(
                        layer[i]
                    );
                }


                return gate;
            }


            for (
                int i = gateSize;
                i < layer.Count;
                i++
            )
            {
                stoneAt[
                    layer[i].x,
                    layer[i].y
                ] = false;
            }
        }


        Debug.LogWarning(
            "CaveQuizController: không dựng được cổng bắt buộc."
        );


        return gate;
    }


    // =========================================================
    // BFS
    // =========================================================

    int[,] BfsDist()
    {
        int[,] dist =
            new int[
                columns,
                rows
            ];


        for (
            int x = 0;
            x < columns;
            x++
        )
        {
            for (
                int y = 0;
                y < rows;
                y++
            )
            {
                dist[x, y] = -1;
            }
        }


        Queue<Vector2Int> queue =
            new Queue<Vector2Int>();


        queue.Enqueue(
            personCell
        );


        dist[
            personCell.x,
            personCell.y
        ] = 0;


        Vector2Int[] dirs =
        {
            Vector2Int.right,
            Vector2Int.left,
            Vector2Int.up,
            Vector2Int.down
        };


        while (
            queue.Count > 0
        )
        {
            Vector2Int c =
                queue.Dequeue();


            foreach (
                Vector2Int d
                in dirs
            )
            {
                Vector2Int n =
                    c + d;


                if (
                    n.x < 0
                    ||
                    n.y < 0
                    ||
                    n.x >= columns
                    ||
                    n.y >= rows
                )
                {
                    continue;
                }


                if (
                    dist[n.x, n.y]
                        != -1
                    ||
                    stoneAt[
                        n.x,
                        n.y
                    ]
                )
                {
                    continue;
                }


                dist[
                    n.x,
                    n.y
                ] =
                    dist[
                        c.x,
                        c.y
                    ] + 1;


                queue.Enqueue(n);
            }
        }


        return dist;
    }


    // =========================================================
    // CELL BUTTONS
    // =========================================================

    void BuildCellButtons()
    {
        GameObject rootGo =
            new GameObject(
                "CellButtons",
                typeof(RectTransform)
            );


        cellsRoot =
            rootGo
                .GetComponent<
                    RectTransform
                >();


        cellsRoot
            .SetParent(
                transform,
                false
            );


        cellsRoot
            .SetAsLastSibling();


        Vector3 a =
            cellTopLeft.position;

        Vector3 b =
            cellBottomRight.position;

        Vector3 s =
            transform.lossyScale;


        float wx =
            columns > 1
                ? Mathf.Abs(
                    b.x - a.x
                )
                / (columns - 1)
                : 100f;


        float wy =
            rows > 1
                ? Mathf.Abs(
                    b.y - a.y
                )
                / (rows - 1)
                : 100f;


        Vector2 size =
            new Vector2(
                wx /
                Mathf.Max(
                    s.x,
                    0.0001f
                ),
                wy /
                Mathf.Max(
                    s.y,
                    0.0001f
                )
            );


        for (
            int x = 0;
            x < columns;
            x++
        )
        {
            for (
                int y = 0;
                y < rows;
                y++
            )
            {
                GameObject go =
                    new GameObject(
                        "cell_"
                        + x
                        + "_"
                        + y,
                        typeof(RectTransform),
                        typeof(Image),
                        typeof(Button)
                    );


                RectTransform rt =
                    go.GetComponent<
                        RectTransform
                    >();


                rt.SetParent(
                    cellsRoot,
                    false
                );


                rt.anchorMin =
                rt.anchorMax =
                    new Vector2(
                        0.5f,
                        0.5f
                    );


                rt.pivot =
                    new Vector2(
                        0.5f,
                        0.5f
                    );


                rt.sizeDelta =
                    size;


                Vector2Int cell =
                    new Vector2Int(
                        x,
                        y
                    );


                rt.position =
                    CellPos(cell);


                Image img =
                    go.GetComponent<Image>();


                img.color =
                    new Color(
                        1f,
                        1f,
                        1f,
                        0f
                    );


                Button btn =
                    go.GetComponent<Button>();


                btn.transition =
                    Selectable
                        .Transition
                        .None;


                Vector2Int capturedCell =
                    cell;


                btn.onClick
                    .AddListener(
                        () =>
                            OnCellClicked(
                                capturedCell
                            )
                    );
            }
        }
    }


    // =========================================================
    // CELL CLICK
    // =========================================================

    void OnCellClicked(
        Vector2Int c
    )
    {
        if (!canMove)
            return;


        int dist =
            Mathf.Abs(
                c.x
                - personCell.x
            )
            +
            Mathf.Abs(
                c.y
                - personCell.y
            );


        if (dist != 1)
            return;


        if (
            stoneAt[
                c.x,
                c.y
            ]
        )
            return;


        int qi =
            questionAt[
                c.x,
                c.y
            ];


        if (
            qi >= 0
            &&
            !clearedAt[
                c.x,
                c.y
            ]
        )
        {
            canMove = false;

            pendingCell = c;

            ShowQuestion(qi);

            return;
        }


        StartCoroutine(
            WalkTo(c)
        );
    }


    // =========================================================
    // WALK
    // =========================================================

    IEnumerator WalkTo(
        Vector2Int target
    )
    {
        canMove = false;


        yield return
            StartCoroutine(
                MoveTo(target)
            );


        personCell =
            target;


        if (
            target ==
            treasureCell
        )
        {
            Finish(
                "Bạn đã tìm thấy kho báu! "
            );

            yield break;
        }


        ShowHint();

        canMove = true;
    }


    // =========================================================
    // MOVE
    // =========================================================

    IEnumerator MoveTo(Vector2Int cell)
    {
        // Lấy vị trí của ô cần di chuyển tới
        Vector3 dest = CellPos(cell);

        // Giữ nguyên trục Z của Person
        dest.z = person.position.z;


        // =====================================================
        // BẮT ĐẦU CHẠY
        // =====================================================

        if (personAnimator != null)
        {
            personAnimator.SetBool("isRunning", true);
        }


        // =====================================================
        // DI CHUYỂN TỚI Ô ĐÍCH
        // =====================================================

        while (
            (person.position - dest).sqrMagnitude
            > 0.01f
        )
        {
            person.position =
                Vector3.MoveTowards(
                    person.position,
                    dest,
                    moveSpeed * Time.deltaTime
                );

            yield return null;
        }


        // Đảm bảo Person nằm chính xác tại ô đích
        person.position = dest;


        // =====================================================
        // DỪNG CHẠY -> IDLE
        // =====================================================

        if (personAnimator != null)
        {
            personAnimator.SetBool("isRunning", false);
        }
    }


    // =========================================================
    // SHOW QUESTION
    // =========================================================

    void ShowQuestion(
        int qi
    )
    {
        currentQ = qi;


        QuestionData q =
            questions[qi];


        locked = false;


        Debug.Log(
            "CAVE ĐANG HIỂN THỊ => "
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


        questionText.text =
            q.question;


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


        locked = true;


        QuestionData q =
            questions[currentQ];


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


        answeredCount++;


        if (isCorrect)
        {
            score +=
                GetPoints(q);

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
                q.explanation;
        }


        UpdateScore();


        StartCoroutine(
            AfterAnswer(
                isCorrect
            )
        );
    }


    // =========================================================
    // AFTER ANSWER
    // =========================================================

    IEnumerator AfterAnswer(
        bool isCorrect
    )
    {
        yield return
            new WaitForSeconds(
                delayNext
            );


        SetAnswersVisible(false);


        if (
            explanationText
        )
        {
            explanationText.text = "";
        }


        Vector2Int qCell =
            pendingCell;


        clearedAt[
            qCell.x,
            qCell.y
        ] = true;


        if (
            markerAt[
                qCell.x,
                qCell.y
            ]
        )
        {
            markerAt[
                qCell.x,
                qCell.y
            ]
            .SetActive(false);
        }


        // =====================================================
        // CORRECT
        // =====================================================

        if (isCorrect)
        {
            yield return
                StartCoroutine(
                    MoveTo(
                        qCell
                    )
                );


            personCell =
                qCell;


            ShowHint();


            canMove = true;
        }

        // =====================================================
        // WRONG
        // =====================================================

        else
        {
            stoneAt[
                qCell.x,
                qCell.y
            ] = true;


            if (
                stoneTemplate
            )
            {
                Spawn(
                    stoneTemplate,
                    qCell
                );
            }


            if (
                !CanReachTreasure()
            )
            {
                Finish(
                    "Đường bị chặn hết rồi! "
                );


                yield break;
            }


            ShowHint();


            canMove = true;
        }
    }


    // =========================================================
    // CAN REACH TREASURE
    // =========================================================

    bool CanReachTreasure()
    {
        bool[,] seen =
            new bool[
                columns,
                rows
            ];


        Queue<Vector2Int> queue =
            new Queue<Vector2Int>();


        queue.Enqueue(
            personCell
        );


        seen[
            personCell.x,
            personCell.y
        ] = true;


        Vector2Int[] dirs =
        {
            Vector2Int.right,
            Vector2Int.left,
            Vector2Int.up,
            Vector2Int.down
        };


        while (
            queue.Count > 0
        )
        {
            Vector2Int c =
                queue.Dequeue();


            if (
                c ==
                treasureCell
            )
            {
                return true;
            }


            foreach (
                Vector2Int d
                in dirs
            )
            {
                Vector2Int n =
                    c + d;


                if (
                    n.x < 0
                    ||
                    n.y < 0
                    ||
                    n.x >= columns
                    ||
                    n.y >= rows
                )
                {
                    continue;
                }


                if (
                    seen[
                        n.x,
                        n.y
                    ]
                    ||
                    stoneAt[
                        n.x,
                        n.y
                    ]
                )
                {
                    continue;
                }


                seen[
                    n.x,
                    n.y
                ] = true;


                queue.Enqueue(n);
            }
        }


        return false;
    }


    // =========================================================
    // FINISH
    // =========================================================

    void Finish(string head)
    {
        canMove = false;
        locked = true;

        SetAnswersVisible(false);

        questionText.text =
            head
            + "Bạn được "
            + score
            + "/"
            + maxScore
            + " điểm"
            + " (đúng "
            + correctCount
            + "/"
            + answeredCount
            + " câu đã gặp)";

        if (explanationText)
        {
            explanationText.text = "";
        }

        if (cellsRoot)
        {
            cellsRoot
                .gameObject
                .SetActive(false);
        }

        SaveHistory();

        onFinished?.Invoke();
    }


    // =========================================================
    // HINT
    // =========================================================

    void ShowHint()
    {
        questionText.text =
            "Bấm vào ô bên cạnh nhân vật để đi tới kho báu!";
    }


    // =========================================================
    // SCORE 100
    // =========================================================

    int GetScoreOutOf100()
    {
        if (
            maxScore <= 0
        )
        {
            return 0;
        }


        float percent =
            (float)score
            /
            maxScore;


        return Mathf.RoundToInt(
            percent * 100f
        );
    }


    // =========================================================
    // POINT
    // =========================================================

    int GetPoints(QuestionData q)
    {
        if (q == null)
            return 0;

        return Mathf.Max(0, q.points);
    }


    // =========================================================
    // CELL POSITION
    // =========================================================

    Vector3 CellPos(
        Vector2Int c
    )
    {
        float tx =
            columns > 1
                ? (float)c.x
                    / (columns - 1)
                : 0f;


        float ty =
            rows > 1
                ? (float)c.y
                    / (rows - 1)
                : 0f;


        Vector3 a =
            cellTopLeft.position;


        Vector3 b =
            cellBottomRight.position;


        return new Vector3(
            Mathf.Lerp(
                a.x,
                b.x,
                tx
            ),
            Mathf.Lerp(
                a.y,
                b.y,
                ty
            ),
            a.z
        );
    }


    // =========================================================
    // NEAREST CELL
    // =========================================================

    Vector2Int NearestCell(
        Vector3 p
    )
    {
        Vector2Int best =
            Vector2Int.zero;


        float bestDist =
            float.MaxValue;


        for (
            int x = 0;
            x < columns;
            x++
        )
        {
            for (
                int y = 0;
                y < rows;
                y++
            )
            {
                Vector3 cp =
                    CellPos(
                        new Vector2Int(
                            x,
                            y
                        )
                    );


                float d =
                    new Vector2(
                        cp.x - p.x,
                        cp.y - p.y
                    )
                    .sqrMagnitude;


                if (
                    d < bestDist
                )
                {
                    bestDist = d;

                    best =
                        new Vector2Int(
                            x,
                            y
                        );
                }
            }
        }


        return best;
    }


    // =========================================================
    // SPAWN
    // =========================================================

    GameObject Spawn(
        GameObject template,
        Vector2Int c
    )
    {
        GameObject go =
            Instantiate(
                template,
                template
                    .transform
                    .parent
            );


        go.SetActive(true);


        (
            (RectTransform)
            go.transform
        ).position =
            CellPos(c);


        DisableRaycast(go);


        spawned.Add(go);


        return go;
    }


    // =========================================================
    // RAYCAST OFF
    // =========================================================

    void DisableRaycast(
        GameObject go
    )
    {
        foreach (
            Graphic g
            in go
                .GetComponentsInChildren<
                    Graphic
                >(true)
        )
        {
            g.raycastTarget = false;
        }
    }


    // =========================================================
    // CLEAN
    // =========================================================

    void CleanUp()
    {
        foreach (
            GameObject go
            in spawned
        )
        {
            if (go)
            {
                Destroy(go);
            }
        }


        spawned.Clear();


        if (
            cellsRoot
        )
        {
            Destroy(
                cellsRoot.gameObject
            );
        }


        cellsRoot = null;
    }


    // =========================================================
    // SHUFFLE
    // =========================================================
    // =========================================================
    // SAVE HISTORY
    // =========================================================

    void SaveHistory()
    {
        if (historySaved)
            return;

        historySaved = true;

        string username =
            PlayerPrefs.GetString(
                "Username",
                ""
            );

        if (string.IsNullOrWhiteSpace(username))
        {
            Debug.LogError(
                "CAVE: Không tìm thấy Username trong PlayerPrefs!"
            );

            return;
        }

        float percent = 0f;

        if (maxScore > 0)
        {
            percent =
                (float)score
                /
                maxScore
                *
                100f;
        }

        string result =
            percent >= 50f
                ? "THẮNG"
                : "THUA";

        Debug.Log(
            "CAVE SAVE HISTORY"
            + " | Username = " + username
            + " | Score = " + score
            + "/" + maxScore
            + " | Percent = " + percent
            + " | Result = " + result
        );

        StartCoroutine(
            HistoryAPI.SaveHistory(
                baseUrl,
                username,
                "Máy AI",
                "Luyện tập",
                result,
                score
            )
        );
    }
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


        Image img =
            answerButtons[i]
                .GetComponent<Image>();


        if (img)
        {
            img.color = c;
        }
    }


    // =========================================================
    // ANSWERS
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
        if (
            scoreText
        )
        {
            scoreText.text =
                GetScoreOutOf100()
                .ToString();
        }
    }


    // =========================================================
    // GIZMOS
    // =========================================================



    void OnDrawGizmosSelected()
    {
        if (
            cellTopLeft == null
            ||
            cellBottomRight == null
        )
        {
            return;
        }


        Gizmos.color =
            Color.yellow;


        for (
            int x = 0;
            x < columns;
            x++
        )
        {
            for (
                int y = 0;
                y < rows;
                y++
            )
            {
                Gizmos.DrawWireSphere(
                    CellPos(
                        new Vector2Int(
                            x,
                            y
                        )
                    ),
                    10f
                );
            }
        }
    }
}