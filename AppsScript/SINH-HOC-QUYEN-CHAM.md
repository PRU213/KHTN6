# Sửa quyền chấm AI Sinh học

Lỗi đã xác minh trong `BaiNopTuLuan.NhatKyLoiChamAI`: `UrlFetchApp.fetch` thiếu scope `https://www.googleapis.com/auth/script.external_request`. API lưu bài vẫn hoạt động nên bài nằm trong Sheet với trạng thái `Failed`, chưa có điểm. Khởi động lại Unity hoặc triển khai Render không cấp được quyền này cho Apps Script.

Đã xử lý trên dịch vụ thật ngày 04/11/2026: tài khoản triển khai chạy thành công hàm kiểm tra HTTP, sau đó cập nhật lại **đúng deployment hiện tại, giữ nguyên version 62**, URL và cài đặt truy cập. Không đưa mã HEAD hoặc chức năng khác lên bản chạy. Chấm lại bài lỗi có sẵn đã trả `Completed`, điểm 6/10 và nhận xét AI sau khoảng 6,7 giây. Không tạo bản ghi thử. Phản hồi được lưu cục bộ ở `outputs/biology/real-grading-retry.json`.

Nếu lỗi này quay lại, kiểm tra quyền bằng hàm chỉ đọc trước. Nếu quyền đã hoạt động trong Editor nhưng web app vẫn thiếu quyền, thử cập nhật lại deployment cùng version trước khi đổi mã hoặc scope.

Chủ tài khoản đang triển khai Apps Script cần:

1. Mở đúng dự án Apps Script của URL chấm đang dùng trong `TuLuanController.backendApiUrl`.
2. Thêm file `BiologyGradingAuthorization.gs`; giữ nguyên các file hiện có.
3. Nếu dự án đặt `oauthScopes` trong `appsscript.json`, **bổ sung** `https://www.googleapis.com/auth/script.external_request` vào danh sách hiện có. Không thay cả manifest hoặc xóa quyền của phần khác.
4. Chọn hàm `authorizeBiologyEssayGrading` rồi Run. Chủ tài khoản tự xem và chấp nhận quyền gọi dịch vụ ngoài mà Google yêu cầu. Hàm chỉ đọc HTTP 204 từ Google, không tạo bài thử hay sửa Sheet.
5. Nếu đã đổi manifest hoặc mã dự án: Deploy → Manage deployments → Edit bản web app đang dùng → New version → Deploy. Giữ URL và các cài đặt triển khai hiện có.
6. Quay lại Sinh học, bấm Thử chấm lại. Giữ SubmissionID nên không thêm hàng trùng. API đã có `saveEssaySubmission` tự chấm và có thể thử lại bản ghi `Failed`.

Không cần thay GEMINI_API_KEY nếu lỗi vẫn là thiếu quyền UrlFetchApp. Nếu Google tiếp tục báo lỗi khóa/quota khác sau khi cấp quyền, kiểm tra lỗi mới thay vì đoán điểm hoặc dùng kết quả giả.

Tài liệu Google: https://developers.google.com/apps-script/guides/services/authorization
