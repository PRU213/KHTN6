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
/// Phù hợp với schema lưu trữ trên Google Sheets gid=865925192.
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
    public float diemAI = -1f;           // Điểm số AI chấm (-1 nếu chưa chấm)
    public string nhanXetAI = "";        // Nhận xét chi tiết từ AI
    public string goiYCaiThien = "";     // Gợi ý cải thiện
    public string chiTietCham = "";      // Chi tiết điểm từng ý theo rubric
    public string ketQua = "";           // "ĐẠT" (>= 5) hoặc "CHƯA ĐẠT" (< 5)
    public string trangThaiCham = "Chờ chấm"; // "Chờ chấm", "Đang chấm", "Đã chấm"
    public string thoiGianNop = "";      // Thời gian nộp bài
}

/// <summary>
/// Kết quả chấm thi (dùng cho Mock Result hiện tại & AI thật sau này).
/// </summary>
[Serializable]
public class TuLuanGradingResult
{
    public float score;              // Điểm số (0 - 10)
    public float maxScore = 10f;     // Thang điểm (thường là 10)
    public string reviewText = "";   // Nhận xét chi tiết
    public string status = "";       // "ĐẠT" hoặc "CHƯA ĐẠT"
    public bool isMock = false;      // Đánh dấu kết quả mock kiểm thử
}
