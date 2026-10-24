# Lịch sử và tổng điểm xếp hạng tuần

Unity và PlanetServer đã sửa. Apps Script online cần cập nhật một lần để nhận
`savePlanetMatch` và đưa điểm đối kháng vào `getProgressRank`.

## Nếu lịch sử đã lưu được (cập nhật bảng tuần)

Chỉ thay nội dung file **PlanetMatchHistory.gs** đang có trên Apps Script bằng
bản mới cùng tên trong thư mục này. Không tạo file thứ hai, không dán thêm Code.gs.
Các nhánh doGet/doPost đã sửa giữ nguyên. Lưu và cập nhật bản triển khai đang dùng
sang **Phiên bản mới**, rồi khởi động lại PlanetServer và Unity Play.

## Nếu cài lần đầu trong dự án Apps Script hiện tại

1. Thêm file Script mới tên **PlanetMatchHistory**, dán toàn bộ nội dung
   `PlanetMatchHistory.gs` nằm cùng thư mục hướng dẫn này.
2. Trong **Mã.gs**, tìm `doPost(e)`. Sau dòng `var action = p.action || "";`, thêm:

   ```javascript
   if (action === "savePlanetMatch") return planetSaveMatch_(p);
   ```

3. Trong **Mã.gs**, tìm nhánh `getProgressRank` trong `doGet(e)` và đổi thành:

   ```javascript
   if (action === "getProgressRank") {
     return planetGetProgressRank_();
   }
   ```

4. Lưu. **Triển khai → Quản lý các bản triển khai → chọn đúng URL đang dùng →
   bút chì → Phiên bản mới → Triển khai**. Giữ quyền truy cập ứng dụng web hiện tại.
5. Khởi động lại PlanetServer (`npm start`), dừng Play Unity rồi chạy lại.

Không dán thêm cả Code.gs: bản Code.gs trong thư mục cũng đã chứa các cập nhật
này, nên dán cả hai sẽ tạo hàm trùng. Các file DriveUpload, EssaySubmission và
EssayAutoGrading hiện có không cần thay đổi cho tính năng lịch sử trận đấu này.

URL hiện tại:
`https://script.google.com/macros/s/AKfycbxLHvShuipgPg7x28IikDmzU7wHoE2e3ai9rhuoEnN50UsFUAWIPTG_nneHOxz-YmaY/exec`

Nếu đổi URL: cập nhật `Assets/Resources/AppsScriptSettings.json` (Unity) và
`PlanetServer/.env` (APPS_SCRIPT_URL). Các giá trị URL cũ gắn trong Inspector
của HistoryManager/RankManager được thay bằng cấu hình chung này khi chạy.

## Dữ liệu và cách tính

- Mỗi người chơi lưu một dòng vào History khi nhận kết quả kết thúc trận.
  Chế độ là **Đối kháng**, đối thủ là Máy Planet hoặc tên người đấu cùng.
- Giữ A:F như lịch sử cũ. Chỉ thêm G:H có tên **PlanetMatchID**, **PlanetRankScore**.
  Nếu G:H đang có dữ liệu khác, API từ chối và giữ nguyên dữ liệu đó.
- Trận hòa ghi **HÒA**; tổng trận bao gồm hòa. Hai bộ đếm Thắng/Thua vẫn chỉ đếm
  đúng các kết quả tương ứng như tên trên giao diện.
- Điểm lịch sử là điểm thực của trận, có thể âm do trả lời sai.
- Tuần xếp hạng: **00:00 thứ Hai đến trước 00:00 thứ Hai kế tiếp**, múi giờ
  **Asia/Ho_Chi_Minh (UTC+7)**. Sang tuần mới tự tính lại từ các kết quả tuần mới,
  không xóa History và không cần trigger chạy xóa dữ liệu.
- Điểm xếp hạng là **tổng điểm thực** của mọi lượt Luyện tập và Đối kháng đã lưu
  trong tuần. Không lấy kỷ lục, không quy đổi về 100. Điểm âm do luật trận đấu
  vẫn được cộng theo dấu. Ví dụ 30 luyện tập + 337 đối kháng = 367 điểm.
- History là sổ kết quả duy nhất để tránh cộng một bài hai lần từ TienDoHocTap.
  Cột PlanetRankScore cũ được giữ để tương thích nhưng không dùng tính bảng tuần.
  Mã UserID và Username được đối chiếu qua Users để một người không bị tách dòng.
- Chỉ hiện người có lượt hoàn thành trong tuần; đầu tuần chưa có lượt thì bảng
  trống. Hàng không có ngày hợp lệ không thể xác định tuần nên bị bỏ qua;
  API báo số hàng này qua ignoredUndatedRows để kiểm tra, không tự đoán ngày.
- Ghi theo thời gian hoàn thành. Bài gửi lại do mất mạng vẫn thuộc tuần hoàn thành,
  không chuyển sang tuần tải lên. Hàng cũ không có thời điểm gốc dùng ngày đã lưu.
- Trắc nghiệm Football/Sheep/Cave, Fly và tự luận Football/Sheep/Cave được nối vào
  hàng đợi lưu kết quả chung; tự luận lưu một lượt khi câu thứ năm đã có kết quả,
  không cộng lại các bản ghi chấm từng câu trong BaiNopTuLuan.
- Mã trận + tài khoản chống ghi trùng khi gửi lại. Hàng đợi lưu bằng PlayerPrefs,
  chạy trên đối tượng DontDestroyOnLoad; rời màn hoặc mở game lại không mất bài
  chưa đồng bộ. Khi lỗi sẽ thử lại sau 30 giây, không báo lưu thành công giả.
- Khi đồng bộ xong, màn lịch sử và xếp hạng đang mở tự tải lại.
- Trận đã kết thúc trước bản sửa không có dữ liệu lưu nên không thể tự khôi phục.
  Không tạo trận hoặc điểm giả để bù lịch sử.

## Kiểm tra

### Khi cả lịch sử và xếp hạng đều trống

Nếu URL `/exec?action=history&username=...` hoặc `/exec?action=getProgressRank`
chuyển sang `accounts.google.com` và trả `text/html`, game đang nhận trang đăng
nhập Google thay vì JSON. HTTP 200 trong trường hợp này không có nghĩa API thành công.

Trong tài khoản sở hữu Apps Script: **Triển khai → Quản lý các bản triển khai →
chọn bản có URL trùng AppsScriptSettings.json → bút chì**. Kiểm tra **Thực thi với
tư cách: Tôi** và **Ai có quyền truy cập: Bất kỳ ai** (không phải chỉ người có tài
khoản Google). Cập nhật bản triển khai hiện tại để giữ URL. Đây là cấu hình truy cập
ứng dụng web; không cần chia sẻ công khai chính file Google Sheet.

Kiểm tra lại URL trong cửa sổ ẩn danh: phải trả JSON, không yêu cầu đăng nhập.
Chỉ khi phản hồi là JSON mới kiểm tra tiếp `success`, `message` và dữ liệu.
Nếu URL đổi, cập nhật cấu hình Unity và PlanetServer như hướng dẫn bên trên.
Unity hiện phân biệt trang đăng nhập, lỗi API, đang tải và dữ liệu thực sự trống.

Mở `URL/exec?action=getProgressRank`: bản mới có `rankVersion: "weekly-v2"`.
Chơi xong một trận có đăng nhập: màn kết quả phải báo **ĐÃ LƯU LỊCH SỬ**.
Mở lịch sử: có dòng Đối kháng mới. Bảng xếp hạng hiện tổng điểm thực trong tuần và nhãn khoảng ngày.
Màn mở tự tải lại tối đa mỗi 60 giây và tại mốc đổi tuần.
Thử trận hòa, ngắt mạng rồi kết nối lại, rời màn trong lúc đang lưu, và bấm lại
Làm mới: không xuất hiện dòng trùng.

Kiểm tra tự động: C# build và các bài `PlanetServer/match-history.test.mjs`,
gồm ghi lịch sử, chống trùng hai người, hòa, điểm âm, bảo vệ cột cũ và xếp hạng.
Chưa thay đổi dữ liệu trên Google Sheet trực tiếp từ máy phát triển.
