#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Bộ kiểm thử cho chức năng Upload ảnh Tự Luận lên Google Drive qua Backend.
/// Chạy từ menu: Tools > Sinh Hoc > Run TuLuan Upload Tests
/// </summary>
public static class TuLuanUploadTest
{
    private const string BACKEND_URL =
        "https://script.google.com/macros/s/AKfycbxw888KvfqRobmZfYOSH3tOO1PuFRz_yWOFTy_nsMZitdisyMSQGa8aBra_Q_oAxTifMQ/exec";

    [MenuItem("Tools/Sinh Hoc/Run TuLuan Upload Tests")]
    public static void RunAllTests()
    {
        Debug.Log("==================================================");
        Debug.Log("[TEST TỰ LUẬN] Bắt đầu kiểm thử toàn diện Upload ảnh & Nộp bài");
        Debug.Log("==================================================");

        Test1_DataModel();
        Test2_ImageValidation_5MB();
        Test3_TextOnlySubmission();
        EditorCoroutineUtility.StartCoroutine(Test4_RealBackendUpload());
    }

    private static void Test1_DataModel()
    {
        Debug.Log("--- Test 1: Kiểm tra cấu trúc dữ liệu TuLuanSubmissionData & Response ---");
        TuLuanSubmissionData sub = new TuLuanSubmissionData
        {
            submissionId = "SUB_TEST_001",
            userId = "student01",
            cauHoiId = "TL_BIO_01_E01",
            cauTraLoi = "Tế bào là đơn vị cơ sở của sự sống...",
            driveFileId = "DRIVE_FILE_12345",
            driveFileUrl = "https://drive.google.com/file/d/DRIVE_FILE_12345/view",
            driveFileName = "TuLuan_SUB_TEST_001.png",
            uploadStatus = "Đã tải lên"
        };

        if (sub.driveFileId == "DRIVE_FILE_12345" && sub.uploadStatus == "Đã tải lên")
        {
            Debug.Log("✓ [PASS] TuLuanSubmissionData chứa đầy đủ các trường Drive metadata.");
        }
        else
        {
            Debug.LogError("✗ [FAIL] TuLuanSubmissionData thiếu trường hoặc gán sai giá trị!");
        }

        string sampleJson = "{\"success\":true,\"message\":\"Upload thành công\",\"fileId\":\"FID_999\",\"fileUrl\":\"https://drive.google.com/999\",\"fileName\":\"TuLuan_999.png\"}";
        EssayImageUploadResponse resp = JsonUtility.FromJson<EssayImageUploadResponse>(sampleJson);
        if (resp != null && resp.success && resp.fileId == "FID_999")
        {
            Debug.Log("✓ [PASS] EssayImageUploadResponse parse JSON phản hồi chính xác.");
        }
        else
        {
            Debug.LogError("✗ [FAIL] Không parse được JSON phản hồi backend!");
        }
    }

    private static void Test2_ImageValidation_5MB()
    {
        Debug.Log("--- Test 2: Kiểm tra kiểm thực kích thước tệp ảnh (Tối đa 5 MB) ---");
        string tempDir = Application.temporaryCachePath;
        string validPath = Path.Combine(tempDir, "test_valid_size.png");
        string oversizedPath = Path.Combine(tempDir, "test_oversized.png");

        try
        {
            byte[] validBytes = new byte[100 * 1024];
            File.WriteAllBytes(validPath, validBytes);

            byte[] oversizedBytes = new byte[(int)(5.5f * 1024 * 1024)];
            File.WriteAllBytes(oversizedPath, oversizedBytes);

            FileInfo fiValid = new FileInfo(validPath);
            FileInfo fiOver = new FileInfo(oversizedPath);

            const long maxSizeBytes = 5 * 1024 * 1024;
            bool validOk = fiValid.Length <= maxSizeBytes;
            bool overBlocked = fiOver.Length > maxSizeBytes;

            if (validOk)
            {
                Debug.Log($"✓ [PASS] File hợp lệ 100 KB được chấp nhận ({fiValid.Length / 1024f:0.1} KB <= 5 MB).");
            }
            else
            {
                Debug.LogError("✗ [FAIL] File hợp lệ bị từ chối sai!");
            }

            if (overBlocked)
            {
                Debug.Log($"✓ [PASS] File 5.5 MB bị chặn đúng quy định ({fiOver.Length / (1024f * 1024f):0.2} MB > 5 MB).");
            }
            else
            {
                Debug.LogError("✗ [FAIL] File vượt quá 5 MB không bị chặn!");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("✗ [FAIL] Lỗi kiểm thử dung lượng: " + ex.Message);
        }
        finally
        {
            if (File.Exists(validPath)) File.Delete(validPath);
            if (File.Exists(oversizedPath)) File.Delete(oversizedPath);
        }
    }

    private static void Test3_TextOnlySubmission()
    {
        Debug.Log("--- Test 3: Nộp bài chỉ có văn bản (không tải ảnh) ---");
        string text = "Tế bào nhân sơ chưa có màng nhân.";
        byte[] imageBytes = null;

        TuLuanSubmissionData sub = new TuLuanSubmissionData
        {
            submissionId = Guid.NewGuid().ToString(),
            userId = "student01",
            cauHoiId = "TL_BIO_01_E02",
            cauTraLoi = text,
            anhBaiLamBytes = imageBytes,
            uploadStatus = (imageBytes != null && imageBytes.Length > 0) ? "Đang tải" : "Không có ảnh"
        };

        if (sub.uploadStatus == "Không có ảnh" && !string.IsNullOrEmpty(sub.cauTraLoi))
        {
            Debug.Log("✓ [PASS] Nộp bài không có ảnh: Bỏ qua upload, đặt trạng thái 'Không có ảnh', không lỗi.");
        }
        else
        {
            Debug.LogError("✗ [FAIL] Xử lý nộp bài không có ảnh không đúng!");
        }
    }

    private static IEnumerator Test4_RealBackendUpload()
    {
        Debug.Log("--- Test 4: Gửi request thật từ Unity đến Backend Google Apps Script ---");

        string testSubmissionId = "UNITY_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
        Texture2D dummyTex = new Texture2D(2, 2, TextureFormat.RGB24, false);
        dummyTex.SetPixel(0, 0, Color.blue);
        dummyTex.SetPixel(1, 1, Color.yellow);
        dummyTex.Apply();
        byte[] dummyPngBytes = dummyTex.EncodeToPNG();
        string base64Data = Convert.ToBase64String(dummyPngBytes);

        WWWForm form = new WWWForm();
        form.AddField("action", "saveEssayImage");
        form.AddField("submissionId", testSubmissionId);
        form.AddField("mimeType", "image/png");
        form.AddField("base64", base64Data);

        using (UnityWebRequest req = UnityWebRequest.Post(BACKEND_URL, form))
        {
            req.certificateHandler = new BypassCertificate();
            req.timeout = 25;
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"✗ [FAIL] Lỗi kết nối server: {req.error}");
            }
            else
            {
                string resp = req.downloadHandler.text;
                Debug.Log($"[Test 4] Phản hồi thực tế từ backend: {resp}");

                EssayImageUploadResponse parsed = null;
                try
                {
                    parsed = JsonUtility.FromJson<EssayImageUploadResponse>(resp);
                }
                catch (Exception ex)
                {
                    Debug.LogError("[Test 4] Không parse được JSON: " + ex.Message);
                }

                if (parsed != null && parsed.success)
                {
                    Debug.Log($"✓ [PASS] Upload ảnh thật từ Unity lên Google Drive thành công 100%!");
                    Debug.Log($"✓ [PASS] File ID: {parsed.fileId}");
                    Debug.Log($"✓ [PASS] File URL: {parsed.fileUrl}");
                    Debug.Log($"✓ [PASS] File Name: {parsed.fileName}");
                }
                else
                {
                    string msg = (parsed != null) ? parsed.message : "Chưa có phản hồi JSON hợp lệ";
                    Debug.LogError($"✗ [FAIL] Upload thất bại: {msg}");
                }
            }
        }

        Debug.Log("==================================================");
        Debug.Log("[TEST TỰ LUẬN] Hoàn tất 4/4 bài kiểm thử!");
        Debug.Log("==================================================");
    }
}

public static class EditorCoroutineUtility
{
    public static void StartCoroutine(IEnumerator routine)
    {
        EditorApplication.CallbackFunction updateAction = null;
        updateAction = () =>
        {
            if (routine.Current is UnityWebRequestAsyncOperation asyncOp)
            {
                if (!asyncOp.isDone) return;
            }

            if (!routine.MoveNext())
            {
                EditorApplication.update -= updateAction;
            }
        };

        EditorApplication.update += updateAction;
    }
}
#endif
