const fs=require('node:fs'),path=require('node:path');
const base=fs.readFileSync('Assets/Scenes/HomePage.unity','utf8').match(/baseUrl: (https:\/\/script\.google\.com\/macros\/s\/[^\s]+\/exec)/)[1];
const server=JSON.parse(fs.readFileSync('Assets/Resources/PlanetDuelSettings.json','utf8')).serverUrl;
const results={questions:[],essayStarts:[],issues:[]};
async function request(url,body){try{const r=await fetch(url,{...(body?{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(body)}:{}),signal:AbortSignal.timeout(45000)});return {status:r.status,data:await r.json()};}catch(e){return {error:e.message,cause:e.cause?.message};}}
(async()=>{
  for(const subject of ['Vật lý','Hóa học'])for(const difficulty of ['Dễ','Trung bình','Khó']){
    const r=await request(base+'?'+new URLSearchParams({action:'questions',subject,difficulty}));
    const qs=r.data?.questions??[];
    const row={subject,difficulty,status:r.status,success:r.data?.success,count:qs.length,byChapter:Array.from({length:5},(_,i)=>({chapter:i+1,count:qs.filter(q=>q.topic?.trim().toLowerCase().startsWith('chương '+(i+1))).length})),invalid:qs.filter(q=>!q.question||!q.optionA||!q.optionB||!q.optionC||!q.optionD||!['A','B','C','D'].includes(q.correct?.trim().toUpperCase())).length,error:r.error??r.data?.message};
    results.questions.push(row);
  }
  console.log(JSON.stringify({phase:'questions',questions:results.questions}));
  let first=true,sourceFailed=false;
  for(const subject of ['Vật lý','Hóa học'])for(let chapter=1;chapter<=5;chapter++)for(const difficulty of ['Dễ','Trung bình','Khó']){
    if(sourceFailed)continue;
    const r=await request(server+'/football/start',{game:'Football',subject,chapter:'Chương '+chapter,difficulties:[difficulty],username:'QA-flow-test',name:'QA-flow-test'});
    const d=r.data;
    const row={subject,chapter,difficulty,status:r.status,state:d?.state,total:d?.total,error:r.error??d?.error};
    if(d?.token){const leave=await request(server+'/football/leave',{token:d.token});row.cleanedUp=leave.data?.success===true;}
    results.essayStarts.push(row);
    if(first&&!d?.token&&!(d?.error??'').includes('Chưa đủ 5 câu'))sourceFailed=true;
    first=false;
  }
  for(const q of results.questions)if(!q.success||q.invalid||q.byChapter.some(c=>!c.count))results.issues.push({kind:'questions',...q});
  for(const q of results.essayStarts)if(q.error||q.total!==5||!q.cleanedUp)results.issues.push({kind:'essayStart',...q});
  fs.writeFileSync(path.join(__dirname,'game-results.json'),JSON.stringify(results,null,2));
  console.log(JSON.stringify({phase:'complete',...results}));
})();
