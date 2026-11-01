# Sửa câu sai sau đối kháng Planet

Phương án đã được người dùng duyệt trong chat ngày 31/10/2026: dùng luồng
giải lại từng bước giống luyện tập tự luận cho trắc nghiệm và tự luận sau trận.

- Lưu riêng lựa chọn/bài nộp từng câu của từng người; liệt kê đúng/sai/chưa trả lời.
- Câu sai hoặc chưa trả lời mở EssayTutorUI: sai thì giải thích bước hiện tại và
  kiểm tra lại, đúng mới đi tiếp; chỉ mở lời giải tổng kết khi hoàn thành.
- Áp dụng cả ba môn trong Planet, giữ phạm vi luyện tập tự luận cũ.
- Điểm/thắng thua ban đầu giữ nguyên; máy chủ khóa trận mới khi còn mục Planet
  chưa hoàn thành. Có thể về trang chủ và tiếp tục từ nhật ký.
- Đấu máy cục bộ giữ bản câu sai trên thiết bị đến khi đăng ký học lại thành công.
- UI dùng danh sách cuộn cho 15 câu; hoàn thành học lại quay về danh sách câu.

Touchpoints: PlanetDuelController.cs, BlastRoundManager.cs, EssayTutorUI.cs,
PlanetServer/server.mjs, tutor.mjs. Không chỉnh scene, không deploy/commit.

Kiểm tra: API round history/registration/auth/gate/retry; state machine bước học;
biên dịch Unity và smoke test giao diện bằng fixture cục bộ. Giữ nguyên các thay
đổi đang có trong workspace, phân biệt lỗi test nền với hồi quy mới.

## Tiến độ

- [x] Lưu lịch sử từng câu, từng người cho trắc nghiệm và tự luận.
- [x] Mở học lại từng bước và đồng bộ hoàn thành về danh sách sau trận.
- [x] Khóa trận mới trên máy chủ khi còn mục Planet chưa hoàn thành.
- [x] Lưu câu sai đấu máy cục bộ đến khi máy chủ xác nhận lưu thành công.
- [x] Review: sửa mất bản chờ khi lưu lỗi, rò bài nộp qua tìm trận chưa xác thực,
  đồng bộ mục chưa có ID, buộc đồng bộ trận đúng hoàn toàn và nhãn AI chưa chấm.
- [x] Kiểm thử backend liên quan: 75/75 đạt; reviewer chạy độc lập 5/5 test lõi.
- [x] Biên dịch C# 0 lỗi; smoke test Unity cô lập đạt: 15 câu cuộn, câu chưa
  chấm, lựa chọn cá nhân, học lại/đồng bộ hoàn thành, khóa trận mới, phục hồi
  sourceId, xáo đáp án đấu máy, trận đúng hoàn toàn, lưu/mở lại và đồng bộ cục bộ.

Trạng thái: triển khai và xác minh xong. Không có phase file riêng cần
đồng bộ. Không commit, push hoặc deploy; các sửa đổi có sẵn vẫn được giữ nguyên.

Toàn bộ backend: 146 test, 132 đạt và 14 lỗi nền đã biết (1 reader Apps Script
TuLuan độc lập, 13 assertion lịch sử/xếp hạng tuần cũ); không có lỗi mới trong
75 test liên quan. Log: `outputs/duel-review-backend-tests.txt`. Chưa gọi Gemini
thật; cần kết nối AI và cấu hình lưu bền khi vận hành.

Ghi nhớ số câu máy chủ cần sửa trên thiết bị để mở lại scene hoặc mất mạng
không bỏ qua cổng học lại. Chỉ xác nhận toàn bộ nghĩa vụ đã hoàn thành sau khi
tải lại tiến độ tài khoản có xác thực. Log UI: `outputs/planet-review-smoke.txt`.
