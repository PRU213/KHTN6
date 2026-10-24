import http from 'node:http';
import { randomUUID } from 'node:crypto';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { existsSync } from 'node:fs';
import { createChat } from './chat.mjs';
import { createEssayGrader, validateEssayImage } from './essay.mjs';
import { createEssaySource, createEssayRecorder } from './essay-source.mjs';
import { FootballPractice } from './football.mjs';

const envPath = fileURLToPath(new URL('./.env', import.meta.url));
if (existsSync(envPath)) process.loadEnvFile(envPath);

const bank = JSON.parse(readFileSync(new URL('./questions.json', import.meta.url), 'utf8'));
const essayBank = JSON.parse(readFileSync(new URL('./essay-questions.json', import.meta.url), 'utf8'));
const shuffle = items => {
  const copy = [...items];
  for (let i = copy.length - 1; i > 0; i--) {
    const j = Math.floor(Math.random() * (i + 1));
    [copy[i], copy[j]] = [copy[j], copy[i]];
  }
  return copy;
};
const point = p => p && Number.isFinite(p.x) && Number.isFinite(p.y) && Math.abs(p.x) < 5000 && Math.abs(p.y) < 5000;
export const essayDuration = difficulty => ({ 'Dễ': 60000, 'Trung bình': 120000, 'Khó': 240000 }[difficulty] || 60000);

// Single process owns queue transitions, physics, timeouts and scores.
export class Arena {
  constructor({ now = Date.now, waitMs = 120000, gradeEssay = createEssayGrader(), essayQuestions = essayBank, recordEssay = createEssayRecorder() } = {}) {
    this.now = now; this.waitMs = waitMs; this.players = new Map(); this.matches = new Map(); this.gradeEssay = gradeEssay;
    this.essayBank = essayQuestions; this.recordEssay = recordEssay;
  }
  join(body) {
    const now = this.now();
    if (body.questionType !== undefined && !['essay', 'multipleChoice'].includes(body.questionType)) throw new Error('Hình thức làm bài không hợp lệ.');
    const questionType = body.questionType || 'multipleChoice';
    const difficulties = [...new Set((body.difficulties || []).filter(d => ['Dễ', 'Trung bình', 'Khó'].includes(d)))].sort();
    if (!difficulties.length) difficulties.push('Dễ');
    const perSubject = questionType === 'essay' ? 3 : 5;
    const questions = ['Physics', 'Chemistry', 'Biology'].flatMap(subject => {
      const pool = (questionType === 'essay' ? this.essayBank : bank).filter(q => q.subject === subject && difficulties.includes(q.difficulty));
      if (pool.length < perSubject) throw new Error(`Mỗi môn cần ít nhất ${perSubject} câu hỏi đã duyệt với độ khó đã chọn.`);
      return shuffle(pool).slice(0, perSubject);
    });
    if (!Array.isArray(body.guns) || body.guns.length !== 2 || !body.guns.every(point)) throw new Error('Vị trí tàu không hợp lệ.');
    if (!Array.isArray(body.targets) || body.targets.length !== 4 || !body.targets.every(point)) throw new Error('Vị trí đáp án không hợp lệ.');
    const p = { token: randomUUID(), name: String(body.name || 'Người chơi').slice(0, 32), username: String(body.username || '').slice(0, 100),
      key: 'PlanetMixed:' + questionType + ':' + difficulties.join(','), questionType, joined: now, seen: now,
      state: 'searching', match: null, seat: 0, layout: { guns: body.guns, targets: body.targets }, questions };
    this.players.set(p.token, p);
    // Resolve expired tickets before trying to pair: no human/bot race at 120 seconds.
    this.tick();
    const other = [...this.players.values()].find(o => o !== p && o.state === 'searching' && o.key === p.key && now - o.seen < 15000);
    if (other) this.createMatch(other, p);
    return this.view(p);
  }
  createMatch(p, other = null) {
    if (p.state !== 'searching' || (other && other.state !== 'searching')) return;
    const start = this.now() + 4000;
    const essay = p.questionType === 'essay';
    const m = { id: randomUUID(), players: [p, other], names: [p.name, other?.name || 'Máy Planet'],
      questionType: p.questionType, roundMs: essay ? 60000 : 20000,
      submissions: [0, 0], reviews: [null, null],
      bot: !other, phase: 'countdown', starts: start, deadline: start + (essay ? 60000 : 20000),
      botNext: start + 3000 + Math.random() * 2000,
      index: 0, scores: [0, 0], stun: [0, 0], cooldown: [0, 0], shot: [false, false, false, false],
      bullets: [], nextBullet: 0, lastTick: this.now(), layout: p.layout, winner: -1, reason: '',
      questions: shuffle(p.questions).map(q => {
        if (essay) return { ...q, options: [], correct: -1 };
        const order = shuffle([0, 1, 2, 3]);
        return { subject: q.subject, text: q.text, options: order.map(i => q.options[i]), correct: order.indexOf(q.correct) };
      }) };
    if (essay) { m.roundMs = essayDuration(m.questions[0].difficulty); m.deadline = start + m.roundMs; }
    const xs = p.layout.targets.map(t => t.x), ys = p.layout.targets.map(t => t.y);
    m.bounds = { minX: Math.min(...xs) - 100, maxX: Math.max(...xs) + 100,
      minY: Math.min(...ys) - 100, maxY: Math.max(...ys) + 100 };
    m.movingTargets = p.layout.targets.map(t => ({ ...t, ...this.randomVelocity(), turnAt: start + 1000 + Math.random() * 2000 }));
    this.matches.set(m.id, m);
    m.players.forEach((player, seat) => { if (player) { player.match = m; player.seat = seat; player.state = 'matched'; } });
  }
  playWithBot(p) {
    // A human match may have been created just before the click arrived.
    // Never replace an existing match or create a second one for this ticket.
    if (p.state === 'searching' && !p.match) this.createMatch(p);
    return this.view(p);
  }
  angles(m, now) {
    const t = Math.max(0, (now - m.starts) / 1000);
    return [-35 + Math.sin(t * 1.6) * 45, 35 + Math.sin(t * 1.9) * 45];
  }
  randomVelocity() {
    const angle = Math.random() * Math.PI * 2, speed = 140 + Math.random() * 80;
    return { vx: Math.cos(angle) * speed, vy: Math.sin(angle) * speed };
  }
  targets(m) { return m.movingTargets.map(p => ({ x: p.x, y: p.y })); }
  moveTargets(m, dt, now) {
    const b = m.bounds;
    for (const p of m.movingTargets) {
      if (now >= p.turnAt) { Object.assign(p, this.randomVelocity()); p.turnAt = now + 1000 + Math.random() * 2000; }
      p.x += p.vx * dt; p.y += p.vy * dt;
    }
    for (let i = 0; i < 4; i++) for (let j = i + 1; j < 4; j++) {
      const p = m.movingTargets[i], q = m.movingTargets[j];
      const dx = q.x - p.x, dy = q.y - p.y, d = Math.hypot(dx, dy);
      if (d < 220) {
        const nx = d > .001 ? dx / d : 1, ny = d > .001 ? dy / d : 0;
        const overlap = (220 - d) / 2;
        p.x -= nx * overlap; p.y -= ny * overlap; q.x += nx * overlap; q.y += ny * overlap;
        const relative = (q.vx - p.vx) * nx + (q.vy - p.vy) * ny;
        if (relative < 0) { p.vx += relative * nx; p.vy += relative * ny; q.vx -= relative * nx; q.vy -= relative * ny; }
      }
    }
    for (const p of m.movingTargets) {
      if (p.x < b.minX) { p.x = b.minX; p.vx = Math.abs(p.vx); }
      if (p.x > b.maxX) { p.x = b.maxX; p.vx = -Math.abs(p.vx); }
      if (p.y < b.minY) { p.y = b.minY; p.vy = Math.abs(p.vy); }
      if (p.y > b.maxY) { p.y = b.maxY; p.vy = -Math.abs(p.vy); }
    }
  }
  fire(m, seat) {
    if (m.questionType === 'essay') return;
    const now = this.now();
    if (m.phase !== 'playing' || now < m.stun[seat] || now < m.cooldown[seat]) return;
    m.cooldown[seat] = now + 350;
    const a = this.angles(m, now)[seat] * Math.PI / 180;
    const dx = -Math.sin(a), dy = Math.cos(a), g = m.layout.guns[seat];
    m.bullets.push({ id: ++m.nextBullet, player: seat, x: g.x + dx * 90, y: g.y + dy * 90, dx, dy });
  }
  endRound(m) { m.phase = 'reveal'; m.revealUntil = this.now() + (m.questionType === 'essay' ? 30000 : 1500); m.bullets = []; }
  choose(m, seat, answer, round) {
    if (m.questionType === 'essay') return;
    const now = this.now();
    if (m.phase !== 'playing' || now >= m.deadline || round !== m.index + 1 ||
        !Number.isInteger(answer) || answer < 0 || answer > 3 || m.shot[answer] || now < m.stun[seat]) return;
    if (answer === m.questions[m.index].correct) {
      m.scores[seat] += 10 + Math.round((m.deadline - now) / 1000);
      this.endRound(m);
    } else {
      m.scores[seat] -= 10; m.shot[answer] = true; m.stun[seat] = now + 1500;
    }
  }
  async submitEssay(p, answer, round, image = null) {
    const m = p.match, seat = p.seat;
    if (!m || m.questionType !== 'essay') throw new Error('Trận này không phải tự luận.');
    if (m.phase !== 'playing' || this.now() >= m.deadline || round !== m.index + 1) throw new Error('Câu hỏi đã kết thúc.');
    if (m.submissions[seat] !== 0) return this.view(p);
    if (typeof answer !== 'string' || (!answer.trim() && !image) || answer.length > 2000) throw new Error('Nhập câu trả lời hoặc đính kèm ảnh bài làm (tối đa 2.000 ký tự).');
    m.submissions[seat] = 1;
    const index = m.index;
    try {
      const review = await this.gradeEssay(m.questions[index], answer.trim(), image);
      if (m.phase !== 'playing' || m.index !== index) return this.view(p);
      if (review.readable === false) { m.submissions[seat] = 0; return { ...this.view(p), error: 'Ảnh chưa đọc được. Bạn chụp rõ toàn bộ bài làm rồi nộp lại nhé.' }; }
      if (!Number.isInteger(review.score) || review.score < 0 || review.score > 10 || typeof review.feedback !== 'string') throw new Error('Invalid grade');
      m.reviews[seat] = { score: review.score, feedback: review.feedback.slice(0, 500), transcription: String(review.transcription || answer).slice(0, 2000), suggestedAnswer: String(review.suggestedAnswer || '').slice(0, 1000) };
      m.scores[seat] += review.score; m.submissions[seat] = 2;
      const question = m.questions[index];
      Promise.resolve().then(() => this.recordEssay({ submissionId: `${m.id}:${index}:${seat}`, matchId: m.id, username: p.username || p.name, questionId: question.id || '',
        subject: question.subject, difficulty: question.difficulty, answer: answer.trim(), hasImage: !!image, ...m.reviews[seat], maxScore: 10 }))
        .catch(() => console.warn('Essay history was not saved; grading result is retained in the match.'));
      if (m.submissions.every(s => s === 2)) this.endRound(m);
      return this.view(p);
    } catch {
      if (m.phase === 'playing' && m.index === index) m.submissions[seat] = 0;
      throw new Error('AI chưa chấm được bài. Bạn thử nộp lại khi còn thời gian nhé.');
    }
  }
  finish(m, winner, reason = '') { m.phase = 'finished'; m.winner = winner; m.reason = reason; m.finishedAt = this.now(); m.bullets = []; }
  leave(p) {
    if (p.match && p.match.phase !== 'finished') this.finish(p.match, 1 - p.seat, 'Đối thủ đã rời trận.');
    p.state = 'cancelled'; this.players.delete(p.token);
  }
  tick() {
    const now = this.now();
    for (const p of this.players.values()) {
      if (now - p.seen > 15000 && (!p.match || p.match.phase !== 'finished')) { this.leave(p); continue; }
      if (p.state === 'searching' && now - p.joined >= this.waitMs) this.createMatch(p);
      if (p.match?.phase === 'finished' && now - p.match.finishedAt > 300000) this.players.delete(p.token);
    }
    for (const m of this.matches.values()) {
      if (m.phase === 'finished') { if (now - m.finishedAt > 300000) this.matches.delete(m.id); continue; }
      const dt = Math.min(.1, Math.max(0, (now - m.lastTick) / 1000)); m.lastTick = now;
      if (m.phase === 'countdown' && now >= m.starts) m.phase = 'playing';
      if (m.phase === 'reveal' && now >= m.revealUntil) {
        m.index++;
        if (m.index >= m.questions.length) { this.finish(m, m.scores[0] === m.scores[1] ? -1 : m.scores[0] > m.scores[1] ? 0 : 1); continue; }
        if (m.questionType === 'essay') m.roundMs = essayDuration(m.questions[m.index].difficulty);
        m.phase = 'playing'; m.deadline = now + m.roundMs; m.shot.fill(false); m.stun.fill(0);
        m.submissions = [0, 0]; m.reviews = [null, null];
        m.botNext = now + 3000 + Math.random() * 2000;
      }
      if (m.phase !== 'playing') continue;
      if (m.questionType === 'essay') {
        if (m.bot && m.submissions[1] === 0 && now >= m.deadline - m.roundMs * .6) {
          // Bot score is simulated; human submissions are always graded by AI.
          const score = 4 + Math.floor(Math.random() * 7);
          m.scores[1] += score; m.submissions[1] = 2;
          m.reviews[1] = { score, feedback: 'Máy Planet đã hoàn thành.' };
        }
        if (m.submissions.every(s => s === 2) || (now >= m.deadline && (!m.submissions.includes(1) || now >= m.deadline + 25000))) this.endRound(m);
        continue;
      }
      this.moveTargets(m, dt, now);
      if (now >= m.deadline) { this.endRound(m); continue; }
      const targets = this.targets(m, now);
      if (m.bot && now >= (m.botNext || 0)) {
        m.botNext = now + 1000 + Math.random() * 1000;
        // Bot waits for its swaying ship to aim near a target, with imperfect decisions.
        const correct = m.questions[m.index].correct;
        const target = targets[Math.random() < .75 ? correct : Math.floor(Math.random() * 4)];
        const gun = m.layout.guns[1], a = this.angles(m, now)[1] * Math.PI / 180;
        const dx = target.x - gun.x, dy = target.y - gun.y;
        const dot = (-Math.sin(a) * dx + Math.cos(a) * dy) / Math.hypot(dx, dy);
        if (dot > .94) this.fire(m, 1);
      }
      for (let i = m.bullets.length - 1; i >= 0; i--) {
        const b = m.bullets[i], ox = b.x, oy = b.y;
        b.x += b.dx * 1500 * dt; b.y += b.dy * 1500 * dt;
        let hit = -1;
        for (let k = 0; k < 4; k++) {
          if (m.shot[k]) continue;
          const vx = b.x - ox, vy = b.y - oy;
          const u = Math.max(0, Math.min(1, ((targets[k].x - ox) * vx + (targets[k].y - oy) * vy) / (vx * vx + vy * vy || 1)));
          if (Math.hypot(ox + vx * u - targets[k].x, oy + vy * u - targets[k].y) < 110) { hit = k; break; }
        }
        if (hit >= 0) {
          m.bullets.splice(i, 1);
          if (hit === m.questions[m.index].correct) {
            m.scores[b.player] += 10 + Math.round((m.deadline - now) / 1000); this.endRound(m); break;
          }
          m.scores[b.player] -= 10;
          m.shot[hit] = true; m.stun[b.player] = now + 1500;
        } else if (Math.abs(b.x) > 2500 || Math.abs(b.y) > 1500) m.bullets.splice(i, 1);
      }
    }
  }
  view(p) {
    const now = this.now(), m = p.match;
    if (!m) return { token: p.token, state: p.state, waited: (now - p.joined) / 1000, waitLimit: this.waitMs / 1000 };
    const q = m.questions[Math.min(m.index, m.questions.length - 1)];
    return { token: p.token, matchId: m.id, finishedAt: m.finishedAt ? new Date(m.finishedAt).toISOString() : '', maxScore: m.questions.length * (m.questionType === 'essay' ? 10 : 10 + m.roundMs / 1000), state: m.phase, seat: p.seat, bot: m.bot, names: m.names,
      questionType: m.questionType, submissions: m.submissions,
      difficulty: q.difficulty || '', duration: m.roundMs / 1000, revealRemaining: m.phase === 'reveal' ? Math.max(0, (m.revealUntil - now) / 1000) : 0,
      transcription: ['reveal', 'finished'].includes(m.phase) ? (m.reviews[p.seat]?.transcription || '') : '',
      suggestedAnswer: ['reveal', 'finished'].includes(m.phase) ? (m.reviews[p.seat]?.suggestedAnswer || '') : '',
      sampleAnswer: m.questionType === 'essay' && ['reveal', 'finished'].includes(m.phase) ? q.sampleAnswer : '',
      feedback: m.questionType === 'essay' && ['reveal', 'finished'].includes(m.phase) ? (m.reviews[p.seat]?.feedback || (m.submissions[p.seat] === 1 ? 'AI chưa chấm xong trong thời gian cho phép; bài này chưa được cộng điểm.' : 'Bạn chưa có bài được chấm ở câu này.')) : '',
      earned: m.reviews[p.seat]?.score ?? 0,
      countdown: Math.max(0, (m.starts - now) / 1000), remaining: Math.max(0, (m.deadline - now) / 1000),
      round: Math.min(m.index + 1, m.questions.length), total: m.questions.length, text: q.text, options: q.options,
      correct: m.phase === 'reveal' || m.phase === 'finished' ? q.correct : -1,
      scores: m.scores, angles: this.angles(m, now), targets: this.targets(m, now), shot: m.shot,
      stunned: m.stun.map(t => now < t), bullets: m.bullets, winner: m.winner, reason: m.reason };
  }
}

export function createServer(arena = new Arena(), chat = createChat(), { essaySource = createEssaySource(), football = new FootballPractice({ source: essaySource }) } = {}) {
  const server = http.createServer(async (req, res) => {
    res.setHeader('Content-Type', 'application/json; charset=utf-8');
    res.setHeader('Access-Control-Allow-Origin', '*');
    res.setHeader('Access-Control-Allow-Headers', 'Content-Type');
    res.setHeader('Access-Control-Allow-Methods', 'POST, GET, OPTIONS');
    res.setHeader('Cache-Control', 'no-store');
    if (req.method === 'OPTIONS') { res.writeHead(204); res.end(); return; }
    if (req.url === '/health') { res.end(JSON.stringify({ ok: true, essayQuestions: essaySource.status?.() })); return; }
    try {
      if (req.method !== 'POST') { res.writeHead(405); res.end('{}'); return; }
      const chunks = []; let size = 0;
      for await (const chunk of req) {
        size += chunk.length;
        if (size > (req.url === '/essay' || req.url === '/football/submit' ? 3000000 : 100000)) throw new Error('Yêu cầu quá lớn.');
        chunks.push(chunk);
      }
      const raw = Buffer.concat(chunks).toString('utf8');
      const body = JSON.parse(raw || '{}');
      if (req.url.startsWith('/football/')) {
        let result;
        if (req.url === '/football/start') result = await football.start(body);
        else if (req.url === '/football/leave') { football.sessions.delete(body.token); result = { success: true }; }
        else {
          const session = football.get(body.token);
          if (req.url === '/football/state') result = football.view(session);
          else if (req.url === '/football/submit') result = await football.submit(session, body);
          else if (req.url === '/football/hint') result = await football.getHint(session, body);
          else if (req.url === '/football/next') result = football.next(session, body);
          else { res.writeHead(404); res.end('{}'); return; }
        }
        res.end(JSON.stringify(result)); return;
      }
      if (req.url === '/chat') {
        if (body.clientId !== undefined && (typeof body.clientId !== 'string' || !/^[a-zA-Z0-9-]{16,64}$/.test(body.clientId))) {
          res.writeHead(400); res.end(JSON.stringify({ error: 'Phiên trò chuyện không hợp lệ. Hãy mở lại game.' })); return;
        }
        const controller = new AbortController();
        const cancel = () => { if (!res.writableEnded) controller.abort(); };
        res.on('close', cancel);
        try {
          // Separate game sessions sharing an IP; this is not authentication.
          const client = `${req.socket.remoteAddress}:${body.clientId || 'legacy'}`;
          const result = await chat(body, client, controller.signal);
          if (!res.destroyed) {
            if (result.retryAfter) res.setHeader('Retry-After', String(result.retryAfter));
            res.writeHead(result.status); res.end(JSON.stringify(result));
          }
        } finally { res.off('close', cancel); }
        return;
      }
      if (req.url === '/join') {
        if (arena.players.size > 2000) throw new Error('Máy chủ đang đầy.');
        if (body.questionType === 'essay') arena.essayBank = await essaySource();
        res.end(JSON.stringify(arena.join(body))); return;
      }
      const p = arena.players.get(body.token);
      if (!p) { res.writeHead(401); res.end(JSON.stringify({ error: 'Phiên đã hết hạn. Hãy tìm trận lại.' })); return; }
      p.seen = arena.now(); arena.tick();
      if (req.url === '/leave') { arena.leave(p); res.end('{}'); return; }
      if (req.url === '/bot') { res.end(JSON.stringify(arena.playWithBot(p))); return; }
      if (req.url === '/essay') {
        const image = validateEssayImage(body.imageBase64, body.mimeType);
        res.end(JSON.stringify(await arena.submitEssay(p, body.text || '', body.round, image))); return;
      }
      if (req.url !== '/poll') { res.writeHead(404); res.end('{}'); return; }
      if (body.playWithBot === true) arena.playWithBot(p);
      if (body.fire && p.match) arena.fire(p.match, p.seat);
      if (p.match && body.answer !== undefined) arena.choose(p.match, p.seat, body.answer, body.round);
      res.end(JSON.stringify(arena.view(p)));
    } catch (error) { res.writeHead(400); res.end(JSON.stringify({ error: error.message })); }
  });
  const timer = setInterval(() => arena.tick(), 25);
  server.on('close', () => clearInterval(timer));
  return server;
}

if (process.argv[1] && fileURLToPath(import.meta.url) === process.argv[1]) {
  const source = createEssaySource({ cacheDirectory: new URL('./.cache/essay/', import.meta.url) });
  // Warm the real Sheet bank without delaying server startup. Later restarts
  // can reuse the validated snapshot while Google Sheets is slow.
  source().catch(() => console.warn('Chưa làm nóng được dữ liệu TuLuan; sẽ thử lại khi có yêu cầu.'));
  createServer(undefined, undefined, { essaySource: source }).listen(Number(process.env.PORT || 8787), '0.0.0.0', () => console.log('Planet server ready on port ' + (process.env.PORT || 8787)));
}
