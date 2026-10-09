using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class HistoryManager : MonoBehaviour
{
    [Header("Google Apps Script")]
    public string baseUrl =
        "DÁN_LINK_WEB_APP_CỦA_BẠN_Ở_ĐÂY";

    [Header("Thông tin người chơi hiện tại (Gắn Text hoặc TMP_Text)")]
    [SerializeField] private UnityEngine.UI.Text txtFullname;
    [SerializeField] private UnityEngine.UI.Text txtRole;
    [SerializeField] private TMP_Text txtFullnameTMP;
    [SerializeField] private TMP_Text txtRoleTMP;

    [Header("Thông tin học sinh")]
    public TMP_Text studentNameText;

    [Header("Thống kê")]
    public TMP_Text totalText;
    public TMP_Text winText;
    public TMP_Text loseText;
    public TMP_Text rateText;

    [Header("Danh sách lịch sử")]
    public Transform historyContent;
    public GameObject historyRowPrefab;


    private void OnEnable()
    {
        LoadUserInfo();
        LoadHistory();
    }

    private void LoadUserInfo()
    {
        // Tự động tìm nốt nếu chưa gắn trong Inspector
        if (txtFullnameTMP == null && txtFullname == null)
        {
            GameObject obj = GameObject.Find("txtFullname");
            if (obj == null) obj = GameObject.Find("txtName");

            if (obj != null)
            {
                txtFullnameTMP = obj.GetComponent<TMP_Text>();
                txtFullname = obj.GetComponent<UnityEngine.UI.Text>();
            }
        }

        if (txtRoleTMP == null && txtRole == null)
        {
            GameObject obj = GameObject.Find("txtRole");
            if (obj == null) obj = GameObject.Find("role");

            if (obj != null)
            {
                txtRoleTMP = obj.GetComponent<TMP_Text>();
                txtRole = obj.GetComponent<UnityEngine.UI.Text>();
            }
        }

        // Lấy thông tin đã được lưu từ sheet lúc đăng nhập
        string fullName = PlayerPrefs.GetString("Fullname", "Người chơi");
        string role = PlayerPrefs.GetString("Role", "Student");

        if (txtFullname != null) txtFullname.text = fullName;
        if (txtRole != null) txtRole.text = role;

        if (txtFullnameTMP != null) txtFullnameTMP.text = fullName;
        if (txtRoleTMP != null) txtRoleTMP.text = role;
    }


    public void LoadHistory()
    {
        StartCoroutine(GetHistory());
    }


    IEnumerator GetHistory()
    {
        // Lấy username thực tế đã lưu khi đăng nhập (nếu không có thì mặc định student01)
        string username = PlayerPrefs.GetString("Username", "student01");

        string url =
            baseUrl +
            "?action=history" +
            "&username=" +
            UnityWebRequest.EscapeURL(username);

        UnityWebRequest request =
            UnityWebRequest.Get(url);

        yield return request.SendWebRequest();


        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(
                "Không lấy được History: " +
                request.error
            );

            yield break;
        }


        Debug.Log(
            "History JSON: " +
            request.downloadHandler.text
        );


        HistoryResponse response =
            JsonUtility.FromJson<HistoryResponse>(
                request.downloadHandler.text
            );


        if (response == null || !response.success)
        {
            Debug.LogError(
                "History API trả dữ liệu lỗi"
            );

            yield break;
        }


        ShowHistory(response);
    }


    void ShowHistory(HistoryResponse data)
    {
        // =============================
        // Thông tin học sinh
        // =============================

        if (studentNameText != null)
            studentNameText.text = data.fullName;


        // =============================
        // Thống kê
        // =============================

        if (totalText != null)
            totalText.text = data.total.ToString();

        if (winText != null)
            winText.text = data.win.ToString();

        if (loseText != null)
            loseText.text = data.lose.ToString();

        if (rateText != null)
            rateText.text = data.winRate + "%";


        // =============================
        // Xóa dòng cũ
        // =============================

        foreach (Transform child in historyContent)
        {
            Destroy(child.gameObject);
        }


        // =============================
        // Tạo lịch sử
        // =============================

        if (data.histories == null)
            return;


        foreach (HistoryItem item in data.histories)
        {
            GameObject row =
                            Instantiate(
                                historyRowPrefab,
                                historyContent
                            );

            HistoryRow historyRow =
                row.GetComponent<HistoryRow>();

            if (historyRow != null)
            {
                historyRow.SetData(item);
            }
        }
    }
}
