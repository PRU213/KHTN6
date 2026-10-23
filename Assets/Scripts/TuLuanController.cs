using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Quản lý logic toàn bộ màn hình Tự Luận môn Sinh học.
/// Đảm bảo không thay đổi layout/sprite có sẵn, tự động phát hiện và liên kết các UI component.
/// </summary>
public class TuLuanController : MonoBehaviour
{
    public static TuLuanController Instance { get; private set; }

    // URL Google Sheet xuất CSV cho bảng Tự luận (gid=610736773)
    private const string TULUAN_SHEET_CSV_URL =
        "https://docs.google.com/spreadsheets/d/1DcJwlN_fUDdcaQ6IfnlkMJApgWuEUqMhyZv9Kn8vfWo/export?format=csv&gid=610736773";

    [Header("=== GIAO DIỆN CHÍNH (TỰ ĐỘNG TÌM NẾU TRỐNG) ===")]
    public TMP_Text txtCauHoi;
    public TMP_InputField inputCauTraLoi;
    public Button btnTaiAnh;
    public Image imgAnh;
    public Button btnNopBai;

    [Header("=== POPUP KẾT QUẢ ===")]
    public GameObject popupKetQua;
    public TMP_Text txtPopupDiem;
    public TMP_Text txtPopupNhanXet;
    public TMP_Text txtPopupKetQua;
    public Button btnPopupDong;
    public TMP_Text txtPopupMockNotice;

    [Header("=== THÔNG BÁO / TOAST ===")]
    public GameObject panelThongBao;
    public TMP_Text txtThongBao;

    [Header("=== CẤU HÌNH KIỂM THỬ MOCK ===")]
    [Tooltip("Bật chế độ thử nghiệm Mock Result khi nộp bài")]
    public bool useMockResult = true;

    [Header("=== MÁU / TIM ===")]
    public Image[] healthIcons = new Image[3];

    [Header("=== DỮ LIỆU HIỆN TẠI ===")]
    [SerializeField] private TuLuanQuestionData currentQuestion;
    [SerializeField] private string selectedImagePath = "";
    [SerializeField] private Texture2D selectedTexture = null;

    private Sprite initialAnhSprite = null;
    private Color initialAnhColor = Color.white;
    private TuLuanGradingResult lastGradingResult = null;

    // Danh sách câu hỏi tự luận đã tải
    private List<TuLuanQuestionData> allLoadedQuestions = new List<TuLuanQuestionData>();
    private List<TuLuanQuestionData> candidateQuestions = new List<TuLuanQuestionData>();

    // Static để Gameplay có thể truyền trực tiếp câu hỏi hoặc ID sang TuLuan
    public static TuLuanQuestionData PassedQuestion = null;
    public static string PassedQuestionId = "";

    // Danh sách câu hỏi dự phòng môn Sinh học khi offline hoặc chưa kết nối được mạng
    private readonly TuLuanQuestionData[] fallbackQuestions = new TuLuanQuestionData[]
    {
        new TuLuanQuestionData
        {
            id = "TL_BIO_01_E01",
            monId = "Sinh học",
            topic = "Chương 1: Tế bào - Đơn vị cơ sở của sự sống",
            difficulty = "Dễ",
            question = "Tế bào là gì? Hãy kể tên 3 thành phần chính cấu tạo nên một tế bào nhân thực điển hình và nêu chức năng của màng tế bào.",
            sampleAnswer = "Tế bào là đơn vị cơ sở cấu tạo và chức năng của mọi cơ thể sống. Ba thành phần chính: Màng tế bào, Tế bào chất và Nhân tế bào. Màng tế bào có chức năng bao bọc bảo vệ và kiểm soát các chất đi vào/ra khỏi tế bào.",
            rubric = "Nêu đúng định nghĩa tế bào (3.0đ) | Kể đúng 3 thành phần chính (4.0đ) | Nêu đúng chức năng màng tế bào (3.0đ)",
            maxScore = 10
        },
        new TuLuanQuestionData
        {
            id = "TL_BIO_01_E02",
            monId = "Sinh học",
            topic = "Chương 1: Tế bào - Đơn vị cơ sở của sự sống",
            difficulty = "Trung bình",
            question = "Trình bày sự khác nhau cơ bản giữa tế bào nhân sơ và tế bào nhân thực. Nêu ví dụ đại diện cho mỗi loại sinh vật.",
            sampleAnswer = "Tế bào nhân sơ chưa có màng nhân bao bọc vật chất di truyền, kích thước nhỏ, cấu tạo đơn giản (ví dụ: vi khuẩn). Tế bào nhân thực đã có nhân hoàn chỉnh được bao bọc bởi màng nhân và có các bào quan có màng (ví dụ: thực vật, động vật, nấm).",
            rubric = "Nêu đúng đặc điểm tế bào nhân sơ (4.0đ) | Nêu đúng đặc điểm tế bào nhân thực (4.0đ) | Cho đúng ví dụ mỗi loại (2.0đ)",
            maxScore = 10
        },
        new TuLuanQuestionData
        {
            id = "TL_BIO_01_E03",
            monId = "Sinh học",
            topic = "Chương 1: Tế bào - Đơn vị cơ sở của sự sống",
            difficulty = "Khó",
            question = "Quang hợp là gì? Hãy viết phương trình chữ tóm tắt quá trình quang hợp ở thực vật và giải thích tại sao quang hợp có vai trò quyết định đối với sự sống trên Trái Đất.",
            sampleAnswer = "Quang hợp là quá trình thực vật sử dụng năng lượng ánh sáng mặt trời để tổng hợp chất hữu cơ từ nước và khí cacbonic, đồng thời giải phóng khí oxi. Phương trình: Nước + Khí cacbonic --(Ánh sáng, diệp lục)--> Chất hữu cơ + Khí oxi. Vai trò: Cung cấp thức ăn, năng lượng và oxi cho toàn bộ sinh giới.",
            rubric = "Nêu đúng định nghĩa quang hợp (3.0đ) | Viết đúng phương trình chữ (3.0đ) | Giải thích đúng vai trò với sự sống (4.0đ)",
            maxScore = 10
        }
    };

    #region Windows Native File Dialog (Comdlg32)
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private class OpenFileName
    {
        public int structSize = 0;
        public IntPtr dlgOwner = IntPtr.Zero;
        public IntPtr instance = IntPtr.Zero;
        public string filter = null;
        public string customFilter = null;
        public int maxCustFilter = 0;
        public int filterIndex = 0;
        public string file = null;
        public int maxFile = 0;
        public string fileTitle = null;
        public int maxFileTitle = 0;
        public string initialDir = null;
        public string title = null;
        public int flags = 0;
        public short fileOffset = 0;
        public short fileExtension = 0;
        public string defExt = null;
        public IntPtr custData = IntPtr.Zero;
        public IntPtr hook = IntPtr.Zero;
        public string templateName = null;
        public IntPtr reservedPtr = IntPtr.Zero;
        public int reservedInt = 0;
        public int flagsEx = 0;
    }

    [DllImport("Comdlg32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool GetOpenFileName([In, Out] OpenFileName ofn);
    #endregion

    private void Awake()
    {
        Instance = this;
        AutoBindUI();
    }

    private void Start()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();

        SetupHealthBar(canvas);
        SetupEvents();
        StartCoroutine(LoadQuestionRoutine());

        // Tự động kích hoạt con trỏ nhấp nháy đen trong ô nhập câu trả lời
        if (inputCauTraLoi != null)
        {
            inputCauTraLoi.Select();
            inputCauTraLoi.ActivateInputField();
        }
    }

    /// <summary>
    /// Tìm hoặc khởi tạo thanh hiển thị tim/máu người chơi góc trên bên trái Canvas.
    /// Đồng bộ với hệ thống máu GameData của dự án.
    /// </summary>
    private void SetupHealthBar(Canvas canvas)
    {
        if (canvas == null) return;

        // Đảm bảo GameData tồn tại
        if (GameData.Instance == null)
        {
            GameObject go = new GameObject("GameData");
            go.AddComponent<GameData>();
        }

        // 1. Kiểm tra xem Scene đã có sẵn các GameObject icon máu hay chưa (máu 1, mau 1, tim1...)
        for (int i = 0; i < 3; i++)
        {
            string n1 = $"máu {i + 1}";
            string n2 = $"mau {i + 1}";
            string n3 = $"tim{i + 1}";
            Transform t = canvas.transform.Find(n1) ?? canvas.transform.Find(n2) ?? canvas.transform.Find(n3);
            if (t != null)
            {
                healthIcons[i] = t.GetComponent<Image>();
            }
        }

        // 2. Nếu chưa có sẵn, tạo thanh máu trực quan ở góc trên bên trái
        bool hasIcons = healthIcons[0] != null || healthIcons[1] != null || healthIcons[2] != null;
        if (!hasIcons)
        {
            Transform existingBar = canvas.transform.Find("HealthBar");
            if (existingBar != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    Transform t = existingBar.Find($"Heart_{i + 1}");
                    if (t != null) healthIcons[i] = t.GetComponent<Image>();
                }
            }
            else
            {
                Sprite heartSprite = null;
#if UNITY_EDITOR
                heartSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Bean_1_science/Environment/07_heart.png");
#endif
                GameObject barObj = new GameObject("HealthBar", typeof(RectTransform));
                barObj.transform.SetParent(canvas.transform, false);
                RectTransform rtBar = barObj.GetComponent<RectTransform>();
                rtBar.anchorMin = new Vector2(0, 1);
                rtBar.anchorMax = new Vector2(0, 1);
                rtBar.pivot = new Vector2(0, 1);
                rtBar.anchoredPosition = new Vector2(40, -35);
                rtBar.sizeDelta = new Vector2(240, 65);

                for (int i = 0; i < 3; i++)
                {
                    GameObject heartObj = new GameObject($"Heart_{i + 1}", typeof(RectTransform));
                    heartObj.transform.SetParent(barObj.transform, false);
                    RectTransform rtH = heartObj.GetComponent<RectTransform>();
                    rtH.anchorMin = new Vector2(0, 0.5f);
                    rtH.anchorMax = new Vector2(0, 0.5f);
                    rtH.pivot = new Vector2(0, 0.5f);
                    rtH.anchoredPosition = new Vector2(i * 75, 0);
                    rtH.sizeDelta = new Vector2(65, 58);

                    Image imgH = heartObj.AddComponent<Image>();
                    if (heartSprite != null)
                    {
                        imgH.sprite = heartSprite;
                        imgH.color = Color.white;
                    }
                    else
                    {
                        imgH.color = new Color(0.95f, 0.2f, 0.25f);
                    }
                    healthIcons[i] = imgH;
                }
            }
        }

        UpdateHealthIcons();
    }

    /// <summary>
    /// Cập nhật hiển thị số tim còn lại theo GameData.Instance.currentHealth.
    /// </summary>
    public void UpdateHealthIcons()
    {
        int currentHp = (GameData.Instance != null) ? GameData.Instance.currentHealth : 3;
        for (int i = 0; i < 3; i++)
        {
            if (healthIcons[i] != null)
            {
                healthIcons[i].enabled = (i < currentHp);
            }
        }
    }

    /// <summary>
    /// Tự động tìm kiếm và liên kết các component trong Scene theo đúng Hierarchy đã thiết kế.
    /// </summary>
    public void AutoBindUI()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        // 1. txtCauHoi có Scrollbar trong KhungDe (chống tràn chữ)
        ConfigureQuestionScroll(canvas);

        // 2. txtCauTraLoi & TMP_InputField (Canvas -> KhungBaiLam -> txtCauTraLoi)
        ConfigureInputField(canvas);

        // 3. btnTaiAnh & Anh (Canvas -> KhungAnh -> btnTaiAnh, btnTai / Anh)
        Transform khAnh = canvas.transform.Find("KhungAnh");
        if (khAnh != null)
        {
            if (btnTaiAnh == null)
            {
                Transform tBtn = khAnh.Find("btnTaiAnh");
                if (tBtn != null)
                {
                    btnTaiAnh = tBtn.GetComponent<Button>();
                    if (btnTaiAnh == null) btnTaiAnh = tBtn.gameObject.AddComponent<Button>();
                }
            }

            if (imgAnh == null)
            {
                Transform tAnh = khAnh.Find("Anh");
                if (tAnh == null) tAnh = khAnh.Find("btnTai"); // Tên GameObject gốc trong Scene
                if (tAnh != null)
                {
                    imgAnh = tAnh.GetComponent<Image>();
                    if (imgAnh != null)
                    {
                        imgAnh.preserveAspect = true;
                        if (initialAnhSprite == null) initialAnhSprite = imgAnh.sprite;
                        initialAnhColor = imgAnh.color;
                    }
                }
            }
        }

        // 4. btnNopBai (Canvas -> btnNopBai)
        if (btnNopBai == null)
        {
            Transform tNop = canvas.transform.Find("btnNopBai");
            if (tNop != null)
            {
                btnNopBai = tNop.GetComponent<Button>();
                if (btnNopBai == null) btnNopBai = tNop.gameObject.AddComponent<Button>();
            }
        }

        // 5. Popup Kết quả: Ưu tiên Scene riêng PopupKetQua.unity, chỉ tự tạo trong Hierarchy nếu Scene chưa thêm vào Build Settings
        if (!Application.CanStreamedLevelBeLoaded("PopupKetQua"))
        {
            EnsureResultPopup(canvas);
        }
    }

    /// <summary>
    /// Cấu hình khung cuộn câu hỏi (ScrollCauHoi) với Scrollbar, Viewport và RectMask2D.
    /// Giúp câu hỏi dài cuộn được mượt mà và không bao giờ bị tràn ra ngoài KhungDe.
    /// </summary>
    private void ConfigureQuestionScroll(Canvas canvas)
    {
        Transform khDe = canvas.transform.Find("KhungDe");
        if (khDe == null) return;

        // 1. Tiêu đề DeBai màu đen
        Transform tDeBai = khDe.Find("DeBai");
        if (tDeBai != null)
        {
            TMP_Text tmpDeBai = tDeBai.GetComponent<TMP_Text>();
            if (tmpDeBai != null) tmpDeBai.color = Color.black;
        }

        // 2. Khung cuộn ScrollCauHoi
        Transform scrollObj = khDe.Find("ScrollCauHoi");
        ScrollRect scrollRect = null;
        Transform viewportT = null;

        if (scrollObj == null)
        {
            GameObject newScroll = new GameObject("ScrollCauHoi", typeof(RectTransform), typeof(ScrollRect));
            newScroll.transform.SetParent(khDe, false);
            RectTransform rtScroll = newScroll.GetComponent<RectTransform>();
            rtScroll.anchorMin = new Vector2(0.5f, 0.5f);
            rtScroll.anchorMax = new Vector2(0.5f, 0.5f);
            rtScroll.pivot = new Vector2(0.5f, 0.5f);
            rtScroll.anchoredPosition = new Vector2(0, -15f);
            rtScroll.sizeDelta = new Vector2(980f, 210f);

            scrollRect = newScroll.GetComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 30f;

            // Viewport có RectMask2D để chống tràn chữ
            GameObject vpObj = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            vpObj.transform.SetParent(newScroll.transform, false);
            RectTransform rtViewport = vpObj.GetComponent<RectTransform>();
            rtViewport.anchorMin = Vector2.zero;
            rtViewport.anchorMax = Vector2.one;
            rtViewport.offsetMin = Vector2.zero;
            rtViewport.offsetMax = new Vector2(-22f, 0f); // chừa chỗ cho thanh cuộn
            viewportT = vpObj.transform;

            // Scrollbar Vertical
            Scrollbar scrollbar = CreateScrollbar(newScroll.transform, "Scrollbar Vertical", new Vector2(14, 0),
                new Color(0f, 0f, 0f, 0.12f), new Color(0.6f, 0.45f, 0.3f, 0.85f));
            RectTransform sbRt = scrollbar.GetComponent<RectTransform>();
            sbRt.anchorMin = new Vector2(1, 0);
            sbRt.anchorMax = new Vector2(1, 1);
            sbRt.pivot = new Vector2(1, 0.5f);
            sbRt.offsetMin = new Vector2(-16, 0);
            sbRt.offsetMax = new Vector2(-2, 0);

            scrollRect.viewport = rtViewport;
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;

            // Đưa txtCauHoi vào trong Viewport nếu đang nằm trực tiếp ở KhungDe
            Transform existingHoi = khDe.Find("txtCauHoi");
            if (existingHoi != null && existingHoi != newScroll.transform)
            {
                existingHoi.SetParent(viewportT, false);
                RectTransform rtHoi = existingHoi.GetComponent<RectTransform>();
                rtHoi.anchorMin = new Vector2(0, 1);
                rtHoi.anchorMax = new Vector2(1, 1);
                rtHoi.pivot = new Vector2(0.5f, 1f);
                rtHoi.anchoredPosition = Vector2.zero;
                rtHoi.sizeDelta = new Vector2(0, 0);

                txtCauHoi = existingHoi.GetComponent<TMP_Text>();
            }
        }
        else
        {
            scrollRect = scrollObj.GetComponent<ScrollRect>();
            viewportT = scrollObj.Find("Viewport");
            if (viewportT != null && viewportT.GetComponent<RectMask2D>() == null)
            {
                viewportT.gameObject.AddComponent<RectMask2D>();
            }
        }

        // Lấy txtCauHoi trong Viewport
        if (txtCauHoi == null)
        {
            if (viewportT != null)
            {
                txtCauHoi = viewportT.GetComponentInChildren<TMP_Text>();
            }
            if (txtCauHoi == null)
            {
                Transform tHoi = khDe.Find("txtCauHoi");
                if (tHoi != null) txtCauHoi = tHoi.GetComponent<TMP_Text>();
            }
        }

        if (txtCauHoi != null)
        {
            txtCauHoi.color = Color.black;
            txtCauHoi.textWrappingMode = TextWrappingModes.Normal;
            txtCauHoi.overflowMode = TextOverflowModes.Overflow;

            ContentSizeFitter csf = txtCauHoi.GetComponent<ContentSizeFitter>();
            if (csf == null) csf = txtCauHoi.gameObject.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            if (scrollRect != null)
            {
                scrollRect.content = txtCauHoi.rectTransform;
            }
        }
    }

    /// <summary>
    /// Cấu hình TMP_InputField trên txtCauTraLoi với Viewport, RectMask2D và Scrollbar dọc.
    /// Giúp câu trả lời dài tự động cuộn được, không bị tràn ra ngoài KhungBaiLam.
    /// </summary>
    private void ConfigureInputField(Canvas canvas)
    {
        Transform khBaiLam = canvas.transform.Find("KhungBaiLam");
        if (khBaiLam == null) return;

        // 1. Tiêu đề BaiLam giữ màu đen
        Transform tBaiLam = khBaiLam.Find("BaiLam");
        if (tBaiLam != null)
        {
            TMP_Text tmpBaiLam = tBaiLam.GetComponent<TMP_Text>();
            if (tmpBaiLam != null) tmpBaiLam.color = Color.black;
        }

        Transform tTraLoi = khBaiLam.Find("txtCauTraLoi");
        if (tTraLoi == null) return;

        // Định vị lại khung txtCauTraLoi cho vừa vặn trong KhungBaiLam
        RectTransform rtTraLoi = tTraLoi.GetComponent<RectTransform>();
        rtTraLoi.anchorMin = new Vector2(0.5f, 0.5f);
        rtTraLoi.anchorMax = new Vector2(0.5f, 0.5f);
        rtTraLoi.pivot = new Vector2(0.5f, 0.5f);
        rtTraLoi.anchoredPosition = new Vector2(0, -35f);
        rtTraLoi.sizeDelta = new Vector2(980f, 310f);

        inputCauTraLoi = tTraLoi.GetComponent<TMP_InputField>();
        if (inputCauTraLoi == null)
        {
            inputCauTraLoi = tTraLoi.gameObject.AddComponent<TMP_InputField>();
        }

        // Tắt TextMeshPro cũ trên root tTraLoi nếu có để tránh vẽ đè 2 lớp chữ
        TextMeshProUGUI existingTextOnRoot = tTraLoi.GetComponent<TextMeshProUGUI>();
        if (existingTextOnRoot != null)
        {
            existingTextOnRoot.enabled = false;
        }

        // 2. TextArea (Viewport có RectMask2D để clip text không tràn ra ngoài)
        Transform textAreaT = tTraLoi.Find("TextArea");
        RectTransform rtTextArea;
        if (textAreaT == null)
        {
            GameObject textAreaObj = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
            textAreaObj.transform.SetParent(tTraLoi, false);
            rtTextArea = textAreaObj.GetComponent<RectTransform>();
            rtTextArea.anchorMin = Vector2.zero;
            rtTextArea.anchorMax = Vector2.one;
            rtTextArea.offsetMin = new Vector2(10, 10);
            rtTextArea.offsetMax = new Vector2(-28, -10); // Chừa 28px bên phải cho thanh cuộn
        }
        else
        {
            rtTextArea = textAreaT.GetComponent<RectTransform>();
            if (textAreaT.GetComponent<RectMask2D>() == null)
            {
                textAreaT.gameObject.AddComponent<RectMask2D>();
            }
        }

        // 3. TextComponent (Con của TextArea)
        Transform textT = rtTextArea.transform.Find("Text");
        TextMeshProUGUI tmpText;
        if (textT == null)
        {
            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(rtTextArea.transform, false);
            RectTransform rtText = textObj.GetComponent<RectTransform>();
            rtText.anchorMin = Vector2.zero;
            rtText.anchorMax = Vector2.one;
            rtText.offsetMin = Vector2.zero;
            rtText.offsetMax = Vector2.zero;
            tmpText = textObj.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            tmpText = textT.GetComponent<TextMeshProUGUI>();
        }
        tmpText.color = Color.black;
        tmpText.fontSize = 32;
        tmpText.textWrappingMode = TextWrappingModes.Normal;
        tmpText.overflowMode = TextOverflowModes.Overflow;

        // 4. Placeholder (Con của TextArea)
        Transform phT = rtTextArea.transform.Find("Placeholder");
        TextMeshProUGUI tmpPh;
        if (phT == null)
        {
            GameObject phObj = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
            phObj.transform.SetParent(rtTextArea.transform, false);
            RectTransform rtPh = phObj.GetComponent<RectTransform>();
            rtPh.anchorMin = Vector2.zero;
            rtPh.anchorMax = Vector2.one;
            rtPh.offsetMin = Vector2.zero;
            rtPh.offsetMax = Vector2.zero;
            tmpPh = phObj.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            tmpPh = phT.GetComponent<TextMeshProUGUI>();
        }
        tmpPh.text = "Nhập câu trả lời...";
        tmpPh.fontStyle = FontStyles.Italic;
        tmpPh.fontSize = 32;
        tmpPh.color = new Color(0.4f, 0.4f, 0.4f, 0.75f);
        tmpPh.textWrappingMode = TextWrappingModes.Normal;

        // 5. Scrollbar Vertical cho ô nhập liệu
        Transform sbT = tTraLoi.Find("Scrollbar Vertical");
        Scrollbar scrollbar;
        if (sbT == null)
        {
            scrollbar = CreateScrollbar(tTraLoi, "Scrollbar Vertical", new Vector2(14, 0),
                new Color(0f, 0f, 0f, 0.12f), new Color(0.6f, 0.45f, 0.3f, 0.85f));
            RectTransform sbRt = scrollbar.GetComponent<RectTransform>();
            sbRt.anchorMin = new Vector2(1, 0);
            sbRt.anchorMax = new Vector2(1, 1);
            sbRt.pivot = new Vector2(1, 0.5f);
            sbRt.offsetMin = new Vector2(-20, 10);
            sbRt.offsetMax = new Vector2(-6, -10);
        }
        else
        {
            scrollbar = sbT.GetComponent<Scrollbar>();
        }

        // 6. Cấu hình TMP_InputField
        inputCauTraLoi.textViewport = rtTextArea;
        inputCauTraLoi.textComponent = tmpText;
        inputCauTraLoi.placeholder = tmpPh;
        inputCauTraLoi.verticalScrollbar = scrollbar;
        inputCauTraLoi.lineType = TMP_InputField.LineType.MultiLineNewline;
        inputCauTraLoi.contentType = TMP_InputField.ContentType.Standard;
        inputCauTraLoi.richText = true;
        inputCauTraLoi.scrollSensitivity = 20f; // Hỗ trợ lăn chuột cuộn mượt mà

        // Con trỏ nhấp nháy đen và màu chữ đen
        inputCauTraLoi.customCaretColor = true;
        inputCauTraLoi.caretColor = Color.black;
        inputCauTraLoi.caretWidth = 4;
        inputCauTraLoi.caretBlinkRate = 0.85f;
        inputCauTraLoi.selectionColor = new Color(0.2f, 0.6f, 1f, 0.35f);

        inputCauTraLoi.onValueChanged.RemoveAllListeners();
        inputCauTraLoi.onValueChanged.AddListener((val) =>
        {
            if (inputCauTraLoi.textComponent != null)
            {
                inputCauTraLoi.textComponent.color = Color.black;
            }
        });
        inputCauTraLoi.onSelect.RemoveAllListeners();
        inputCauTraLoi.onSelect.AddListener((val) =>
        {
            if (inputCauTraLoi.textComponent != null)
            {
                inputCauTraLoi.textComponent.color = Color.black;
            }
        });

        // Click bất cứ đâu trên KhungBaiLam cũng tự động focus vào ô nhập và bật con trỏ
        Button btnKhung = khBaiLam.GetComponent<Button>();
        if (btnKhung == null)
        {
            btnKhung = khBaiLam.gameObject.AddComponent<Button>();
            btnKhung.transition = Selectable.Transition.None;
        }
        btnKhung.onClick.RemoveAllListeners();
        btnKhung.onClick.AddListener(() =>
        {
            if (inputCauTraLoi != null)
            {
                inputCauTraLoi.Select();
                inputCauTraLoi.ActivateInputField();
            }
        });
    }

    /// <summary>
    /// Tạo thanh cuộn chuẩn Unity UI Scrollbar với nền track và Handle kéo/thả.
    /// </summary>
    public static Scrollbar CreateScrollbar(Transform parent, string name, Vector2 size, Color trackColor, Color handleColor)
    {
        // 1. Scrollbar root
        GameObject sbObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        sbObj.transform.SetParent(parent, false);

        RectTransform sbRt = sbObj.GetComponent<RectTransform>();
        sbRt.anchorMin = new Vector2(1, 0);
        sbRt.anchorMax = new Vector2(1, 1);
        sbRt.pivot = new Vector2(1, 0.5f);
        sbRt.sizeDelta = new Vector2(size.x, size.y);
        sbRt.anchoredPosition = Vector2.zero;

        Image trackImg = sbObj.GetComponent<Image>();
        trackImg.color = trackColor;

        Scrollbar scrollbar = sbObj.GetComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;

        // 2. Sliding Area
        GameObject slideObj = new GameObject("Sliding Area", typeof(RectTransform));
        slideObj.transform.SetParent(sbObj.transform, false);
        RectTransform slideRt = slideObj.GetComponent<RectTransform>();
        slideRt.anchorMin = Vector2.zero;
        slideRt.anchorMax = Vector2.one;
        slideRt.sizeDelta = new Vector2(-4, -4);
        slideRt.anchoredPosition = Vector2.zero;

        // 3. Handle
        GameObject handleObj = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handleObj.transform.SetParent(slideObj.transform, false);
        RectTransform handleRt = handleObj.GetComponent<RectTransform>();
        handleRt.anchorMin = Vector2.zero;
        handleRt.anchorMax = Vector2.one;
        handleRt.sizeDelta = Vector2.zero;
        handleRt.anchoredPosition = Vector2.zero;

        Image handleImg = handleObj.GetComponent<Image>();
        handleImg.color = handleColor;

        scrollbar.handleRect = handleRt;
        scrollbar.targetGraphic = handleImg;

        ColorBlock colors = scrollbar.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        scrollbar.colors = colors;

        return scrollbar;
    }

    /// <summary>
    /// Gán sự kiện click cho các Button.
    /// </summary>
    private void SetupEvents()
    {
        if (btnTaiAnh != null)
        {
            btnTaiAnh.onClick.RemoveAllListeners();
            btnTaiAnh.onClick.AddListener(OnSelectImageClicked);
        }

        if (btnNopBai != null)
        {
            btnNopBai.onClick.RemoveAllListeners();
            btnNopBai.onClick.AddListener(OnSubmitClicked);
        }

        if (btnPopupDong != null)
        {
            btnPopupDong.onClick.RemoveAllListeners();
            btnPopupDong.onClick.AddListener(OnClosePopupClicked);
        }
    }

    #region Question Loading Logic
    /// <summary>
    /// Tải câu hỏi từ Google Sheets hoặc dữ liệu truyền từ Gameplay.
    /// </summary>
    private IEnumerator LoadQuestionRoutine()
    {
        if (txtCauHoi != null)
        {
            txtCauHoi.text = "Đang tải câu hỏi tự luận...";
        }

        // 1. Kiểm tra nếu có câu hỏi được truyền trực tiếp từ Gameplay
        if (PassedQuestion != null && !string.IsNullOrEmpty(PassedQuestion.question))
        {
            currentQuestion = PassedQuestion;
            ApplyCurrentQuestion();
            yield break;
        }

        string targetId = !string.IsNullOrEmpty(PassedQuestionId) 
            ? PassedQuestionId 
            : PlayerPrefs.GetString("CurrentTuLuanQuestionID", "");

        // 2. Tải từ Google Sheets
        List<TuLuanQuestionData> sheetQuestions = new List<TuLuanQuestionData>();
        using (UnityWebRequest req = UnityWebRequest.Get(TULUAN_SHEET_CSV_URL))
        {
            req.certificateHandler = new BypassCertificate();
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success && !string.IsNullOrEmpty(req.downloadHandler.text))
            {
                sheetQuestions = ParseTuLuanCSV(req.downloadHandler.text);
                allLoadedQuestions = new List<TuLuanQuestionData>(sheetQuestions);
            }
            else
            {
                Debug.LogWarning("Không thể tải Google Sheet tự luận: " + req.error + ". Sử dụng dữ liệu dự phòng.");
            }
        }

        // 3. Chọn câu hỏi phù hợp
        TuLuanQuestionData chosen = null;

        // 3.1. Tìm theo ID cụ thể nếu có
        if (!string.IsNullOrEmpty(targetId))
        {
            chosen = sheetQuestions.Find(q => q.id.Equals(targetId, StringComparison.OrdinalIgnoreCase));
        }

        // 3.2. Lọc theo môn Sinh học và độ khó từ GameSession nếu có
        if (chosen == null && sheetQuestions.Count > 0)
        {
            List<TuLuanQuestionData> filtered = sheetQuestions.FindAll(q =>
                q.monId.IndexOf("Sinh", StringComparison.OrdinalIgnoreCase) >= 0);

            if (filtered.Count == 0) filtered = sheetQuestions;

            // Nếu người chơi chọn độ khó ở menu_game
            if (GameSession.Difficulties != null && GameSession.Difficulties.Count > 0)
            {
                List<TuLuanQuestionData> diffFiltered = filtered.FindAll(q =>
                    GameSession.Difficulties.Contains(q.difficulty.Trim()));
                if (diffFiltered.Count > 0)
                {
                    filtered = diffFiltered;
                }
            }

            candidateQuestions = filtered;

            if (filtered.Count > 0)
            {
                int rIndex = UnityEngine.Random.Range(0, filtered.Count);
                chosen = filtered[rIndex];
            }
        }

        // 3.3. Sử dụng câu hỏi dự phòng nếu không tìm thấy
        if (chosen == null)
        {
            chosen = fallbackQuestions != null && fallbackQuestions.Length > 0 ? fallbackQuestions[0] : null;
        }

        currentQuestion = chosen;
        ApplyCurrentQuestion();
    }

    /// <summary>
    /// Chọn ngẫu nhiên một câu hỏi tự luận khác câu hỏi hiện tại.
    /// </summary>
    public TuLuanQuestionData PickDifferentQuestion()
    {
        List<TuLuanQuestionData> pool = (candidateQuestions != null && candidateQuestions.Count > 0)
            ? candidateQuestions
            : (allLoadedQuestions != null && allLoadedQuestions.Count > 0) ? allLoadedQuestions : null;

        if (pool != null && pool.Count > 0)
        {
            List<TuLuanQuestionData> others = pool.FindAll(q =>
                currentQuestion == null || !string.Equals(q.id, currentQuestion.id, StringComparison.OrdinalIgnoreCase));

            if (others.Count > 0)
            {
                return others[UnityEngine.Random.Range(0, others.Count)];
            }
        }

        if (fallbackQuestions != null && fallbackQuestions.Length > 0)
        {
            for (int i = 0; i < fallbackQuestions.Length; i++)
            {
                if (currentQuestion == null || !string.Equals(fallbackQuestions[i].id, currentQuestion.id, StringComparison.OrdinalIgnoreCase))
                {
                    return fallbackQuestions[i];
                }
            }
            return fallbackQuestions[0];
        }

        return fallbackQuestions != null && fallbackQuestions.Length > 0 ? fallbackQuestions[0] : null;
    }

    /// <summary>
    /// Chuyển sang câu hỏi tự luận khác khi bài làm chưa đạt (< 5 điểm) và người chơi vẫn còn tim.
    /// </summary>
    public void LoadNextQuestion()
    {
        // 1. Reset câu trả lời văn bản
        if (inputCauTraLoi != null)
        {
            inputCauTraLoi.text = "";
        }

        // 2. Reset ảnh đã chọn nếu có
        ClearSelectedImage();

        // 3. Chọn và hiển thị câu hỏi khác
        TuLuanQuestionData nextQ = PickDifferentQuestion();
        if (nextQ != null)
        {
            currentQuestion = nextQ;
            ApplyCurrentQuestion();
        }

        // 4. Thông báo và focus lại ô nhập
        int remHealth = GameData.Instance != null ? GameData.Instance.currentHealth : 0;
        ShowNotification($"Chưa đạt! Bị trừ 1 tim (còn {remHealth} tim). Hãy hoàn thành câu hỏi mới này!");

        if (inputCauTraLoi != null)
        {
            inputCauTraLoi.Select();
            inputCauTraLoi.ActivateInputField();
        }
    }

    /// <summary>
    /// Xóa ảnh bài làm đã chọn, khôi phục lại trạng thái ban đầu của khung ảnh.
    /// </summary>
    public void ClearSelectedImage()
    {
        if (selectedTexture != null)
        {
            Destroy(selectedTexture);
            selectedTexture = null;
        }
        selectedImagePath = "";

        if (imgAnh != null)
        {
            imgAnh.sprite = initialAnhSprite;
            imgAnh.color = initialAnhColor;
        }
    }

    private void ApplyCurrentQuestion()
    {
        if (currentQuestion == null) return;

        if (txtCauHoi != null)
        {
            txtCauHoi.text = currentQuestion.question;
            txtCauHoi.color = Color.black; // Text câu hỏi màu đen

            // Reset vị trí cuộn lên đỉnh đầu câu hỏi
            ScrollRect scrollRect = txtCauHoi.GetComponentInParent<ScrollRect>();
            if (scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 1f;
            }
        }

        Debug.Log($"[TuLuan] Đã nạp câu hỏi: ID={currentQuestion.id} | Chủ đề={currentQuestion.topic} | Độ khó={currentQuestion.difficulty}");
    }

    /// <summary>
    /// Parser CSV hoàn chỉnh xử lý dấu ngoặc kép, dấu phẩy trong chuỗi và xuống dòng.
    /// </summary>
    private List<TuLuanQuestionData> ParseTuLuanCSV(string csvData)
    {
        List<TuLuanQuestionData> list = new List<TuLuanQuestionData>();
        List<string[]> rows = ParseCSVRows(csvData);

        for (int i = 1; i < rows.Count; i++) // Bỏ dòng tiêu đề (header)
        {
            string[] cols = rows[i];
            if (cols.Length >= 5)
            {
                TuLuanQuestionData q = new TuLuanQuestionData
                {
                    id = cols.Length > 0 ? cols[0].Trim() : "",
                    monId = cols.Length > 1 ? cols[1].Trim() : "",
                    topic = cols.Length > 2 ? cols[2].Trim() : "",
                    difficulty = cols.Length > 3 ? cols[3].Trim() : "",
                    question = cols.Length > 4 ? cols[4].Trim() : "",
                    imageUrl = cols.Length > 5 ? cols[5].Trim() : "",
                    sampleAnswer = cols.Length > 6 ? cols[6].Trim() : "",
                    rubric = cols.Length > 7 ? cols[7].Trim() : "",
                    maxScore = cols.Length > 8 && int.TryParse(cols[8].Trim(), out int score) ? score : 10
                };

                if (!string.IsNullOrEmpty(q.question))
                {
                    list.Add(q);
                }
            }
        }

        return list;
    }

    private List<string[]> ParseCSVRows(string csvData)
    {
        List<string[]> rows = new List<string[]>();
        List<string> currentCols = new List<string>();
        bool inQuotes = false;
        string currentField = "";

        for (int i = 0; i < csvData.Length; i++)
        {
            char c = csvData[i];
            if (c == '\"')
            {
                if (inQuotes && i + 1 < csvData.Length && csvData[i + 1] == '\"')
                {
                    currentField += '\"';
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                currentCols.Add(currentField);
                currentField = "";
            }
            else if ((c == '\r' || c == '\n') && !inQuotes)
            {
                if (c == '\r' && i + 1 < csvData.Length && csvData[i + 1] == '\n')
                {
                    i++;
                }
                if (currentCols.Count > 0 || !string.IsNullOrEmpty(currentField))
                {
                    currentCols.Add(currentField);
                    rows.Add(currentCols.ToArray());
                }
                currentCols.Clear();
                currentField = "";
            }
            else
            {
                currentField += c;
            }
        }

        if (currentCols.Count > 0 || !string.IsNullOrEmpty(currentField))
        {
            currentCols.Add(currentField);
            rows.Add(currentCols.ToArray());
        }

        return rows;
    }
    #endregion

    #region Image Selection Logic
    /// <summary>
    /// Xử lý chọn ảnh từ thiết bị (hỗ trợ PNG, JPG, JPEG).
    /// </summary>
    public void OnSelectImageClicked()
    {
        string path = OpenFilePicker();

        if (string.IsNullOrEmpty(path))
        {
            Debug.Log("[TuLuan] Người dùng đã hủy chọn ảnh.");
            return;
        }

        if (!File.Exists(path))
        {
            ShowNotification("Tệp ảnh không tồn tại!");
            return;
        }

        string ext = Path.GetExtension(path).ToLower();
        if (ext != ".png" && ext != ".jpg" && ext != ".jpeg")
        {
            ShowNotification("Vui lòng chọn tệp ảnh hợp lệ (.PNG, .JPG, .JPEG)!");
            return;
        }

        try
        {
            byte[] fileBytes = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2);
            if (tex.LoadImage(fileBytes))
            {
                // Giải phóng texture cũ nếu có
                if (selectedTexture != null)
                {
                    Destroy(selectedTexture);
                }

                selectedTexture = tex;
                selectedImagePath = path;

                Sprite newSprite = Sprite.Create(
                    tex,
                    new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f)
                );

                if (imgAnh != null)
                {
                    imgAnh.sprite = newSprite;
                    imgAnh.preserveAspect = true;
                    imgAnh.color = Color.white;
                }

                Debug.Log($"[TuLuan] Đã tải ảnh thành công: {path} ({tex.width}x{tex.height})");
                ShowNotification("Đã tải ảnh bài làm thành công!");
            }
            else
            {
                ShowNotification("Không thể đọc định dạng hình ảnh!");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[TuLuan] Lỗi nạp ảnh: " + ex.Message);
            ShowNotification("Lỗi khi tải ảnh: " + ex.Message);
        }
    }

    private string OpenFilePicker()
    {
#if UNITY_EDITOR
        string path = UnityEditor.EditorUtility.OpenFilePanel("Chọn ảnh bài làm", "", "png,jpg,jpeg");
        return path;
#else
        OpenFileName ofn = new OpenFileName();
        ofn.structSize = Marshal.SizeOf(ofn);
        ofn.filter = "Image Files (*.png;*.jpg;*.jpeg)\0*.png;*.jpg;*.jpeg\0All Files (*.*)\0*.*\0";
        ofn.file = new string(new char[260]);
        ofn.maxFile = ofn.file.Length;
        ofn.fileTitle = new string(new char[64]);
        ofn.maxFileTitle = ofn.fileTitle.Length;
        ofn.initialDir = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        ofn.title = "Chọn ảnh bài làm tự luận";
        ofn.flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000200 | 0x00000008; // OFN_EXPLORER | OFN_FILEMUSTEXIST | OFN_PATHMUSTEXIST

        if (GetOpenFileName(ofn))
        {
            return ofn.file;
        }
        return null;
#endif
    }
    #endregion

    #region Submission Logic
    /// <summary>
    /// Kiểm tra hợp lệ và thu thập dữ liệu bài nộp.
    /// </summary>
    public void OnSubmitClicked()
    {
        string answerText = inputCauTraLoi != null ? inputCauTraLoi.text.Trim() : "";
        bool hasText = !string.IsNullOrEmpty(answerText);
        bool hasImage = selectedTexture != null;

        if (!hasText && !hasImage)
        {
            ShowNotification("Vui lòng nhập văn bản trả lời HOẶC tải ảnh bài làm trước khi nộp!");
            return;
        }

        string username = !string.IsNullOrEmpty(GameSession.Username) 
            ? GameSession.Username 
            : PlayerPrefs.GetString("Username", "student01");

        // 1. Thu thập dữ liệu bài nộp
        TuLuanSubmissionData submission = new TuLuanSubmissionData
        {
            submissionId = Guid.NewGuid().ToString(),
            userId = username,
            cauHoiId = currentQuestion != null ? currentQuestion.id : "TL_DEFAULT",
            cauTraLoi = answerText,
            anhBaiLamPath = selectedImagePath,
            anhBaiLamBytes = selectedTexture != null ? selectedTexture.EncodeToPNG() : null,
            thoiGianNop = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        Debug.Log($"[TuLuan] Bài nộp được tạo: ID={submission.submissionId} | User={submission.userId} | Câu={submission.cauHoiId} | Chữ={answerText.Length} ký tự | Có ảnh={hasImage}");

        // 2. Chấm điểm (Mock Result để kiểm thử - Chưa kết nối AI API)
        if (useMockResult)
        {
            TuLuanGradingResult mockResult = GenerateMockResult(answerText, hasImage);
            ShowResultPopup(mockResult);
        }
        else
        {
            ShowNotification("Bài nộp đã được ghi nhận. Đang chờ kết nối AI chấm bài.");
        }
    }

    /// <summary>
    /// Sinh Mock Result để kiểm thử các trường hợp điểm >= 5 (ĐẠT) và dưới 5 (CHƯA ĐẠT).
    /// </summary>
    private TuLuanGradingResult GenerateMockResult(string text, bool hasImage)
    {
        TuLuanGradingResult res = new TuLuanGradingResult();
        res.isMock = true;

        // Nếu nội dung quá ngắn (< 25 ký tự) và không có ảnh đính kèm -> mô phỏng Điểm < 5 (Chưa đạt)
        if (text.Length < 25 && !hasImage)
        {
            res.score = 4.0f;
            res.status = "CHƯA ĐẠT";
            res.reviewText = "• Bài làm còn quá sơ sài, chưa nêu đủ các ý chính của câu hỏi.\n" +
                            "• Cần giải thích rõ cơ chế và chức năng của từng bộ phận.\n" +
                            "• Gợi ý: Tham khảo lại kiến thức trong sách giáo khoa để bổ sung bài làm.";
        }
        else
        {
            // Bài làm chi tiết hoặc có ảnh bài làm -> mô phỏng Điểm >= 5 (Đạt)
            res.score = 8.5f;
            res.status = "ĐẠT";
            res.reviewText = "• Trả lời đúng trọng tâm câu hỏi, bố cục rõ ràng.\n" +
                            "• Trình bày mạch lạc, nêu đúng tên và chức năng của các thành phần chính.\n" +
                            "• Phát huy tốt tinh thần tự học và liên hệ thực tế.";
        }

        return res;
    }
    #endregion

    #region Popup Result & UI Management
    /// <summary>
    /// Hiển thị popup kết quả chấm thi.
    /// Nếu điểm < 5: Tương đương trả lời sai (trừ 1 tim, phải làm tiếp câu khác hoặc chơi lại nếu hết tim).
    /// Nếu điểm >= 5: Tương đương trả lời đúng (tiêu diệt quái vật và tiếp tục di chuyển).
    /// </summary>
    public void ShowResultPopup(TuLuanGradingResult result)
    {
        if (result == null) return;
        lastGradingResult = result;

        // Đảm bảo GameData tồn tại
        if (GameData.Instance == null)
        {
            GameObject go = new GameObject("GameData");
            go.AddComponent<GameData>();
        }

        // XỬ LÝ LOGIC TIM VÀ TRẠNG THÁI:
        if (result.score < 5.0f)
        {
            bool alive = GameData.Instance.TakeDamage();
            UpdateHealthIcons();
            result.status = "CHƯA ĐẠT";
        }
        else
        {
            result.status = "ĐẠT";
        }

        // 1. Truyền kết quả sang controller popup
        PopupKetQuaController.LastResult = result;

        // 2. Nếu có Scene riêng PopupKetQua thì mở cộng gộp (Additive) để người dùng dễ chỉnh sửa riêng biệt
        if (Application.CanStreamedLevelBeLoaded("PopupKetQua"))
        {
            if (!SceneManager.GetSceneByName("PopupKetQua").isLoaded)
            {
                SceneManager.LoadScene("PopupKetQua", LoadSceneMode.Additive);
            }
            else if (PopupKetQuaController.Instance != null)
            {
                PopupKetQuaController.Instance.ApplyResultData(result);
            }
            return;
        }

        // 3. Dự phòng nếu scene chưa được thêm vào Build Settings
        if (popupKetQua == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
            EnsureResultPopup(canvas);
        }

        if (popupKetQua != null)
        {
            popupKetQua.SetActive(true);
            popupKetQua.transform.SetAsLastSibling(); // Luôn hiển thị trên cùng
        }

        if (txtPopupDiem != null)
        {
            txtPopupDiem.text = $"{result.score:0.0} / {result.maxScore:0}";
            txtPopupDiem.color = result.score >= 5.0f 
                ? new Color(0.2f, 0.85f, 0.2f) 
                : new Color(0.95f, 0.25f, 0.25f);
        }

        if (txtPopupKetQua != null)
        {
            txtPopupKetQua.text = result.status;
            txtPopupKetQua.color = result.score >= 5.0f 
                ? new Color(0.2f, 0.85f, 0.2f) 
                : new Color(0.95f, 0.25f, 0.25f);
        }

        if (txtPopupNhanXet != null)
        {
            txtPopupNhanXet.text = result.reviewText;
        }

        if (txtPopupMockNotice != null)
        {
            txtPopupMockNotice.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Xử lý khi nhấn nút Đóng / Tiếp tục / Làm tiếp trên Popup Kết quả.
    /// </summary>
    public void OnClosePopupClicked()
    {
        if (popupKetQua != null)
        {
            popupKetQua.SetActive(false);
        }

        bool passed = lastGradingResult != null && lastGradingResult.score >= 5.0f;

        if (passed)
        {
            // === ĐẠT (>= 5 ĐIỂM): VƯỢT QUA VÀ TIẾP TỤC DI CHUYỂN ===
            // 1. Đánh dấu quái vật đã tiêu diệt
            if (GameData.Instance != null && !string.IsNullOrEmpty(GameData.Instance.currentEnemyName))
            {
                GameData.Instance.DefeatCurrentEnemy();
            }

            // 2. Quay lại màn chơi trước đó (PlayerController sẽ tiếp tục từ lastPlayerPosition)
            ReturnToMainGameplay();
        }
        else
        {
            // === CHƯA ĐẠT (< 5 ĐIỂM): TƯƠNG ĐƯƠNG TRẢ LỜI SAI ===
            int currentHp = GameData.Instance != null ? GameData.Instance.currentHealth : 0;
            if (currentHp <= 0)
            {
                // Hết tim -> Chơi lại từ đầu màn chơi giống trắc nghiệm
                RestartGame();
            }
            else
            {
                // Còn tim -> Ở lại scene tự luận và nạp câu hỏi khác để làm tiếp
                LoadNextQuestion();
            }
        }
    }

    /// <summary>
    /// Quay lại màn chơi gameplay chính (nhân vật tiếp tục di chuyển từ vị trí đã lưu).
    /// </summary>
    private void ReturnToMainGameplay()
    {
        string previousScene = GameData.Instance != null && !string.IsNullOrEmpty(GameData.Instance.lastSceneName)
            ? GameData.Instance.lastSceneName
            : "bean_1";

        if (Application.CanStreamedLevelBeLoaded(previousScene))
        {
            SceneManager.LoadScene(previousScene);
        }
        else if (Application.CanStreamedLevelBeLoaded("bean_1"))
        {
            SceneManager.LoadScene("bean_1");
        }
        else if (Application.CanStreamedLevelBeLoaded("ChonBai"))
        {
            SceneManager.LoadScene("ChonBai");
        }
    }

    /// <summary>
    /// Chơi lại từ đầu màn chơi khi hết tim (reset máu, reset vị trí, reset quái).
    /// Đồng bộ cơ chế RestartGame() của QuestionManager.
    /// </summary>
    private void RestartGame()
    {
        if (GameData.Instance != null)
        {
            GameData.Instance.currentHealth = GameData.Instance.maxHealth;
            GameData.Instance.defeatedEnemies.Clear();
            GameData.Instance.currentEnemyName = "";
            GameData.Instance.hasSavedPosition = false;
        }
        ReturnToMainGameplay();
    }

    /// <summary>
    /// Hiển thị thông báo toast tạm thời trên màn hình.
    /// </summary>
    public void ShowNotification(string message)
    {
        if (panelThongBao != null && txtThongBao != null)
        {
            txtThongBao.text = message;
            panelThongBao.SetActive(true);
            panelThongBao.transform.SetAsLastSibling();
            StopCoroutine("HideNotificationRoutine");
            StartCoroutine("HideNotificationRoutine");
        }
        else
        {
            Debug.Log($"[Thông báo]: {message}");
        }
    }

    private IEnumerator HideNotificationRoutine()
    {
        yield return new WaitForSeconds(3.5f);
        if (panelThongBao != null) panelThongBao.SetActive(false);
    }

    /// <summary>
    /// Tự động khởi tạo Popup Kết quả đồng bộ phong cách đồ họa fantasy nếu chưa có sẵn.
    /// </summary>
    private void EnsureResultPopup(Canvas canvas)
    {
        if (canvas == null) return;

        Transform existingPopup = canvas.transform.Find("PopupKetQua");
        if (existingPopup != null)
        {
            popupKetQua = existingPopup.gameObject;
            txtPopupDiem = existingPopup.Find("Frame/txtDiem")?.GetComponent<TMP_Text>();
            txtPopupKetQua = existingPopup.Find("Frame/txtKetQua")?.GetComponent<TMP_Text>();
            txtPopupNhanXet = existingPopup.Find("Frame/txtNhanXet")?.GetComponent<TMP_Text>();
            txtPopupMockNotice = existingPopup.Find("Frame/txtMockNotice")?.GetComponent<TMP_Text>();
            btnPopupDong = existingPopup.Find("Frame/btnDong")?.GetComponent<Button>();
            return;
        }

        // Lấy Sprite viền khung gỗ từ KhungDe để đồng bộ đồ họa
        Sprite frameSprite = null;
        Transform khDe = canvas.transform.Find("KhungDe");
        if (khDe != null)
        {
            Image imgDe = khDe.GetComponent<Image>();
            if (imgDe != null) frameSprite = imgDe.sprite;
        }

        // 1. Tạo Root PopupKetQua (Panel nền đen mờ chắn click)
        GameObject popupObj = new GameObject("PopupKetQua", typeof(RectTransform));
        popupObj.transform.SetParent(canvas.transform, false);
        popupKetQua = popupObj;

        RectTransform rtPopup = popupObj.GetComponent<RectTransform>();
        rtPopup.anchorMin = Vector2.zero;
        rtPopup.anchorMax = Vector2.one;
        rtPopup.offsetMin = Vector2.zero;
        rtPopup.offsetMax = Vector2.zero;

        Image bgOverlay = popupObj.AddComponent<Image>();
        bgOverlay.color = new Color(0f, 0f, 0f, 0.75f);
        bgOverlay.raycastTarget = true;

        // 2. Tạo Frame nội dung (kích thước cân đối giữa màn hình)
        GameObject frameObj = new GameObject("Frame", typeof(RectTransform));
        frameObj.transform.SetParent(popupObj.transform, false);
        RectTransform rtFrame = frameObj.GetComponent<RectTransform>();
        rtFrame.sizeDelta = new Vector2(850, 620);
        rtFrame.anchoredPosition = Vector2.zero;

        Image imgFrame = frameObj.AddComponent<Image>();
        if (frameSprite != null)
        {
            imgFrame.sprite = frameSprite;
            imgFrame.type = Image.Type.Sliced;
        }
        else
        {
            imgFrame.color = new Color(0.18f, 0.12f, 0.08f, 0.95f);
        }

        // 3. Tiêu đề Popup
        GameObject titleObj = new GameObject("txtTieuDe", typeof(RectTransform));
        titleObj.transform.SetParent(frameObj.transform, false);
        RectTransform rtTitle = titleObj.GetComponent<RectTransform>();
        rtTitle.anchoredPosition = new Vector2(0, 240);
        rtTitle.sizeDelta = new Vector2(700, 60);

        TextMeshProUGUI tmpTitle = titleObj.AddComponent<TextMeshProUGUI>();
        tmpTitle.text = "KẾT QUẢ CHẤM BÀI TỰ LUẬN";
        tmpTitle.fontSize = 34;
        tmpTitle.fontStyle = FontStyles.Bold;
        tmpTitle.alignment = TextAlignmentOptions.Center;
        tmpTitle.color = new Color(1f, 0.9f, 0.5f);

        // 4. Trạng thái ĐẠT / CHƯA ĐẠT
        GameObject kqObj = new GameObject("txtKetQua", typeof(RectTransform));
        kqObj.transform.SetParent(frameObj.transform, false);
        RectTransform rtKq = kqObj.GetComponent<RectTransform>();
        rtKq.anchoredPosition = new Vector2(0, 180);
        rtKq.sizeDelta = new Vector2(400, 50);

        txtPopupKetQua = kqObj.AddComponent<TextMeshProUGUI>();
        txtPopupKetQua.text = "ĐẠT";
        txtPopupKetQua.fontSize = 32;
        txtPopupKetQua.fontStyle = FontStyles.Bold;
        txtPopupKetQua.alignment = TextAlignmentOptions.Center;
        txtPopupKetQua.color = new Color(0.2f, 0.85f, 0.2f);

        // 5. Điểm số
        GameObject diemObj = new GameObject("txtDiem", typeof(RectTransform));
        diemObj.transform.SetParent(frameObj.transform, false);
        RectTransform rtDiem = diemObj.GetComponent<RectTransform>();
        rtDiem.anchoredPosition = new Vector2(0, 110);
        rtDiem.sizeDelta = new Vector2(500, 70);

        txtPopupDiem = diemObj.AddComponent<TextMeshProUGUI>();
        txtPopupDiem.text = "8.5 / 10";
        txtPopupDiem.fontSize = 52;
        txtPopupDiem.fontStyle = FontStyles.Bold;
        txtPopupDiem.alignment = TextAlignmentOptions.Center;
        txtPopupDiem.color = new Color(1f, 0.85f, 0.2f);

        // 6. Nhận xét chi tiết
        GameObject nxObj = new GameObject("txtNhanXet", typeof(RectTransform));
        nxObj.transform.SetParent(frameObj.transform, false);
        RectTransform rtNx = nxObj.GetComponent<RectTransform>();
        rtNx.anchoredPosition = new Vector2(0, -20);
        rtNx.sizeDelta = new Vector2(720, 180);

        txtPopupNhanXet = nxObj.AddComponent<TextMeshProUGUI>();
        txtPopupNhanXet.text = "Nhận xét chi tiết bài làm...";
        txtPopupNhanXet.fontSize = 24;
        txtPopupNhanXet.alignment = TextAlignmentOptions.TopLeft;
        txtPopupNhanXet.textWrappingMode = TextWrappingModes.Normal;
        txtPopupNhanXet.color = Color.white;

        // 7. Chú thích Mock test
        GameObject mockNoticeObj = new GameObject("txtMockNotice", typeof(RectTransform));
        mockNoticeObj.transform.SetParent(frameObj.transform, false);
        RectTransform rtNotice = mockNoticeObj.GetComponent<RectTransform>();
        rtNotice.anchoredPosition = new Vector2(0, -145);
        rtNotice.sizeDelta = new Vector2(700, 35);

        txtPopupMockNotice = mockNoticeObj.AddComponent<TextMeshProUGUI>();
        txtPopupMockNotice.text = "[KẾT QUẢ THỬ NGHIỆM MOCK - CHƯA KẾT NỐI AI CHẤM THẬT]";
        txtPopupMockNotice.fontSize = 18;
        txtPopupMockNotice.fontStyle = FontStyles.Italic;
        txtPopupMockNotice.alignment = TextAlignmentOptions.Center;
        txtPopupMockNotice.color = new Color(0.9f, 0.7f, 0.3f, 0.9f);

        // 8. Nút Đóng / Quay lại
        GameObject btnObj = new GameObject("btnDong", typeof(RectTransform));
        btnObj.transform.SetParent(frameObj.transform, false);
        RectTransform rtBtn = btnObj.GetComponent<RectTransform>();
        rtBtn.anchoredPosition = new Vector2(0, -220);
        rtBtn.sizeDelta = new Vector2(220, 60);

        Image imgBtn = btnObj.AddComponent<Image>();
        imgBtn.color = new Color(0.2f, 0.6f, 0.25f);
        btnPopupDong = btnObj.AddComponent<Button>();

        GameObject txtBtnObj = new GameObject("Text", typeof(RectTransform));
        txtBtnObj.transform.SetParent(btnObj.transform, false);
        RectTransform rtTxtBtn = txtBtnObj.GetComponent<RectTransform>();
        rtTxtBtn.anchorMin = Vector2.zero;
        rtTxtBtn.anchorMax = Vector2.one;
        rtTxtBtn.offsetMin = Vector2.zero;
        rtTxtBtn.offsetMax = Vector2.zero;

        TextMeshProUGUI tmpBtn = txtBtnObj.AddComponent<TextMeshProUGUI>();
        tmpBtn.text = "Tiếp tục";
        tmpBtn.fontSize = 26;
        tmpBtn.fontStyle = FontStyles.Bold;
        tmpBtn.alignment = TextAlignmentOptions.Center;
        tmpBtn.color = Color.white;

        // Ẩn mặc định
        popupKetQua.SetActive(false);

        // Đảm bảo có Toast thông báo
        EnsureToastNotification(canvas);
    }

    private void EnsureToastNotification(Canvas canvas)
    {
        if (panelThongBao != null) return;

        Transform existing = canvas.transform.Find("PanelThongBao");
        if (existing != null)
        {
            panelThongBao = existing.gameObject;
            txtThongBao = existing.GetComponentInChildren<TMP_Text>();
            return;
        }

        GameObject toast = new GameObject("PanelThongBao", typeof(RectTransform));
        toast.transform.SetParent(canvas.transform, false);
        panelThongBao = toast;

        RectTransform rt = toast.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(700, 70);
        rt.anchoredPosition = new Vector2(0, 360);

        Image img = toast.AddComponent<Image>();
        img.color = new Color(0.1f, 0.1f, 0.1f, 0.92f);

        GameObject txtObj = new GameObject("txtThongBao", typeof(RectTransform));
        txtObj.transform.SetParent(toast.transform, false);
        RectTransform rtTxt = txtObj.GetComponent<RectTransform>();
        rtTxt.anchorMin = Vector2.zero;
        rtTxt.anchorMax = Vector2.one;
        rtTxt.offsetMin = new Vector2(20, 5);
        rtTxt.offsetMax = new Vector2(-20, -5);

        txtThongBao = txtObj.AddComponent<TextMeshProUGUI>();
        txtThongBao.text = "";
        txtThongBao.fontSize = 24;
        txtThongBao.alignment = TextAlignmentOptions.Center;
        txtThongBao.color = new Color(1f, 0.9f, 0.4f);

        toast.SetActive(false);
    }
    #endregion

    #region Context Menu Testing Helpers
    [ContextMenu("Test Mock Result (>= 5 Diem - DAT)")]
    public void TestMockResult_Dat()
    {
        TuLuanGradingResult dat = new TuLuanGradingResult
        {
            score = 8.5f,
            maxScore = 10f,
            status = "ĐẠT",
            reviewText = "• Trả lời chính xác và đầy đủ các thành phần chính của tế bào.\n" +
                         "• Nêu rõ chức năng bảo vệ và trao đổi chất của màng sinh chất.\n" +
                         "• Trình bày rõ ràng, dễ hiểu.",
            isMock = true
        };
        ShowResultPopup(dat);
    }

    [ContextMenu("Test Mock Result (< 5 Diem - CHUA DAT)")]
    public void TestMockResult_ChuaDat()
    {
        TuLuanGradingResult chuaDat = new TuLuanGradingResult
        {
            score = 3.5f,
            maxScore = 10f,
            status = "CHƯA ĐẠT",
            reviewText = "• Chưa nêu được đầy đủ các thành phần chính của tế bào nhân thực.\n" +
                         "• Thiếu phần giải thích chức năng của màng tế bào.\n" +
                         "• Gợi ý: Hãy đọc lại phần 1 bài Tế bào để nắm chắc kiến thức cơ bản.",
            isMock = true
        };
        ShowResultPopup(chuaDat);
    }
    #endregion
}
