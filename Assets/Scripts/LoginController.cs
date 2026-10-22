using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class LoginController : MonoBehaviour
{
    [Header("Input Fields")]
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;

    [Header("Role Buttons")]
    public Button studentButton;
    public Button teacherButton;

    [Header("API")]
    public GoogleSheetAPI api;

    [Header("UI Status")]
    public TMP_Text errorText;

    [Header("Password Visibility")]
    public Button btnHienMK;
    public Button btnAnMK;

    private enum Role { Student, Teacher }
    private Role selectedRole = Role.Student;

    void Start()
    {
        if (studentButton != null)
            studentButton.onClick.AddListener(() => SelectRole(Role.Student));
        if (teacherButton != null)
            teacherButton.onClick.AddListener(() => SelectRole(Role.Teacher));
        UpdateRoleVisual();
        
        if (errorText != null) errorText.text = "";

        if (btnHienMK != null) btnHienMK.onClick.AddListener(ShowPassword);
        if (btnAnMK != null) btnAnMK.onClick.AddListener(HidePassword);

        // Đặt trạng thái mặc định: Ẩn mật khẩu
        HidePassword();

        // Tự động nhận diện vai trò khi gõ username
        if (usernameInput != null)
        {
            usernameInput.onValueChanged.AddListener((val) =>
            {
                string v = val.Trim().ToLower();
                if (v.StartsWith("teacher") || v.StartsWith("gv") || v.Contains("giaovien"))
                {
                    SelectRole(Role.Teacher);
                }
                else if (v.StartsWith("student") || v.StartsWith("hs") || v.Contains("hocsinh"))
                {
                    SelectRole(Role.Student);
                }
            });
        }
    }

    public void ShowPassword()
    {
        if (passwordInput != null)
        {
            passwordInput.contentType = TMP_InputField.ContentType.Standard;
            passwordInput.ForceLabelUpdate();
        }
        if (btnHienMK != null) btnHienMK.gameObject.SetActive(false);
        if (btnAnMK != null) btnAnMK.gameObject.SetActive(true);
    }

    public void HidePassword()
    {
        if (passwordInput != null)
        {
            passwordInput.contentType = TMP_InputField.ContentType.Password;
            passwordInput.ForceLabelUpdate();
        }
        if (btnHienMK != null) btnHienMK.gameObject.SetActive(true);
        if (btnAnMK != null) btnAnMK.gameObject.SetActive(false);
    }

    void SelectRole(Role role)
    {
        selectedRole = role;
        UpdateRoleVisual();
        Debug.Log("Đã chọn vai trò: " + selectedRole);
    }

    void UpdateRoleVisual()
    {
        if (studentButton != null)
            studentButton.transform.localScale = (selectedRole == Role.Student) ? Vector3.one * 1.08f : Vector3.one;
        if (teacherButton != null)
            teacherButton.transform.localScale = (selectedRole == Role.Teacher) ? Vector3.one * 1.08f : Vector3.one;
    }

    public void OnLoginClicked()
    {
        string username = usernameInput != null ? usernameInput.text.Trim() : "";
        string password = passwordInput != null ? passwordInput.text.Trim() : "";

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            if (errorText != null) errorText.text = "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu!";
            return;
        }

        // Tự động chỉnh role nếu tên đăng nhập là giáo viên hoặc học sinh
        string lowerUser = username.ToLower();
        if (lowerUser.StartsWith("teacher") || lowerUser.StartsWith("gv") || lowerUser.Contains("giaovien"))
        {
            selectedRole = Role.Teacher;
            UpdateRoleVisual();
        }
        else if (lowerUser.StartsWith("student") || lowerUser.StartsWith("hs") || lowerUser.Contains("hocsinh"))
        {
            selectedRole = Role.Student;
            UpdateRoleVisual();
        }

        if (errorText != null) errorText.text = "Đang đăng nhập...";

        Debug.Log($"Đăng nhập với: {username} / vai trò: {selectedRole}");
        api.Login(username, password, selectedRole.ToString(), OnLoginResponse);
    }

    private void OnLoginResponse(bool isSuccess, string message)
    {
        if (isSuccess)
        {
            Debug.Log(message);

            string username = usernameInput != null ? usernameInput.text.Trim() : "";
            
            // Lấy role từ GameSession hoặc PlayerPrefs (đã được GoogleSheetAPI lưu chính xác từ dữ liệu thực)
            string role = !string.IsNullOrEmpty(GameSession.Role) ? GameSession.Role : PlayerPrefs.GetString("Role", "");
            if (string.IsNullOrEmpty(role))
            {
                role = selectedRole.ToString();
            }

            // LƯU USERNAME VÀ ROLE NGƯỜI ĐANG ĐĂNG NHẬP
            PlayerPrefs.SetString("Username", username);
            PlayerPrefs.SetString("Role", role);
            PlayerPrefs.Save();

            // Lưu vào session
            GameSession.Username = username;
            GameSession.Role = role;

            // CHECK ROLE VÀ ĐIỀU HƯỚNG TRỰC TIẾP TỪ LOGIN:
            // - Nếu role là Teacher -> điều hướng đến scene GiaoVien
            // - Nếu role là Student -> điều hướng đến scene HomePage
            if (GameSession.IsTeacher())
            {
                Debug.Log($"[Login] Phát hiện vai trò Giáo viên ({role}) -> Điều hướng đến scene GiaoVien");
                SceneManager.LoadScene("GiaoVien");
            }
            else
            {
                Debug.Log($"[Login] Phát hiện vai trò Học sinh ({role}) -> Điều hướng đến scene HomePage");
                SceneManager.LoadScene("HomePage");
            }
        }
        else
        {
            Debug.LogError(message);

            if (errorText != null)
            {
                errorText.text = message;
            }
        }
    }
}
