using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Game bay dùng năng lượng (kiểu Flappy Bird):
//  - Mỗi lần chạm màn hình = 1 lần vỗ cánh, tốn năng lượng.
//  - Gần hết năng lượng => game dừng, hiện câu hỏi. Trả lời ĐÚNG => cộng năng lượng
//    bằng đúng số điểm của câu đó trong sheet (QuestionSetLoader.GetPoints).
//  - Mỗi cột bay qua được +pointsPerPillar điểm (mặc định 10).
//  - Chạm cột hoặc rơi xuống đất => thua.
// Gắn vào object "QuizManager" (nhớ tắt / gỡ script QuizManager cũ để khỏi đụng nhau).
public class FlyEnergyGame : MonoBehaviour
{
    [Header("Khu vực chơi & nhân vật (để trống = tự tìm)")]
    public RectTransform area;               // Fly_Game5 (mặc định là cha của object này)
    public RectTransform student;            // object tên "student"
    public RectTransform[] stones;           // để trống = tự lấy các con tên bắt đầu bằng "Stone"
    public float groundHeight = 140f;        // độ dày mặt đất tính từ đáy khung

    [Header("Bay")]
    public float gravity = 2200f;
    public float flapVelocity = 700f;
    public float pillarSpeed = 300f;
    public float pillarSpacing = 0f;         // 0 = tự tính theo khoảng cách cột trong Scene
    public float yJitter = 120f;             // cột tái sinh lệch lên/xuống ngẫu nhiên tối đa bấy nhiêu
    public float groupTolerance = 40f;       // các Stone có x gần nhau trong khoảng này = 1 cột (trên + dưới)
    [Range(0f, 0.45f)] public float studentShrink = 0.3f;   // thu nhỏ vùng va chạm của nhân vật cho dễ chơi
    [Range(0f, 0.45f)] public float stoneShrink = 0.05f;

    [Header("Năng lượng & điểm")]
    public int maxEnergy = 100;
    public int startEnergy = 60;
    public int energyPerFlap = 10;           // mỗi lần vỗ cánh tốn bao nhiêu
    public int askBelowEnergy = 15;          // năng lượng dưới mức này thì bắt trả lời câu hỏi
    public int pointsPerPillar = 10;         // điểm mỗi cột bay qua

    [Header("Câu hỏi")]
    public GameObject questionPanel;         // QsPanel1 (nếu có)
    public TMP_Text questionText;
    public Button[] answerButtons;           // A, B, C, D
    public TMP_Text[] answerTexts;
    public TMP_Text explanationText;         // tùy chọn

    [Header("Hiển thị (tùy chọn)")]
    public TMP_Text scoreText;
    public TMP_Text energyText;
    public Image energyFill;                 // Image kiểu Filled để làm thanh năng lượng
    public Slider energySlider;              // hoặc dùng Slider (chỉ cần gán 1 trong 2)
    public TMP_Text hintText;                // "Chạm để bay", "Game over"...
    public Button restartButton;             // nút chơi lại (hiện khi thua)
    public Button exitButton;                // nút thoát (hiện khi thua)

    [Header("Dữ liệu")]
    public string baseUrl = "DÁN_LINK_EXEC_VÀO_ĐÂY";
    public string subject = "Vật lý";        // chỉ dùng khi test riêng game
    public string chapter = "Chương 1";      // chỉ dùng khi test riêng game
    public int easyCount = 3;
    public int mediumCount = 2;
    public int hardCount = 1;
    public int targetPoints = 100;

    [Header("Màu phản hồi")]
    public Color correctColor = new Color(0.3f, 0.85f, 0.3f);
    public Color wrongColor = new Color(0.95f, 0.3f, 0.3f);
    public float delayNext = 1.2f;

    [Header("Sự kiện")]
    public UnityEvent onCorrect;
    public UnityEvent onWrong;
    public UnityEvent onFinished;
    public UnityEvent onExit;                // gọi khi bấm nút Thoát (tự nối trong Inspector)

    enum State { Loading, Ready, Playing, Asking, Over }
    State state = State.Loading;

    class Group
    {
        public List<RectTransform> items = new List<RectTransform>();
        public List<float> offsetX = new List<float>();
        public List<float> baseY = new List<float>();
        public float startX, x, dy;
        public bool passed;
    }

    List<Group> groups = new List<Group>();
    float spacing = 600f;
    Vector3 studentStart;
    float velY;
    int energy, score, correctCount, answeredCount;

    List<QuestionData> questions = new List<QuestionData>();
    List<int> order = new List<int>();
    int orderIdx;
    QuestionData currentQ;
    bool locked;
    Color[] originalColors;
    GameObject tapArea;
    bool built;

    // ---------- khởi tạo ----------
    void Awake()
    {
        if (!area) area = transform.parent as RectTransform;
        if (!student) student = FindChildByName("student");

        if (stones == null || stones.Length == 0)
        {
            var list = new List<RectTransform>();
            foreach (Transform t in area)
                if (t.name.ToLower().StartsWith("stone")) list.Add((RectTransform)t);
            stones = list.ToArray();
        }

        originalColors = new Color[answerButtons.Length];
        for (int i = 0; i < answerButtons.Length; i++)
        {
            int idx = i;
            var img = answerButtons[i].GetComponent<Image>();
            originalColors[i] = img ? img.color : Color.white;
            answerButtons[i].onClick.AddListener(() => OnAnswer(idx));
        }

        if (restartButton) restartButton.onClick.AddListener(Restart);
        if (exitButton) exitButton.onClick.AddListener(ExitGame);
    }

    public void ExitGame()
    {
        onExit?.Invoke();
    }

    void OnEnable()
    {
        Time.timeScale = 1f;   // script QuizManager cũ có thể để lại timeScale = 0
        StartCoroutine(Begin());
    }

    void OnDisable()
    {
        StopAllCoroutines();
        locked = false;
        if (state != State.Loading) state = State.Over;
    }

    IEnumerator Begin()
    {
        state = State.Loading;
        SetQuestionUI(false);
        if (restartButton) restartButton.gameObject.SetActive(false);
        if (exitButton) exitButton.gameObject.SetActive(false);
        EnsureTapArea();
        tapArea.SetActive(false);
        Say("Đang tải câu hỏi...");

        List<QuestionData> loaded = null;
        string error = null;
        // Môn / chương do bản đồ chặng đặt vào GameSession; trống thì dùng giá trị trong Inspector (test riêng)
        string useSubject = string.IsNullOrEmpty(GameSession.Subject) ? subject : GameSession.Subject;
        string useChapter = string.IsNullOrEmpty(GameSession.Chapter) ? chapter : GameSession.Chapter;
        Debug.Log("FlyEnergyGame: Subject=" + useSubject + " | Chapter=" + useChapter);

        yield return StartCoroutine(QuestionSetLoader.Load(
            baseUrl, useSubject, useChapter,
            easyCount, mediumCount, hardCount, targetPoints,
            (set, err) => { loaded = set; error = err; }));

        if (loaded == null || loaded.Count == 0)
        {
            Say(string.IsNullOrEmpty(error) ? "Không tải được câu hỏi" : error);
            yield break;
        }

        if (area == null || student == null || stones == null || stones.Length == 0)
        {
            Say("Thiếu Area / Student / Stone trong Inspector");
            Debug.LogError("FlyEnergyGame: thiếu tham chiếu");
            yield break;
        }

        questions = loaded;
        if (!built) { BuildGroups(); built = true; }
        ResetRun();
    }

    // ---------- chơi ----------
    void ResetRun()
    {
        energy = Mathf.Clamp(startEnergy, 0, maxEnergy);
        score = 0;
        correctCount = 0;
        answeredCount = 0;
        velY = 0f;
        order.Clear();
        orderIdx = 0;

        student.localPosition = studentStart;
        student.localRotation = Quaternion.identity;
        foreach (var g in groups)
        {
            g.x = g.startX;
            g.dy = 0f;
            g.passed = false;
            Place(g);
        }

        if (restartButton) restartButton.gameObject.SetActive(false);
        if (exitButton) exitButton.gameObject.SetActive(false);
        SetQuestionUI(false);
        UpdateUI();
        Say("Chạm để bay!");
        tapArea.SetActive(true);
        tapArea.transform.SetAsLastSibling();
        state = State.Ready;
    }

    public void Restart()
    {
        if (state == State.Over) ResetRun();
    }

    public void Flap()
    {
        if (state == State.Ready)
        {
            state = State.Playing;
            Say("");
        }
        if (state != State.Playing) return;
        if (energy < energyPerFlap) return;

        energy -= energyPerFlap;
        velY = flapVelocity;
        UpdateUI();
    }

    void Update()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Space)) Flap();
#endif
        if (state == State.Ready)
        {
            // đứng yên chờ chạm
            return;
        }
        if (state != State.Playing) return;

        float dt = Time.deltaTime;

        // nhân vật
        velY -= gravity * dt;
        Vector3 p = student.localPosition;
        p.y += velY * dt;
        float ceil = area.rect.height * 0.5f;
        if (p.y > ceil) { p.y = ceil; velY = Mathf.Min(velY, 0f); }
        student.localPosition = p;
        student.localRotation = Quaternion.Euler(0f, 0f, Mathf.Clamp(velY * 0.03f, -30f, 25f));

        Rect sRect = LocalRect(student, studentShrink);
        float floor = -area.rect.height * 0.5f + groundHeight;
        if (sRect.yMin < floor) { Finish("Rơi xuống đất rồi! "); return; }

        // cột
        float leftEdge = -area.rect.width * 0.5f - 100f;
        foreach (var g in groups)
        {
            g.x -= pillarSpeed * dt;
            Place(g);

            float right = GroupRight(g);

            if (!g.passed && right < sRect.xMin)
            {
                g.passed = true;
                score += pointsPerPillar;
                UpdateUI();
            }

            if (right < leftEdge)
            {
                float maxX = float.MinValue;
                foreach (var g2 in groups) maxX = Mathf.Max(maxX, g2.x);
                g.x = maxX + spacing;
                g.dy = Random.Range(-yJitter, yJitter);
                g.passed = false;
                Place(g);
            }
        }

        // va chạm
        foreach (var g in groups)
            foreach (var s in g.items)
            {
                if (!s.gameObject.activeInHierarchy) continue;
                if (LocalRect(s, stoneShrink).Overlaps(sRect))
                {
                    Finish("Va vào cột rồi! ");
                    return;
                }
            }

        // gần hết năng lượng => bắt trả lời
        if (energy < Mathf.Max(askBelowEnergy, energyPerFlap))
            BeginAsking();
    }

    // ---------- câu hỏi ----------
    void BeginAsking()
    {
        state = State.Asking;
        tapArea.SetActive(false);
        Say("");
        ShowNextQuestion();
    }

    void ShowNextQuestion()
    {
        if (order.Count == 0 || orderIdx >= order.Count)
        {
            order.Clear();
            for (int i = 0; i < questions.Count; i++) order.Add(i);
            Shuffle(order);
            orderIdx = 0;
        }
        currentQ = questions[order[orderIdx++]];
        locked = false;

        questionText.text = currentQ.question;
        string[] opts = { currentQ.optionA, currentQ.optionB, currentQ.optionC, currentQ.optionD };
        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (i < answerTexts.Length && answerTexts[i]) answerTexts[i].text = opts[i];
            var img = answerButtons[i].GetComponent<Image>();
            if (img) img.color = originalColors[i];
        }

        if (explanationText) explanationText.text = "";
        SetQuestionUI(true);
    }

    void OnAnswer(int chosen)
    {
        if (state != State.Asking || locked) return;
        locked = true;

        int correctIndex = ParseLetter(currentQ.correct);
        bool isCorrect = chosen == correctIndex;

        SetButtonColor(correctIndex, correctColor);
        if (!isCorrect) SetButtonColor(chosen, wrongColor);

        answeredCount++;
        if (isCorrect)
        {
            int gain = QuestionSetLoader.GetPoints(currentQ);   // cột điểm trong sheet = năng lượng nhận được
            energy = Mathf.Min(maxEnergy, energy + gain);
            correctCount++;
            onCorrect?.Invoke();
        }
        else onWrong?.Invoke();

        if (explanationText) explanationText.text = currentQ.explanation;
        UpdateUI();
        StartCoroutine(AfterAnswer());
    }

    IEnumerator AfterAnswer()
    {
        yield return new WaitForSeconds(delayNext);
        if (explanationText) explanationText.text = "";

        if (energy < Mathf.Max(askBelowEnergy, energyPerFlap))
        {
            // Vẫn chưa đủ năng lượng (trả lời sai hoặc câu ít điểm): hỏi tiếp
            ShowNextQuestion();
            yield break;
        }

        SetQuestionUI(false);
        Say("Chạm để bay tiếp!");
        tapArea.SetActive(true);
        tapArea.transform.SetAsLastSibling();
        state = State.Ready;
    }

    void Finish(string head)
    {
        state = State.Over;
        tapArea.SetActive(false);
        SetQuestionUI(false);
        Say(head + "Bạn được " + score + " điểm (đúng " + correctCount + "/" + answeredCount + " câu)");
        if (restartButton)
        {
            restartButton.gameObject.SetActive(true);
            restartButton.transform.SetAsLastSibling();
        }
        if (exitButton)
        {
            exitButton.gameObject.SetActive(true);
            exitButton.transform.SetAsLastSibling();
        }
        onFinished?.Invoke();
    }

    // ---------- cột ----------
    void BuildGroups()
    {
        groups.Clear();
        studentStart = student.localPosition;

        var list = new List<RectTransform>();
        foreach (var s in stones) if (s) list.Add(s);
        list.Sort((a, b) => a.localPosition.x.CompareTo(b.localPosition.x));

        Group cur = null;
        foreach (var s in list)
        {
            float x = s.localPosition.x;
            if (cur == null || x - cur.startX > groupTolerance)
            {
                cur = new Group { startX = x, x = x };
                groups.Add(cur);
            }
            cur.items.Add(s);
            cur.offsetX.Add(x - cur.startX);
            cur.baseY.Add(s.localPosition.y);
        }

        spacing = pillarSpacing;
        if (spacing <= 0f)
            spacing = groups.Count > 1
                ? (groups[groups.Count - 1].startX - groups[0].startX) / (groups.Count - 1)
                : 600f;
        if (spacing < 50f) spacing = 600f;
    }

    void Place(Group g)
    {
        for (int i = 0; i < g.items.Count; i++)
        {
            Vector3 lp = g.items[i].localPosition;
            lp.x = g.x + g.offsetX[i];
            lp.y = g.baseY[i] + g.dy;
            g.items[i].localPosition = lp;
        }
    }

    float GroupRight(Group g)
    {
        float right = float.MinValue;
        foreach (var s in g.items)
        {
            float r = s.localPosition.x + s.rect.xMax * s.localScale.x;
            if (r > right) right = r;
        }
        return right;
    }

    Rect LocalRect(RectTransform rt, float shrink)
    {
        Vector3 p = rt.localPosition;
        Vector3 sc = rt.localScale;
        Rect r = rt.rect;
        float w = r.width * Mathf.Abs(sc.x);
        float h = r.height * Mathf.Abs(sc.y);
        float x = p.x + r.x * sc.x;
        float y = p.y + r.y * sc.y;
        return new Rect(x + w * shrink, y + h * shrink, w * (1f - 2f * shrink), h * (1f - 2f * shrink));
    }

    // ---------- tiện ích ----------
    void EnsureTapArea()
    {
        if (tapArea != null) return;
        tapArea = new GameObject("TapArea", typeof(RectTransform), typeof(Image), typeof(Button));
        var rt = tapArea.GetComponent<RectTransform>();
        rt.SetParent(area, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        tapArea.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
        var btn = tapArea.GetComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.onClick.AddListener(Flap);
    }

    RectTransform FindChildByName(string childName)
    {
        if (!area) return null;
        foreach (Transform t in area)
            if (t.name.ToLower() == childName.ToLower()) return (RectTransform)t;
        return null;
    }

    void SetQuestionUI(bool show)
    {
        if (questionPanel) questionPanel.SetActive(show);
        if (questionText) questionText.gameObject.SetActive(show);
        foreach (var b in answerButtons) b.gameObject.SetActive(show);
        if (!show && explanationText) explanationText.text = "";
    }

    void Say(string msg)
    {
        if (hintText)
        {
            hintText.gameObject.SetActive(!string.IsNullOrEmpty(msg));
            hintText.text = msg;
        }
        else if (questionText && !string.IsNullOrEmpty(msg))
        {
            questionText.gameObject.SetActive(true);
            questionText.text = msg;
        }
    }

    void UpdateUI()
    {
        if (scoreText) scoreText.text = score.ToString();
        if (energyText) energyText.text = energy + "/" + maxEnergy;
        if (energyFill) energyFill.fillAmount = maxEnergy > 0 ? (float)energy / maxEnergy : 0f;
        if (energySlider)
        {
            energySlider.interactable = false;   // người chơi không kéo được
            energySlider.minValue = 0;
            energySlider.maxValue = maxEnergy;
            energySlider.value = energy;
        }
    }

    void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var tmp = list[i]; list[i] = list[j]; list[j] = tmp;
        }
    }

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
}