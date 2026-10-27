const fs=require('node:fs'),path=require('node:path');
const account='student01';
const base=JSON.parse(fs.readFileSync('Assets/Resources/AppsScriptSettings.json','utf8')).url;
const loginSource=fs.readFileSync('Assets/Scripts/GoogleSheetAPI.cs','utf8');
const csvUrl=loginSource.match(/csvURL = "([^"]+)"/)[1];
const report={account,loginSource:{},history:{},rank:{},issues:[]};
async function get(url){try{const r=await fetch(url,{signal:AbortSignal.timeout(45000)});return {status:r.status,text:await r.text(),contentType:r.headers.get('content-type')};}catch(e){return {error:e.message,cause:e.cause?.message};}}
function parseCsv(text){const rows=[];let row=[],cell='',quoted=false;for(let i=0;i<text.length;i++){const c=text[i];if(c==='"'){if(quoted&&text[i+1]==='"'){cell+='"';i++;}else quoted=!quoted;}else if(c===','&&!quoted){row.push(cell);cell='';}else if((c==='\n'||c==='\r')&&!quoted){if(c==='\r'&&text[i+1]==='\n')i++;row.push(cell);if(row.some(Boolean))rows.push(row);row=[];cell='';}else cell+=c;}if(cell||row.length){row.push(cell);rows.push(row);}return rows;}
(async()=>{
  const [csv,history,rank]=await Promise.all([get(csvUrl),get(base+'?'+new URLSearchParams({action:'history',username:account})),get(base+'?action=getProgressRank')]);
  if(csv.text&&!/^\s*</.test(csv.text)){
    const rows=parseCsv(csv.text),match=rows.slice(1).filter(r=>r[1]?.trim().toLowerCase()===account.toLowerCase());
    const actualRows=csv.text.split(/[\r\n]+/).slice(1).map(r=>r.split(',')).filter(r=>r[1]?.trim().toLowerCase()===account.toLowerCase());
    report.loginSource={status:csv.status,accountFound:match.length===1,matchingAccounts:match.length,fullName:match[0]?.[3]?.trim(),role:match[0]?.[4]?.trim(),loginParserMatchesProfile:actualRows[0]?.[3]?.trim()===match[0]?.[3]?.trim()&&actualRows[0]?.[4]?.trim()===match[0]?.[4]?.trim(),passwordPresent:!!match[0]?.[2],note:'CSV downloaded only in memory. No passwords saved or printed; no login credentials submitted.'};
  }else report.loginSource={status:csv.status,error:csv.error??'Login CSV returned HTML or empty data'};
  let h;
  try{h=JSON.parse(history.text);}catch{}
  if(h){
    const items=h.histories??[];const counts=Object.fromEntries([...new Set(items.map(x=>x.mode))].map(mode=>[mode,items.filter(x=>x.mode===mode).length]));
    report.history={status:history.status,success:h.success,username:h.username,fullName:h.fullName,total:h.total,rowCount:items.length,win:h.win,lose:h.lose,winRate:h.winRate,modeCounts:counts,firstTime:items[0]?.time,lastTime:items.at(-1)?.time,missingChapter:items.filter(x=>x.mode==='Luyện tập'&&!x.chapter).length,error:h.message,validSummary:h.total===items.length&&h.win===items.filter(x=>x.result==='THẮNG').length&&h.lose===items.filter(x=>x.result==='THUA').length};
  }else report.history={status:history.status,error:history.error??'Non-JSON history response'};
  let r;
  try{r=JSON.parse(rank.text);}catch{}
  if(r){
    const players=r.players??[],mine=players.find(p=>p.userID===account);
    report.rank={status:rank.status,success:r.success,rankVersion:r.rankVersion,weekStartLabel:r.weekStartLabel,weekEndLabel:r.weekEndLabel,weekStartsAt:r.weekStartsAt,weekEndsAt:r.weekEndsAt,playerCount:players.length,accountPresent:!!mine,accountRank:mine?players.indexOf(mine)+1:undefined,accountScore:mine?.score,accountName:mine?.fullName,missingNames:players.filter(p=>!p.fullName).length,sorted:players.every((p,i)=>!i||players[i-1].score>=p.score),ignoredUndatedRows:r.ignoredUndatedRows,error:r.message};
    if(h&&r.weekStartsAt&&r.weekEndsAt){const start=Date.parse(r.weekStartsAt),end=Date.parse(r.weekEndsAt);const thisWeek=(h.histories??[]).filter(x=>{let stamp=Date.parse(x.time);const m=x.time?.match(/^(\d{1,2})\/(\d{1,2})\/(\d{4}) (\d{1,2}):(\d{2})/);if(m)stamp=Date.UTC(+m[3],+m[2]-1,+m[1],+m[4],+m[5])-7*3600000;return stamp>=start&&stamp<end&&['Luyện tập','Đối kháng'].includes(x.mode);});report.rank.accountHistoryThisWeek=thisWeek.length;report.rank.accountRawHistoryScore=thisWeek.reduce((s,x)=>s+(Number(x.score)||0),0);}
  }else report.rank={status:rank.status,error:rank.error??'Non-JSON rank response'};
  if(!report.loginSource.accountFound||!report.loginSource.loginParserMatchesProfile)report.issues.push('Login CSV unavailable/account absent/profile parse mismatch');
  if(!report.history.success||!report.history.validSummary)report.issues.push('History request/summary failed');
  if(!report.rank.success||report.rank.rankVersion!=='weekly-v2')report.issues.push('Rank response unavailable or incompatible with client weekly-v2');
  if(report.rank.accountPresent&&report.rank.accountScore!==report.rank.accountRawHistoryScore)report.issues.push('Weekly rank differs from raw sum of this account history; requires checking duplicates/future times');
  fs.writeFileSync(path.join(__dirname,'api-results.json'),JSON.stringify(report,null,2));
  console.log(JSON.stringify(report));
})();
