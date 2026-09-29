using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class GoogleSheetAPI : MonoBehaviour
{
    private string webAppURL = "https://script.google.com/macros/s/AKfycbzdm32EURe6WzbEO3iEk-fzd8o6lJcpv8QJ1y5JR_X9CuGm8aCBg-I4Xcv4ATrvPlAKOw/exec"; // dán URL bạn vừa copy

    public void Register(string username, string password, string role)
    {
        StartCoroutine(PostRegister(username, password, role));
    }

    public void Login(string username, string password)
    {
        StartCoroutine(GetLogin(username, password));
    }

    IEnumerator PostRegister(string username, string password, string role)
    {
        WWWForm form = new WWWForm();
        string json = "{\"username\":\"" + username + "\",\"password\":\"" + password + "\",\"role\":\"" + role + "\"}";

        UnityWebRequest req = new UnityWebRequest(webAppURL, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
            Debug.Log("Đăng ký thành công: " + req.downloadHandler.text);
        else
            Debug.LogError("Lỗi: " + req.error);
    }

    IEnumerator GetLogin(string username, string password)
    {
        string url = $"{webAppURL}?username={UnityWebRequest.EscapeURL(username)}&password={UnityWebRequest.EscapeURL(password)}";
        UnityWebRequest req = UnityWebRequest.Get(url);

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
            Debug.Log("Kết quả đăng nhập: " + req.downloadHandler.text);
        else
            Debug.LogError("Lỗi: " + req.error);
    }
}