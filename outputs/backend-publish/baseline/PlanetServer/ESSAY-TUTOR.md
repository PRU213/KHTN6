# Học lại tự luận Lí / Hóa với thầy AI

## Hành vi

- Chỉ Physics/Chemistry: Football, Sheep, Cave và các câu Lí/Hóa trong Planet.
- Điểm **kiến thức trước khi trừ gợi ý < 10/10** tạo mục học lại; hết giờ cũng tạo mục.
  Bài đúng hoàn toàn nhưng bị trừ điểm do dùng gợi ý không bị coi là làm sai.
- Màn kết quả câu cần học lại chỉ hiện điểm và bài làm AI đọc, ẩn nhận xét/đáp án có
  thể tiết lộ lời giải. Chọn **Học lại từng bước / Hỏi AI**. Planet học lại sau trận.
- AI tạo 3–8 bước phù hợp bài. Mỗi bước có câu hỏi, kiến thức để giải thích khi sai,
  và câu kiểm tra lại. Sai hoặc **Em chưa biết**: giữ bước, giải thích, kiểm tra lại.
  Chỉ trả lời đúng mới qua bước. Không có nút bỏ qua hay xin lời giải toàn bộ.
- Qua hết các bước mới mở lời giải tổng kết; điểm bài nộp ban đầu giữ nguyên.
- Trong luyện tập Football/Sheep/Cave, câu sai tự mở màn học lại. Nút chuyển câu
  bị khóa đến khi server xác nhận hoàn thành toàn bộ các bước; áp dụng cả câu cuối
  trước tổng kết. Đóng màn học lại không mở khóa, có thể mở lại để tiếp tục.
  Server cũng chặn `/football/next`, không tin cờ hoàn thành do client gửi.
- Nếu Unity mới nhưng server cũ, bảng kết quả hiện rõ lỗi phiên bản, không cho
  chuyển câu. Kiểm tra `/health`: bản mới phải có `tutorVersion: 2` và
  `mandatoryPracticeTutor: true`. Cần cập nhật server, không chỉ bấm deploy lại
  cùng commit cũ. Phiên luyện tập mở trước deploy cần thoát và bắt đầu lại.
- Đóng màn có thể tiếp tục từ **Lịch sử → Nhật ký học lại Lí / Hóa**.
- Giáo viên: **Tiến độ → chọn học sinh → Nhật ký học lại Lí / Hóa**. Xem trạng thái
  chưa học/đang học/hoàn thành, số bước, câu hỏi, câu trả lời, đúng/sai và thời gian.
  Giáo viên chỉ xem, không trả lời thay học sinh qua API.
- Biology giữ nguyên luồng chấm, kết quả và hỏi AI cũ. Không thay đổi trắc nghiệm.

## Cấu hình và chạy

1. Cập nhật server Node và Unity scripts cùng phiên bản. Giữ `GEMINI_API_KEY` và
   `GEMINI_MODEL` hiện có; không đưa khóa vào Unity. Không cần sửa Apps Script.
2. Nhật ký lưu file JSON riêng mỗi mục trên server, gồm cả kế hoạch và lượt trả lời.
   Đặt `TUTOR_DATA_DIR` trỏ tới **ổ đĩa bền** của host. Mặc định `PlanetServer/data/tutor`.
   Với Render cần mount persistent disk rồi trỏ biến này vào thư mục mount. Filesystem
   tạm của host có thể bị xóa khi redeploy; không dùng nó cho nhật ký thực tế.
3. `npm start` để khởi động lại server. Giữ `PlanetDuelSettings.json` trỏ tới đúng server.
4. Lần đầu mở nhật ký/học lại, nhập mật khẩu tài khoản hiện tại. Server xác minh
   với tab Users đang dùng đăng nhập; yêu cầu các cột `Username`, `Password`, `Role`.
   Có thể đặt `TUTOR_USERS_URL` nếu dùng nguồn Users CSV khác. Không có đăng nhập
   offline cho nhật ký. Token chỉ giữ trong RAM client, có hạn 8 giờ; server khởi động
   lại cần xác minh lại. Không ghi mật khẩu/token vào file nhật ký.
5. Sao lưu thư mục nhật ký cùng dữ liệu ứng dụng. Không chạy nhiều process ghi chung
   thư mục; implementation hiện dùng một Node process như Arena. File hỏng sẽ làm
   server báo lỗi khởi động thay vì bỏ qua và mất tiến trình âm thầm.

`/tutor/login` cấp bearer token theo vai trò xác minh. `/tutor/list` và `/tutor/state`
cho phép student xem của mình, teacher xem học sinh. `/tutor/start` và `/tutor/answer`
chỉ student của bản ghi. Server quản lý bước/revision/requestId; retry không thêm
lượt hoặc qua bước hai lần. Nội dung nhận xét do Gemini sinh vẫn cần giáo viên
đánh giá chất lượng; không xem việc AI chấp nhận câu trả lời là chứng nhận năng lực.
Danh tính gắn với bài nộp gốc vẫn theo username từ game hiện tại; thay đổi này không
thay thế hệ thống xác thực các API chấm/ghép trận cũ.

## Kiểm tra

- `node --test tutor.test.mjs`: state machine, không tiết lộ lời giải trước hoàn thành,
  retry, khóa yêu cầu đồng thời, lỗi AI/file, restart, phân quyền và giới hạn môn.
- `npm test`: toàn bộ kiểm thử server. Có baseline log riêng nếu test cũ thất bại.
- Unity: `-batchmode -projectPath <project> -executeMethod EssayTutorSmokeTests.RunBatch
  -logFile <log>` (không thêm `-quit`); fixture offline kiểm tra UI/JSON, xuất ảnh
  `outputs/essay-tutor-preview.png`, không gọi AI hoặc lưu scene.

Để nghiệm thu AI thật: làm sai một câu Lí và một câu Hóa, chọn học lại, trả lời sai,
chọn chưa biết, trả lời đúng, đóng/mở lại, hoàn thành, rồi đăng nhập giáo viên để xem
cùng nhật ký. Kiểm tra một câu Biology trong Planet vẫn hiện kết quả như trước.
