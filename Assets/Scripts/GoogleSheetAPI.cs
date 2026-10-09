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
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            string csvData = req.downloadHandler.text;
            bool isSuccess = false;

            string[] rows = csvData.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 1; i < rows.Length; i++) // Skip header
            {
                string[] cols = rows[i].Split(',');
                if (cols.Length >= 5)
                {
                    string dbUser = cols[1].Trim();
                    string dbPass = cols[2].Trim();
                    string dbRole = cols[4].Trim();

                    if (dbUser == username && dbPass == password && dbRole.Equals(role, StringComparison.OrdinalIgnoreCase))
                    {
                        PlayerPrefs.SetString("Username", dbUser);
                        if (cols.Length > 3) PlayerPrefs.SetString("Fullname", cols[3].Trim());
                        if (cols.Length > 4) PlayerPrefs.SetString("Role", cols[4].Trim());
                        PlayerPrefs.Save();
                        isSuccess = true;
                        break;
                    }
                }
            }

            if (isSuccess)
            {
                onComplete?.Invoke(true, "Đăng nhập thành công");
            }
            else
            {
                onComplete?.Invoke(false, "Tên đăng nhập hoặc mật khẩu không đúng");
            }
        }
        else
        {
            onComplete?.Invoke(false, "Lỗi kết nối mạng: " + req.error);
        }
    }
}