# Kiểm tra đăng nhập, trang chủ, làm bài, lịch sử và xếp hạng — 27/10/2026

## Phạm vi và giới hạn

Tài khoản kiểm tra là `student01`, lấy từ dữ liệu phiên Unity đã lưu của chính dự án. Chỉ đọc các khóa Username, Fullname, Role và biệt danh của tài khoản này; không thay Registry. Không sửa mã ứng dụng, scene, prefab hoặc dữ liệu Google Sheets. Không gửi mật khẩu, không tạo bài làm/điểm/lịch sử giả.

Công cụ điều khiển giao diện không khởi tạo được, trả `failed to write kernel assets: The system cannot find the path specified. (os error 3)`. Vì vậy chưa thực hiện một lần đăng nhập mới hoặc chơi bài thi hoàn chỉnh bằng UI. Kết quả dưới đây là kiểm tra dữ liệu thật, phiên đã lưu và cấu hình/mã liên kết giao diện; không tuyên bố đã bấm toàn bộ luồng trong Unity.

## Kết quả chính

| Hạng mục | Kết quả |
|---|---|
| Nguồn dữ liệu đăng nhập | HTTP 200; tìm đúng một tài khoản student01, họ tên Nguyễn Văn An, vai trò Student. Cách tách CSV hiện tại đọc đúng họ tên và vai trò của tài khoản này. Không kiểm thử mật khẩu hoặc đăng nhập lại. |
| Phiên đã lưu | Username=student01, Fullname=Nguyễn Văn An, Role=Student. |
| Tên trang chủ dự kiến | `Nguyen  Van An`, do có biệt danh riêng đã lưu cho student01. DisplayName ưu tiên biệt danh trước họ tên. |
| Ô tên/vai trò Homepage | Hai đối tượng txtFullname và txtRole là UI.Text, đang active dưới HomePage; phù hợp với cơ chế tự tìm của HomeMenuController. Giá trị trong Edit Mode còn New Text là giá trị thiết kế, không chứng minh lỗi runtime. Chưa xác nhận render trong Play. |
| Vào bài trắc nghiệm | API Vật lý/Dễ trả 82 câu, trong đó chương 1 có 18; Hóa học/Dễ trả 85 câu, chương 1 có 17. Chưa chơi UI đến kết quả. |
| Lịch sử student01 | Tải thành công 27 dòng: 11 thắng, 16 thua, tỉ lệ thắng 41%; 22 luyện tập và 5 đối kháng. Tổng/thống kê khớp dữ liệu trả về. |
| Xếp hạng tuần | Không tải được. Hai lần gọi API đều success=false với ReferenceError: planetGetProgressRank_ is not defined. HTTP 200 không đồng nghĩa thành công nghiệp vụ. |
| Test logic lịch sử/xếp hạng hiện có | 17/17 test đạt, không bỏ qua. Đây là kiểm tra mã cục bộ; API triển khai thực tế vẫn lỗi như trên. |

## Lỗi 1: API xếp hạng triển khai thiếu hàm

- Bước tương ứng: đăng nhập học sinh → Homepage → Xếp hạng.
- Yêu cầu mà RankManager sử dụng: `action=getProgressRank` tại AppsScriptSettings.json.
- Phản hồi thật: `success:false`, `message:"ReferenceError: planetGetProgressRank_ is not defined"`.
- Tái hiện hai lần độc lập.
- Hệ quả theo mã client: ClearRank được gọi; không có danh sách xếp hạng để hiển thị, nhãn trạng thái hiện thông báo API.
- Mã trong AppsScript/Code.gs hiện có hàm planetGetProgressRank_ ở dòng 861. Cần đối chiếu và triển khai đúng phiên bản Apps Script; chưa thực hiện cập nhật triển khai.
- Bằng chứng: api-results.json, case-results.json.

## Lỗi 2: Đăng nhập bằng chữ hoa có thể mất lịch sử

- GoogleSheetAPI chấp nhận username không phân biệt hoa/thường và ban đầu lưu tên chuẩn từ nguồn dữ liệu.
- Sau đăng nhập, LoginController.OnLoginResponse lại ghi đè Username bằng nguyên văn nội dung người dùng nhập.
- API lịch sử so khớp username có phân biệt hoa/thường.
- Đối chiếu API thật: `student01` trả 27 lượt và tên Nguyễn Văn An; `STUDENT01` trả 0 lượt và tên STUDENT01.
- Chuỗi gây lỗi được xác định từ mã và phản hồi API; chưa thực hiện đăng nhập UI với chữ hoa.
- Tác động: cùng tài khoản hợp lệ có thể thấy lịch sử trống nếu nhập tên đăng nhập bằng chữ hoa. Các tác vụ lưu kết quả cũng cần đối chiếu username chuẩn.
- Vị trí: Assets/Scripts/GoogleSheetAPI.cs dòng 44–52; Assets/Scripts/LoginController.cs dòng 136–151; AppsScript/Code.gs dòng 347 và 372.
- Bằng chứng: case-results.json.

## Dữ liệu lịch sử còn thiếu

18 trong 22 dòng luyện tập hiện có không có trường chương. Client có thể hiển thị dấu gạch hoặc chỉ tên môn ở cột chương, tùy dữ liệu môn có sẵn. Đây là thiếu metadata của các dòng đang lưu, không phải API không tải được lịch sử.

Lịch sử hiện có trả thời gian từ 07/10/2026 đến 24/10/2026. Chưa xác nhận việc hoàn thành một bài mới rồi lưu và cập nhật lịch sử/xếp hạng trên môi trường thật vì chưa thao tác được UI và API xếp hạng đang lỗi.

## Liên kết scene đã rà

- Scene đầu tiên trong Build Settings là SampleScene, sử dụng Assets/Scenes/Login.prefab.
- LoginController đã gán usernameInput, passwordInput, api và errorText; nút đăng nhập có callback OnLoginClicked.
- Đăng nhập Student thành công điều hướng tới scene HomePage; source profile lưu Fullname, Role vào GameSession và PlayerPrefs.
- HomeMenuController trên HomePage tìm UI.Text tên txtFullname/txtRole rồi gán DisplayName và Role.
- HomeHistoryController đã tham chiếu HomePage, History và Rank. Nút lịch sử/xếp hạng gọi đúng phương thức mở panel.
- HistoryManager dùng AppsScriptEndpoint.Url và username phiên hiện tại, không dựa vào baseUrl cũ còn nằm trong prefab.
- RankManager cũng dùng AppsScriptEndpoint.Url, đòi hỏi phản hồi weekly-v2, và xóa dữ liệu bảng khi API báo lỗi.

## Tệp bằng chứng

- api-results.json: tóm tắt dữ liệu thật, không chứa mật khẩu hoặc toàn bộ lịch sử người dùng khác.
- case-results.json: so sánh chữ hoa/thường và thử lại xếp hạng.
- questions-results.json: API câu hỏi Vật lý/Hóa học.
- profile-bindings.json: đường dẫn và loại thành phần UI trên Homepage.
- login-scene-results.json, account-scene-results.json: tham chiếu scene/prefab.
- history-rank-tests.log: kết quả 17 test cục bộ.
