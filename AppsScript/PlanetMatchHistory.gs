// Also embedded in Code.gs. Do not paste both copies into the same project.
// Existing History columns A:F are preserved. G:H are match ID and rank score.
function planetSaveMatch_(p) {
  var record = JSON.parse(p.record || '{}');
  var username = String(record.username || '').trim();
  var id = String(record.matchId || '');
  var mode = record.mode || 'Đối kháng';
  var practice = mode === 'Luyện tập';
  var maximum = practice ? (record.questionType === 'essay' ? 50 : 0)
    : record.questionType === 'essay' ? 90 : record.questionType === 'multipleChoice' ? 450 : 0;
  if (!/^[A-Za-z0-9_-]{8,100}$/.test(id) || !username || username.length > 100 ||
      !['THẮNG', 'THUA', 'HÒA'].includes(record.result) || !['Luyện tập', 'Đối kháng'].includes(mode) || (!practice && !maximum) || record.maxScore !== maximum ||
      !Number.isInteger(record.score) || record.score > (maximum || 100000) || record.score < (practice ? 0 : maximum === 90 ? 0 : -600))
    return jsonOut({ success: false, message: 'Kết quả Planet không hợp lệ.' });
  var completed = record.finishedAt ? planetRankTime_(record.finishedAt) : Date.now();
  if (!Number.isFinite(completed) || completed > Date.now() + 300000)
    return jsonOut({ success: false, message: 'Thời gian hoàn thành không hợp lệ.' });
  var ss = SpreadsheetApp.openById(SPREADSHEET_ID);
  var users = ss.getSheetByName('Users');
  if (!users || !users.getDataRange().getValues().slice(1).some(function(row) { return String(row[1]).trim() === username; }))
    return jsonOut({ success: false, message: 'Không tìm thấy tài khoản trong Users.' });
  var lock = LockService.getScriptLock(); lock.waitLock(15000);
  try {
    var sheet = ss.getSheetByName('History');
    if (!sheet) {
      sheet = ss.insertSheet('History');
      sheet.appendRow(['Username', 'Opponent', 'Mode', 'Result', 'Score', 'Time']);
    }
    if (sheet.getMaxColumns() < 8) sheet.insertColumnsAfter(sheet.getMaxColumns(), 8 - sheet.getMaxColumns());
    var extra = sheet.getRange(1, 7, 1, 2).getValues()[0];
    var expected = ['PlanetMatchID', 'PlanetRankScore'];
    for (var col = 0; col < 2; col++) {
      if (extra[col] && extra[col] !== expected[col])
        return jsonOut({ success: false, message: 'History cột G:H đã có dữ liệu khác. Cần kiểm tra trước khi lưu Planet.' });
    }
    var rows = sheet.getDataRange().getValues();
    if ((!extra[0] || !extra[1]) && rows.slice(1).some(function(row) {
      return (!extra[0] && row[6] !== '' && row[6] != null) || (!extra[1] && row[7] !== '' && row[7] != null);
    })) return jsonOut({ success: false, message: 'History G:H có dữ liệu chưa đặt tên; không ghi đè.' });
    sheet.getRange(1, 7, 1, 2).setValues([expected]);
    for (var i = 1; i < rows.length; i++) {
      if (String(rows[i][0]).trim() === username && String(rows[i][6]) === id)
        return jsonOut({ success: true, duplicate: true, matchId: id });
    }
    function safe(value) {
      var text = String(value || '').slice(0, 100);
      return /^\s*[=+@-]/.test(text) ? "'" + text : text;
    }
    var rankScore = Math.max(0, Math.min(100, maximum ? Math.round(record.score / maximum * 100) : 0));
    var lessonColumns = planetHistoryLessonColumns_(sheet);
    var entry = [safe(username), safe(record.opponent || 'Máy AI'), mode, record.result,
      record.score, new Date(completed), id, rankScore];
    while (entry.length <= Math.max(lessonColumns.subject, lessonColumns.chapter)) entry.push('');
    entry[lessonColumns.subject] = practice ? safe(record.subject) : '';
    entry[lessonColumns.chapter] = practice ? safe(record.chapter) : '';
    sheet.appendRow(entry);
    SpreadsheetApp.flush();
    return jsonOut({ success: true, matchId: id, rankScore: rankScore });
  } finally { lock.releaseLock(); }
}

function planetHistoryLessonColumns_(sheet) {
  var headers = sheet.getDataRange().getValues()[0].slice();
  while (headers.length < 8) headers.push('');
  var subject = headers.indexOf('Subject'), chapter = headers.indexOf('Chapter');
  if (subject < 0) { subject = headers.length; headers.push('Subject'); }
  if (chapter < 0) { chapter = headers.length; headers.push('Chapter'); }
  if (sheet.getMaxColumns() < headers.length) sheet.insertColumnsAfter(sheet.getMaxColumns(), headers.length - sheet.getMaxColumns());
  sheet.getRange(1, 1, 1, headers.length).setValues([headers]);
  return { subject: subject, chapter: chapter };
}

// Week is [Monday 00:00, next Monday 00:00), Asia/Ho_Chi_Minh (UTC+7).
// No rows are deleted when the week changes.
function planetRankWeek_(now) {
  var offset = 7 * 60 * 60 * 1000;
  var local = new Date(Number(now) + offset);
  var daysSinceMonday = (local.getUTCDay() + 6) % 7;
  var start = Date.UTC(local.getUTCFullYear(), local.getUTCMonth(), local.getUTCDate() - daysSinceMonday) - offset;
  return { start: start, end: start + 7 * 24 * 60 * 60 * 1000 };
}

function planetRankTime_(value) {
  if (value && typeof value.getTime === 'function') return value.getTime();
  var text = String(value || '').trim();
  // ISO timestamps carrying an explicit timezone, including Unity's UTC format.
  if (/^\d{4}-\d{2}-\d{2}T.*(?:Z|[+-]\d{2}:?\d{2})$/i.test(text)) return Date.parse(text);
  var match = text.match(/^(\d{1,2})\/(\d{1,2})\/(\d{4})(?:[ T](\d{1,2}):(\d{2})(?::(\d{2}))?)?$/);
  if (!match) return NaN;
  var year = Number(match[3]), month = Number(match[2]), day = Number(match[1]);
  var hour = Number(match[4] || 0), minute = Number(match[5] || 0), second = Number(match[6] || 0);
  var local = new Date(Date.UTC(year, month - 1, day, hour, minute, second));
  if (local.getUTCFullYear() !== year || local.getUTCMonth() !== month - 1 || local.getUTCDate() !== day ||
      hour > 23 || minute > 59 || second > 59) return NaN;
  return local.getTime() - 7 * 60 * 60 * 1000;
}

function planetGetProgressRank_() {
  var ss = SpreadsheetApp.openById(SPREADSHEET_ID);
  var usersSheet = ss.getSheetByName('Users');
  var users = usersSheet ? usersSheet.getDataRange().getValues().slice(1) : [];
  var aliases = Object.create(null), names = Object.create(null), scores = Object.create(null);
  users.forEach(function(row) {
    var username = String(row[1] || '').trim(), id = String(row[0] || '').trim();
    if (!username && !id) return;
    var key = username || id;
    if (username) aliases[username] = key;
    if (id) aliases[id] = key;
    names[key] = String(row[3] || key);
  });
  var now = Date.now(), week = planetRankWeek_(now), seen = Object.create(null), undated = 0;
  var history = ss.getSheetByName('History');
  if (history) {
    var rows = history.getDataRange().getValues();
    var hasMatchId = rows.length && rows[0][6] === 'PlanetMatchID';
    rows.slice(1).forEach(function(row) {
      var mode = String(row[2] || '').normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/[đĐ]/g, 'd').toLowerCase().trim();
      if (!['luyen tap', 'doi khang'].includes(mode)) return;
      var key = String(row[0] || '').trim(), points = Number(row[4]);
      if (!key || row[4] === '' || row[4] == null || !Number.isFinite(points)) return;
      key = aliases[key] || key;
      var stamp = planetRankTime_(row[5]);
      if (!Number.isFinite(stamp)) { undated++; return; }
      if (stamp < week.start || stamp >= week.end || stamp > now) return;
      if (hasMatchId && row[6]) {
        var id = key + ':' + String(row[6]);
        if (seen[id]) return;
        seen[id] = true;
      }
      // History is the single result ledger. Never add the same result again
      // from TienDoHocTap or the normalized legacy PlanetRankScore column.
      scores[key] = (scores[key] || 0) + points;
    });
  }
  var players = Object.keys(scores).map(function(key) { return { userID: key, fullName: names[key] || key, score: scores[key] }; });
  players.sort(function(a, b) { return b.score - a.score || a.userID.localeCompare(b.userID); });
  players.forEach(function(player, index) { player.rank = index + 1; });
  return ContentService.createTextOutput(JSON.stringify({ success: true, rankVersion: 'weekly-v2',
    timezone: 'Asia/Ho_Chi_Minh', weekStartsAt: new Date(week.start).toISOString(), weekEndsAt: new Date(week.end).toISOString(),
    weekStartLabel: Utilities.formatDate(new Date(week.start), 'Asia/Ho_Chi_Minh', 'dd/MM/yyyy'),
    weekEndLabel: Utilities.formatDate(new Date(week.end - 1), 'Asia/Ho_Chi_Minh', 'dd/MM/yyyy'),
    ignoredUndatedRows: undated, players: players })).setMimeType(ContentService.MimeType.JSON);
}

