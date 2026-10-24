# Bốn màn hình giáo viên trong Unity

Mở `Assets/Scenes/GiaoVien.unity` rồi bấm Play. Ba nút mở:

- `GiaoVienTracNghiem.unity`: đủ 12 trường của Questions, kiểm tra dữ liệu và chọn một đáp án đúng.
- `GiaoVienTuLuan.unity`: đủ 13 trường của TuLuan, chọn/xóa/xem trước ảnh PNG/JPG trong Editor hoặc bản Windows.
- `GiaoVienTienDo.unity`: tìm học sinh, lọc lớp, xem đủ 9 trường tiến độ; cuộn danh sách và lọc chương/trạng thái.

Bốn scene đã nằm trong Build Settings. Mỗi scene chứa `TeacherCanvas` với các GameObject uGUI/TextMeshPro thật, có thể chỉnh trực tiếp bằng Inspector. Hình và tọa độ lấy từ bộ `KHTN6_Teacher_UI`, giữ khung tham chiếu 1920×1080. Font DejaVu được nhúng để hiện đúng tiếng Việt. Không dùng ảnh mockup làm giao diện tương tác.

Đã căn chữ giữa theo chiều dọc trong các nút/khung, mở rộng thanh tài khoản lên 814×138, tăng avatar/icon và cỡ chữ tên giáo viên. Tên dài tự thu nhỏ hoặc có dấu ba chấm để không tràn. Font SDF96 và ảnh không nén với lọc bilinear giúp chữ/viền rõ hơn ở các mức thu phóng; Canvas bật Pixel Perfect. Nên xem Game ở độ phân giải 1920×1080 trong cửa sổ đủ lớn, vì Scene/Game view thu quá nhỏ sẽ làm toàn bộ giao diện khó đọc.

Nút Quay lại ở ba màn con trở về trang giáo viên. Nút Cài đặt mở điều chỉnh âm lượng và kết nối dịch vụ lưu. Thông tin tài khoản lấy từ phiên đăng nhập hiện có; nếu bấm Play trực tiếp, các thông tin đã lưu từ lần đăng nhập trước vẫn được dùng.

## Dữ liệu thật và trạng thái lưu

Môn/chương, danh sách học sinh/lớp và tiến độ được đọc từ đúng Google Sheet đang dùng trong dự án. Tỷ lệ đúng hiển thị độ chính xác khi làm bài, không phải tỷ lệ hoàn thành. Hai cột `LanHocCuoi` trùng trong Sheet được gộp thành một trường hiển thị, ưu tiên giá trị đầu tiên có dữ liệu.

Endpoint gameplay hiện có chưa hỗ trợ ghi câu hỏi giáo viên. Khi chưa kết nối endpoint ghi, nút Lưu giữ bản nháp JSON và xuất CSV trên máy, đồng thời báo rõ **chưa ghi Google Sheet**. Bản nháp ở `Application.persistentDataPath/TeacherDrafts`, CSV ở `Application.persistentDataPath/TeacherExports`. Hướng dẫn và mã endpoint đã chuẩn bị tại `tools/teacher_backend/README.md` và `TeacherApi.gs`.

Để lưu trực tiếp: chủ Sheet triển khai endpoint này, nhập URL `/exec` trong Cài đặt và xác thực bằng tài khoản giáo viên đang đăng nhập. Chỉ khi server xác nhận bản ghi đúng ID, màn hình mới báo đã lưu lên Sheet. Không thay thế endpoint gameplay cũ.

## Chỉnh sửa và dựng lại

- Hình/font/layout: `Assets/TeacherUI`.
- Mã điều khiển: `Assets/Scripts/Teacher`.
- Dựng lại từ layout: menu `Tools > KHTN6 > Dung 4 man giao vien`. Thao tác này thay nội dung bốn scene bằng layout nguồn; chỉnh file layout nếu muốn giữ thay đổi khi dựng lại.
- Bản sao scene giáo viên trước khi thay: `tools/teacher_ui/GiaoVien.before.unity`.

Chọn lại `GiaoVien.unity` trước khi bấm Play để bắt đầu từ menu giáo viên.
