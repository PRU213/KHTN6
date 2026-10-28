import fs from 'node:fs';
import { createEssaySource } from '../../PlanetServer/essay-source.mjs';
import { FootballPractice } from '../../PlanetServer/football.mjs';
const source = createEssaySource();
const bank = await source();
const practice = new FootballPractice({ source });
const results = [];
for (const game of ['Football','Sheep','Cave']) for (const subject of ['Vật lý','Hóa học']) for (let chapter=1; chapter<=5; chapter++) for (const difficulty of ['Dễ','Trung bình','Khó']) {
  try {
    const state = await practice.start({game,subject,chapter:'Chương '+chapter,difficulties:[difficulty],username:'QA-flow-test'});
    results.push({game,subject,chapter,difficulty,state:state.state,total:state.total});
    practice.sessions.delete(state.token);
  } catch (e) { results.push({game,subject,chapter,difficulty,error:e.message}); }
}
const report={bankCount:bank.length,bySubject:Object.fromEntries(['Physics','Chemistry','Biology'].map(s=>[s,bank.filter(q=>q.subject===s).length])),attempts:results.length,passed:results.filter(r=>r.total===5&&r.state==='answering').length,issues:results.filter(r=>r.error),sessionsRemaining:practice.sessions.size,results};
fs.writeFileSync(new URL('./current-essay-results.json',import.meta.url),JSON.stringify(report,null,2));
console.log(JSON.stringify({bankCount:report.bankCount,attempts:report.attempts,passed:report.passed,issues:report.issues,sessionsRemaining:report.sessionsRemaining}));
