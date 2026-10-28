# Cài phần tự luận Planet

## 1. Cập nhật Apps Script (bắt buộc)

`Code.gs` trong thư mục này là bản hoàn chỉnh, được ghép từ đúng mã bạn cung cấp,
giữ các API Questions, Theory, bảng xếp hạng, tiến độ và lịch sử hiện có.

1. Mở Google Sheet → **Tiện ích mở rộng → Apps Script**.
2. Sao lưu mã cũ, rồi thay toàn bộ nội dung file mã hiện tại bằng `AppsScript/Code.gs`.
   Nếu có nhiều file `.gs`, không dán thêm bản thứ hai trùng các hàm doGet/doPost.
3. Bấm **Lưu**.
4. Chọn **Triển khai → Quản lý các bản triển khai → biểu tượng bút chì**.
5. Ở Phiên bản, chọn **Phiên bản mới**, rồi **Triển khai**. Cấp quyền nếu Google yêu cầu.
   Cập nhật bản triển khai đang dùng sẽ giữ địa chỉ `/exec` hiện tại. Nếu tạo bản triển khai
   mới có URL khác, cập nhật `APPS_SCRIPT_URL` trong `PlanetServer/.env`.
6. Giữ cấu hình thực thi và quyền truy cập phù hợp với web app hiện tại để máy chủ có thể đọc API.

Các cột bắt buộc ở tab **TuLuan**:

`CauHoiID`, `monID`, `difficulty`, `Question`, `DapAnThamKhao`, `HuongDanChamAI`, `DiemToiDa`, `TrangThai`.

Mã đọc cột theo tên, không phụ thuộc thứ tự. Chỉ dòng có `TrangThai` là **Đã duyệt**
được dùng. Môn: Vật lý/Vật lí, Hóa học, Sinh học. Độ khó: Dễ, Trung bình, Khó.
Mỗi môn cần ít nhất 3 câu với nhóm độ khó đang chọn. ID phải duy nhất, đáp án và
hướng dẫn chấm không được để trống. `HinhAnh` để trống hoặc `none` cho bản đề chữ
này; phần ảnh mới là **ảnh bài làm của người chơi**, không phải ảnh minh họa đề bài.

Kiểm tra bằng cách thêm `?action=essayQuestions` vào URL `/exec` (khi chưa bật secret).
Phản hồi đúng chứa `"success":true`, `"questionType":"essay"` và mảng `questions`.
Nếu chỉ Lưu mà chưa triển khai phiên bản mới, game vẫn dùng mã cũ và sẽ báo cần cập nhật Apps Script.

## 2. Khởi động game

1. Khởi động lại máy chủ `PlanetServer` bằng `npm start`.
2. Dừng Play trong Unity, chờ biên dịch xong và chạy Play lại.
3. Chọn Planet → độ khó → Tự luận → Bắt đầu.

Mỗi trận tự luận có 9 câu, 3 câu mỗi môn. Mỗi câu có thời gian theo độ khó:
Dễ 60 giây, Trung bình 120 giây, Khó 240 giây. Khi chọn nhiều độ khó, thời gian
được lấy từ chính câu đang chơi. Trắc nghiệm vẫn 15 câu như trước.

Nhập chữ, chọn ảnh JPG/PNG hoặc chụp bằng webcam trên Windows. Có thể dùng cả
chữ lẫn ảnh trong một bài. Ảnh được xem trước, đổi/xóa trước khi nộp; game thu nhỏ
ảnh tối đa 1600 pixel theo cạnh dài và nén JPEG, giới hạn gửi 2 MB.
Hộp chọn file mở trong tiến trình Windows riêng, bắt đầu ở thư mục Ảnh hoặc thư mục
đã chọn lần trước. Giải mã và thu nhỏ ảnh cũng chạy trong tiến trình riêng trước
khi đưa ảnh vào Unity. Có thể hủy chọn; hộp thoại tự đóng khi hết giờ hoặc rời trận.
Game vẫn giữ kết nối và tính thời gian. Đồng hồ
không dừng khi chọn ảnh hoặc chụp. Chỉ mở webcam khi bấm Chụp ảnh; camera dừng
khi đóng, hết giờ hoặc rời màn. Máy không có camera có thể dùng Chọn ảnh.

AI nhận câu hỏi, đáp án, hướng dẫn chấm từ máy chủ và bài làm. Không đọc được ảnh
thì cho chụp/nộp lại khi còn giờ, không chốt 0 điểm. Điểm được quy đổi về 10/câu
từ DiemToiDa của Sheet, tối đa 90/trận. Sau mỗi câu, xem kết quả 30 giây,
cuộn để đọc điểm, nhận xét, nội dung AI nhận dạng, đáp án Sheet và bài giải gợi ý AI.
Bài nộp đúng giờ có tối đa 25 giây chờ chấm; Gemini có timeout 20 giây.

## 3. Lưu lịch sử vào BaiNopTuLuan (tùy chọn)

Chấm bằng ảnh hoạt động **không cần lưu ảnh lên Google Drive**.
Ảnh được gửi từ game qua máy chủ tới Gemini, không lưu ảnh vào Sheet hoặc đĩa.
API `saveEssayImage` cũ chưa có phần triển khai được thay bằng thông báo giải thích,
game không gọi API đó.

Muốn lưu bài nhập chữ, nội dung AI đọc, điểm và nhận xét:

1. Trong Apps Script, mở **Cài đặt dự án → Thuộc tính tập lệnh**, thêm
   `PLANET_SERVER_SECRET` với một chuỗi bí mật tự chọn, đủ dài.
2. Mở `PlanetServer/.env`, thêm `APPS_SCRIPT_SECRET=` với đúng cùng chuỗi đó.
   Không đưa giá trị này vào Unity Assets hoặc gửi qua chat.
3. Chạy hàm **setupPlanetEssay** một lần trong Apps Script để tạo tab/header.
   Nếu tab BaiNopTuLuan đang trống, chương trình sẽ đặt các cột tự động.
   Nếu tab có cấu trúc khác, hãy đổi tên tab cũ để giữ dữ liệu; hàm không xóa hay
   ghi đè cấu trúc đang có.
4. Khởi động lại máy chủ. Nếu bật secret, API đọc đề cũng yêu cầu secret;
   máy chủ tự đính kèm. Không mở link chứa secret rồi chia sẻ cho người khác.

Các cột được tạo:
SubmissionID, MatchID, Username, CauHoiID, monID, difficulty, BaiLamText, CoAnh,
AINhanDang, Diem, DiemToiDa, NhanXetAI, DapAnAI, NgayNop.

Không có secret thì vẫn chơi/chấm được (khi API đọc đề chưa bật secret), chỉ không
lưu lịch sử. Lỗi lưu Sheet không làm mất điểm của trận đang chơi.

## Kiểm tra

`npm test` kiểm tra 9 câu, phân bố môn, thời gian, lọc dữ liệu đã duyệt, hợp đồng
API mới, ảnh, chấm, nộp trùng và không làm lộ đáp án sớm. Sau khi triển khai Apps
Script, cần thử trực tiếp trong Unity với hai client, hộp chọn ảnh và webcam thật.

Tài liệu kỹ thuật: [Unity WebCamTexture](https://docs.unity.com/en-us/engine/6000.5/script-reference/unityengine/webcamtexture),
[Windows file dialog](https://learn.microsoft.com/en-us/windows/win32/api/commdlg/ns-commdlg-openfilenamew),
[Gemini GenerateContent](https://ai.google.dev/api/generate-content).
