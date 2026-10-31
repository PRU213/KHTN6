import test from 'node:test';
import assert from 'node:assert/strict';
import { mkdtempSync, rmSync, readFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { Arena, createServer } from './server.mjs';
import { EssayTutor } from './tutor.mjs';
import { TutorAuth } from './tutor-auth.mjs';

const request = (username = 'alice', extra = {}) => ({ name: username, username, difficulties: ['Dễ'],
  guns: [{ x: -650, y: -400 }, { x: 650, y: -400 }],
  targets: [{ x: -400, y: 100 }, { x: -120, y: 180 }, { x: 160, y: 180 }, { x: 440, y: 100 }], ...extra });
const ai = { plan: async () => ({ steps: [1, 2, 3].map(n => ({ question: 'Bước ' + n, expected: 'đúng', explanation: 'Gợi ý ' + n,
  checkQuestion: 'Thử lại ' + n, checkExpected: 'đúng' })), solution: 'Lời giải cuối cùng' }), evaluate: async ({ answer }) => ({ correct: answer === 'đúng' }) };
function fixture(t, options = {}) {
  let now = 1000;
  const tutor = new EssayTutor({ ai, ...options });
  const arena = new Arena({ now: () => now, recordEssay: async () => {}, gradeEssay: async (_q, answer) => ({ score: 4, feedback: 'Lời giải riêng', transcription: answer }) });
  const tutorAuth = new TutorAuth({ authenticate: async username => ({ username, teacher: username === 'teacher' }) });
  const server = createServer(arena, undefined, { tutor, tutorAuth });
  t.after(() => server.close());
  const advance = ms => { now += ms; for (const p of arena.players.values()) p.seen = now; arena.tick(); };
  const start = (extra = {}) => {
    const joined = arena.join(request('alice', extra)); arena.join(request('bob', extra));
    const p = arena.players.get(joined.token), m = p.match; advance(4000); return { p, m };
  };
  return { tutor, arena, server, tutorAuth, start, advance };
}
async function complete(tutor, id, username = 'alice') {
  await tutor.start({ id, username });
  for (let revision = 0; revision < 3; revision++) await tutor.answer({ id, username, revision, requestId: 'answer-' + revision, answer: 'đúng' });
}

test('MC click history remains personal, wrong-then-correct still needs steps, initial scores never change', async t => {
  const { arena, tutor, start, advance } = fixture(t), { p, m } = start();
  const correct = m.questions[0].correct, wrong = (correct + 1) % 4;
  arena.choose(m, 0, wrong, 1); advance(1500); arena.choose(m, 0, correct, 1);
  arena.finish(m, 0);
  const scores = [...m.scores], a = arena.view(p), b = arena.view(m.players[1]);
  assert.deepEqual(a.reviewQuestions[0].choices, [wrong, correct]);
  assert.equal(a.reviewQuestions[0].status, 'wrong'); assert.equal(a.reviewPending, 1);
  assert.equal(b.reviewQuestions[0].status, 'unanswered'); assert.deepEqual(b.reviewQuestions[0].choices, []);
  assert.equal(a.reviewQuestions[0].correct, -1); assert.equal(a.reviewQuestions[0].sampleAnswer, ''); assert.equal(a.correct, -1);
  const id = a.reviewQuestions[0].tutorId;
  assert.ok(id); assert.notEqual(id, b.reviewQuestions[0].tutorId);
  assert.throws(() => arena.join(request()), e => e.reviewRequired && e.reviewPending === 1);
  let state = await tutor.start({ id, username: 'alice' });
  state = await tutor.answer({ id, username: 'alice', revision: state.revision, requestId: 'wrong-step', answer: 'sai' });
  assert.equal(state.step, 0); assert.equal(state.checking, true); assert.equal(state.solution, '');
  for (let i = 0; i < 3; i++) state = await tutor.answer({ id, username: 'alice', revision: state.revision, requestId: 'correct-step-' + i, answer: 'đúng' });
  assert.equal(state.status, 'completed'); assert.equal(state.solution, 'Lời giải cuối cùng');
  assert.equal(arena.view(p).reviewPending, 0); assert.equal(arena.view(p).reviewQuestions[0].tutorCompleted, true);
  assert.deepEqual(m.scores, scores); assert.equal(m.winner, 0);
  assert.equal(arena.join(request()).state, 'searching');
});

test('bullet collisions register the same wrong-answer history as direct clicks', t => {
  const { arena, start } = fixture(t), { p, m } = start();
  const wrong = (m.questions[0].correct + 1) % 4, target = m.movingTargets[wrong];
  m.bullets.push({ id: 1, player: 0, x: target.x, y: target.y, dx: 0, dy: 0 });
  arena.tick(); arena.finish(m, 1);
  assert.deepEqual(arena.view(p).reviewQuestions[0].choices, [wrong]);
  assert.equal(arena.view(p).reviewQuestions[0].status, 'wrong'); assert.equal(m.scores[0], -10);
});

test('clean correct answers need no remediation; future and countdown rounds are excluded', t => {
  const { arena, start } = fixture(t), { p, m } = start();
  arena.choose(m, 0, m.questions[0].correct, 1); arena.finish(m, 0);
  assert.equal(arena.view(p).reviewQuestions.length, 1); assert.equal(arena.view(p).reviewPending, 0);
  assert.equal(arena.view(p).reviewQuestions[0].status, 'correct');
  const joined = arena.join(request('carol')); arena.playWithBot(arena.players.get(joined.token));
  const c = arena.players.get(joined.token); arena.finish(c.match, 0);
  assert.deepEqual(arena.view(c).reviewQuestions, []);
});

test('Biology essays use identical step remediation; AI still grading is not labelled wrong', async t => {
  const { arena, start } = fixture(t), { p, m } = start({ questionType: 'essay' });
  m.questions[0].subject = 'Biology';
  await arena.submitEssay(p, 'Bài làm học sinh', 1); m.submissions[1] = 1;
  arena.finish(m, 0);
  const a = arena.view(p).reviewQuestions[0], b = arena.view(m.players[1]).reviewQuestions[0];
  assert.equal(a.status, 'wrong'); assert.ok(a.tutorId); assert.equal(a.sampleAnswer, '');
  assert.equal(a.transcription, 'Bài làm học sinh'); assert.equal(b.status, 'ungraded'); assert.equal(b.needsTutor, false);
});

test('failed registration blocks new joins and retries; timeout cleanup retains unsaved history', t => {
  const { arena, start, advance } = fixture(t), { p, m } = start();
  const register = arena.registerTutor; arena.registerTutor = () => { throw new Error('disk'); };
  arena.finish(m, 0);
  assert.equal(arena.view(p).reviewQuestions[0].tutorCompleted, false);
  assert.throws(() => arena.join(request()), e => e.reviewRequired && e.reviewQuestions === undefined);
  advance(301000); assert.equal(arena.matches.has(m.id), true);
  arena.registerTutor = register;
  assert.throws(() => arena.join(request()), e => e.reviewRequired);
  assert.ok(arena.pendingReviews('alice')[0].tutorId);
});

test('pending Planet obligations and completion survive restart without live match sessions', async t => {
  const directory = mkdtempSync(join(tmpdir(), 'khtn-duel-review-'));
  t.after(() => rmSync(directory, { recursive: true, force: true }));
  const first = fixture(t, { directory }), { p, m } = first.start();
  first.arena.finish(m, 0); const id = first.arena.view(p).reviewQuestions[0].tutorId;
  const restored = new EssayTutor({ ai, directory });
  const arena = new Arena(), server = createServer(arena, undefined, { tutor: restored }); t.after(() => server.close());
  assert.throws(() => arena.join(request()), e => e.reviewRequired);
  await complete(restored, id);
  const final = new EssayTutor({ ai, directory }); assert.equal(final.get(id, 'alice').status, 'completed');
  assert.equal(arena.join(request()).state, 'searching');
});

test('authenticated local registration validates bank and ignores forged completion/reference flags', async t => {
  const { server, tutorAuth, tutor } = fixture(t);
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
  t.after(() => server.closeAllConnections());
  const base = `http://127.0.0.1:${server.address().port}`;
  const login = await tutorAuth.login({ username: 'alice', password: 'test' }, 'local');
  const teacher = await tutorAuth.login({ username: 'teacher', password: 'test' }, 'teacher');
  const post = async (path, body, token = login.token) => {
    const response = await fetch(base + path, { method: 'POST', headers: { Authorization: 'Bearer ' + token }, body: JSON.stringify(body) });
    return { status: response.status, data: await response.json() };
  };
  const q = JSON.parse(readFileSync(new URL('./questions.json', import.meta.url), 'utf8')).find(q => q.subject === 'Biology');
  const body = { username: 'alice', matchId: 'local-' + 'a'.repeat(32), reviewQuestions: [{ round: 1, text: q.text, subject: q.subject, options: q.options,
    choices: [(q.correct + 1) % 4], answeredCorrectly: true, tutorCompleted: true, sampleAnswer: 'Forged answer' }] };
  assert.equal((await post('/review/local', body, '')).status, 401);
  assert.equal((await post('/review/local', body, teacher.token)).status, 403);
  assert.equal((await post('/review/local', { ...body, username: 'bob' })).status, 403);
  assert.equal((await post('/review/local', { ...body, reviewQuestions: [{ ...body.reviewQuestions[0], text: 'Forged question' }] })).status, 400);
  const first = await post('/review/local', body);
  assert.equal(first.status, 200); assert.equal(first.data.reviewPending, 1); assert.equal(first.data.reviewQuestions[0].tutorCompleted, false);
  const id = first.data.reviewQuestions[0].tutorId; assert.ok(id);
  assert.equal(first.data.reviewQuestions[0].sourceId, `planet:${body.matchId}:0:1`);
  assert.equal(tutor.get(id, 'alice').reference, q.options[q.correct]);
  assert.ok(tutor.get(id, 'alice').question.includes(q.options[0]));
  assert.equal((await post('/review/local', body)).data.reviewQuestions[0].tutorId, id);
  assert.equal(tutor.list('alice').records.length, 1);
  const register = tutor.register.bind(tutor);
  tutor.register = () => { throw new Error('disk unavailable'); };
  assert.equal((await post('/review/local', body)).status, 503);
  tutor.register = register;
  assert.equal((await post('/review/local', body)).data.reviewQuestions[0].tutorId, id);
  const blocked = await post('/join', request()); assert.equal(blocked.data.reviewRequired, true);
  assert.equal(blocked.data.reviewQuestions, undefined); assert.equal(JSON.stringify(blocked.data).includes('transcription'), false);
  assert.equal((await post('/review/pending', { username: 'alice' })).data.reviewPending, 1);
  await complete(tutor, id);
  const done = await post('/review/pending', { username: 'alice' });
  assert.equal(done.data.reviewPending, 0); assert.equal(done.data.reviewQuestions[0].tutorCompleted, true);
  assert.equal((await post('/join', request())).status, 200);
});

test('a pre-existing queue ticket cannot use automatic/manual bot matching to bypass pending lessons', t => {
  const { arena, tutor, advance } = fixture(t);
  const joined = arena.join(request()), p = arena.players.get(joined.token);
  tutor.register({ source: 'planet', sourceId: 'planet:previous:0:1', username: 'alice', subject: 'Biology', question: 'Câu hỏi cũ', reference: 'Đáp án kín',
    review: { needsTutor: true, questionType: 'essay', round: 1 }, score: 0 });
  assert.throws(() => arena.playWithBot(p), e => e.reviewRequired);
  advance(120000);
  const state = arena.view(p);
  assert.equal(p.match, null); assert.equal(state.reviewRequired, true); assert.equal(state.reviewQuestions, undefined);
});

test('upgrading old Planet metadata preserves the active lesson and its answers', async t => {
  const { tutor } = fixture(t);
  const input = { sourceId: 'planet:old:0:1', username: 'alice', subject: 'Physics', question: 'Câu cũ', reference: 'Đáp án', score: 4 };
  const id = tutor.register(input); await tutor.start({ id, username: 'alice' });
  await tutor.answer({ id, username: 'alice', revision: 0, requestId: 'old-answer', answer: 'sai' });
  assert.equal(tutor.register({ ...input, source: 'planet', review: { round: 1, needsTutor: true } }), id);
  const upgraded = tutor.get(id, 'alice');
  assert.equal(upgraded.source, 'planet'); assert.equal(upgraded.events.length, 1); assert.equal(upgraded.checking, true); assert.equal(upgraded.revision, 1);
});
