# Học lại tự luận Vật lý / Hóa học

Phạm vi: Football, Sheep, Cave và các câu Physics/Chemistry trong Planet.
Không sửa nội dung, gameplay, chấm điểm hoặc màn tự luận Biology. Trắc nghiệm ngoài phạm vi.

1. Đăng ký câu chưa đạt điểm kiến thức tối đa thành một mục học lại trên server,
   kể cả hết giờ/chưa nộp. Điểm ban đầu giữ nguyên. Giấu nhận xét/lời giải có thể
   tiết lộ đáp án, cung cấp nút học từng bước.
2. AI tạo kế hoạch 3–8 bước. Server giữ đáp án kiểm tra, bước hiện tại và trạng thái.
   Sai/không biết: giải thích đúng bước hiện tại và hỏi câu kiểm tra lại.
   Chỉ đáp án đúng mới qua bước; hoàn thành tất cả mới mở tổng kết.
3. Lưu bền mục học lại, trạng thái và từng lần trả lời; tiếp tục được từ nhật ký.
   Học sinh xem mục của mình; giáo viên xem chi tiết học sinh đang chọn, chỉ đọc.
4. Kiểm thử state machine, retry/concurrency/restart, không lộ lời giải, giới hạn môn;
   chạy kiểm thử server hiện có và kiểm tra biên dịch Unity; review thay đổi.

Kế hoạch đã đối chiếu các điểm gọi trong FootballEssayController,
PlanetDuelController, HomeHistoryController, TeacherScreenController, football.mjs
và server.mjs. Dùng UI runtime như các màn kết quả hiện tại, không chỉnh scene.
Lưu server cần thư mục bền trên host; cập nhật server là bước vận hành sau khi kiểm tra.

## Kết quả kiểm tra

- Đã triển khai các bước 1–4; thêm xác thực riêng cho nhật ký, học sinh chỉ thao tác
  mục của mình, giáo viên chỉ đọc. Không sửa login hoặc scripts/scenes Biology.
- 86/86 test các module liên quan đạt; toàn bộ 130 test có 116 đạt và 14 lỗi.
  Đã chạy lại bản HEAD trong thư mục tách biệt: 116 test gốc, 102 đạt/14 lỗi giống
  hệt, thuộc Apps Script/lịch sử cũ. Không mở rộng phạm vi sang sửa các lỗi này.
- Biên dịch C# với Unity references: 0 lỗi. Unity Play Mode smoke test đạt:
  tạo UI, ô trả lời, scroll, deserialize các trường response kế thừa; ảnh giao diện
  đã xem tại outputs/essay-tutor-preview.png. Review đã sửa retry Planet,
  quyền truy cập, username hoa/thường và đóng/mở lại lúc xác minh mật khẩu.
  Smoke test cuối cũng xác nhận token nhật ký được xóa khi quay về màn đăng nhập.
- Chưa triển khai lên server đang host và chưa gọi Gemini thật trong kiểm thử.
  Vận hành: xem PlanetServer/ESSAY-TUTOR.md, đặc biệt TUTOR_DATA_DIR trên ổ bền.

## Điều chỉnh bắt buộc học lại (30/10/2026)

- Đã tái hiện server Render cũ: `/health` không có tutorVersion, `/tutor/list`
  trả lỗi phiên đối kháng thay vì API học lại. Nút cũ thiếu tutorId nên chỉ tải lại
  trạng thái và đặt lỗi sau bảng kết quả, gây cảm giác không hoạt động.
- Khóa Next tại Unity và `/football/next` đến khi trạng thái EssayTutor thật sự
  completed; áp dụng cả câu cuối. Tự mở học lại lần đầu khi kết quả câu sai có ID.
- Thông báo phiên bản/lỗi tải lại hiện ngay trong bảng kết quả; hỗ trợ đóng/mở
  học lại và cập nhật trạng thái hoàn thành qua polling hiện có.
- 93/93 kiểm thử liên quan đạt, gồm gửi cờ hoàn thành giả, sai ở bước kiểm tra,
  lỗi lưu mục học lại, câu cuối và bài làm đúng hoàn toàn. C# biên dịch 0 lỗi.
- Đã đưa đúng 10 file backend/.gitignore lên PRU213/KHTN6 main bằng GitHub CLI,
  commit c0b0dd3f843ed63fe00d2eb94cc972a7ce745447. Đã kiểm tra Render thực tế trả
  tutorVersion=2, mandatoryPracticeTutor=true, API nhật ký trả đúng yêu cầu đăng nhập.
- Smoke test Unity riêng xác nhận Next khóa khi sai, lỗi server cũ hiện ngay trên
  bảng kết quả và Next mở khi completed, cho cả Football/Sheep/Cave. Không dùng
  phiên Unity của người dùng, không sửa scene hoặc logic Biology.

## Sửa ô mật khẩu không nhập được — 30/10/2026

- `EssayInputState.Clear(password)` đặt readOnly=true, nhưng password chưa được
  Configure như ô trả lời, nên chọn ô không bao giờ mở lại quyền nhập.
- Gắn Configure trước khi bật field; đặt single-line, plain text cho input mật khẩu.
- Kiểm thử trước sửa tái hiện “Selecting password leaves it read-only”. Sau sửa
  đã qua raycast/click, chọn, ProcessEvent nhập ký tự và đóng/mở lại, cùng các
  kiểm tra khóa Next trước đó. Không gọi server hay dùng mật khẩu thật.

## Sửa ô trả lời sau phản hồi AI — 30/10/2026

- Ô trả lời được bật lại nhưng Clear vẫn để readOnly=true. Việc mở khóa dựa vào
  callback chọn ô bị mất khi Unity nạp lại script khiến học sinh không nhập được.
- TutorUI quản lý readOnly cùng interactable theo trạng thái bài học, không còn
  dùng callback focus để quyết định quyền nhập. Không sửa helper dùng chung game.
- Trước sửa đã tái hiện “Answer stays read-only after AI response 0”. Sau sửa
  kiểm tra raycast/click và gõ được qua ba phản hồi liên tiếp, cả lúc mất callback
  runtime; trạng thái chờ AI/giáo viên/hoàn thành vẫn khóa đúng. Không đổi backend.

## Nút đi tiếp trên màn hoàn thành — 30/10/2026

- Thêm nút Câu tiếp theo ngay trên màn tổng kết học lại; câu cuối hiện Xem tổng kết.
  Nối đúng FootballEssayController đang chạy bằng ID học lại, không dựa vào một
  callback tạm hoặc yêu cầu học sinh đóng cửa sổ để tìm nút phía dưới.
- Gửi yêu cầu next để server kiểm tra hoàn thành; không phụ thuộc snapshot polling
  phía game còn giữ tutorCompleted=false trong giây vừa hoàn thành.
- Nhật ký không có lượt chơi tương ứng hiện Về nhật ký; giáo viên và mục chưa
  hoàn thành không có nút điều khiển luyện tập.
- Unity integration smoke test đã click qua raycast/PointerEvent, chạy HTTP client
  thật với fixture localhost và kiểm tra câu 2 hiển thị, câu cuối ra tổng kết,
  không gửi trùng hoặc chuyển nhầm ID. Không đổi server hoặc phần Biology.
