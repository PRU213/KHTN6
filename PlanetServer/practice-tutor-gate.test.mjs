import test from 'node:test';
import assert from 'node:assert/strict';
import { FootballPractice } from './football.mjs';
import { EssayTutor } from './tutor.mjs';

const ai = { plan: async () => ({ steps: [1,2,3].map(n => ({ question: 'Bước ' + n, expected: 'đúng', explanation: 'Kiến thức ' + n,
  checkQuestion: 'Kiểm tra ' + n, checkExpected: 'đúng' })), solution: 'Lời giải tổng kết' }), evaluate: async ({ answer }) => ({ correct: answer === 'đúng' }) };
function fixture(subject, game, score = 4) {
  const tutor = new EssayTutor({ ai });
  const practice = new FootballPractice({ source: async () => Array.from({length:5}, (_,i) => ({ id:'q'+i, subject, topic:'Chương 1', difficulty:'Dễ', text:'Đề '+i, sampleAnswer:'Tham khảo' })),
    grade: async () => ({score, feedback:'Nhận xét'}), registerTutor: r => tutor.register(r), isTutorComplete: (id, username) => tutor.get(id, username).status === 'completed' });
  return { tutor, practice, start: () => practice.start({ subject, game, username:'student', chapter:'Chương 1', difficulties:['Dễ'] }) };
}
for (const subject of ['Physics','Chemistry']) for (const game of ['Football','Sheep','Cave']) test(`${game} ${subject}: cannot advance until all tutorial steps are mastered`, async () => {
  const { tutor, practice, start } = fixture(subject, game);
  const state = await start(), s = practice.get(state.token);
  let review = await practice.submit(s, { round:1, text:'sai' });
  assert.equal(review.tutorCompleted, false);
  assert.equal(practice.next(s, { round:1, tutorCompleted:true }).round, 1);
  assert.match(practice.next(s, {round:1}).error, /học lại/);
  const body = {id:review.tutorId, username:'student'}; await tutor.start(body);
  await tutor.answer({...body, revision:0, requestId:'wrong', answer:'sai'});
  assert.equal(practice.next(s, {round:1}).round, 1);
  for (let revision=1; revision<=3; revision++) await tutor.answer({...body, revision, requestId:'right'+revision, answer:'đúng'});
  assert.equal(practice.view(s).tutorCompleted, true);
  assert.equal(practice.next(s, {round:1}).round, 2);
  assert.equal(s.score, 4);
});
test('last question and missing tutorial storage cannot bypass the gate; perfect work can advance', async () => {
  const {practice,start} = fixture('Physics','Football'); const st = await start(), s = practice.get(st.token);
  s.index=4; await practice.submit(s,{round:5,text:'sai'});
  assert.equal(practice.next(s,{round:5}).state,'review');
  practice.registerTutor = () => { throw new Error('disk'); };
  assert.equal(practice.next(s,{round:5}).state,'review');
  const f=fixture('Chemistry','Cave',10), perfect=await f.start(), p=f.practice.get(perfect.token);
  await f.practice.submit(p,{round:1,text:'đúng'}); assert.equal(f.practice.next(p,{round:1}).round,2);
});
