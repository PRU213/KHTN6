// These helpers are appended to the original Apps Script in AppsScript/Code.gs.
function essayNormalize_(value) {
  return String(value || '').normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/[đĐ]/g, 'd').toLowerCase().trim();
}
function getEssayQuestions(e) {
  var params = (e && e.parameter) || {};
  var secret = PropertiesService.getScriptProperties().getProperty('PLANET_SERVER_SECRET');
  if (secret && params.serverSecret !== secret) return jsonOut({ success: false, message: 'Unauthorized essay access' });
  var sheet = SpreadsheetApp.openById(SPREADSHEET_ID).getSheetByName('TuLuan');
  if (!sheet) return jsonOut({ success: false, message: "Không tìm thấy tab TuLuan" });
  var rows = sheet.getDataRange().getValues();
  if (!rows.length) return jsonOut({ success: false, message: 'TuLuan chưa có hàng tiêu đề' });
  var columns = {};
  rows[0].forEach(function(name, index) { columns[essayNormalize_(name)] = index; });
  var required = ['CauHoiID', 'monID', 'difficulty', 'Question', 'DapAnThamKhao', 'HuongDanChamAI', 'DiemToiDa', 'TrangThai'];
  for (var k = 0; k < required.length; k++) {
    if (columns[essayNormalize_(required[k])] === undefined) return jsonOut({ success: false, message: 'TuLuan thiếu cột ' + required[k] });
  }
  function cell(row, name) { var index = columns[essayNormalize_(name)]; return index === undefined ? '' : String(row[index] == null ? '' : row[index]).trim(); }
  var difficulties = String(params.difficulty || '').split(',').map(essayNormalize_).filter(Boolean);
  var subject = essayNormalize_(params.subject || params.mon || '');
  var questions = [];
  for (var i = 1; i < rows.length; i++) {
    var row = rows[i];
    if (essayNormalize_(cell(row, 'TrangThai')) !== 'da duyet') continue;
    var monId = cell(row, 'monID'), difficulty = cell(row, 'difficulty');
    if (subject && essayNormalize_(monId) !== subject) continue;
    if (difficulties.length && difficulties.indexOf(essayNormalize_(difficulty)) < 0) continue;
    var image = cell(row, 'HinhAnh');
    questions.push({ id: cell(row, 'CauHoiID'), monId: monId, topic: cell(row, 'Chương/Chủ đề'), difficulty: difficulty,
      question: cell(row, 'Question'), imageUrl: essayNormalize_(image) === 'none' ? '' : image,
      sampleAnswer: cell(row, 'DapAnThamKhao'), rubric: cell(row, 'HuongDanChamAI'), maxPoints: Number(cell(row, 'DiemToiDa')) });
  }
  return jsonOut({ success: true, questionType: 'essay', count: questions.length, questions: questions });
}

function essayHistorySheet_() {
  var ss = SpreadsheetApp.openById(SPREADSHEET_ID);
  var sheet = ss.getSheetByName('BaiNopTuLuan') || ss.insertSheet('BaiNopTuLuan');
  var headers = ['SubmissionID', 'MatchID', 'Username', 'CauHoiID', 'monID', 'difficulty', 'BaiLamText', 'CoAnh', 'AINhanDang', 'Diem', 'DiemToiDa', 'NhanXetAI', 'DapAnAI', 'NgayNop'];
  if (sheet.getLastRow() === 0) { sheet.getRange(1, 1, 1, headers.length).setValues([headers]); sheet.setFrozenRows(1); }
  var existing = sheet.getRange(1, 1, 1, headers.length).getValues()[0];
  if (headers.some(function(name, index) { return existing[index] !== name; })) throw new Error('BaiNopTuLuan đang có cấu trúc khác. Hãy đổi tên tab cũ để giữ dữ liệu, rồi chạy setupPlanetEssay lại.');
  return sheet;
}
function setupPlanetEssay() {
  essayHistorySheet_();
  Logger.log('Đã chuẩn bị BaiNopTuLuan. Không thay đổi dữ liệu câu hỏi hoặc các tab khác.');
}
function saveEssaySubmission(p) {
  var secret = PropertiesService.getScriptProperties().getProperty('PLANET_SERVER_SECRET');
  if (!secret || p.serverSecret !== secret) return jsonOut({ success: false, message: 'Unauthorized essay submission' });
  var record = JSON.parse(p.record || '{}');
  if (!record.submissionId || !record.questionId || !Number.isFinite(record.score) || record.score < 0 || record.score > 10)
    return jsonOut({ success: false, message: 'Invalid essay record' });
  function safe(value, length) { var text = String(value || '').slice(0, length || 2000); return /^[\s]*[=+@-]/.test(text) ? "'" + text : text; }
  var lock = LockService.getScriptLock(); lock.waitLock(10000);
  try {
    var sheet = essayHistorySheet_();
    if (sheet.getLastRow() > 1) {
      var found = sheet.getRange(2, 1, sheet.getLastRow() - 1, 1).createTextFinder(String(record.submissionId)).matchEntireCell(true).useRegularExpression(false).findNext();
      if (found) return jsonOut({ success: true, duplicate: true });
    }
    sheet.appendRow([safe(record.submissionId, 120), safe(record.matchId, 100), safe(record.username, 100), safe(record.questionId, 100),
      safe(record.subject, 50), safe(record.difficulty, 30), safe(record.answer), Boolean(record.hasImage), safe(record.transcription), record.score, 10,
      safe(record.feedback, 500), safe(record.suggestedAnswer, 1000), new Date()]);
    SpreadsheetApp.flush(); return jsonOut({ success: true });
  } finally { lock.releaseLock(); }
}
// Images are graded directly through PlanetServer, not uploaded to Drive.
function saveEssayImage() {
  return { success: false, message: 'Ảnh bài làm được gửi trực tiếp tới máy chủ Planet để AI chấm; API này không lưu ảnh lên Drive.' };
}
