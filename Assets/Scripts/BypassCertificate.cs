using UnityEngine.Networking;

/// <summary>
/// Bỏ qua xác thực SSL certificate trong UnityWebRequest để tránh lỗi Curl error 35: Cert verify failed.
/// </summary>
public class BypassCertificate : CertificateHandler
{
    protected override bool ValidateCertificate(byte[] certificateData)
    {
        return true;
    }
}
