using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
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

    [Header("=== CHẨN ĐOÁN TREO SCENE ===")]
    [Tooltip("Tắt tạm để kiểm tra treo có phải do tải Google Sheets hay không.")]
    public bool loadQuestionsFromGoogleSheets = true;

    [Header("=== GIAO DIỆN CHÍNH (TỰ ĐỘNG TÌM NẾU TRỐNG) ===")]
    public TMP_Text txtCauHoi;
    public TMP_InputField inputCauTraLoi;
    public Button btnTaiAnh;
    public Image imgAnh;
    public Button btnNopBai;
    public Button btnTamDung;

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

    [Header("=== LOADING KHI AI CHẤM BÀI ===")]
    public GameObject panelLoading;
    public TMP_Text txtLoading;

    [Header("=== CẤU HÌNH KIỂM THỬ MOCK ===")]
    [Tooltip("Bật chế độ thử nghiệm Mock Result khi nộp bài")]
    public bool useMockResult = false;

    [Header("=== CẤU HÌNH BACKEND UPLOAD ===")]
    [Tooltip("Google Apps Script Web App Exec URL")]
    public string backendApiUrl =
        "https://script.google.com/macros/s/AKfycbxLHvShuipgPg7x28IikDmzU7wHoE2e3ai9rhuoEnN50UsFUAWIPTG_nneHOxz-YmaY/exec";

    [Header("=== MÁU / TIM ===")]
    public Image[] healthIcons = new Image[3];

    [Header("=== DỮ LIỆU HIỆN TẠI ===")]
    [SerializeField] private TuLuanQuestionData currentQuestion;
    [SerializeField] private string selectedImagePath = "";
    [SerializeField] private Texture2D selectedTexture = null;
    [SerializeField] private byte[] selectedImageBytes = null;
    [SerializeField] private string selectedImageMimeType = "";
    [SerializeField] private string currentSubmissionId = "";
    [SerializeField] private bool isSubmitting = false;

    // Cache kết quả upload ảnh Drive để retry không upload lại nếu chỉ lỗi lưu Sheet hoặc đang chờ chấm
    [SerializeField] private string uploadedDriveFileId = "";
    [SerializeField] private string uploadedDriveFileUrl = "";
    [SerializeField] private string uploadedDriveFileName = "";

    private Sprite initialAnhSprite = null;
    private Color initialAnhColor = Color.white;
    private TuLuanGradingResult lastGradingResult = null;
    private TuLuanSubmissionData lastSubmission = null;

    public TuLuanSubmissionData LastSubmission => lastSubmission;
    public string CurrentSubmissionId => currentSubmissionId;
    public bool IsSubmitting => isSubmitting;

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
        Time.timeScale = 1f; // Khôi phục thời gian bình thường, chống lỗi đứng/dừng màn hình
        AutoBindUI();
    }

    private void Start()
    {
        Time.timeScale = 1f; // Đảm bảo thời gian chạy bình thường khi bắt đầu
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();

        Debug.Log("[TuLuan DIAG] Start: Bắt đầu liên kết nút và nạp câu hỏi.");
        SetupHealthBar(canvas);
        // Bản cũ không gắn nút Tạm Dừng ở runtime nếu chưa chạy Setup Tool.
        SetupPauseButton(canvas);
        SetupEvents();

        // Tránh một PanelLoading còn active trong Scene che toàn bộ nút.
        if (panelLoading == null && canvas != null)
        {
            Transform loadingTransform = canvas.transform.Find("PanelLoading");
            if (loadingTransform != null) panelLoading = loadingTransform.gameObject;
        }
        if (panelLoading != null && !isSubmitting) panelLoading.SetActive(false);
        if (popupKetQua == null && canvas != null)
        {
            Transform popupTransform = canvas.transform.Find("PopupKetQua");
            if (popupTransform != null) popupKetQua = popupTransform.gameObject;
        }
        if (popupKetQua != null && !isSubmitting) popupKetQua.SetActive(false);

        StartCoroutine(LoadQuestionRoutine());
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

    /// <summary>
    /// Tìm hoặc tự động khởi tạo nút Tạm Dừng (btnTamDung) ở góc trên bên phải Canvas.
    /// Đồng bộ với hệ thống PauseMenuController của trò chơi.
    /// </summary>
    public void SetupPauseButton(Canvas canvas)
    {
        if (canvas == null) return;

        // Ưu tiên reference đã được gán trong Inspector, không sinh nút trùng.
        if (btnTamDung != null)
        {
            btnTamDung.onClick.RemoveListener(OnTamDungClicked);
            btnTamDung.onClick.AddListener(OnTamDungClicked);
            return;
        }

        Transform tPause = canvas.transform.Find("btnTamDung");
        if (tPause == null)
        {
            GameObject pauseObj = new GameObject("btnTamDung", typeof(RectTransform));
            pauseObj.transform.SetParent(canvas.transform, false);
            tPause = pauseObj.transform;

            RectTransform rt = pauseObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1);
            rt.anchoredPosition = new Vector2(-50, -50);
            rt.sizeDelta = new Vector2(90, 90);

            Image img = pauseObj.AddComponent<Image>();
            Sprite pauseSprite = null;
#if UNITY_EDITOR
            pauseSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/ChonBai/btnTamDung.png");
            if (pauseSprite == null)
                pauseSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/SinhTuLuan/Pause.png");
#endif
            if (pauseSprite != null)
            {
                img.sprite = pauseSprite;
                img.preserveAspect = true;
            }
            img.color = Color.white;
            img.raycastTarget = true;
        }

        btnTamDung = tPause.GetComponent<Button>();
        if (btnTamDung == null)
        {
            btnTamDung = tPause.gameObject.AddComponent<Button>();
        }

        btnTamDung.onClick.RemoveAllListeners();
        btnTamDung.onClick.AddListener(OnTamDungClicked);
    }

    /// <summary>
    /// Xử lý sự kiện nhấn nút Tạm Dừng trong màn Tự Luận.
    /// Mở Scene TamDung cộng gộp và tạm dừng thời gian (timeScale = 0).
    /// </summary>
    public void OnTamDungClicked()
    {
        if (SceneManager.GetSceneByName("TamDung").isLoaded) return;
        Time.timeScale = 0f;
        SceneManager.LoadScene("TamDung", LoadSceneMode.Additive);
    }

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

        // Chỉ liên kết reference, tuyệt đối không cấu hình lại TMP/ScrollRect
        // trong Awake hoặc khi chạy Setup Tool (tránh vòng lặp layout rebuild).
        BindRuntimeTextFields(canvas);

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
        if (Application.isPlaying &&
            !Application.CanStreamedLevelBeLoaded("PopupKetQua") &&
            canvas.transform.Find("PopupKetQua") != null)
        {
            // Chỉ lấy reference của popup có sẵn, KHÔNG tạo UI mới lúc Awake.
            EnsureResultPopup(canvas);
        }
    }

    /// <summary>
    /// Chỉ liên kết UI, không Destroy component TMP, không thay đổi ScrollRect.
    /// Chỉ tạo input overlay khi Play và chưa có TMP_InputField hợp lệ.
    /// </summary>
    private void BindRuntimeTextFields(Canvas canvas)
    {
        Transform khDe = canvas.transform.Find("KhungDe");
        if (txtCauHoi == null && khDe != null)
        {
            Transform scroll = khDe.Find("ScrollCauHoi/Viewport");
            if (scroll != null) txtCauHoi = scroll.GetComponentInChildren<TMP_Text>(true);
            if (txtCauHoi == null)
            {
                Transform text = khDe.Find("txtCauHoi");
                if (text != null) txtCauHoi = text.GetComponent<TMP_Text>();
            }
        }

        Transform khBaiLam = canvas.transform.Find("KhungBaiLam");
        Transform answerRoot = khBaiLam != null ? khBaiLam.Find("txtCauTraLoi") : null;
        if (inputCauTraLoi == null && answerRoot != null)
        {
            // Ưu tiên TMP_InputField đã được chuẩn bị sẵn trong Scene.
            inputCauTraLoi = answerRoot.GetComponent<TMP_InputField>();
            if (inputCauTraLoi == null)
                inputCauTraLoi = answerRoot.GetComponentInChildren<TMP_InputField>(true);
        }

        if (Application.isPlaying && answerRoot != null)
        {
            // Nếu bản cũ đã lưu một TMP_InputField bị hỏng vào Scene,
            // vô hiệu hóa nó để không chạy Update(), rồi tạo input độc lập.
            if (inputCauTraLoi != null &&
                (inputCauTraLoi.textComponent == null || inputCauTraLoi.textViewport == null))
            {
                Debug.LogWarning("[TuLuan] TMP_InputField cũ thiếu Text/Viewport. Chuyển sang RuntimeInput an toàn.");
                inputCauTraLoi.enabled = false;
                inputCauTraLoi = null;
            }
            if (inputCauTraLoi == null)
                inputCauTraLoi = CreateSafeRuntimeInput(answerRoot);
        }
        if (inputCauTraLoi == null && Application.isPlaying)
            Debug.LogError("[TuLuan] Không tìm thấy ô nhập câu trả lời. Kiểm tra Canvas/KhungBaiLam/txtCauTraLoi.");
    }

    /// <summary>
    /// Tạo input overlay an toàn trong Play mode, không Destroy TextMeshProUGUI gốc.
    /// </summary>
    private TMP_InputField CreateSafeRuntimeInput(Transform answerRoot)
    {
        TMP_Text original = answerRoot.GetComponent<TMP_Text>();
        if (original != null)
        {
            original.text = "";
            original.raycastTarget = false;
        }

        GameObject inputObj = new GameObject("RuntimeInput", typeof(RectTransform), typeof(Image));
        inputObj.transform.SetParent(answerRoot, false);
        RectTransform inputRect = inputObj.GetComponent<RectTransform>();
        inputRect.anchorMin = Vector2.zero;
        inputRect.anchorMax = Vector2.one;
        inputRect.offsetMin = Vector2.zero;
        inputRect.offsetMax = Vector2.zero;
        Image inputImage = inputObj.GetComponent<Image>();
        inputImage.color = new Color(1f, 1f, 1f, 0.001f);
        inputImage.raycastTarget = true;

        GameObject viewportObj = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
        viewportObj.transform.SetParent(inputObj.transform, false);
        RectTransform viewport = viewportObj.GetComponent<RectTransform>();
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(12, 12);
        viewport.offsetMax = new Vector2(-12, -12);

        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(viewportObj.transform, false);
        TextMeshProUGUI text = textObj.GetComponent<TextMeshProUGUI>();
        StretchRuntimeRect(text.rectTransform);
        if (original != null && original.font != null) text.font = original.font;
        text.fontSize = 30;
        text.color = Color.black;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;

        GameObject placeholderObj = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
        placeholderObj.transform.SetParent(viewportObj.transform, false);
        TextMeshProUGUI placeholder = placeholderObj.GetComponent<TextMeshProUGUI>();
        StretchRuntimeRect(placeholder.rectTransform);
        if (original != null && original.font != null) placeholder.font = original.font;
        placeholder.text = "Nhập câu trả lời...";
        placeholder.fontSize = 30;
        placeholder.color = new Color(0.35f, 0.35f, 0.35f, 0.7f);
        placeholder.raycastTarget = false;

        TMP_InputField field = inputObj.AddComponent<TMP_InputField>();
        field.targetGraphic = inputImage;
        field.textViewport = viewport;
        field.textComponent = text;
        field.placeholder = placeholder;
        field.lineType = TMP_InputField.LineType.MultiLineNewline;
        field.contentType = TMP_InputField.ContentType.Standard;
        Debug.Log("[TuLuan] Đã tạo RuntimeInput an toàn (không thay đổi TextMeshPro gốc).");
        return field;
    }

    private static void StretchRuntimeRect(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    // Hai hàm cũ ConfigureQuestionScroll / ConfigureInputField đã được bỏ.
    // Không còn Destroy TextMeshPro hay gán lại ContentSizeFitter/ScrollRect
    // khi Scene được mở hoặc Setup Tool được chạy.

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
    /// Chọn ngay câu hỏi tự luận mặc định ban đầu theo độ khó và môn học.
    /// </summary>
    private TuLuanQuestionData PickDefaultQuestion()
    {
        string targetId = !string.IsNullOrEmpty(PassedQuestionId)
            ? PassedQuestionId
            : PlayerPrefs.GetString("CurrentTuLuanQuestionID", "");

        string selDiff = PlayerPrefs.GetString("SelectedDifficulty", "Dễ");

        if (fallbackQuestions != null && fallbackQuestions.Length > 0)
        {
            if (!string.IsNullOrEmpty(targetId))
            {
                var matchId = Array.Find(fallbackQuestions, q => q.id.Equals(targetId, StringComparison.OrdinalIgnoreCase));
                if (matchId != null) return matchId;
            }

            var matchDiff = Array.Find(fallbackQuestions, q => q.difficulty.Equals(selDiff, StringComparison.OrdinalIgnoreCase));
            if (matchDiff != null) return matchDiff;

            return fallbackQuestions[0];
        }
        return null;
    }

    /// <summary>
    /// Tải câu hỏi từ Google Sheets hoặc dữ liệu truyền từ Gameplay.
    /// Hiển thị ngay câu hỏi phù hợp mà không bao giờ bị đứng màn hình.
    /// </summary>
    private IEnumerator LoadQuestionRoutine()
    {
        // 1. Kiểm tra nếu có câu hỏi được truyền trực tiếp từ Gameplay
        if (PassedQuestion != null && !string.IsNullOrEmpty(PassedQuestion.question))
        {
            currentQuestion = PassedQuestion;
            ApplyCurrentQuestion();
            yield break;
        }

        // 2. Nạp ngay câu hỏi mặc định phù hợp nhất để hiển thị NGAY LẬP TỨC
        currentQuestion = PickDefaultQuestion();
        ApplyCurrentQuestion();

        string targetId = !string.IsNullOrEmpty(PassedQuestionId)
            ? PassedQuestionId
            : PlayerPrefs.GetString("CurrentTuLuanQuestionID", "");

        // 3. Có thể tắt request để khoanh vùng treo Unity ngay trong Inspector.
        if (!loadQuestionsFromGoogleSheets)
        {
            Debug.Log("[TuLuan DIAG] Đã tắt Google Sheets. Chỉ dùng câu hỏi mặc định.");
            yield break;
        }

        // Nhường 1 frame cho scene hoàn thành khởi tạo UI trước khi tải dữ liệu.
        yield return null;
        Debug.Log("[TuLuan DIAG] Bắt đầu tải CSV Google Sheets.");
        List<TuLuanQuestionData> sheetQuestions = new List<TuLuanQuestionData>();
        using (UnityWebRequest req = UnityWebRequest.Get(TULUAN_SHEET_CSV_URL))
        {
            req.certificateHandler = new BypassCertificate();
            req.timeout = 3;
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success && !string.IsNullOrEmpty(req.downloadHandler?.text))
            {
                string csvText = req.downloadHandler.text;
                string trimmedStart = csvText.TrimStart();

                // Google có thể trả về trang login HTML dù HTTP 200; không parse như CSV.
                if (csvText.Length > 2 * 1024 * 1024 ||
                    trimmedStart.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase) ||
                    trimmedStart.StartsWith("<html", StringComparison.OrdinalIgnoreCase))
                {
                    Debug.LogError("[TuLuan] Google Sheets không trả CSV hợp lệ hoặc dữ liệu vượt 2 MB. Giữ câu hỏi mặc định. Bytes/Chars=" + csvText.Length);
                    yield break;
                }

                Debug.Log("[TuLuan DIAG] Tải CSV xong. Số ký tự=" + csvText.Length + ". Bắt đầu parse.");
                try
                {
                    sheetQuestions = ParseTuLuanCSV(csvText);
                }
                catch (Exception ex)
                {
                    Debug.LogError("[TuLuan] Lỗi đọc CSV: " + ex);
                    yield break;
                }
                Debug.Log("[TuLuan DIAG] Parse CSV xong: " + sheetQuestions.Count + " câu hỏi.");
                allLoadedQuestions = new List<TuLuanQuestionData>(sheetQuestions);

                TuLuanQuestionData chosen = null;
                if (!string.IsNullOrEmpty(targetId))
                {
                    chosen = sheetQuestions.Find(q => q.id.Equals(targetId, StringComparison.OrdinalIgnoreCase));
                }

                if (chosen == null && sheetQuestions.Count > 0)
                {
                    string selDiff = PlayerPrefs.GetString("SelectedDifficulty", "Dễ");
                    List<TuLuanQuestionData> filtered = sheetQuestions.FindAll(q =>
                        q.monId.IndexOf("Sinh", StringComparison.OrdinalIgnoreCase) >= 0);

                    if (filtered.Count == 0) filtered = sheetQuestions;

                    List<TuLuanQuestionData> diffFiltered = filtered.FindAll(q =>
                        q.difficulty.Equals(selDiff, StringComparison.OrdinalIgnoreCase));
                    if (diffFiltered.Count > 0) filtered = diffFiltered;

                    candidateQuestions = filtered;
                    if (filtered.Count > 0)
                    {
                        chosen = filtered[UnityEngine.Random.Range(0, filtered.Count)];
                    }
                }

                if (chosen != null)
                {
                    currentQuestion = chosen;
                    ApplyCurrentQuestion();
                }
            }
            else
            {
                Debug.LogWarning("[TuLuan] Google Sheets không phản hồi trong 3s, giữ câu hỏi hiện tại: " + req.error);
            }
        }
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

        // Reset trạng thái nộp bài và cache upload
        HideLoading();
        currentSubmissionId = "";
        uploadedDriveFileId = "";
        uploadedDriveFileUrl = "";
        uploadedDriveFileName = "";
        isSubmitting = false;
        if (btnNopBai != null) btnNopBai.interactable = true;

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

        // User focuses manually to avoid layout cycle
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
        selectedImageBytes = null;
        selectedImageMimeType = "";
        uploadedDriveFileId = "";
        uploadedDriveFileUrl = "";
        uploadedDriveFileName = "";

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

            // Reset vị trí cuộn lên đỉnh đầu câu hỏi mượt mà
            ScrollRect scrollRect = txtCauHoi.GetComponentInParent<ScrollRect>();
            if (scrollRect != null && gameObject.activeInHierarchy)
            {
                StartCoroutine(ResetQuestionScrollRoutine(scrollRect));
            }
        }

        Debug.Log($"[TuLuan] Đã nạp câu hỏi: ID={currentQuestion.id} | Chủ đề={currentQuestion.topic} | Độ khó={currentQuestion.difficulty}");
    }

    private IEnumerator ResetQuestionScrollRoutine(ScrollRect sr)
    {
        yield return null;
        if (sr != null)
        {
            sr.verticalNormalizedPosition = 1f;
        }
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

    public List<string[]> ParseCsv(string csvData) => ParseCSVRows(csvData);

    private List<string[]> ParseCSVRows(string csvData)
    {
        // StringBuilder thay cho chuỗi += trong vòng lặp (O(n^2)) của bản gốc.
        // Giữ hỗ trợ CSV có dấu phẩy, ngoặc kép, và xuống dòng trong ô.
        List<string[]> rows = new List<string[]>();
        if (string.IsNullOrEmpty(csvData)) return rows;
        if (csvData.Length > 2 * 1024 * 1024)
            throw new InvalidOperationException("CSV quá lớn (> 2 MB), từ chối parse trên main thread.");

        List<string> columns = new List<string>();
        StringBuilder cell = new StringBuilder(256);
        bool inQuotes = false;

        for (int i = 0; i < csvData.Length; i++)
        {
            char c = csvData[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < csvData.Length && csvData[i + 1] == '"')
                {
                    cell.Append('"');
                    i++;
                }
                else inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                columns.Add(cell.ToString());
                cell.Clear();
            }
            else if ((c == '\r' || c == '\n') && !inQuotes)
            {
                if (c == '\r' && i + 1 < csvData.Length && csvData[i + 1] == '\n') i++;
                if (columns.Count > 0 || cell.Length > 0)
                {
                    columns.Add(cell.ToString());
                    rows.Add(columns.ToArray());
                }
                columns.Clear();
                cell.Clear();
            }
            else
            {
                cell.Append(c);
            }
        }
        if (columns.Count > 0 || cell.Length > 0)
        {
            columns.Add(cell.ToString());
            rows.Add(columns.ToArray());
        }
        return rows;
    }
    #endregion

    #region Image Selection Logic
    /// <summary>
    /// Xử lý chọn ảnh từ thiết bị (hỗ trợ PNG, JPG, JPEG, tối đa 5 MB).
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

        // Giới hạn dung lượng tối đa 5 MB
        try
        {
            FileInfo fi = new FileInfo(path);
            const long maxSizeBytes = 5 * 1024 * 1024; // 5 MB
            if (fi.Length > maxSizeBytes)
            {
                float sizeMB = fi.Length / (1024f * 1024f);
                ShowNotification($"Ảnh quá lớn ({sizeMB:0.1} MB)! Vui lòng chọn ảnh tối đa 5 MB.");
                Debug.LogWarning($"[TuLuan] Ảnh vượt quá 5 MB: {path} ({sizeMB:0.2} MB)");
                return;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[TuLuan] Không thể kiểm tra dung lượng file: " + ex.Message);
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
                selectedImageBytes = fileBytes;
                selectedImageMimeType = (ext == ".png") ? "image/png" : "image/jpeg";
                uploadedDriveFileId = "";
                uploadedDriveFileUrl = "";
                uploadedDriveFileName = "";

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

                Debug.Log($"[TuLuan] Đã nạp ảnh thành công: {path} ({tex.width}x{tex.height}, {fileBytes.Length / 1024f:0.0} KB)");
                ShowNotification("Đã chọn ảnh bài làm thành công!");
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
    /// Lấy ID người chơi thực tế từ hệ thống đăng nhập (GameSession hoặc PlayerPrefs).
    /// Tuyệt đối không hardcode ID giả.
    /// </summary>
    public static string GetCurrentUserId()
    {
        string savedId = PlayerPrefs.GetString("UserID", "");
        if (!string.IsNullOrEmpty(savedId))
            return savedId;

        if (!string.IsNullOrEmpty(GameSession.Username))
            return GameSession.Username;

        string savedUser = PlayerPrefs.GetString("Username", "");
        if (!string.IsNullOrEmpty(savedUser))
            return savedUser;

        return "HS001"; // Dự phòng nếu test trực tiếp Scene mà chưa qua màn Login
    }

    /// <summary>
    /// Xử lý sự kiện nhấn nút Nộp bài.
    /// Cho phép nộp văn bản, ảnh hoặc cả hai.
    /// Chống nộp trùng, gửi ảnh lên Drive (nếu có), ghi bài nộp vào sheet BaiNopTuLuan và chờ kết quả AI thật.
    /// Giữ nguyên SubmissionID và không upload lại ảnh nếu chỉ bước lưu Sheet hoặc chờ chấm bị gián đoạn.
    /// </summary>
    public void OnSubmitClicked()
    {
        if (isSubmitting)
        {
            Debug.LogWarning("[TuLuan] Đang trong quá trình nộp bài, vui lòng chờ...");
            return;
        }

        string answerText = inputCauTraLoi != null ? inputCauTraLoi.text.Trim() : "";
        bool hasText = !string.IsNullOrEmpty(answerText);
        bool hasImage = selectedImageBytes != null && selectedImageBytes.Length > 0;

        if (!hasText && !hasImage)
        {
            ShowNotification("Vui lòng nhập văn bản trả lời HOẶC tải ảnh bài làm trước khi nộp!");
            return;
        }

        // Tạo SubmissionID duy nhất nếu chưa có (khi retry giữ nguyên ID này để chống bản ghi trùng)
        if (string.IsNullOrEmpty(currentSubmissionId))
        {
            currentSubmissionId = Guid.NewGuid().ToString();
        }

        StartCoroutine(SubmitRoutine(answerText, hasImage));
    }

    private IEnumerator SubmitRoutine(string answerText, bool hasImage)
    {
        isSubmitting = true;
        if (btnNopBai != null) btnNopBai.interactable = false;

        string currentUserId = GetCurrentUserId();
        string questionId = currentQuestion != null ? currentQuestion.id : "TL_DEFAULT";

        // 1. Thu thập dữ liệu bài nộp
        TuLuanSubmissionData submission = new TuLuanSubmissionData
        {
            submissionId = currentSubmissionId,
            userId = currentUserId,
            cauHoiId = questionId,
            cauTraLoi = answerText,
            anhBaiLamPath = selectedImagePath,
            anhBaiLamBytes = selectedImageBytes,
            thoiGianNop = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        Debug.Log($"[TuLuan] Bắt đầu nộp bài: ID={submission.submissionId} | User={submission.userId} | Câu={submission.cauHoiId} | Chữ={answerText.Length} ký tự | Có ảnh={hasImage}");

        // 2. Nếu có ảnh: kiểm tra xem ảnh đã được upload Drive ở lần bấm trước chưa
        string fileUrlToSubmit = "";
        if (hasImage)
        {
            if (string.IsNullOrEmpty(uploadedDriveFileUrl))
            {
                ShowNotification("Đang tải ảnh bài làm lên Google Drive...");

                bool uploadSuccess = false;
                EssayImageUploadResponse uploadResp = null;
                string uploadError = "";

                yield return UploadEssayImageRoutine(
                    submission.submissionId,
                    selectedImageBytes,
                    selectedImageMimeType,
                    (success, resp, err) =>
                    {
                        uploadSuccess = success;
                        uploadResp = resp;
                        uploadError = err;
                    }
                );

                if (!uploadSuccess)
                {
                    // Thất bại khi upload ảnh: Giữ nguyên văn bản & ảnh đã chọn, giữ currentSubmissionId để retry
                    isSubmitting = false;
                    if (btnNopBai != null) btnNopBai.interactable = true;
                    submission.uploadStatus = "Tải ảnh thất bại";
                    lastSubmission = submission;

                    string displayErr = !string.IsNullOrEmpty(uploadError) ? uploadError : "Lỗi server Drive";
                    ShowNotification($"Tải ảnh thất bại: {displayErr}. Nhấn Nộp bài để thử lại!");
                    Debug.LogError($"[TuLuan] Upload ảnh thất bại: {uploadError} (Submission ID: {currentSubmissionId})");
                    yield break;
                }

                uploadedDriveFileId = (uploadResp != null) ? uploadResp.fileId : "";
                uploadedDriveFileUrl = (uploadResp != null) ? uploadResp.fileUrl : "";
                uploadedDriveFileName = (uploadResp != null) ? uploadResp.fileName : "";
            }

            submission.driveFileId = uploadedDriveFileId;
            submission.driveFileUrl = uploadedDriveFileUrl;
            submission.driveFileName = uploadedDriveFileName;
            submission.uploadStatus = "Đã tải lên Drive";
            fileUrlToSubmit = uploadedDriveFileUrl;
            Debug.Log($"[TuLuan] Ảnh Drive đã sẵn sàng: FileId={submission.driveFileId} | Url={submission.driveFileUrl}");
        }
        else
        {
            submission.uploadStatus = "Không có ảnh";
            fileUrlToSubmit = "";
        }

        // 3. Tải ảnh hoàn tất -> Bắt đầu quá trình lưu bài và chấm AI: Hiển thị màn hình Loading
        ShowLoading("AI đang chấm bài... Vui lòng chờ trong giây lát...");

        bool sheetSuccess = false;
        EssaySubmissionSaveResponse sheetResp = null;
        string sheetError = "";

        yield return SaveSubmissionToSheetRoutine(
            submission.submissionId,
            submission.userId,
            submission.cauHoiId,
            submission.cauTraLoi,
            fileUrlToSubmit,
            (success, resp, err) =>
            {
                sheetSuccess = success;
                sheetResp = resp;
                sheetError = err;
            }
        );

        if (!sheetSuccess)
        {
            // Thất bại khi lưu vào sheet: Ẩn loading, giữ nguyên bài làm và ảnh để retry
            HideLoading();
            isSubmitting = false;
            if (btnNopBai != null) btnNopBai.interactable = true;
            submission.uploadStatus = "Lưu Sheet thất bại";
            lastSubmission = submission;

            string displayErr = !string.IsNullOrEmpty(sheetError) ? sheetError : "Lỗi lưu dữ liệu Sheet";
            ShowNotification($"Lưu bài nộp thất bại: {displayErr}. Nhấn Nộp bài để thử lại!");
            Debug.LogError($"[TuLuan] Lưu Sheet thất bại: {sheetError} (Submission ID: {currentSubmissionId})");
            yield break;
        }

        submission.isSavedToSheet = true;
        submission.uploadStatus = "Đã lưu Sheet - Chờ AI chấm";
        lastSubmission = submission;

        Debug.Log($"[TuLuan] Lưu Google Sheets thành công! ID={submission.submissionId} | AlreadyExists={(sheetResp != null && sheetResp.alreadyExists)}");

        // 4. Nhận kết quả chấm AI:
        bool gradingDone = false;
        TuLuanGradingResult realResult = null;
        string gradingErrorMessage = "";

        // Kiểm tra xem backend đã chấm xong và trả về kết quả trực tiếp trong phản hồi chưa
        if (sheetResp != null && sheetResp.completed)
        {
            float finalScore = sheetResp.score;
            string finalStatus = (finalScore >= 5.0f) ? "ĐẠT" : "CHƯA ĐẠT";

            realResult = new TuLuanGradingResult
            {
                score = finalScore,
                maxScore = 10f,
                reviewText = sheetResp.feedback,
                improvementSuggestion = sheetResp.improvements,
                detailJson = sheetResp.criteria,
                status = finalStatus,
                isMock = false
            };
            gradingDone = true;
            Debug.Log($"[TuLuan] Đã nhận kết quả AI trực tiếp từ backend: Điểm={finalScore} | {finalStatus}");
        }
        else
        {
            // Nếu chưa xong trực tiếp: Gửi request kích hoạt backend và polling Google Sheets
            yield return RequestBackendGradingRoutine(submission.submissionId);

            yield return PollGradingResultRoutine(
                submission.submissionId,
                (success, result, err) =>
                {
                    gradingDone = success;
                    realResult = result;
                    gradingErrorMessage = err;
                }
            );
        }

        // Ẩn màn hình loading khi AI chấm xong
        HideLoading();

        if (gradingDone && realResult != null)
        {
            // Nhận kết quả AI thực tế thành công
            submission.diemAI = realResult.score;
            submission.nhanXetAI = realResult.reviewText;
            submission.goiYCaiThien = realResult.improvementSuggestion;
            submission.chiTietCham = realResult.detailJson;
            submission.ketQua = realResult.status;
            submission.trangThaiCham = "Completed";
            lastSubmission = submission;

            // Giải phóng trạng thái nộp bài cho lượt kế tiếp
            isSubmitting = false;
            if (btnNopBai != null) btnNopBai.interactable = true;
            currentSubmissionId = "";
            uploadedDriveFileId = "";
            uploadedDriveFileUrl = "";
            uploadedDriveFileName = "";

            ShowResultPopup(realResult);
        }
        else
        {
            // AI chấm chưa hoàn tất hoặc timeout: Giữ nguyên bài làm của học sinh để retry
            isSubmitting = false;
            if (btnNopBai != null) btnNopBai.interactable = true;

            string idShort = submission.submissionId.Length > 8
                ? submission.submissionId.Substring(0, 8)
                : submission.submissionId;

            string notice = !string.IsNullOrEmpty(gradingErrorMessage)
                ? $"Bài làm đã lưu (Mã: {idShort}...). {gradingErrorMessage}"
                : $"Bài làm đã lưu (Mã: {idShort}...). AI đang ở trạng thái Chờ chấm (Pending). Backend cần kích hoạt trigger tự động.";

            ShowNotification(notice);
            Debug.LogWarning($"[TuLuan] AI chưa chấm xong hoặc timeout: {notice} (ID: {submission.submissionId})");
        }
    }

    /// <summary>
    /// Gửi request upload ảnh bất đồng bộ lên backend Google Apps Script bằng UnityWebRequest.
    /// Backend lưu ảnh lên Google Drive (Folder ID: 1dNjjX0EqY88oqu7BdCoO4rDYFe1tPamC) và trả về metadata.
    /// </summary>
    private IEnumerator UploadEssayImageRoutine(
        string submissionId,
        byte[] imageBytes,
        string mimeType,
        Action<bool, EssayImageUploadResponse, string> callback)
    {
        if (string.IsNullOrEmpty(backendApiUrl))
        {
            callback?.Invoke(false, null, "Chưa cấu hình URL backend.");
            yield break;
        }

        if (imageBytes == null || imageBytes.Length == 0)
        {
            callback?.Invoke(false, null, "Dữ liệu ảnh trống.");
            yield break;
        }

        string base64Data = Convert.ToBase64String(imageBytes);

        WWWForm form = new WWWForm();
        form.AddField("action", "saveEssayImage");
        form.AddField("submissionId", submissionId);
        form.AddField("mimeType", mimeType);
        form.AddField("base64", base64Data);

        using (UnityWebRequest req = UnityWebRequest.Post(backendApiUrl, form))
        {
            req.certificateHandler = new BypassCertificate();
            req.timeout = 45; // Timeout 45s cho file upload
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                callback?.Invoke(false, null, $"Lỗi kết nối ({req.error})");
                yield break;
            }

            string responseText = req.downloadHandler.text;
            Debug.Log($"[TuLuan] Backend upload response: {responseText}");

            EssayImageUploadResponse resp = null;
            try
            {
                resp = JsonUtility.FromJson<EssayImageUploadResponse>(responseText);
            }
            catch (Exception ex)
            {
                callback?.Invoke(false, null, "Không đọc được phản hồi từ server: " + ex.Message);
                yield break;
            }

            if (resp != null && resp.success)
            {
                callback?.Invoke(true, resp, null);
            }
            else
            {
                string msg = (resp != null && !string.IsNullOrEmpty(resp.message))
                    ? resp.message
                    : "Lỗi lưu ảnh trên server.";
                callback?.Invoke(false, resp, msg);
            }
        }
    }

    /// <summary>
    /// Gửi thông tin bài nộp lên Google Sheets (action = saveEssaySubmission).
    /// Ghi dữ liệu vào sheet BaiNopTuLuan với trạng thái Pending.
    /// </summary>
    private IEnumerator SaveSubmissionToSheetRoutine(
        string submissionId,
        string userId,
        string questionId,
        string answerText,
        string fileUrl,
        Action<bool, EssaySubmissionSaveResponse, string> callback)
    {
        if (string.IsNullOrEmpty(backendApiUrl))
        {
            callback?.Invoke(false, null, "Chưa cấu hình URL backend.");
            yield break;
        }

        WWWForm form = new WWWForm();
        form.AddField("action", "saveEssaySubmission");
        form.AddField("submissionId", submissionId);
        form.AddField("userId", userId);
        form.AddField("questionId", questionId);
        form.AddField("answerText", answerText);
        form.AddField("fileUrl", fileUrl ?? "");

        using (UnityWebRequest req = UnityWebRequest.Post(backendApiUrl, form))
        {
            req.certificateHandler = new BypassCertificate();
            req.timeout = 30; // Timeout 30s
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                callback?.Invoke(false, null, $"Lỗi kết nối ({req.error})");
                yield break;
            }

            string responseText = req.downloadHandler.text;
            Debug.Log($"[TuLuan] Backend saveEssaySubmission response: {responseText}");

            EssaySubmissionSaveResponse resp = null;
            try
            {
                resp = JsonUtility.FromJson<EssaySubmissionSaveResponse>(responseText);
            }
            catch (Exception ex)
            {
                callback?.Invoke(false, null, "Không đọc được phản hồi từ server: " + ex.Message);
                yield break;
            }

            if (resp != null && resp.success)
            {
                callback?.Invoke(true, resp, null);
            }
            else
            {
                string msg = (resp != null && !string.IsNullOrEmpty(resp.message))
                    ? resp.message
                    : "Lỗi lưu bài nộp trên Google Sheets.";
                callback?.Invoke(false, resp, msg);
            }
        }
    }

    /// <summary>
    /// Gửi request yêu cầu backend chấm bài theo SubmissionID nếu có action hỗ trợ.
    /// Nếu backend bảo mật không mở trực tiếp action cho ANYONE_ANONYMOUS, request sẽ thất bại êm thấm và luồng chuyển sang polling.
    /// </summary>
    private IEnumerator RequestBackendGradingRoutine(string submissionId)
    {
        if (string.IsNullOrEmpty(backendApiUrl)) yield break;

        WWWForm form = new WWWForm();
        form.AddField("action", "gradeEssaySubmission");
        form.AddField("submissionId", submissionId);

        using (UnityWebRequest req = UnityWebRequest.Post(backendApiUrl, form))
        {
            req.certificateHandler = new BypassCertificate();
            req.timeout = 10;
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"[TuLuan] Backend grade request response: {req.downloadHandler.text}");
            }
            else
            {
                Debug.Log($"[TuLuan] Backend grade request ignored or restricted: {req.error}");
            }
        }
    }

    /// <summary>
    /// Thăm dò (polling) kết quả chấm AI thực tế từ Google Sheet BaiNopTuLuan qua Google Visualization Query API.
    /// Polling mỗi 2.5 giây trong tối đa 25 giây (10 lần).
    /// </summary>
    public IEnumerator PollGradingResultRoutine(
        string submissionId,
        Action<bool, TuLuanGradingResult, string> callback)
    {
        string spreadsheetId = "1DcJwlN_fUDdcaQ6IfnlkMJApgWuEUqMhyZv9Kn8vfWo";
        string sheetName = "BaiNopTuLuan";
        string escapedQuery = UnityWebRequest.EscapeURL($"select * where A = '{submissionId}'");
        string gvizUrl = $"https://docs.google.com/spreadsheets/d/{spreadsheetId}/gviz/tq?tqx=out:csv&sheet={sheetName}&tq={escapedQuery}";

        const int maxPollAttempts = 10;
        const float pollInterval = 2.5f;

        for (int attempt = 1; attempt <= maxPollAttempts; attempt++)
        {
            yield return new WaitForSeconds(pollInterval);

            using (UnityWebRequest req = UnityWebRequest.Get(gvizUrl))
            {
                req.certificateHandler = new BypassCertificate();
                req.timeout = 15;
                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"[TuLuan Polling] Lần {attempt}/{maxPollAttempts} lỗi mạng: {req.error}");
                    continue;
                }

                string csvText = req.downloadHandler.text;
                List<string[]> rows = ParseCSVRows(csvText);

                // rows[0] là header, rows[1] là dòng dữ liệu nếu tìm thấy
                if (rows != null && rows.Count >= 2)
                {
                    string[] cols = rows[1];
                    if (cols != null && cols.Length >= 11)
                    {
                        string diemAiStr = cols[5].Trim();
                        string nhanXetAi = cols.Length > 6 ? cols[6].Trim() : "";
                        string goiYCaiThien = cols.Length > 7 ? cols[7].Trim() : "";
                        string chiTietCham = cols.Length > 8 ? cols[8].Trim() : "";
                        string trangThaiCham = cols[10].Trim();

                        bool isCompleted = trangThaiCham.Equals("Completed", StringComparison.OrdinalIgnoreCase);
                        bool hasScore = float.TryParse(diemAiStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float scoreVal);

                        if (isCompleted || (hasScore && !string.IsNullOrEmpty(diemAiStr)))
                        {
                            float finalScore = hasScore ? scoreVal : 0f;
                            string finalStatus = (finalScore >= 5.0f) ? "ĐẠT" : "CHƯA ĐẠT";

                            TuLuanGradingResult result = new TuLuanGradingResult
                            {
                                score = finalScore,
                                maxScore = 10f,
                                reviewText = nhanXetAi,
                                improvementSuggestion = goiYCaiThien,
                                detailJson = chiTietCham,
                                status = finalStatus,
                                isMock = false
                            };

                            Debug.Log($"[TuLuan Polling] Đã nhận kết quả AI thành công ở lần thử {attempt}: Điểm={finalScore} | {finalStatus}");
                            callback?.Invoke(true, result, null);
                            yield break;
                        }
                        else
                        {
                            Debug.Log($"[TuLuan Polling] Lần {attempt}/{maxPollAttempts}: Trạng thái '{trangThaiCham}', điểm '{diemAiStr}'");
                        }
                    }
                }
            }
        }

        // Hết thời gian chờ (Timeout)
        callback?.Invoke(false, null, "Hết thời gian chờ kết quả AI (Timeout). Trạng thái hiện tại: Chờ chấm (Pending).");
    }

    /// <summary>
    /// Sinh Mock Result để kiểm thử offline khi cần thiết (không dùng để quyết định gameplay nộp bài).
    /// </summary>
    public TuLuanGradingResult GenerateMockResult(string text, bool hasImage)
    {
        TuLuanGradingResult res = new TuLuanGradingResult();
        res.isMock = true;

        if (text.Length < 25 && !hasImage)
        {
            res.score = 4.0f;
            res.status = "CHƯA ĐẠT";
            res.reviewText = "• Bài làm còn sơ sài, chưa nêu đủ các ý chính của câu hỏi.\n" +
                            "• Cần giải thích rõ cơ chế và chức năng của từng bộ phận.";
            res.improvementSuggestion = "Tham khảo lại kiến thức trong sách giáo khoa để bổ sung bài làm.";
        }
        else
        {
            res.score = 8.5f;
            res.status = "ĐẠT";
            res.reviewText = "• Trả lời đúng trọng tâm câu hỏi, bố cục rõ ràng.\n" +
                            "• Trình bày mạch lạc, nêu đúng tên và chức năng của các thành phần chính.";
            res.improvementSuggestion = "Phát huy tốt tinh thần tự học và liên hệ thực tế sâu hơn.";
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

        // Popup được tạo muộn nên phải gắn nút Đóng tại thời điểm hiển thị.
        if (btnPopupDong != null)
        {
            btnPopupDong.onClick.RemoveListener(OnClosePopupClicked);
            btnPopupDong.onClick.AddListener(OnClosePopupClicked);
        }

        if (popupKetQua != null)
        {
            popupKetQua.SetActive(true);
            popupKetQua.transform.SetAsLastSibling(); // Luôn hiển thị trên cùng
        }

        if (txtPopupDiem != null)
        {
            txtPopupDiem.text = $"{result.score:0.0} / {result.maxScore:0}";
            txtPopupDiem.color = Color.black;
        }

        if (txtPopupKetQua != null)
        {
            txtPopupKetQua.text = result.status;
            txtPopupKetQua.color = Color.black;
        }

        if (txtPopupNhanXet != null)
        {
            string fullReview = !string.IsNullOrEmpty(result.reviewText) ? result.reviewText.Trim() : "";
            if (!string.IsNullOrEmpty(result.improvementSuggestion))
            {
                fullReview += "\n\n[Gợi ý cải thiện]:\n" + result.improvementSuggestion.Trim();
            }
            txtPopupNhanXet.text = fullReview;
            txtPopupNhanXet.color = Color.black;

            ScrollRect sr = txtPopupNhanXet.GetComponentInParent<ScrollRect>();
            if (sr != null)
            {
                StartCoroutine(ResetScrollToTopRoutine(sr));
            }
        }

        if (btnPopupDong != null)
        {
            TMP_Text txtBtn = btnPopupDong.GetComponentInChildren<TMP_Text>();
            if (txtBtn != null)
            {
                txtBtn.text = "Tiếp tục";
                txtBtn.color = Color.black;
            }
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
        Time.timeScale = 1f; // Khôi phục thời gian khi trở lại gameplay
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
        Time.timeScale = 1f; // Khôi phục thời gian khi chơi lại
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
        txtPopupKetQua.color = Color.black;

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
        txtPopupDiem.color = Color.black;

        // 6. ScrollView Nhận xét chi tiết kèm Scrollbar dọc
        GameObject scrollObj = new GameObject("ScrollView_NhanXet", typeof(RectTransform), typeof(ScrollRect));
        scrollObj.transform.SetParent(frameObj.transform, false);
        RectTransform rtScroll = scrollObj.GetComponent<RectTransform>();
        rtScroll.anchoredPosition = new Vector2(0, -45);
        rtScroll.sizeDelta = new Vector2(740, 240);

        ScrollRect sRect = scrollObj.GetComponent<ScrollRect>();
        sRect.horizontal = false;
        sRect.vertical = true;
        sRect.movementType = ScrollRect.MovementType.Clamped;
        sRect.scrollSensitivity = 25f;

        // Viewport
        GameObject viewportObj = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
        viewportObj.transform.SetParent(scrollObj.transform, false);
        RectTransform rtVp = viewportObj.GetComponent<RectTransform>();
        rtVp.anchorMin = Vector2.zero;
        rtVp.anchorMax = Vector2.one;
        rtVp.pivot = new Vector2(0f, 1f);
        rtVp.offsetMin = Vector2.zero;
        rtVp.offsetMax = new Vector2(-22, 0);

        Image imgVp = viewportObj.GetComponent<Image>();
        imgVp.color = Color.clear;

        // Content
        GameObject contentObj = new GameObject("Content", typeof(RectTransform), typeof(ContentSizeFitter), typeof(VerticalLayoutGroup));
        contentObj.transform.SetParent(viewportObj.transform, false);
        RectTransform rtContent = contentObj.GetComponent<RectTransform>();
        rtContent.anchorMin = new Vector2(0, 1);
        rtContent.anchorMax = new Vector2(1, 1);
        rtContent.pivot = new Vector2(0.5f, 1);
        rtContent.offsetMin = Vector2.zero;
        rtContent.offsetMax = Vector2.zero;

        ContentSizeFitter csf = contentObj.GetComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        VerticalLayoutGroup vlg = contentObj.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(6, 6, 6, 6);
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // txtNhanXet bên trong Content
        GameObject nxObj = new GameObject("txtNhanXet", typeof(RectTransform));
        nxObj.transform.SetParent(contentObj.transform, false);
        RectTransform rtNx = nxObj.GetComponent<RectTransform>();
        rtNx.anchorMin = new Vector2(0, 1);
        rtNx.anchorMax = new Vector2(1, 1);
        rtNx.pivot = new Vector2(0.5f, 1);

        txtPopupNhanXet = nxObj.AddComponent<TextMeshProUGUI>();
        txtPopupNhanXet.text = "Nhận xét chi tiết bài làm...";
        txtPopupNhanXet.fontSize = 22;
        txtPopupNhanXet.alignment = TextAlignmentOptions.TopLeft;
        txtPopupNhanXet.textWrappingMode = TextWrappingModes.Normal;
        txtPopupNhanXet.overflowMode = TextOverflowModes.Masking;
        txtPopupNhanXet.color = Color.black;

        // Scrollbar Vertical
        GameObject sbObj = new GameObject("Scrollbar Vertical", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        sbObj.transform.SetParent(scrollObj.transform, false);
        RectTransform rtSb = sbObj.GetComponent<RectTransform>();
        rtSb.anchorMin = new Vector2(1, 0);
        rtSb.anchorMax = new Vector2(1, 1);
        rtSb.pivot = new Vector2(1, 1);
        rtSb.anchoredPosition = Vector2.zero;
        rtSb.sizeDelta = new Vector2(16, 0);

        Image imgTrack = sbObj.GetComponent<Image>();
        imgTrack.color = new Color(0.85f, 0.85f, 0.85f, 0.6f);

        Scrollbar sb = sbObj.GetComponent<Scrollbar>();
        sb.direction = Scrollbar.Direction.BottomToTop;

        GameObject slideArea = new GameObject("Sliding Area", typeof(RectTransform));
        slideArea.transform.SetParent(sbObj.transform, false);
        RectTransform rtSlide = slideArea.GetComponent<RectTransform>();
        rtSlide.anchorMin = Vector2.zero;
        rtSlide.anchorMax = Vector2.one;
        rtSlide.sizeDelta = Vector2.zero;

        GameObject handleObj = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handleObj.transform.SetParent(slideArea.transform, false);
        RectTransform rtHandle = handleObj.GetComponent<RectTransform>();
        rtHandle.anchorMin = Vector2.zero;
        rtHandle.anchorMax = Vector2.one;
        rtHandle.sizeDelta = Vector2.zero;

        Image imgHandle = handleObj.GetComponent<Image>();
        imgHandle.color = new Color(0.35f, 0.35f, 0.35f, 0.85f);

        sb.targetGraphic = imgHandle;
        sb.handleRect = rtHandle;

        sRect.content = rtContent;
        sRect.viewport = rtVp;
        sRect.verticalScrollbar = sb;
        sRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

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
        tmpBtn.color = Color.black;

        // Ẩn mặc định
        popupKetQua.SetActive(false);

        // Đảm bảo có Toast thông báo và Panel Loading
        EnsureToastNotification(canvas);
        EnsureLoadingPanel(canvas);
    }

    private IEnumerator ResetScrollToTopRoutine(ScrollRect sr)
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        if (sr != null)
        {
            sr.verticalNormalizedPosition = 1f;
        }
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

    private Coroutine loadingAnimCoroutine = null;

    /// <summary>
    /// Hiển thị màn hình loading kèm thông báo AI đang chấm bài cho người dùng biết.
    /// </summary>
    public void ShowLoading(string message = "AI đang chấm bài...")
    {
        if (panelLoading == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
            EnsureLoadingPanel(canvas);
        }

        if (panelLoading != null)
        {
            if (txtLoading != null && !string.IsNullOrEmpty(message))
            {
                txtLoading.text = message;
            }
            panelLoading.SetActive(true);
            panelLoading.transform.SetAsLastSibling();

            if (loadingAnimCoroutine != null) StopCoroutine(loadingAnimCoroutine);
            loadingAnimCoroutine = StartCoroutine(AnimateLoadingRoutine(message));
        }
    }

    /// <summary>
    /// Ẩn màn hình loading khi AI chấm xong hoặc dừng.
    /// </summary>
    public void HideLoading()
    {
        if (loadingAnimCoroutine != null)
        {
            StopCoroutine(loadingAnimCoroutine);
            loadingAnimCoroutine = null;
        }

        if (panelLoading != null)
        {
            panelLoading.SetActive(false);
        }
    }

    /// <summary>
    /// Hiệu ứng động nhấp nháy dấu chấm và xoay biểu tượng loading giúp người dùng thấy hệ thống đang xử lý.
    /// </summary>
    private IEnumerator AnimateLoadingRoutine(string baseMessage)
    {
        string baseText = string.IsNullOrEmpty(baseMessage)
            ? "AI đang chấm bài"
            : baseMessage.Replace("...", "").Replace("…", "").Trim();

        int dotCount = 0;
        Transform spinnerT = panelLoading != null ? panelLoading.transform.Find("Box/Spinner") : null;

        while (true)
        {
            dotCount = (dotCount + 1) % 4;
            string dots = new string('.', dotCount);

            if (txtLoading != null)
            {
                txtLoading.text = baseText + dots;
            }

            if (spinnerT != null)
            {
                spinnerT.Rotate(0, 0, -45f);
            }

            yield return new WaitForSeconds(0.4f);
        }
    }

    /// <summary>
    /// Tự động khởi tạo giao diện Loading đồng bộ phong cách với game nếu chưa có trong Scene.
    /// </summary>
    private void EnsureLoadingPanel(Canvas canvas)
    {
        if (canvas == null) return;

        Transform existing = canvas.transform.Find("PanelLoading");
        if (existing != null)
        {
            panelLoading = existing.gameObject;
            txtLoading = existing.Find("Box/txtLoading")?.GetComponent<TMP_Text>();
            if (txtLoading == null) txtLoading = existing.GetComponentInChildren<TMP_Text>();
            return;
        }

        // 1. Root Panel (Overlay đen mờ che màn hình chắn thao tác người dùng khi đang chấm)
        GameObject loadingObj = new GameObject("PanelLoading", typeof(RectTransform));
        loadingObj.transform.SetParent(canvas.transform, false);
        panelLoading = loadingObj;

        RectTransform rtRoot = loadingObj.GetComponent<RectTransform>();
        rtRoot.anchorMin = Vector2.zero;
        rtRoot.anchorMax = Vector2.one;
        rtRoot.offsetMin = Vector2.zero;
        rtRoot.offsetMax = Vector2.zero;

        Image bgOverlay = loadingObj.AddComponent<Image>();
        bgOverlay.color = new Color(0f, 0f, 0f, 0.65f);
        bgOverlay.raycastTarget = true;

        // 2. Box Khung thông báo ở giữa
        GameObject boxObj = new GameObject("Box", typeof(RectTransform));
        boxObj.transform.SetParent(loadingObj.transform, false);
        RectTransform rtBox = boxObj.GetComponent<RectTransform>();
        rtBox.sizeDelta = new Vector2(560, 200);
        rtBox.anchoredPosition = Vector2.zero;

        Image imgBox = boxObj.AddComponent<Image>();
        Sprite frameSprite = null;
        Transform khDe = canvas.transform.Find("KhungDe");
        if (khDe != null)
        {
            Image imgDe = khDe.GetComponent<Image>();
            if (imgDe != null) frameSprite = imgDe.sprite;
        }

        if (frameSprite != null)
        {
            imgBox.sprite = frameSprite;
            imgBox.type = Image.Type.Sliced;
        }
        else
        {
            imgBox.color = new Color(0.14f, 0.10f, 0.08f, 0.95f);
        }

        // 3. Biểu tượng / Spinner xoay
        GameObject spinnerObj = new GameObject("Spinner", typeof(RectTransform));
        spinnerObj.transform.SetParent(boxObj.transform, false);
        RectTransform rtSpinner = spinnerObj.GetComponent<RectTransform>();
        rtSpinner.anchoredPosition = new Vector2(0, 38);
        rtSpinner.sizeDelta = new Vector2(44, 44);

        TextMeshProUGUI tmpSpin = spinnerObj.AddComponent<TextMeshProUGUI>();
        tmpSpin.text = "⚙";
        tmpSpin.fontSize = 38;
        tmpSpin.alignment = TextAlignmentOptions.Center;
        tmpSpin.color = new Color(1f, 0.85f, 0.2f);

        // 4. Text tiêu đề loading
        GameObject textObj = new GameObject("txtLoading", typeof(RectTransform));
        textObj.transform.SetParent(boxObj.transform, false);
        RectTransform rtText = textObj.GetComponent<RectTransform>();
        rtText.anchoredPosition = new Vector2(0, -18);
        rtText.sizeDelta = new Vector2(500, 45);

        txtLoading = textObj.AddComponent<TextMeshProUGUI>();
        txtLoading.text = "AI đang chấm bài...";
        txtLoading.fontSize = 28;
        txtLoading.fontStyle = FontStyles.Bold;
        txtLoading.alignment = TextAlignmentOptions.Center;
        txtLoading.color = new Color(1f, 0.92f, 0.55f);

        // 5. Text phụ chú
        GameObject subObj = new GameObject("txtSub", typeof(RectTransform));
        subObj.transform.SetParent(boxObj.transform, false);
        RectTransform rtSub = subObj.GetComponent<RectTransform>();
        rtSub.anchoredPosition = new Vector2(0, -56);
        rtSub.sizeDelta = new Vector2(500, 35);

        TextMeshProUGUI tmpSub = subObj.AddComponent<TextMeshProUGUI>();
        tmpSub.text = "Vui lòng chờ trong giây lát...";
        tmpSub.fontSize = 20;
        tmpSub.fontStyle = FontStyles.Italic;
        tmpSub.alignment = TextAlignmentOptions.Center;
        tmpSub.color = new Color(0.85f, 0.85f, 0.85f, 0.9f);

        loadingObj.SetActive(false);
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
