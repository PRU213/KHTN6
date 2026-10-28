using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Quản lý giao diện Popup Loading (chờ AI chấm bài) trong Scene riêng biệt: Assets/Scenes/Biology/PopupLoading.unity
/// Giúp người phát triển dễ dàng mở scene này để căn chỉnh hình ảnh, font chữ, màu sắc trực quan trong Unity Editor.
/// </summary>
public class PopupLoadingController : MonoBehaviour
{
    public static PopupLoadingController Instance { get; private set; }

    /// <summary>
    /// Thông điệp hiển thị được truyền vào trước khi LoadScene Additive.
    /// </summary>
    public static string CurrentMessage = "AI đang chấm bài...";
    public static string CurrentSubMessage = "Vui lòng chờ trong giây lát...";

    [Header("=== GIAO DIỆN TEXT ===")]
    [Tooltip("Dòng chữ thông báo chính: AI đang chấm bài...")]
    public TMP_Text txtLoading;

    [Tooltip("Dòng chữ phụ chú: Vui lòng chờ trong giây lát...")]
    public TMP_Text txtSub;

    [Header("=== HIỆU ỨNG QUAY (SPINNER) ===")]
    [Tooltip("RectTransform của biểu tượng quay")]
    public RectTransform spinner;

    [Tooltip("Sprite biểu tượng xoay (bánh răng / mũi tên tròn)")]
    public Sprite spinnerSprite;

    [Tooltip("Tốc độ quay (độ/giây)")]
    public float spinSpeed = 120f;

    [Header("=== FONT TIẾNG VIỆT (TỰ ĐỘNG GÁN NẾU THIẾU) ===")]
    public TMP_FontAsset fontRegular;
    public TMP_FontAsset fontBold;

    private void Awake()
    {
        Instance = this;

        EnsureSpinnerGraphic();
        EnsureVietnameseFont();

        // Nếu đã có EventSystem từ scene trước thì tắt EventSystem của scene này để tránh xung đột
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
        EnsureSpinnerGraphic();
        EnsureVietnameseFont();
        ApplyMessages(CurrentMessage, CurrentSubMessage);
    }

    private void Update()
    {
        if (spinner != null)
        {
            spinner.Rotate(0f, 0f, -spinSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// Đảm bảo spinner dùng Image sprite bánh răng / xoay thay vì ký tự Unicode (tránh lỗi ô vuông missing glyph trên TextMeshPro).
    /// </summary>
    public void EnsureSpinnerGraphic()
    {
        if (spinner == null) return;

        // Nếu Spinner trước đó là TextMeshProUGUI với ký tự unicode bánh răng ⚙ (bị ô vuông thiếu glyph)
        TMP_Text tmpSpinner = spinner.GetComponent<TMP_Text>();
        if (tmpSpinner != null)
        {
            Destroy(tmpSpinner);
        }

        Image img = spinner.GetComponent<Image>();
        if (img == null)
        {
            img = spinner.gameObject.AddComponent<Image>();
        }

#if UNITY_EDITOR
        if (spinnerSprite == null)
        {
            spinnerSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/TeacherUI/Sprites/Icons/icon_gear.png")
                         ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/TeacherUI/Sprites/Icons/icon_refresh.png");
        }
#endif
        if (spinnerSprite != null && img.sprite == null)
        {
            img.sprite = spinnerSprite;
        }

        if (img.sprite != null)
        {
            img.color = new Color(0.93f, 0.86f, 0.68f, 1f); // Màu vàng kem hài hòa khung gỗ
        }
        else
        {
            img.color = new Color(1f, 0.85f, 0.2f, 1f);
        }
        img.raycastTarget = false;
    }

    /// <summary>
    /// Đảm bảo font chữ hỗ trợ đầy đủ tiếng Việt có dấu (sắc, huyền, hỏi, ngã, nặng...).
    /// Tuyệt đối không dùng FontStyles.Italic trên SDF font nếu không có Italic face vì sẽ làm mất dấu tiếng Việt.
    /// </summary>
    public void EnsureVietnameseFont()
    {
#if UNITY_EDITOR
        if (fontBold == null)
        {
            fontBold = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TeacherUI/Fonts/DejaVuSans-Bold SDF96.asset");
        }
        if (fontRegular == null)
        {
            fontRegular = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TeacherUI/Fonts/DejaVuSans SDF96.asset")
                       ?? fontBold;
        }
#endif

        TMP_FontAsset vietnameseFont = fontBold != null ? fontBold : fontRegular;

        if (vietnameseFont != null)
        {
            if (txtLoading != null)
            {
                txtLoading.font = vietnameseFont;
                txtLoading.fontStyle = FontStyles.Bold;
            }

            if (txtSub != null)
            {
                txtSub.font = vietnameseFont;
                txtSub.fontStyle = FontStyles.Normal; // Bắt buộc Normal để hiển thị trọn vẹn tiếng Việt có dấu
            }
        }
    }

    public void ApplyMessages(string mainMsg, string subMsg = null)
    {
        EnsureVietnameseFont();

        // 1. Nếu caller truyền gộp cả câu chính lẫn câu phụ vào mainMsg
        if (!string.IsNullOrEmpty(mainMsg) && mainMsg.Contains("Vui lòng chờ trong giây lát"))
        {
            mainMsg = "AI đang chấm bài...";
            if (string.IsNullOrEmpty(subMsg))
            {
                subMsg = "Vui lòng chờ trong giây lát...";
            }
        }

        // 2. Tẩy sạch lỗi mã hoá ký tự / mojibake nếu có chuỗi lỗi truyền vào
        if (!string.IsNullOrEmpty(subMsg) && (subMsg.Contains("Ã") || subMsg.Contains("Ä") || subMsg.Contains("chá»") || subMsg.Contains("lA") || subMsg.Contains("")))
        {
            subMsg = "Vui lòng chờ trong giây lát...";
        }

        if (txtLoading != null && !string.IsNullOrEmpty(mainMsg))
        {
            txtLoading.text = mainMsg;
        }

        if (txtSub != null && !string.IsNullOrEmpty(subMsg))
        {
            txtSub.text = subMsg;
        }
    }

    /// <summary>
    /// Helper tĩnh để mở popup loading từ bất kỳ đâu.
    /// </summary>
    public static void Show(string message = "AI đang chấm bài...", string subMessage = "Vui lòng chờ trong giây lát...")
    {
        if (!string.IsNullOrEmpty(message) && message.Contains("Vui lòng chờ trong giây lát"))
        {
            message = "AI đang chấm bài...";
            if (string.IsNullOrEmpty(subMessage))
            {
                subMessage = "Vui lòng chờ trong giây lát...";
            }
        }

        if (!string.IsNullOrEmpty(subMessage) && (subMessage.Contains("Ã") || subMessage.Contains("Ä") || subMessage.Contains("chá»") || subMessage.Contains("lA") || subMessage.Contains("")))
        {
            subMessage = "Vui lòng chờ trong giây lát...";
        }

        CurrentMessage = !string.IsNullOrEmpty(message) ? message : "AI đang chấm bài...";
        CurrentSubMessage = !string.IsNullOrEmpty(subMessage) ? subMessage : "Vui lòng chờ trong giây lát...";

        if (Application.CanStreamedLevelBeLoaded("PopupLoading"))
        {
            if (!SceneManager.GetSceneByName("PopupLoading").isLoaded)
            {
                SceneManager.LoadScene("PopupLoading", LoadSceneMode.Additive);
            }
            else if (Instance != null)
            {
                Instance.ApplyMessages(CurrentMessage, CurrentSubMessage);
            }
        }
    }

    /// <summary>
    /// Helper tĩnh để đóng popup loading.
    /// </summary>
    public static void Hide()
    {
        if (SceneManager.GetSceneByName("PopupLoading").isLoaded)
        {
            SceneManager.UnloadSceneAsync("PopupLoading");
        }
    }
}
