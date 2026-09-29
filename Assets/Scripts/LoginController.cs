using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    private enum Role { Student, Teacher }
    private Role selectedRole = Role.Student;

    void Start()
    {
        studentButton.onClick.AddListener(() => SelectRole(Role.Student));
        teacherButton.onClick.AddListener(() => SelectRole(Role.Teacher));
        UpdateRoleVisual();
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
            Debug.LogWarning("Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu!");
            return;
        }

        Debug.Log($"Đăng nhập với: {username} / vai trò: {selectedRole}");
        api.Login(username, password);
    }
}