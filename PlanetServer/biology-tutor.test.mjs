import test from 'node:test';
import assert from 'node:assert/strict';
import { createBiologyTutorSource, startBiologyTutor, parseBiologyCsv } from './biology-tutor.mjs';
import { EssayTutor } from './tutor.mjs';
import { createCopyAI } from './copy-penalty.mjs';
import { Arena, createServer } from './server.mjs';
import { TutorAuth } from './tutor-auth.mjs';

const csv = rows => rows.map(row => row.map(value => '"' + String(value ?? '').replaceAll('"', '""') + '"').join(',')).join('\r\n');
function sourceFixture({ owner = 'HS01', score = '4,5', status = 'Completed', subject = 'Sinh học', topic = 'Chương 1: Tế bào', chapterSubject = 'Sinh học' } = {}) {
  const seen = [];
  const source = createBiologyTutorSource({ fetchImpl: async url => {
    url = new URL(url); const sheet = url.searchParams.get('sheet'); seen.push(url);
    const rows = sheet === 'BaiNopTuLuan' ? [['SubmissionID','UserID','CauHoiID','CauTraLoi','DiemAI','TrangThaiCham'], ['BIO_SUB_1', owner, 'TL_BIO_1', 'Bài sai', score, status]]
      : sheet === 'TuLuan' ? [['CauHoiID','monID','Chương/Chủ đề','Question','DapAnThamKhao'], ['TL_BIO_1',subject,topic,'Tế bào là gì?','Tế bào là đơn vị cơ sở của sự sống.']]
      : sheet === 'Theory' ? [['Mã chương (ChapterID)','monID','Thứ tự chương','Tên chương (ChapterTitle)','Tóm tắt chương (Summary)','Trọng tâm kiến thức (KeyPoints)','Công thức / Ghi nhớ (Formula_Note)'], ['CH_BIO_01',chapterSubject,'Chương 1','Tế bào','Mọi cơ thể sống gồm tế bào.','Màng tế bào, chất tế bào và nhân.','Đơn vị cơ sở của sự sống.']]
      : [['ID','Username'],['HS01','alice'],['HS02','bob']];
    return { ok: true, text: async () => csv(rows) };
  } });
  return { source, seen };
}
const request = { id: 'BIO_SUB_1', username: 'alice' };

test('Biology source maps the submitted ID to its owner, correct question, score and complete chapter', async () => {
  const { source, seen } = sourceFixture();
  const record = await source({ ...request, score: 10, reference: 'CLIENT CANNOT OVERRIDE' });
  assert.equal(record.sourceId, 'biology:BIO_SUB_1'); assert.equal(record.subject, 'Biology');
  assert.equal(record.score, 4.5); assert.equal(record.reference, 'Tế bào là đơn vị cơ sở của sự sống.');
  assert.match(record.chapterContent, /Mọi cơ thể sống/); assert.match(record.chapterContent, /Màng tế bào/);
  assert.equal(seen.find(url => !url.searchParams.has('sheet')).searchParams.get('tq'), 'select A, B');
  assert.match(seen[0].searchParams.get('tq'), /where A = 'BIO_SUB_1'/);
});

test('Biology rejects another pupil, passing scores, incomplete grading, wrong subject/chapter, and malformed IDs', async () => {
  await assert.rejects(sourceFixture().source({ ...request, username: 'bob' }), error => error.status === 403);
  for (const score of ['5', '6', '10']) await assert.rejects(sourceFixture({ score }).source(request), /từ 5 điểm/);
  for (const score of ['', 'NaN', '-1', '11']) await assert.rejects(sourceFixture({ score }).source(request), /chưa có kết quả/);
  await assert.rejects(sourceFixture({ status: 'Pending' }).source(request), /chưa có kết quả/);
  await assert.rejects(sourceFixture({ subject: 'Hóa học' }).source(request), /không phải/);
  await assert.rejects(sourceFixture({ chapterSubject: 'Vật lý' }).source(request), /đúng nội dung chương/);
  await assert.rejects(sourceFixture().source({ ...request, id: "x' or true" }), /mã bài nộp/);
});

test('Biology CSV preserves multiline answers and quotes, rejects HTML and malformed CSV', () => {
  assert.equal(parseBiologyCsv(csv([['Question','DapAnThamKhao'],['a, b','Dòng 1\n"Dòng 2"']]))[0].dapanthamkhao, 'Dòng 1\n"Dòng 2"');
  assert.throws(() => parseBiologyCsv('<html>login</html>'));
  assert.throws(() => parseBiologyCsv('"Question\nmissing close'));
});

function tutorFixture() {
  let checked = { handwritten: true, uncertain: false, count: 1, questionCopied: true, redoCorrect: true, chapterCopied: false, feedback: 'Thiếu nội dung chương.' };
  const tutor = new EssayTutor({ ai: {
    plan: async () => ({ steps: [1,2,3].map(i => ({ question: 'Ý ' + i, expected: 'Đúng', explanation: 'Giải thích', checkQuestion: 'Kiểm tra', checkExpected: 'Đúng' })), solution: 'Đáp án tổng kết.' }),
    evaluate: async ({ answer }) => ({ correct: answer === 'đúng' }),
    guide: async () => ({ explanation: 'Nhìn lại thành phần tế bào.', checkQuestion: 'Tế bào có những phần nào?' })
  }, copyAI: { prepare: async () => { throw Error('Biology must keep the actual chapter and reference.'); }, check: async () => checked } });
  return { tutor, result: value => { checked = value; } };
}
test('AI guides Biology before copying; only complete chapter + correct answer unlock, retries do not duplicate', async () => {
  const { tutor, result } = tutorFixture(); const source = sourceFixture().source;
  let record = await startBiologyTutor(tutor, request, source);
  assert.equal(record.status, 'learning'); assert.equal(record.penalty.status, 'deferred');
  const body = { id: record.id, username: 'alice' };
  await assert.rejects(tutor.prepareCopy(body), /hoàn thành học lại/);
  assert.equal((await startBiologyTutor(tutor, request, source)).id, record.id); assert.equal(tutor.records.size, 1);
  record = await tutor.answer({ ...body, answer: 'Em chưa biết', revision: record.revision, requestId: 'unknown' });
  assert.equal(record.step, 0); assert.equal(record.checking, true); assert.match(record.explanation, /tế bào/);
  for (let i = 0; i < 3; i++) record = await tutor.answer({ ...body, answer: 'đúng', revision: record.revision, requestId: 'right-' + i });
  assert.equal(record.status, 'completed'); assert.equal(tutor.isComplete(body.id, body.username), false);
  record = await tutor.prepareCopy(body);
  assert.match(record.penalty.content, /Màng tế bào/); assert.match(record.penalty.content, /đơn vị cơ sở/);
  const upload = n => tutor.submitCopy({ ...body, requestId: 'photo-' + n, images: [{ mimeType: 'image/jpeg', base64: Buffer.from([255,216,255,n]).toString('base64') }] });
  record = await upload(1); assert.equal(record.penalty.status, 'copying'); assert.equal(tutor.isComplete(body.id, body.username), false);
  result({ handwritten: true, uncertain: false, count: 1, questionCopied: true, redoCorrect: true, chapterCopied: true, feedback: 'Đủ chương và đáp án.' });
  record = await upload(2); assert.equal(record.penalty.status, 'completed'); assert.equal(tutor.isComplete(body.id, body.username), true);
});

test('Biology registration and thresholds leave Physics/Chemistry/game-entry rules intact', async () => {
  const { tutor } = tutorFixture();
  const bio = await sourceFixture().source(request); const id = tutor.register(bio);
  assert.ok(id); assert.doesNotThrow(() => tutor.assertCanPlay('alice'), 'Biology obligation must not block unrelated games.');
  assert.equal(tutor.register({ ...bio, sourceId: 'biology:pass', score: 6, finalScore: 6 }), '');
  assert.equal(tutor.register({ ...bio, source: 'practice' }), '', 'Old Biology practice remains disabled.');
  for (const subject of ['Physics', 'Chemistry']) {
    const legacyId = tutor.register({ sourceId: `practice:${subject}:0`, username: subject, subject, question: 'Đề', reference: 'Đáp', score: 6 });
    assert.ok(tutor.get(legacyId, subject).penalty); assert.throws(() => tutor.assertCanPlay(subject));
  }
});

test('Biology HTTP route requires pupil authentication and exposes its own capability', async t => {
  const { tutor } = tutorFixture();
  const auth = new TutorAuth({ authenticate: async username => ({ username, teacher: username === 'teacher' }) });
  const server = createServer(new Arena(), undefined, { tutor, tutorAuth: auth, biologySource: sourceFixture().source });
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve)); t.after(() => new Promise(resolve => server.close(resolve)));
  const root = `http://127.0.0.1:${server.address().port}`;
  const post = (path, body, token) => fetch(root + path, { method:'POST', headers:{'Content-Type':'application/json', ...(token ? { Authorization:'Bearer ' + token } : {})}, body:JSON.stringify(body) });
  assert.equal((await (await fetch(root + '/health')).json()).biologyTutorVersion, 1);
  assert.equal((await post('/tutor/biology-start', request)).status, 401);
  const identity = await (await post('/tutor/login', {username:'alice',password:'test'})).json();
  const response = await post('/tutor/biology-start', request, identity.token);
  assert.equal(response.status, 200); assert.equal((await response.json()).sourceId, 'biology:BIO_SUB_1');
  const teacher = await (await post('/tutor/login', {username:'teacher',password:'test'})).json();
  assert.equal((await post('/tutor/biology-start', { ...request, username:'teacher' }, teacher.token)).status, 403);
});

test('Only Biology handwriting prompt adds the chapter check', async () => {
  const sent = [];
  const ai = createCopyAI({ key:'test', fetchImpl: async (url, options) => {
    sent.push(JSON.parse(options.body)); return { ok:true, json:async () => ({ candidates:[{finishReason:'STOP',content:{parts:[{text:JSON.stringify({})}]}}] }) };
  } });
  await ai.check({content:'Legacy',redoQuestion:'Đề',redoSolution:'Đáp'}, []);
  await ai.check({biologyChapter:true,chapterContent:'Cả chương',redoQuestion:'Đề',redoSolution:'Đáp'}, []);
  assert.doesNotMatch(sent[0].systemInstruction.parts[0].text, /chapterCopied/);
  assert.match(sent[1].systemInstruction.parts[0].text, /chapterCopied/);
  assert.equal(JSON.parse(sent[1].contents[0].parts[0].text).chapterContent, 'Cả chương');
});
