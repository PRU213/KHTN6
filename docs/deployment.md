# Triển khai backend KHTN6

- Nền tảng: Render, dịch vụ `khtn6-server` (gói hiện tại Free, không đổi gói).
- URL: https://khtn6-server.onrender.com
- Repo: https://github.com/PRU213/KHTN6, nhánh `main`, root directory `PlanetServer`.
- Service ID: `srv-dbgdh8t9fdbs73e0c1og`.
- Build: `npm install`; Start: `node server.mjs` hoặc `npm start`.
- Cập nhật mã trên nhánh kết nối để auto-deploy; nếu không tự chạy, dùng Render
  Manual Deploy → Deploy latest commit. Chỉ deploy lại commit cũ không cập nhật code.

## Bản sửa học lại bắt buộc — 30/10/2026

Commit backend: `c0b0dd3f843ed63fe00d2eb94cc972a7ce745447`.
Đã xác minh server thực tế sau cập nhật:

```json
{"ok":true,"tutorVersion":2,"mandatoryPracticeTutor":true}
```

POST `/tutor/list` không có bearer token trả HTTP 401 với thông báo đăng nhập
nhật ký; không còn trả lỗi phiên đấu của server cũ. Kiểm thử nội bộ 93/93 đạt,
bao gồm khóa chuyển câu tại API và mở khóa khi các bước đã hoàn thành.

Sau deploy, thoát lượt luyện tập cũ và bắt đầu lại. Khi thay script trong Unity
đang Play, dừng Play, chờ biên dịch, Play lại để dựng UI mới.

## Biến môi trường

- Giữ `GEMINI_API_KEY`, `GEMINI_MODEL`, `APPS_SCRIPT_URL` / `APPS_SCRIPT_SECRET`
  hiện có. Không đưa giá trị khóa vào repository hoặc bản build Unity.
- `TUTOR_USERS_URL`: tùy chọn, nguồn Users CSV đang dùng để xác minh tài khoản.
- `TUTOR_DATA_DIR`: thư mục lưu nhật ký học lại. Mặc định `PlanetServer/data/tutor`.
  Dữ liệu trên filesystem tạm không bền qua redeploy; để giữ lâu dài cần ổ bền
  hoặc chuyển sang dịch vụ lưu trữ bền. Bản sửa này không mua/nâng cấp dịch vụ.

Rollback backend: trong Render chọn bản deploy trước commit trên. Unity mới sẽ
hiện thông báo server chưa hỗ trợ và tiếp tục khóa chuyển câu sai; không âm thầm
bỏ qua yêu cầu học lại. Hướng dẫn tính năng: `PlanetServer/ESSAY-TUTOR.md`.

## Học lại sau đối kháng — 31/10/2026

Bản sửa hiện có trong workspace, **chưa deploy lên Render**. Cần cập nhật cả
Unity và PlanetServer. Máy chủ hỗ trợ trả `duelReviewVersion: 1` trong `/health`
và các phản hồi trạng thái đối kháng. Unity mới sẽ báo cần cập nhật nếu máy chủ
còn chạy bản cũ, tránh kết thúc trận mà thiếu câu sai cần học lại.

Các API `/review/pending` và `/review/local` dùng Bearer token của đăng nhập
nhật ký. Giữ cấu hình Gemini và `TUTOR_DATA_DIR` trên ổ bền; không thêm khóa vào
Unity. Đấu máy ngoại tuyến giữ bản câu sai trên thiết bị đến khi đăng ký nhật ký
thành công. Hướng dẫn hành vi, API và kiểm thử: `PlanetServer/DUEL-REVIEW.md`.
Thiết bị cũng ghi nhớ nghĩa vụ học lại của các trận máy chủ; chỉ xóa cổng đã
ghi nhớ sau khi tải lại tiến độ toàn tài khoản có xác thực, kể cả khi học sinh
đã hoàn thành trên thiết bị khác.

## Chép phạt — 02/11/2026

Thay đổi ở workspace, chưa deploy. Cập nhật Unity và PlanetServer cùng nhau;
`/health` phải trả `copyPenaltyVersion: 2`. Giữ Gemini và `TUTOR_DATA_DIR` trên
ổ bền. Tự luận điểm cuối ≤ 6: học lại rồi chép phạt ngay trước câu tiếp theo.
Trắc nghiệm/đối kháng học lại và chép phạt từng câu sai sau lượt; bỏ điều kiện sai 3 lần. Lượt mới
cần xác minh nhật ký qua mạng, gồm cả đấu máy và game trắc nghiệm chạy cục bộ.
Quy tắc, API nộp ảnh và hướng dẫn kiểm tra: [COPY-PENALTY.md](../PlanetServer/COPY-PENALTY.md).
