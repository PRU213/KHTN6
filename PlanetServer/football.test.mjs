import test from 'node:test';
import assert from 'node:assert/strict';
import { FootballPractice, createFootballHint, sameChapter } from './football.mjs';
import { createServer } from './server.mjs';

const bank = ['Physics', 'Chemistry'].flatMap(subject => [1, 10].flatMap(chapter => ['Dễ', 'Trung bình', 'Khó'].flatMap(difficulty => Array.from({ length: 6 }, (_, i) => ({
  id: `${subject}:${chapter}:${difficulty}:${i}`, subject, topic: `Chương ${chapter}: Chủ đề`, difficulty, text: `Câu ${i}`, sampleAnswer: 'Đáp án tham khảo', rubric: 'Các ý chính', maxPoints: 10
})))));
const body = (extra = {}) => ({ subject: 'Vật lý', chapter: 'Chương 1', difficulties: ['Dễ'], username: 'student', ...extra });
const review = { score: 8, readable: true, feedback: 'Đúng ý chính', transcription: 'Bài học sinh', suggestedAnswer: 'Bài mẫu' };
function fixture(extra = {}) {
  let now = 1000;
  const practice = new FootballPractice({ source: async () => bank, grade: async () => review, hint: async (_q, level) => `Hướng làm ${level}`, record: async () => {}, now: () => now, ...extra });
  return { practice, advance: ms => { now += ms; } };
}

test('Football selects exactly five questions by subject, chapter and difficulty without answer leakage', async () => {
  for (const subject of ['Vật lý', 'Hóa học']) for (const difficulty of ['Dễ', 'Trung bình', 'Khó']) {
    const { practice } = fixture();
    const state = await practice.start(body({ subject, difficulties: [difficulty] }));
    const session = practice.get(state.token);
    assert.equal(session.questions.length, 5); assert.equal(new Set(session.questions.map(q => q.id)).size, 5);
    assert.ok(session.questions.every(q => q.subject === (subject === 'Vật lý' ? 'Physics' : 'Chemistry') && q.topic.startsWith('Chương 1:') && q.difficulty === difficulty));
    assert.equal(state.duration, { 'Dễ': 60, 'Trung bình': 120, 'Khó': 240 }[difficulty]);
    assert.equal(state.sampleAnswer, ''); assert.equal(state.suggestedAnswer, ''); assert.equal(state.questions, undefined); assert.equal(state.rubric, undefined);
  }
});

test('chapter matching never mistakes chapter 1 for chapter 10 and accepts legacy chapter IDs', () => {
  assert.ok(sameChapter('Chương 1: Các phép đo', 'Chương 1'));
  assert.ok(sameChapter('Chương 1: Các phép đo', 'CH_PHY_01'));
  assert.equal(sameChapter('Chương 10: Chủ đề', 'Chương 1'), false);
  assert.equal(sameChapter('', 'Chương 1'), false);
});

test('exit confirmation pauses the server deadline and resumes exactly once', async () => {
  const { practice, advance } = fixture();
  const state = await practice.start(body()), session = practice.get(state.token);
  advance(10000);
  assert.equal(practice.pause(session).remaining, 50);
  advance(120000);
  assert.equal(practice.view(practice.get(state.token)).state, 'answering');
  assert.equal(practice.pause(session).remaining, 50);
  assert.equal((await practice.submit(session, { round: 1, text: 'answer' })).score, 0);
  assert.equal(practice.resume(session).remaining, 50);
  advance(10000);
  assert.equal(practice.resume(session).remaining, 40);
  advance(40000);
  assert.equal(practice.view(session).state, 'review');
});

for (const game of ['Sheep', 'Cave']) test(`${game} uses the same five-question rules for both subjects and all difficulties`, async () => {
  for (const subject of ['Vật lý', 'Hóa học']) for (const difficulty of ['Dễ', 'Trung bình', 'Khó']) {
    const { practice } = fixture();
    const state = await practice.start(body({ game, subject, difficulties: [difficulty] }));
    const session = practice.get(state.token);
    assert.equal(state.game, game); assert.equal(state.total, 5); assert.equal(state.maxScore, 50);
    assert.equal(state.duration, { 'Dễ': 60, 'Trung bình': 120, 'Khó': 240 }[difficulty]);
    assert.ok(session.questions.every(q => q.subject === (subject === 'Vật lý' ? 'Physics' : 'Chemistry') && sameChapter(q.topic, 'Chương 1') && q.difficulty === difficulty));
    assert.equal(state.sampleAnswer, '');
  }
});

test('Football and Sheep sessions keep hints, points and progress separate', async () => {
  const { practice } = fixture();
  const football = await practice.start(body()), sheep = await practice.start(body({ game: 'Sheep' }));
  assert.notEqual(football.token, sheep.token);
  const a = practice.get(football.token), b = practice.get(sheep.token);
  await practice.getHint(b, { round: 1, level: 1 });
  await practice.submit(b, { round: 1, text: 'Bài của Sheep' });
  assert.equal(practice.view(a).score, 0); assert.deepEqual(practice.view(a).hints, []); assert.equal(a.phase, 'answering');
  assert.equal(practice.view(b).score, 7); assert.equal(b.game, 'Sheep');
  await assert.rejects(practice.start(body({ game: 'Unknown' })), /không hợp lệ/);
});

test('Cave keeps its hints and graded results separate from Football and Sheep', async () => {
  const { practice } = fixture();
  const states = await Promise.all(['Football', 'Sheep', 'Cave'].map(game => practice.start(body({ game }))));
  assert.equal(new Set(states.map(s => s.token)).size, 3);
  const cave = practice.get(states[2].token);
  await practice.getHint(cave, { round: 1, level: 1 });
  const result = await practice.submit(cave, { round: 1, text: 'Bài làm Đào hang' });
  assert.equal(result.game, 'Cave'); assert.equal(result.earned, 7);
  assert.equal(result.sampleAnswer, 'Đáp án tham khảo'); assert.equal(result.suggestedAnswer, 'Bài mẫu');
  for (const state of states.slice(0, 2)) {
    const untouched = practice.view(practice.get(state.token));
    assert.equal(untouched.score, 0); assert.equal(untouched.state, 'answering'); assert.deepEqual(untouched.hints, []);
  }
  const next = practice.next(cave, { round: 1 });
  assert.equal(next.round, 2); assert.equal(next.remaining, 60); assert.deepEqual(next.hints, []);
});

for (const game of ['Sheep', 'Cave']) test(`${game} can submit a photo and finish five questions for a maximum of fifty points`, async () => {
  let receivedPhoto = false;
  const { practice } = fixture({ grade: async (_q, text, image) => {
    if (image) { receivedPhoto = true; assert.equal(text, ''); }
    return { ...review, score: 10 };
  } });
  const state = await practice.start(body({ game })), s = practice.get(state.token);
  const photo = Buffer.from([137,80,78,71,13,10,26,10]).toString('base64');
  for (let round = 1; round <= 5; round++) {
    const result = await practice.submit(s, round === 1 ? { round, text: '', imageBase64: photo, mimeType: 'image/png' } : { round, text: 'Bài làm' });
    assert.equal(result.state, 'review'); practice.next(s, { round });
  }
  assert.ok(receivedPhoto); assert.equal(practice.view(s).score, 50); assert.equal(practice.view(s).state, 'finished');
});

test('insufficient questions and missing lesson selection fail instead of borrowing other chapters', async () => {
  const { practice } = fixture({ source: async () => bank.filter(q => q.id.endsWith(':0')) });
  await assert.rejects(practice.start(body()), /Chưa đủ 5/);
  await assert.rejects(practice.start(body({ chapter: '' })), /chương/);
  await assert.rejects(practice.start(body({ subject: 'Sinh học' })), /Lí\/Hóa/);
});

test('two hints are cached, sequential and deduct one plus two points without revealing the reference', async () => {
  let calls = 0;
  const { practice } = fixture({ hint: async (_q, level, previous) => { calls++; assert.equal(previous.length, level - 1); return `Gợi ý ${level}`; } });
  const state = await practice.start(body()), s = practice.get(state.token);
  const first = await practice.getHint(s, { round: 1, level: 1 });
  assert.deepEqual(first.hints, ['Gợi ý 1']); assert.equal(first.score, 0); assert.equal(first.sampleAnswer, '');
  await practice.getHint(s, { round: 1, level: 1 }); assert.equal(calls, 1);
  await practice.getHint(s, { round: 1, level: 2 }); assert.equal(calls, 2);
  await practice.getHint(s, { round: 1, level: 2 }); assert.equal(calls, 2);
  await assert.rejects(practice.getHint(s, { round: 1, level: 3 }), /hai gợi ý/);
  const result = await practice.submit(s, { round: 1, text: 'Bài' }); assert.equal(result.score, 5);
  assert.equal(result.rawScore, 8); assert.equal(result.hintPenalty, 3); assert.equal(result.earned, 5);
});

for (const game of ['Football', 'Sheep', 'Cave']) test(`${game} applies hint costs to final points and recorded results, resetting each question`, async () => {
  for (const used of [0, 1, 2]) {
    const records = [];
    const { practice } = fixture({ record: async value => records.push(value) });
    const state = await practice.start(body({ game })), s = practice.get(state.token);
    for (let level = 1; level <= used; level++) await practice.getHint(s, { round: 1, level });
    const penalty = [0, 1, 3][used];
    const result = await practice.submit(s, { round: 1, text: 'Bài', hintPenalty: -999, score: 999 });
    assert.equal(result.rawScore, 8); assert.equal(result.hintPenalty, penalty); assert.equal(result.earned, 8 - penalty);
    assert.equal(result.score, 8 - penalty);
    assert.equal(records[0].rawScore, 8); assert.equal(records[0].score, 8 - penalty); assert.equal(records[0].hintPenalty, penalty);
    const again = await practice.submit(s, { round: 1, text: 'Trùng' }); assert.equal(again.score, result.score);
    assert.equal(practice.next(s, { round: 1 }).hintPenalty, 0);
    const next = await practice.submit(s, { round: 2, text: 'Bài mới' }); assert.equal(next.earned, 8); assert.equal(next.score, result.score + 8);
  }
});

test('failed or empty hints are free, while low scores and timeouts never become negative', async () => {
  let fail = false;
  const { practice, advance } = fixture({ grade: async () => ({ ...review, score: 2 }), hint: async () => { if (fail) throw new Error('offline'); return 'Gợi ý'; } });
  const state = await practice.start(body()), s = practice.get(state.token);
  await practice.getHint(s, { round: 1, level: 1 }); fail = true;
  await assert.rejects(practice.getHint(s, { round: 1, level: 2 })); assert.equal(practice.view(s).hintPenalty, 1);
  practice.hint = async () => '';
  await assert.rejects(practice.getHint(s, { round: 1, level: 2 })); assert.equal(practice.view(s).hintPenalty, 1);
  practice.hint = async () => 'Gợi ý 2'; await practice.getHint(s, { round: 1, level: 2 });
  const result = await practice.submit(s, { round: 1, text: 'Bài' });
  assert.equal(result.rawScore, 2); assert.equal(result.hintPenalty, 3); assert.equal(result.earned, 0); assert.equal(result.score, 0);
  practice.next(s, { round: 1 }); await practice.getHint(s, { round: 2, level: 1 });
  advance(60000); const expired = practice.view(s); assert.equal(expired.earned, 0); assert.equal(expired.score, 0); assert.equal(expired.rawScore, 0);
});

test('failed hints do not consume allowance and late hint results do not enter the next question', async () => {
  let calls = 0, finish;
  const { practice } = fixture({ hint: async () => { if (++calls === 1) throw new Error('offline'); return new Promise(resolve => { finish = resolve; }); } });
  const state = await practice.start(body()), s = practice.get(state.token);
  await assert.rejects(practice.getHint(s, { round: 1, level: 1 })); assert.equal(s.hints.length, 0); assert.equal(s.hintPending, false);
  const pending = practice.getHint(s, { round: 1, level: 1 });
  await practice.submit(s, { round: 1, text: 'Bài' }); practice.next(s, { round: 1 });
  finish('Gợi ý câu cũ'); await pending;
  assert.deepEqual(s.hints, []); assert.equal(s.hintPending, false);
});

test('duplicate submissions score only once and finished practice totals at most fifty', async () => {
  let finish, calls = 0;
  const { practice } = fixture({ grade: async () => { calls++; return new Promise(resolve => { finish = resolve; }); } });
  const state = await practice.start(body()), s = practice.get(state.token);
  for (let round = 1; round <= 5; round++) {
    const pending = practice.submit(s, { round, text: 'Bài' });
    assert.equal((await practice.submit(s, { round, text: 'Trùng' })).state, 'grading');
    finish({ ...review, score: 10 }); await pending;
    const again = await practice.submit(s, { round, text: 'Trùng' }); assert.equal(again.score, round * 10);
    practice.next(s, { round });
  }
  assert.equal(calls, 5); assert.equal(s.phase, 'finished'); assert.equal(practice.view(s).score, 50);
});

test('timeout reveals the solution and next question receives its own deadline', async () => {
  const { practice, advance } = fixture();
  const state = await practice.start(body()), s = practice.get(state.token);
  advance(60000); const expired = practice.view(s);
  assert.equal(expired.state, 'review'); assert.equal(expired.earned, 0); assert.equal(expired.sampleAnswer, 'Đáp án tham khảo');
  const next = practice.next(s, { round: 1 }); assert.equal(next.round, 2); assert.equal(next.remaining, 60);
  await assert.rejects(practice.submit(s, { round: 1, text: 'Bài cũ' }), /thay đổi/);
});

test('unreadable images and provider errors allow resubmission with a twenty-second grace period', async () => {
  let calls = 0;
  const { practice, advance } = fixture({ grade: async () => { advance(20000); if (++calls === 1) return { readable: false }; if (calls === 2) throw new Error('offline'); return review; } });
  const state = await practice.start(body()), s = practice.get(state.token); advance(50000);
  const image = Buffer.from([137,80,78,71,13,10,26,10]).toString('base64');
  const result = await practice.submit(s, { round: 1, text: '', imageBase64: image, mimeType: 'image/png' });
  assert.match(result.error, /Ảnh/); assert.equal(result.state, 'answering'); assert.equal(s.score, 0); assert.equal(s.gradePending, false);
  await assert.rejects(practice.submit(s, { round: 1, text: 'Bài' }), /20 giây/);
  assert.ok(practice.view(s).remaining >= 20);
  await practice.submit(s, { round: 1, text: 'Bài' }); assert.equal(s.score, 8);
});

for (const game of ['Football', 'Sheep', 'Cave']) test(`${game} grades a readable off-topic photo as zero instead of requesting another image`, async () => {
  const { practice } = fixture({ grade: async () => ({ score: 0, readable: true, answerStatus: 'off_topic',
    feedback: 'Nội dung ảnh không liên quan đến câu hỏi.', transcription: 'Understanding Mono-Processor Systems', suggestedAnswer: 'Bài mẫu.' }) });
  const state = await practice.start(body({ game })), s = practice.get(state.token);
  const image = Buffer.from([137,80,78,71,13,10,26,10]).toString('base64');
  const result = await practice.submit(s, { round: 1, text: '', imageBase64: image, mimeType: 'image/png' });
  assert.equal(result.state, 'review'); assert.equal(result.earned, 0); assert.equal(result.error, undefined);
  assert.match(result.feedback, /không liên quan/); assert.match(result.transcription, /Mono-Processor/);
  assert.equal(practice.next(s, { round: 1 }).round, 2);
});

test('hint provider uses the trusted reference without publishing it as a response field', async () => {
  let captured;
  const hint = createFootballHint({ key: 'test', model: 'test', fetchImpl: async (_url, options) => {
    captured = JSON.parse(options.body);
    return { ok: true, json: async () => ({ candidates: [{ finishReason: 'STOP', content: { parts: [{ text: 'Hãy xác định đại lượng cần tìm.' }] } }] }) };
  } });
  assert.equal(await hint(bank[0], 1, []), 'Hãy xác định đại lượng cần tìm.');
  assert.match(captured.systemInstruction.parts[0].text, /không đưa đáp số/);
  assert.equal(JSON.parse(captured.contents[0].parts[0].text).reference, bank[0].sampleAnswer);
});

for (const game of ['Football', 'Sheep', 'Cave']) test(`${game} HTTP start, hint, submit, next and leave work independently of duel tickets`, async () => {
  const { practice } = fixture();
  const server = createServer(undefined, undefined, { football: practice });
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
  async function post(path, data) {
    const response = await fetch(`http://127.0.0.1:${server.address().port}/football/${path}`, { method: 'POST', body: JSON.stringify(data) });
    assert.equal(response.status, 200); return response.json();
  }
  try {
    const state = await post('start', body({ game }));
    assert.equal(state.game, game);
    await post('hint', { token: state.token, round: 1, level: 1 });
    const result = await post('submit', { token: state.token, round: 1, text: 'Bài làm', score: 999 });
    assert.equal(result.score, 7); assert.equal(result.state, 'review');
    assert.equal((await post('next', { token: state.token, round: 1 })).round, 2);
    await post('leave', { token: state.token }); assert.equal(practice.sessions.size, 0);
  } finally { server.closeAllConnections(); await new Promise(resolve => server.close(resolve)); }
});
