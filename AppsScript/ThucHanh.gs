// Add this file to the existing Apps Script project. Uses SPREADSHEET_ID
// and jsonOut already defined in Ma.gs / Code.gs. Does not define doGet.
function practiceKey_(value) {
  return String(value == null ? '' : value).normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '').replace(/[đĐ]/g, 'd')
    .toLowerCase().trim();
}

function practiceSubject_(value) {
  var key = practiceKey_(value);
  if (['vat ly', 'vat li', 'physics', 'phy'].indexOf(key) >= 0) return 'PHY';
  if (['hoa hoc', 'chemistry', 'chm'].indexOf(key) >= 0) return 'CHM';
  return '';
}

function practiceReadTable_(ss, name, required) {
  var sheet = ss.getSheetByName(name);
  if (!sheet) throw new Error('Không tìm thấy tab ' + name);
  var rows = sheet.getDataRange().getValues();
  var columns = {};
  (rows[0] || []).forEach(function(value, i) {
    var key = practiceKey_(value);
    if (!key) return;
    if (columns[key] !== undefined) throw new Error(name + ' trùng tên cột ' + value);
    columns[key] = i;
  });
  required.forEach(function(field) {
    if (columns[practiceKey_(field)] === undefined) throw new Error(name + ' thiếu cột ' + field);
  });
  return {
    rows: rows.slice(1),
    cell: function(row, field) {
      var i = columns[practiceKey_(field)];
      return i === undefined ? '' : String(row[i] == null ? '' : row[i]).trim();
    }
  };
}

// A Drive viewing link is not an image response. Keep the original URL and
// expose a download URL plus file ID for the future game image loader.
// This helper does not change permissions or fetch private Drive files.
function practiceImage_(value) {
  var url = String(value || '').trim();
  if (!url) return { originalUrl: '', url: '', fileId: '', ready: false };
  if (!/^https:\/\//i.test(url)) return { originalUrl: url, url: '', fileId: '', ready: false };
  var drive = /^https:\/\/drive\.google\.com\//i.test(url);
  var match = drive && (url.match(/\/file\/d\/([A-Za-z0-9_-]+)/) || url.match(/[?&]id=([A-Za-z0-9_-]+)/));
  if (!drive) return { originalUrl: url, url: url, fileId: '', ready: true };
  if (!match) return { originalUrl: url, url: '', fileId: '', ready: false };
  var key = url.match(/[?&]resourcekey=([^&#]+)/i);
  return { originalUrl: url,
    url: 'https://drive.google.com/uc?export=download&id=' + match[1] + (key ? '&resourcekey=' + key[1] : ''),
    fileId: match[1], ready: true };
}

function practiceLesson_(table, row) {
  var cell = table.cell;
  var id = cell(row, 'ThucHanhID');
  var chapter = cell(row, 'ChuongID');
  if (!/^TH_(PHY|CHM)_\d{2}_\d{2}$/.test(id)) throw new Error('Mã bài thực hành không hợp lệ: ' + id);
  if (!/^CH_(PHY|CHM)_\d{2}$/.test(chapter)) throw new Error('ChuongID không hợp lệ ở bài ' + id);
  var subject = practiceSubject_(cell(row, 'MonID'));
  if (!subject || chapter.split('_')[1] !== subject || id.split('_')[1] !== subject || id.split('_')[2] !== chapter.split('_')[2])
    throw new Error('Môn hoặc chương không khớp mã bài ' + id);
  var config;
  try { config = JSON.parse(cell(row, 'CauHinhJSON')); }
  catch (error) { throw new Error('CauHinhJSON không hợp lệ ở bài ' + id); }
  if (!config || typeof config !== 'object' || Array.isArray(config)) throw new Error('CauHinhJSON cần là đối tượng ở bài ' + id);
  var title = cell(row, 'TenBai'), simulation = cell(row, 'LoaiMoPhong');
  if (!title || !simulation) throw new Error('Bài ' + id + ' thiếu TenBai hoặc LoaiMoPhong');
  var cover = practiceImage_(cell(row, 'AnhDaiDien'));
  var instruction = practiceImage_(cell(row, 'AnhHuongDan'));
  return {
    id: id, monId: cell(row, 'MonID'), chapterId: chapter,
    chapterTitle: cell(row, 'TenChuong'), order: Number(cell(row, 'ThuTu')) || 0,
    title: title, objective: cell(row, 'MucTieu'), simulationType: simulation,
    equipment: cell(row, 'DungCu'), predictionQuestion: cell(row, 'CauHoiDuDoan'),
    instructions: cell(row, 'HuongDanTomTat'), expectedObservation: cell(row, 'KetQuaMongDoi'),
    conclusionQuestion: cell(row, 'CauHoiKetLuan'), referenceAnswer: cell(row, 'DapAnThamKhao'),
    aiHint: cell(row, 'GoiYAI'), config: config,
    // A JSON string is also supplied for Unity JsonUtility clients.
    configJson: JSON.stringify(config),
    imageUrl: cover.url, imageOriginalUrl: cover.originalUrl, imageFileId: cover.fileId,
    instructionImageUrl: instruction.url, instructionImageOriginalUrl: instruction.originalUrl,
    instructionImageFileId: instruction.fileId, imagesConfigured: cover.ready && instruction.ready,
    durationMinutes: Number(cell(row, 'ThoiLuongPhut')) || 0,
    safety: cell(row, 'AnToan'), status: cell(row, 'TrangThai')
  };
}

function practiceLoadLessons_(ss, chapterId, lessonId) {
  var table = practiceReadTable_(ss, 'ThucHanh', [
    'ThucHanhID', 'MonID', 'ChuongID', 'TenBai', 'ThuTu', 'LoaiMoPhong',
    'CauHinhJSON', 'AnhDaiDien', 'AnhHuongDan', 'TrangThai'
  ]);
  var lessons = [], seen = {};
  table.rows.forEach(function(row) {
    if (practiceKey_(table.cell(row, 'TrangThai')) !== 'da duyet') return;
    if (chapterId && table.cell(row, 'ChuongID') !== chapterId) return;
    if (lessonId && table.cell(row, 'ThucHanhID') !== lessonId) return;
    var lesson = practiceLesson_(table, row);
    if (seen[lesson.id]) throw new Error('Trùng ThucHanhID: ' + lesson.id);
    seen[lesson.id] = true;
    lessons.push(lesson);
  });
  lessons.sort(function(a, b) { return a.order - b.order || a.id.localeCompare(b.id); });
  return lessons;
}

// GET ?action=practiceList&chapterId=CH_PHY_01
// Optional subject=Vật lý / Hóa học / Physics / Chemistry.
function getPracticeLessons(e) {
  try {
    var params = e && e.parameter || {};
    var chapter = String(params.chapterId || '').trim().toUpperCase();
    if (!/^CH_(PHY|CHM)_\d{2}$/.test(chapter)) throw new Error('Cần chapterId dạng CH_PHY_01 hoặc CH_CHM_01');
    if (params.subject && practiceSubject_(params.subject) !== chapter.split('_')[1]) throw new Error('Môn không khớp chapterId');
    var lessons = practiceLoadLessons_(SpreadsheetApp.openById(SPREADSHEET_ID), chapter, '');
    return jsonOut({ success: true, apiVersion: 'practice-v1', chapterId: chapter, count: lessons.length,
      message: lessons.length ? '' : 'Chương này chưa có bài thực hành Đã duyệt.',
      lessons: lessons });
  } catch (error) { return jsonOut({ success: false, message: String(error.message || error) }); }
}

// GET ?action=practiceDetail&lessonId=TH_PHY_01_01
function getPracticeLesson(e) {
  try {
    var params = e && e.parameter || {};
    var id = String(params.lessonId || '').trim().toUpperCase();
    if (!/^TH_(PHY|CHM)_\d{2}_\d{2}$/.test(id)) throw new Error('Cần lessonId dạng TH_PHY_01_01 hoặc TH_CHM_01_01');
    var ss = SpreadsheetApp.openById(SPREADSHEET_ID);
    var lessons = practiceLoadLessons_(ss, '', id);
    if (!lessons.length) throw new Error('Không tìm thấy bài ' + id + ' ở trạng thái Đã duyệt');
    var table = practiceReadTable_(ss, 'BuocThucHanh', [
      'BuocID', 'ThucHanhID', 'ThuTu', 'TenBuoc', 'ThaoTacHocSinh',
      'HienTuongQuanSat', 'DieuKienHoanThanh', 'PhanHoiKhiSai'
    ]);
    var steps = [], seen = {}, orders = {};
    table.rows.forEach(function(row) {
      if (table.cell(row, 'ThucHanhID') !== id) return;
      var stepId = table.cell(row, 'BuocID');
      var order = Number(table.cell(row, 'ThuTu'));
      if (!stepId || seen[stepId] || !Number.isInteger(order) || order <= 0 || orders[order])
        throw new Error('BuocID hoặc ThuTu trùng/không hợp lệ ở bài ' + id);
      var title = table.cell(row, 'TenBuoc'), action = table.cell(row, 'ThaoTacHocSinh');
      var completion = table.cell(row, 'DieuKienHoanThanh');
      if (!title || !action || !completion) throw new Error('Bước ' + stepId + ' thiếu tên, thao tác hoặc điều kiện hoàn thành');
      seen[stepId] = true; orders[order] = true;
      steps.push({ id: stepId, order: order, title: title, action: action,
        observation: table.cell(row, 'HienTuongQuanSat'), completionCondition: completion,
        incorrectFeedback: table.cell(row, 'PhanHoiKhiSai') });
    });
    steps.sort(function(a, b) { return a.order - b.order; });
    if (!steps.length) throw new Error('Bài ' + id + ' chưa có bước thực hành');
    for (var i = 0; i < steps.length; i++) if (steps[i].order !== i + 1) throw new Error('ThuTu bước phải liên tiếp từ 1 ở bài ' + id);
    return jsonOut({ success: true, apiVersion: 'practice-v1', lesson: lessons[0], stepCount: steps.length, steps: steps });
  } catch (error) { return jsonOut({ success: false, message: String(error.message || error) }); }
}
