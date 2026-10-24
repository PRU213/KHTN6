/* Standalone Apps Script web app. Deploy with Execute as: Me; Who has access: Anyone.
 * Every write requires a teacher session token issued after server-side account validation.
 * Does not replace the existing gameplay web app. See README.md beside this file.
 */
const TEACHER_BOOK_ID = '1DcJwlN_fUDdcaQ6IfnlkMJApgWuEUqMhyZv9Kn8vfWo';
const TEACHER_SESSION_SECONDS = 3600;

function doGet(e) {
  // Public metadata lets Unity verify the destination before transmitting credentials.
  // No account information or session tokens are exposed by this read-only endpoint.
  return json_({
    success: true,
    service: 'KHTN6 Teacher API',
    version: 1,
    workbookId: TEACHER_BOOK_ID,
    actions: ['teacherCapabilities', 'teacherLogin', 'saveQuestion', 'saveEssay']
  });
}

function doPost(e) {
  try {
    if (!e || !e.postData || e.postData.contents.length > 6000000) throw new Error('Nội dung gửi không hợp lệ.');
    const p = JSON.parse(e.postData.contents);
    if (p.action === 'teacherLogin') return json_(teacherLogin_(p));
    const session = teacherSession_(p.token);
    if (p.action === 'saveQuestion') return json_(saveQuestion_(p.question, session));
    if (p.action === 'saveEssay') return json_(saveEssay_(p.essay, session));
    throw new Error('Chức năng giáo viên không được hỗ trợ.');
  } catch (err) {
    return json_({ success: false, saved: false, message: err.message || 'Không thể xử lý yêu cầu.' });
  }
}

function teacherLogin_(p) {
  const username = String(p.username || '').trim();
  const password = String(p.password || '');
  if (!username || !password) throw new Error('Vui lòng nhập tài khoản và mật khẩu.');
  const cache = CacheService.getScriptCache();
  const key = 'attempt:' + digest_(username.toLowerCase());
  const attempts = Number(cache.get(key) || 0);
  if (attempts >= 8) throw new Error('Đã nhập sai nhiều lần. Vui lòng thử lại sau 10 phút.');
  const accounts = SpreadsheetApp.openById(TEACHER_BOOK_ID).getSheets()[0].getDataRange().getDisplayValues();
  const headers = accounts.shift().map(normalize_);
  const userCol = headers.indexOf('username'), passCol = headers.indexOf('password');
  const roleCol = headers.indexOf('role'), idCol = headers.indexOf('id');
  if ([userCol, passCol, roleCol, idCol].some(x => x < 0)) throw new Error('Không tìm thấy cấu trúc bảng tài khoản.');
  const account = accounts.find(row => String(row[userCol]).trim().toLowerCase() === username.toLowerCase()
    && String(row[passCol]).trim() === password
    && /^(teacher|giaovien)$/i.test(String(row[roleCol]).trim()));
  if (!account) {
    cache.put(key, String(attempts + 1), 600);
    throw new Error('Tài khoản giáo viên hoặc mật khẩu không đúng.');
  }
  cache.remove(key);
  const token = Utilities.getUuid() + Utilities.getUuid();
  cache.put('session:' + digest_(token), JSON.stringify({ id: account[idCol], username: account[userCol] }), TEACHER_SESSION_SECONDS);
  return { success: true, token: token, expiresInSeconds: TEACHER_SESSION_SECONDS, message: 'Đã xác thực giáo viên.' };
}

function teacherSession_(token) {
  if (!token || typeof token !== 'string') throw new Error('Vui lòng xác thực lại tài khoản giáo viên.');
  const raw = CacheService.getScriptCache().get('session:' + digest_(token));
  if (!raw) throw new Error('Phiên lưu câu hỏi đã hết hạn. Vui lòng xác thực lại giáo viên.');
  return JSON.parse(raw);
}

function saveQuestion_(q, session) {
  requireFields_(q, ['id', 'monID', 'chapter', 'difficulty', 'points', 'question', 'option_a', 'option_b', 'option_c', 'option_d', 'correct_option']);
  const points = positiveNumber_(q.points);
  const correct = String(q.correct_option).trim().toUpperCase();
  if (!/^[ABCD]$/.test(correct)) throw new Error('Đáp án đúng phải là A, B, C hoặc D.');
  const values = {
    id: q.id, monID: q.monID, 'Chương / Chủ đề': q.chapter, difficulty: q.difficulty,
    question: q.question, option_a: q.option_a, option_b: q.option_b, option_c: q.option_c,
    option_d: q.option_d, correct_option: correct, explanation: q.explanation || '', points: points
  };
  return appendOnce_('Questions', 'id', q.id, values, digest_(JSON.stringify(values)), null);
}

function saveEssay_(q, session) {
  requireFields_(q, ['CauHoiID', 'monID', 'chapter', 'difficulty', 'DiemToiDa', 'TrangThai', 'NguonTao', 'Question', 'DapAnThamKhao', 'HuongDanChamAI']);
  const values = {
    CauHoiID: q.CauHoiID, monID: q.monID, 'Chương/Chủ đề': q.chapter, difficulty: q.difficulty,
    Question: q.Question, HinhAnh: q.HinhAnh || '', DapAnThamKhao: q.DapAnThamKhao,
    HuongDanChamAI: q.HuongDanChamAI, DiemToiDa: positiveNumber_(q.DiemToiDa),
    TrangThai: q.TrangThai, GiaoVienID: session.id, NguonTao: q.NguonTao,
    NgayTao: q.NgayTao || Utilities.formatDate(new Date(), 'Asia/Ho_Chi_Minh', 'yyyy-MM-dd HH:mm:ss')
  };
  const imageData = values.HinhAnh.startsWith('data:image/') ? values.HinhAnh : null;
  if (!imageData && values.HinhAnh && !/^https:\/\//i.test(values.HinhAnh)) throw new Error('Ảnh phải là ảnh đã chọn hoặc đường dẫn HTTPS.');
  // Hash the original image payload so network retries do not create a second question/image.
  return appendOnce_('TuLuan', 'CauHoiID', q.CauHoiID, values, digest_(JSON.stringify(values)), imageData);
}

function appendOnce_(sheetName, idHeader, id, values, fingerprint, imageData) {
  const lock = LockService.getScriptLock();
  if (!lock.tryLock(20000)) throw new Error('Dịch vụ đang lưu một câu hỏi khác. Vui lòng thử lại.');
  let imageFile = null;
  try {
    const sheet = SpreadsheetApp.openById(TEACHER_BOOK_ID).getSheetByName(sheetName);
    if (!sheet) throw new Error('Không tìm thấy bảng ' + sheetName + '.');
    const headers = sheet.getRange(1, 1, 1, sheet.getLastColumn()).getDisplayValues()[0];
    const normalized = headers.map(normalize_);
    Object.keys(values).forEach(k => { if (normalized.indexOf(normalize_(k)) < 0) throw new Error('Bảng thiếu cột ' + k + '.'); });
    const idCol = normalized.indexOf(normalize_(idHeader));
    const ids = sheet.getLastRow() > 1 ? sheet.getRange(2, idCol + 1, sheet.getLastRow() - 1, 1).getDisplayValues() : [];
    const existing = ids.findIndex(row => row[0] === String(id));
    const receiptKey = 'receipt:' + sheetName + ':' + String(id);
    const cache = CacheService.getScriptCache();
    if (existing >= 0) {
      if (cache.get(receiptKey) === fingerprint) return { success: true, saved: true, id: id, sheet: sheetName, message: 'Câu hỏi đã được lưu trước đó.' };
      throw new Error('Mã câu hỏi đã tồn tại. Hãy tạo câu hỏi mới hoặc kiểm tra hàng đã lưu trong Sheet.');
    }
    if (imageData) {
      imageFile = createQuestionImage_(imageData, id);
      values.HinhAnh = 'https://drive.google.com/uc?export=view&id=' + imageFile.getId();
    }
    const byHeader = {};
    Object.keys(values).forEach(k => byHeader[normalize_(k)] = values[k]);
    const row = headers.map(h => literal_(byHeader[normalize_(h)] === undefined ? '' : byHeader[normalize_(h)]));
    sheet.getRange(sheet.getLastRow() + 1, 1, 1, row.length).setValues([row]);
    SpreadsheetApp.flush();
    cache.put(receiptKey, fingerprint, 21600);
    return { success: true, saved: true, id: id, sheet: sheetName, message: 'Đã lưu câu hỏi.' };
  } catch (err) {
    // Keep files if a sheet write may already have succeeded; deleting them could break the saved row.
    throw err;
  } finally { lock.releaseLock(); }
}

function createQuestionImage_(data, id) {
  const m = /^data:(image\/(png|jpeg|jpg));base64,([A-Za-z0-9+/=\r\n]+)$/.exec(data);
  if (!m) throw new Error('Chỉ hỗ trợ ảnh PNG hoặc JPG.');
  const bytes = Utilities.base64Decode(m[3]);
  if (bytes.length > 4 * 1024 * 1024) throw new Error('Ảnh phải nhỏ hơn hoặc bằng 4 MB.');
  const properties = PropertiesService.getScriptProperties();
  let folderId = properties.getProperty('TEACHER_IMAGES_FOLDER');
  let folder;
  if (folderId) folder = DriveApp.getFolderById(folderId);
  else { folder = DriveApp.createFolder('KHTN6_Question_Images'); properties.setProperty('TEACHER_IMAGES_FOLDER', folder.getId()); }
  const file = folder.createFile(Utilities.newBlob(bytes, m[1] === 'image/jpg' ? 'image/jpeg' : m[1], String(id) + '.' + (m[2] === 'png' ? 'png' : 'jpg')));
  file.setSharing(DriveApp.Access.ANYONE_WITH_LINK, DriveApp.Permission.VIEW);
  return file;
}

function requireFields_(value, keys) {
  if (!value || typeof value !== 'object') throw new Error('Thiếu nội dung câu hỏi.');
  keys.forEach(k => { if (value[k] === undefined || value[k] === null || String(value[k]).trim() === '') throw new Error('Vui lòng nhập trường ' + k + '.'); });
  Object.keys(value).forEach(k => { if (k !== 'HinhAnh' && String(value[k] || '').length > 45000) throw new Error('Nội dung trường ' + k + ' quá dài.'); });
}
function positiveNumber_(value) {
  const n = Number(String(value).replace(',', '.'));
  if (!Number.isFinite(n) || n <= 0) throw new Error('Điểm câu hỏi phải lớn hơn 0.');
  return n;
}
function literal_(value) {
  // setValues interprets leading '=' as a formula; question content must remain literal.
  return typeof value === 'string' && value.startsWith('=') ? "'" + value : value;
}
function normalize_(value) { return String(value).trim().toLowerCase().replace(/[\s/_]/g, ''); }
function digest_(value) { return Utilities.base64EncodeWebSafe(Utilities.computeDigest(Utilities.DigestAlgorithm.SHA_256, value, Utilities.Charset.UTF_8)); }
function json_(value) { return ContentService.createTextOutput(JSON.stringify(value)).setMimeType(ContentService.MimeType.JSON); }
