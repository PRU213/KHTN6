var SPREADSHEET_ID = "1DcJwlN_fUDdcaQ6IfnlkMJApgWuEUqMhyZv9Kn8vfWo";

// =======================================================
// MAIN GET API
// =======================================================

function doGet(e) {
  try {
    var params = (e && e.parameter) ? e.parameter : {};
    var action = params.action || "";
    if (action === "essayQuestions") return getEssayQuestions(e);

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

    if (action === "savePlanetMatch") return planetSaveMatch_(p);

    // -----------------------------------
    // API LƯU LEADERBOARD CŨ
    // -----------------------------------
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
    // API LƯU BÀI NỘP TỰ LUẬN
    // -----------------------------------
    if (action === "saveEssaySubmission") {
      var result = saveEssaySubmission(p);
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
// Essay reader is included so replacing this main file cannot remove it.
function planetEssayQuestionKey_(value) {
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
  rows[0].forEach(function(name, index) { columns[planetEssayQuestionKey_(name)] = index; });
  var required = ['CauHoiID', 'monID', 'difficulty', 'Question', 'DapAnThamKhao', 'HuongDanChamAI', 'DiemToiDa', 'TrangThai'];
  for (var k = 0; k < required.length; k++) {
    if (columns[planetEssayQuestionKey_(required[k])] === undefined) return jsonOut({ success: false, message: 'TuLuan thiếu cột ' + required[k] });
  }
  function cell(row, name) { var index = columns[planetEssayQuestionKey_(name)]; return index === undefined ? '' : String(row[index] == null ? '' : row[index]).trim(); }
  var difficulties = String(params.difficulty || '').split(',').map(planetEssayQuestionKey_).filter(Boolean);
  var subject = planetEssayQuestionKey_(params.subject || params.mon || '');
  var questions = [];
  for (var i = 1; i < rows.length; i++) {
    var row = rows[i];
    if (planetEssayQuestionKey_(cell(row, 'TrangThai')) !== 'da duyet') continue;
    var monId = cell(row, 'monID'), difficulty = cell(row, 'difficulty');
    if (subject && planetEssayQuestionKey_(monId) !== subject) continue;
    if (difficulties.length && difficulties.indexOf(planetEssayQuestionKey_(difficulty)) < 0) continue;
    var image = cell(row, 'HinhAnh');
    questions.push({ id: cell(row, 'CauHoiID'), monId: monId, topic: cell(row, 'Chương/Chủ đề'), difficulty: difficulty,
      question: cell(row, 'Question'), imageUrl: planetEssayQuestionKey_(image) === 'none' ? '' : image,
      sampleAnswer: cell(row, 'DapAnThamKhao'), rubric: cell(row, 'HuongDanChamAI'), maxPoints: Number(cell(row, 'DiemToiDa')) });
  }
  return jsonOut({ success: true, questionType: 'essay', count: questions.length, questions: questions });
}

