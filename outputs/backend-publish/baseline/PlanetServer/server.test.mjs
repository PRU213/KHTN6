import { test } from 'node:test';
import assert from 'node:assert/strict';
import { Arena, createServer } from './server.mjs';
import { readFileSync } from 'node:fs';

test('each difficulty supplies five distinct questions per subject and filters correctly', () => {
  const bank = JSON.parse(readFileSync(new URL('./questions.json', import.meta.url), 'utf8'));
  for (const difficulty of ['Dễ', 'Trung bình', 'Khó']) {
    const { arena } = fixture();
    const a = arena.join(request({ difficulties: [difficulty] }));
    arena.join(request({ difficulties: [difficulty] }));
    const questions = arena.players.get(a.token).match.questions;
    assert.equal(questions.length, 15);
    for (const q of questions) assert.equal(bank.find(b => b.text === q.text).difficulty, difficulty);
    for (const subject of ['Physics', 'Chemistry', 'Biology']) assert.equal(questions.filter(q => q.subject === subject).length, 5);
  }
});

const request = (extra = {}) => ({ name: 'Học sinh', subject: 'Physics', difficulties: ['Dễ'],
  guns: [{ x: -650, y: -400 }, { x: 650, y: -400 }],
  targets: [{ x: -400, y: 100 }, { x: -120, y: 180 }, { x: 160, y: 180 }, { x: 440, y: 100 }], ...extra });

test('direct answer clicks respect stun, invalid indices, old rounds and single scoring', () => {
  const { arena, advance } = fixture();
  const a = arena.join(request()); arena.join(request());
  const m = arena.players.get(a.token).match;
  const correct = m.questions[0].correct, wrong = (correct + 1) % 4;
  arena.choose(m, 0, correct, 1); assert.equal(m.scores[0], 0);
  advance(4000);
  arena.choose(m, 0, 99, 1); arena.choose(m, 0, correct, 0);
  assert.equal(m.scores[0], 0);
  arena.choose(m, 0, wrong, 1); assert.equal(m.scores[0], -10);
  arena.choose(m, 0, correct, 1); assert.equal(m.scores[0], -10);
  advance(1500);
  arena.choose(m, 0, correct, 1); assert.equal(m.scores[0], 19);
  arena.choose(m, 1, correct, 1); assert.equal(m.scores[1], 0);
  advance(1500);
  arena.choose(m, 0, m.questions[1].correct, 1); assert.equal(m.phase, 'playing');
  arena.choose(m, 0, m.questions[1].correct, 2); assert.equal(m.phase, 'reveal');
});
test('play with bot immediately preserves selected mode and difficulty and leaves the queue', () => {
  for (const questionType of ['multipleChoice', 'essay']) {
    const { arena } = fixture();
    const joined = arena.join(request({ questionType, difficulties: ['Khó'] }));
    const p = arena.players.get(joined.token);
    const state = arena.playWithBot(p);
    assert.equal(state.bot, true); assert.equal(state.state, 'countdown');
    assert.equal(state.total, questionType === 'essay' ? 9 : 15);
    assert.equal(p.match.questionType, questionType);
    if (questionType === 'essay') assert.equal(p.match.roundMs, 240000);
    const match = p.match;
    arena.playWithBot(p);
    assert.equal(arena.matches.size, 1); assert.equal(p.match, match);
    const late = arena.join(request({ questionType, difficulties: ['Khó'] }));
    assert.equal(late.state, 'searching'); assert.equal(p.match, match);
  }
});

test('a late bot request never replaces an already paired human opponent', () => {
  const { arena } = fixture();
  const first = arena.join(request()); arena.join(request());
  const p = arena.players.get(first.token), match = p.match;
  const state = arena.playWithBot(p);
  assert.equal(state.bot, false); assert.equal(p.match, match);
  assert.equal(arena.matches.size, 1);
});

test('automatic timeout and manual bot request do not create duplicate matches', () => {
  const { arena, advance } = fixture();
  const first = arena.join(request()), p = arena.players.get(first.token);
  advance(120000);
  const match = p.match;
  arena.playWithBot(p);
  assert.equal(p.match, match); assert.equal(match.bot, true); assert.equal(arena.matches.size, 1);
});

test('HTTP polling switches an existing searching ticket directly to bot play', async () => {
  const { arena } = fixture();
  const first = arena.join(request());
  const server = createServer(arena);
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
  try {
    const url = `http://127.0.0.1:${server.address().port}/poll`;
    for (let i = 0; i < 2; i++) {
      const response = await fetch(url, { method: 'POST', body: JSON.stringify({ token: first.token, playWithBot: true }) });
      const state = await response.json();
      assert.equal(response.status, 200); assert.equal(state.bot, true); assert.equal(state.state, 'countdown');
    }
    assert.equal(arena.matches.size, 1);
  } finally { server.closeAllConnections(); await new Promise(resolve => server.close(resolve)); }
});

test('explicit bot route immediately starts both question modes and is idempotent', async () => {
  for (const questionType of ['multipleChoice', 'essay']) {
    const { arena } = fixture();
    const first = arena.join(request({ questionType }));
    const server = createServer(arena);
    await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
    try {
      for (let i = 0; i < 2; i++) {
        const response = await fetch(`http://127.0.0.1:${server.address().port}/bot`, { method: 'POST', body: JSON.stringify({ token: first.token }) });
        const state = await response.json();
        assert.equal(response.status, 200); assert.equal(state.state, 'countdown');
        assert.equal(state.bot, true); assert.equal(state.countdown, 4);
        assert.equal(state.total, questionType === 'essay' ? 9 : 15);
      }
      assert.equal(arena.matches.size, 1);
    } finally { server.closeAllConnections(); await new Promise(resolve => server.close(resolve)); }
  }
});

test('explicit bot route requires a valid ticket and preserves a raced human match', async () => {
  const { arena } = fixture();
  const first = arena.join(request()); arena.join(request());
  const server = createServer(arena);
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
  try {
    const url = `http://127.0.0.1:${server.address().port}/bot`;
    const invalid = await fetch(url, { method: 'POST', body: JSON.stringify({ token: 'invalid' }) });
    assert.equal(invalid.status, 401); await invalid.json();
    const response = await fetch(url, { method: 'POST', body: JSON.stringify({ token: first.token }) });
    const state = await response.json(); assert.equal(state.bot, false);
    assert.equal(arena.matches.size, 1);
  } finally { server.closeAllConnections(); await new Promise(resolve => server.close(resolve)); }
});

function fixture() {
  let now = 1000000;
  const arena = new Arena({ now: () => now });
  const advance = ms => {
    for (let elapsed = 0; elapsed < ms; elapsed += 25) {
      now += Math.min(25, ms - elapsed);
      for (const p of arena.players.values()) p.seen = now;
      arena.tick();
    }
  };
  return { arena, advance, setTime: value => { now = value; } };
}

test('two devices get the same authoritative questions, distinct seats and no leaked answer', () => {
  const { arena } = fixture();
  const a = arena.join(request()), b = arena.join(request({ name: 'Đối thủ' }));
  const p = arena.players.get(a.token), q = arena.players.get(b.token);
  assert.equal(p.match, q.match); assert.notEqual(p.seat, q.seat);
  assert.equal(p.match.bot, false);
  assert.deepEqual(arena.view(p).options, arena.view(q).options);
  assert.equal(arena.view(p).correct, -1);
  assert.equal(arena.view(p).state, 'countdown');
});

test('different subject selections pair into one mixed 15-question match', () => {
  const { arena } = fixture();
  const a = arena.join(request({ difficulties: ['Dễ', 'Khó'] }));
  const b = arena.join(request({ subject: 'Chemistry', difficulties: ['Khó', 'Dễ', 'Dễ'] }));
  const c = arena.join(request({ difficulties: ['Trung bình'] }));
  const m = arena.players.get(a.token).match;
  assert.equal(m, arena.players.get(b.token).match);
  assert.equal(arena.players.get(c.token).state, 'searching');
  assert.equal(m.questions.length, 15);
  for (const subject of ['Physics', 'Chemistry', 'Biology']) assert.equal(m.questions.filter(q => q.subject === subject).length, 5);
  assert.equal(new Set(m.questions.map(q => q.text)).size, 15);
});

test('120-second threshold creates bot exactly once and excludes late human', () => {
  const { arena, advance } = fixture();
  const a = arena.join(request()), p = arena.players.get(a.token);
  advance(119975); assert.equal(p.match, null);
  advance(25); const match = p.match; assert.equal(match.bot, true);
  const b = arena.join(request());
  assert.equal(arena.players.get(b.token).match, null);
  advance(100); assert.equal(p.match, match);
});

test('cancel removes queue ticket; leaving or disconnecting a match awards opponent', () => {
  const { arena, setTime } = fixture();
  const a = arena.join(request()); arena.leave(arena.players.get(a.token));
  assert.equal(arena.players.size, 0);
  const b = arena.join(request()), c = arena.join(request());
  const p = arena.players.get(b.token), q = arena.players.get(c.token);
  setTime(1016000); q.seen = 1016000; arena.tick();
  assert.equal(p.match.phase, 'finished'); assert.equal(p.match.winner, q.seat);
  assert.equal(arena.players.has(b.token), false);
});

test('server rejects firing before start, enforces cooldown and does not accept client points', () => {
  const { arena, advance } = fixture();
  const a = arena.join(request()); arena.join(request());
  const p = arena.players.get(a.token), m = p.match;
  arena.fire(m, p.seat); assert.equal(m.bullets.length, 0);
  advance(4000); arena.fire(m, p.seat); arena.fire(m, p.seat);
  assert.equal(arena.view(p).remaining, 20);
  assert.equal(m.bullets.length, 1); assert.deepEqual(m.scores, [0, 0]);
});

test('server collision awards one player and advances round only once', () => {
  const { arena, advance } = fixture();
  const a = arena.join(request()); arena.join(request());
  const m = arena.players.get(a.token).match;
  advance(4000);
  const target = arena.targets(m, arena.now())[m.questions[0].correct];
  m.bullets.push({ id: 1, player: 0, x: target.x, y: target.y, dx: 0, dy: 1 });
  advance(25); assert.equal(m.phase, 'reveal'); assert.ok(m.scores[0] >= 10); assert.equal(m.scores[1], 0);
  advance(1500); assert.equal(m.index, 1); assert.equal(m.phase, 'playing');
});

test('wrong hit stuns player and round timeout / complete game produces result', () => {
  const { arena, advance } = fixture();
  const a = arena.join(request()); arena.join(request());
  const m = arena.players.get(a.token).match;
  advance(4000);
  const wrong = (m.questions[0].correct + 1) % 4;
  const target = arena.targets(m, arena.now())[wrong];
  m.bullets.push({ id: 1, player: 0, x: target.x, y: target.y, dx: 0, dy: 1 });
  advance(25); assert.equal(m.shot[wrong], true);
  assert.deepEqual(m.scores, [-10, 0]);
  arena.fire(m, 0); assert.equal(m.bullets.length, 0);
  advance(300975); assert.equal(m.phase, 'playing'); assert.equal(m.index, 14);
  advance(21500); assert.equal(m.phase, 'finished'); assert.equal(m.winner, 1);
  assert.equal(arena.view(arena.players.get(a.token)).round, 15);
});

test('planets roam, turn and stay inside bounds with identical positions on both clients', () => {
  const { arena, advance } = fixture();
  const a = arena.join(request()), b = arena.join(request());
  const p = arena.players.get(a.token), q = arena.players.get(b.token), m = p.match;
  const before = arena.targets(m);
  advance(6000);
  assert.notDeepEqual(arena.targets(m), before);
  assert.deepEqual(arena.view(p).targets, arena.view(q).targets);
  for (let i = 0; i < 100; i++) {
    advance(100);
    for (const target of arena.targets(m)) {
      assert.ok(target.x >= m.bounds.minX && target.x <= m.bounds.maxX);
      assert.ok(target.y >= m.bounds.minY && target.y <= m.bounds.maxY);
    }
  }
});

test('abandoned queue expires instead of creating a bot', () => {
  const { arena, setTime } = fixture();
  const a = arena.join(request()); setTime(1120000); arena.tick();
  assert.equal(arena.players.has(a.token), false); assert.equal(arena.matches.size, 0);
});

test('bot plays through normal bullets and can earn points without client input', () => {
  const { arena, advance } = fixture();
  const random = Math.random;
  try {
    Math.random = () => .5;
    const a = arena.join(request()); advance(120000);
    const m = arena.players.get(a.token).match;
    advance(7000);
    assert.equal(m.nextBullet, 0, 'bot must wait before aiming at first answer');
    advance(35000);
    assert.ok(m.nextBullet > 0, 'bot should fire actual projectiles');
    assert.ok(m.scores[1] > 0, 'bot should hit an answer through authoritative collision');
  } finally { Math.random = random; }
});

test('HTTP join, poll, leave and invalid-session paths work over real socket', async t => {
  const server = createServer(); await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
  t.after(() => new Promise(resolve => server.close(resolve)));
  const url = `http://127.0.0.1:${server.address().port}`;
  const post = (path, body) => fetch(url + path, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) });
  const a = await (await post('/join', request())).json();
  const b = await (await post('/join', request())).json();
  const state = await (await post('/poll', { token: a.token })).json();
  assert.equal(state.state, 'countdown'); assert.deepEqual(state.options, b.options);
  await post('/leave', { token: a.token });
  const result = await (await post('/poll', { token: b.token })).json();
  assert.equal(result.state, 'finished'); assert.equal(result.winner, result.seat);
  assert.equal((await post('/poll', { token: 'invalid' })).status, 401);
  assert.equal((await post('/join', {})).status, 400);
});
