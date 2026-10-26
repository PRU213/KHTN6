var SPREADSHEET_ID = "1DcJwlN_fUDdcaQ6IfnlkMJApgWuEUqMhyZv9Kn8vfWo";

// =======================================================
// MAIN GET API
// =======================================================

function doGet(e) {
  try {
    var params = (e && e.parameter) ? e.parameter : {};
    var action = params.action || "";

    if (action === "essayQuestions") return getEssayQuestions(e);
    if (action === "practiceList") return getPracticeLessons(e);
    if (action === "practiceDetail") return getPracticeLesson(e);

    // 1. QUESTIONS
    if (action === "questions") {
      return getQuestions(e);
    }

    // 2. LEADERBOARD
    if (action === "getLeaderboard") {
      return getLeaderboardAPI();
    }

    // 3. PROGRESS RANK
    if (action === "getProgressRank") {
      return planetGetProgressRank_();
    }

    // 4. HISTORY - MỚI
    if (action === "history" || action === "getHistory") {
      return getHistory(e);
    }

    // 5. THEORY - API CŨ
    // Giữ nguyên cách gọi chapterId
    return getTheory(e);

  } catch (error) {
    return jsonOut({
      success: false,
      message: error.toString()
    });
  }
}

// =======================================================
// QUESTIONS API
// =======================================================

function getQuestions(e) {
  var spreadsheet = SpreadsheetApp.openById(SPREADSHEET_ID);
  var sheet = spreadsheet.getSheetByName("Questions");

  if (!sheet) {
    return jsonOut({
      success: false,
      message: "Sheet 'Questions' not found"
    });
  }

  var rows = sheet.getDataRange().getValues();

  var subject = (
    e.parameter.subject ||
    e.parameter.mon ||
    ""
  ).toString().trim();

  var difficultyString = (
    e.parameter.difficulty || ""
  ).toString().trim();

  var selectedDifficulties = [];

  if (difficultyString !== "") {
    selectedDifficulties = difficultyString
      .split(",")
      .map(function(item) {
        return item.toString().trim();
      })
      .filter(function(item) {
        return item !== "";
      });
  }

  var list = [];

  for (var i = 1; i < rows.length; i++) {
    var r = rows[i];

    if (!r[0]) continue;

    var id = String(r[0]).trim();
    var monId = String(r[1]).trim();
    var topic = String(r[2]).trim();
    var difficulty = String(r[3]).trim();
    var question = String(r[4]).trim();
    var optionA = String(r[5]).trim();
    var optionB = String(r[6]).trim();
    var optionC = String(r[7]).trim();
    var optionD = String(r[8]).trim();
    var correct = String(r[9]).trim();
    var explanation = String(r[10]).trim();
    var points = Number(r[11]) || 10;

    if (subject !== "" && monId !== subject) {
      continue;
    }

    if (
      selectedDifficulties.length > 0 &&
      selectedDifficulties.indexOf(difficulty) === -1
    ) {
      continue;
    }

    list.push({
      id: id,
      monId: monId,
      topic: topic,
      difficulty: difficulty,
      question: question,
      optionA: optionA,
      optionB: optionB,
      optionC: optionC,
      optionD: optionD,
      correct: correct,
      explanation: explanation,
      points: points
    });
  }

  return jsonOut({
success: true,
    count: list.length,
    subject: subject,
    difficulties: selectedDifficulties,
    questions: list
  });
}

// =======================================================
// THEORY API
// =======================================================

function getTheory(e) {
  var spreadsheet = SpreadsheetApp.openById(SPREADSHEET_ID);
  var sheet = spreadsheet.getSheetByName("Theory");

  if (!sheet) {
    return jsonOut({
      success: false,
      message: "Sheet 'Theory' not found"
    });
  }

  var chapterId = e.parameter.chapterId;

  if (!chapterId) {
    return jsonOut({
      success: false,
      message: "Missing chapterId"
    });
  }

  chapterId = chapterId.toString().trim();

  var data = sheet.getDataRange().getValues();

  for (var i = 1; i < data.length; i++) {
    var currentId = String(data[i][0]).trim();

    if (currentId === chapterId) {
      return jsonOut({
        success: true,
        chapterId: currentId,
        monId: String(data[i][1]),
        chapterOrder: data[i][2],
        chapterTitle: String(data[i][3]),
        summary: String(data[i][4]),
        keyPoints: String(data[i][5]),
        formulaNote: String(data[i][6]),
        iconImage: String(data[i][7])
      });
    }
  }

  return jsonOut({
    success: false,
    message: "Chapter not found",
    searchedId: chapterId,
    sheetName: sheet.getName(),
    totalRows: data.length
  });
}

// =======================================================
// GET LEADERBOARD API
// =======================================================

function getLeaderboardAPI() {
  var sheet = SpreadsheetApp
    .openById(SPREADSHEET_ID)
    .getSheetByName("LeaderBoard");

  if (!sheet) {
    return jsonOut([]);
  }

  var data = sheet.getDataRange().getValues();
  var records = [];

  for (var i = 1; i < data.length; i++) {
    var row = data[i];

    if (!row[0]) continue;

    records.push({
      username: String(row[0]),
      fullname: String(row[1]),
      subject: String(row[2]),
      chapter: String(row[3]),
      difficulty: String(row[4]),
      time: String(row[5])
    });
  }

  return jsonOut(records);
}

// =======================================================
// GET PROGRESS RANK API
// =======================================================

function getProgressRank() {
  var spreadsheet = SpreadsheetApp.openById(SPREADSHEET_ID);
  var sheet = spreadsheet.getSheetByName("TienDoHocTap");

  if (!sheet) {
    return jsonOut({
      success: false,
      message: "Sheet 'TienDoHocTap' not found"
    });
  }

  var data = sheet.getDataRange().getValues();
  var playerScores = {};

  for (var i = 1; i < data.length; i++) {
    var row = data[i];

    var userID = String(row[1] || "").trim();
    var score = Number(row[5]);

    if (!userID) continue;

    if (isNaN(score)) {
      score = 0;
    }

    if (
      playerScores[userID] === undefined ||
      score > playerScores[userID]
    ) {
playerScores[userID] = score;
    }
  }

  var leaderboard = [];

  for (var userID in playerScores) {
    leaderboard.push({
      userID: userID,
      score: playerScores[userID]
    });
  }

  leaderboard.sort(function(a, b) {
    return b.score - a.score;
  });

  for (var j = 0; j < leaderboard.length; j++) {
    leaderboard[j].rank = j + 1;
  }

  return jsonOut({
    success: true,
    players: leaderboard
  });
}

// =======================================================
// HISTORY API - MỚI
// Lấy lịch sử theo Username
// =======================================================

function getHistory(e) {
  var username = String(
    e.parameter.username || ""
  ).trim();

  if (!username) {
    return jsonOut({
      success: false,
      message: "Missing username"
    });
  }

  var ss = SpreadsheetApp.openById(SPREADSHEET_ID);

  var historySheet = ss.getSheetByName("History");
  var userSheet = ss.getSheetByName("Users");

  if (!historySheet) {
    return jsonOut({
      success: false,
      message: "Sheet 'History' not found"
    });
  }

  if (!userSheet) {
    return jsonOut({
      success: false,
      message: "Sheet 'Users' not found"
    });
  }

  var historyData = historySheet.getDataRange().getValues();
  var subjectColumn = historyData[0].indexOf('Subject');
  var chapterColumn = historyData[0].indexOf('Chapter');
  var userData = userSheet.getDataRange().getValues();

  // -----------------------------------
  // LẤY TÊN HỌC SINH
  // Users:
  // B = Username
  // D = FullName
  // -----------------------------------

  var fullName = username;

  for (var i = 1; i < userData.length; i++) {
    var currentUsername = String(userData[i][1]).trim();

    if (currentUsername === username) {
      fullName = String(userData[i][3]);
      break;
    }
  }

  // -----------------------------------
  // ĐỌC LỊCH SỬ
  // A = Username
  // B = Opponent
  // C = Mode
  // D = Result
  // E = Score
  // F = Time
  // -----------------------------------

  var histories = [];
  var win = 0;
  var lose = 0;

  for (var j = 1; j < historyData.length; j++) {
    var row = historyData[j];

    var rowUsername = String(row[0]).trim();

    if (rowUsername !== username) {
      continue;
    }

    var result = String(row[3]).trim().toUpperCase();

    if (result === "THẮNG") {
      win++;
    } else if (result === "THUA") {
      lose++;
    }

    var timeValue = row[5];

    // Định dạng ngày tháng
    if (timeValue instanceof Date) {
      timeValue = Utilities.formatDate(
        timeValue,
        Session.getScriptTimeZone(),
        "dd/MM/yyyy HH:mm"
      );
    } else {
      timeValue = String(timeValue);
    }

    histories.push({
      opponent: String(row[1]),
      mode: String(row[2]),
      result: result,
      score: Number(row[4]) || 0,
      time: timeValue,
      subject: subjectColumn >= 0 ? String(row[subjectColumn] || '') : '',
      chapter: chapterColumn >= 0 ? String(row[chapterColumn] || '') : ''
    });
  }

  // -----------------------------------
  // ĐƯA TRẬN MỚI NHẤT LÊN ĐẦU
  // Dựa trên thứ tự ghi dữ liệu trong Sheet
  // -----------------------------------

  histories.reverse();

  var total = histories.length;
var winRate = total > 0
    ? Math.round((win / total) * 100)
    : 0;

  return jsonOut({
    success: true,
    username: username,
    fullName: fullName,
    total: total,
    win: win,
    lose: lose,
    winRate: winRate,
    histories: histories
  });
}

// =======================================================
// POST API
// Lưu điểm và lịch sử
// =======================================================


function doPost(e) {
  try {
    var p = (e && e.parameter) ? e.parameter : {};
    var action = p.action || "";

    // -----------------------------------
    // API LƯU LEADERBOARD CŨ
    // -----------------------------------
    if (action === "savePlanetMatch") return planetSaveMatch_(p);

    if (action === "saveEssaySubmission") return saveEssaySubmission(p);

    if (action === "saveLeaderboard") {
      var sheetLB = SpreadsheetApp
        .openById(SPREADSHEET_ID)
        .getSheetByName("LeaderBoard");

      if (!sheetLB) {
        return ContentService.createTextOutput(
          "Lỗi: Không tìm thấy sheet LeaderBoard"
        );
      }

      var username = p.Username || "Unknown";
      var fullname = p.Fullname || "Unknown";
      var subject = p.Subject || "";
      var chapter = p.Chapter || "";
      var difficulty = p.Difficulty || "";
      var time = p.Time || "00:00";

      sheetLB.appendRow([
        username,
        fullname,
        subject,
        chapter,
        difficulty,
        time
      ]);

      return ContentService.createTextOutput("Thành công");
    }

    // -----------------------------------
    // API LƯU HISTORY
    // -----------------------------------
    if (action === "saveHistory") {
      return saveHistory(p);
    }

    // -----------------------------------
    // API UPLOAD ẢNH BÀI TỰ LUẬN
    // -----------------------------------
    if (action === "saveEssayImage") {

      if (!p.base64 || !p.mimeType || !p.submissionId) {
        return jsonOut({
          success: false,
          message: "Thiếu dữ liệu upload ảnh"
        });
      }

      var result = saveEssayImage(
        p.base64,
        p.mimeType,
        p.submissionId
      );

      return jsonOut(result);
    }

    // -----------------------------------
    // ACTION KHÔNG HỢP LỆ
    // -----------------------------------
    return jsonOut({
      success: false,
      message: "Invalid POST action"
    });

  } catch (error) {
    return jsonOut({
      success: false,
      message: error.toString()
    });
  }
}


// =======================================================
// SAVE HISTORY - MỚI
// =======================================================

function saveHistory(p) {
  var ss = SpreadsheetApp.openById(SPREADSHEET_ID);
  var sheet = ss.getSheetByName("History");

  if (!sheet) {
    return jsonOut({
      success: false,
      message: "Sheet 'History' not found"
    });
  }

  var username = String(
    p.username || p.Username || ""
  ).trim();

  var opponent = String(
    p.opponent || p.Opponent || "Máy AI"
  ).trim();

  var mode = String(
    p.mode || p.Mode || "Luyện tập"
  ).trim();

  var result = String(
    p.result || p.Result || ""
  ).trim().toUpperCase();

  var scoreText = String(
    p.score !== undefined ? p.score :
    p.Score !== undefined ? p.Score : ""
  ).trim();

  var score = Number(scoreText);

  if (!username) {
    return jsonOut({
      success: false,
      message: "Missing username"
    });
  }

  if (result !== "THẮNG" && result !== "THUA") {
    return jsonOut({
      success: false,
      message: "Result must be THẮNG or THUA"
    });
  }

  if (scoreText === "" || !isFinite(score)) {
    return jsonOut({
      success: false,
message: "Invalid score"
    });
  }

  // Tự tạo thời gian từ server
  // Không cần Unity gửi thời gian
  var time = new Date();

  var lock = LockService.getScriptLock();
  lock.waitLock(10000);

  try {
    sheet.appendRow([
      username,
      opponent,
      mode,
      result,
      score,
      time
    ]);

    SpreadsheetApp.flush();
  } finally {
    lock.releaseLock();
  }

  return jsonOut({
    success: true,
    message: "History saved successfully",
    username: username,
    opponent: opponent,
    mode: mode,
    result: result,
    score: score,
    time: Utilities.formatDate(
      time,
      Session.getScriptTimeZone(),
      "dd/MM/yyyy HH:mm"
    )
  });
}

// =======================================================
// HÀM GET LEADERBOARD TỪ TIẾN ĐỘ - GIỮ CODE CŨ
// =======================================================

function getLeaderboard() {
  var sheet = SpreadsheetApp
    .openById(SPREADSHEET_ID)
    .getSheetByName("TienDoHocTap");

  var data = sheet.getDataRange().getValues();
  var playerScores = {};

  for (var i = 1; i < data.length; i++) {
    var userID = data[i][1];
    var score = Number(data[i][5]);

    if (!userID) continue;

    if (
      playerScores[userID] == null ||
      score > playerScores[userID]
    ) {
      playerScores[userID] = score;
    }
  }

  var leaderboard = [];

  for (var userID in playerScores) {
    leaderboard.push({
      userID: userID,
      score: playerScores[userID]
    });
  }

  leaderboard.sort(function(a, b) {
    return b.score - a.score;
  });

  return leaderboard;
}

// =======================================================
// JSON OUTPUT
// =======================================================

function jsonOut(obj) {
  // Chỉ bổ sung tên cho API bảng xếp hạng (có mảng players)
  if (obj && Array.isArray(obj.players)) {
    const nameMap = getUserNameMap_();
    obj.players.forEach(p => {
      p.fullName = p.fullName || nameMap[String(p.userID).trim()] || p.userID;
    });
  }

  return ContentService
    .createTextOutput(JSON.stringify(obj))
    .setMimeType(ContentService.MimeType.JSON);
}
function getUserNameMap_() {
  const sheet = SpreadsheetApp.openById(SPREADSHEET_ID).getSheetByName('Users');
  const data = sheet.getDataRange().getValues();
  const map = {};
  for (let i = 1; i < data.length; i++) {      // bỏ dòng tiêu đề
    map[String(data[i][0]).trim()] = data[i][3]; // A = ID, D = FullName
  }
  return map;
}
// These helpers are appended to the original Apps Script in AppsScript/Code.gs.
function essayNormalize_(value) {
  return String(value || '').normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/[đĐ]/g, 'd').toLowerCase().trim();
}
function getEssayQuestions(e) {
  var params = (e && e.parameter) || {};
  var secret = PropertiesService.getScriptProperties().getProperty('PLANET_SERVER_SECRET');
  if (secret && params.serverSecret !== secret) return jsonOut({ success: false, message: 'Unauthorized essay access' });
  var sheet = SpreadsheetApp.openById(SPREADSHEET_ID).getSheetByName('TuLuan');
  if (!sheet) return jsonOut({ success: false, message: "Không tìm thấy tab TuLuan" });
  var rows = sheet.getDataRange().getValues();
  if (!rows.length) return jsonOut({ success: false, message: 'TuLuan chưa có hàng tiêu đề' });
  var columns = {};
  rows[0].forEach(function(name, index) { columns[essayNormalize_(name)] = index; });
  var required = ['CauHoiID', 'monID', 'difficulty', 'Question', 'DapAnThamKhao', 'HuongDanChamAI', 'DiemToiDa', 'TrangThai'];
  for (var k = 0; k < required.length; k++) {
    if (columns[essayNormalize_(required[k])] === undefined) return jsonOut({ success: false, message: 'TuLuan thiếu cột ' + required[k] });
  }
  function cell(row, name) { var index = columns[essayNormalize_(name)]; return index === undefined ? '' : String(row[index] == null ? '' : row[index]).trim(); }
  var difficulties = String(params.difficulty || '').split(',').map(essayNormalize_).filter(Boolean);
  var subject = essayNormalize_(params.subject || params.mon || '');
  var questions = [];
  for (var i = 1; i < rows.length; i++) {
    var row = rows[i];
    if (essayNormalize_(cell(row, 'TrangThai')) !== 'da duyet') continue;
    var monId = cell(row, 'monID'), difficulty = cell(row, 'difficulty');
    if (subject && essayNormalize_(monId) !== subject) continue;
    if (difficulties.length && difficulties.indexOf(essayNormalize_(difficulty)) < 0) continue;
    var image = cell(row, 'HinhAnh');
    questions.push({ id: cell(row, 'CauHoiID'), monId: monId, topic: cell(row, 'Chương/Chủ đề'), difficulty: difficulty,
      question: cell(row, 'Question'), imageUrl: essayNormalize_(image) === 'none' ? '' : image,
      sampleAnswer: cell(row, 'DapAnThamKhao'), rubric: cell(row, 'HuongDanChamAI'), maxPoints: Number(cell(row, 'DiemToiDa')) });
  }
  return jsonOut({ success: true, questionType: 'essay', count: questions.length, questions: questions });
}

function essayHistorySheet_() {
  var ss = SpreadsheetApp.openById(SPREADSHEET_ID);
  var sheet = ss.getSheetByName('BaiNopTuLuan') || ss.insertSheet('BaiNopTuLuan');
  var headers = ['SubmissionID', 'MatchID', 'Username', 'CauHoiID', 'monID', 'difficulty', 'BaiLamText', 'CoAnh', 'AINhanDang', 'Diem', 'DiemToiDa', 'NhanXetAI', 'DapAnAI', 'NgayNop'];
  if (sheet.getLastRow() === 0) { sheet.getRange(1, 1, 1, headers.length).setValues([headers]); sheet.setFrozenRows(1); }
  var existing = sheet.getRange(1, 1, 1, headers.length).getValues()[0];
  if (headers.some(function(name, index) { return existing[index] !== name; })) throw new Error('BaiNopTuLuan đang có cấu trúc khác. Hãy đổi tên tab cũ để giữ dữ liệu, rồi chạy setupPlanetEssay lại.');
  return sheet;
}
function setupPlanetEssay() {
  essayHistorySheet_();
  Logger.log('Đã chuẩn bị BaiNopTuLuan. Không thay đổi dữ liệu câu hỏi hoặc các tab khác.');
}
function saveEssaySubmission(p) {
  var secret = PropertiesService.getScriptProperties().getProperty('PLANET_SERVER_SECRET');
  if (!secret || p.serverSecret !== secret) return jsonOut({ success: false, message: 'Unauthorized essay submission' });
  var record = JSON.parse(p.record || '{}');
  if (!record.submissionId || !record.questionId || !Number.isFinite(record.score) || record.score < 0 || record.score > 10)
    return jsonOut({ success: false, message: 'Invalid essay record' });
  function safe(value, length) { var text = String(value || '').slice(0, length || 2000); return /^[\s]*[=+@-]/.test(text) ? "'" + text : text; }
  var lock = LockService.getScriptLock(); lock.waitLock(10000);
  try {
    var sheet = essayHistorySheet_();
    if (sheet.getLastRow() > 1) {
      var found = sheet.getRange(2, 1, sheet.getLastRow() - 1, 1).createTextFinder(String(record.submissionId)).matchEntireCell(true).useRegularExpression(false).findNext();
      if (found) return jsonOut({ success: true, duplicate: true });
    }
    sheet.appendRow([safe(record.submissionId, 120), safe(record.matchId, 100), safe(record.username, 100), safe(record.questionId, 100),
      safe(record.subject, 50), safe(record.difficulty, 30), safe(record.answer), Boolean(record.hasImage), safe(record.transcription), record.score, 10,
      safe(record.feedback, 500), safe(record.suggestedAnswer, 1000), new Date()]);
    SpreadsheetApp.flush(); return jsonOut({ success: true });
  } finally { lock.releaseLock(); }
}
// Images are graded directly through PlanetServer, not uploaded to Drive.
function saveEssayImage() {
  return { success: false, message: 'Ảnh bài làm được gửi trực tiếp tới máy chủ Planet để AI chấm; API này không lưu ảnh lên Drive.' };
}

// Also embedded in Code.gs. Do not paste both copies into the same project.
// Existing History columns A:F are preserved. G:H are match ID and rank score.
function planetSaveMatch_(p) {
  var record = JSON.parse(p.record || '{}');
  var username = String(record.username || '').trim();
  var id = String(record.matchId || '');
  var mode = record.mode || 'Đối kháng';
  var practice = mode === 'Luyện tập';
  var maximum = practice ? (record.questionType === 'essay' ? 50 : 0)
    : record.questionType === 'essay' ? 90 : record.questionType === 'multipleChoice' ? 450 : 0;
  if (!/^[A-Za-z0-9_-]{8,100}$/.test(id) || !username || username.length > 100 ||
      !['THẮNG', 'THUA', 'HÒA'].includes(record.result) || !['Luyện tập', 'Đối kháng'].includes(mode) || (!practice && !maximum) || record.maxScore !== maximum ||
      !Number.isInteger(record.score) || record.score > (maximum || 100000) || record.score < (practice ? 0 : maximum === 90 ? 0 : -600))
    return jsonOut({ success: false, message: 'Kết quả Planet không hợp lệ.' });
  var completed = record.finishedAt ? planetRankTime_(record.finishedAt) : Date.now();
  if (!Number.isFinite(completed) || completed > Date.now() + 300000)
    return jsonOut({ success: false, message: 'Thời gian hoàn thành không hợp lệ.' });
  var ss = SpreadsheetApp.openById(SPREADSHEET_ID);
  var users = ss.getSheetByName('Users');
  if (!users || !users.getDataRange().getValues().slice(1).some(function(row) { return String(row[1]).trim() === username; }))
    return jsonOut({ success: false, message: 'Không tìm thấy tài khoản trong Users.' });
  var lock = LockService.getScriptLock(); lock.waitLock(15000);
  try {
    var sheet = ss.getSheetByName('History');
    if (!sheet) {
      sheet = ss.insertSheet('History');
      sheet.appendRow(['Username', 'Opponent', 'Mode', 'Result', 'Score', 'Time']);
    }
    if (sheet.getMaxColumns() < 8) sheet.insertColumnsAfter(sheet.getMaxColumns(), 8 - sheet.getMaxColumns());
    var extra = sheet.getRange(1, 7, 1, 2).getValues()[0];
    var expected = ['PlanetMatchID', 'PlanetRankScore'];
    for (var col = 0; col < 2; col++) {
      if (extra[col] && extra[col] !== expected[col])
        return jsonOut({ success: false, message: 'History cột G:H đã có dữ liệu khác. Cần kiểm tra trước khi lưu Planet.' });
    }
    var rows = sheet.getDataRange().getValues();
    if ((!extra[0] || !extra[1]) && rows.slice(1).some(function(row) {
      return (!extra[0] && row[6] !== '' && row[6] != null) || (!extra[1] && row[7] !== '' && row[7] != null);
    })) return jsonOut({ success: false, message: 'History G:H có dữ liệu chưa đặt tên; không ghi đè.' });
    sheet.getRange(1, 7, 1, 2).setValues([expected]);
    for (var i = 1; i < rows.length; i++) {
      if (String(rows[i][0]).trim() === username && String(rows[i][6]) === id)
        return jsonOut({ success: true, duplicate: true, matchId: id });
    }
    function safe(value) {
      var text = String(value || '').slice(0, 100);
      return /^\s*[=+@-]/.test(text) ? "'" + text : text;
    }
    var rankScore = Math.max(0, Math.min(100, maximum ? Math.round(record.score / maximum * 100) : 0));
    var lessonColumns = planetHistoryLessonColumns_(sheet);
    var entry = [safe(username), safe(record.opponent || 'Máy AI'), mode, record.result,
      record.score, new Date(completed), id, rankScore];
    while (entry.length <= Math.max(lessonColumns.subject, lessonColumns.chapter)) entry.push('');
    entry[lessonColumns.subject] = practice ? safe(record.subject) : '';
    entry[lessonColumns.chapter] = practice ? safe(record.chapter) : '';
    sheet.appendRow(entry);
    SpreadsheetApp.flush();
    return jsonOut({ success: true, matchId: id, rankScore: rankScore });
  } finally { lock.releaseLock(); }
}

function planetHistoryLessonColumns_(sheet) {
  var headers = sheet.getDataRange().getValues()[0].slice();
  while (headers.length < 8) headers.push('');
  var subject = headers.indexOf('Subject'), chapter = headers.indexOf('Chapter');
  if (subject < 0) { subject = headers.length; headers.push('Subject'); }
  if (chapter < 0) { chapter = headers.length; headers.push('Chapter'); }
  if (sheet.getMaxColumns() < headers.length) sheet.insertColumnsAfter(sheet.getMaxColumns(), headers.length - sheet.getMaxColumns());
  sheet.getRange(1, 1, 1, headers.length).setValues([headers]);
  return { subject: subject, chapter: chapter };
}

// Week is [Monday 00:00, next Monday 00:00), Asia/Ho_Chi_Minh (UTC+7).
// No rows are deleted when the week changes.
function planetRankWeek_(now) {
  var offset = 7 * 60 * 60 * 1000;
  var local = new Date(Number(now) + offset);
  var daysSinceMonday = (local.getUTCDay() + 6) % 7;
  var start = Date.UTC(local.getUTCFullYear(), local.getUTCMonth(), local.getUTCDate() - daysSinceMonday) - offset;
  return { start: start, end: start + 7 * 24 * 60 * 60 * 1000 };
}

function planetRankTime_(value) {
  if (value && typeof value.getTime === 'function') return value.getTime();
  var text = String(value || '').trim();
  // ISO timestamps carrying an explicit timezone, including Unity's UTC format.
  if (/^\d{4}-\d{2}-\d{2}T.*(?:Z|[+-]\d{2}:?\d{2})$/i.test(text)) return Date.parse(text);
  var match = text.match(/^(\d{1,2})\/(\d{1,2})\/(\d{4})(?:[ T](\d{1,2}):(\d{2})(?::(\d{2}))?)?$/);
  if (!match) return NaN;
  var year = Number(match[3]), month = Number(match[2]), day = Number(match[1]);
  var hour = Number(match[4] || 0), minute = Number(match[5] || 0), second = Number(match[6] || 0);
  var local = new Date(Date.UTC(year, month - 1, day, hour, minute, second));
  if (local.getUTCFullYear() !== year || local.getUTCMonth() !== month - 1 || local.getUTCDate() !== day ||
      hour > 23 || minute > 59 || second > 59) return NaN;
  return local.getTime() - 7 * 60 * 60 * 1000;
}

function planetGetProgressRank_() {
  var ss = SpreadsheetApp.openById(SPREADSHEET_ID);
  var usersSheet = ss.getSheetByName('Users');
  var users = usersSheet ? usersSheet.getDataRange().getValues().slice(1) : [];
  var aliases = Object.create(null), names = Object.create(null), scores = Object.create(null);
  users.forEach(function(row) {
    var username = String(row[1] || '').trim(), id = String(row[0] || '').trim();
    if (!username && !id) return;
    var key = username || id;
    if (username) aliases[username] = key;
    if (id) aliases[id] = key;
    names[key] = String(row[3] || key);
  });
  var now = Date.now(), week = planetRankWeek_(now), seen = Object.create(null), undated = 0;
  var history = ss.getSheetByName('History');
  if (history) {
    var rows = history.getDataRange().getValues();
    var hasMatchId = rows.length && rows[0][6] === 'PlanetMatchID';
    rows.slice(1).forEach(function(row) {
      var mode = String(row[2] || '').normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/[đĐ]/g, 'd').toLowerCase().trim();
      if (!['luyen tap', 'doi khang'].includes(mode)) return;
      var key = String(row[0] || '').trim(), points = Number(row[4]);
      if (!key || row[4] === '' || row[4] == null || !Number.isFinite(points)) return;
      key = aliases[key] || key;
      var stamp = planetRankTime_(row[5]);
      if (!Number.isFinite(stamp)) { undated++; return; }
      if (stamp < week.start || stamp >= week.end || stamp > now) return;
      if (hasMatchId && row[6]) {
        var id = key + ':' + String(row[6]);
        if (seen[id]) return;
        seen[id] = true;
      }
      // History is the single result ledger. Never add the same result again
      // from TienDoHocTap or the normalized legacy PlanetRankScore column.
      scores[key] = (scores[key] || 0) + points;
    });
  }
  var players = Object.keys(scores).map(function(key) { return { userID: key, fullName: names[key] || key, score: scores[key] }; });
  players.sort(function(a, b) { return b.score - a.score || a.userID.localeCompare(b.userID); });
  players.forEach(function(player, index) { player.rank = index + 1; });
  return ContentService.createTextOutput(JSON.stringify({ success: true, rankVersion: 'weekly-v2',
    timezone: 'Asia/Ho_Chi_Minh', weekStartsAt: new Date(week.start).toISOString(), weekEndsAt: new Date(week.end).toISOString(),
    weekStartLabel: Utilities.formatDate(new Date(week.start), 'Asia/Ho_Chi_Minh', 'dd/MM/yyyy'),
    weekEndLabel: Utilities.formatDate(new Date(week.end - 1), 'Asia/Ho_Chi_Minh', 'dd/MM/yyyy'),
    ignoredUndatedRows: undated, players: players })).setMimeType(ContentService.MimeType.JSON);
}

