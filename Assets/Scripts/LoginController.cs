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
        studentButton.onClick.AddListener(() => SelectRole(Role.Student));
        teacherButton.onClick.AddListener(() => SelectRole(Role.Teacher));
        UpdateRoleVisual();
        
        if (errorText != null) errorText.text = "";

        if (btnHienMK != null) btnHienMK.onClick.AddListener(ShowPassword);
        if (btnAnMK != null) btnAnMK.onClick.AddListener(HidePassword);

        // Đặt trạng thái mặc định: Ẩn mật khẩu
        HidePassword();
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
        studentButton.transform.localScale = (selectedRole == Role.Student) ? Vector3.one * 1.08f : Vector3.one;
        teacherButton.transform.localScale = (selectedRole == Role.Teacher) ? Vector3.one * 1.08f : Vector3.one;
    }

    public void OnLoginClicked()
    {
        string username = usernameInput.text;
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            if (errorText != null) errorText.text = "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu!";
            return;
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

            // LƯU USERNAME NGƯỜI ĐANG ĐĂNG NHẬP
            PlayerPrefs.SetString(
                "Username",
                usernameInput.text.Trim()
            );

            PlayerPrefs.Save();

            SceneManager.LoadScene("HomePage");
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