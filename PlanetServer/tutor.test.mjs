import test from 'node:test';
import assert from 'node:assert/strict';
import { mkdtempSync, rmSync, readFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { EssayTutor, createTutorAI } from './tutor.mjs';
import { FootballPractice } from './football.mjs';
import { Arena, createServer } from './server.mjs';
import { TutorAuth, parseUsersCSV } from './tutor-auth.mjs';

const plan = () => ({ steps: [1, 2, 3].map(n => ({ question: 'Câu bước ' + n, expected: 'Đúng ' + n,
  explanation: 'Giải thích riêng bước ' + n, checkQuestion: 'Kiểm tra ' + n, checkExpected: 'Hiểu ' + n })), solution: 'LỜI GIẢI CUỐI CÙNG' });
const record = (extra = {}) => ({ sourceId: 'submission-1', username: 'student', subject: 'Physics', chapter: 'Chương 1', question: 'Đề tính lực', reference: 'Đáp án kín', studentAnswer: 'Sai', score: 4, ...extra });
function fixture(options = {}) {
  let calls = 0;
  const tutor = new EssayTutor({ ai: { plan: async () => plan(), evaluate: async ({ answer }) => { calls++; return { correct: answer === 'đúng' }; } }, ...options });
  const id = tutor.register(record());
  const body = { id, username: 'student' };
  const answer = async (value, extra = {}) => tutor.answer({ ...body, requestId: 'request-' + Math.random(), answer: value, revision: tutor.get(id, 'student').revision, ...extra });
  return { tutor, id, body, answer, calls: () => calls };
}

test('only Physics/Chemistry imperfect knowledge scores create records, registration is idempotent', () => {
  const { tutor, id } = fixture();
  assert.equal(tutor.register(record()), id);
  assert.equal(tutor.register(record({ username: 'STUDENT' })), id);
  assert.equal(tutor.get(id, 'STUDENT').id, id);
  assert.equal(tutor.register(record({ subject: 'Biology' })), '');
  assert.equal(tutor.register(record({ score: 10 })), '');
  assert.ok(tutor.register(record({ subject: 'Chemistry', sourceId: 'chemistry' })));
  assert.equal(tutor.list('student').records.length, 2);
  assert.equal(tutor.list('STUDENT').records.length, 2);
  assert.throws(() => tutor.get(id, 'another-student'));
});

test('pending and in-progress views never return plan, reference, expected answers or solution', async () => {
  const { tutor, body } = fixture();
  assert.equal(tutor.list('student').records[0].status, 'pending');
  const view = await tutor.start(body), json = JSON.stringify(view);
  for (const secret of ['LỜI GIẢI CUỐI CÙNG', 'Đáp án kín', 'Đúng 1', 'Hiểu 1', 'Câu bước 2']) assert.ok(!json.includes(secret));
  assert.equal(view.prompt, 'Câu bước 1');
  assert.equal((await tutor.start(body)).step, 0);
});

test('wrong and unknown answers explain just the current step, check again, never advance', async () => {
  const { tutor, body, answer, calls } = fixture(); await tutor.start(body);
  let state = await answer('sai');
  assert.equal(state.step, 0); assert.equal(state.checking, true); assert.equal(state.prompt, 'Kiểm tra 1');
  assert.equal(state.explanation, 'Giải thích riêng bước 1'); assert.equal(state.solution, '');
  state = await answer('Em chưa biết');
  assert.equal(state.step, 0); assert.equal(calls(), 1);
  state = await answer('sai'); assert.equal(state.step, 0); assert.equal(state.events.length, 3);
  state = await answer('đúng'); assert.equal(state.step, 1); assert.equal(state.checking, false); assert.equal(state.prompt, 'Câu bước 2');
});

test('solution unlocks only after every step is answered correctly; score stays unchanged', async () => {
  const { tutor, body, answer } = fixture(); await tutor.start(body);
  for (let i = 1; i <= 3; i++) {
    const view = await answer('đúng');
    assert.equal(view.status, i === 3 ? 'completed' : 'learning'); assert.equal(view.score, 4);
    assert.equal(view.solution, i === 3 ? plan().solution : '');
  }
  assert.equal(tutor.view(tutor.get(body.id, body.username), true).events.length, 3);
  await assert.rejects(answer('đúng'), /mở mục học lại/);
});

test('retry is idempotent, stale answers and empty input do not produce events or call AI', async () => {
  const { tutor, body, answer, calls } = fixture(); await tutor.start(body);
  await answer('sai', { requestId: 'same' });
  await answer('sai', { requestId: 'same', revision: 0 });
  assert.equal(calls(), 1); assert.equal(tutor.get(body.id, body.username).events.length, 1);
  await assert.rejects(answer('đúng', { revision: 0 }), /thay đổi/);
  await assert.rejects(answer(''), /Nhập/); assert.equal(calls(), 1);
});

test('AI failures and malformed plans/evaluations keep the last committed state', async () => {
  const { tutor, body, answer } = fixture();
  tutor.ai.plan = async () => ({ steps: [], solution: '' });
  await assert.rejects(tutor.start(body), /hợp lệ/); assert.equal(tutor.get(body.id, body.username).status, 'pending');
  tutor.ai.plan = async () => plan(); await tutor.start(body);
  tutor.ai.evaluate = async () => ({ correct: 'true' });
  await assert.rejects(answer('đúng'), /kiểm tra/); assert.equal(tutor.get(body.id, body.username).revision, 0);
  tutor.ai.evaluate = async () => { throw new Error('network'); };
  await assert.rejects(answer('đúng'), /network/); assert.equal(tutor.get(body.id, body.username).events.length, 0);
});

test('overlapping requests cannot skip steps', async () => {
  let release;
  const { tutor, body, answer } = fixture(); await tutor.start(body);
  tutor.ai.evaluate = () => new Promise(resolve => { release = resolve; });
  const pending = answer('đúng'); await assert.rejects(answer('đúng'), /đang xử lý/);
  release({ correct: true }); await pending;
  assert.equal(tutor.get(body.id, body.username).step, 1);
});

test('journal survives restart with verification phase, transcript, idempotency and completion', async () => {
  const directory = mkdtempSync(join(tmpdir(), 'khtn-tutor-'));
  try {
    const f = fixture({ directory }); await f.tutor.start(f.body); await f.answer('sai', { requestId: 'persisted' });
    const restored = new EssayTutor({ directory, ai: { evaluate: async () => ({ correct: true }) } });
    const state = await restored.start(f.body);
    assert.equal(state.checking, true); assert.equal(state.events[0].answer, 'sai');
    await restored.answer({ ...f.body, answer: 'sai', revision: 0, requestId: 'persisted' });
    for (let revision = 1; revision < 4; revision++) await restored.answer({ ...f.body, answer: 'đúng', revision, requestId: 'next-' + revision });
    assert.equal(new EssayTutor({ directory }).list('student').records[0].status, 'completed');
  } finally { rmSync(directory, { recursive: true, force: true }); }
});

test('disk failure does not acknowledge or advance an answer', async () => {
  const { tutor, body, answer } = fixture(); await tutor.start(body);
  tutor.directory = join(tmpdir(), 'missing-tutor-parent-' + Math.random(), 'child');
  await assert.rejects(answer('đúng'), /lưu/);
  assert.equal(tutor.get(body.id, body.username).step, 0); assert.equal(tutor.get(body.id, body.username).events.length, 0);
});

test('real practice grading and timeout register remediation, hide answer leaks, leave scores and perfect work intact', async () => {
  const { tutor } = fixture();
  const source = async () => Array.from({ length: 5 }, (_, i) => ({ id: '' + i, subject: 'Physics', topic: 'Chương 1', difficulty: 'Dễ', text: 'Đề ' + i, sampleAnswer: 'ĐÁP ÁN KÍN' }));
  const practice = new FootballPractice({ source, grade: async () => ({ score: 4, feedback: 'LỘ ĐÁP ÁN', suggestedAnswer: 'LỘ LỜI GIẢI' }),
    registerTutor: r => tutor.register(r), isTutorComplete: (id, username) => tutor.get(id, username).status === 'completed' });
  const start = await practice.start({ username: 'practice-user', subject: 'Vật lý', chapter: 'Chương 1', difficulties: ['Dễ'] });
  const session = practice.get(start.token);
  const result = await practice.submit(session, { round: 1, text: 'bài làm' });
  assert.ok(result.tutorId); assert.equal(result.needsTutor, true); assert.equal(result.score, 4);
  assert.equal(result.sampleAnswer, ''); assert.equal(result.feedback, ''); assert.equal(result.suggestedAnswer, '');
  assert.equal(practice.next(session, { round: 1 }).round, 1);
  const body = { id: result.tutorId, username: 'practice-user' }; await tutor.start(body);
  for (let revision = 0; revision < 3; revision++) await tutor.answer({ ...body, answer: 'đúng', revision, requestId: 'practice-' + revision });
  practice.next(session, { round: 1 }); session.deadline = 0;
  assert.ok(practice.view(session).tutorId); assert.equal(tutor.list('practice-user').records.length, 2);
});

test('Planet opts every subject into tutoring while practice Biology remains excluded', () => {
  const { tutor } = fixture(); const arena = new Arena(); arena.registerTutor = r => tutor.register(r);
  const body = { name: 'S', username: 'planet-user', questionType: 'essay', difficulties: ['Dễ'], guns: [{ x: -1, y: -1 }, { x: 1, y: -1 }], targets: [0,1,2,3].map(x => ({ x, y: 0 })) };
  const joined = arena.join(body); arena.join({ ...body, username: 'other' });
  const p = arena.players.get(joined.token), m = p.match;
  for (const subject of ['Biology', 'Physics', 'Chemistry']) {
    m.reviewHistory = null; m.essayHistory = null;
    m.questions[0] = { ...m.questions[0], subject }; m.index = 0; m.phase = 'reveal';
    m.reviews[0] = { score: 4, feedback: 'FEEDBACK', suggestedAnswer: 'SOLUTION' };
    arena.saveEssayReview(m); m.phase = 'finished';
    const state = arena.view(p), entry = state.reviewQuestions[0];
    assert.equal(state.feedback, '');
    assert.equal(entry.suggestedAnswer, '');
    assert.equal(!!entry.tutorId, true);
  }
  assert.equal(tutor.register(record({ subject: 'Biology', sourceId: 'practice-biology' })), '');
});

test('HTTP tutor start/answer/history uses server-owned records and rejects made-up IDs', async t => {
  const { tutor, id } = fixture();
  const tutorAuth = new TutorAuth({ authenticate: async username => ({ username, teacher: username === 'teacher' }) });
  const access = await tutorAuth.login({ username: 'student', password: 'test' }, 'test');
  const teacherAccess = await tutorAuth.login({ username: 'teacher', password: 'test' }, 'teacher-test');
  const server = createServer(undefined, undefined, { tutor, tutorAuth }); await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
  t.after(() => { server.closeAllConnections(); server.close(); });
  const call = async (path, body, token = access.token) => { const response = await fetch(`http://127.0.0.1:${server.address().port}/tutor/${path}`, { method: 'POST', headers: { Authorization: 'Bearer ' + token }, body: JSON.stringify(body) }); return { status: response.status, data: await response.json() }; };
  assert.equal((await call('start', { id: 'made-up', username: 'student' })).status, 400);
  assert.equal((await call('start', { id, username: 'student' })).data.prompt, 'Câu bước 1');
  assert.equal((await call('answer', { id, username: 'student', revision: 0, answer: 'sai', requestId: 'http' })).data.checking, true);
  assert.equal((await call('state', { id, username: 'student' })).data.events.length, 1);
  assert.equal((await call('list', { username: 'another' })).status, 403);
  assert.equal((await call('list', { username: 'student' }, '')).status, 401);
  assert.equal((await call('list', { username: 'student' }, teacherAccess.token)).data.records.length, 1);
  assert.equal((await call('answer', { id, username: 'student', revision: 1, answer: 'đúng', requestId: 'teacher' }, teacherAccess.token)).status, 403);
});

test('journal login verifies the Users sheet and authoritative role; credentials never become a token payload', async () => {
  const users = parseUsersCSV('ID,Username,Password,Fullname,Role\r\n1,student,"p,ass","Tên, Họ",Student\r\n2,teacher,p2,Thầy,Teacher\r\n');
  assert.equal(users[0].password, 'p,ass');
  const auth = new TutorAuth({ fetchImpl: async () => ({ ok: true, text: async () => 'Username,Password,Role\nstudent,p1,Student\nteacher,p2,Teacher' }) });
  await assert.rejects(auth.login({ username: 'teacher', password: 'wrong' }, 'c'), /không đúng/);
  const student = await auth.login({ username: 'STUDENT', password: 'p1', teacher: true }, 'c');
  assert.equal(student.teacher, false); assert.equal(student.token.length, 64);
  const teacher = await auth.login({ username: 'teacher', password: 'p2' }, 'c'); assert.equal(teacher.teacher, true);
  assert.equal(JSON.stringify(student).includes('p1'), false);
});

test('Gemini adapter requests structured JSON, ignores thought content and never accepts partial output', async () => {
  let payload;
  const ai = createTutorAI({ key: 'test', fetchImpl: async (url, options) => { payload = JSON.parse(options.body); return { ok: true, json: async () => ({ candidates: [{ finishReason: 'STOP', content: { parts: [{ thought: true, text: 'hidden' }, { text: '{"correct":true}' }] } }] }) }; } });
  assert.deepEqual(await ai.evaluate({ answer: 'đúng' }), { correct: true });
  assert.equal(payload.generationConfig.responseMimeType, 'application/json');
  const bad = createTutorAI({ key: 'test', fetchImpl: async () => ({ ok: true, json: async () => ({ candidates: [{ finishReason: 'MAX_TOKENS' }] }) }) });
  await assert.rejects(bad.plan(record()), /đủ nội dung/);
});
