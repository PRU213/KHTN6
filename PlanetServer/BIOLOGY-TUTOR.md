# Tự luận Sinh học: hướng dẫn và bài chép

Chỉ áp dụng cho bài nộp của `TuLuanController`. Điểm **dưới 5** yêu cầu học lại với AI rồi viết tay nội dung chương, đề bài và đáp án đúng một lần. Điểm **5 trở lên**, gồm 6 điểm, giữ luồng cho qua đã có của Sinh học. Quy tắc Hóa, Vật lý, đối kháng và giáo viên giữ nguyên.

`POST /tutor/biology-start` dùng phiên đăng nhập nhật ký hiện có, body `{id: SubmissionID, username}`. Máy chủ tự đọc `BaiNopTuLuan`, xác minh người nộp qua ID/tên tài khoản, trạng thái `Completed`, điểm AI, câu gốc trong `TuLuan` và đúng chương Sinh học trong `Theory`. Điểm, đáp án và nội dung chương từ client không được dùng để quyết định nhiệm vụ.

AI hướng dẫn từng bước bằng luồng tutor hiện có. Sau khi học lại, bài chép lấy nguyên nội dung chương và đáp án tham khảo từ Sheet. `copy-submit` chỉ hoàn thành khi AI xác nhận bài viết tay đủ chương, đề và đáp án; ảnh thiếu phần hoặc không đọc rõ giữ nguyên yêu cầu. Nhiệm vụ có thể mở lại sau khi đóng màn. Nhiệm vụ riêng Sinh học không khóa các trò chơi Hóa/Vật lý.

Không cần sửa Apps Script, đổi URL Sheet, đổi biến môi trường hay triển khai thêm dịch vụ. Cập nhật các file sau vào dịch vụ PlanetServer hiện có rồi deploy:

- `biology-tutor.mjs` (mới)
- `server.mjs`
- `tutor.mjs`
- `copy-penalty.mjs`

Sau deploy, `GET /health` cần trả `biologyTutorVersion: 1` cùng `copyPenaltyVersion: 2`. Unity tự kiểm tra phiên bản trước khi mở bài, giữ nhiệm vụ nếu server chưa sẵn sàng. Không gửi câu trả lời, mật khẩu hoặc ảnh trong health check.

Kiểm tra: `node --test biology-tutor.test.mjs` có 7 phép thử về nguồn Sheet, quyền sở hữu, điểm, AI hướng dẫn, điều kiện chép, API và phạm vi ảnh hưởng. Toàn bộ bộ kiểm thử tại thời điểm sửa: 178/192 đạt; 14 lỗi lịch sử/xếp hạng/Apps Script cũng có trên mã gốc (171/185 đạt), không có lỗi mới ngoài mã gốc.

Unity: menu `Tools > KHTN6 > Sinh hoc > Kiem tra nap cau hoi va hoc lai`, báo cáo ở `outputs/biology/unity-report.json`. Chạy thử bằng dữ liệu kiểm soát và đọc ngân hàng thật; không nộp dữ liệu thử lên Sheet.
