import { readFileSync, writeFileSync } from 'node:fs';
const questions = JSON.parse(readFileSync(new URL('./questions.json', import.meta.url), 'utf8'));
for (const q of questions) {
  if (!['Physics', 'Chemistry', 'Biology'].includes(q.subject) || !['Dễ', 'Trung bình', 'Khó'].includes(q.difficulty) ||
      !q.text || q.options?.length !== 4 || !Number.isInteger(q.correct) || q.correct < 0 || q.correct > 3) {
    throw new Error('Câu hỏi không hợp lệ: ' + JSON.stringify(q));
  }
}
writeFileSync(new URL('../Assets/Resources/PlanetDuelQuestions.json', import.meta.url), JSON.stringify({ questions }, null, 2) + '\n');
console.log(`Đã đồng bộ ${questions.length} câu hỏi ngoại tuyến.`);
