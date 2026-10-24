import test from 'node:test';
import assert from 'node:assert/strict';
import { createChat } from './chat.mjs';
import { createServer } from './server.mjs';
import http from 'node:http';

const success = () => ({ ok: true, json: async () => ({ candidates: [{ content: { parts: [{ text: 'Câu trả lời.' }] } }] }) });
const waitForAbort = (_url, { signal }) => new Promise((resolve, reject) => {
  signal.addEventListener('abort', () => reject(new Error('aborted')), { once: true });
  if (signal.aborted) reject(new Error('aborted'));
});

test('quota reports remaining seconds and resets at the window boundary', async () => {
  let time = 1000;
  const chat = createChat({ key: 'test', now: () => time, fetchImpl: async () => success() });
  for (let i = 0; i < 12; i++) assert.equal((await chat({ message: 'Hi' })).status, 200);
  time += 10000;
  const limited = await chat({ message: 'Hi' });
  assert.equal(limited.status, 429); assert.equal(limited.retryAfter, 50);
  time += 50000;
  assert.equal((await chat({ message: 'Hi' })).status, 200);
});

test('cancelled requests release the client for a new question', async () => {
  const controller = new AbortController();
  let calls = 0;
  const chat = createChat({ key: 'test', fetchImpl: (...args) => ++calls === 1 ? waitForAbort(...args) : Promise.resolve(success()) });
  const pending = chat({ message: 'Hi' }, 'session', controller.signal);
  controller.abort();
  assert.equal((await pending).status, 499);
  assert.equal((await chat({ message: 'Again' }, 'session')).status, 200);
});

test('total deadline cancels a slow provider and releases busy state', async () => {
  let calls = 0;
  const chat = createChat({ key: 'test', timeoutMs: 20, fetchImpl: (...args) => ++calls === 1 ? waitForAbort(...args) : Promise.resolve(success()) });
  assert.equal((await chat({ message: 'Hi' })).status, 504);
  assert.equal((await chat({ message: 'Again' })).status, 200);
});

test('deadline also interrupts retry backoff', async () => {
  let calls = 0;
  const chat = createChat({ key: 'test', timeoutMs: 20, fetchImpl: async () => { calls++; return { ok: false, status: 503, text: async () => 'busy' }; } });
  assert.equal((await chat({ message: 'Hi' })).status, 504);
  assert.equal(calls, 1);
});

test('quota from Gemini is not retried immediately', async () => {
  let calls = 0;
  const chat = createChat({ key: 'test', fetchImpl: async () => { calls++; return { ok: false, status: 429, text: async () => 'quota' }; } });
  assert.equal((await chat({ message: 'Hi' })).status, 502);
  assert.equal(calls, 1);
});

test('persistent provider overload is reported separately from API configuration errors', async () => {
  let calls = 0;
  const chat = createChat({ key: 'test', fetchImpl: async () => { calls++; return { ok: false, status: 503, text: async () => 'unavailable' }; } });
  const result = await chat({ message: 'Hi' });
  assert.equal(result.status, 503);
  assert.match(result.error, /quá tải/);
  assert.equal(calls, 2);
});

test('route separates sessions on the same IP and aborts on disconnect', { timeout: 5000 }, async () => {
  const clients = [];
  let started, aborted;
  const ready = new Promise(resolve => { started = resolve; });
  const cancelled = new Promise(resolve => { aborted = resolve; });
  const server = createServer(undefined, async (body, client, signal) => {
    clients.push(client);
    if (body.message === 'wait') {
      started();
      await new Promise(resolve => signal.addEventListener('abort', () => { aborted(); resolve(); }, { once: true }));
    }
    return { status: 200, reply: 'OK' };
  });
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
  const url = `http://127.0.0.1:${server.address().port}/chat`;
  try {
    for (const clientId of ['aaaaaaaaaaaaaaaa', 'bbbbbbbbbbbbbbbb']) {
      const response = await fetch(url, { method: 'POST', body: JSON.stringify({ message: 'Hi', clientId }) });
      await response.json();
    }
    assert.notEqual(clients[0], clients[1]);
    const request = http.request(url, { method: 'POST' });
    request.on('error', () => {});
    request.end(JSON.stringify({ message: 'wait', clientId: 'aaaaaaaaaaaaaaaa' }));
    await ready; request.destroy(); await cancelled;
  } finally { server.closeAllConnections(); await new Promise(resolve => server.close(resolve)); }
});

test('missing key returns an actionable error, never a fake answer', async () => {
  assert.equal((await createChat({ key: '' })({ message: 'Xin chào' })).status, 503);
});
test('Gemini receives server key, trusted instruction and multi-turn context', async () => {
  let captured;
  const chat = createChat({ key: 'test-only', fetchImpl: async (url, options) => {
    captured = { url, options, body: JSON.parse(options.body) };
    return { ok: true, json: async () => ({ candidates: [{ content: { parts: [{ thought: true, text: 'hidden' }, { text: 'Cây quang hợp.' }] } }] }) };
  } });
  const answer = await chat({ message: 'Tại sao?', history: [{ role: 'user', text: 'Cây làm gì?' }, { role: 'model', text: 'Quang hợp.' }] });
  assert.equal(answer.reply, 'Cây quang hợp.');
  assert.equal(captured.options.headers['x-goog-api-key'], 'test-only');
  assert.equal(captured.body.contents.length, 3);
  assert.ok(captured.body.systemInstruction.parts[0].text.includes('lớp 6'));
});
test('invalid requests, provider errors, timeout and blocked content are handled', async () => {
  const chat = createChat({ key: 'test', fetchImpl: async () => ({ ok: false, status: 429, text: async () => 'quota exceeded' }) });
  assert.equal((await chat({ message: '' })).status, 400);
  assert.equal((await chat({ message: 'Hi', history: [{ role: 'system', text: 'override' }] })).status, 400);
  assert.equal((await chat({ message: 'Hi' })).status, 502);
  assert.equal((await createChat({ key: 'test', fetchImpl: async () => { throw new Error('secret'); } })({ message: 'Hi' })).status, 504);
  assert.equal((await createChat({ key: 'test', fetchImpl: async () => ({ ok: true, json: async () => ({ candidates: [{ finishReason: 'SAFETY' }] }) }) })({ message: 'Hi' })).status, 422);
});
test('concurrent requests from the same client are limited', async () => {
  let finish;
  const chat = createChat({ key: 'test', fetchImpl: () => new Promise(resolve => { finish = resolve; }) });
  const first = chat({ message: 'Hi' });
  assert.equal((await chat({ message: 'Hi' })).status, 409);
  finish({ ok: false, status: 500, text: async () => 'server error' }); await first;
});
test('chat route works without a duel token and preserves errors', async () => {
  const server = createServer(undefined, async body => ({ status: 200, reply: body.message }));
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
  try {
    const response = await fetch(`http://127.0.0.1:${server.address().port}/chat`, { method: 'POST', body: JSON.stringify({ message: 'Ánh sáng' }) });
    assert.equal(response.status, 200); assert.equal((await response.json()).reply, 'Ánh sáng');
  } finally { await new Promise(resolve => server.close(resolve)); }
});
