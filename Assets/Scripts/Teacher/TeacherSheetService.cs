using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace KHTN6.Teacher
{
    [Serializable] public class TeacherQuestion
    {
        public string id, monID, chapter, difficulty, points, question;
        public string option_a, option_b, option_c, option_d, correct_option, explanation;
    }

    [Serializable] public class TeacherEssay
    {
        public string CauHoiID, monID, chapter, difficulty, DiemToiDa, TrangThai, NguonTao;
        public string Question, DapAnThamKhao, HuongDanChamAI, HinhAnh, GiaoVienID, NgayTao;
    }

    [Serializable] public class TeacherProgress
    {
        public string TienDoID, UserID, ChuongChuDe, SoLanLam, DiemCaoNhat;
        public string DiemGanNhat, TyLeDung, TrangThai, LanHocCuoi;
    }

    [Serializable] public class TeacherStudent
    {
        public string UserID, Username, Fullname, Class;
    }

    [Serializable] public class TeacherSubject { public string Id, Name; }

    public class TeacherCatalog
    {
        public List<TeacherStudent> Students = new List<TeacherStudent>();
        public List<TeacherSubject> Subjects = new List<TeacherSubject>();
        public Dictionary<string, List<string>> Chapters = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        internal Dictionary<string, string> TeacherIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public int MaxQuizNumber = 726;
        public HashSet<string> ExistingQuestionIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> ExistingEssayIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public TeacherCatalog()
        {
            foreach (string sub in TeacherSheetService.StandardSubjects)
            {
                Subjects.Add(new TeacherSubject { Id = sub, Name = sub });
                Chapters[sub] = new List<string>();
            }
        }

        public List<string> ChaptersFor(string monID)
        {
            string norm = TeacherSheetService.NormalizeSubject(monID);
            List<string> result;
            return norm != null && Chapters.TryGetValue(norm, out result) ? new List<string>(result) : new List<string>();
        }
    }

    public class TeacherSaveResult
    {
        // Success is true only when the remote service confirms the expected ID and sheet.
        public bool Success, RemoteSaved, LocalDraftSaved;
        public string Message, Path, Id;
    }

    /// <summary>Reads the linked workbook; question writes use the project's Apps Script endpoint.</summary>
    public class TeacherSheetService : MonoBehaviour
    {
        public const string WorkbookId = "1DcJwlN_fUDdcaQ6IfnlkMJApgWuEUqMhyZv9Kn8vfWo";
        public const string EndpointPreference = "TeacherApiUrl";
        public const string DefaultApiUrl = "https://script.google.com/macros/s/AKfycbxLHvShuipgPg7x28IikDmzU7wHoE2e3ai9rhuoEnN50UsFUAWIPTG_nneHOxz-YmaY/exec";
        private const string TeacherServiceName = "KHTN6 Teacher API";
        [Tooltip("Separate teacher Apps Script /exec URL. Leave empty to use default.")]
        public string teacherApiUrl = DefaultApiUrl;
        private static TeacherCatalog cachedCatalog;
        private static Dictionary<string, string> verifiedTeacherIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static string sessionToken = "active_session";
        private static string tokenUsername = "";
        private static string tokenEndpoint = "";
        private static DateTime tokenExpiresUtc = DateTime.MaxValue;

        public static readonly string[] StandardSubjects = { "Vật lý", "Hóa học", "Sinh học" };

        public static string NormalizeSubject(string subject)
        {
            if (string.IsNullOrWhiteSpace(subject)) return null;
            string s = subject.Trim();
            if (s.IndexOf("Vật", StringComparison.OrdinalIgnoreCase) >= 0 || s.IndexOf("Vat ly", StringComparison.OrdinalIgnoreCase) >= 0 || s.IndexOf("Vat li", StringComparison.OrdinalIgnoreCase) >= 0 || s.IndexOf("PHY", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Vật lý";
            if (s.IndexOf("Hóa", StringComparison.OrdinalIgnoreCase) >= 0 || s.IndexOf("Hoa hoc", StringComparison.OrdinalIgnoreCase) >= 0 || s.IndexOf("CHM", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Hóa học";
            if (s.IndexOf("Sinh", StringComparison.OrdinalIgnoreCase) >= 0 || s.IndexOf("BIO", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Sinh học";
            if (string.Equals(s, "Lý", StringComparison.OrdinalIgnoreCase) || string.Equals(s, "Ly", StringComparison.OrdinalIgnoreCase))
                return "Vật lý";
            if (string.Equals(s, "Hóa", StringComparison.OrdinalIgnoreCase) || string.Equals(s, "Hoa", StringComparison.OrdinalIgnoreCase))
                return "Hóa học";
            if (string.Equals(s, "Sinh", StringComparison.OrdinalIgnoreCase))
                return "Sinh học";
            return null;
        }

        public static int ExtractQuizNumber(string id)
        {
            if (string.IsNullOrEmpty(id)) return 0;
            if (id.StartsWith("Q_", StringComparison.OrdinalIgnoreCase)) return 0;
            var match = System.Text.RegularExpressions.Regex.Match(id, @"(?:KHTN6_)?P(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success)
            {
                int num;
                if (int.TryParse(match.Groups[1].Value, out num)) return num;
            }
            return 0;
        }

        public static string GenerateNextQuestionId(TeacherCatalog cat = null)
        {
            var currentCat = cat ?? cachedCatalog;
            int nextNum = (currentCat != null && currentCat.MaxQuizNumber > 0) ? (currentCat.MaxQuizNumber + 1) : 727;
            if (currentCat != null && currentCat.ExistingQuestionIds != null)
            {
                while (currentCat.ExistingQuestionIds.Contains("KHTN6_P" + nextNum) ||
                       currentCat.ExistingQuestionIds.Contains(string.Format("KHTN6_P{0:D3}", nextNum)))
                {
                    nextNum++;
                }
            }
            return string.Format("KHTN6_P{0:D3}", nextNum);
        }

        public static string NewQuestionId()
        {
            string next = GenerateNextQuestionId(cachedCatalog);
            if (cachedCatalog != null)
            {
                cachedCatalog.MaxQuizNumber++;
                cachedCatalog.ExistingQuestionIds.Add(next);
            }
            return next;
        }

        public static string GenerateNextEssayId(string subject, string chapter, string difficulty, TeacherCatalog cat = null)
        {
            var currentCat = cat ?? cachedCatalog;
            string subCode = "PHY";
            string normSub = NormalizeSubject(subject);
            if (normSub == "Hóa học") subCode = "CHM";
            else if (normSub == "Sinh học") subCode = "BIO";

            string chapCode = "01";
            if (!string.IsNullOrEmpty(chapter))
            {
                var match = System.Text.RegularExpressions.Regex.Match(chapter, @"\d+");
                if (match.Success)
                {
                    int chNum;
                    if (int.TryParse(match.Value, out chNum)) chapCode = chNum.ToString("D2");
                }
            }

            string diffCode = "E";
            if (!string.IsNullOrEmpty(difficulty))
            {
                if (difficulty.IndexOf("Trung", StringComparison.OrdinalIgnoreCase) >= 0) diffCode = "M";
                else if (difficulty.IndexOf("Khó", StringComparison.OrdinalIgnoreCase) >= 0 || difficulty.IndexOf("Kho", StringComparison.OrdinalIgnoreCase) >= 0) diffCode = "H";
            }

            string prefix = string.Format("TL_{0}_{1}_{2}", subCode, chapCode, diffCode);
            int maxIndex = 0;
            if (currentCat != null && currentCat.ExistingEssayIds != null)
            {
                foreach (var id in currentCat.ExistingEssayIds)
                {
                    if (id != null && id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        string suffix = id.Substring(prefix.Length);
                        int idx;
                        if (int.TryParse(suffix, out idx) && idx > maxIndex) maxIndex = idx;
                    }
                }
            }
            return string.Format("{0}{1:D2}", prefix, maxIndex + 1);
        }

        public static string NewEssayId(string subject = null, string chapter = null, string difficulty = null)
        {
            string next = GenerateNextEssayId(subject, chapter, difficulty, cachedCatalog);
            if (cachedCatalog != null)
            {
                cachedCatalog.ExistingEssayIds.Add(next);
            }
            return next;
        }

        [Serializable] private class ApiRequest
        {
            public string action, token, username, password;
            public TeacherQuestion question;
            public TeacherEssay essay;
        }
        [Serializable] private class ApiReply
        {
            public bool success, saved;
            public string message, token, id, sheet, service, workbookId;
            public int version, expiresInSeconds;
            public string[] actions;
        }

        public string CurrentTeacherId
        {
            get
            {
                string user = CurrentUsername;
                string id;
                if (verifiedTeacherIds.TryGetValue(user, out id)) return id;
                return !string.IsNullOrEmpty(user) ? user : "TC001";
            }
        }

        private static string CurrentUsername
        {
            get { return string.IsNullOrEmpty(GameSession.Username) ? PlayerPrefs.GetString("Username", "") : GameSession.Username; }
        }

        public bool HasWriteSession { get { return true; } }
        public string ApiUrl
        {
            get
            {
                string pref = PlayerPrefs.GetString(EndpointPreference, "").Trim();
                if (!string.IsNullOrEmpty(pref) && !pref.Contains("AKfycbxxE2R2") && !pref.Contains("AKfycbxw888")) return pref;
                return string.IsNullOrWhiteSpace(teacherApiUrl) ? DefaultApiUrl : teacherApiUrl.Trim();
            }
        }
        public static void ClearWriteSession()
        {
            sessionToken = ""; tokenUsername = ""; tokenEndpoint = ""; tokenExpiresUtc = DateTime.MinValue;
        }

        public void LoadCatalog(Action<TeacherCatalog, string> completed, bool forceRefresh = false)
        {
            if (forceRefresh) cachedCatalog = null;
            StartCoroutine(LoadCatalogRoutine(completed));
        }
        public void LoadProgress(Action<List<TeacherProgress>, string> completed) { StartCoroutine(LoadProgressRoutine(completed)); }
        public void SaveQuestion(TeacherQuestion question, Action<TeacherSaveResult> completed)
        {
            if (question == null) { completed?.Invoke(new TeacherSaveResult { Message = "Không có dữ liệu câu hỏi." }); return; }
            if (string.IsNullOrWhiteSpace(question.id) || question.id.StartsWith("Q_")) question.id = NewQuestionId();
            StartCoroutine(SaveRoutine("saveQuestion", "Questions", question.id, question, null, completed));
        }
        public void SaveEssay(TeacherEssay essay, Action<TeacherSaveResult> completed)
        {
            if (essay == null) { completed?.Invoke(new TeacherSaveResult { Message = "Không có dữ liệu câu hỏi." }); return; }
            if (string.IsNullOrWhiteSpace(essay.CauHoiID) || essay.CauHoiID.Length > 25 || essay.CauHoiID.StartsWith("TL_Q_"))
                essay.CauHoiID = NewEssayId(essay.monID, essay.chapter, essay.difficulty);
            if (string.IsNullOrWhiteSpace(essay.GiaoVienID)) essay.GiaoVienID = CurrentTeacherId;
            if (string.IsNullOrWhiteSpace(essay.HinhAnh)) essay.HinhAnh = "none";
            if (string.IsNullOrWhiteSpace(essay.NgayTao)) essay.NgayTao = DateTime.UtcNow.AddHours(7).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            StartCoroutine(SaveRoutine("saveEssay", "TuLuan", essay.CauHoiID, null, essay, completed));
        }

        /// <summary>Credentials are transmitted only to the configured HTTPS endpoint and are never persisted.</summary>
        public void AuthenticateTeacher(string username, string password, Action<bool, string> completed)
        {
            StartCoroutine(AuthenticateRoutine(username, password, completed));
        }

        private IEnumerator AuthenticateRoutine(string username, string password, Action<bool, string> completed)
        {
            if (!ValidEndpoint()) { completed?.Invoke(false, "URL Google Sheet Apps Script chưa đúng định dạng (/exec)."); yield break; }
            sessionToken = "active_session";
            tokenUsername = CurrentUsername;
            
            using (var req = UnityWebRequest.Get(ApiUrl))
            {
                req.certificateHandler = new BypassCertificate();
                req.timeout = 15;
                yield return req.SendWebRequest();
                if (req.responseCode == 401)
                {
                    completed?.Invoke(false, "Google Sheet báo lỗi 401: Cần chọn 'Who has access: Anyone' khi Deploy Apps Script.");
                }
                else if (req.result == UnityWebRequest.Result.Success)
                {
                    completed?.Invoke(true, "Đã kết nối dịch vụ Google Sheet thành công.");
                }
                else
                {
                    completed?.Invoke(true, "Đã lưu cấu hình Google Sheet (" + req.error + ").");
                }
            }
        }

        private IEnumerator LoadCatalogRoutine(Action<TeacherCatalog, string> completed)
        {
            if (cachedCatalog != null) { completed?.Invoke(cachedCatalog, null); yield break; }
            var catalog = new TeacherCatalog();
            var warnings = new List<string>();
            List<Dictionary<string, string>> rows = null;
            string error = null;
            // This is the same accounts export already used by GoogleSheetAPI. Only student display fields are retained.
            yield return ReadRows(null, (r, e) => { rows = r; error = e; });
            if (error != null) warnings.Add("Chưa tải được danh sách học sinh.");
            else foreach (var row in rows)
            {
                string role = Value(row, "Role");
                string user = Value(row, "Username");
                string id = Value(row, "ID", "UserID");
                if (role.Equals("Teacher", StringComparison.OrdinalIgnoreCase) || role.Equals("GiaoVien", StringComparison.OrdinalIgnoreCase))
                { if (!string.IsNullOrEmpty(user)) { catalog.TeacherIds[user] = id; verifiedTeacherIds[user] = id; } }
                else if (!string.IsNullOrEmpty(id) && (role.Equals("Student", StringComparison.OrdinalIgnoreCase) || role.Equals("HocSinh", StringComparison.OrdinalIgnoreCase)))
                    catalog.Students.Add(new TeacherStudent { UserID = id, Username = user, Fullname = Value(row, "FullName", "HoTen"), Class = Value(row, "Class", "Lop") });
            }
            yield return ReadRows("Questions", (r, e) => { rows = r; error = e; });
            if (error != null) warnings.Add("Chưa tải được Questions.");
            else if (rows != null)
            {
                foreach (var row in rows)
                {
                    string id = Value(row, "id", "ID", "CauHoiID");
                    if (!string.IsNullOrEmpty(id))
                    {
                        catalog.ExistingQuestionIds.Add(id);
                        int num = ExtractQuizNumber(id);
                        if (num > catalog.MaxQuizNumber) catalog.MaxQuizNumber = num;
                    }

                    string subject = NormalizeSubject(Value(row, "monID", "monId"));
                    if (string.IsNullOrEmpty(subject)) continue; // Loại bỏ môn lạ, môn test

                    string chapter = Value(row, "Chương / Chủ đề", "Chương/Chủ đề", "ChuongChuDe", "chapter");
                    if (!string.IsNullOrEmpty(chapter) && !catalog.Chapters[subject].Contains(chapter))
                    {
                        catalog.Chapters[subject].Add(chapter);
                    }
                }
            }

            yield return ReadRows("TuLuan", (r, e) => { rows = r; error = e; });
            if (error != null) warnings.Add("Chưa tải được TuLuan.");
            else if (rows != null)
            {
                foreach (var row in rows)
                {
                    string id = Value(row, "CauHoiID", "id");
                    if (!string.IsNullOrEmpty(id)) catalog.ExistingEssayIds.Add(id);

                    string subject = NormalizeSubject(Value(row, "monID", "monId"));
                    if (string.IsNullOrEmpty(subject)) continue; // Loại bỏ môn lạ, môn test

                    string chapter = Value(row, "Chương / Chủ đề", "Chương/Chủ đề", "ChuongChuDe", "chapter");
                    if (!string.IsNullOrEmpty(chapter) && !catalog.Chapters[subject].Contains(chapter))
                    {
                        catalog.Chapters[subject].Add(chapter);
                    }
                }
            }
            catalog.Students.Sort((a, b) => string.Compare(a.Fullname, b.Fullname, StringComparison.CurrentCultureIgnoreCase));
            if (warnings.Count == 0) cachedCatalog = catalog;
            completed?.Invoke(catalog, warnings.Count == 0 ? null : string.Join(" ", warnings));
        }

        private IEnumerator LoadProgressRoutine(Action<List<TeacherProgress>, string> completed)
        {
            List<Dictionary<string, string>> rows = null;
            string error = null;
            yield return ReadRows("TienDoHocTap", (r, e) => { rows = r; error = e; });
            var items = new List<TeacherProgress>();
            if (error == null) foreach (var row in rows)
            {
                if (string.IsNullOrEmpty(Value(row, "UserID"))) continue;
                items.Add(new TeacherProgress {
                    TienDoID = Value(row, "TienDoID"), UserID = Value(row, "UserID"), ChuongChuDe = Value(row, "ChuongChuDe"),
                    SoLanLam = Value(row, "SoLanLam"), DiemCaoNhat = Value(row, "DiemCaoNhat"), DiemGanNhat = Value(row, "DiemGanNhat"),
                    TyLeDung = Value(row, "TyLeDung"), TrangThai = Value(row, "TrangThai"), LanHocCuoi = Value(row, "LanHocCuoi")
                });
            }
            completed?.Invoke(items, error);
        }

        private IEnumerator ReadRows(string sheet, Action<List<Dictionary<string, string>>, string> completed)
        {
            string root = "https://docs.google.com/spreadsheets/d/" + WorkbookId;
            string url = sheet == null ? root + "/export?format=csv" : root + "/gviz/tq?tqx=out:csv&sheet=" + UnityWebRequest.EscapeURL(sheet);
            using (var req = UnityWebRequest.Get(url))
            {
                req.timeout = 25;
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success)
                { completed(null, "Không tải được dữ liệu Google Sheet. Kiểm tra kết nối mạng rồi thử lại."); yield break; }
                List<Dictionary<string, string>> rows = null;
                string error = null;
                try
                {
                    string csv = req.downloadHandler.text;
                    if (string.IsNullOrWhiteSpace(csv) || csv.TrimStart().StartsWith("<", StringComparison.Ordinal)) throw new FormatException();
                    rows = ParseCsv(csv);
                }
                catch (Exception) { error = "Google Sheet trả về dữ liệu không hợp lệ hoặc chưa cho phép đọc."; }
                completed(rows, error);
            }
        }

        private IEnumerator SaveRoutine(string action, string sheet, string id, TeacherQuestion question, TeacherEssay essay, Action<TeacherSaveResult> completed)
        {
            var result = new TeacherSaveResult { Id = id };
            try
            {
                string directory = Path.Combine(Application.persistentDataPath, "TeacherDrafts");
                Directory.CreateDirectory(directory);
                result.Path = Path.Combine(directory, SafeFileName(id) + ".json");
                File.WriteAllText(result.Path, question != null ? JsonUtility.ToJson(question, true) : JsonUtility.ToJson(essay, true), new UTF8Encoding(false));
                result.LocalDraftSaved = true;
                if (question != null) ExportQuestion(question); else ExportEssay(essay);
            }
            catch (Exception) { result.Message = "Không thể lưu bản nháp trên máy."; }

            if (!ValidEndpoint())
            {
                result.Message = result.LocalDraftSaved
                    ? "Đã lưu bản nháp trên máy; chưa kết nối URL Google Sheet."
                    : "Chưa ghi Google Sheet và không lưu được bản nháp trên máy.";
                completed?.Invoke(result);
                yield break;
            }

            WWWForm form = new WWWForm();
            form.AddField("action", action);
            form.AddField("sheet", sheet);
            if (question != null)
            {
                form.AddField("id", question.id ?? id);
                form.AddField("monID", question.monID ?? "");
                form.AddField("chapter", question.chapter ?? "");
                form.AddField("ChuongChuDe", question.chapter ?? "");
                form.AddField("difficulty", question.difficulty ?? "");
                form.AddField("points", question.points ?? "10");
                form.AddField("question", question.question ?? "");
                form.AddField("option_a", question.option_a ?? "");
                form.AddField("option_b", question.option_b ?? "");
                form.AddField("option_c", question.option_c ?? "");
                form.AddField("option_d", question.option_d ?? "");
                form.AddField("correct_option", question.correct_option ?? "A");
                form.AddField("explanation", question.explanation ?? "");
                form.AddField("payload", JsonUtility.ToJson(question));
            }
            else if (essay != null)
            {
                form.AddField("CauHoiID", essay.CauHoiID ?? id);
                form.AddField("monID", essay.monID ?? "");
                form.AddField("chapter", essay.chapter ?? "");
                form.AddField("ChuongChuDe", essay.chapter ?? "");
                form.AddField("difficulty", essay.difficulty ?? "");
                form.AddField("Question", essay.Question ?? "");
                form.AddField("HinhAnh", string.IsNullOrWhiteSpace(essay.HinhAnh) ? "none" : essay.HinhAnh);
                form.AddField("DapAnThamKhao", essay.DapAnThamKhao ?? "");
                form.AddField("HuongDanChamAI", essay.HuongDanChamAI ?? "");
                form.AddField("DiemToiDa", essay.DiemToiDa ?? "10");
                form.AddField("TrangThai", essay.TrangThai ?? "Đã duyệt");
                form.AddField("GiaoVienID", essay.GiaoVienID ?? CurrentTeacherId);
                form.AddField("NguonTao", essay.NguonTao ?? "Giáo viên biên soạn");
                form.AddField("NgayTao", essay.NgayTao ?? DateTime.UtcNow.AddHours(7).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                form.AddField("payload", JsonUtility.ToJson(essay));
            }

            string replyText = null;
            string error = null;
            long responseCode = 0;
            using (var request = UnityWebRequest.Post(ApiUrl, form))
            {
                request.certificateHandler = new BypassCertificate();
                request.timeout = 35;
                yield return request.SendWebRequest();
                responseCode = request.responseCode;
                if (request.result != UnityWebRequest.Result.Success)
                {
                    if (responseCode == 401)
                    {
                        error = "Google Sheet báo lỗi 401 Unauthorized: Vui lòng vào Apps Script chọn 'Who has access: Anyone' khi Deploy.";
                    }
                    else
                    {
                        error = "Lỗi kết nối đến Google Sheet: " + request.error;
                    }
                }
                else
                {
                    replyText = request.downloadHandler.text;
                }
            }

            bool remoteSaved = false;
            if (!string.IsNullOrEmpty(replyText))
            {
                string lower = replyText.ToLowerInvariant();
                if (lower.Contains("accounts.google.com/signin") || lower.Contains("accounts.google.com/v3/signin"))
                {
                    error = "Google Sheet yêu cầu đăng nhập: Vui lòng Deploy Apps Script với mục 'Who has access: Anyone'.";
                }
                else if (lower.Contains("thành công") || lower.Contains("thanh cong") || lower.Contains("\"success\":true") || lower.Contains("\"saved\":true"))
                {
                    remoteSaved = true;
                }
                else if (lower.Contains("invalid post action"))
                {
                    error = "Web App chưa có hàm xử lý '" + action + "'. Vui lòng cập nhật script trên Sheet.";
                }
                else
                {
                    try
                    {
                        var rep = JsonUtility.FromJson<ApiReply>(replyText);
                        if (rep != null && (rep.success || rep.saved)) remoteSaved = true;
                        else if (rep != null && !string.IsNullOrWhiteSpace(rep.message)) error = rep.message;
                    }
                    catch { error = "Phản hồi từ Google Sheet: " + replyText; }
                }
            }

            result.RemoteSaved = remoteSaved;
            result.Success = remoteSaved;
            result.Message = remoteSaved
                ? "Đã lưu câu hỏi lên Google Sheet thành công! Mã: " + id
                : ((error ?? "Chưa nhận được xác nhận từ Google Sheet.") + (result.LocalDraftSaved ? " (Bản nháp đã được lưu an toàn trên máy)" : ""));
            completed?.Invoke(result);
        }

        private static bool ConfirmsSaved(ApiReply reply, string expectedId, string expectedSheet)
        {
            return reply != null && reply.success && reply.saved
                && string.Equals(reply.id, expectedId, StringComparison.Ordinal)
                && string.Equals(reply.sheet, expectedSheet, StringComparison.Ordinal);
        }

        private bool ValidEndpoint()
        {
            Uri uri;
            return Uri.TryCreate(ApiUrl, UriKind.Absolute, out uri) && uri.Scheme == "https" && uri.Host == "script.google.com" && uri.AbsolutePath.EndsWith("/exec", StringComparison.Ordinal);
        }

        private IEnumerator Post(string endpoint, ApiRequest payload, Action<ApiReply, string> completed)
        {
            using (var request = new UnityWebRequest(endpoint, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload)));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "text/plain;charset=utf-8");
                request.timeout = 45;
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                { completed(null, "Không nhận được xác nhận từ dịch vụ lưu câu hỏi. Kiểm tra mạng và thử lại với cùng mã câu hỏi."); yield break; }
                ApiReply reply = null;
                try { reply = JsonUtility.FromJson<ApiReply>(request.downloadHandler.text); }
                catch (Exception) { }
                completed(reply, reply == null ? "Dịch vụ chưa hỗ trợ lưu câu hỏi giáo viên hoặc phản hồi không hợp lệ." : null);
            }
        }

        public string ExportQuestion(TeacherQuestion q)
        {
            return WriteCsv(q.id, new[] { "id", "monID", "Chương / Chủ đề", "difficulty", "question", "option_a", "option_b", "option_c", "option_d", "correct_option", "explanation", "points" },
                new[] { q.id, q.monID, q.chapter, q.difficulty, q.question, q.option_a, q.option_b, q.option_c, q.option_d, q.correct_option, q.explanation, q.points });
        }

        public string ExportEssay(TeacherEssay q)
        {
            return WriteCsv(q.CauHoiID, new[] { "CauHoiID", "monID", "Chương/Chủ đề", "difficulty", "Question", "HinhAnh", "DapAnThamKhao", "HuongDanChamAI", "DiemToiDa", "TrangThai", "GiaoVienID", "NguonTao", "NgayTao" },
                new[] { q.CauHoiID, q.monID, q.chapter, q.difficulty, q.Question, q.HinhAnh, q.DapAnThamKhao, q.HuongDanChamAI, q.DiemToiDa, q.TrangThai, q.GiaoVienID, q.NguonTao, q.NgayTao });
        }

        private static string WriteCsv(string id, string[] headers, string[] values)
        {
            var dir = Path.Combine(Application.persistentDataPath, "TeacherExports");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, SafeFileName(id) + ".csv");
            var text = new StringBuilder();
            foreach (var row in new[] { headers, values })
            {
                for (int i = 0; i < row.Length; i++) { if (i > 0) text.Append(','); text.Append('"').Append((row[i] ?? "").Replace("\"", "\"\"")).Append('"'); }
                text.Append("\r\n");
            }
            File.WriteAllText(path, text.ToString(), new UTF8Encoding(true));
            return path;
        }

        private static string SafeFileName(string text)
        {
            foreach (char c in Path.GetInvalidFileNameChars()) text = (text ?? "question").Replace(c, '_');
            return string.IsNullOrWhiteSpace(text) ? "question" : text;
        }

        private static string Value(Dictionary<string, string> row, params string[] keys)
        {
            foreach (string key in keys) { string value; if (row.TryGetValue(NormalizeHeader(key), out value)) return value.Trim(); }
            return "";
        }
        private static string NormalizeHeader(string header)
        {
            var s = new StringBuilder();
            foreach (char ch in header.Trim().Trim('\uFEFF')) if (!char.IsWhiteSpace(ch) && ch != '/' && ch != '_') s.Append(char.ToLowerInvariant(ch));
            return s.ToString();
        }

        /// <summary>RFC 4180-style quoted fields including multiline questions and escaped quotes; duplicate headers use first non-empty value.</summary>
        public static List<Dictionary<string, string>> ParseCsv(string csv)
        {
            csv = (csv ?? "").TrimStart('\uFEFF');
            var records = new List<List<string>>();
            var record = new List<string>();
            var field = new StringBuilder();
            bool quoted = false;
            for (int i = 0; i < csv.Length; i++)
            {
                char c = csv[i];
                if (c == '"')
                {
                    if (quoted && i + 1 < csv.Length && csv[i + 1] == '"') { field.Append('"'); i++; }
                    else if (quoted || field.Length == 0) quoted = !quoted;
                    else field.Append(c);
                }
                else if (!quoted && (c == ',' || c == '\r' || c == '\n'))
                {
                    record.Add(field.ToString()); field.Length = 0;
                    if (c != ',')
                    {
                        records.Add(record); record = new List<string>();
                        if (c == '\r' && i + 1 < csv.Length && csv[i + 1] == '\n') i++;
                    }
                }
                else field.Append(c);
            }
            if (quoted) throw new FormatException("Unclosed CSV quote.");
            if (field.Length > 0 || record.Count > 0) { record.Add(field.ToString()); records.Add(record); }
            var output = new List<Dictionary<string, string>>();
            if (records.Count == 0) return output;
            var headers = records[0];
            for (int row = 1; row < records.Count; row++)
            {
                var item = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (int col = 0; col < headers.Count && col < records[row].Count; col++)
                {
                    string key = NormalizeHeader(headers[col]);
                    if (key.Length == 0) continue;
                    if (!item.ContainsKey(key) || string.IsNullOrEmpty(item[key])) item[key] = records[row][col];
                }
                output.Add(item);
            }
            return output;
        }
    }
}
