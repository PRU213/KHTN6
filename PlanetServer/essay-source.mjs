import { createHash, randomUUID } from 'node:crypto';
import { readFileSync, mkdirSync, writeFileSync, renameSync, unlinkSync } from 'node:fs';
import { join } from 'node:path';
import { fileURLToPath } from 'node:url';

export const DEFAULT_APPS_SCRIPT_URL = 'https://script.google.com/macros/s/AKfycbxLHvShuipgPg7x28IikDmzU7wHoE2e3ai9rhuoEnN50UsFUAWIPTG_nneHOxz-YmaY/exec';
const subjects = new Map([['vat ly', 'Physics'], ['vat li', 'Physics'], ['physics', 'Physics'], ['hoa hoc', 'Chemistry'], ['chemistry', 'Chemistry'], ['sinh hoc', 'Biology'], ['biology', 'Biology']]);
const normalize = value => String(value || '').normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/đ/g, 'd').toLowerCase().trim();
export const resolveSubject = value => subjects.get(normalize(value));
export function parseEssayQuestions(data) {
  if (data?.success === false && typeof data.message === 'string') {
    if (/getEssayQuestions.*not defined/i.test(data.message))
      throw new Error('Apps Script thiếu hàm getEssayQuestions. Hãy thêm file AppsScript/TuLuanQuestions.gs rồi triển khai phiên bản mới.');
    if (data.message === 'Missing chapterId')
      throw new Error('API essayQuestions đang chạy vào phần lý thuyết. Kiểm tra nhánh essayQuestions trong doGet và bản triển khai Apps Script.');
    if (/Unauthorized essay access/i.test(data.message))
      throw new Error('Máy chủ chưa có quyền đọc tự luận. Kiểm tra APPS_SCRIPT_SECRET khớp PLANET_SERVER_SECRET trong Apps Script.');
    throw new Error('Không tải được câu tự luận: ' + data.message.slice(0, 300));
  }
  if (!data?.success || data.questionType !== 'essay' || !Array.isArray(data.questions))
    throw new Error('API chưa trả danh sách tự luận hợp lệ. Kiểm tra essayQuestions trong doGet và triển khai đúng phiên bản Apps Script.');
  const ids = new Set();
  return data.questions.map(q => {
    const item = { id: String(q.id || '').trim(), subject: subjects.get(normalize(q.monId)), topic: String(q.topic || '').trim(), difficulty: String(q.difficulty || '').trim(),
      text: String(q.question || '').trim(), sampleAnswer: String(q.sampleAnswer || '').trim(), rubric: String(q.rubric || '').trim(),
      maxPoints: Number(q.maxPoints), imageUrl: String(q.imageUrl || '').trim() };
    if (!item.id || ids.has(item.id) || !item.subject || !['Dễ', 'Trung bình', 'Khó'].includes(item.difficulty) || !item.text || !item.sampleAnswer || !item.rubric || !Number.isFinite(item.maxPoints) || item.maxPoints <= 0 || item.maxPoints > 100)
      throw new Error('Dữ liệu TuLuan thiếu hoặc sai ID, môn, độ khó, đáp án, hướng dẫn chấm hoặc điểm tối đa.');
    ids.add(item.id);
    // Current game clients display text questions only. Exclude the whole
    // illustrated question rather than showing an incomplete prompt or failing
    // the entire bank (including unrelated subjects and chapters).
    if (item.imageUrl && item.imageUrl.toLowerCase() !== 'none') return null;
    item.imageUrl = '';
    return item;
  }).filter(item => item !== null);
}
export function createEssaySource({ url = process.env.APPS_SCRIPT_URL || DEFAULT_APPS_SCRIPT_URL, secret = process.env.APPS_SCRIPT_SECRET,
  fetchImpl = fetch, now = Date.now, timeoutMs = 35000, cacheDirectory = null, maxCacheAgeMs = 86400000, onWarning = message => console.warn(message) } = {}) {
  let cache, savedAt = 0, pending, nextRefreshAt = 0, fatalError;
  // Separate deployments and credentials. Neither value is written to disk.
  const identity = createHash('sha256').update(url + '\0' + (secret || '')).digest('hex');
  const directory = cacheDirectory instanceof URL ? fileURLToPath(cacheDirectory) : cacheDirectory;
  const file = directory ? join(directory, identity + '.json') : null;
  const usable = () => cache !== undefined && now() >= savedAt && now() - savedAt < maxCacheAgeMs;
  if (file) {
    try {
      const snapshot = JSON.parse(readFileSync(file, 'utf8'));
      if (snapshot.version === 1 && snapshot.identity === identity && Number.isFinite(snapshot.savedAt) &&
          snapshot.savedAt <= now() && now() - snapshot.savedAt < maxCacheAgeMs) {
        cache = parseEssayQuestions(snapshot.payload); savedAt = snapshot.savedAt;
      }
    } catch { /* A missing/corrupt snapshot never replaces a real Sheet response. */ }
  }
  function persist(payload) {
    if (!file) return;
    const temporary = file + '.' + randomUUID() + '.tmp';
    try {
      mkdirSync(directory, { recursive: true });
      writeFileSync(temporary, JSON.stringify({ version: 1, identity, savedAt, payload }), { mode: 0o600 });
      renameSync(temporary, file);
    } catch (error) {
      try { unlinkSync(temporary); } catch { }
      onWarning('Không lưu được bộ nhớ đệm câu hỏi (' + (error.code || 'IO') + '); bộ câu hợp lệ vẫn còn trong RAM.');
    }
  }
  function refresh() {
    if (pending) return pending;
    let authoritativeFailure = false;
    pending = (async () => {
      const endpoint = new URL(url); endpoint.searchParams.set('action', 'essayQuestions');
      if (secret) endpoint.searchParams.set('serverSecret', secret);
      const response = await fetchImpl(endpoint, { signal: AbortSignal.timeout(timeoutMs) });
      if (!response.ok) {
        authoritativeFailure = response.status !== 429 && response.status < 500;
        throw new Error('Không tải được tab TuLuan từ Apps Script (HTTP ' + response.status + ').');
      }
      let data;
      try { data = await response.json(); } catch (error) {
        if (error?.name === 'TimeoutError' || error?.name === 'AbortError') throw error;
        authoritativeFailure = true;
        throw new Error('Apps Script không trả JSON. Kiểm tra URL /exec và quyền truy cập bản triển khai.');
      }
      let questions;
      try { questions = parseEssayQuestions(data); } catch (error) { authoritativeFailure = true; throw error; }
      cache = questions; savedAt = now(); fatalError = null; persist(data);
      return cache;
    })().catch(error => {
      if (authoritativeFailure) {
        // Never conceal revoked access, a wrong deployment or invalid Sheet data.
        fatalError = error; cache = undefined;
        if (file) try { unlinkSync(file); } catch { }
      }
      if (error?.name === 'TimeoutError' || error?.name === 'AbortError')
        throw new Error('Google Sheets phản hồi quá chậm khi tải câu hỏi. Bạn bấm Thử lại nhé; không cần dừng Play hoặc khởi động lại server.');
      throw error;
    }).finally(() => { pending = null; nextRefreshAt = now() + 30000; });
    return pending;
  }
  const source = async () => {
    if (fatalError) {
      if (now() >= nextRefreshAt) return refresh();
      throw fatalError;
    }
    if (usable()) {
      if (now() - savedAt >= 60000 && now() >= nextRefreshAt && !pending)
        refresh().catch(() => onWarning('Chưa cập nhật được TuLuan từ Google Sheets; giữ bộ câu hợp lệ gần nhất nếu lỗi kết nối tạm thời.'));
      return cache;
    }
    return refresh();
  };
  source.status = () => ({ ready: usable() && !fatalError, count: usable() && !fatalError ? cache.length : 0,
    savedAt: savedAt || null, refreshing: !!pending, stale: usable() && now() - savedAt >= 60000 });
  return source;
}

// Optional history: grading does not depend on the availability of Sheets.
export function createEssayRecorder({ url = process.env.APPS_SCRIPT_URL || DEFAULT_APPS_SCRIPT_URL, secret = process.env.APPS_SCRIPT_SECRET, fetchImpl = fetch } = {}) {
  return async record => {
    if (!secret) return;
    const response = await fetchImpl(url, { method: 'POST', signal: AbortSignal.timeout(10000),
      body: new URLSearchParams({ action: 'saveEssaySubmission', serverSecret: secret, record: JSON.stringify(record) }) });
    if (!response.ok || !(await response.json()).success) throw new Error('Không lưu được lịch sử tự luận.');
  };
}
