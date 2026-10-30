import { randomBytes } from 'node:crypto';

const normalize = value => String(value || '').trim().toLowerCase();
// Uses the same Users sheet as the existing Unity sign-in; no offline credential fallback.
const defaultUsersUrl = 'https://docs.google.com/spreadsheets/d/1DcJwlN_fUDdcaQ6IfnlkMJApgWuEUqMhyZv9Kn8vfWo/export?format=csv';
export function parseUsersCSV(csv) {
  const rows = []; let row = [], field = '', quoted = false;
  for (let i = 0; i < csv.length; i++) {
    const c = csv[i];
    if (c === '"') { if (quoted && csv[i + 1] === '"') { field += '"'; i++; } else quoted = !quoted; }
    else if (!quoted && (c === ',' || c === '\n')) { row.push(field); field = ''; if (c === '\n') { rows.push(row); row = []; } }
    else if (c !== '\r' || quoted) field += c;
  }
  if (quoted) throw new Error('Danh sách tài khoản chưa hợp lệ.');
  if (field || row.length) { row.push(field); rows.push(row); }
  const headers = (rows.shift() || []).map(normalize);
  const username = headers.indexOf('username'), password = headers.indexOf('password'), role = headers.indexOf('role');
  if ([username, password, role].some(i => i < 0)) throw new Error('Tab Users cần các cột Username, Password, Role để xác thực nhật ký.');
  return rows.map(r => ({ username: (r[username] || '').trim(), password: (r[password] || '').trim(), role: normalize(r[role]) }));
}

export class TutorAuth {
  constructor({ usersUrl = process.env.TUTOR_USERS_URL || defaultUsersUrl, fetchImpl = fetch, now = Date.now, authenticate } = {}) {
    this.now = now; this.tokens = new Map(); this.rates = new Map();
    this.authenticate = authenticate || (async (username, password) => {
      const response = await fetchImpl(usersUrl, { signal: AbortSignal.timeout(20000) });
      if (!response.ok) throw new Error('Chưa xác minh được tài khoản. Hãy thử lại.');
      const users = parseUsersCSV(await response.text());
      const user = users.find(u => normalize(u.username) === normalize(username) && u.password === password);
      if (!user || !['student', 'học sinh', 'hocsinh', 'teacher', 'giáo viên', 'giaovien'].includes(user.role)) throw new Error('Tài khoản hoặc mật khẩu không đúng.');
      return { username: user.username, teacher: ['teacher', 'giáo viên', 'giaovien'].includes(user.role) };
    });
  }
  async login(body, client) {
    const now = this.now();
    for (const [key, rate] of this.rates) if (now - rate.since >= 60000) this.rates.delete(key);
    for (const [key, session] of this.tokens) if (now >= session.expires) this.tokens.delete(key);
    const rate = this.rates.get(client) || { since: now, count: 0 };
    if (rate.count >= 10) throw new Error('Đăng nhập quá nhiều lần. Hãy chờ một phút.');
    rate.count++; this.rates.set(client, rate);
    if (typeof body.username !== 'string' || !body.username.trim() || body.username.length > 100 || typeof body.password !== 'string' || !body.password || body.password.length > 200) throw new Error('Hãy nhập tài khoản và mật khẩu.');
    const identity = await this.authenticate(body.username, body.password);
    const token = randomBytes(32).toString('hex');
    this.tokens.set(token, { username: identity.username, teacher: identity.teacher === true, expires: this.now() + 8 * 60 * 60 * 1000 });
    return { token, username: identity.username, teacher: identity.teacher === true };
  }
  authorize(header, body, action) {
    const session = typeof header === 'string' && header.startsWith('Bearer ') ? this.tokens.get(header.slice(7)) : null;
    if (!session || this.now() >= session.expires) { const error = new Error('Đăng nhập để mở nhật ký học lại.'); error.status = 401; throw error; }
    const read = action === '/tutor/list' || action === '/tutor/state';
    if ((session.teacher && !read) || (!session.teacher && normalize(session.username) !== normalize(body.username))) {
      const error = new Error('Bạn không có quyền thao tác nhật ký học lại này.'); error.status = 403; throw error;
    }
    return session;
  }
}
