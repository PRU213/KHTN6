import { DEFAULT_APPS_SCRIPT_URL } from './essay-source.mjs';

// Only the server knows this credential. Unity never reads/writes journal rows directly.
export function createTutorSheetStore({ url = process.env.APPS_SCRIPT_URL || DEFAULT_APPS_SCRIPT_URL, secret = process.env.APPS_SCRIPT_SECRET, fetchImpl = fetch } = {}) {
  if (!secret) throw new Error('Lưu nhật ký Google Sheet cần APPS_SCRIPT_SECRET khớp PLANET_SERVER_SECRET.');
  const revisions = new Map();
  async function post(action, data = {}) {
    let response;
    try {
      response = await fetchImpl(url, { method: 'POST', signal: AbortSignal.timeout(45000),
        body: new URLSearchParams({ action, serverSecret: secret, ...data }) });
    } catch { throw new Error('Google Sheet chưa xác nhận lưu nhật ký. Giữ màn hiện tại và thử lại khi có kết nối.'); }
    if (!response.ok) throw new Error('Chưa kết nối được Google Sheet để lưu nhật ký. Hãy thử lại.');
    let result;
    try { result = await response.json(); } catch { throw new Error('Apps Script chưa trả nhật ký hợp lệ. Kiểm tra bản triển khai mới.'); }
    if (result?.success !== true || result.journalVersion !== 1)
      throw new Error(result?.code === 'CONFLICT' ? 'Nhật ký đã thay đổi trên máy chủ khác. Cần khởi động lại backend để tải đúng tiến độ.' : 'Chưa lưu/đọc được nhật ký Google Sheet. Kiểm tra TutorJournal.gs, deployment và khóa bí mật.');
    return result;
  }
  return {
    kind: 'googleSheets',
    async load() {
      const result = await post('tutorJournalLoad');
      if (!Array.isArray(result.records)) throw new Error('Google Sheet thiếu danh sách nhật ký.');
      const seen = new Set();
      for (const entry of result.records) {
        const r = entry?.record;
        if (!r || !/^[a-f0-9]{40}$/.test(r.id || '') || seen.has(r.id) || r.version !== 1 || typeof r.username !== 'string' || !r.username.trim() || !['Physics','Chemistry','Biology'].includes(r.subject) || !Array.isArray(r.events) || typeof r.sourceId !== 'string' || !Number.isInteger(entry.revision) || entry.revision < 1)
          throw new Error('Nhật ký trong Google Sheet hỏng hoặc không đúng cấu trúc; không bỏ qua dữ liệu.');
        seen.add(r.id); revisions.set(r.id, entry.revision);
      }
      return result.records.map(e => e.record);
    },
    async save(records) {
      // Keep requests bounded. A timeout is safe: the Sheet recognizes identical content on retry.
      for (let offset = 0; offset < records.length; offset += 10) {
        const batch = records.slice(offset, offset + 10);
        const result = await post('tutorJournalSave', { records: JSON.stringify(batch.map(record => ({ record, expectedRevision: revisions.get(record.id) || 0 }))) });
        if (!Array.isArray(result.saved) || result.saved.length !== batch.length || result.saved.some((s, i) => s.id !== batch[i].id || !Number.isInteger(s.revision) || s.revision < 1))
          throw new Error('Google Sheet chưa xác nhận lưu đủ nhật ký. Hãy thử lại.');
        for (const row of result.saved) revisions.set(row.id, row.revision);
      }
    }
  };
}
