# API thực hành ảo

## 1. Thêm file

Trong dự án Google Apps Script hiện tại, tạo file `ThucHanh.gs` và dán toàn bộ
nội dung file cùng tên trong thư mục này. File dùng `SPREADSHEET_ID` và `jsonOut`
đã có trong `Mã.gs`; các hàm khác đều có sẵn trong ThucHanh.gs.
Không thay thế file Mã.gs và không tạo thêm một hàm doGet.

## 2. Thêm hai nhánh trong doGet(e) của Mã.gs

Đặt ngay sau dòng khai báo `action`, trước các nhánh cũ và trước
`return getTheory(e)`:

```javascript
if (action === "practiceList") return getPracticeLessons(e);
if (action === "practiceDetail") return getPracticeLesson(e);
```

Giữ nguyên các nhánh questions, essayQuestions, history, getProgressRank và doPost.

## 3. Duyệt bài mẫu và triển khai

Trong ThucHanh, đổi TrangThai của bài TH_PHY_01_01 thành `Đã duyệt` sau khi
kiểm tra nội dung. Các bài Chờ duyệt hoặc Ẩn không được trả về, kể cả khi biết ID.

Lưu mã. Chọn Triển khai > Quản lý các bản triển khai > Chỉnh sửa > Phiên bản mới
> Triển khai. Dùng lại bản triển khai hiện có để giữ nguyên URL /exec.

## 4. Kiểm tra bằng trình duyệt

Thay `LINK_EXEC` bằng đường dẫn Web App /exec đang dùng:

```text
LINK_EXEC?action=practiceList&chapterId=CH_PHY_01
LINK_EXEC?action=practiceDetail&lessonId=TH_PHY_01_01
```

Danh sách đúng sẽ có `success: true`, `apiVersion: practice-v1` và `lessons`.
Chi tiết bài mẫu sẽ có `success: true`, `stepCount: 3`, `lesson` và `steps`.
Nếu không có bài đã duyệt, danh sách trả `count: 0` cùng thông báo cụ thể.

Để xem Hóa học, dùng `chapterId=CH_CHM_01` và `lessonId=TH_CHM_01_01`.
API nhận mã chương chính xác, không lấy nhầm Chương 1 và Chương 10.

## Ảnh

`imageOriginalUrl` và `instructionImageOriginalUrl` giữ nguyên liên kết trong Sheet.
Với link Drive, `imageUrl` và `instructionImageUrl` là URL tải ảnh, kèm trường
`imageFileId` / `instructionImageFileId` để bộ tải Unity xử lý khi cần.

`imagesConfigured` chỉ xác nhận cả hai liên kết có cấu trúc dùng được;
không xác nhận quyền truy cập hay tải ảnh thành công. Ảnh phải được chia sẻ
cho đúng người chơi. Nếu Drive trả trang đăng nhập hoặc thông báo thay vì ảnh,
game cần báo lỗi ảnh; không dùng ảnh nhúng trong Sheet làm đường dẫn tải.
API không thay đổi quyền chia sẻ và không tải ảnh qua tài khoản chủ sở hữu.

## Phạm vi

Đây là API chỉ đọc nội dung học tập. Không ghi điểm, sửa Sheet hoặc tạo mô phỏng.
Màn Lí thuyết / Thực hành và các thao tác trong Unity là bước tiếp theo sau
khi hai đường dẫn kiểm tra hoạt động.
