import fs from 'node:fs';
import { EssayTutor } from '../../PlanetServer/tutor.mjs';

// Use the project's existing AI configuration. Synthetic records remain in RAM only.
const env = new URL('../../PlanetServer/.env', import.meta.url);
if (fs.existsSync(env)) process.loadEnvFile(env);
const report = { checkedAt:new Date().toISOString(), sheetWrites:false, status:'SKIP' };
try {
  if (!process.env.GEMINI_API_KEY || process.env.GEMINI_API_KEY === 'YOUR_GEMINI_API_KEY') throw new Error('Local AI configuration is unavailable.');
  const tutor = new EssayTutor();
  const id = tutor.register({ source:'biology',sourceId:'biology:synthetic-ai-check',username:'synthetic-check',subject:'Biology',chapter:'Chương 1: Tế bào',
    chapterContent:'Tế bào là đơn vị cơ sở của sự sống. Các phần chính gồm màng tế bào, chất tế bào và nhân hoặc vùng nhân.',
    question:'Tế bào là gì? Kể tên ba thành phần chính.',reference:'Tế bào là đơn vị cơ sở của sự sống. Ba thành phần chính là màng tế bào, chất tế bào và nhân hoặc vùng nhân.',studentAnswer:'Em chưa hiểu.',score:4,finalScore:4 });
  const record = await tutor.start({id,username:'synthetic-check'});
  report.guidanceCreated = record.status === 'learning' && record.total >= 3 && !!record.prompt;
  report.steps = record.total;
  const image = fs.readFileSync(new URL('./biology-instructions.png',import.meta.url));
  const result = await tutor.copyAI.check({biologyChapter:true,chapterContent:'Tế bào là đơn vị cơ sở của sự sống. Màng tế bào, chất tế bào và nhân.',
    redoQuestion:'Tế bào là gì?',redoSolution:'Tế bào là đơn vị cơ sở của sự sống.'},[{mimeType:'image/png',base64:image.toString('base64')}]);
  report.typedImageRejected = result.handwritten === false && result.count === 0 && result.chapterCopied === false;
  report.status = report.guidanceCreated && report.typedImageRejected ? 'PASS' : 'FAIL';
} catch (error) { report.status='UNAVAILABLE'; report.detail=error.message; }
fs.writeFileSync(new URL('./live-ai.json',import.meta.url),JSON.stringify(report,null,2));
console.log(JSON.stringify(report));
