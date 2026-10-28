# Robot hỏi đáp Gemini

1. Sao chép `.env.example` thành `.env` trong thư mục này (nếu chưa có).
2. Mở `.env` trên máy, thay `YOUR_GEMINI_API_KEY` bằng khóa Gemini của bạn. Không gửi khóa qua chat, không đưa vào Unity Assets.
3. `GEMINI_MODEL` phải là model Gemini tài khoản của bạn dùng được. Mặc định dùng `gemini-3.5-flash-lite` cho hỏi đáp ngắn. Có thể đổi giá trị này theo Google AI Studio. Nếu model trả lỗi 503 quá tải liên tục, chọn model khác mà tài khoản hỗ trợ rồi khởi động lại server.
4. Dùng Node.js 20.12 trở lên. Trong thư mục `PlanetServer`, chạy `npm start`. Nếu server cũ đang chạy, dừng bằng Ctrl+C rồi chạy lại để đọc cấu hình mới.
5. Unity: mở HomePage, bấm Play, bấm robot màu xanh bên cạnh giáo viên. Gõ câu hỏi rồi Enter hoặc Gửi. Có thể bấm ba câu hỏi gợi ý.

Màn hỏi đáp được tạo khi chạy game bởi `ScienceChatController`, sử dụng nền trong `Assets/Resources/ScienceChatSkin.png`. Không cần tạo scene mới hay thay đổi scene đang chỉnh sửa. Quay lại hoặc X sẽ đóng màn; câu hỏi chưa gửi thành công được giữ để thử lại. Trò chuyện mới xóa lịch sử trong phiên. Không lưu hội thoại xuống đĩa.

Địa chỉ máy chủ: `Assets/Resources/ScienceChatSettings.json`. `localhost` chỉ dành cho game và máy chủ trên cùng máy. Khi dùng điện thoại/máy khác, đổi sang địa chỉ LAN của máy chủ. Khi phát hành ra internet, dùng HTTPS và thêm xác thực người dùng, hạn mức theo tài khoản cùng bảo vệ máy chủ; giới hạn theo IP hiện tại dành cho thử nghiệm nội bộ.

Khóa chỉ nằm trên máy chủ. Nội dung câu hỏi và tối đa 6 lượt trao đổi trước được gửi tới Gemini. Chưa điền khóa thì màn hình vẫn hoạt động nhưng sẽ báo chưa cấu hình, không giả lập câu trả lời AI.

Máy chủ xử lý mỗi câu hỏi trong tối đa 45 giây cho toàn bộ các lần thử; Unity chờ tối đa 60 giây. Đóng màn hỏi đáp hoặc ngắt kết nối sẽ hủy tác vụ trên máy chủ. Lỗi 503 của Gemini được thử lại một lần; lỗi hết hạn mức 429 được báo ngay để tránh gọi dồn.

Mỗi phiên màn hỏi đáp có mã riêng, giới hạn 12 yêu cầu trong 60 giây. Yêu cầu đang xử lý trả lỗi 409; hết lượt trả lỗi 429 kèm số giây cần chờ. Mã phiên giúp các game cùng IP không chặn lẫn nhau, không phải cơ chế xác thực hoặc chống lạm dụng khi phát hành công khai. Game cũ không gửi mã phiên vẫn dùng giới hạn chung theo IP.

Kiểm thử: `npm test`. Test Gemini dùng provider giả lập, không tiêu thụ API và không xác nhận khóa thật.
