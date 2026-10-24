using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Quản lý giao diện Popup Kết quả trong Scene riêng biệt: Assets/Scenes/Biology/PopupKetQua.unity
/// Giúp người phát triển dễ dàng mở scene này để căn chỉnh font chữ, màu sắc, vị trí khung hình trực quan trong Unity Editor.
/// </summary>
public class PopupKetQuaController : MonoBehaviour
{
    public static PopupKetQuaController Instance { get; private set; }

    /// <summary>
    /// Dữ liệu kết quả chấm bài được truyền từ TuLuanController trước khi LoadScene Additive.
    /// </summary>
    public static TuLuanGradingResult LastResult;

    [Header("=== GIAO DIỆN TEXT ===")]
    [Tooltip("Tiêu đề: KẾT QUẢ CHẤM BÀI")]
    public TMP_Text txtTieuDe;

    [Tooltip("Kết quả trạng thái: ĐẠT / CHƯA ĐẠT")]
    public TMP_Text txtKetQua;

    [Tooltip("Điểm số hiển thị (ví dụ: 8.5 / 10)")]
    public TMP_Text txtDiem;

    [Tooltip("Nội dung nhận xét chi tiết của bài làm")]
    public TMP_Text txtNhanXet;

    [Tooltip("Dòng chữ chú thích kết quả thử nghiệm Mock")]
    public TMP_Text txtMockNotice;

    [Header("=== CUỘN VĂN BẢN ===")]
    [Tooltip("ScrollRect của phần nhận xét chi tiết")]
    public ScrollRect scrollNhanXet;

    [Tooltip("Scrollbar dọc của phần nhận xét")]
    public Scrollbar scrollbarNhanXet;

    [Header("=== NÚT BẤM ===")]
    [Tooltip("Nút đóng popup hoặc quay lại màn chơi")]
    public Button btnDong;

    private void Awake()
    {
        Instance = this;
        EnsureScrollView();

        // Nếu đã có EventSystem từ scene trước thì tắt EventSystem của scene PopupKetQua để tránh cảnh báo 2 EventSystem
        UnityEngine.EventSystems.EventSystem[] esList = FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Include);
        if (esList.Length > 1)
        {
            foreach (var es in esList)
            {
                if (es.gameObject.scene == gameObject.scene)
                {
                    es.gameObject.SetActive(false);
                }
            }
        }
    }

    private void Start()
    {
        EnsureScrollView();

        if (btnDong != null)
        {
            btnDong.onClick.RemoveAllListeners();
            btnDong.onClick.AddListener(OnCloseClicked);
        }

        // Cập nhật giao diện từ dữ liệu truyền vào
        if (LastResult != null)
        {
            ApplyResultData(LastResult);
        }
        else
        {
            // Dữ liệu mẫu (Preview) khi bạn mở trực tiếp Scene PopupKetQua để chỉnh sửa trong Editor
            ApplySamplePreview();
        }
    }

    /// <summary>
    /// Đảm bảo văn bản nhận xét nằm gọn bên trong ScrollView có Scrollbar dọc, tránh tràn chữ ra ngoài.
    /// </summary>
    public void EnsureScrollView()
    {
        if (txtNhanXet == null) return;

        if (scrollNhanXet == null)
        {
            scrollNhanXet = txtNhanXet.GetComponentInParent<ScrollRect>();
        }

        if (scrollNhanXet != null)
        {
            txtNhanXet.color = Color.black;
            txtNhanXet.overflowMode = TextOverflowModes.Overflow;
            txtNhanXet.textWrappingMode = TextWrappingModes.Normal;
            txtNhanXet.alignment = TextAlignmentOptions.TopLeft;
            return;
        }

        // Tạo cấu trúc ScrollView bao quanh txtNhanXet
        Transform origParent = txtNhanXet.transform.parent;
        int origSiblingIndex = txtNhanXet.transform.GetSiblingIndex();

        GameObject scrollObj = new GameObject("ScrollView_NhanXet", typeof(RectTransform), typeof(ScrollRect));
        scrollObj.transform.SetParent(origParent, false);
        scrollObj.transform.SetSiblingIndex(origSiblingIndex);

        RectTransform rtScroll = scrollObj.GetComponent<RectTransform>();
        rtScroll.anchorMin = new Vector2(0.5f, 0.5f);
        rtScroll.anchorMax = new Vector2(0.5f, 0.5f);
        rtScroll.pivot = new Vector2(0.5f, 0.5f);
        rtScroll.anchoredPosition = new Vector2(0f, -45f);
        rtScroll.sizeDelta = new Vector2(740f, 240f);

        scrollNhanXet = scrollObj.GetComponent<ScrollRect>();
        scrollNhanXet.horizontal = false;
        scrollNhanXet.vertical = true;
        scrollNhanXet.movementType = ScrollRect.MovementType.Clamped;
        scrollNhanXet.scrollSensitivity = 25f;

        // Viewport
        GameObject viewportObj = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
        viewportObj.transform.SetParent(scrollObj.transform, false);
        RectTransform rtViewport = viewportObj.GetComponent<RectTransform>();
        rtViewport.anchorMin = Vector2.zero;
        rtViewport.anchorMax = Vector2.one;
        rtViewport.pivot = new Vector2(0f, 1f);
        rtViewport.offsetMin = Vector2.zero;
        rtViewport.offsetMax = new Vector2(-22f, 0f);

        Image imgViewport = viewportObj.GetComponent<Image>();
        imgViewport.color = Color.clear;

        // Content
        GameObject contentObj = new GameObject("Content", typeof(RectTransform), typeof(ContentSizeFitter), typeof(VerticalLayoutGroup));
        contentObj.transform.SetParent(viewportObj.transform, false);
        RectTransform rtContent = contentObj.GetComponent<RectTransform>();
        rtContent.anchorMin = new Vector2(0f, 1f);
        rtContent.anchorMax = new Vector2(1f, 1f);
        rtContent.pivot = new Vector2(0.5f, 1f);
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

        // Chuyển txtNhanXet vào Content
        txtNhanXet.transform.SetParent(contentObj.transform, false);
        RectTransform rtText = txtNhanXet.rectTransform;
        rtText.anchorMin = new Vector2(0f, 1f);
        rtText.anchorMax = new Vector2(1f, 1f);
        rtText.pivot = new Vector2(0.5f, 1f);
        txtNhanXet.alignment = TextAlignmentOptions.TopLeft;
        txtNhanXet.textWrappingMode = TextWrappingModes.Normal;
        txtNhanXet.overflowMode = TextOverflowModes.Overflow;
        txtNhanXet.color = Color.black;
        txtNhanXet.fontSize = 22f;

        // Scrollbar Vertical
        GameObject scrollbarObj = new GameObject("Scrollbar Vertical", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        scrollbarObj.transform.SetParent(scrollObj.transform, false);
        RectTransform rtScrollbar = scrollbarObj.GetComponent<RectTransform>();
        rtScrollbar.anchorMin = new Vector2(1f, 0f);
        rtScrollbar.anchorMax = new Vector2(1f, 1f);
        rtScrollbar.pivot = new Vector2(1f, 1f);
        rtScrollbar.anchoredPosition = Vector2.zero;
        rtScrollbar.sizeDelta = new Vector2(16f, 0f);

        Image imgTrack = scrollbarObj.GetComponent<Image>();
        imgTrack.color = new Color(0.85f, 0.85f, 0.85f, 0.6f);

        scrollbarNhanXet = scrollbarObj.GetComponent<Scrollbar>();
        scrollbarNhanXet.direction = Scrollbar.Direction.BottomToTop;

        // Sliding Area
        GameObject slidingArea = new GameObject("Sliding Area", typeof(RectTransform));
        slidingArea.transform.SetParent(scrollbarObj.transform, false);
        RectTransform rtSliding = slidingArea.GetComponent<RectTransform>();
        rtSliding.anchorMin = Vector2.zero;
        rtSliding.anchorMax = Vector2.one;
        rtSliding.sizeDelta = Vector2.zero;

        // Handle
        GameObject handleObj = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handleObj.transform.SetParent(slidingArea.transform, false);
        RectTransform rtHandle = handleObj.GetComponent<RectTransform>();
        rtHandle.anchorMin = Vector2.zero;
        rtHandle.anchorMax = Vector2.one;
        rtHandle.sizeDelta = Vector2.zero;

        Image imgHandle = handleObj.GetComponent<Image>();
        imgHandle.color = new Color(0.35f, 0.35f, 0.35f, 0.85f);

        scrollbarNhanXet.targetGraphic = imgHandle;
        scrollbarNhanXet.handleRect = rtHandle;

        scrollNhanXet.content = rtContent;
        scrollNhanXet.viewport = rtViewport;
        scrollNhanXet.verticalScrollbar = scrollbarNhanXet;
        scrollNhanXet.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
    }

    private System.Collections.IEnumerator ResetScrollToTopRoutine()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        if (scrollNhanXet != null)
        {
            scrollNhanXet.verticalNormalizedPosition = 1f;
        }
    }

    /// <summary>
    /// Hiển thị dữ liệu bài chấm thực tế hoặc mock được truyền từ TuLuanController
    /// </summary>
    public void ApplyResultData(TuLuanGradingResult result)
    {
        if (result == null) return;
        EnsureScrollView();

        if (txtTieuDe != null)
        {
            txtTieuDe.color = Color.black;
        }

        if (txtDiem != null)
        {
            txtDiem.text = $"Điểm số: {result.score:0.0} / {result.maxScore:0}";
            txtDiem.color = Color.black;
        }

        if (txtKetQua != null)
        {
            txtKetQua.text = result.status;
            txtKetQua.color = Color.black;
        }

        if (txtNhanXet != null)
        {
            string fullReview = !string.IsNullOrEmpty(result.reviewText) ? result.reviewText.Trim() : "Không có nhận xét.";
            if (!string.IsNullOrEmpty(result.improvementSuggestion))
            {
                fullReview += "\n\n[Gợi ý cải thiện]:\n" + result.improvementSuggestion.Trim();
            }
            txtNhanXet.text = fullReview;
            txtNhanXet.color = Color.black;
        }

        if (txtMockNotice != null)
        {
            txtMockNotice.gameObject.SetActive(false);
        }

        // Nhãn nút bấm: Luôn hiển thị "Tiếp tục" theo đúng yêu cầu
        if (btnDong != null)
        {
            TMP_Text txtBtn = btnDong.GetComponentInChildren<TMP_Text>();
            if (txtBtn != null)
            {
                txtBtn.text = "Tiếp tục";
                txtBtn.color = Color.black;
            }
        }

        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(ResetScrollToTopRoutine());
        }
    }

    /// <summary>
    /// Hiển thị dữ liệu mẫu để bạn nhìn thấy trực quan khi mở Scene PopupKetQua trong Unity Editor
    /// </summary>
    private void ApplySamplePreview()
    {
        EnsureScrollView();

        if (txtTieuDe != null) txtTieuDe.color = Color.black;

        if (txtDiem != null)
        {
            txtDiem.text = "Điểm số: 8.5 / 10";
            txtDiem.color = Color.black;
        }

        if (txtKetQua != null)
        {
            txtKetQua.text = "ĐẠT";
            txtKetQua.color = Color.black;
        }

        if (txtNhanXet != null)
        {
            txtNhanXet.text = "Bài làm phân tích đúng trọng tâm, trình bày rõ ràng các nội dung chính của câu hỏi.\n\n[Gợi ý cải thiện]:\nEm đã nắm vững kiến thức cơ bản, có thể mở rộng thêm liên hệ thực tiễn.";
            txtNhanXet.color = Color.black;
        }

        if (txtMockNotice != null)
        {
            txtMockNotice.gameObject.SetActive(false);
        }

        if (btnDong != null)
        {
            TMP_Text txtBtn = btnDong.GetComponentInChildren<TMP_Text>();
            if (txtBtn != null)
            {
                txtBtn.text = "Tiếp tục";
                txtBtn.color = Color.black;
            }
        }

        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(ResetScrollToTopRoutine());
        }
    }

    /// <summary>
    /// Xử lý khi nhấn nút trên Popup
    /// </summary>
    public void OnCloseClicked()
    {
        bool isPassed = LastResult != null && LastResult.score >= 5.0f;
        int currentHp = GameData.Instance != null ? GameData.Instance.currentHealth : 3;
        bool isGameOver = !isPassed && currentHp <= 0;

        // 1. Báo cho TuLuanController biết để xử lý logic:
        // - Nếu >= 5: Tiêu diệt quái và load lại game để tiếp tục di chuyển
        // - Nếu < 5 (còn tim): Ở lại TuLuan.unity, xóa bài cũ và nạp câu hỏi khác
        // - Nếu < 5 (hết tim): Reset quái, reset máu và load lại game từ đầu
        if (TuLuanController.Instance != null)
        {
            TuLuanController.Instance.OnClosePopupClicked();
        }

        // 2. Nếu người chơi còn sống và cần làm tiếp câu khác trong TuLuan, unload popup
        if (!isPassed && !isGameOver)
        {
            Scene popupScene = SceneManager.GetSceneByName("PopupKetQua");
            if (popupScene.isLoaded)
            {
                SceneManager.UnloadSceneAsync("PopupKetQua");
            }
        }
    }
}
