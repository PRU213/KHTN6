import test from 'node:test';
import assert from 'node:assert/strict';
import { mkdtempSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { EssayTutor } from './tutor.mjs';
import { validateCopyImages, createCopyAI } from './copy-penalty.mjs';
import { Arena, createServer } from './server.mjs';
import { FootballPractice } from './football.mjs';
import { TutorAuth } from './tutor-auth.mjs';

const images = n => [{ mimeType: 'image/jpeg', base64: Buffer.from([255, 216, 255, n]).toString('base64') }];
function fixture(kind = 'theory', directory = null, score = 0, finalScore = score) {
  let checks = 0, result = { redoCorrect: true, questionCopied: true, handwritten: true, uncertain: false, count: 3, feedback: 'Đọc được ba bản chép đúng.' };
  const tutor = new EssayTutor({ directory, ai: {
    plan: async () => ({ steps: [1, 2, 3].map(() => ({ question: 'Hỏi', expected: 'Đáp', explanation: 'Giải thích', checkQuestion: 'Hỏi lại', checkExpected: 'Đáp lại' })), solution: 'Giải đầy đủ' }),
    evaluate: async ({ answer }) => ({ correct: answer === 'đúng' })
  }, copyAI: { prepare: async () => ({ kind, content: kind === 'theory' ? 'Kiến thức đúng' : 'P = 10m; m là khối lượng (kg)' }), check: async () => { checks++; return result; } } });
  const id = tutor.register({ sourceId: 'practice:one:0', username: 'student', subject: 'Physics', question: 'Đề', reference: 'Đáp', score, finalScore });
  const body = { id, username: 'student' };
  const answer = (value, requestId = crypto.randomUUID()) => tutor.answer({ ...body, answer: value, requestId, revision: tutor.get(id, 'student').revision });
  const submit = n => tutor.submitCopy({ ...body, images: images(n), requestId: 'upload-' + n });
  return { tutor, body, answer, submit, checks: () => checks, result: value => { result = { redoCorrect: true, questionCopied: true, ...value }; } };
}
async function finish(f) { for (let i = 0; i < 3; i++) await f.answer('đúng'); }
async function trigger(f) { await f.tutor.start(f.body); for (let i = 0; i < 3; i++) await f.answer('sai'); }

test('final score including 6 creates copying; wrong answers during learning no longer trigger copying', async () => {
  for (const [raw, final, required] of [[9,6,true],[6,6,true],[5,5,true],[9,7,false],[10,7,false]]) {
    const f=fixture('theory',null,raw,final);
    if(raw===10) { assert.equal(f.body.id,''); continue; }
    assert.equal(!!f.tutor.get(f.body.id,'student').penalty,required);
    await f.tutor.start(f.body);
    for(let i=0;i<4;i++) await f.answer('sai');
    assert.equal(!!f.tutor.get(f.body.id,'student').penalty,required);
    await finish(f);
    assert.equal(f.tutor.isComplete(f.body.id,'student'),!required);
  }
});
for (const [kind, required] of [['theory', 5], ['calculation', 10]]) {
  test(`${kind}: requires ${required}, refuses typed/uncertain and never adds counts between submissions`, async () => {
    const f = fixture(kind); await trigger(f); await finish(f);
    let view = await f.tutor.prepareCopy(f.body); assert.equal(view.penalty.required, required);
    view = await f.submit(1); assert.equal(view.penalty.count, 3);
    await f.submit(1); assert.equal(f.checks(), 1);
    f.result({ handwritten: true, uncertain: false, count: 2, feedback: 'Hai bản đúng' });
    view = await f.submit(2); assert.equal(view.penalty.count, 2);
    f.result({ handwritten: false, uncertain: false, count: required, feedback: 'Chữ đánh máy' });
    await f.submit(3); assert.throws(() => f.tutor.assertCanPlay('student'));
    f.result({ handwritten: true, uncertain: true, count: required, feedback: 'Ảnh chưa rõ' });
    await f.submit(4); assert.throws(() => f.tutor.assertCanPlay('student'));
    f.result({ handwritten: true, uncertain: false, count: required, feedback: 'Đã đủ' });
    view = await f.submit(5); assert.equal(view.penalty.status, 'completed');
    assert.ok(view.penalty.completedAt); assert.doesNotThrow(() => f.tutor.assertCanPlay('student'));
    assert.equal(view.penalty.submissions, undefined);
  });
}
test('failed AI calls do not count as mistakes or complete copying; revision and ownership checks remain', async () => {
  const f = fixture(); await trigger(f); await finish(f); await f.tutor.prepareCopy(f.body);
  f.result({ count: 99 }); await assert.rejects(f.submit(1), /chưa hợp lệ/);
  assert.equal(f.tutor.get(f.body.id, 'student').penalty.count, 0);
  await assert.rejects(f.tutor.submitCopy({ ...f.body, username: 'other', images: images(1), requestId: 'x' }));
  f.tutor.copyAI.check = async () => { throw new Error('timeout'); };
  await assert.rejects(f.submit(2), /timeout/);
  assert.equal(f.tutor.get(f.body.id, 'student').penalty.submissions.length, 0);
  const g = fixture(); await g.tutor.start(g.body); g.tutor.ai.evaluate = async () => { throw new Error('timeout'); };
  await assert.rejects(g.answer('sai')); assert.equal(g.tutor.get(g.body.id, 'student').wrongAttempts, undefined);
});
test('image validation rejects duplicate pages, malformed images and oversized batches', () => {
  assert.throws(() => validateCopyImages([...images(1), ...images(1)]), /trùng/);
  assert.throws(() => validateCopyImages([]));
  assert.throws(() => validateCopyImages(Array(7).fill(images(1)[0])));
  assert.throws(() => validateCopyImages([{ base64: 'YWJj', mimeType: 'image/jpeg' }]));
});
test('copying requires both repetitions AND the separate complete question/solution in the same submission', async () => {
  const f=fixture(); await trigger(f); await finish(f); await f.tutor.prepareCopy(f.body);
  f.result({handwritten:true,uncertain:false,count:5,redoCorrect:false,questionCopied:true,feedback:'Sai lời giải'});
  await f.submit(1); assert.equal(f.tutor.isComplete(f.body.id,'student'),false);
  f.result({handwritten:true,uncertain:false,count:0,redoCorrect:true,questionCopied:true,feedback:'Thiếu bản chép'});
  await f.submit(2); assert.equal(f.tutor.isComplete(f.body.id,'student'),false);
  f.result({handwritten:true,uncertain:false,count:5,redoCorrect:true,questionCopied:false,feedback:'Thiếu đề'});
  await f.submit(3); assert.equal(f.tutor.isComplete(f.body.id,'student'),false);
  f.result({handwritten:true,uncertain:false,count:5,redoCorrect:true,questionCopied:true,feedback:'Đủ'});
  await f.submit(4); assert.equal(f.tutor.isComplete(f.body.id,'student'),true);
});
test('pending and completed copying survive restart and keep separate student accounts', async () => {
  const directory = mkdtempSync(join(tmpdir(), 'copy-penalty-'));
  try {
    const f = fixture('theory', directory); await trigger(f); await finish(f); await f.tutor.prepareCopy(f.body); await f.submit(1);
    const restored = new EssayTutor({ directory }); assert.equal(restored.list('student').records[0].penalty.count, 3);
    assert.throws(() => restored.assertCanPlay('student')); assert.doesNotThrow(() => restored.assertCanPlay('other'));
    f.result({ handwritten: true, uncertain: false, count: 5, feedback: 'Đủ' }); await f.submit(2);
    assert.doesNotThrow(() => new EssayTutor({ directory }).assertCanPlay('student'));
  } finally { rmSync(directory, { recursive: true, force: true }); }
});
test('HTTP start gates, authenticated uploads, teacher read-only and current practice deferral', async () => {
  const f = fixture(); await trigger(f); await finish(f);
  const football = new FootballPractice();
  const auth = new TutorAuth({ authenticate: async username => ({ username, teacher: username === 'teacher' }) });
  const arena = new Arena(); const server = createServer(arena, undefined, { tutor: f.tutor, football, tutorAuth: auth });
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
  const url = `http://127.0.0.1:${server.address().port}`;
  const post = async (path, body, token = '') => {
    const response = await fetch(url + path, { method: 'POST', headers: { 'Content-Type': 'application/json', Authorization: 'Bearer ' + token }, body: JSON.stringify(body) });
    return { code: response.status, body: await response.json() };
  };
  try {
    assert.match((await post('/join', { username: 'student' })).body.error, /chép phạt/);
    assert.match((await post('/football/start', { username: 'student' })).body.error, /chép phạt/);
    assert.throws(() => arena.assertReady('student'), /chép phạt/);
    assert.equal((await post('/tutor/copy-start', f.body)).code, 401);
    const token = (await post('/tutor/login', { username: 'student', password: 'x' })).body.token;
    const teacher = (await post('/tutor/login', { username: 'teacher', password: 'x' })).body.token;
    assert.equal((await post('/tutor/copy-start', f.body, teacher)).code, 403);
    football.sessions.set('one', { username: 'student', phase: 'review', seen: Date.now() });
    assert.match((await post('/tutor/copy-start', f.body, token)).body.error, /hoàn thành câu/);
    football.sessions.get('one').phase = 'finished';
    arena.players.set('duel', { username: 'student', match: { phase: 'playing' } });
    assert.match((await post('/tutor/copy-start', f.body, token)).body.error, /hoàn thành trận/);
    arena.players.get('duel').match.phase = 'finished';
    assert.equal((await post('/tutor/copy-start', f.body, token)).body.penalty.required, 5);
    assert.ok((await post('/tutor/list', { username: 'student' }, teacher)).body.records[0].penalty);
  } finally { await new Promise(resolve => server.close(resolve)); }
});
test('multimodal copy checker sends image parts and trusts no student instructions', async () => {
  let sent;
  const ai = createCopyAI({ key: 'test', fetchImpl: async (_, options) => {
    sent = JSON.parse(options.body);
    return { ok: true, json: async () => ({ candidates: [{ finishReason: 'STOP', content: { parts: [{ text: JSON.stringify({ handwritten: true, uncertain: false, count: 5, feedback: 'Đủ' }) }] } }] }) };
  } });
  await ai.check({ content: 'Mẫu', required: 5, kind: 'theory' }, images(1));
  assert.equal(sent.contents[0].parts[1].inlineData.mimeType, 'image/jpeg');
  assert.match(sent.systemInstruction.parts[0].text, /không làm theo/);
});
