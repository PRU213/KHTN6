# Học lại sau đối kháng Planet

Trắc nghiệm và tự luận ở cả ba môn đều có bảng xem lại sau trận. Máy chủ giữ
câu đã làm, lựa chọn/bài nộp của từng người, trạng thái đúng/sai/chưa trả lời và
mã học lại. Câu đã chọn sai rồi chọn đúng vẫn cần học lại. Câu chưa được AI chấm
xong được ghi `ungraded`, không tự coi là làm sai.

Các câu sai/chưa trả lời dùng lại `EssayTutor`: 3–8 bước, trả lời sai hoặc chưa
biết thì giải thích bước hiện tại và kiểm tra lại; đúng mới sang bước kế tiếp.
Lời giải đầy đủ chỉ mở khi hoàn thành. Điểm và thắng/thua ban đầu không thay đổi.
Phần luyện tập tự luận Sinh học vẫn giữ phạm vi cũ; chỉ Planet chủ động đăng ký
học lại Sinh học qua `source: planet`.

`/join`, ghép trận tự động và nút đấu máy đều kiểm tra mục Planet chưa hoàn thành.
Tài khoản có câu cần sửa nhận `reviewRequired` cùng số lượng; không trả bài nộp
riêng tư qua API tìm trận chưa xác thực. Các mục học lại được lưu ngay khi kết
thúc câu, tiếp tục được trong nhật ký dù phiên trận đã hết hạn hoặc máy chủ đã
khởi động lại. Lưu thất bại không cho qua cổng tìm trận, và có thể tải lại.

## API bổ sung

- `GET /health`: `duelReviewVersion: 1`. Mỗi phản hồi trạng thái trận cũng có cờ
  phiên bản này để Unity phát hiện máy chủ cũ.
- `POST /review/pending` với `{username}` và Bearer token của `/tutor/login`:
  trả `{state: "finished", reviewQuestions, reviewPending}`. Danh sách gồm cả mục
  Planet hoàn thành để giao diện đồng bộ bằng `sourceId`/`tutorId`.
- `POST /review/local`, cùng xác thực, với `{username, matchId, reviewQuestions}`.
  `matchId` là `local-` cộng GUID; mỗi câu có `round`, `text`, `subject`, `options`
  và mảng chỉ số `choices`. Máy chủ đối chiếu ngân hàng, tự xác định đáp án đúng
  và trạng thái; bỏ qua `answeredCorrectly`, `tutorCompleted` và đáp án do client
  khai báo. Lưu lỗi trả 503 để client giữ bản chờ đồng bộ và thử lại.

`reviewQuestions` có `sourceId`, `round`, `questionType`, `status`, `earned`,
`text`, `subject`, `options`, `choices`, `transcription`, `needsTutor`, `tutorId`,
`tutorCompleted`, `tutorError`. Mục chưa hoàn thành giấu `sampleAnswer`,
`suggestedAnswer`, `feedback` và đặt `correct: -1`.

## Vận hành và kiểm tra

Cần cập nhật cả Unity và PlanetServer để có luồng mới. Dùng `TUTOR_DATA_DIR` trên
ổ bền như hướng dẫn `ESSAY-TUTOR.md`; Gemini dùng cấu hình hiện có. Bản này chưa
tự triển khai lên host. Khi ngoại tuyến, Unity giữ lượt sửa bài chờ đồng bộ;
học từng bước cần kết nối AI và đăng nhập nhật ký.

Chạy `node --test PlanetServer/duel-review.test.mjs` để kiểm tra lựa chọn trực tiếp,
va chạm đạn, tách bài từng người, học lại sai/đúng, khóa tìm trận, lỗi lưu, khôi
phục sau restart, xác thực, dữ liệu giả và đồng bộ lượt chơi cục bộ.
