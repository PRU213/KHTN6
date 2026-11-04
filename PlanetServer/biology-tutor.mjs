import { resolveSubject } from './essay-source.mjs';

export const BIOLOGY_WORKBOOK_ID = '1DcJwlN_fUDdcaQ6IfnlkMJApgWuEUqMhyZv9Kn8vfWo';
const normalize = value => String(value ?? '').normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/[đĐ]/g, 'd').toLowerCase().trim();
const header = value => normalize(value).replace(/[^a-z0-9]/g, '');
const account = value => String(value ?? '').trim().toLowerCase();
const cell = (row, ...names) => names.map(name => row[header(name)]).find(value => value !== undefined)?.trim() || '';

// Local to Biology: never changes the existing question or account readers.
export function parseBiologyCsv(csv) {
  if (typeof csv !== 'string' || !csv.trim() || csv.trimStart().startsWith('<')) throw new Error('Sheet Sinh học chưa trả dữ liệu hợp lệ.');
  const rows = []; let row = [], field = '', quoted = false;
  csv = csv.replace(/^\uFEFF/, '');
  for (let i = 0; i < csv.length; i++) {
    const c = csv[i];
    if (c === '"') { if (quoted && csv[i + 1] === '"') { field += '"'; i++; } else quoted = !quoted; }
    else if (!quoted && (c === ',' || c === '\n' || c === '\r')) {
      row.push(field); field = '';
      if (c !== ',') { if (c === '\r' && csv[i + 1] === '\n') i++; rows.push(row); row = []; }
    } else field += c;
  }
  if (quoted) throw new Error('Dữ liệu Sheet bị thiếu dấu đóng ngoặc kép.');
  if (field.length || row.length) { row.push(field); rows.push(row); }
  const headers = (rows.shift() || []).map(header);
  return rows.filter(row => row.some(value => value.trim())).map(row => Object.fromEntries(headers.flatMap((name, i) => name ? [[name, row[i] || '']] : [])));
}

export function createBiologyTutorSource({ workbookId = BIOLOGY_WORKBOOK_ID, fetchImpl = fetch } = {}) {
  async function read(sheet, query) {
    const url = new URL(`https://docs.google.com/spreadsheets/d/${encodeURIComponent(workbookId)}/gviz/tq`);
    url.searchParams.set('tqx', 'out:csv'); url.searchParams.set('headers', '1');
    if (sheet) url.searchParams.set('sheet', sheet);
    if (query) url.searchParams.set('tq', query);
    const response = await fetchImpl(url, { signal: AbortSignal.timeout(25000), cache: 'no-store' });
    if (!response.ok) throw new Error('Chưa tải được bài Sinh học đã chấm. Hãy thử lại.');
    return parseBiologyCsv(await response.text());
  }
  return async ({ id, username }) => {
    if (typeof id !== 'string' || !/^[a-z0-9_-]{1,80}$/i.test(id) || !account(username)) throw new Error('Thiếu mã bài nộp Sinh học hợp lệ.');
    const submissions = await read('BaiNopTuLuan', `select * where A = '${id}' limit 2`);
    if (submissions.length !== 1 || cell(submissions[0], 'SubmissionID') !== id) throw new Error('Không tìm thấy đúng bài nộp Sinh học đã chấm.');
    const submission = submissions[0], userId = cell(submission, 'UserID');
    let owns = account(userId) === account(username);
    if (!owns) {
      // Read only ID and Username, excluding the password column.
      const identities = await read(null, 'select A, B');
      owns = identities.some(row => account(cell(row, 'Username')) === account(username) && account(cell(row, 'ID', 'UserID')) === account(userId));
    }
    if (!owns) throw Object.assign(new Error('Bài nộp này không thuộc tài khoản đang đăng nhập.'), { status: 403 });
    const rawScore = cell(submission, 'DiemAI'), score = Number(rawScore.replace(',', '.'));
    if (normalize(cell(submission, 'TrangThaiCham')) !== 'completed' || !rawScore || !Number.isFinite(score) || score < 0 || score > 10)
      throw new Error('Bài nộp chưa có kết quả AI hoàn tất. Hãy thử lại sau.');
    if (score >= 5) throw new Error('Bài từ 5 điểm trở lên được tiếp tục theo luồng cũ, không cần chép phạt Sinh học.');
    const questionId = cell(submission, 'CauHoiID');
    if (!/^[a-z0-9_-]{1,100}$/i.test(questionId)) throw new Error('Mã câu tự luận Sinh học chưa hợp lệ.');
    const questions = await read('TuLuan', `select * where A = '${questionId}' limit 2`);
    if (questions.length !== 1) throw new Error('Không tìm thấy câu tự luận gốc của bài nộp.');
    const q = questions[0], topic = cell(q, 'Chương/Chủ đề', 'ChuongChuDe', 'topic');
    if (cell(q, 'CauHoiID') !== questionId || resolveSubject(cell(q, 'monID')) !== 'Biology') throw new Error('Bài nộp không phải câu tự luận Sinh học.');
    const question = cell(q, 'Question'), reference = cell(q, 'DapAnThamKhao');
    if (!question || !reference) throw new Error('Câu Sinh học chưa có nội dung hoặc đáp án tham khảo.');
    const chapters = await read('Theory', 'select * limit 1000');
    const number = normalize(topic).match(/(?:chuong|chapter)\s*(\d+)/)?.[1];
    const matches = chapters.filter(row => {
      if (resolveSubject(cell(row, 'monID')) !== 'Biology') return false;
      const order = normalize(cell(row, 'Thứ tự chương', 'chapterOrder')).match(/\d+/)?.[0];
      const title = normalize(cell(row, 'Tên chương (ChapterTitle)', 'ChapterTitle'));
      return number ? Number(order) === Number(number) : !!title && (normalize(topic) === title || normalize(topic) === `${order}. ${title}`);
    });
    if (matches.length !== 1) throw new Error('Chưa xác định được đúng nội dung chương Sinh học trong bảng Theory.');
    const chapter = matches[0];
    const parts = [
      cell(chapter, 'Tên chương (ChapterTitle)', 'ChapterTitle'),
      cell(chapter, 'Tóm tắt chương (Summary)', 'Summary'),
      cell(chapter, 'Trọng tâm kiến thức (KeyPoints)', 'KeyPoints'),
      cell(chapter, 'Công thức / Ghi nhớ (Formula_Note)', 'Formula_Note')
    ].filter(value => value && normalize(value) !== 'none');
    if (parts.length < 2) throw new Error('Chương Sinh học chưa có nội dung để hướng dẫn và chép lại.');
    const chapterContent = parts.join('\n\n');
    if (chapterContent.length > 30000) throw new Error('Nội dung chương Sinh học vượt giới hạn bài chép hiện tại.');
    return { source: 'biology', sourceId: 'biology:' + id, username, subject: 'Biology', chapter: topic, chapterContent,
      question, reference, studentAnswer: cell(submission, 'CauTraLoi'), score, finalScore: score };
  };
}

export async function startBiologyTutor(tutor, body, source) {
  // The ID is the only client-provided lesson data. Owner, score and reference come from Sheets.
  const record = await source({ id: body.id, username: body.username });
  const id = tutor.register(record);
  if (!id) throw new Error('Chưa tạo được bài hướng dẫn Sinh học.');
  return tutor.start({ id, username: body.username });
}
