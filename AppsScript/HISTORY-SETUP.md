# Cập nhật cột Chương trong lịch sử

Phần lọc Tất cả / Đối kháng / Luyện tập và màu nút chạy trong Unity.
Để các trận luyện tập mới lưu và đọc lại môn/chương từ Google Sheets,
cần cập nhật mã Google Apps Script đang triển khai.

## Nếu dự án Apps Script dùng Code.gs

Từ bản Code.gs trong thư mục này, cập nhật hai hàm `getHistory` và
`planetSaveMatch_`, rồi thêm hàm `planetHistoryLessonColumns_`.
Không tạo thêm bản sao của các hàm đã tồn tại và không thay đổi
`SPREADSHEET_ID` hay cấu hình riêng của bản đang triển khai.

## Nếu dự án Apps Script dùng Ma-da-sua.gs và PlanetMatchHistory.gs

Cập nhật hàm `getHistory` theo Ma-da-sua.gs.
Trong PlanetMatchHistory.gs, cập nhật `planetSaveMatch_` và thêm
`planetHistoryLessonColumns_`. Không thêm PlanetMatchHistory.gs nếu
các hàm đó đã được nhúng trong file chính.

## Áp dụng

Trong Apps Script, chọn Deploy > Manage deployments > Edit > New version > Deploy.
Giữ nguyên bản triển khai và URL /exec mà Unity đang dùng.

Không cần tự chèn cột vào Sheet. Lần lưu trận mới sẽ tự thêm các cột
`Subject` và `Chapter` vào cuối nếu chưa có; các cột cũ được giữ nguyên.
Thông tin chỉ được lưu cho Luyện tập. Bản ghi cũ chưa lưu môn/chương và
các trận Đối kháng hiển thị dấu “—”; chương không được suy đoán từ điểm hay ngày.

Sau khi Unity biên dịch lại, thử ba tab để kiểm tra lọc và màu nút.
Hoàn thành một lượt luyện tập mới, mở Lịch sử và kiểm tra ví dụ
“Chương 1 - Vật lí”.

## Sửa lệch cột History (27/10/2026)

Mã mới hỗ trợ cấu trúc trong ảnh: Username, Fullname, Difficulty, Opponent,
Mode, Result, Score, Time, Duration, PlanetMatchID, PlanetRankScore, Subject, Chapter.
Đọc và ghi theo tiêu đề; không cần xóa hoặc di chuyển cột trong Sheet.

1. Nếu dùng Code.gs: cập nhật file chính bằng Code.gs trong thư mục này.
2. Nếu dùng Ma-da-sua.gs + PlanetMatchHistory.gs: cập nhật cả hai file cùng tên.
   Không thêm Code.gs vì sẽ trùng hàm.
3. Giữ SPREADSHEET_ID đang dùng và các file tích hợp riêng.
4. Deploy > Manage deployments > Edit > New version > Deploy. Giữ URL /exec.
5. Mở lại game, chờ đồng bộ rồi tải lại Lịch sử và Xếp hạng.

Fullname, Difficulty, Duration cũ được giữ nguyên; lượt mới chưa có thông tin
cho các trường này sẽ để trống. Xếp hạng chỉ tính lượt hoàn thành trong tuần.
