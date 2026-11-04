# Chép phạt và làm lại câu hỏi — quy tắc phiên bản 2

## Luồng thực hiện

- **Tự luận Football/Sheep/Cave**: AI chấm bài nộp. Điểm kiến thức < 10/10
  yêu cầu học lại ngay theo từng bước. Xét điểm cuối sau trừ gợi ý của chính
  bài nộp ban đầu: **≤ 6/10** phải chép phạt sau khi học lại xong, trước câu
  tiếp theo. Ví dụ 9 − 3 = 6 vẫn phải chép. Bài đúng 10/10 dùng hai gợi ý
  còn 7/10 không cần học lại hoặc chép phạt. Hết giờ chưa nộp được tính 0.
- **Trắc nghiệm Football/Sheep/Cave và đối kháng Planet**: kết thúc lượt/trận
  theo luật game, sau đó học lại các câu sai. Hoàn thành các bước của một câu
  rồi chép phạt, đạt mới được mở câu sai tiếp theo. Giữ điểm trận ban đầu.
- **Bỏ quy tắc sai 3 lần**: số lần sai hoặc “Em chưa biết” trong phần học lại
  không quyết định phát sinh hình phạt nữa. Sai thì vẫn ở lại bước và có gợi ý.

## Bài viết tay bắt buộc

- **Lý thuyết và tính toán đều chỉ viết tay đề và lời giải/đáp án đầy đủ 1 lần**.
  Bài tính có công thức, thay số, đơn vị và kết luận phù hợp. Không cần chép lặp
  công thức/đáp án hoặc viết thêm một bài riêng. Mẫu mở sau khi học lại hoàn tất.
- Nhiệm vụ cũ đang chờ 5/10 lần được đổi về 1 khi tải lại hoặc gửi ảnh. Lịch sử
  bài đã hoàn thành giữ nguyên. Không tự cho qua bài đang chờ; ảnh cũ được phép
  gửi kiểm tra lại theo yêu cầu mới, kể cả đã gửi và bị từ chối ở quy tắc cũ.
- Nộp toàn bộ các trang trong cùng một lần, tối đa 6 ảnh JPG/PNG, mỗi ảnh 2 MB;
  có thể chọn từ máy hoặc chụp webcam. AI phải xác nhận viết tay, đủ số bản đúng,
  đã chép đủ đề và phần giải lại đúng/đầy đủ trong **cùng một bộ ảnh**. Không cộng
  giữa các lần nộp; không ghép phần chép đạt ở lần trước với lời giải ở lần sau.
- Ảnh trùng byte trong một bộ bị từ chối. AI không tính nhiều lần từ ảnh chồng lấn,
  nhưng không loại một bài hoàn chỉnh chỉ vì cùng nét chữ hoặc cùng trang xuất
  hiện ở nhiều ảnh. AI loại chữ đánh máy, nội dung thiếu/sai và ảnh không rõ;
  chấp nhận cách diễn đạt tương đương/viết tắt hiểu rõ. AI vẫn có thể nhận nhầm và không chứng minh
  được ai tự viết. Lỗi dịch vụ/JSON không hợp lệ giữ nguyên tiến độ.

## Dữ liệu và các cổng kiểm tra

Nhật ký dùng `TUTOR_DATA_DIR` trên ổ bền, gồm điểm kiến thức, điểm cuối, trạng thái
học lại và `penalty`: `deferred` (chờ học lại), `copying`, `completed`; mẫu, đề,
lời giải, số cần/đạt, cờ phần giải lại đạt và thời gian hoàn thành. Lịch sử nộp
lưu hash/kết quả, không lưu ảnh gốc. Học sinh/giáo viên xem trong Nhật ký học lại;
giáo viên chỉ đọc.

`status: completed` của học lại chỉ xác nhận đã qua các bước. Cổng hoàn thành
câu còn kiểm tra chép phạt. Máy chủ chặn `/football/next` và mở câu học lại sau
trong cùng lượt; nút tiếp tục Unity cũng khóa. Thoát rồi vào lại/đổi thiết bị
không xóa nghĩa vụ đã lưu. Mọi lượt mới cần mạng và xác thực nhật ký.

Trắc nghiệm luyện tập ghi câu sai vào hàng đợi PlayerPrefs theo tài khoản trước
khi đồng bộ. Khi hết lượt hoặc trước lần chơi mới, backend xác minh ID/đáp án
với `action=questions` của Apps Script đang cấu hình. Không tin đáp án mẫu hoặc
cờ hoàn thành từ client. Chỉ xóa hàng đợi sau khi backend lưu thành công. Không
thêm cột hoặc sửa Apps Script. Backend cần đọc được cùng ngân hàng với Unity.

Nhật ký cũ theo quy tắc 3 lần được gỡ hình phạt cũ khi tải. Các câu của phiên còn
hoạt động được đăng ký lại theo điểm cuối; lịch sử đã hoàn thành không tự bị
phạt hồi tố chỉ vì từng sai 3 lần.

## API và triển khai

- `GET /health`: `copyPenaltyVersion: 2`.
- `POST /tutor/quiz-review`: `{username,quizBatches:[{id,game,subject,questions:[{id,chosen}]}]}`.
- `POST /tutor/copy-start`: `{id,username}`; chỉ sau học lại. Được thực hiện ngay
  trên câu tự luận đang ở màn kết quả; đối kháng chờ hết trận.
- `POST /tutor/copy-submit`: `{id,username,requestId,images:[{base64,mimeType}]}`.
- Các API tutor dùng Bearer token của `/tutor/login`, phân quyền theo tài khoản,
  khóa đồng thời theo mục và chống gửi trùng. Client không quyết định số đạt.

Cập nhật **Unity và PlanetServer** cùng nhau; chưa tự deploy từ workspace.
Giữ `GEMINI_API_KEY`, `GEMINI_MODEL` và `TUTOR_DATA_DIR`. Unity mới từ chối backend
có `copyPenaltyVersion < 2`, tránh cho chuyển câu theo quy tắc cũ.

## Kiểm tra

Backend: chạy `node --test copy-penalty.test.mjs quiz-review.test.mjs tutor.test.mjs tutor-guidance.test.mjs practice-tutor-gate.test.mjs duel-review.test.mjs football.test.mjs server.test.mjs`.

Fixture UI offline: khi project không mở trong Unity khác, chạy Unity với
`-batchmode -projectPath <project> -executeMethod CopyPenaltySmokeTests.RunBatch
-logFile <log>` (không thêm `-quit`). Xuất `outputs/copy-penalty-preview.png` và
`outputs/copy-penalty-smoke.txt`, không lưu scene hoặc gọi AI thật.

Kiểm tra thủ công: tự luận 9 điểm dùng hai gợi ý còn 6, học lại xong vẫn khóa
chuyển câu; ảnh đủ số bản nhưng thiếu đề/lời giải vẫn khóa; đủ cả hai mới sang
câu tiếp theo. Trắc nghiệm có hai câu sai phải xử lý câu thứ nhất gồm chép phạt
trước khi mở câu thứ hai; đóng/mở lại nhật ký vẫn giữ đúng tiến độ.
