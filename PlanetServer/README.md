# Planet đối kháng

## Chạy thử

**Mở đúng dự án Unity:** `D:\Downloadss\KHTN6-main\KHTN6-main`.
Thư mục bên ngoài cũng có ProjectSettings nhưng không chứa các màn game.

1. Cài Node.js 20 trở lên. Máy phát triển hiện đã có Node.js.
2. Mở terminal tại thư mục `PlanetServer`, chạy `npm start` (không cần npm install).
3. Mở scene `Assets/Scenes/HomePage.unity`, bấm Play.
4. Bấm **Đối kháng**, chọn độ khó ở màn cài đặt rồi bấm bắt đầu. Không chọn môn.
   Planet hiện chỉ bật **Trắc nghiệm**; Tự luận được vô hiệu hóa.
   Hoặc mở menu game từ môn học và chọn hình Planet (game3).
5. Một phiên chờ được tính từ lúc máy chủ nhận yêu cầu. Máy chủ tìm người
   để đấu bộ đề tổng hợp 3 môn. Đủ 120 giây chưa ghép được sẽ tạo Máy Planet,
   đếm ngược 4 giây, rồi bắt đầu trận. Hai người chỉ cần dùng **A**, **Space**
   hoặc nút **BẮN** trong phiên bản cũ. Hiện tại người chơi **bấm trực tiếp
   vào hành tinh chứa đáp án** để chọn ngay, không cần canh bắn.
6. Cuối trận có kết quả và nút tìm trận mới / về menu. Rời trận đang chơi
   tính là bỏ cuộc. Nếu không kết nối được máy chủ, màn lỗi cho phép chủ động
   chọn chơi với máy ngoại tuyến ngay.

Mỗi trận trộn đúng 15 câu trắc nghiệm: 5 Physics, 5 Chemistry, 5 Biology,
không phụ thuộc môn đang chọn ở các game khác. Câu hỏi được lọc theo độ khó
đã chọn; ghép cặp với người chọn cùng tập độ khó. Hết câu 15 mới tổng kết.
Mỗi câu có 20 giây. Đúng: 10 + số giây còn lại (làm tròn); mỗi lần bắn trúng
đáp án sai: trừ 10 điểm, điểm có thể âm, và khóa bắn 1,5 giây. Hết giờ không
trừ điểm. Máy chờ 3–5 giây đầu mỗi câu rồi kiểm tra hướng bắn mỗi 1–2 giây.
Các hành tinh bay tự do, đổi hướng ngẫu nhiên, bật lại ở biên và khi chạm nhau.
Máy chủ đồng bộ vị trí cho cả hai người; chơi máy ngoại tuyến cũng có chuyển động.

## Hai thiết bị cùng mạng LAN

- Cho máy chạy server và hai thiết bị vào cùng mạng.
- Trong `Assets/Resources/PlanetDuelSettings.json`, đổi `serverUrl` thành
  `http://DIA_CHI_IP_MAY_CHAY_SERVER:8787`, rồi build lại client.
- Cả Editor lẫn bản build phải dùng cùng địa chỉ server. `localhost` trên
  điện thoại/máy khác trỏ về chính thiết bị đó.
- Cho phép kết nối vào cổng 8787 trên máy chạy server nếu hệ điều hành hỏi.
- Kiểm tra `http://DIA_CHI_IP_MAY_CHAY_SERVER:8787/health`: phải trả về `{"ok":true}`.

## Hai thiết bị qua Internet

Chạy thư mục `PlanetServer` trên máy chủ Node.js có địa chỉ công khai, một process.
Lệnh chạy `node server.mjs`, port lấy từ biến `PORT`, mặc định 8787.
Đặt reverse proxy HTTPS trước dịch vụ rồi cấu hình `serverUrl` bằng địa chỉ
HTTPS đó trên cả hai client. Không cần tài khoản dịch vụ Unity hay khóa API.
Đối với WebGL phục vụ qua HTTPS, server cũng phải dùng HTTPS.

**Mã đã được chuẩn bị để chạy; chưa triển khai lên dịch vụ hosting công khai.**
Hàng chờ và trận đang chạy nằm trong RAM; khởi động lại server sẽ kết thúc
các phiên. Không chạy nhiều bản server độc lập sau load balancer, vì các bản
đó không chia sẻ hàng chờ. Phiên dùng token ngẫu nhiên, chưa liên kết xác thực
tài khoản đăng nhập của dự án. Kết quả chưa ghi vào bảng xếp hạng/lịch sử hiện có.

## Nội dung và giao diện

- `questions.json`: ngân hàng câu hỏi khởi đầu cho Physics/Chemistry/Biology,
  có nhãn độ khó. Mỗi trận lấy ngẫu nhiên 5 câu mỗi môn rồi trộn 15 câu.
  Mỗi môn phải có ít nhất 5 câu khớp độ khó, nếu thiếu hệ thống báo lỗi.
- `Assets/Resources/PlanetDuelQuestions.json`: cùng ngân hàng, dùng khi chơi
  máy ngoại tuyến. Sau khi đổi câu hỏi server, chạy
  `node sync-questions.mjs` để cập nhật bản ngoại tuyến trước khi build Unity.
- Chưa tự đọc Google Sheet: sửa `questions.json` để đưa bộ câu chính thức vào.
  Câu hỏi có cấu trúc `subject`, `difficulty`, `text`, `options` (đúng 4 phần tử),
  `correct` (chỉ số 0–3). Không đưa endpoint chứa thông tin riêng vào client.
- `PlanetDuelController` tạo các panel khi Planet được mở. Các panel dùng font,
  material chữ, sprite tàu và khung của chính prefab Planet; không cần kéo
  từng tham chiếu UI mới bằng tay trong Inspector.
- `BlastRoundManager.enableOnlineDuel`: mặc định bật. Tắt nếu muốn quay lại
  thử chế độ hai phím A/L cũ trong scene thử nghiệm.

## Đồng bộ và kiểm tra

Football tự luận (luyện tập cá nhân 5 câu đúng môn/chương/độ khó, chấm ảnh và AI
gợi ý hai mức) dùng chung máy chủ này. Xem [hướng dẫn Football tự luận](FOOTBALL-TU-LUAN.md).
Sheep tự luận dùng cùng cơ chế, có giao diện rừng đêm và phiên riêng.
Xem [hướng dẫn Sheep tự luận](SHEEP-TU-LUAN.md).

### Tự luận Planet

Trong màn chờ ghép đối thủ, bấm **Chơi với máy ngay** để bỏ qua thời gian chờ,
áp dụng cho cả trắc nghiệm và tự luận. Máy chủ chuyển vé đang tìm sang trận với
Máy Planet, giữ hình thức và độ khó đã chọn. Nếu vẫn tiếp tục chờ, mốc tự ghép máy
120 giây giữ nguyên. Nếu vừa ghép được người thật trước khi yêu cầu đến máy chủ,
trận người thật được giữ để không bỏ rơi đối thủ hoặc tạo trùng trận.
Unity gửi yêu cầu riêng `/bot` và kiểm tra trạng thái trận trả về, tránh kẹt nút
"Đang chuyển" nếu máy chủ cũ bỏ qua tham số polling. Khi chuyển thất bại,
trắc nghiệm dùng máy cục bộ; tự luận báo lỗi kết nối/cập nhật server vì cần AI chấm.

Chọn Planet, chọn độ khó rồi chọn **Tự luận** tại màn hình chọn hình thức.
Trắc nghiệm và tự luận được ghép vào hai hàng chờ riêng. Tự luận gồm 9 câu
(3 câu mỗi môn); Dễ 60 giây, Trung bình 120 giây, Khó 240 giây theo từng câu.
Nhập tối đa 2.000 ký tự hoặc đính kèm ảnh JPG/PNG, bấm **Nộp bài**;
Enter dùng để xuống dòng. Mỗi người được nộp một bài thành công cho mỗi câu.

Máy chủ gửi câu hỏi, đáp án tham khảo và bài làm tới Gemini để chấm 0–10 điểm,
có điểm từng phần và nhận xét tiếng Việt. Khóa và model dùng chung cấu hình
`.env` với robot hỏi đáp. Đáp án tham khảo chỉ được gửi về game sau khi kết thúc
câu hỏi. Khi cả hai bài được chấm hoặc hết giờ, game hiện kết quả trong 30 giây.
Nếu bài nộp đúng giờ đang được chấm, máy chủ chờ thêm tối đa 25 giây.
AI lỗi thì bài chưa được cộng điểm; người chơi có thể nộp lại khi còn thời gian.

Máy chủ lấy câu đã duyệt từ tab `TuLuan` qua `action=essayQuestions` của Apps Script,
dùng `DapAnThamKhao`, `HuongDanChamAI` và `DiemToiDa` để chấm rồi quy đổi về 10/câu.
`essay-questions.json` chỉ còn làm dữ liệu kiểm thử, không thay thế Sheet khi chơi thật.
Máy chủ lưu bộ câu đã tải thành công và kiểm tra hợp lệ trong `.cache/essay/`.
Khi bản lưu quá 60 giây, lượt chơi dùng ngay bản gần nhất và cập nhật Sheet ở nền;
khởi động lại server vẫn đọc được bản lưu. Không dùng bản quá 24 giờ hoặc bản của
URL/secret khác. Lỗi mạng tạm thời giữ bộ câu cũ, nhưng lỗi quyền truy cập, API sai
hay dữ liệu không hợp lệ sẽ vô hiệu hóa bản lưu và báo lỗi. Lần đầu chưa có bản lưu
vẫn cần tải Sheet thành công; không tự thay bằng câu mẫu. `/health` có trạng thái
`essayQuestions.ready/count/savedAt/refreshing/stale` để kiểm tra mà không lộ đáp án.
Ảnh bài làm gửi thẳng tới Gemini, không cần lưu Drive. Có chọn ảnh từ Windows,
xem trước/xóa ảnh và chụp webcam; ảnh không đọc được sẽ cho nộp lại khi còn giờ.
Kết quả gồm nhận xét, nội dung nhận dạng và đáp án; có thể cuộn để đọc.
Xem [hướng dẫn triển khai Apps Script](../AppsScript/HUONG-DAN.md) trước khi chạy.
Chấm AI có thể khác nhau giữa các lượt; đây là chấm trong game, cần rà soát
nội dung trước khi dùng để đánh giá chính thức.

Chờ 120 giây không có đối thủ vẫn ghép với Máy Planet. Điểm đối thủ máy được
mô phỏng (4–10 điểm/câu), bài của người chơi được AI chấm thật. Tự luận cần server
và Gemini hoạt động; không dùng chấm điểm giả khi mất mạng. `npm test` bao gồm
kiểm tra nộp trùng, nộp muộn, lỗi AI, không lộ đáp án sớm và trận tự luận đủ 9 câu.

Server quyết định câu hỏi, thứ tự đáp án, vị trí mục tiêu, hướng tàu, đạn,
va chạm, khóa bắn, thời gian và điểm. Client chỉ gửi ý định bắn. Server mô phỏng
mỗi 25 ms, client lấy trạng thái khoảng 10 lần/giây tùy độ trễ. Đây là giao thức
HTTP polling đơn giản; mạng chậm có thể làm chuyển động giật. Không phải giải
pháp thi đấu xếp hạng đã triển khai ở quy mô lớn.

Chạy `npm test` để kiểm tra ghép hai thiết bị, đủ 5 câu mỗi môn, mốc 120 giây,
không ghép người thật sau khi đã tạo bot, hủy tìm, hết phiên, bỏ cuộc,
khóa bắn, va chạm/tính điểm, hết lượt và các API qua kết nối HTTP thực.

Trước khi phát hành: kiểm tra Play Mode ở hai client với cùng server, thử
đủ 120 giây, rời trận, tắt mạng, bấm tìm trận lại, và kiểm tra UI ở tỷ lệ màn
hình đích. Biên dịch C# và kiểm thử server không thay thế các bước Play Mode này.

Server dùng HTTP tích hợp sẵn của Node.js:
https://nodejs.org/api/http.html
