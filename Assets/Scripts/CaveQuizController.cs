using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class CaveQuizController : MonoBehaviour
{
    [Header("Khung câu hỏi")]
    public TMP_Text questionText;

    [Header("4 đáp án, theo thứ tự A, B, C, D")]
    public Button[] answerButtons;         // AnswerA..AnswerD
    public TMP_Text[] answerTexts;         // chữ trong từng nút

    [Header("Tùy chọn")]
    public TMP_Text scoreText;
    public TMP_Text explanationText;

    [Header("Lưới (matrix)")]
    public RectTransform cellTopLeft;      // object trống đặt đúng TÂM ô góc trên-trái
    public RectTransform cellBottomRight;  // object trống đặt đúng TÂM ô góc dưới-phải
    public int columns = 8;                // số cột (ô) theo chiều ngang
    public int rows = 6;                   // số hàng (ô) theo chiều dọc

    [Header("Nhân vật, kho báu, đá")]
    public RectTransform person;           // "Person" (đang đứng ở ô xuất phát)
    public RectTransform treasure;         // "box_treasure" (đang nằm ở ô đích)
    public GameObject stoneTemplate;       // "Stone" (dùng làm mẫu, game sẽ nhân bản ra)
    public GameObject questionMarker;      // "Question" (dấu ? đặt lên các ô câu hỏi)
    public int obstacleCount = 6;          // số tảng đá chắn ngẫu nhiên lúc bắt đầu
    public int gateCount = 2;              // số ô ? ở "cổng" bắt buộc phải qua để tới kho báu
    public float moveSpeed = 600f;

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
    public float delayNext = 1.2f;

    [Header("Sự kiện")]
    public UnityEvent onCorrect;
    public UnityEvent onWrong;
    public UnityEvent onFinished;

    List<QuestionData> questions = new List<QuestionData>();
    Color[] originalColors;
    int score, maxScore, correctCount, answeredCount, currentQ;
    bool locked, canMove;

    bool[,] stoneAt;
    bool[,] clearedAt;
    int[,] questionAt;
    GameObject[,] markerAt;
    Vector2Int personCell, pendingCell, treasureCell;
    RectTransform cellsRoot;
    List<GameObject> spawned = new List<GameObject>();

    // Lưu vị trí ban đầu (chỉ lưu 1 lần) để mỗi lần chơi lại nhân vật về đúng chỗ xuất phát
    bool initialSaved;
    Vector3 personStartPos, treasureStartPos;

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

    void OnDisable()
    {
        // Thoát game giữa chừng: dừng các coroutine đang chạy dở
        StopAllCoroutines();
        locked = false;
        canMove = false;
    }

    IEnumerator Begin()
    {
        CleanUp();
        canMove = false;
        SetAnswersVisible(false);
        questionText.text = "Đang tải câu hỏi...";
        if (explanationText) explanationText.text = "";
        if (scoreText) scoreText.text = "0";

        List<QuestionData> loaded = null;
        string error = null;

        yield return StartCoroutine(QuestionSetLoader.Load(
            baseUrl, subject, chapter,
            easyCount, mediumCount, hardCount, targetPoints,
            (set, err) => { loaded = set; error = err; }));

        if (loaded == null)
        {
            questionText.text = string.IsNullOrEmpty(error) ? "Không tải được câu hỏi" : error;
            yield break;
        }

        if (person == null || treasure == null || cellTopLeft == null || cellBottomRight == null)
        {
            questionText.text = "Chưa gán đủ Person / Treasure / Cell Top Left / Cell Bottom Right";
            Debug.LogError("CaveQuizController: thiếu tham chiếu trong Inspector");
            yield break;
        }

        questions = loaded;
        maxScore = 0;
        foreach (var q in questions) maxScore += QuestionSetLoader.GetPoints(q);
        score = 0;
        correctCount = 0;
        answeredCount = 0;

        SetupGrid();
        ShowHint();
        canMove = true;
    }

    // ---------- dựng lưới ----------
    void SetupGrid()
    {
        stoneAt = new bool[columns, rows];
        clearedAt = new bool[columns, rows];
        questionAt = new int[columns, rows];
        markerAt = new GameObject[columns, rows];
        for (int x = 0; x < columns; x++)
            for (int y = 0; y < rows; y++)
                questionAt[x, y] = -1;

        DisableRaycast(person.gameObject);
        DisableRaycast(treasure.gameObject);
        if (stoneTemplate) stoneTemplate.SetActive(false);
        if (questionMarker) questionMarker.SetActive(false);

        // Lần đầu: ghi nhớ vị trí xuất phát / kho báu đặt trong Scene.
        // Các lần chơi lại: trả về đúng vị trí đã lưu (tránh bị kẹt ở vạch đích).
        if (!initialSaved)
        {
            personStartPos = person.position;
            treasureStartPos = treasure.position;
            initialSaved = true;
        }
        person.position = personStartPos;
        treasure.position = treasureStartPos;

        personCell = NearestCell(personStartPos);
        treasureCell = NearestCell(treasureStartPos);
        Vector3 start = CellPos(personCell);
        start.z = person.position.z;
        person.position = start;

        // Các ô trống (không phải ô xuất phát / đích)
        var free = new List<Vector2Int>();
        for (int x = 0; x < columns; x++)
            for (int y = 0; y < rows; y++)
            {
                var c = new Vector2Int(x, y);
                if (c != personCell && c != treasureCell) free.Add(c);
            }

        // 1) Đá chắn ngẫu nhiên, đảm bảo vẫn còn đường tới kho báu
        int obstacles = Mathf.Clamp(obstacleCount, 0, Mathf.Max(0, free.Count - questions.Count));
        bool ok = false;
        for (int attempt = 0; attempt < 50 && !ok; attempt++)
        {
            Shuffle(free);
            System.Array.Clear(stoneAt, 0, stoneAt.Length);
            for (int i = 0; i < obstacles; i++) stoneAt[free[i].x, free[i].y] = true;
            ok = CanReachTreasure();
        }
        if (!ok) System.Array.Clear(stoneAt, 0, stoneAt.Length);

        // 2) "Cổng": hàng đá chắn ngang đường, chỉ chừa vài ô ? => bắt buộc phải trả lời mới tới được kho báu
        List<Vector2Int> gate = BuildGate();

        // 3) Các câu hỏi còn lại đặt ngẫu nhiên vào ô trống
        var rest = new List<Vector2Int>();
        foreach (var c in free)
            if (!stoneAt[c.x, c.y] && !gate.Contains(c)) rest.Add(c);
        Shuffle(rest);

        var qCells = new List<Vector2Int>(gate);
        for (int i = 0; qCells.Count < questions.Count && i < rest.Count; i++) qCells.Add(rest[i]);

        for (int i = 0; i < qCells.Count; i++)
        {
            var c = qCells[i];
            questionAt[c.x, c.y] = i;
            if (questionMarker) markerAt[c.x, c.y] = Spawn(questionMarker, c);
        }

        // 4) Hiện tất cả các tảng đá lên lưới
        if (stoneTemplate)
            for (int x = 0; x < columns; x++)
                for (int y = 0; y < rows; y++)
                    if (stoneAt[x, y]) Spawn(stoneTemplate, new Vector2Int(x, y));

        BuildCellButtons();
    }

    // Dựng "cổng" bắt buộc: chọn một lớp ô cách điểm xuất phát d bước.
    // Mọi đường tới kho báu đều phải đi qua đúng một ô của lớp này,
    // nên chỉ giữ lại vài ô làm ô ?, các ô còn lại của lớp biến thành đá.
    List<Vector2Int> BuildGate()
    {
        var gate = new List<Vector2Int>();
        int[,] dist = BfsDist();
        int distT = dist[treasureCell.x, treasureCell.y];
        if (distT < 2) return gate;   // kho báu sát ô xuất phát, không dựng được cổng

        var layers = new List<List<Vector2Int>>();
        for (int d = 0; d <= distT; d++) layers.Add(new List<Vector2Int>());
        for (int x = 0; x < columns; x++)
            for (int y = 0; y < rows; y++)
            {
                int d = dist[x, y];
                if (d >= 0 && d <= distT) layers[d].Add(new Vector2Int(x, y));
            }

        // Ưu tiên lớp đủ ô cho cổng (để sai 1 câu chưa thua ngay), rồi chọn lớp ít ô nhất
        int need = Mathf.Min(Mathf.Max(1, gateCount), questions.Count);
        int best = int.MaxValue;
        for (int d = 1; d < distT; d++)
            if (layers[d].Count >= need) best = Mathf.Min(best, layers[d].Count);
        if (best == int.MaxValue)
            for (int d = 1; d < distT; d++) best = Mathf.Min(best, layers[d].Count);

        var picks = new List<int>();
        for (int d = 1; d < distT; d++)
            if (layers[d].Count <= best + 1 && layers[d].Count > 0) picks.Add(d);
        if (picks.Count == 0) return gate;

        var layer = layers[picks[Random.Range(0, picks.Count)]];
        int gateSize = Mathf.Clamp(Mathf.Max(1, gateCount), 1, Mathf.Min(layer.Count, questions.Count));

        for (int attempt = 0; attempt < 30; attempt++)
        {
            Shuffle(layer);
            for (int i = gateSize; i < layer.Count; i++) stoneAt[layer[i].x, layer[i].y] = true;
            if (CanReachTreasure())
            {
                for (int i = 0; i < gateSize; i++) gate.Add(layer[i]);
                return gate;
            }
            for (int i = gateSize; i < layer.Count; i++) stoneAt[layer[i].x, layer[i].y] = false;
        }

        Debug.LogWarning("CaveQuizController: không dựng được cổng bắt buộc, chạy bản thường.");
        return gate;
    }

    // Khoảng cách (số bước) từ ô nhân vật tới mọi ô, đá chắn thì không đi qua; -1 = không tới được
    int[,] BfsDist()
    {
        var dist = new int[columns, rows];
        for (int x = 0; x < columns; x++)
            for (int y = 0; y < rows; y++) dist[x, y] = -1;

        var queue = new Queue<Vector2Int>();
        queue.Enqueue(personCell);
        dist[personCell.x, personCell.y] = 0;
        Vector2Int[] dirs = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

        while (queue.Count > 0)
        {
            var c = queue.Dequeue();
            foreach (var d in dirs)
            {
                var n = c + d;
                if (n.x < 0 || n.y < 0 || n.x >= columns || n.y >= rows) continue;
                if (dist[n.x, n.y] != -1 || stoneAt[n.x, n.y]) continue;
                dist[n.x, n.y] = dist[c.x, c.y] + 1;
                queue.Enqueue(n);
            }
        }
        return dist;
    }

    void BuildCellButtons()
    {
        var rootGo = new GameObject("CellButtons", typeof(RectTransform));
        cellsRoot = rootGo.GetComponent<RectTransform>();
        cellsRoot.SetParent(transform, false);
        cellsRoot.SetAsLastSibling();

        Vector3 a = cellTopLeft.position;
        Vector3 b = cellBottomRight.position;
        Vector3 s = transform.lossyScale;
        float wx = columns > 1 ? Mathf.Abs(b.x - a.x) / (columns - 1) : 100f;
        float wy = rows > 1 ? Mathf.Abs(b.y - a.y) / (rows - 1) : 100f;
        Vector2 size = new Vector2(wx / Mathf.Max(s.x, 0.0001f), wy / Mathf.Max(s.y, 0.0001f));

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                var go = new GameObject("cell_" + x + "_" + y,
                    typeof(RectTransform), typeof(Image), typeof(Button));
                var rt = go.GetComponent<RectTransform>();
                rt.SetParent(cellsRoot, false);
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = size;
                rt.position = CellPos(new Vector2Int(x, y));

                var img = go.GetComponent<Image>();
                img.color = new Color(1f, 1f, 1f, 0f);   // trong suốt nhưng vẫn bấm được

                var btn = go.GetComponent<Button>();
                btn.transition = Selectable.Transition.None;
                var cell = new Vector2Int(x, y);
                btn.onClick.AddListener(() => OnCellClicked(cell));
            }
        }
    }

    // ---------- di chuyển ----------
    void OnCellClicked(Vector2Int c)
    {
        if (!canMove) return;
        int dist = Mathf.Abs(c.x - personCell.x) + Mathf.Abs(c.y - personCell.y);
        if (dist != 1) return;              // chỉ đi sang ô kề bên
        if (stoneAt[c.x, c.y]) return;      // ô có đá thì không đi được

        int qi = questionAt[c.x, c.y];
        if (qi >= 0 && !clearedAt[c.x, c.y])
        {
            // Ô dấu ?: phải trả lời câu hỏi trước khi bước vào
            canMove = false;
            pendingCell = c;
            ShowQuestion(qi);
            return;
        }

        StartCoroutine(WalkTo(c));
    }

    IEnumerator WalkTo(Vector2Int target)
    {
        canMove = false;
        yield return StartCoroutine(MoveTo(target));
        personCell = target;

        if (target == treasureCell)
        {
            Finish("Bạn đã tìm thấy kho báu! ");
            yield break;
        }

        ShowHint();
        canMove = true;
    }

    IEnumerator MoveTo(Vector2Int cell)
    {
        Vector3 dest = CellPos(cell);
        dest.z = person.position.z;
        while ((person.position - dest).sqrMagnitude > 0.01f)
        {
            person.position = Vector3.MoveTowards(person.position, dest, moveSpeed * Time.deltaTime);
            yield return null;
        }
        person.position = dest;
    }

    // ---------- câu hỏi ----------
    void ShowQuestion(int qi)
    {
        currentQ = qi;
        var q = questions[qi];
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
        SetAnswersVisible(true);
    }

    void OnAnswer(int chosen)
    {
        if (locked) return;
        locked = true;

        var q = questions[currentQ];
        int correctIndex = ParseLetter(q.correct);
        bool isCorrect = chosen == correctIndex;

        SetButtonColor(correctIndex, correctColor);
        if (!isCorrect) SetButtonColor(chosen, wrongColor);

        answeredCount++;
        if (isCorrect)
        {
            score += QuestionSetLoader.GetPoints(q);
            correctCount++;
            onCorrect?.Invoke();
        }
        else onWrong?.Invoke();

        if (explanationText) explanationText.text = q.explanation;
        UpdateScore();
        StartCoroutine(AfterAnswer(isCorrect));
    }

    IEnumerator AfterAnswer(bool isCorrect)
    {
        yield return new WaitForSeconds(delayNext);
        SetAnswersVisible(false);
        if (explanationText) explanationText.text = "";

        Vector2Int qCell = pendingCell;
        clearedAt[qCell.x, qCell.y] = true;
        if (markerAt[qCell.x, qCell.y]) markerAt[qCell.x, qCell.y].SetActive(false);

        if (isCorrect)
        {
            // Đúng: nhân vật bước vào ô đó và đi tiếp
            yield return StartCoroutine(MoveTo(qCell));
            personCell = qCell;
            ShowHint();
            canMove = true;
        }
        else
        {
            // Sai: tảng đá rơi xuống chặn ô đó, nhân vật đứng yên
            stoneAt[qCell.x, qCell.y] = true;
            if (stoneTemplate) Spawn(stoneTemplate, qCell);

            if (!CanReachTreasure())
            {
                Finish("Đường bị chặn hết rồi! ");
                yield break;
            }

            ShowHint();
            canMove = true;
        }
    }

    bool CanReachTreasure()
    {
        var seen = new bool[columns, rows];
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(personCell);
        seen[personCell.x, personCell.y] = true;
        Vector2Int[] dirs = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

        while (queue.Count > 0)
        {
            var c = queue.Dequeue();
            if (c == treasureCell) return true;
            foreach (var d in dirs)
            {
                var n = c + d;
                if (n.x < 0 || n.y < 0 || n.x >= columns || n.y >= rows) continue;
                if (seen[n.x, n.y] || stoneAt[n.x, n.y]) continue;
                seen[n.x, n.y] = true;
                queue.Enqueue(n);
            }
        }
        return false;
    }

    void Finish(string head)
    {
        canMove = false;
        SetAnswersVisible(false);
        questionText.text = head + "Bạn được " + score + " điểm (đúng " + correctCount + "/" + answeredCount + " câu đã gặp)";
        if (explanationText) explanationText.text = "";
        if (cellsRoot) cellsRoot.gameObject.SetActive(false);
        onFinished?.Invoke();
    }

    void ShowHint()
    {
        questionText.text = "Bấm vào ô bên cạnh nhân vật để đi tới kho báu!";
    }

    // ---------- tiện ích ----------
    Vector3 CellPos(Vector2Int c)
    {
        float tx = columns > 1 ? (float)c.x / (columns - 1) : 0f;
        float ty = rows > 1 ? (float)c.y / (rows - 1) : 0f;   // hàng 0 = hàng trên cùng
        Vector3 a = cellTopLeft.position;
        Vector3 b = cellBottomRight.position;
        return new Vector3(Mathf.Lerp(a.x, b.x, tx), Mathf.Lerp(a.y, b.y, ty), a.z);
    }

    Vector2Int NearestCell(Vector3 p)
    {
        Vector2Int best = Vector2Int.zero;
        float bestDist = float.MaxValue;
        for (int x = 0; x < columns; x++)
            for (int y = 0; y < rows; y++)
            {
                Vector3 cp = CellPos(new Vector2Int(x, y));
                float d = new Vector2(cp.x - p.x, cp.y - p.y).sqrMagnitude;
                if (d < bestDist) { bestDist = d; best = new Vector2Int(x, y); }
            }
        return best;
    }

    GameObject Spawn(GameObject template, Vector2Int c)
    {
        var go = Instantiate(template, template.transform.parent);
        go.SetActive(true);
        ((RectTransform)go.transform).position = CellPos(c);
        DisableRaycast(go);
        spawned.Add(go);
        return go;
    }

    void DisableRaycast(GameObject go)
    {
        foreach (var g in go.GetComponentsInChildren<Graphic>(true)) g.raycastTarget = false;
    }

    void CleanUp()
    {
        foreach (var go in spawned) if (go) Destroy(go);
        spawned.Clear();
        if (cellsRoot) Destroy(cellsRoot.gameObject);
        cellsRoot = null;
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

    void SetAnswersVisible(bool show)
    {
        foreach (var b in answerButtons) b.gameObject.SetActive(show);
    }

    void UpdateScore()
    {
        if (scoreText) scoreText.text = score.ToString();
    }

    // Chọn Cave_dao_2 trong Hierarchy: các vòng tròn vàng trong Scene là TÂM từng ô.
    // Dùng để kiểm tra Columns / Rows và 2 điểm mốc đã khớp với lưới chưa.
    void OnDrawGizmosSelected()
    {
        if (cellTopLeft == null || cellBottomRight == null) return;
        Gizmos.color = Color.yellow;
        for (int x = 0; x < columns; x++)
            for (int y = 0; y < rows; y++)
                Gizmos.DrawWireSphere(CellPos(new Vector2Int(x, y)), 10f);
    }
}