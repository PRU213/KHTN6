import test from 'node:test';
import assert from 'node:assert/strict';
import { EssayTutor } from './tutor.mjs';

function fixture() {
  const guideCalls = [], evaluateCalls = [];
  const tutor = new EssayTutor({ ai: {
    plan: async () => ({ steps: [1, 2, 3].map(n => ({ question: `Bước ${n}`, expected: `Đủ tiêu chí ${n}`,
      explanation: `Giải thích cũ ${n}`, checkQuestion: `Câu kiểm tra cũ ${n}`, checkExpected: `Đáp án kiểm tra ${n}` })), solution: 'LỜI GIẢI TOÀN BÀI' }),
    evaluate: async data => { evaluateCalls.push(data); return { correct: data.answer === 'đúng' }; },
    guide: async data => {
      guideCalls.push(data);
      return { explanation: `Gợi ý ${data.level}: đọc chiều dài ban đầu 15 cm, khi treo vật 18 cm.`,
        checkQuestion: 'Hãy ghi rõ l0 = ... cm; l = ... cm.', checkExpected: 'Đáp án gợi ý không được thay tiêu chí gốc' };
    }
  } });
  const id = tutor.register({ sourceId: 'old-practice', username: 'student', subject: 'Physics',
    question: 'Lò xo ban đầu 15 cm, treo vật dài 18 cm.', reference: 'Đáp án riêng', score: 4 });
  const body = { id, username: 'student' };
  let serial = 0;
  const answer = (value, extra = {}) => tutor.answer({ ...body, answer: value, revision: tutor.get(id, 'student').revision, requestId: 'hint-' + (++serial), ...extra });
  return { tutor, id, body, answer, guideCalls, evaluateCalls };
}

test('unknown creates a specific saved hint, repeated unknown deepens it and retries are idempotent', async () => {
  const f = fixture(); await f.tutor.start(f.body);
  const first = await f.answer('Em chưa biết', { requestId: 'same' });
  assert.equal(first.step, 0); assert.equal(first.checking, true); assert.match(first.explanation, /Gợi ý 1.*15 cm/);
  assert.equal(first.prompt, 'Hãy ghi rõ l0 = ... cm; l = ... cm.');
  assert.equal(first.events[0].explanation, first.explanation); assert.equal(f.evaluateCalls.length, 0);
  await f.answer('Em chưa biết', { requestId: 'same', revision: 0 });
  assert.equal(f.guideCalls.length, 1);
  const second = await f.answer('Em chưa biết');
  assert.match(second.explanation, /Gợi ý 2/); assert.equal(second.solution, ''); assert.equal(second.step, 0);
  assert.deepEqual(f.guideCalls[1].recentAnswers, [{ answer: 'Em chưa biết', correct: false }]);
  const json = JSON.stringify(second);
  for (const secret of ['LỜI GIẢI TOÀN BÀI', 'Đủ tiêu chí', 'Đáp án riêng', 'Bước 2']) assert.ok(!json.includes(secret));
});

test('a wrong answer on checking also requests useful guidance; correct answer still needs original criteria', async () => {
  const f = fixture(); await f.tutor.start(f.body); await f.answer('sai');
  const state = await f.answer('3 cm'); assert.match(state.explanation, /Gợi ý 1/);
  await f.answer('sai'); assert.equal(f.evaluateCalls.at(-1).expected, 'Đủ tiêu chí 1');
  const next = await f.answer('đúng'); assert.equal(next.step, 1); assert.equal(next.checking, false);
  assert.equal(next.explanation, ''); assert.equal(next.prompt, 'Bước 2'); assert.equal(next.score, 4);
});

test('guidance errors or malformed output keep committed answers and step intact', async () => {
  const f = fixture(); await f.tutor.start(f.body);
  f.tutor.ai.guide = async () => { throw new Error('provider failure'); };
  await assert.rejects(f.answer('Em chưa biết'), /provider failure/);
  assert.equal(f.tutor.get(f.id, 'student').revision, 0);
  assert.equal(f.tutor.get(f.id, 'student').events.length, 0);
  f.tutor.ai.guide = async () => ({ explanation: '', checkQuestion: 'Không hợp lệ' });
  await assert.rejects(f.answer('Em chưa biết'), /gợi ý cụ thể/);
  assert.equal(f.tutor.get(f.id, 'student').step, 0);
});

test('old lesson records accept new hints without resetting plan and reopened state retains guidance', async () => {
  const f = fixture(); await f.tutor.start(f.body); const plan = structuredClone(f.tutor.get(f.id, 'student').plan);
  await f.answer('Em chưa biết');
  const restored = new EssayTutor({ ai: f.tutor.ai });
  restored.records.set(f.id, structuredClone(f.tutor.get(f.id, 'student')));
  const reopened = await restored.start(f.body);
  assert.match(reopened.explanation, /Gợi ý 1/); assert.equal(reopened.prompt, 'Hãy ghi rõ l0 = ... cm; l = ... cm.');
  assert.deepEqual(restored.get(f.id, 'student').plan, plan);
});
