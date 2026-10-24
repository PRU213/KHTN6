using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GiaoVienController : MonoBehaviour
{
    [Header("UI Text (Tuỳ chọn)")]
    public Text txtName;
    public Text txtRole;
    public TMP_Text txtNameTMP;
    public TMP_Text txtRoleTMP;

    [Header("Nút điều hướng")]
    public Button btnDangXuat;
    public Button btnQuayLai;

    void Start()
    {
        AutoFindReferences();

        string fullname = !string.IsNullOrEmpty(GameSession.Fullname) 
            ? GameSession.Fullname 
            : PlayerPrefs.GetString("Fullname", "Giáo viên");
        string role = !string.IsNullOrEmpty(GameSession.Role) 
            ? GameSession.Role 
            : PlayerPrefs.GetString("Role", "Teacher");

        if (txtName != null) txtName.text = fullname;
        if (txtRole != null) txtRole.text = role;
        if (txtNameTMP != null) txtNameTMP.text = fullname;
        if (txtRoleTMP != null) txtRoleTMP.text = role;

        if (btnDangXuat != null)
        {
            btnDangXuat.onClick.AddListener(DangXuat);
        }

        if (btnQuayLai != null)
        {
            btnQuayLai.onClick.AddListener(QuayLai);
        }
    }

    void AutoFindReferences()
    {
        if (btnDangXuat == null)
        {
            GameObject obj = GameObject.Find("btn_DangXuat");
            if (obj == null) obj = GameObject.Find("btnLogout");
            if (obj == null) obj = GameObject.Find("btnDangXuat");
            if (obj != null) btnDangXuat = obj.GetComponent<Button>();
        }

        if (btnQuayLai == null)
        {
            GameObject obj = GameObject.Find("btn_back");
            if (obj == null) obj = GameObject.Find("btnBack");
            if (obj == null) obj = GameObject.Find("btnQuayLai");
            if (obj != null) btnQuayLai = obj.GetComponent<Button>();
        }
    }

    /// <summary>
    /// Đăng xuất: Xóa thông tin phiên và quay lại màn hình đăng nhập
    /// </summary>
    public void DangXuat()
    {
        Debug.Log("[GiaoVien] Đăng xuất tài khoản");
        GameSession.ClearUserSession();
        PlayerPrefs.DeleteKey("Role");
        PlayerPrefs.Save();
        SceneManager.LoadScene("SampleScene");
    }

    /// <summary>
    /// Quay lại màn hình đăng nhập
    /// </summary>
    public void QuayLai()
    {
        SceneManager.LoadScene("SampleScene");
    }
}
