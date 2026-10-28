const fs = require('node:fs');
const path = require('node:path');
const root = process.cwd();
const current = JSON.parse(fs.readFileSync('Assets/Resources/AppsScriptSettings.json', 'utf8')).url;
const scene = fs.readFileSync('Assets/Scenes/HomePage.unity', 'utf8');
const legacy = scene.match(/baseUrl: (https:\/\/script\.google\.com\/macros\/s\/[^\s]+\/exec)/)[1];
const report = { startedAt: new Date().toISOString(), readOnly: true, theory: [], practice: [], details: [], issues: [] };
const liveItems = [];
async function get(base, query) {
  const started = Date.now();
  try {
    const response = await fetch(base + '?' + new URLSearchParams(query), {signal: AbortSignal.timeout(30000)});
    const body = await response.text();
    let data;
    try { data = JSON.parse(body); } catch { return {status: response.status, ms: Date.now()-started, error:'Non-JSON response', contentType:response.headers.get('content-type'), redirectedToLogin:/accounts\.google\.com/.test(response.url+body)}; }
    return {status:response.status, ms:Date.now()-started, data};
  } catch(e) {return {error:e.message, cause:e.cause?.message, ms:Date.now()-started};}
}
async function pool(items, worker) {
  let next=0;
  await Promise.all(Array.from({length:3}, async()=>{while(next<items.length) await worker(items[next++]);}));
}
(async()=>{
  const chapters = ['PHY','CHM'].flatMap(subject=>Array.from({length:5},(_,i)=>`CH_${subject}_${String(i+1).padStart(2,'0')}`));
  await pool(chapters, async chapterId=>{
    for (const [name,base,query] of [['title',current,{action:'',chapterId}],['theory',legacy,{chapterId}]]) {
      const r=await get(base,query), d=r.data;
      report.theory.push({chapterId,endpoint:name,status:r.status,ms:r.ms,success:d?.success,title:d?.chapterTitle,hasKeyPoints:!!d?.keyPoints,hasFormulaNote:!!d?.formulaNote,error:r.error??d?.message,redirectedToLogin:r.redirectedToLogin});
    }
    const r=await get(current,{action:'practiceList',chapterId}), d=r.data;
    report.practice.push({chapterId,status:r.status,ms:r.ms,success:d?.success,count:d?.lessons?.length,error:r.error??d?.message});
    if(d?.success) for(const item of d.lessons??[]) liveItems.push({chapterId,lesson:item});
  });
  console.log(JSON.stringify({phase:'chapters',theory:report.theory,practice:report.practice}));
  await pool(liveItems,async item=>{
    const r=await get(current,{action:'practiceDetail',lessonId:item.lesson.id}), d=r.data, l=d?.lesson;
    if(d?.success&&l) liveItems.find(x=>x.lesson.id===l.id).detail=d;
    const result={id:item.lesson.id,chapterId:item.chapterId,status:r.status,ms:r.ms,success:d?.success,type:l?.simulationType,stepCount:d?.steps?.length,hasObjective:!!l?.objective,hasConclusion:!!l?.referenceAnswer,error:r.error??d?.message};
    if(l) {
      try {JSON.parse(l.configJson);} catch {result.configError='Invalid JSON';}
      const base='Assets/Resources/Practice/Lessons/'+l.id;
      result.hasBundledImage=fs.existsSync(base+'_daidien.png');
      result.hasBundledGuide=fs.existsSync(base+'_huongdan.png');
    }
    report.details.push(result);
  });
  for(const t of report.theory) if(!t.success||!t.title) report.issues.push({kind:'theory',...t});
  for(const t of report.practice) if(!t.success||!t.count) report.issues.push({kind:'list',...t});
  for(const t of report.details) if(!t.success||t.stepCount!==3||t.configError) report.issues.push({kind:'detail',...t});
  report.finishedAt=new Date().toISOString();
  fs.writeFileSync(path.join(__dirname,'api-results.json'),JSON.stringify(report,null,2));
  fs.writeFileSync(path.join(__dirname,'live-fixtures.json'),JSON.stringify({items:liveItems.filter(x=>x.detail).map(x=>x.detail)},null,2));
  console.log(JSON.stringify({phase:'complete',theoryRequests:report.theory.length,chapterLists:report.practice.length,lessonDetails:report.details.length,issues:report.issues,details:report.details}));
})().catch(e=>{console.error(e);process.exitCode=1;});
