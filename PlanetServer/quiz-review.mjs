import { DEFAULT_APPS_SCRIPT_URL, resolveSubject } from './essay-source.mjs';

// Read the same bank as Unity. Never accept an answer/reference supplied by the client.
export function createQuizReviewSource({ url = process.env.APPS_SCRIPT_URL || DEFAULT_APPS_SCRIPT_URL, fetchImpl = fetch } = {}) {
  return async subject => {
    const endpoint = new URL(url); endpoint.searchParams.set('action', 'questions'); endpoint.searchParams.set('subject', subject);
    endpoint.searchParams.set('difficulty', 'Dễ,Trung bình,Khó');
    const response = await fetchImpl(endpoint, { signal: AbortSignal.timeout(35000) });
    if (!response.ok) throw new Error('Chưa tải được ngân hàng để xác minh câu sai. Hãy thử lại.');
    const data = await response.json();
    if (!data.success || !Array.isArray(data.questions)) throw new Error('Ngân hàng trắc nghiệm chưa hợp lệ.');
    return data.questions;
  };
}
export async function registerQuizReviews(tutor, body, source) {
  if (!Array.isArray(body.quizBatches) || body.quizBatches.length < 1 || body.quizBatches.length > 30) throw new Error('Danh sách lượt trắc nghiệm chưa hợp lệ.');
  const bank = new Map(), records = [], ids = new Set();
  for (const batch of body.quizBatches) {
    const subject = resolveSubject(batch.subject);
    if (!subject || !['Football', 'Sheep', 'Cave'].includes(batch.game) || !/^[a-f0-9]{32}$/i.test(batch.id || '') || !Array.isArray(batch.questions) || !batch.questions.length || batch.questions.length > 100) throw new Error('Lượt trắc nghiệm chưa hợp lệ.');
    if (!bank.has(subject)) bank.set(subject, await source(batch.subject));
    for (let i = 0; i < batch.questions.length; i++) {
      const entry = batch.questions[i];
      const q = bank.get(subject).find(q => q.id === entry.id);
      if (!q || !Number.isInteger(entry.chosen) || entry.chosen < -1 || entry.chosen > 3) throw new Error('Không tìm thấy câu trắc nghiệm trong ngân hàng.');
      const letter = String(q.correct || '').trim().toUpperCase().charAt(0);
      const correct = letter ? 'ABCD'.indexOf(letter) : -1;
      const options = [q.optionA, q.optionB, q.optionC, q.optionD];
      if (correct < 0 || options.some(o => typeof o !== 'string' || !o.trim()) || typeof q.question !== 'string' || resolveSubject(q.monId) !== subject) throw new Error('Câu trắc nghiệm thiếu dữ liệu chuẩn.');
      if (entry.chosen === correct) continue;
      const sourceId = `quiz:${batch.id}:${i + 1}`;
      if (ids.has(sourceId)) throw new Error('Lượt trắc nghiệm bị trùng.'); ids.add(sourceId);
      records.push({ source: 'quiz', sourceId, username: body.username, subject, chapter: q.topic || '', question: q.question,
        reference: options[correct] + (q.explanation ? '\n' + q.explanation : ''), studentAnswer: entry.chosen < 0 ? 'Chưa trả lời' : options[entry.chosen], score: 0, finalScore: 0 });
    }
  }
  // Validate the complete batch before changing any journal record.
  for (const record of records) tutor.register(record);
  return tutor.list(body.username);
}
