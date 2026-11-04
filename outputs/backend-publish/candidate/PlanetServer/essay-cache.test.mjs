import test from 'node:test';
import assert from 'node:assert/strict';
import { mkdtempSync, readdirSync, readFileSync, writeFileSync, unlinkSync, rmdirSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { createEssaySource } from './essay-source.mjs';

const payload = (id = 'TL1') => ({ success: true, questionType: 'essay', questions: [{ id, monId: 'Vật lý', topic: 'Chương 1',
  difficulty: 'Dễ', question: 'Đề bài thật', sampleAnswer: 'Đáp án', rubric: 'Ý đúng', maxPoints: 10 }] });
const ok = data => ({ ok: true, json: async () => data });
const settle = () => new Promise(resolve => setImmediate(resolve));
function directory(t) {
  const path = mkdtempSync(join(tmpdir(), 'khtn-essay-cache-'));
  t.after(() => { for (const file of readdirSync(path)) unlinkSync(join(path, file)); rmdirSync(path); });
  return path;
}

test('expired in-memory bank serves immediately while one background refresh replaces it', { timeout: 1000 }, async () => {
  let now = 1000, calls = 0, finish;
  const source = createEssaySource({ now: () => now, fetchImpl: async () => {
    if (++calls === 1) return ok(payload());
    return new Promise(resolve => { finish = () => resolve(ok(payload('TL2'))); });
  } });
  await source(); now += 61000;
  assert.equal((await source())[0].id, 'TL1'); assert.equal(source.status().refreshing, true);
  for (let i = 0; i < 10; i++) assert.equal((await source())[0].id, 'TL1');
  assert.equal(calls, 2);
  finish(); await settle();
  assert.equal((await source())[0].id, 'TL2'); assert.equal(source.status().refreshing, false);
});

test('temporary network failure preserves the approved bank and backs off repeated refresh attempts', async () => {
  let now = 1000, calls = 0;
  const source = createEssaySource({ now: () => now, onWarning() {}, fetchImpl: async () => {
    if (++calls === 1) return ok(payload());
    throw new TypeError('fetch failed');
  } });
  await source(); now += 61000;
  assert.equal((await source()).length, 1); await settle();
  for (let i = 0; i < 10; i++) await source();
  assert.equal(calls, 2); assert.equal(source.status().ready, true);
  now += 30000; await source(); await settle(); assert.equal(calls, 3);
});

test('validated snapshot survives process/source recreation and contains no credentials', async t => {
  const cacheDirectory = directory(t); let now = 1000, calls = 0;
  const options = { url: 'https://example.test/exec', secret: 'private-server-secret', cacheDirectory, now: () => now, onWarning() {} };
  await createEssaySource({ ...options, fetchImpl: async () => ok(payload()) })();
  const files = readdirSync(cacheDirectory); assert.equal(files.length, 1);
  assert.ok(!readFileSync(join(cacheDirectory, files[0]), 'utf8').includes('private-server-secret'));
  const restarted = createEssaySource({ ...options, fetchImpl: async () => { calls++; throw new TypeError('offline'); } });
  assert.equal((await restarted())[0].id, 'TL1'); assert.equal(calls, 0);
  now += 61000; assert.equal((await restarted())[0].id, 'TL1'); await settle();
  assert.equal(calls, 1); assert.equal(restarted.status().ready, true);
});

test('snapshot from another URL or credential never substitutes for that source', async t => {
  const cacheDirectory = directory(t);
  const options = { cacheDirectory, now: () => 1000, url: 'https://one.test/exec', secret: 'one' };
  await createEssaySource({ ...options, fetchImpl: async () => ok(payload()) })();
  for (const extra of [{ url: 'https://two.test/exec' }, { secret: 'two' }]) {
    const other = createEssaySource({ ...options, ...extra, fetchImpl: async () => { throw new TypeError('offline'); } });
    await assert.rejects(other(), /offline/); assert.equal(other.status().ready, false);
  }
});

test('old or corrupt disk snapshots cannot provide fabricated questions', async t => {
  const cacheDirectory = directory(t); let now = 1000;
  const options = { cacheDirectory, now: () => now, maxCacheAgeMs: 1000 };
  await createEssaySource({ ...options, fetchImpl: async () => ok(payload()) })();
  now += 1001;
  await assert.rejects(createEssaySource({ ...options, fetchImpl: async () => { throw new TypeError('offline'); } })(), /offline/);
  now = 1000; const file = join(cacheDirectory, readdirSync(cacheDirectory)[0]); writeFileSync(file, 'broken json');
  await assert.rejects(createEssaySource({ ...options, fetchImpl: async () => { throw new TypeError('offline'); } })(), /offline/);
});

test('upstream authorization or schema errors invalidate the bank instead of silently continuing', async t => {
  const cacheDirectory = directory(t); let now = 1000, calls = 0;
  const source = createEssaySource({ cacheDirectory, now: () => now, onWarning() {}, fetchImpl: async () =>
    ok(++calls === 1 ? payload() : { success: false, message: 'Unauthorized essay access' }) });
  await source(); now += 61000;
  await source(); await settle();
  await assert.rejects(source(), /APPS_SCRIPT_SECRET/);
  assert.equal(source.status().ready, false); assert.equal(readdirSync(cacheDirectory).length, 0);
});

test('valid empty latest bank removes old questions rather than filling from stale data', async () => {
  let now = 1000, calls = 0;
  const source = createEssaySource({ now: () => now, fetchImpl: async () => ok(++calls === 1 ? payload() : { success: true, questionType: 'essay', questions: [] }) });
  await source(); now += 61000; await source(); await settle();
  assert.deepEqual(await source(), []);
});
