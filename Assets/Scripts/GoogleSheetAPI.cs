using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;

public class GoogleSheetAPI : MonoBehaviour
{
    private string csvURL = "https://docs.google.com/spreadsheets/d/1DcJwlN_fUDdcaQ6IfnlkMJApgWuEUqMhyZv9Kn8vfWo/export?format=csv";

    public void Login(string username, string password, string role, Action<bool, string> onComplete)
    {
        StartCoroutine(GetCSVData(username, password, role, onComplete));
    }

    IEnumerator GetCSVData(string username, string password, string role, Action<bool, string> onComplete)
    {
        UnityWebRequest req = UnityWebRequest.Get(csvURL);
        req.certificateHandler = new BypassCertificate();
        yield return req.SendWebRequest();

        string csvData = null;
        if (req.result == UnityWebRequest.Result.Success)
        {
            csvData = req.downloadHandler.text;
        }

        bool isSuccess = false;
        string trimmedUser = username != null ? username.Trim() : "";
        string trimmedPass = password != null ? password.Trim() : "";

        if (!string.IsNullOrEmpty(csvData))
        {
            string[] rows = csvData.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 1; i < rows.Length; i++) // Skip header
            {
                string[] cols = rows[i].Split(',');
                if (cols.Length >= 5)
                {
                    string dbUser = cols[1].Trim();
                    string dbPass = cols[2].Trim();
                    string dbRole = cols[4].Trim();

                    // Khớp username (không phân biệt hoa/thường) và password
                    if (dbUser.Equals(trimmedUser, StringComparison.OrdinalIgnoreCase) && dbPass == trimmedPass)
                    {
                        PlayerPrefs.SetString("Username", dbUser);
                        string fullname = cols.Length > 3 ? cols[3].Trim() : "";
                        if (!string.IsNullOrEmpty(fullname)) PlayerPrefs.SetString("Fullname", fullname);
                        PlayerPrefs.SetString("Role", dbRole);
                        PlayerPrefs.Save();

                        GameSession.Username = dbUser;
                        GameSession.Role = dbRole;
                        if (!string.IsNullOrEmpty(fullname)) GameSession.Fullname = fullname;

                        isSuccess = true;
                        break;
                    }
                }
            }
        }

        // Dự phòng offline nếu mạng lỗi
        if (!isSuccess && req.result != UnityWebRequest.Result.Success)
        {
            if (trimmedUser.Equals("teacher01", StringComparison.OrdinalIgnoreCase) && trimmedPass == "123456")
            {
                PlayerPrefs.SetString("Username", "teacher01");
                PlayerPrefs.SetString("Fullname", "Phạm Hà Lam");
                PlayerPrefs.SetString("Role", "Teacher");
                PlayerPrefs.Save();

                GameSession.Username = "teacher01";
                GameSession.Fullname = "Phạm Hà Lam";
                GameSession.Role = "Teacher";
                isSuccess = true;
            }
            else if (trimmedUser.StartsWith("student", StringComparison.OrdinalIgnoreCase) && trimmedPass == "123456")
            {
                PlayerPrefs.SetString("Username", trimmedUser);
                PlayerPrefs.SetString("Fullname", "Học sinh");
                PlayerPrefs.SetString("Role", "Student");
                PlayerPrefs.Save();

                GameSession.Username = trimmedUser;
                GameSession.Fullname = "Học sinh";
                GameSession.Role = "Student";
                isSuccess = true;
            }
        }

        if (isSuccess)
        {
            onComplete?.Invoke(true, "Đăng nhập thành công");
        }
        else
        {
            if (req.result != UnityWebRequest.Result.Success)
            {
                onComplete?.Invoke(false, "Lỗi kết nối mạng: " + req.error);
            }
            else
            {
                onComplete?.Invoke(false, "Tên đăng nhập hoặc mật khẩu không đúng");
            }
        }
    }
}
