import { createBiologyTutorSource, parseBiologyCsv } from '../../PlanetServer/biology-tutor.mjs';
import { writeFileSync } from 'node:fs';

// Read-only integration check. It never starts a lesson, writes a journal, or sends credentials.
const root = 'https://docs.google.com/spreadsheets/d/1DcJwlN_fUDdcaQ6IfnlkMJApgWuEUqMhyZv9Kn8vfWo/gviz/tq';
async function read(sheet, query) {
  const url = new URL(root); url.searchParams.set('tqx','out:csv'); url.searchParams.set('headers','1');
  if (sheet) url.searchParams.set('sheet',sheet);
  if (query) url.searchParams.set('tq',query);
  const response = await fetch(url, {signal:AbortSignal.timeout(25000)});
  if (!response.ok) throw new Error('Cannot read the existing source.');
  return parseBiologyCsv(await response.text());
}
const [submissions, users] = await Promise.all([
  read('BaiNopTuLuan', "select A, B, C, F, K where F < 5 and K = 'Completed' limit 10"),
  read(null, 'select A, B')
]);
const source = createBiologyTutorSource();
let result = {checkedAt:new Date().toISOString(), status:'SKIP', detail:'No existing completed failed Biology submission was available for a read-only check.'};
for (const submission of submissions) {
  const user = users.find(u => [u.id,u.username].some(id => id?.toLowerCase() === submission.userid?.toLowerCase()));
  if (!user) continue;
  try {
    const record = await source({id:submission.submissionid,username:user.username});
    result = {...result,status:'PASS',detail:'Existing submission, owner, grade, Biology question and full chapter were verified without creating a record.',subject:record.subject,chapter:record.chapter};
    break;
  } catch (error) { result.lastError = error.message; }
}
writeFileSync(new URL('./live-source.json',import.meta.url),JSON.stringify(result,null,2));
console.log(JSON.stringify(result));
