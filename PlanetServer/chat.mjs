const instruction = 'Bạn là robot Bạn đồng hành khoa học trong game Khoa học tự nhiên lớp 6. Trả lời tiếng Việt, xưng tớ và gọi bạn, thân thiện. Giải thích kiến thức Vật lí, Hóa học, Sinh học bằng ví dụ gần gũi, thường dưới 200 từ. Giúp học sinh hiểu cách làm. Nếu không chắc hãy nói rõ, không bịa nguồn. Không hướng dẫn thí nghiệm nguy hiểm; đề xuất hoạt động an toàn có người lớn hỗ trợ. Chỉ trả lời bằng văn bản thường, không dùng bảng hay Markdown.';

export function createChat({ key = process.env.GEMINI_API_KEY, model = process.env.GEMINI_MODEL || 'gemini-3.5-flash-lite', fetchImpl = fetch, timeoutMs = 45000, now = Date.now } = {}) {
  const clients = new Map();
  return async (body, client = 'local', callerSignal) => {
    if (!key || key === 'YOUR_GEMINI_API_KEY') return { status: 503, error: 'Máy chủ chưa được cấu hình Gemini. Hãy thêm GEMINI_API_KEY rồi khởi động lại máy chủ.' };
    if (typeof body.message !== 'string' || !body.message.trim() || body.message.length > 1200)
      return { status: 400, error: 'Bạn hãy nhập câu hỏi từ 1 đến 1.200 ký tự nhé.' };
    const history = body.history || [];
    if (!Array.isArray(history) || history.length > 12 || history.some(x => !x || !['user', 'model'].includes(x.role) || typeof x.text !== 'string' || x.text.length > 6000))
      return { status: 400, error: 'Lịch sử hội thoại không hợp lệ. Hãy mở cuộc trò chuyện mới.' };
    const time = now();
    for (const [id, value] of clients) if (!value.busy && time - value.since >= 60000) clients.delete(id);
    const state = clients.get(client) || { since: time, count: 0, busy: false };
    if (state.busy) return { status: 409, error: 'Tớ vẫn đang xử lý câu hỏi trước. Bạn chờ một chút rồi gửi lại nhé.' };
    if (state.count >= 12) {
      const retryAfter = Math.max(1, Math.ceil((60000 - (time - state.since)) / 1000));
      return { status: 429, retryAfter, error: `Bạn đã gửi nhiều câu hỏi liên tiếp. Hãy chờ ${retryAfter} giây rồi thử lại nhé.` };
    }
    state.busy = true; state.count++; clients.set(client, state);
    const controller = new AbortController();
    const cancel = () => controller.abort();
    callerSignal?.addEventListener('abort', cancel, { once: true });
    if (callerSignal?.aborted) cancel();
    // One deadline covers all attempts, response bodies and retry delays.
    const timer = setTimeout(cancel, timeoutMs);

    try {
      let response;
      for (let attempt = 1; attempt <= 2; attempt++) {
        controller.signal.throwIfAborted();
        response = await fetchImpl(`https://generativelanguage.googleapis.com/v1beta/models/${encodeURIComponent(model)}:generateContent`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json', 'x-goog-api-key': key },
          signal: controller.signal,
          body: JSON.stringify({
            systemInstruction: { parts: [{ text: instruction }] },
            contents: [...history, { role: 'user', text: body.message.trim() }].map(x => ({ role: x.role, parts: [{ text: x.text }] })),
            generationConfig: {
              maxOutputTokens: 2048,
              temperature: 0.6,
              thinkingConfig: model.startsWith('gemini-2.5') ? { thinkingBudget: 0 } : { thinkingLevel: 'low' }
            }
          })
        });

        if (response.ok) break;

        await response.text();
        // Keep credentials and provider payloads out of logs.
        console.warn(`Gemini attempt ${attempt}: HTTP ${response.status}`);

        if (response.status === 503 && attempt < 2) {
          await new Promise((resolve, reject) => {
            const abort = () => { clearTimeout(delay); reject(new Error('Aborted')); };
            const delay = setTimeout(() => { controller.signal.removeEventListener('abort', abort); resolve(); }, 1000);
            controller.signal.addEventListener('abort', abort, { once: true });
            if (controller.signal.aborted) abort();
          });
          continue;
        }

        if (response.status === 503) return { status: 503, error: 'Gemini đang quá tải. Bạn thử lại sau một chút nhé.' };
        return { status: 502, error: response.status === 429 ? 'Gemini đang hết lượt hoặc quá tải. Bạn thử lại sau nhé.' : 'Chưa kết nối được Gemini. Hãy kiểm tra khóa API và tên model trên máy chủ.' };
      }

      const data = await response.json();
      const candidate = data.candidates?.[0];
      const text = candidate?.content?.parts?.filter(x => !x.thought && typeof x.text === 'string').map(x => x.text).join('\n').trim();
      if (!text || ['SAFETY', 'RECITATION', 'PROHIBITED_CONTENT'].includes(candidate?.finishReason)) {
        return { status: 422, error: 'Tớ chưa thể trả lời câu này. Bạn thử diễn đạt lại hoặc hỏi một câu khoa học khác nhé.' };
      }
      return { status: 200, reply: text.slice(0, 6000) };
    } catch (err) {
      if (callerSignal?.aborted) return { status: 499, error: 'Đã hủy câu hỏi.' };
      const networkCode = err.cause?.code || err.code;
      const safeCode = typeof networkCode === 'string' && /^[A-Z0-9_]{1,50}$/.test(networkCode) ? networkCode : 'unknown';
      console.warn('Gemini request failed:', controller.signal.aborted ? 'timeout' : `connection error (${safeCode})`);
      return { status: 504, error: 'Kết nối Gemini bị gián đoạn hoặc quá thời gian. Bạn thử gửi lại nhé.' };
    } finally {
      clearTimeout(timer);
      callerSignal?.removeEventListener('abort', cancel);
      state.busy = false;
    }
  };
}
