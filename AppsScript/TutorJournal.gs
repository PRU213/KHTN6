/** Add this file beside Mã.gs. Keep only ONE doGet/doPost in the project. */
function tutorJournalSpreadsheet_() {
  var id = PropertiesService.getScriptProperties().getProperty('TUTOR_SPREADSHEET_ID');
  if (!id && typeof SPREADSHEET_ID !== 'undefined') id = SPREADSHEET_ID;
  if (!id) throw new Error('Thiếu Script Property TUTOR_SPREADSHEET_ID.');
  return SpreadsheetApp.openById(id);
}
function tutorJournalSheet_() {
  var ss = tutorJournalSpreadsheet_();
  var sheet = ss.getSheetByName('TutorJournal');
  if (!sheet) throw new Error('Chạy setupTutorJournal trước khi dùng.');
  var headers = sheet.getRange(1, 1, 1, 6).getValues()[0];
  if (headers.join('|') !== 'RecordID|Username|Revision|Parts|SHA256|UpdatedAt') throw new Error('Tab TutorJournal sai cấu trúc; không ghi đè.');
  return sheet;
}
function setupTutorJournal() {
  var ss = tutorJournalSpreadsheet_();
  var sheet = ss.getSheetByName('TutorJournal');
  if (sheet) { tutorJournalSheet_(); return; }
  sheet = ss.insertSheet('TutorJournal');
  sheet.getRange(1, 1, 1, 6).setValues([['RecordID','Username','Revision','Parts','SHA256','UpdatedAt']]);
  sheet.setFrozenRows(1);
}
function tutorJournalDigest_(value) {
  return Utilities.computeDigest(Utilities.DigestAlgorithm.SHA_256, value, Utilities.Charset.UTF_8).map(function(b) { return ('0' + ((b + 256) % 256).toString(16)).slice(-2); }).join('');
}
function tutorJournalReadRows_(sheet) {
  var rows = sheet.getDataRange().getValues().slice(1), seen = {};
  return rows.map(function(row, index) {
    if (!row[0]) throw new Error('Nhật ký có dòng trống bất thường.');
    var id = String(row[0]), revision = Number(row[2]), parts = Number(row[3]);
    if (!/^[a-f0-9]{40}$/.test(id) || seen[id] || !Number.isInteger(revision) || revision < 1 || !Number.isInteger(parts) || parts < 1 || parts > 100) throw new Error('Nhật ký hỏng metadata.');
    seen[id] = true;
    var encoded = row.slice(6, 6 + parts).map(function(part) { if (String(part).slice(0, 2) !== 'b:') throw new Error('Nhật ký sai phần dữ liệu.'); return String(part).slice(2); }).join('');
    var json = Utilities.newBlob(Utilities.base64Decode(encoded)).getDataAsString('UTF-8');
    if (tutorJournalDigest_(json) !== row[4]) throw new Error('Nhật ký sai checksum.');
    var record = JSON.parse(json);
    if (record.id !== id || 'u:' + record.username !== row[1] || record.version !== 1 || !Array.isArray(record.events)) throw new Error('Nhật ký sai nội dung.');
    return { row: index + 2, record: record, revision: revision, digest: row[4] };
  });
}
function tutorJournalApi_(p) {
  var configured = PropertiesService.getScriptProperties().getProperty('PLANET_SERVER_SECRET');
  if (!configured || p.serverSecret !== configured) return jsonOut({ success:false, journalVersion:1, code:'UNAUTHORIZED' });
  var lock = LockService.getScriptLock();
  lock.waitLock(30000);
  try {
    var sheet = tutorJournalSheet_(), rows = tutorJournalReadRows_(sheet);
    if (p.action === 'tutorJournalLoad') return jsonOut({ success:true, journalVersion:1, records:rows.map(function(r) { return { record:r.record, revision:r.revision }; }) });
    if (p.action !== 'tutorJournalSave') throw new Error('Sai action nhật ký.');
    var updates = typeof p.records === 'string' ? JSON.parse(p.records) : p.records;
    if (!Array.isArray(updates) || updates.length < 1 || updates.length > 10) throw new Error('Sai số bản ghi.');
    var seen = {}, nextRow = rows.length + 2;
    var writes = updates.map(function(update) {
      var record = update.record, expected = update.expectedRevision;
      if (!record || !/^[a-f0-9]{40}$/.test(record.id || '') || seen[record.id] || record.version !== 1 || typeof record.username !== 'string' || !record.username.trim() || !Array.isArray(record.events) || !Number.isInteger(expected) || expected < 0) throw new Error('Bản ghi nhật ký không hợp lệ.');
      seen[record.id] = true;
      var json = JSON.stringify(record), digest = tutorJournalDigest_(json);
      var encoded = Utilities.base64Encode(json, Utilities.Charset.UTF_8);
      if (encoded.length > 3900000) throw new Error('Nhật ký quá dài.');
      var old = rows.find(function(r) { return r.record.id === record.id; });
      if (old && old.record.username !== record.username) throw new Error('Sai chủ sở hữu nhật ký.');
      if (old && old.digest === digest) return { id:record.id, revision:old.revision };
      if ((old ? old.revision : 0) !== expected) return { conflict:true };
      var chunks = [];
      for (var i = 0; i < encoded.length; i += 39000) chunks.push('b:' + encoded.slice(i, i + 39000));
      return { id:record.id, revision:expected + 1, row:old ? old.row : nextRow++, data:[record.id,'u:' + record.username,expected + 1,chunks.length,digest,new Date().toISOString()].concat(chunks) };
    });
    if (writes.some(function(w) { return w.conflict; })) return jsonOut({ success:false, journalVersion:1, code:'CONFLICT' });
    writes.forEach(function(w) {
      if (!w.data) return;
      if (sheet.getMaxColumns() < w.data.length) sheet.insertColumnsAfter(sheet.getMaxColumns(), w.data.length - sheet.getMaxColumns());
      if (sheet.getMaxRows() < w.row) sheet.insertRowsAfter(sheet.getMaxRows(), w.row - sheet.getMaxRows());
      // Base64 chunks and the u: username prefix cannot become Sheet formulas.
      sheet.getRange(w.row, 1, 1, w.data.length).setValues([w.data]);
    });
    SpreadsheetApp.flush();
    return jsonOut({ success:true, journalVersion:1, saved:writes.map(function(w) { return { id:w.id, revision:w.revision }; }) });
  } catch (error) {
    return jsonOut({ success:false, journalVersion:1, code:'JOURNAL_ERROR' });
  } finally { lock.releaseLock(); }
}
