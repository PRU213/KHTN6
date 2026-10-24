# Kết nối chức năng lưu câu hỏi giáo viên

Unity tiếp tục đọc đúng Google Sheet đang được dự án dùng, gồm tài khoản, `Questions`, `TuLuan` và `TienDoHocTap`. Cấu hình ghi bên dưới chỉ áp dụng cho hai màn tạo câu hỏi giáo viên; không thay URL, mã đăng nhập, bảng xếp hạng hoặc API gameplay của các màn khác. Kiểm tra endpoint gameplay hiện tại chỉ xác nhận được khả năng đọc câu hỏi; chưa có phản hồi hợp đồng ghi giáo viên.

Khi chưa có endpoint ghi đã xác thực, nút lưu giữ **bản nháp JSON và CSV trên máy**, đồng thời báo rõ chưa ghi lên Sheet. URL xem Sheet hoặc CSV chỉ dùng để đọc; không thể thay URL triển khai dịch vụ ghi.

## Triển khai endpoint ghi

1. Đăng nhập tài khoản Google có quyền sửa workbook `1DcJwlN_fUDdcaQ6IfnlkMJApgWuEUqMhyZv9Kn8vfWo`.
2. Tạo một dự án Apps Script độc lập và chép toàn bộ `TeacherApi.gs`. Không thay thế script gameplay hiện có.
3. Deploy → New deployment → Web app, **Execute as: Me**, **Who has access: Anyone**. Cấp quyền Sheets/Drive cho chủ dự án. Nếu tổ chức cấm chia sẻ ảnh công khai, dùng ảnh HTTPS có sẵn thay vì upload ảnh.
4. Chép URL `/exec` mới vào phần kết nối trong màn giáo viên. Cấu hình runtime được lưu riêng ở PlayerPrefs `TeacherApiUrl`. Không tự dùng endpoint gameplay làm URL ghi. Nếu đặt `teacherApiUrl` rõ ràng trên component thì giá trị đó được ưu tiên; để trống để dùng Cài đặt.
5. Unity gọi GET `?action=teacherCapabilities` trước khi gửi mật khẩu. Phản hồi phải có `service: "KHTN6 Teacher API"`, `version: 1`, đúng `workbookId`, và các action `teacherLogin`, `saveQuestion`, `saveEssay`. Endpoint chỉ đọc hoặc trỏ sang workbook khác sẽ bị từ chối.
6. Xác thực bằng đúng tài khoản giáo viên đang đăng nhập vào trò chơi, mật khẩu được kiểm tra phía server theo bảng tài khoản đầu tiên. Token chỉ nằm trong RAM, gắn với tài khoản và URL đã xác thực, hết hạn tối đa một giờ. Đổi tài khoản hoặc URL cần xác thực lại; mật khẩu không được lưu bởi service giáo viên.

Không tự động deploy hoặc sửa dữ liệu thật trong quá trình tạo giao diện. Chức năng lưu từ xa chỉ sẵn sàng sau khi endpoint này được triển khai và xác thực thành công.

## Luồng dữ liệu

- `teacherLogin`: trả token sau khi kiểm tra Username/Password/Role trong Sheet.
- Các POST dùng JSON với object `question` hoặc `essay` và token. Không dùng form phẳng, không bỏ kiểm tra chứng chỉ HTTPS và không coi HTTP 200 là xác nhận đã ghi.
- `saveQuestion`: đủ 12 trường `Questions`, gán `chapter` vào cột `Chương / Chủ đề`.
- `saveEssay`: đủ 13 trường `TuLuan`, gán `chapter` vào `Chương/Chủ đề`; mã giáo viên lấy từ phiên server. Ảnh PNG/JPG tối đa 4 MB được lưu trong thư mục Drive `KHTN6_Question_Images`, chia sẻ xem qua link và ghi URL vào `HinhAnh`.
- Lưu thành công cần phản hồi cùng lúc `success:true`, `saved:true`, đúng `id` và `sheet`. Một ID không thể ghi thêm hàng trùng. Khi gửi lại cùng dữ liệu trong 6 giờ, receipt xác nhận hàng đã được lưu trước đó.
- CSV có dấu ngoặc kép, dấu phẩy, xuống dòng được đọc đầy đủ. `TienDoHocTap` hiện có hai cột `LanHocCuoi`; UI dùng một trường, ưu tiên cột đầu tiên có dữ liệu.
- Bản nháp: `Application.persistentDataPath/TeacherDrafts/<ID>.json`. CSV: `Application.persistentDataPath/TeacherExports/<ID>.csv`. Bản nháp có ảnh vẫn giữ dữ liệu ảnh gốc.

Việc đọc tài khoản hiện vẫn theo cơ chế Sheet công khai đã có của dự án; script này bổ sung kiểm tra quyền giáo viên khi ghi và không thay đổi hệ thống đăng nhập hiện tại.

## Bản sửa gần đây của service

Trước khi khôi phục luồng ghi, bản service người dùng sửa được giữ tại `tools/teacher_ui/backups/TeacherSheetService.user-20261024.cs.txt`. Phần đọc Sheet và tải lại danh mục được giữ. Chỉ các phần ghi không tương thích được thay: endpoint gameplay mặc định, xác thực giả qua GET đọc câu hỏi, form POST không token, bỏ kiểm tra chứng chỉ và nhận định thành công bằng tìm chuỗi.
