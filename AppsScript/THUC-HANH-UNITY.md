# Thực hành ảo trong Unity

## Luồng đã nối
- Chọn chương Vật lý hoặc Hóa học → Lí thuyết / Thực hành ảo.
- Lí thuyết giữ luồng đọc bài và chọn game hiện có.
- Thực hành tải các bài đã duyệt qua `AppsScriptEndpoint.Url`.
- Dạng `DoChieuDai` có ba thao tác: kéo thước sát vật, kéo hai mốc đọc số, nhập hiệu hai số đo.
- Các dạng khác có thể xem mục tiêu và ảnh; chưa có mô phỏng tương tác.
- Kết quả thực hành chưa lưu vào History. Hỏi AI sử dụng cấu hình server của ScienceChatController hiện có.

## Kiểm tra trên Unity
1. Đợi Unity import script và ảnh mới, mở HomePage rồi Play.
2. Chọn Vật lý → Chương 1 → Thực hành ảo → bài đo chiều dài đã duyệt.
3. Kéo thước lên sát dưới vật rồi bấm Kiểm tra bước.
4. Kéo A tới vạch 2 và B tới vạch 10 rồi kiểm tra (với cấu hình mẫu).
5. Nhập 10: phải nhận phản hồi sửa. Nhập 8: hiện kết luận và ảnh hướng dẫn.
6. Thử Làm lại, Quay lại, nhánh Lí thuyết và một chương Hóa học.
7. Nếu tắt mạng, phải có thông báo lỗi và nút tải lại; nếu ảnh Drive không công khai, nội dung và thao tác vẫn dùng được.
8. Đóng màn trong lúc đang tải rồi mở chương khác: dữ liệu cũ không được ghi vào chương mới.

## Giao diện
Tái sử dụng nền TrangChuBg2, khung gỗ hoa lá và nút xanh 3D của dự án trong Resources/Practice. Không cần kéo thêm tham chiếu trong Inspector cho giao diện mới.

## Xác minh tự động
Biên dịch C# thành công (bao gồm script mới). Bảy kiểm tra API trong PlanetServer/practice-api.test.mjs đạt. Chưa xác minh thao tác và bố cục bằng Play Mode trong Unity ở phiên này.
