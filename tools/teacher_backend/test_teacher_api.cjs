/* Offline contract checks. Only built-in Node modules; Google services are in-memory mocks. */
'use strict';

const assert = require('node:assert/strict');
const crypto = require('node:crypto');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

const sourcePath = path.join(__dirname, 'TeacherApi.gs');
const reportPath = path.join(__dirname, 'test_teacher_api_report.txt');
const source = fs.readFileSync(sourcePath, 'utf8');
const workbookId = '1DcJwlN_fUDdcaQ6IfnlkMJApgWuEUqMhyZv9Kn8vfWo';
const clone = value => JSON.parse(JSON.stringify(value));
const tests = [];

function fixture() {
    const writes = [];
    const cacheEntries = new Map();
    let now = 0;
    let acquired = 0;
    let released = 0;

    class Sheet {
        constructor(name, rows) { this.name = name; this.rows = clone(rows); }
        getLastRow() { return this.rows.length; }
        getLastColumn() { return this.rows[0].length; }
        getDataRange() { return this.getRange(1, 1, this.getLastRow(), this.getLastColumn()); }
        getRange(row, column, height, width) {
            assert.ok(row >= 1 && column >= 1 && height > 0 && width > 0, 'Valid 1-based Sheet range');
            return {
                getDisplayValues: () => Array.from({ length: height }, (_, y) =>
                    Array.from({ length: width }, (_, x) => String(this.rows[row - 1 + y]?.[column - 1 + x] ?? ''))),
                setValues: values => {
                    assert.equal(values.length, height);
                    values.forEach(value => assert.equal(value.length, width));
                    writes.push({ sheet: this.name, row, column, values: clone(values) });
                    values.forEach((value, y) => {
                        this.rows[row - 1 + y] ||= [];
                        value.forEach((cell, x) => { this.rows[row - 1 + y][column - 1 + x] = cell; });
                    });
                }
            };
        }
        lastRecord() {
            return Object.fromEntries(this.rows[0].map((header, index) => [header, this.rows.at(-1)[index]]));
        }
    }

    // Deliberately shuffle question/essay columns: writes must map headers, not assume positions.
    const sheets = new Map([
        ['Accounts', new Sheet('Accounts', [
            ['ID', 'Username', 'Password', 'FullName', 'Role', 'Class'],
            ['GV_TEST', 'teacher_test', 'offline-password', 'Giáo viên giả lập', 'Teacher', ''],
            ['GV_ALIAS', 'teacher_alias', 'offline-password', 'Giáo viên giả lập 2', 'GiaoVien', ''],
            ['HS_TEST', 'student_test', 'offline-password', 'Học sinh giả lập', 'Student', '6A'],
            ['OTHER_TEST', 'other_test', 'offline-password', 'Khác', 'Admin', '']
        ])],
        ['Questions', new Sheet('Questions', [[
            'explanation', 'points', 'option_d', 'id', 'monID', 'Chương / Chủ đề',
            'difficulty', 'question', 'option_a', 'option_b', 'option_c', 'correct_option'
        ]])],
        ['TuLuan', new Sheet('TuLuan', [[
            'NgayTao', 'NguonTao', 'GiaoVienID', 'TrangThai', 'DiemToiDa', 'HuongDanChamAI',
            'DapAnThamKhao', 'HinhAnh', 'Question', 'difficulty', 'Chương/Chủ đề', 'monID', 'CauHoiID'
        ]])],
        ['TienDoHocTap', new Sheet('TienDoHocTap', [['TienDoID', 'UserID'], ['TD_TEST', 'HS_TEST']])],
        ['Leaderboard', new Sheet('Leaderboard', [['UserID', 'Score'], ['HS_TEST', '100']])]
    ]);
    const untouched = Object.fromEntries(['Accounts', 'TienDoHocTap', 'Leaderboard'].map(name => [name, clone(sheets.get(name).rows)]));
    const cache = {
        get(key) {
            const entry = cacheEntries.get(key);
            if (!entry || entry.until <= now) { cacheEntries.delete(key); return null; }
            return entry.value;
        },
        put(key, value, seconds) { cacheEntries.set(key, { value, until: now + seconds }); },
        remove(key) { cacheEntries.delete(key); }
    };
    const context = vm.createContext({
        SpreadsheetApp: {
            openById(id) {
                assert.equal(id, workbookId, 'Only the configured workbook is used');
                return { getSheets: () => Array.from(sheets.values()), getSheetByName: name => sheets.get(name) };
            },
            flush() {}
        },
        CacheService: { getScriptCache: () => cache },
        LockService: { getScriptLock: () => ({
            tryLock() { acquired++; return true; },
            releaseLock() { released++; }
        }) },
        Utilities: {
            DigestAlgorithm: { SHA_256: 'SHA_256' },
            Charset: { UTF_8: 'UTF_8' },
            getUuid: () => crypto.randomUUID(),
            computeDigest: (_algorithm, value) => Array.from(crypto.createHash('sha256').update(String(value), 'utf8').digest()),
            base64EncodeWebSafe: bytes => Buffer.from(bytes).toString('base64url'),
            base64Decode: value => Array.from(Buffer.from(value, 'base64')),
            formatDate: () => '2026-10-24 09:00:00'
        },
        ContentService: {
            MimeType: { JSON: 'application/json' },
            createTextOutput(text) { return { text, setMimeType() { return this; } }; }
        }
    });
    vm.runInContext(source, context, { filename: 'TeacherApi.gs', timeout: 1000 });
    const post = payload => JSON.parse(context.doPost({ postData: { contents: JSON.stringify(payload) } }).text);
    return {
        context, sheets, writes, post,
        authenticate(username = 'teacher_test', password = 'offline-password') {
            const reply = post({ action: 'teacherLogin', username, password });
            assert.equal(reply.success, true, reply.message);
            assert.ok(reply.token);
            return reply.token;
        },
        advance(seconds) { now += seconds; },
        verifyIsolation() {
            for (const [name, rows] of Object.entries(untouched)) assert.deepEqual(sheets.get(name).rows, rows, name + ' must stay unchanged');
            assert.ok(writes.every(write => ['Questions', 'TuLuan'].includes(write.sheet)), 'Only the two question tabs may be written');
            assert.equal(released, acquired, 'Every acquired write lock must be released');
        }
    };
}

function quiz(overrides = {}) {
    return {
        id: 'Q_OFFLINE_TEST', monID: 'Vật lý', chapter: 'Chương 1: Mở đầu và Các phép đo', difficulty: 'Dễ',
        points: '10', question: 'Đơn vị đo chiều dài SI là gì?', option_a: 'cm', option_b: 'm',
        option_c: 'km', option_d: 'mm', correct_option: 'B', explanation: 'Đơn vị cơ bản là mét.', ...overrides
    };
}

function essay(overrides = {}) {
    return {
        CauHoiID: 'TL_OFFLINE_TEST', monID: 'Sinh học', chapter: 'Tế bào', difficulty: 'Trung bình',
        Question: 'Nêu đặc điểm của tế bào.', HinhAnh: 'https://example.invalid/illustration.png',
        DapAnThamKhao: 'Màng, chất tế bào và vật chất di truyền.', HuongDanChamAI: 'Chấm từng ý chính.',
        DiemToiDa: '2,5', TrangThai: 'Đã duyệt', GiaoVienID: 'FORGED_CLIENT_ID',
        NguonTao: 'Giáo viên biên soạn', NgayTao: '2026-10-24 08:30:00', ...overrides
    };
}

function rejected(reply) {
    assert.equal(reply.success, false);
    assert.equal(reply.saved, false);
    assert.ok(reply.message);
}

function test(name, run) { tests.push({ name, run }); }

test('Teacher login requires username, password, matching credentials and teacher role', env => {
    for (const payload of [
        {}, { username: 'teacher_test' }, { password: 'offline-password' },
        { username: 'teacher_test', password: 'wrong' },
        { username: 'student_test', password: 'offline-password' },
        { username: 'other_test', password: 'offline-password' }
    ]) rejected(env.post({ action: 'teacherLogin', ...payload }));
    env.authenticate(' TEACHER_TEST ');
    env.authenticate('teacher_alias');
    assert.equal(env.writes.length, 0);
});

test('Missing, forged and expired session tokens cannot write either question tab', env => {
    for (const token of [undefined, '', 'active_session', 123]) {
        rejected(env.post({ action: 'saveQuestion', token, question: quiz() }));
        rejected(env.post({ action: 'saveEssay', token, essay: essay() }));
    }
    const token = env.authenticate();
    env.advance(3601);
    rejected(env.post({ action: 'saveQuestion', token, question: quiz() }));
    assert.equal(env.writes.length, 0);
});

test('Quiz maps all 12 headers correctly, including answer B and numeric points', env => {
    const q = quiz();
    const reply = env.post({ action: 'saveQuestion', token: env.authenticate(), question: q });
    assert.equal(reply.success, true); assert.equal(reply.saved, true);
    assert.equal(reply.id, q.id); assert.equal(reply.sheet, 'Questions');
    assert.deepEqual(env.sheets.get('Questions').lastRecord(), {
        id: q.id, monID: q.monID, 'Chương / Chủ đề': q.chapter, difficulty: q.difficulty,
        question: q.question, option_a: q.option_a, option_b: q.option_b, option_c: q.option_c,
        option_d: q.option_d, correct_option: 'B', explanation: q.explanation, points: 10
    });
    assert.equal(env.sheets.get('Questions').getLastColumn(), 12);
    assert.equal(env.writes.length, 1);
});

test('Essay maps all 13 headers, uses server teacher ID and supports decimal score', env => {
    const q = essay();
    const reply = env.post({ action: 'saveEssay', token: env.authenticate(), essay: q });
    assert.equal(reply.success, true); assert.equal(reply.saved, true);
    assert.equal(reply.id, q.CauHoiID); assert.equal(reply.sheet, 'TuLuan');
    assert.deepEqual(env.sheets.get('TuLuan').lastRecord(), {
        CauHoiID: q.CauHoiID, monID: q.monID, 'Chương/Chủ đề': q.chapter, difficulty: q.difficulty,
        Question: q.Question, HinhAnh: q.HinhAnh, DapAnThamKhao: q.DapAnThamKhao,
        HuongDanChamAI: q.HuongDanChamAI, DiemToiDa: 2.5, TrangThai: q.TrangThai,
        GiaoVienID: 'GV_TEST', NguonTao: q.NguonTao, NgayTao: q.NgayTao
    });
    assert.equal(env.sheets.get('TuLuan').getLastColumn(), 13);
});

test('An identical quiz retry is idempotent', env => {
    const payload = { action: 'saveQuestion', token: env.authenticate(), question: quiz() };
    const first = env.post(payload), second = env.post(payload);
    assert.equal(first.saved, true); assert.equal(second.saved, true); assert.equal(second.id, first.id);
    assert.equal(env.sheets.get('Questions').getLastRow(), 2);
    assert.equal(env.writes.length, 1);
});

test('An identical essay retry is idempotent', env => {
    const payload = { action: 'saveEssay', token: env.authenticate(), essay: essay() };
    assert.equal(env.post(payload).saved, true); assert.equal(env.post(payload).saved, true);
    assert.equal(env.sheets.get('TuLuan').getLastRow(), 2);
    assert.equal(env.writes.length, 1);
});

test('Changed payload under an existing ID is rejected without modifying the original', env => {
    const token = env.authenticate();
    assert.equal(env.post({ action: 'saveQuestion', token, question: quiz() }).saved, true);
    assert.equal(env.post({ action: 'saveEssay', token, essay: essay() }).saved, true);
    const before = clone(env.writes);
    rejected(env.post({ action: 'saveQuestion', token, question: quiz({ option_b: 'changed' }) }));
    rejected(env.post({ action: 'saveEssay', token, essay: essay({ Question: 'changed' }) }));
    assert.deepEqual(env.writes, before);
});

test('Invalid correct-answer values are rejected; lowercase answer is normalized', env => {
    const token = env.authenticate();
    for (const correct_option of ['', 'E', 'AB', 'option_b', '1'])
        rejected(env.post({ action: 'saveQuestion', token, question: quiz({ correct_option }) }));
    assert.equal(env.writes.length, 0);
    assert.equal(env.post({ action: 'saveQuestion', token, question: quiz({ correct_option: ' b ' }) }).saved, true);
    assert.equal(env.sheets.get('Questions').lastRecord().correct_option, 'B');
});

test('Zero, negative, nonnumeric and nonfinite scores are rejected for both types', env => {
    const token = env.authenticate();
    for (const score of ['0', '-1', 'abc', 'NaN', 'Infinity', '']) {
        rejected(env.post({ action: 'saveQuestion', token, question: quiz({ points: score }) }));
        rejected(env.post({ action: 'saveEssay', token, essay: essay({ DiemToiDa: score }) }));
    }
    assert.equal(env.writes.length, 0);
});

test('Missing required quiz or essay content cannot create partial rows', env => {
    const token = env.authenticate();
    for (const field of ['id', 'monID', 'chapter', 'question', 'option_a', 'option_b', 'option_c', 'option_d']) {
        const q = quiz(); delete q[field];
        rejected(env.post({ action: 'saveQuestion', token, question: q }));
    }
    for (const field of ['CauHoiID', 'Question', 'DapAnThamKhao', 'HuongDanChamAI', 'TrangThai', 'NguonTao']) {
        const q = essay(); delete q[field];
        rejected(env.post({ action: 'saveEssay', token, essay: q }));
    }
    assert.equal(env.writes.length, 0);
});

test('Caller-supplied sheet names cannot redirect writes; unrelated actions are rejected', env => {
    const token = env.authenticate();
    assert.equal(env.post({ action: 'saveQuestion', token, sheet: 'Accounts', question: quiz() }).sheet, 'Questions');
    assert.equal(env.post({ action: 'saveEssay', token, sheet: 'TienDoHocTap', essay: essay() }).sheet, 'TuLuan');
    rejected(env.post({ action: 'saveLeaderboard', token, sheet: 'Leaderboard', score: 9999 }));
    assert.deepEqual(env.writes.map(write => write.sheet), ['Questions', 'TuLuan']);
});

test('Missing Sheet columns fail before any data mutation', env => {
    const questions = env.sheets.get('Questions');
    questions.rows[0] = questions.rows[0].filter(header => header !== 'option_b');
    rejected(env.post({ action: 'saveQuestion', token: env.authenticate(), question: quiz() }));
    assert.equal(env.writes.length, 0);
});

test('Malformed JSON is rejected and formula-like question text remains literal', env => {
    rejected(JSON.parse(env.context.doPost({ postData: { contents: 'not-json' } }).text));
    assert.equal(env.post({ action: 'saveQuestion', token: env.authenticate(), question: quiz({ question: '=1+1' }) }).saved, true);
    assert.equal(env.sheets.get('Questions').lastRecord().question, "'=1+1");
});

const lines = [
    'Teacher API offline contract tests',
    'Run UTC: ' + new Date().toISOString(),
    'Source SHA256: ' + crypto.createHash('sha256').update(source).digest('hex'),
    'Environment: Node ' + process.version + '; in-memory Google service mocks; no network or remote writes.', ''
];
let failed = 0;
for (const { name, run } of tests) {
    const env = fixture();
    try {
        run(env);
        env.verifyIsolation();
        lines.push('PASS ' + name);
    } catch (error) {
        failed++;
        lines.push('FAIL ' + name + '\n' + error.stack);
    }
}
lines.push('', `${tests.length - failed}/${tests.length} passed; ${failed} failed.`,
    'Limits: checks source behavior with mocks; does not confirm deployment, real credentials, Google permissions or Drive image upload.');
fs.writeFileSync(reportPath, lines.join('\n') + '\n', 'utf8');
console.log(lines.join('\n'));
process.exitCode = failed ? 1 : 0;
