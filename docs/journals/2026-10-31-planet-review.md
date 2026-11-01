---
date: 2026-10-31
status: completed
tags: [planet, remediation, unity]
---

# Sửa câu sai sau đối kháng Planet

## Gợi ý khi chọn Em chưa biết

Trước sửa, answer() bỏ qua AI khi nhận “Em chưa biết”, đặt checking=true rồi
trả lại explanation/checkQuestion tĩnh của kế hoạch. Vì vậy các lần nhờ giúp
lặp nguyên nội dung, kể cả câu hỏi kiểm tra mơ hồ. Thêm guide() theo đúng bước,
đề gốc và lần trả lời gần đây; gợi ý tăng mức cụ thể, câu hỏi phải ghi rõ ký hiệu,
đơn vị và đủ tiêu chí của bước gốc. Chấm câu hỏi được hướng dẫn vẫn dùng expected
gốc, không nhận đáp án riêng do gợi ý tự khai. Không đổi điểm hoặc tự qua bước.
Lưu guidance cùng nhật ký; retry không tạo thêm lượt, lỗi AI không làm mất tiến độ.
Giao diện gắn nhãn GỢI Ý CÁCH LÀM và EM THỬ TRẢ LỜI, không gọi yêu cầu trợ giúp
là một đáp án “chưa đúng”. 60/60 kiểm thử liên quan đạt, C# 0 lỗi. Chưa deploy
Render và chưa gọi Gemini thật cho gợi ý mới; cấu hình endpoint vẫn giữ Render.

## Khôi phục tự luận về máy chủ đang hoạt động

Sau khi thử local, người dùng báo tự luận không hoạt động như trước. Cấu hình
PlanetDuelSettings dùng chung cho đối kháng và luyện tập Football/Sheep/Cave,
nên đổi sang localhost cũng chuyển các lượt tự luận sang local. Kiểm tra trực
tiếp Gemini bằng cấu hình `.env` người dùng vừa cung cấp nhận HTTP401 khóa
không hợp lệ; khóa trong `.env.example` cũ nhận HTTP403 đã bị Google thu hồi do
lộ khóa. Không ghi khóa vào log/tài liệu này. Thử `/chat` Render trả HTTP200 và
có câu trả lời AI. Khôi phục PlanetDuelSettings về Render như trước để phần tự
luận dùng lại backend cũ. Không sửa essay.mjs/chấm điểm hoặc đẩy mã lên host.
Học lại đối kháng mới còn cần deploy backend hoặc cấu hình Gemini local hợp lệ.

## Sửa vòng lặp đăng nhập nhật ký

Kiểm tra host thực tế: `/health` có tutorVersion=2 nhưng thiếu duelReviewVersion;
POST rỗng `/review/pending` trả 401 “Phiên đã hết hạn. Hãy tìm trận lại.” từ
router đối kháng cũ. UI coi mọi 401 là token nhật ký hết hạn nên mở lại ô mật
khẩu, kể cả sau khi xác minh thành công. Không dùng mật khẩu thật để kiểm tra.

EssayTutorUI kiểm tra phiên bản `/health` trước khi hỏi mật khẩu cho API xem
lại, báo rõ cần cập nhật PlanetServer và giữ nguyên bài chờ. Phản hồi 401 kiểu
phiên đấu cũ cũng hiện lỗi phiên bản thay vì mở vòng lặp đăng nhập. Không đổi
xác thực, không thêm mật khẩu dự phòng và chưa deploy backend.

Biên dịch lại C# đạt 0 lỗi. Unity fixture xác nhận máy chủ cũ hiện đúng thông
báo phiên bản, không gửi API xem lại chưa hỗ trợ và không mở lại ô mật khẩu;
toàn bộ luồng xem lại/học lại/đồng bộ hiện có vẫn đạt. Bằng chứng:
`outputs/planet-review-login-smoke.txt`, `outputs/planet-review-login-compile.log`.

## Bối cảnh

Người dùng yêu cầu trắc nghiệm và tự luận đối kháng hiển thị câu sai, gợi ý và
giải lại từng bước giống luyện tập tự luận. Phương án được duyệt trước khi sửa.

## Thay đổi

Lưu lựa chọn/bài nộp theo từng người và từng câu, gồm cả chọn sai rồi chọn đúng.
Danh sách sau trận cuộn được đủ 15 câu; câu sai/chưa trả lời dùng lại EssayTutor.
Sai hoặc chưa biết thì giải thích và kiểm tra lại bước hiện tại; đúng mới đi
tiếp. Máy chủ xác nhận hoàn thành, giữ nguyên điểm/thắng thua ban đầu và khóa
trận mới khi còn mục Planet chưa sửa. Sinh học được mở trong Planet, phạm vi
luyện tập tự luận cũ được giữ.

## Review và quyết định

Đấu máy cục bộ giữ bản chờ trên thiết bị đến khi lưu thành công; lỗi lưu trả
503 và thử lại không tạo mục trùng. Cổng tìm trận chỉ trả số lượng cần học lại,
không lộ bài nộp chưa xác thực. Dùng sourceId để khôi phục đúng mục khi đăng ký
ban đầu lỗi. Trận đúng hoàn toàn không bị buộc đăng nhập đồng bộ. AI chưa chấm
xong hiện chờ kiểm tra, không tự coi đúng hoặc sai.
Thiết bị ghi nhớ nghĩa vụ từ trận máy chủ để mở lại scene hoặc chuyển đấu máy
khi mất mạng không bỏ qua học lại; chỉ cập nhật hết nghĩa vụ sau khi tải lại
tiến độ toàn tài khoản có xác thực.

## Kiểm tra và việc tiếp theo

75/75 test backend liên quan đạt; review độc lập chạy 5/5 test lõi đạt. Toàn bộ
backend có 146 test: 132 đạt, 14 lỗi nền đã biết (1 reader Apps Script TuLuan độc
lập và 13 assertion lịch sử/xếp hạng tuần cũ), log tại
`outputs/duel-review-backend-tests.txt`. Biên dịch C# 0 lỗi và smoke test Unity cô
lập đạt (15 câu cuộn, trạng thái/ lựa chọn cá nhân, học lại và xác thực đồng bộ,
khóa trận mới, phục hồi sourceId, câu đấu máy sau xáo đáp án, trận đúng hoàn toàn,
lưu/mở lại và đồng bộ cục bộ); log `outputs/planet-review-smoke.txt`. Chưa gọi
Gemini thật, chưa commit/push/deploy. Khi đưa vào dùng cần cập nhật cả Unity và
PlanetServer, kiểm tra duelReviewVersion=1 và lưu nhật ký trên ổ bền.
