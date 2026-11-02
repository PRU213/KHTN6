import test from 'node:test';
import assert from 'node:assert/strict';
import vm from 'node:vm';
import { createHash } from 'node:crypto';
import { readFileSync } from 'node:fs';
import { createTutorSheetStore } from './tutor-sheet-store.mjs';
import { EssayTutor } from './tutor.mjs';
import { createServer, Arena } from './server.mjs';
import { TutorAuth } from './tutor-auth.mjs';

function sheetFixture() {
  let sheet, failWrite = false;
  const properties = { PLANET_SERVER_SECRET:'test-secret', TUTOR_SPREADSHEET_ID:'test-sheet' };
  function makeSheet() {
    const data = []; let columns = 6, maxRows = 100;
    return { data, getMaxColumns:()=>columns, getMaxRows:()=>maxRows, setFrozenRows(){},
      insertColumnsAfter:(_at,count)=>{columns+=count;}, insertRowsAfter:(_at,count)=>{maxRows+=count;},
      getDataRange:()=>({getValues:()=>data.map(r=>[...r])}),
      getRange:(row,col,height,width)=>({
        getValues:()=>Array.from({length:height},(_,y)=>Array.from({length:width},(_,x)=>data[row-1+y]?.[col-1+x]??'')),
        setValues:values=>{if(failWrite)throw new Error('disk unavailable'); values.forEach((line,y)=>line.forEach((value,x)=>{(data[row-1+y]??=[])[col-1+x]=value;}));}
      }) };
  }
  const context=vm.createContext({PropertiesService:{getScriptProperties:()=>({getProperty:key=>properties[key]})},
    SpreadsheetApp:{openById:()=>({getSheetByName:()=>sheet,insertSheet:()=>sheet=makeSheet()}),flush(){}},
    LockService:{getScriptLock:()=>({waitLock(){},releaseLock(){}})}, jsonOut:value=>value,
    Utilities:{DigestAlgorithm:{SHA_256:'sha256'},Charset:{UTF_8:'utf8'},computeDigest:(_algo,value)=>Array.from(createHash('sha256').update(value).digest()),
      base64Encode:value=>Buffer.from(value,'utf8').toString('base64'),base64Decode:value=>Buffer.from(value,'base64'),newBlob:bytes=>({getDataAsString:()=>Buffer.from(bytes).toString('utf8')})}});
  vm.runInContext(readFileSync(new URL('../AppsScript/TutorJournal.gs',import.meta.url),'utf8'),context);
  context.setupTutorJournal();
  const fetchImpl=async(_url,options)=>{
    const params=Object.fromEntries(options.body); const result=context.tutorJournalApi_(params);
    return {ok:true,json:async()=>JSON.parse(JSON.stringify(result))};
  };
  return {context,fetchImpl,sheet:()=>sheet, fail:value=>{failWrite=value;},store:()=>createTutorSheetStore({url:'https://example.test/exec',secret:'test-secret',fetchImpl})};
}
const input={sourceId:'practice:session:0',username:'student',subject:'Physics',question:'Câu hỏi',reference:'Đáp án',score:4};
const ai={plan:async()=>({steps:[1,2,3].map(()=>({question:'Hỏi',expected:'Đúng',explanation:'Giải thích',checkQuestion:'Hỏi lại',checkExpected:'Đúng'})),solution:'Lời giải đầy đủ'}),evaluate:async()=>({correct:true})};
const copyAI={prepare:async()=>({kind:'theory',content:'Mẫu chép'}),check:async()=>({handwritten:true,uncertain:false,count:5,questionCopied:true,redoCorrect:true,feedback:'Đủ'})};

test('journal survives a fresh server with no local files, including learning and completed copying', async()=>{
  const f=sheetFixture(),first=new EssayTutor({ai,copyAI,store:f.store()}); await first.ensureLoaded();
  const id=first.register(input); await first.flush(); await first.start({id,username:'student'}); await first.flush();
  await first.answer({id,username:'student',revision:0,requestId:'a',answer:'Đúng'}); await first.flush();
  const second=new EssayTutor({ai,copyAI,store:f.store()}); await second.ensureLoaded();
  assert.equal(second.get(id,'student').step,1); assert.equal(second.get(id,'student').events.length,1);
  for(let revision=1;revision<3;revision++)await second.answer({id,username:'student',revision,requestId:'a'+revision,answer:'Đúng'});
  await second.prepareCopy({id,username:'student'});
  await second.submitCopy({id,username:'student',requestId:'image',images:[{mimeType:'image/jpeg',base64:Buffer.from([255,216,255,1]).toString('base64')}]});
  await second.flush();
  const third=new EssayTutor({store:f.store()}); await third.ensureLoaded();
  assert.equal(third.isComplete(id,'student'),true); assert.equal(third.get(id,'student').penalty.count,5);
  assert.doesNotThrow(()=>third.assertCanPlay('student'));
});
test('large Unicode journals split into safe cells, retry is idempotent, stale writer cannot overwrite progress',async()=>{
  const f=sheetFixture(),tutor=new EssayTutor(); const id=tutor.register(input); const record=structuredClone(tutor.get(id,'student'));
  record.studentAnswer='=IMPORTXML("fake")\nĐáp án tiếng Việt '.repeat(4000);
  const store=f.store(); await store.load(); await store.save([record]);
  const row=f.sheet().data[1]; assert.ok(row[3]>1); assert.ok(row.slice(6).every(c=>c.length<=40000&&!/^[=+@-]/.test(c)));
  assert.equal((await f.store().load())[0].studentAnswer,record.studentAnswer);
  const stale=f.store(); await stale.load();
  record.updatedAt++; await store.save([record]); await store.save([record]);
  const outdated=structuredClone(record); outdated.step=2;
  await assert.rejects(stale.save([outdated]),/máy chủ khác/);
  assert.equal(f.sheet().data.length,2);
});
test('missing secret, wrong secret, damaged rows and a missing setup never silently become an empty journal',async()=>{
  assert.throws(()=>createTutorSheetStore({secret:''}));
  const f=sheetFixture();
  const wrong=createTutorSheetStore({secret:'wrong',fetchImpl:f.fetchImpl});await assert.rejects(wrong.load());
  const tutor=new EssayTutor({store:f.store()});await tutor.ensureLoaded();tutor.register(input);await tutor.flush();
  f.sheet().data[1][6]='bad checksum';await assert.rejects(f.store().load());
});
test('HTTP does not acknowledge learning changes until Sheet confirms; a failed write can safely retry',async()=>{
  const f=sheetFixture(),tutor=new EssayTutor({ai,store:f.store()});await tutor.ensureLoaded();const id=tutor.register(input);await tutor.flush();
  const auth=new TutorAuth({authenticate:async username=>({username,teacher:false})});
  const server=createServer(new Arena(),undefined,{tutor,tutorAuth:auth});await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));
  const token=(await auth.login({username:'student',password:'test'},'test')).token;
  const post=async(path,body)=>{const res=await fetch(`http://127.0.0.1:${server.address().port}/tutor/${path}`,{method:'POST',headers:{Authorization:'Bearer '+token},body:JSON.stringify(body)});return{status:res.status,body:await res.json()};};
  try {
    f.fail(true);const failed=await post('start',{id,username:'student'});assert.notEqual(failed.status,200);assert.ok(failed.body.error);
    f.fail(false);assert.equal((await post('start',{id,username:'student'})).body.status,'learning');
    const restored=new EssayTutor({store:f.store()});await restored.ensureLoaded();assert.equal(restored.get(id,'student').status,'learning');
  }finally{await new Promise(resolve=>server.close(resolve));}
});
test('setup refuses a conflicting existing tab and invalidates optimistic revisions',()=>{
  const f=sheetFixture();f.sheet().data[0][0]='OtherData';
  assert.throws(()=>f.context.setupTutorJournal(),/cấu trúc/);
});
