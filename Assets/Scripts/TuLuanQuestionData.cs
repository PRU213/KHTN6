using System;
using System.Collections.Generic;

/// <summary>
/// Cấu trúc dữ liệu câu hỏi tự luận từ Google Sheets.
/// Phù hợp với schema gid=610736773 trên Google Sheets:
/// CauHoiID, monID, Chuong/Chủ đề, difficulty, Question, HinhAnh, DapAnThamKhao, HuongDanChamAI, DiemToiDa, TrangThai, GiaoVienID, NguonTao, NgayTao
/// </summary>
[Serializable]
public class TuLuanQuestionData
{
    public string id = "";            // CauHoiID (e.g. TL_BIO_01_E01)
    public string monId = "";         // monID (Sinh học)
    public string topic = "";         // Chuong/Chủ đề
    public string difficulty = "";    // difficulty (Dễ, Trung bình, Khó)
    public string question = "";      // Question
    public string imageUrl = "";      // HinhAnh
    public string sampleAnswer = "";  // DapAnThamKhao
    public string rubric = "";        // HuongDanChamAI (tiêu chí chấm cho AI)
    public int maxScore = 10;         // DiemToiDa
}

/// <summary>
/// Cấu trúc dữ liệu bài nộp tự luận để chuẩn bị gửi AI chấm bài sau này.
/// Phù hợp với schema lưu trữ trên Google Sheets gid=865925192 (sheet: BaiNopTuLuan).
/// </summary>
[Serializable]
public class TuLuanSubmissionData
{
    public string submissionId = "";     // Mã lượt nộp (GUID)
    public string userId = "";           // Mã học sinh / Username
    public string cauHoiId = "";         // ID câu hỏi tự luận
    public string cauTraLoi = "";        // Nội dung văn bản câu trả lời
    public string anhBaiLamPath = "";    // Đường dẫn ảnh đã chọn trên máy
    public byte[] anhBaiLamBytes = null; // Dữ liệu nhị phân ảnh (dùng upload)
    public string driveFileId = "";      // ID file ảnh trên Google Drive
    public string driveFileUrl = "";     // URL xem ảnh trên Google Drive
    public string driveFileName = "";    // Tên file ảnh đã lưu trên Drive
    public string uploadStatus = "Chưa tải"; // Trạng thái: "Chưa tải", "Đã tải lên Drive", "Đã lưu Sheet", "Không có ảnh", "Thất bại"
    public bool isSavedToSheet = false;  // Đã lưu thành công vào sheet BaiNopTuLuan
    public float diemAI = -1f;           // Điểm số AI chấm (-1 nếu chưa chấm)
    public string nhanXetAI = "";        // Nhận xét chi tiết từ AI
    public string goiYCaiThien = "";     // Gợi ý cải thiện
    public string chiTietCham = "";      // Chi tiết điểm từng ý theo rubric
    public string ketQua = "";           // "ĐẠT" (>= 5) hoặc "CHƯA ĐẠT" (< 5)
    public string trangThaiCham = "Pending"; // "Pending" (chờ chấm), "Đang chấm", "Đã chấm"
    public string thoiGianNop = "";      // Thời gian nộp bài
}

/// <summary>
/// Model phản hồi từ Backend Google Apps Script khi upload ảnh lên Google Drive (action: saveEssayImage).
/// </summary>
[Serializable]
public class EssayImageUploadResponse
{
    public bool success;
    public string message = "";
    public string fileId = "";
    public string fileUrl = "";
    public string fileName = "";
}

/// <summary>
/// Model phản hồi từ Backend Google Apps Script khi lưu bài nộp vào sheet BaiNopTuLuan (action: saveEssaySubmission).
/// </summary>
[Serializable]
public class EssaySubmissionSaveResponse
{
    public bool success;
    public bool alreadyExists;
    public string submissionId = "";
    public string message = "";
    public string status = "";
    public string gradingStatus = "";
    public bool completed;
    public float score;
    public string feedback = "";
    public string improvements = "";
    public string criteria = "";
    public string result = "";
    public bool passed;
}

/// <summary>
/// Kết quả chấm thi từ AI (hoặc mock kiểm thử).
/// </summary>
[Serializable]
public class TuLuanGradingResult
{
    public float score;                       // Điểm số (0 - 10)
    public float maxScore = 10f;              // Thang điểm (thường là 10)
    public string reviewText = "";            // Nhận xét chi tiết từ AI (cột NhanXetAI)
    public string improvementSuggestion = ""; // Gợi ý cải thiện từ AI (cột GoiYCaiThien)
    public string detailJson = "";            // Chi tiết điểm từng tiêu chí rubric (cột ChiTietCham)
    public string status = "";                // "ĐẠT" hoặc "CHƯA ĐẠT" (cột KetQua)
    public bool isMock = false;               // Đánh dấu kết quả mock kiểm thử
}
