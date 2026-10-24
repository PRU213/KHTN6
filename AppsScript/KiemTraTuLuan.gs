// Chỉ đọc dữ liệu và ghi nhật ký, không cập nhật Sheet hay bản triển khai.
// Dán vào một file .gs mới, chọn kiemTraTuLuan rồi bấm Chạy.
function kiemTraTuLuan() {
  var secret = PropertiesService.getScriptProperties().getProperty('PLANET_SERVER_SECRET');
  var params = { action: 'essayQuestions' };
  if (secret) params.serverSecret = secret;

  function check(label, read) {
    try {
      var data = JSON.parse(read());
      Logger.log(label + ': ' + JSON.stringify({
        success: data.success,
        questionType: data.questionType || '',
        count: Array.isArray(data.questions) ? data.questions.length : null,
        message: String(data.message || data.error || '')
      }));
    } catch (error) {
      // Không ghi URL có secret hoặc toàn bộ dữ liệu câu hỏi vào nhật ký.
      var detail = String(error && error.message ? error.message : error);
      if (secret) detail = detail.split(secret).join('[AN_SECRET]');
      detail = detail.replace(/serverSecret=[^\s&"<>]*/g, 'serverSecret=[AN_SECRET]');
      Logger.log(label + ': LOI ' + String(error && error.name || 'Error') + ': ' + detail.slice(0, 1200));
    }
  }

  check('1. HAM TU LUAN TRONG MA HIEN TAI', function() {
    return getEssayQuestions({ parameter: params }).getContent();
  });
  check('2. DOGET TRONG MA HIEN TAI', function() {
    return doGet({ parameter: params }).getContent();
  });
  check('3. URL GAME DANG GOI', function() {
    var url = 'https://script.google.com/macros/s/AKfycbxLHvShuipgPg7x28IikDmzU7wHoE2e3ai9rhuoEnN50UsFUAWIPTG_nneHOxz-YmaY/exec'
      + '?action=essayQuestions&_check=' + Date.now();
    if (secret) url += '&serverSecret=' + encodeURIComponent(secret);
    var response = UrlFetchApp.fetch(url, { muteHttpExceptions: true });
    Logger.log('3. HTTP STATUS: ' + response.getResponseCode());
    var content = response.getContentText();
    try { JSON.parse(content); } catch (error) {
      throw new Error('URL trả về nội dung không phải JSON (HTTP ' + response.getResponseCode() + '). Kiểm tra URL và quyền truy cập ứng dụng web.');
    }
    return content;
  });
}
