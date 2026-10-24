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

    [Header("=== NÚT BẤM ===")]
    [Tooltip("Nút đóng popup hoặc quay lại màn chơi")]
    public Button btnDong;

    private void Awake()
    {
        Instance = this;

        // Nếu đã có EventSystem từ scene trước thì tắt EventSystem của scene PopupKetQua để tránh cảnh báo 2 EventSystem
        UnityEngine.EventSystems.EventSystem[] esList = FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None);
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
    /// Hiển thị dữ liệu bài chấm thực tế hoặc mock được truyền từ TuLuanController
    /// </summary>
    public void ApplyResultData(TuLuanGradingResult result)
    {
        if (result == null) return;

        if (txtDiem != null)
        {
            txtDiem.text = $"Điểm số: {result.score:0.0} / {result.maxScore:0}";
            txtDiem.color = Color.black;
        }

        if (txtKetQua != null)
        {
            txtKetQua.text = result.status;
            txtKetQua.color = result.score >= 5.0f
                ? new Color(0.12f, 0.75f, 0.2f)   // Xanh lá (Đạt)
                : new Color(0.9f, 0.2f, 0.2f);    // Đỏ (Chưa đạt)
        }

        if (txtNhanXet != null)
        {
            txtNhanXet.text = result.reviewText;
            txtNhanXet.color = Color.black;
        }

        if (txtMockNotice != null)
        {
            txtMockNotice.gameObject.SetActive(false);
        }

        // Nhãn nút bấm
        if (btnDong != null)
        {
            TMP_Text txtBtn = btnDong.GetComponentInChildren<TMP_Text>();
            if (txtBtn != null)
            {
                txtBtn.text = "Đóng";
            }
        }
    }

    /// <summary>
    /// Hiển thị dữ liệu mẫu để bạn nhìn thấy trực quan khi mở Scene PopupKetQua trong Unity Editor
    /// </summary>
    private void ApplySamplePreview()
    {
        if (txtDiem != null) txtDiem.text = "Điểm số: 8.5 / 10";
        if (txtKetQua != null)
        {
            txtKetQua.text = "ĐẠT";
            txtKetQua.color = new Color(0.12f, 0.75f, 0.2f);
        }
        if (txtNhanXet != null)
        {
            txtNhanXet.text = "Bài làm phân tích đúng trọng tâm, trình bày rõ ràng các nội dung chính của câu hỏi.";
        }
        if (txtMockNotice != null)
        {
            txtMockNotice.gameObject.SetActive(false);
        }
        if (btnDong != null)
        {
            TMP_Text txtBtn = btnDong.GetComponentInChildren<TMP_Text>();
            if (txtBtn != null) txtBtn.text = "Đóng";
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
