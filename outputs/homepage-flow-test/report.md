# Kiểm thử Vật lý và Hóa học từ Homepage — 27/10/2026

## Kết luận

Phát hiện 2 vấn đề ở game tự luận. Chưa thể xác nhận kiểm thử giao diện đầu-cuối bằng thao tác trong Unity: cả công cụ Windows và công cụ giao diện dự phòng đều lỗi khởi tạo `failed to write kernel assets: The system cannot find the path specified. (os error 3)` trước khi điều khiển được cửa sổ.

Không sửa mã ứng dụng, scene, prefab hoặc dữ liệu Sheet. Chỉ tạo tệp phục vụ kiểm thử trong thư mục báo cáo. Các phiên tự luận thử chạy riêng không nộp bài, không gọi AI chấm điểm và không ghi lịch sử.

## Lỗi 1 — Server đang chạy không mở được game tự luận

- Mức độ: cao, chặn mở bài.
- Luồng tương ứng: Homepage → Vật lý/Hóa học → chương 1 → Lí thuyết → chọn Football/Sheep/Cave → Tự luận → Dễ → Bắt đầu.
- Phương pháp tái hiện: gửi đúng yêu cầu bắt đầu phiên mà client dùng đến `http://localhost:8787/football/start`; chưa bấm trực tiếp giao diện.
- Kết quả: 6/6 trường hợp (2 môn × 3 game) trả HTTP 400, không tạo được phiên.
- Thông báo: `Câu TL_BIO_01_E07 có ảnh đề. Bản hiện tại cần câu đề bằng chữ; hãy bỏ ảnh bắt buộc hoặc bổ sung nội dung ảnh vào Question.`
- `/health` trả `ok: true` nhưng `essayQuestions.ready: false`, `count: 0`. Server sống nhưng nguồn tự luận chưa sử dụng được.
- Đối chiếu: mã `PlanetServer/essay-source.mjs` hiện trên đĩa bỏ qua câu có ảnh, tải thành công 260 câu dạng chữ (89 Vật lý, 87 Hóa học, 84 Sinh học). Thông báo lỗi trên không có trong mã hiện tại.
- Nhận định: có khả năng tiến trình localhost đang chạy bản server cũ hoặc bản ở thư mục khác; chưa xác minh tiến trình vì truy vấn CIM bị từ chối truy cập. Chưa khởi động lại hoặc thay tiến trình.
- Đề xuất: kiểm tra đường dẫn tiến trình và khởi động server từ đúng mã hiện tại, sau đó thử lại luồng thật.
- Bằng chứng: `essay-reproduction.json`.

## Lỗi 2 — Hóa học chương 5 thiếu câu tự luận mức Khó

- Mức độ: trung bình, chặn một lựa chọn cụ thể ngay cả khi server dùng mã hiện tại.
- Luồng tương ứng: Homepage → Hóa học → chương 5 → Lí thuyết → Football/Sheep/Cave → Tự luận → chỉ chọn Khó → Bắt đầu.
- Phương pháp: gọi lớp `FootballPractice` từ mã hiện tại với dữ liệu Sheet thật, chạy riêng trong tiến trình kiểm thử; không thay server ở cổng 8787.
- Kết quả: cả 3 game đều báo `Chưa đủ 5 câu tự luận dạng chữ đã duyệt ... Hiện có 3 câu`.
- Toàn bộ ma trận 3 game × 2 môn × 5 chương × 3 mức khó: 87/90 trường hợp mở được phiên 5 câu; 3 trường hợp lỗi là lựa chọn này.
- Tất cả phiên thành công đã được xóa khỏi bộ nhớ tiến trình kiểm thử; không nộp/chấm bài hoặc lưu lịch sử.
- Đề xuất: bổ sung ít nhất 2 câu dạng chữ đã duyệt đúng môn/chương/mức Khó; thông báo hiện tại cũng hướng dẫn chọn thêm mức khó.
- Bằng chứng: `current-essay-results.json`.

## Các kiểm tra đã đạt

| Hạng mục | Phương pháp | Kết quả |
|---|---|---|
| Biên dịch mã C# runtime | Build `Assembly-CSharp.csproj`, đầu ra riêng | 0 lỗi, 85 cảnh báo; chưa phải build player Unity |
| Tên và nội dung 10 chương | Gọi cả endpoint tên chương và endpoint lý thuyết của scene | 20/20 phản hồi thành công, có tên và nội dung |
| Danh sách thực hành | API thật, 10 chương | 10/10 thành công, mỗi chương có 3 bài |
| Chi tiết thực hành | API thật, 30 bài | 30/30 thành công; mỗi bài có 3 bước và cấu hình JSON hợp lệ |
| Ảnh bài thực hành | Kiểm tra tài nguyên đi kèm | 30/30 bài có ảnh đại diện và hướng dẫn |
| Logic 29 mô phỏng ngoài bài đo chiều dài | Bộ test hiện có + chạy lại bằng dữ liệu API thật | 1.006 kiểm tra đạt trên mỗi lần chạy |
| Ngân hàng trắc nghiệm | API thật, 2 môn × 3 mức khó | 6/6 thành công; 462 câu tổng cộng, không thiếu lựa chọn/đáp án A–D |
| Phân bố trắc nghiệm theo chương | Kiểm tra dữ liệu trả về với bộ lọc chapter tương ứng | Cả 30 tổ hợp môn/chương/mức khó đều có câu |
| Logic server | Toàn bộ `PlanetServer/*.test.mjs` | 114/114 đạt, không bỏ qua test |
| Khởi tạo tự luận theo mã hiện tại và dữ liệu thật | Tiến trình kiểm thử riêng | 87/90 đạt; lỗi còn lại nêu phía trên |

Các test mô phỏng bao gồm hoàn thành 3 bước, chặn bỏ qua bước, từ chối đáp án số sai/NaN/Infinity, ghép thẻ sai, quay lại khôi phục trạng thái và làm lại không lặp kết quả. Đây là kiểm tra logic, không xác nhận thao tác kéo thả trên Canvas.

## Rà liên kết từ Homepage

Đọc `Assets/Scenes/HomePage.unity` cùng prefab đang được scene sử dụng và các override:

- HomePage có tham chiếu nút và bản đồ cho cả hai môn.
- Vật lý có 5 nút chương và panel lý thuyết được gán.
- Hóa học dùng prefab `Assets/Prefabs/ngoc/Chemistry_map_game.prefab`, có 5 nút chương; scene override đã gán `theoryPanel`. GameObject ChemistryMapController rỗng khác trong scene đang tắt, không được coi là lỗi chặn luồng.
- Cả hai môn gọi màn chọn cách học chung.
- Các nút vào game còn có callback lưu môn/quay lại qua `GameMenuController`; không kết luận lỗi chỉ từ `GameSelectScreen.backButton` để trống.
- Thực hành có các nhánh kết quả, làm lại, chọn bài khác, chương tiếp và về Homepage trong mã.

Bằng chứng cấu hình: `scene-results.json`. Đây là rà cấu hình/mã, chưa phải kiểm chứng trạng thái hiển thị thực tế.

## Phần chưa thể xác nhận

- Bấm từ Homepage qua toàn bộ màn hình trong Unity và trở lại đúng panel.
- Kéo thước, kéo thả thẻ, nhập đáp án, cuộn nội dung và tương tác mô hình trên Canvas.
- Chồng lớp giao diện, chữ bị cắt, tỉ lệ màn hình và vùng bấm.
- Chơi từng minigame tới màn kết quả bằng UI, game đối kháng hai máy và lưu lịch sử bằng tài khoản thật.
- Chấm tự luận, gợi ý AI, chat AI, camera/ảnh bài làm bằng dịch vụ thật.
- Lỗi TLS lặp lại có xuất hiện trong log Unity hiện có, nhưng chưa xác định yêu cầu nào gây ra; không kết luận đó là lỗi của luồng học.

Vì các giới hạn này, không kết luận “toàn bộ luồng từ Homepage đã chạy đạt”.

## Tệp kiểm thử

- `audit-api.cjs`, `api-results.json`, `live-fixtures.json`: API lý thuyết/thực hành.
- `LiveExperiments.csproj`: chạy bộ kiểm tra mô phỏng bằng dữ liệu thật.
- `audit-scene.cjs`, `scene-results.json`: cấu hình scene và prefab override.
- `audit-games.cjs`, `game-results.json`: trắc nghiệm và lỗi khởi tạo tự luận.
- `essay-reproduction.json`: tái hiện trên server đang chạy.
- `audit-current-essay.mjs`, `current-essay-results.json`: đối chiếu mã hiện tại.
- `server-tests.log`: 114 test server.
