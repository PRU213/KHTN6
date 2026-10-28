const fs=require('node:fs'),path=require('node:path');
const base=JSON.parse(fs.readFileSync('Assets/Resources/AppsScriptSettings.json','utf8')).url;
(async()=>{
const results=[];
for(const username of ['student01','STUDENT01']){
  const response=await fetch(base+'?'+new URLSearchParams({action:'history',username}),{signal:AbortSignal.timeout(45000)});
  const data=await response.json();results.push({username,status:response.status,success:data.success,total:data.total,fullName:data.fullName,error:data.message});
}
const rankResponse=await fetch(base+'?action=getProgressRank',{signal:AbortSignal.timeout(45000)});
const rank=await rankResponse.json();
const report={history:results,rankRetry:{status:rankResponse.status,success:rank.success,message:rank.message}};
fs.writeFileSync(path.join(__dirname,'case-results.json'),JSON.stringify(report,null,2));console.log(JSON.stringify(report));
})().catch(e=>{console.error(e.message);process.exitCode=1;});
