# Lưu nhật ký học lại/chép phạt trên Google Sheet

Mã đã chuẩn bị trong workspace, chưa cập nhật Apps Script/Render thay bạn.
Làm theo thứ tự Apps Script trước, Render sau. Không cần chép lại bài đã viết.

## 1. Sao lưu

Sao lưu code Apps Script hiện tại và Google Sheet. Không xóa Mã.gs, doGet/doPost,
các tab dữ liệu cũ hoặc các Script Properties đang có.

Nhật ký có câu trả lời học sinh và tiêu chí chấm. Tab TutorJournal **không được
publish lên web hoặc chia sẻ công khai**. Nếu file dữ liệu game đang publish toàn
bộ workbook, tạo một Google Sheet riêng tư riêng cho nhật ký rồi dùng ID của file
đó ở bước 3. Không chỉ ẩn tab để bảo vệ dữ liệu.

## 2. Thêm file và nối vào Mã.gs

Trong project Apps Script hiện có, bấm dấu **+ → Script**, đặt tên `TutorJournal`.
Dán **toàn bộ nội dung** `AppsScript/TutorJournal.gs` vào file mới rồi lưu.
File mới không chứa doGet/doPost, không thay Mã.gs.

Trong **Mã.gs**, tìm dòng `function doPost(e) {`. Chèn ngay sau dấu `{` đoạn này,
trước các nhánh xử lý đang có:

```js
  var tutorParams = (e && e.parameter) ? e.parameter : {};
  if (tutorParams.action === 'tutorJournalLoad' ||
      tutorParams.action === 'tutorJournalSave') {
    return tutorJournalApi_(tutorParams);
  }
```

Nếu đã có nhánh gọi `tutorJournalApi_(p)` thì giữ nhánh đó, không thêm lần nữa.
Chỉ có **một** hàm doPost và **một** hàm doGet trong project. Các file Code.gs và
Ma-da-sua.gs trong repo là các bản chính thay thế nhau; đừng dán cả hai vào project.

## 3. Script Properties và tạo tab

Trong Apps Script, mở **Project Settings → Script Properties**:

| Property | Giá trị |
|---|---|
| `TUTOR_SPREADSHEET_ID` | ID của Sheet dùng lưu nhật ký, phần nằm giữa `/d/` và `/edit` trong URL |
| `PLANET_SERVER_SECRET` | Giữ khóa hiện có; phải trùng với `APPS_SCRIPT_SECRET` trên Render |

Nếu chưa có khóa, tự đặt một chuỗi bí mật dài, ngẫu nhiên và dùng cùng giá trị
ở hai nơi. Không gửi khóa vào chat, không đưa vào Unity/GitHub. Không đổi khóa
hiện có nếu các chức năng khác đang dùng nó.

Quay lại Editor, chọn hàm **setupTutorJournal → Run**, cấp quyền cho chính script
này nếu Google yêu cầu. Chủ sở hữu chạy web app phải có quyền chỉnh sửa Sheet đã
chọn. Kết quả là tab mới `TutorJournal`, có sáu cột đầu:

`RecordID | Username | Revision | Parts | SHA256 | UpdatedAt`

Các cột dữ liệu tiếp theo được tạo tự động. Không tự sửa các ô dữ liệu đã lưu.
Chuỗi `u:` và `b:` trong ô là định dạng lưu nội bộ để không bị hiểu thành công thức.

## 4. Cập nhật deployment Apps Script cũ

**Deploy → Manage deployments → chọn web app đang dùng → Edit (bút chì) →
Version: New version → Deploy**. Cập nhật deployment hiện có để giữ URL `/exec`
đang cấu hình trên Render. Giữ cấu hình quyền chạy đang dùng, không tự mở thêm
quyền chia sẻ Sheet. Chỉ lưu code mà không deploy thì backend vẫn gọi bản cũ.

## 5. Cập nhật Render

Commit/push các file backend mới và sửa đổi (đặc biệt `tutor-sheet-store.mjs`,
`tutor.mjs`, `server.mjs`) lên nhánh mà service Render đang theo dõi. Không push
`.env`, thư mục `data` hoặc các khóa bí mật.

Vào service `khtn6-server` → **Environment**, thêm/kiểm tra:

| Biến | Giá trị |
|---|---|
| `TUTOR_STORAGE` | `googleSheets` (đúng chữ hoa/thường) |
| `APPS_SCRIPT_URL` | URL `/exec` của deployment Apps Script vừa cập nhật |
| `APPS_SCRIPT_SECRET` | Trùng `PLANET_SERVER_SECRET` ở bước 3 |

Giữ nguyên GEMINI_API_KEY và các biến hiện có. Lưu thay đổi, rồi deploy commit
mới nếu Render chưa tự deploy. Không cần nâng gói Render hoặc mua persistent disk.

Khi Live, mở `https://khtn6-server.onrender.com/health`, phải có:

```json
{
  "copyPenaltyVersion": 2,
  "journalStorage": "googleSheets",
  "journalReady": true
}
```

Nếu vẫn là `local` thì chưa bật TUTOR_STORAGE hoặc đang chạy commit cũ. Nếu deploy
thất bại do nhật ký, kiểm tra đúng URL /exec, đã New version, đã setup tab và hai
khóa trùng nhau. Không bỏ cổng kiểm tra hay chuyển về local để che lỗi.

## 6. Kiểm tra không mất tiến độ

1. Dừng Play Unity, chờ biên dịch, Play lại và đăng nhập tài khoản thử nghiệm.
2. Làm một câu cần học lại, trả lời đúng một bước; xem có bản ghi trong TutorJournal.
3. Stop/Play lại, mở Nhật ký: phải còn câu và đúng bước đã học.
4. Với tài khoản thử nghiệm, restart Render rồi đăng nhập lại nhật ký: tiến độ
   vẫn phải còn, vì file tạm không còn là bản lưu chính.
5. Chọn ảnh chép phạt rồi Stop/Play và mở lại cùng mục: bản nháp ảnh được tải lại
   trên đúng thiết bị/tài khoản. Không lưu ảnh gốc lên Sheet.
6. Nộp bộ ảnh đủ: trạng thái hoàn thành phải xuất hiện trên Sheet và giữ nguyên
   sau restart. Chỉ khi đó mới được chuyển câu/mở lượt mới.

Nhật ký được xác nhận lưu trên Sheet trước khi backend trả thành công. Lỗi Sheet
sẽ báo lỗi và giữ thay đổi chờ thử lại; không âm thầm coi là đã lưu. Mỗi bản ghi
có revision/checksum; gửi trùng không tạo hai bản ghi, tiến trình cũ không ghi đè
lên bản mới. Chạy một backend process cho service này như cấu hình hiện tại.

Dữ liệu cũ còn trong thư mục local sẽ được chuyển lên Sheet khi bật chế độ mới.
Những bản ghi đã mất khỏi cả server và bản sao không thể tự phục hồi đầy đủ từ
ảnh chép tay. Không tự đánh dấu hoàn thành cho bài bị mất.
