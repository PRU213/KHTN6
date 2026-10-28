using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[System.Serializable]
public class Question
{
    public string text;
    public string[] options;   // đúng 4 đáp án
    public int correct;        // vị trí đáp án đúng (0-3)
}

public class BlastRoundManager : MonoBehaviour
{
    [Header("Đối kháng online")]
    public bool enableOnlineDuel = true;
    public bool OnlineControlled { get; set; }
    public bool PlayWithBot { get; set; }
    public System.Action<int, int> MatchFinished;
    float botThinkAt;
    [Header("UI")]
    public TMP_Text questionText;
    public TMP_Text timerText;
    public TMP_Text[] blobTexts;       // 4 Label
    public Image[] blobImages;         // 4 Image của Blob
    public TMP_Text[] scoreTexts;      // [0]=P1, [1]=P2
    public GameObject resultPanel;
    public TMP_Text resultText;

    [Header("Tàu (0 = P1 bên trái, 1 = P2 bên phải)")]
    public RectTransform[] guns;                          // ship_vu_tru_1, ship_vu_tru_2
    public KeyCode[] fireKeys = { KeyCode.A, KeyCode.L };
    public float[] centerAngle = { -35f, 35f };           // âm = nghiêng sang phải
    public float swingAngle = 45f;
    public float[] swaySpeed = { 1.6f, 1.9f };
    public float gunLength = 90f;                         // từ tâm tàu đến mũi tàu
    public float fireCooldown = 0.35f;
    public float stunTime = 1.5f;                         // bắn trúng đáp án sai thì bị khóa bắn
    public Color stunColor = new Color(0.4f, 0.4f, 0.4f);

    [Header("Đạn")]
    public Sprite bulletSprite;                           // chọn "Knob" cho đạn tròn
    public Color[] bulletColors = {
        new Color(0.3f, 0.82f, 1f), new Color(1f, 0.48f, 0.78f) };
    public Vector2 bulletSize = new Vector2(36f, 36f);
    public float bulletSpeed = 1500f;
    public float blobRadius = 110f;
    public RectTransform bulletParent;                    // để trống = tự lấy object cha

    [Header("Config")]
    public float roundTime = 20f;
    public int questionsPerGame = 5;
    public bool useDemoData = true;
    public Color normalColor = Color.white;
    public Color correctColor = new Color(0.3f, 0.9f, 0.4f);
    public Color wrongColor = new Color(0.95f, 0.3f, 0.3f);
    public float bulletAngleOffset = 0f;

    class Bullet
    {
        public RectTransform rt;
        public Vector2 dir;
        public int player;
    }

    List<Question> questions = new List<Question>();
    List<Bullet> bullets = new List<Bullet>();
    Image[] gunImages;
    int qIndex, correctIndex;
    int[] scores = new int[2];
    float[] nextFireTime = new float[2];
    float[] stunUntil = new float[2];
    bool[] blobShot = new bool[4];
    bool roundActive;
    float timeLeft;

    void OnEnable()
    {
        if (guns == null || guns.Length < 2 || guns[0] == null || guns[1] == null)
        {
            Debug.LogError("BlastManager: chưa kéo 2 tàu vào ô Guns!");
            enabled = false;
            return;
        }

        if (bulletParent == null) bulletParent = guns[0].parent as RectTransform;
        gunImages = new Image[2];
        for (int p = 0; p < 2; p++) gunImages[p] = guns[p].GetComponent<Image>();

        if (enableOnlineDuel)
        {
            OnlineControlled = true;
            var duel = GetComponent<PlanetDuelController>();
            if (duel == null) duel = gameObject.AddComponent<PlanetDuelController>();
            duel.Open(this);
        }
        else if (useDemoData) StartGame(DemoQuestions());
    }

    void OnDisable()
    {
        StopAllCoroutines();
        ClearBullets();
        roundActive = false;
    }

    public void StartGame(List<Question> source)
    {
        StopAllCoroutines();
        ClearBullets();
        questions.Clear();
        foreach (var q in source) questions.Add(Shuffle(q));
        if (questions.Count > questionsPerGame)
            questions = questions.GetRange(0, questionsPerGame);

        qIndex = 0;
        scores = new int[2];
        foreach (var s in scoreTexts) s.text = "0";
        resultPanel.SetActive(false);
        StartRound();
    }

    void StartRound()
    {
        if (qIndex >= questions.Count) { EndGame(); return; }

        var q = questions[qIndex];
        questionText.text = q.text;
        correctIndex = q.correct;

        for (int i = 0; i < 4; i++)
        {
            blobTexts[i].text = q.options[i];
            blobImages[i].color = normalColor;
            blobShot[i] = false;
        }

        for (int p = 0; p < 2; p++) { stunUntil[p] = 0f; nextFireTime[p] = 0f; }
        timeLeft = roundTime;
        botThinkAt = Time.time + Random.Range(3f, 5f);
        roundActive = true;
    }

    void Update()
    {
        if (OnlineControlled) return;
        UpdateGuns();        // tàu luôn lắc mũi, kể cả giữa các vòng
        if (!roundActive) return;

        timeLeft -= Time.deltaTime;
        timerText.text = Mathf.CeilToInt(Mathf.Max(0, timeLeft)).ToString();
        if (timeLeft <= 0) { EndRound(-1); return; }

        for (int p = 0; p < (PlayWithBot ? 0 : 2); p++)
            if (Pressed(p)) TryFire(p);
        if (PlayWithBot && Time.time >= botThinkAt)
        {
            botThinkAt = Time.time + Random.Range(1f, 2f);
            int target = Random.value < .75f ? correctIndex : Random.Range(0, 4);
            Vector2 aim = blobImages[target].rectTransform.position - guns[1].position;
            if (Vector2.Angle(guns[1].up, aim) < 16f) TryFire(1);
        }

        MoveBullets();
    }

    public static bool Pressed(int player)
    {
#if ENABLE_INPUT_SYSTEM
        var keyboard = Keyboard.current;
        return keyboard != null && (player == 0 ? keyboard.aKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame : keyboard.lKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(player == 0 ? KeyCode.A : KeyCode.L) || (player == 0 && Input.GetKeyDown(KeyCode.Space));
#endif
    }

    public void FireLocal() { if (!OnlineControlled && roundActive) TryFire(0); }
    public void ChooseLocal(int answer)
    {
        if (OnlineControlled || !roundActive || timeLeft <= 0 || answer < 0 || answer >= 4 ||
            blobShot[answer] || Time.time < stunUntil[0]) return;
        OnBlobHit(0, answer);
    }
    public void StopMatch() { StopAllCoroutines(); ClearBullets(); roundActive = false; }

    void UpdateGuns()
    {
        for (int p = 0; p < 2; p++)
        {
            float a = centerAngle[p] + Mathf.Sin(Time.time * swaySpeed[p]) * swingAngle;
            guns[p].localRotation = Quaternion.Euler(0f, 0f, a);
            if (gunImages[p] != null)
                gunImages[p].color = Time.time < stunUntil[p] ? stunColor : Color.white;
        }
    }

    void TryFire(int p)
    {
        if (Time.time < nextFireTime[p] || Time.time < stunUntil[p]) return;
        nextFireTime[p] = Time.time + fireCooldown;

        Vector2 dir = ((Vector2)bulletParent.InverseTransformDirection(guns[p].up)).normalized;
        Vector2 muzzle = (Vector2)bulletParent.InverseTransformPoint(guns[p].position) + dir * gunLength;

        var go = new GameObject("Bullet_P" + (p + 1), typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(bulletParent, false);
        rt.anchorMin = rt.anchorMax = bulletParent.pivot;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = bulletSize;
        rt.anchoredPosition = muzzle;

        var img = go.GetComponent<Image>();
        img.sprite = bulletSprite;
        img.color = bulletSprite != null ? Color.white : bulletColors[p];
        img.raycastTarget = false;
        img.preserveAspect = true;
        float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        rt.localRotation = Quaternion.Euler(0f, 0f, ang + bulletAngleOffset);

        bullets.Add(new Bullet { rt = rt, dir = dir, player = p });
    }

    void MoveBullets()
    {
        for (int i = bullets.Count - 1; i >= 0; i--)
        {
            var b = bullets[i];
            Vector2 pos = b.rt.anchoredPosition + b.dir * bulletSpeed * Time.deltaTime;
            b.rt.anchoredPosition = pos;

            if (!new Rect(bulletParent.rect.xMin - 150f, bulletParent.rect.yMin - 150f,
                bulletParent.rect.width + 300f, bulletParent.rect.height + 300f).Contains(pos))
            {
                RemoveBullet(i);
                continue;
            }

            for (int k = 0; k < 4; k++)
            {
                if (blobShot[k]) continue;   // hố đã bị bắn sai thì đạn xuyên qua
                float d = Vector2.Distance(pos, bulletParent.InverseTransformPoint(blobImages[k].rectTransform.position));
                if (d < blobRadius)
                {
                    int player = b.player;
                    RemoveBullet(i);
                    OnBlobHit(player, k);
                    break;
                }
            }
            if (!roundActive) return;        // vòng đã kết thúc, đạn đã bị xóa hết
        }
    }

    void OnBlobHit(int player, int blob)
    {
        if (blob == correctIndex)
        {
            int pts = 10 + Mathf.RoundToInt(timeLeft);   // bắn sớm thì điểm cao hơn
            scores[player] += pts;
            scoreTexts[player].text = scores[player].ToString();
            EndRound(player);
        }
        else
        {
            scores[player] -= 10;
            scoreTexts[player].text = scores[player].ToString();
            blobShot[blob] = true;
            blobImages[blob].color = wrongColor;
            stunUntil[player] = Time.time + stunTime;
        }
    }

    void EndRound(int winner)
    {
        roundActive = false;
        ClearBullets();
        blobImages[correctIndex].color = correctColor;
        StartCoroutine(NextRound());
    }

    IEnumerator NextRound()
    {
        yield return new WaitForSeconds(1.5f);
        qIndex++;
        StartRound();
    }

    void EndGame()
    {
        roundActive = false;
        ClearBullets();
        if (MatchFinished != null) { MatchFinished(scores[0], scores[1]); return; }
        resultPanel.SetActive(true);
        resultText.text =
            $"P1: {scores[0]}  -  P2: {scores[1]}\n" +
            (scores[0] == scores[1] ? "Hòa!" :
             scores[0] > scores[1] ? "Người chơi 1 thắng!" : "Người chơi 2 thắng!");
    }

    void RemoveBullet(int i)
    {
        Destroy(bullets[i].rt.gameObject);
        bullets.RemoveAt(i);
    }

    void ClearBullets()
    {
        foreach (var b in bullets)
            if (b.rt != null) Destroy(b.rt.gameObject);
        bullets.Clear();
    }

    // Xáo đáp án và tính lại vị trí đáp án đúng
    Question Shuffle(Question q)
    {
        var idx = new List<int> { 0, 1, 2, 3 };
        for (int i = idx.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (idx[i], idx[j]) = (idx[j], idx[i]);
        }
        var n = new Question { text = q.text, options = new string[4] };
        for (int i = 0; i < 4; i++)
        {
            n.options[i] = q.options[idx[i]];
            if (idx[i] == q.correct) n.correct = i;
        }
        return n;
    }

    List<Question> DemoQuestions() => new List<Question>
    {
        new Question{ text="Đơn vị đo chiều dài trong hệ SI là gì?",
            options=new[]{"Mét","Kilôgam","Giây","Lít"}, correct=0 },
        new Question{ text="Chất nào sau đây ở thể khí ở điều kiện thường?",
            options=new[]{"Sắt","Nước","Oxygen","Muối ăn"}, correct=2 },
        new Question{ text="Dụng cụ nào dùng để đo khối lượng?",
            options=new[]{"Thước","Cân","Nhiệt kế","Đồng hồ"}, correct=1 },
        new Question{ text="Nước sôi ở bao nhiêu độ C (áp suất thường)?",
            options=new[]{"50","80","100","120"}, correct=2 },
        new Question{ text="Đơn vị đo thời gian trong hệ SI là gì?",
            options=new[]{"Phút","Giờ","Ngày","Giây"}, correct=3 },
    };
}
