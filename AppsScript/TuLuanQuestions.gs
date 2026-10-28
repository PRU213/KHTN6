// Read-only TuLuan API. Add this file without replacing the history/rank files.
function planetEssayQuestionKey_(value) {
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
  rows[0].forEach(function(name, index) { columns[planetEssayQuestionKey_(name)] = index; });
  var required = ['CauHoiID', 'monID', 'difficulty', 'Question', 'DapAnThamKhao', 'HuongDanChamAI', 'DiemToiDa', 'TrangThai'];
  for (var k = 0; k < required.length; k++) {
    if (columns[planetEssayQuestionKey_(required[k])] === undefined) return jsonOut({ success: false, message: 'TuLuan thiếu cột ' + required[k] });
  }
  function cell(row, name) { var index = columns[planetEssayQuestionKey_(name)]; return index === undefined ? '' : String(row[index] == null ? '' : row[index]).trim(); }
  var difficulties = String(params.difficulty || '').split(',').map(planetEssayQuestionKey_).filter(Boolean);
  var subject = planetEssayQuestionKey_(params.subject || params.mon || '');
  var questions = [];
  for (var i = 1; i < rows.length; i++) {
    var row = rows[i];
    if (planetEssayQuestionKey_(cell(row, 'TrangThai')) !== 'da duyet') continue;
    var monId = cell(row, 'monID'), difficulty = cell(row, 'difficulty');
    if (subject && planetEssayQuestionKey_(monId) !== subject) continue;
    if (difficulties.length && difficulties.indexOf(planetEssayQuestionKey_(difficulty)) < 0) continue;
    var image = cell(row, 'HinhAnh');
    questions.push({ id: cell(row, 'CauHoiID'), monId: monId, topic: cell(row, 'Chương/Chủ đề'), difficulty: difficulty,
      question: cell(row, 'Question'), imageUrl: planetEssayQuestionKey_(image) === 'none' ? '' : image,
      sampleAnswer: cell(row, 'DapAnThamKhao'), rubric: cell(row, 'HuongDanChamAI'), maxPoints: Number(cell(row, 'DiemToiDa')) });
  }
  return jsonOut({ success: true, questionType: 'essay', count: questions.length, questions: questions });
}

