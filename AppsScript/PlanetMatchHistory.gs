// Also embedded in Code.gs. Do not paste both copies into the same project.
// History fields are located by header; existing extra columns are preserved.
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
    var columns = planetHistoryColumns_(sheet.getDataRange().getValues()[0] || []);
    var headers = sheet.getDataRange().getValues()[0].slice();
    // Append metadata after existing columns; never reuse an unnamed occupied column.
    ['PlanetMatchID', 'PlanetRankScore', 'Subject', 'Chapter'].forEach(function(name) {
      if (headers.indexOf(name) < 0) headers.push(name);
    });
    if (sheet.getMaxColumns() < headers.length) sheet.insertColumnsAfter(sheet.getMaxColumns(), headers.length - sheet.getMaxColumns());
    sheet.getRange(1, 1, 1, headers.length).setValues([headers]);
    columns = planetHistoryColumns_(headers);
    var rows = sheet.getDataRange().getValues();
    for (var i = 1; i < rows.length; i++) {
      if (String(rows[i][columns.Username]).trim() === username && String(rows[i][columns.PlanetMatchID]) === id)
        return jsonOut({ success: true, duplicate: true, matchId: id });
    }
    function safe(value) {
      var text = String(value || '').slice(0, 100);
      return /^\s*[=+@-]/.test(text) ? "'" + text : text;
    }
    var rankScore = Math.max(0, Math.min(100, maximum ? Math.round(record.score / maximum * 100) : 0));
    var entry = headers.map(function() { return ''; });
    var values = { Username: safe(username), Opponent: safe(record.opponent || 'Máy AI'), Mode: mode,
      Result: record.result, Score: record.score, Time: new Date(completed), PlanetMatchID: id,
      PlanetRankScore: rankScore, Subject: practice ? safe(record.subject) : '', Chapter: practice ? safe(record.chapter) : '' };
    Object.keys(values).forEach(function(name) { entry[columns[name]] = values[name]; });
    sheet.appendRow(entry);
    SpreadsheetApp.flush();
    return jsonOut({ success: true, matchId: id, rankScore: rankScore });
  } finally { lock.releaseLock(); }
}

function planetHistoryColumns_(headers) {
  var columns = Object.create(null);
  headers.forEach(function(name, index) {
    var key = String(name || '').trim();
    if (!key) return;
    if (columns[key] !== undefined) throw new Error('History trùng cột ' + key);
    columns[key] = index;
  });
  ['Username', 'Opponent', 'Mode', 'Result', 'Score', 'Time'].forEach(function(name) {
    if (columns[name] === undefined) throw new Error('History thiếu cột ' + name);
  });
  return columns;
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
    var columns = planetHistoryColumns_(rows[0] || []);
    var hasMatchId = columns.PlanetMatchID !== undefined;
    rows.slice(1).forEach(function(row) {
      var mode = String(row[columns.Mode] || '').normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/[đĐ]/g, 'd').toLowerCase().trim();
      if (!['luyen tap', 'doi khang'].includes(mode)) return;
      var key = String(row[columns.Username] || '').trim(), points = Number(row[columns.Score]);
      if (!key || row[columns.Score] === '' || row[columns.Score] == null || !Number.isFinite(points)) return;
      key = aliases[key] || key;
      var stamp = planetRankTime_(row[columns.Time]);
      if (!Number.isFinite(stamp)) { undated++; return; }
      if (stamp < week.start || stamp >= week.end || stamp > now) return;
      if (hasMatchId && row[columns.PlanetMatchID]) {
        var id = key + ':' + String(row[columns.PlanetMatchID]);
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

