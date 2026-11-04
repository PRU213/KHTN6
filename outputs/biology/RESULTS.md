# Kết quả sửa Sinh học

Đã sửa tải trắc nghiệm học sinh Sinh học và triển khai hướng dẫn/chép lại cho tự luận dưới 5 điểm. Điểm 5 và 6 giữ luồng cho qua hiện có.

- Unity: 39 kiểm tra đạt, 0 lỗi. Bao gồm bốn scene trắc nghiệm, nguồn Sheet thật, lựa chọn chương/độ khó, thứ tự A–D, hướng dẫn, lưu nhiệm vụ và chặn chuyển câu.
- Backend Sinh học: 7 kiểm tra mới đạt. Toàn bộ backend: 178/192 đạt, cùng 14 lỗi Apps Script/lịch sử đã có trên mã gốc (171/185 đạt). Không sửa các phần đó.
- Nguồn thật: PASS. Xác minh bài nộp, chủ bài, điểm, câu tự luận và đúng chương; chỉ đọc, không tạo bài nộp thử.
- Máy chủ online: HTTP 200, biologyTutorVersion=1, copyPenaltyVersion=2, nhật ký Google Sheet sẵn sàng.
- AI trên máy chủ online: HTTP 200, trả lời thành công=true. Khóa AI trong file cấu hình cục bộ cũ không có quyền (403); Unity dùng máy chủ online đã kiểm tra, không dùng khóa cục bộ này.

Bản máy chủ: [498d955c](https://github.com/PRU213/KHTN6/commit/498d955c41469dc6b1f551ff4b3a7ee51f7338c8). Chỉ 6 file PlanetServer được đưa lên nhánh triển khai; các thay đổi Unity nằm trong dự án hiện tại, không chuyển nhánh đang làm.

Phần mới lấy nội dung chương từ Theory và đáp án đúng từ TuLuan. Học sinh phải học lại từng bước rồi nộp đủ ảnh bài viết tay một lần. AI phải xác nhận đủ nội dung chương, đề và đáp án mới mở nút tiếp tục. Nhiệm vụ Sinh học không khóa trò chơi Hóa/Vật lý.

Không thay đổi màn giáo viên, mã trò chơi Hóa/Vật lý, URL dịch vụ dùng chung, Apps Script hoặc dữ liệu câu hỏi gốc. Không ghi dữ liệu kiểm thử lên Google Sheet. Luồng học và ảnh trong kiểm thử Unity dùng dữ liệu kiểm soát; không nộp ảnh của học sinh thật để kiểm thử.

Báo cáo chi tiết: [Unity](unity-report.json), [backend](backend-tests.txt), [mã gốc](baseline-tests.txt), [máy chủ](live-health.json), [AI online](live-server-ai.json).

Ảnh: [trắc nghiệm thật](biology-live-quiz.png), [hướng dẫn](biology-instructions.png), [AI từng bước](biology-ai-guidance.png), [chép nội dung chương](biology-copy-chapter.png).
