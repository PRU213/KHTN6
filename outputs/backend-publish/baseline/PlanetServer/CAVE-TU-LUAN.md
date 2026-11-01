# Đào hang (Cave) tự luận

Luồng: Trang chủ → Lí hoặc Hóa → chọn chương → xem lý thuyết → Cave → độ khó
và Tự luận → Bắt đầu. Tự luận là luyện tập cá nhân, không có bước ghép trận.

## Chạy

1. Khởi động lại PlanetServer bằng `npm start`.
2. Dừng Play, chờ Unity nhập `Assets/Resources/CaveEssaySkin.png` và biên dịch,
   rồi chạy lại. Không cần gán thêm tham chiếu trong Inspector.
3. Đi theo luồng trên và chọn Tự luận ở game Cave.

Cave dùng chung controller và dịch vụ luyện tập với Football để giữ thống nhất
cách chọn ảnh, chụp webcam, chấm và gợi ý. Mỗi phiên có token, bộ câu, tiến độ,
gợi ý và điểm riêng; dùng profile giao diện đảo bay fantasy từ ảnh người dùng cung cấp.
Màn trắc nghiệm Cave vẫn chạy qua CaveQuizController như trước.

## Cơ chế

- 5 câu đã duyệt từ TuLuan, đúng môn, chương và độ khó đã chọn.
- Không lấy bù từ chương hoặc độ khó khác khi chưa đủ 5 câu.
- Dễ 60 giây, Trung bình 120 giây, Khó 240 giây cho từng câu.
- Nhập tối đa 2.000 ký tự; bộ đếm trên ô nhập khớp giới hạn này.
- Chụp/chọn ảnh mở menu: chọn file Windows, mở webcam, xóa ảnh.
  Có xem trước; ảnh chỉ gửi cho AI khi bấm Nộp bài. Bộ chọn file dùng tiến trình
  riêng đã sửa cho Planet/Football, ảnh nén tối đa 2 MB và cạnh dài 1600 pixel.
- AI dùng DapAnThamKhao, HuongDanChamAI và DiemToiDa của Sheet, quy đổi tối đa
  10 điểm/câu và 50 điểm/lượt. Không đọc được ảnh hoặc lỗi AI thì cho ít nhất
  20 giây để nộp lại, không chốt 0 điểm cho ảnh chưa đọc được.
- AI Gợi ý tối đa hai lần/câu: lần 1 trừ 1 điểm, lần 2 trừ thêm 2 điểm; chỉ tính khi nhận thành công, mở lại không trừ thêm, điểm cuối không dưới 0. Hỏi AI thêm sau nộp miễn phí;
  không cần thêm cột gợi ý trong Sheet. AI được yêu cầu gợi hướng làm thay vì
  đưa đáp số hoặc giải toàn bộ bài.
- Sau mỗi câu: điểm, nhận xét, nội dung AI đọc, đáp án Sheet và bài giải AI.
  Có cuộn để đọc; chỉ một nút Câu tiếp theo. Sau câu 5, nút đổi thành Xem tổng kết,
  rồi Luyện tập lại ở tổng kết. Nút X của giao diện chính dùng để thoát.

Không cần đổi Apps Script nếu API essayQuestions đã hoạt động cho Football/Planet.
Lưu lịch sử dùng cấu hình tùy chọn APPS_SCRIPT_SECRET như trước; ảnh không lưu Drive.

## Kiểm tra

`npm test` bao gồm Cave cho hai môn và ba mức độ khó, ảnh, tổng điểm 5 câu và
tách phiên với Football, cùng các kiểm thử chấm/gợi ý có sẵn. Cần kiểm tra trực tiếp
Play Mode: luồng Lí và Hóa, nhập tiếng Việt, mở/hủy file, webcam, thoát/vào lại,
và chuyển qua lại giữa trắc nghiệm với tự luận.
