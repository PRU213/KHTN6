const fs=require('node:fs'),path=require('node:path');
const current=JSON.parse(fs.readFileSync('Assets/Resources/AppsScriptSettings.json','utf8')).url;
const biology=fs.readFileSync('Assets/Scripts/LeaderboardAPI.cs','utf8').match(/webAppUrl = "([^"]+)"/)[1];
const normal=s=>String(s??'').normalize('NFD').replace(/[\u0300-\u036f]/g,'').replace(/[đĐ]/g,'d').toLowerCase().trim();
const isBio=s=>['sinh hoc','biology','bio'].includes(normal(s));
async function get(base,query){try{const r=await fetch(base+'?'+new URLSearchParams(query),{signal:AbortSignal.timeout(45000)});const d=await r.json();return {status:r.status,data:d};}catch(e){return {error:e.message};}}
(async()=>{
const [h,r,b,c]=await Promise.all([get(current,{action:'history',username:'student01'}),get(current,{action:'getProgressRank'}),get(biology,{action:'getLeaderboard'}),get(current,{action:'getLeaderboard'})]);
function leaderboard(result){const rows=Array.isArray(result.data)?result.data:[];const mine=rows.filter(x=>x.username==='student01');return {status:result.status,arrayResponse:Array.isArray(result.data),total:rows.length,biology:rows.filter(x=>isBio(x.subject)).length,accountRows:mine.length,accountBiologyRows:mine.filter(x=>isBio(x.subject)).length,accountBiology:mine.filter(x=>isBio(x.subject)).map(x=>({subject:x.subject,chapter:x.chapter,difficulty:x.difficulty,time:x.time})),error:result.error??result.data?.message};}
const hs=h.data?.histories??[];const duels=hs.filter(x=>normal(x.mode)==='doi khang');
const report={account:'student01',history:{status:h.status,success:h.data?.success,total:h.data?.total,biologyRows:hs.filter(x=>isBio(x.subject)).length,practiceBiologyRows:hs.filter(x=>isBio(x.subject)&&normal(x.mode)==='luyen tap').length,duelRows:duels.length,duelRowsWithSubject:duels.filter(x=>x.subject).length,subjects:[...new Set(hs.map(x=>x.subject||'(empty)'))],error:h.error??h.data?.message},weeklyRank:{status:r.status,success:r.data?.success,rankVersion:r.data?.rankVersion,players:r.data?.players?.length,weekStart:r.data?.weekStartLabel,weekEnd:r.data?.weekEndLabel,error:r.error??r.data?.message},biologyLeaderboard:leaderboard(b),currentLeaderboard:leaderboard(c)};
fs.writeFileSync(path.join(__dirname,'results.json'),JSON.stringify(report,null,2));console.log(JSON.stringify(report));
})().catch(e=>{console.error(e.message);process.exitCode=1;});
