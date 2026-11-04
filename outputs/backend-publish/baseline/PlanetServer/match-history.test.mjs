import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import vm from 'node:vm';
import { Arena } from './server.mjs';

function fixture(main = 'Code.gs') {
  let current = Date.parse('2026-10-24T05:00:00Z');
  class Clock extends Date { constructor(...args) { super(...(args.length ? args : [current])); } static now() { return current; } }
  function sheet(initial) {
    const data = initial.map(r => [...r]);
    let columns = 8;
    return { data, getMaxColumns: () => columns, insertColumnsAfter: (_after, count) => { columns += count; }, getDataRange: () => ({ getValues: () => data.map(r => [...r]) }),
      getRange: (row, col, height, width) => ({
        getValues: () => Array.from({ length: height }, (_, y) => Array.from({ length: width }, (_, x) => data[row - 1 + y]?.[col - 1 + x] ?? '')),
        setValues: values => values.forEach((line, y) => line.forEach((value, x) => { (data[row - 1 + y] ??= [])[col - 1 + x] = value; }))
      }), appendRow: row => data.push([...row]) };
  }
  const sheets = {
    Users: sheet([['ID', 'Username', 'Password', 'FullName'], ['U1', 'student01', '', 'Học sinh Một'], ['U2', 'student02', '', 'Học sinh Hai']]),
    History: sheet([['Username', 'Opponent', 'Mode', 'Result', 'Score', 'Time'], ['student01', 'Máy AI', 'Luyện tập', 'THẮNG', 90, new Clock()]]),
    TienDoHocTap: sheet([['ID', 'UserID', '', '', '', 'Score'], ['P1', 'U1', '', '', '', 40], ['P2', 'student02', '', '', '', 90]])
  };
  const ss = { getSheetByName: name => sheets[name], insertSheet: name => (sheets[name] = sheet([])) };
  const context = vm.createContext({ Date: Clock,
    SpreadsheetApp: { openById: () => ss, flush() {} },
    LockService: { getScriptLock: () => ({ waitLock() {}, releaseLock() {} }) },
    Utilities: { formatDate: (date, _zone, pattern) => {
      const local = new Date(date.getTime() + 7 * 60 * 60 * 1000);
      const pad = n => String(n).padStart(2, '0');
      const day = pad(local.getUTCDate()) + '/' + pad(local.getUTCMonth() + 1) + '/' + local.getUTCFullYear();
      return pattern.includes('HH') ? day + ' ' + pad(local.getUTCHours()) + ':' + pad(local.getUTCMinutes()) : day;
    } }, Session: { getScriptTimeZone: () => 'Asia/Saigon' },
    ContentService: { MimeType: { JSON: 'json' }, createTextOutput: text => ({ setMimeType: () => JSON.parse(text) }) }
  });
  vm.runInContext(fs.readFileSync(new URL('../AppsScript/' + main, import.meta.url), 'utf8'), context);
  if (main !== 'Code.gs') vm.runInContext(fs.readFileSync(new URL('../AppsScript/PlanetMatchHistory.gs', import.meta.url), 'utf8'), context);
  const save = extra => context.doPost({ parameter: { action: 'savePlanetMatch', record: JSON.stringify({
    matchId: 'match-12345678', username: 'student01', opponent: 'Máy Planet', result: 'THẮNG', score: 360, maxScore: 450, questionType: 'multipleChoice', ...extra
  }) } });
  return { sheets, context, save, setNow: value => { current = Date.parse(value); }, rank: () => context.doGet({ parameter: { action: 'getProgressRank' } }) };
}

test('finished Planet match writes compatible history and raises the same user rank without duplicate names', () => {
  const { save, sheets, rank, context } = fixture();
  assert.equal(save().success, true); assert.equal(sheets.History.data.length, 3);
  const row = sheets.History.data[2];
  assert.deepEqual(row.slice(0, 5), ['student01', 'Máy Planet', 'Đối kháng', 'THẮNG', 360]);
  assert.equal(row[7], 80);
  const players = rank().players;
  assert.equal(players.length, 1); assert.equal(players[0].userID, 'student01'); assert.equal(players[0].score, 450);
  assert.equal(players[0].fullName, 'Học sinh Một');
  const history = context.doGet({ parameter: { action: 'history', username: 'student01' } });
  assert.equal(history.total, 2); assert.equal(history.histories[0].mode, 'Đối kháng');
});

for (const main of ['Code.gs', 'Ma-da-sua.gs']) test(`${main}: practice history round-trips subject/chapter while old rows remain blank`, () => {
  const { save, sheets, context } = fixture(main);
  assert.equal(save({ mode: 'Luyện tập', questionType: 'essay', maxScore: 50, score: 35, subject: 'Physics', chapter: 'Chương 1' }).success, true);
  const headers = sheets.History.data[0];
  assert.equal(headers[6], 'PlanetMatchID'); assert.equal(headers[7], 'PlanetRankScore');
  assert.equal(sheets.History.data[2][headers.indexOf('Subject')], 'Physics');
  const result = context.doGet({ parameter: { action: 'history', username: 'student01' } });
  assert.equal(result.histories[0].subject, 'Physics'); assert.equal(result.histories[0].chapter, 'Chương 1');
  assert.equal(result.histories[1].subject, ''); assert.equal(result.histories[1].chapter, '');
  assert.equal(save({ mode: 'Luyện tập', questionType: 'essay', maxScore: 50, score: 35, subject: 'Physics', chapter: 'Chương 1' }).duplicate, true);
  assert.equal(sheets.History.data.length, 3);
});

test('lesson metadata preserves other columns and follows named headers instead of fixed indexes', () => {
  const { save, sheets, context } = fixture();
  sheets.History.data[0].push('PlanetMatchID', 'PlanetRankScore', 'TeacherNote', 'Chapter', 'Subject');
  sheets.History.data[1][8] = 'Giữ nguyên';
  assert.equal(save({ mode: 'Luyện tập', questionType: 'multipleChoice', maxScore: 0, score: 25, subject: 'Chemistry', chapter: 'CH_CHM_01' }).success, true);
  assert.equal(sheets.History.data[1][8], 'Giữ nguyên');
  assert.equal(sheets.History.data[2][9], 'CH_CHM_01'); assert.equal(sheets.History.data[2][10], 'Chemistry');
  const result = context.doGet({ parameter: { action: 'history', username: 'student01' } });
  assert.equal(result.histories[0].subject, 'Chemistry'); assert.equal(result.histories[0].chapter, 'CH_CHM_01');
});

test('corrected user main script routes match saves through POST and restores essay GET', () => {
  const { save, rank, sheets, context } = fixture('Ma-da-sua.gs');
  assert.equal(save().success, true);
  assert.equal(save().duplicate, true);
  assert.equal(sheets.History.data.length, 3);
  assert.equal(rank().rankVersion, 'weekly-v2');
  context.getEssayQuestions = () => ({ success: true, questionType: 'essay', count: 260 });
  assert.equal(context.doGet({ parameter: { action: 'essayQuestions' } }).count, 260);
  const invalidGet = context.doGet({ parameter: { action: 'savePlanetMatch' } });
  assert.equal(invalidGet.success, false);
  assert.equal(sheets.History.data.length, 3);
  context.saveEssaySubmission = () => ({ success: true, submissionId: 'legacy-test' });
  assert.equal(context.doPost({ parameter: { action: 'saveEssaySubmission' } }).submissionId, 'legacy-test');
});

test('retries are idempotent per player, including both human seats in the same match', () => {
  const { save, sheets } = fixture();
  save(); assert.equal(save().duplicate, true); assert.equal(sheets.History.data.length, 3);
  assert.equal(save({ username: 'student02', result: 'THUA', score: 90 }).success, true);
  assert.equal(sheets.History.data.length, 4);
});

test('essay, draw and signed duel points add to the weekly total without normalization', () => {
  const { save, rank, sheets } = fixture();
  assert.equal(save({ questionType: 'essay', maxScore: 90, score: 81, result: 'HÒA' }).rankScore, 90);
  assert.equal(sheets.History.data[2][3], 'HÒA');
  assert.equal(save({ matchId: 'another-match', result: 'THUA', score: -30 }).rankScore, 0);
  assert.equal(rank().players.find(p => p.userID === 'student01').score, 141);
});

test('invalid records never append and named extra columns are preserved', () => {
  const { save, sheets } = fixture();
  for (const bad of [{ username: 'absent' }, { matchId: '' }, { score: 451 }, { maxScore: 150 }, { score: NaN }, { result: 'win' }])
    assert.equal(save(bad).success, false);
  assert.equal(sheets.History.data.length, 2);
  sheets.History.data[0][6] = 'ExistingData';
  assert.equal(save().success, true); assert.equal(sheets.History.data[0][6], 'ExistingData');
});

test('rank works from match history without a progress sheet and preserves old practice rows', () => {
  const { save, rank, sheets } = fixture();
  save(); delete sheets.TienDoHocTap;
  assert.equal(rank().players[0].score, 450); assert.equal(sheets.History.data[1][4], 90);
});

test('server exposes stable match identity and the actual maximum including speed bonus', () => {
  const arena = new Arena();
  const state = arena.join({ name: 'One', username: 'student01', difficulties: ['Dễ'], guns: [{ x: 0, y: 0 }, { x: 1, y: 1 }], targets: Array.from({ length: 4 }, (_, i) => ({ x: i, y: i })) });
  const p = arena.players.get(state.token); arena.playWithBot(p);
  assert.equal(arena.view(p).maxScore, 450);
  assert.equal(arena.view(p).matchId, p.match.id);
});

test('weekly boundary is Monday midnight Vietnam, even across a year change', () => {
  const { context } = fixture();
  const before = context.planetRankWeek_(Date.parse('2026-10-25T16:59:59.999Z'));
  assert.equal(new Date(before.start).toISOString(), '2026-10-18T17:00:00.000Z');
  assert.equal(new Date(before.end).toISOString(), '2026-10-25T17:00:00.000Z');
  const after = context.planetRankWeek_(Date.parse('2026-10-25T17:00:00.000Z'));
  assert.equal(after.start, before.end);
  const january = context.planetRankWeek_(Date.parse('2027-01-01T00:00:00Z'));
  assert.equal(new Date(january.start).toISOString(), '2026-12-27T17:00:00.000Z');
});

test('rank includes the week start, excludes previous week, future and undated rows, and keeps all history', () => {
  const { sheets, rank } = fixture();
  for (const [stamp, score] of [
    ['2026-10-18T16:59:59.999Z', 999], ['2026-10-18T17:00:00Z', 10],
    ['2026-10-25T17:00:00Z', 999], ['2026-10-24T05:00:01Z', 999], ['not-a-date', 999]
  ]) sheets.History.data.push(['U1', 'Máy AI', 'Luyện tập', 'THẮNG', score, stamp]);
  const length = sheets.History.data.length;
  const result = rank();
  assert.equal(result.players[0].score, 100);
  assert.equal(result.ignoredUndatedRows, 1);
  assert.equal(result.weekStartLabel, '19/10/2026'); assert.equal(result.weekEndLabel, '25/10/2026');
  assert.equal(sheets.History.data.length, length);
});

test('new week resets the ranking by filtering, without deleting historical results', () => {
  const { sheets, save, rank, setNow } = fixture();
  save(); assert.equal(rank().players[0].score, 450);
  const before = JSON.stringify(sheets.History.data);
  setNow('2026-10-25T17:00:00Z');
  assert.deepEqual(rank().players, []);
  assert.equal(JSON.stringify(sheets.History.data), before);
  save({ matchId: 'new-week-match', score: 30 });
  assert.equal(rank().players[0].score, 30);
});

test('duplicate match rows and ID aliases never inflate the weekly sum; progress is not added twice', () => {
  const { sheets, save, rank } = fixture();
  save(); const duplicate = [...sheets.History.data[2]]; duplicate[0] = 'U1';
  sheets.History.data.push(duplicate);
  sheets.TienDoHocTap.data.push(['Other', 'student01', '', '', '', 10000]);
  assert.equal(rank().players[0].score, 450); assert.equal(rank().players.length, 1);
});

test('completed essay and multiple-choice practice add once through the same persistent result API', () => {
  const { save, rank, sheets } = fixture();
  const essay = { mode: 'Luyện tập', matchId: 'practice-essay-1', questionType: 'essay', maxScore: 50, score: 40 };
  assert.equal(save(essay).success, true); assert.equal(save(essay).duplicate, true);
  assert.equal(save({ mode: 'Luyện tập', matchId: 'practice-quiz-1', maxScore: 0, score: 123 }).success, true);
  assert.equal(rank().players[0].score, 253);
  assert.equal(sheets.History.data[2][2], 'Luyện tập');
});

test('offline retry keeps the completion week instead of assigning points to the upload week', () => {
  const { save, rank, setNow, sheets } = fixture();
  setNow('2026-10-25T18:00:00Z');
  assert.equal(save({ finishedAt: '2026-10-25T16:59:00Z' }).success, true);
  assert.deepEqual(rank().players, []);
  assert.equal(new Date(sheets.History.data[2][5]).toISOString(), '2026-10-25T16:59:00.000Z');
  assert.equal(save({ matchId: 'future-match', finishedAt: '2026-10-26T18:00:00Z' }).success, false);
});

test('Sheet date values and explicit Vietnamese strings are parsed without US date ambiguity', () => {
  const { context } = fixture();
  assert.equal(context.planetRankTime_('01/02/2026 00:00'), Date.parse('2026-01-31T17:00:00Z'));
  assert.ok(Number.isNaN(context.planetRankTime_('31/02/2026 12:00')));
  assert.ok(Number.isNaN(context.planetRankTime_('24/10/2026 25:00')));
  assert.equal(context.planetRankTime_(new Date('2026-10-24T05:00:00Z')), Date.parse('2026-10-24T05:00:00Z'));
});

for (const main of ['Code.gs', 'Ma-da-sua.gs']) test(main + ': screenshot layout round-trips and ranks', () => {
  const { sheets, save, rank, context } = fixture(main);
  const headers = ['Username', 'Fullname', 'Difficulty', 'Opponent', 'Mode', 'Result', 'Score', 'Time', 'Duration', 'PlanetMatchID', 'PlanetRankScore', 'Subject', 'Chapter'];
  sheets.History.data[0] = headers;
  sheets.History.data[1] = ['student01', 'Tên cũ', 'Dễ', 'Máy AI', 'Luyện tập', 'THẮNG', 90, '24/10/2026 08:15', 120, '', '', 'Physics', 'Chương 1'];
  assert.equal(save().success, true); assert.equal(save().duplicate, true);
  assert.deepEqual(sheets.History.data[0], headers); assert.equal(sheets.History.data[1][8], 120);
  const row = sheets.History.data[2];
  assert.equal(row[3], 'Máy Planet'); assert.equal(row[6], 360); assert.equal(row[9], 'match-12345678');
  assert.equal(row[1], ''); assert.equal(row[2], ''); assert.equal(row[8], '');
  const h = context.doGet({ parameter: { action: 'history', username: 'student01' } });
  assert.equal(h.total, 2); assert.equal(h.histories[0].score, 360);
  assert.equal(h.histories[1].result, 'THẮNG'); assert.equal(h.histories[1].time, '24/10/2026 08:15');
  assert.equal(rank().players[0].score, 450);
});
