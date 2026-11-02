import test from 'node:test';
import assert from 'node:assert/strict';
import { EssayTutor } from './tutor.mjs';
import { createQuizReviewSource, registerQuizReviews } from './quiz-review.mjs';
import { FootballPractice } from './football.mjs';
import { createServer, Arena } from './server.mjs';
import { TutorAuth } from './tutor-auth.mjs';

const ai = { plan: async () => ({ steps: [1,2,3].map(() => ({question:'Hỏi',expected:'Đúng',explanation:'Giải thích',checkQuestion:'Hỏi lại',checkExpected:'Đúng'})),solution:'Lời giải chuẩn' }), evaluate: async () => ({correct:true}) };
const copyAI = { prepare: async () => ({kind:'theory',content:'Đáp án chuẩn'}), check: async () => ({handwritten:true,uncertain:false,count:5,questionCopied:true,redoCorrect:true,feedback:'Đủ'}) };
const bank = [1,2].map(n => ({id:'q'+n,monId:'Biology',topic:'Chương 1',question:'Câu '+n,correct:'B',optionA:'Sai A',optionB:'Đúng B',optionC:'Sai C',optionD:'Sai D',explanation:'Giải thích chuẩn'}));
const batch = { username:'student',quizBatches:[{id:'a'.repeat(32),game:'Football',subject:'Biology',questions:[{id:'q1',chosen:0},{id:'q2',chosen:2}]}] };
async function learn(tutor,id) {
  const body={id,username:'student'}; await tutor.start(body);
  for(let i=0;i<3;i++) await tutor.answer({...body,revision:i,requestId:'right-'+i,answer:'Đúng'});
  return body;
}
async function copy(tutor,body) {
  await tutor.prepareCopy(body); await tutor.submitCopy({...body,requestId:'copy',images:[{mimeType:'image/jpeg',base64:Buffer.from([255,216,255,1]).toString('base64')}]});
}
test('quiz mistakes use authoritative bank answers and must finish learning AND copying in question order', async () => {
  const tutor=new EssayTutor({ai,copyAI});
  await registerQuizReviews(tutor,batch,async()=>bank);
  await registerQuizReviews(tutor,batch,async()=>bank);
  assert.equal(tutor.records.size,2);
  const [first,second]=[...tutor.records.values()];
  assert.equal(tutor.view(second).blockedBy,first.id);
  assert.equal(first.reference,'Đúng B\nGiải thích chuẩn'); assert.ok(first.penalty);
  await assert.rejects(tutor.start({id:second.id,username:'student'}),/câu trước/);
  const body=await learn(tutor,first.id);
  await assert.rejects(tutor.start({id:second.id,username:'student'}),/câu trước/);
  await copy(tutor,body);
  assert.equal(tutor.view(second).blockedBy,'');
  assert.doesNotThrow(()=>tutor.isComplete(first.id,'student'));
  await tutor.start({id:second.id,username:'student'});
  assert.throws(()=>tutor.assertCanPlay('student'),/chép phạt/);
});
test('quiz validation rejects made-up IDs and a bad batch without registering partial data; correct answers need no penalty', async () => {
  const tutor=new EssayTutor({ai,copyAI}); const bad=structuredClone(batch); bad.quizBatches[0].questions[1].id='fake';
  await assert.rejects(registerQuizReviews(tutor,bad,async()=>bank)); assert.equal(tutor.records.size,0);
  const correct=structuredClone(batch); correct.quizBatches[0].questions=[{id:'q1',chosen:1,reference:'forged'}];
  await registerQuizReviews(tutor,correct,async()=>bank); assert.equal(tutor.records.size,0);
});
test('quiz source reads existing questions endpoint, not client-supplied solutions', async () => {
  let url;
  const source=createQuizReviewSource({url:'https://example.test/exec',fetchImpl:async value=>{url=value;return{ok:true,json:async()=>({success:true,questions:bank})};}});
  assert.deepEqual(await source('Biology'),bank); assert.equal(url.searchParams.get('action'),'questions'); assert.equal(url.searchParams.get('subject'),'Biology');
});
test('HTTP practice allows copying current reviewed question, but blocks next at final 9-3=6 until copying passes', async () => {
  const tutor=new EssayTutor({ai,copyAI});
  const practice=new FootballPractice({source:async()=>[1,2,3,4,5].map(n=>({id:''+n,subject:'Physics',topic:'Chương 1',difficulty:'Dễ',text:'Đề',sampleAnswer:'Đáp'})),grade:async()=>({score:9,feedback:'Thiếu ý'}),record:async()=>{}});
  const auth=new TutorAuth({authenticate:async username=>({username,teacher:false})});
  const server=createServer(new Arena(),undefined,{tutor,football:practice,tutorAuth:auth});
  await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));
  const token=(await auth.login({username:'student',password:'x'},'test')).token;
  const post=async(path,body)=>(await fetch(`http://127.0.0.1:${server.address().port}${path}`,{method:'POST',headers:{Authorization:'Bearer '+token},body:JSON.stringify(body)})).json();
  try {
    const started=await post('/football/start',{username:'student',subject:'Physics',chapter:'Chương 1',difficulties:['Dễ']});
    const session=practice.get(started.token); session.hints=['gợi ý 1','gợi ý 2'];
    const review=await post('/football/submit',{token:started.token,round:1,text:'Bài làm'});
    assert.equal(review.earned,6); assert.equal(review.rawScore,9);
    const body=await learn(tutor,review.tutorId);
    assert.equal((await post('/football/next',{token:started.token,round:1})).round,1);
    assert.equal((await post('/tutor/copy-start',body)).penalty.required,1);
    await post('/tutor/copy-submit',{...body,requestId:'photo',images:[{mimeType:'image/jpeg',base64:Buffer.from([255,216,255,2]).toString('base64')}]});
    assert.equal((await post('/football/next',{token:started.token,round:1})).round,2);
    assert.equal(session.score,6);
  } finally {await new Promise(resolve=>server.close(resolve));}
});
