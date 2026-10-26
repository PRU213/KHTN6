# Football tự luận

Luồng sử dụng: Trang chủ → Lí hoặc Hóa → chọn chương → xem lý thuyết → Football
→ chọn độ khó và Tự luận → Bắt đầu. Màn Lí được bổ sung lưu `GameSession.Chapter`
ngay khi chọn chương, kể cả khi lý thuyết được lấy từ cache.

## Chạy

1. Khởi động lại `PlanetServer` bằng `npm start`.
2. Unity nhập hai asset mới `FootballEssayController.cs` và `FootballEssaySkin.png`
   tự động. Dừng Play, chờ biên dịch xong rồi chạy lại.
3. Chọn theo luồng trên. Không cần gán thêm script/nút bằng Inspector.

Nguồn dữ liệu là API `action=essayQuestions` trong Apps Script đã dùng cho Planet.
Nếu chưa cập nhật Apps Script, dùng `AppsScript/Code.gs` và hướng dẫn đi kèm.
Nếu đã cập nhật bản đó thì không cần thay Apps Script riêng cho Football.
Tab TuLuan cần ít nhất 5 câu **Đã duyệt** cho đúng môn, chương và nhóm độ khó chọn.
Cột `Chương/Chủ đề` phải có dạng `Chương 1: ...` để khớp chương đang học.
Chương 1 không khớp Chương 10. Khi thiếu câu, game báo số câu đang có;
không lấy bù sang chương hoặc độ khó khác.

## Làm bài và chấm

Luyện tập cá nhân, 5 câu, tối đa 50 điểm. Thời gian từng câu: Dễ 60 giây,
Trung bình 120 giây, Khó 240 giây. Đồng hồ do máy chủ quản lý.
Nhập nhiều dòng (tối đa 2.000 ký tự) hoặc dùng Chụp/Chọn ảnh để mở menu ảnh.
Menu cho chọn ảnh Windows, chụp webcam hoặc xóa ảnh. Ảnh thu nhỏ tối đa 1600 pixel,
nén JPEG và gửi tối đa 2 MB. Chọn file dùng chung tiến trình riêng đã sửa cho Planet.
Ảnh chỉ gửi khi bấm Nộp bài, không tự gửi lúc chọn/xem trước.

Gemini dùng DapAnThamKhao và HuongDanChamAI để chấm, quy đổi DiemToiDa về 10/câu.
Sau mỗi câu, hiện nhận xét, nội dung nhận dạng, đáp án Sheet và bài giải gợi ý AI.
Cuộn để đọc và bấm Câu tiếp theo; sau câu 5 bấm Xem tổng kết, có thể luyện tập lại.
Hết giờ chưa nộp được 0 điểm và vẫn xem lời giải. Nếu AI lỗi hoặc ảnh không đọc được,
không chốt 0 điểm và cho ít nhất 20 giây để nộp lại. Nộp trùng không cộng điểm lần hai.

## AI gợi ý

Không cần thêm cột gợi ý vào Sheet. Máy chủ dùng câu hỏi, đáp án và rubric để yêu cầu
Gemini gợi ý hướng làm, không đưa đáp số hoặc giải trọn bài. Mức 1 nhắc kiến thức;
mức 2 gợi bước tiếp theo. Tối đa hai gợi ý mỗi câu: lần 1 trừ 1 điểm,
lần 2 trừ thêm 2 điểm (tổng 3). Chỉ tính gợi ý nhận thành công; xem lại
hoặc yêu cầu lỗi không trừ thêm. Điểm nhận = max(0, điểm AI chấm - mức trừ).
Hỏi AI thêm sau khi nộp không trừ điểm.
Gợi ý được cache trong phiên; mở lại không gọi AI lần nữa. Gợi ý lỗi có thể thử lại
và không bị mất lượt. Gợi ý có thể sai hoặc vô tình quá cụ thể, cần rà soát khi dùng
cho hoạt động đánh giá chính thức. Bài làm và số gợi ý sử dụng được giữ trong phiên.

## Kỹ thuật và kiểm tra

Các API POST riêng `/football/start`, `/state`, `/hint`, `/submit`, `/next`, `/leave`.
Token, câu hỏi, đáp án, thời gian và điểm do máy chủ quản lý. Client không gửi điểm
hay đáp án chuẩn để chấm. Phiên không hoạt động quá 30 phút được dọn khi có truy cập.
Lưu BaiNopTuLuan dùng cấu hình APPS_SCRIPT_SECRET tùy chọn như Planet; lỗi ghi lịch sử
không làm mất kết quả trong phiên. Ảnh không được lưu trên Drive.

`npm test` bao gồm lọc môn/chương/độ khó, giới hạn gợi ý, cache, timeout,
nộp ảnh, chống nộp trùng, bài hết giờ và toàn bộ API luyện tập.
Vẫn cần kiểm tra trực tiếp Play Mode ở luồng Lí và Hóa, chọn file, webcam,
thoát/vào lại và chuyển qua lại giữa trắc nghiệm với tự luận.
