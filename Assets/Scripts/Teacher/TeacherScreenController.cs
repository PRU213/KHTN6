using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KHTN6.Teacher
{
    /// <summary>Connects the four authored teacher Canvases to the Google Sheet data service.</summary>
    [DisallowMultipleComponent]
    public sealed class TeacherScreenController : MonoBehaviour
    {
        private TeacherCanvasView view;
        private TeacherSheetService service;
        private TeacherCatalog catalog;
        private readonly List<TeacherStudent> students = new List<TeacherStudent>();
        private List<TeacherProgress> progress = new List<TeacherProgress>();
        private readonly List<string> subjectIds = new List<string>();
        private readonly List<string> chapters = new List<string>();
        private readonly Dictionary<string, Image> studentRows = new Dictionary<string, Image>();
        private TeacherStudent selectedStudent;
        private bool busy;
        private bool changingOptions;
        private string imageData = "";
        private Texture2D imageTexture;
        private Vector2 previewSize;
        private Vector2 previewPosition;
        private GameObject settingsPanel;

        private static readonly Color Cream = new Color32(255, 246, 220, 255);
        private static readonly Color Muted = new Color32(164, 195, 185, 255);
        private static readonly Color Mint = new Color32(126, 218, 193, 255);
        private static readonly Color Gold = new Color32(237, 195, 111, 255);

        private void Start()
        {
            view = GetComponent<TeacherCanvasView>();
            if (view == null) view = GetComponentInChildren<TeacherCanvasView>(true);
            if (view == null) { Debug.LogError("TeacherCanvasView chưa được gắn.", this); return; }
            service = GetComponent<TeacherSheetService>();
            if (service == null) service = gameObject.AddComponent<TeacherSheetService>();
            if (view.imagePreview != null)
            {
                previewSize = view.imagePreview.rectTransform.sizeDelta;
                previewPosition = view.imagePreview.rectTransform.anchoredPosition;
            }
            AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat("TeacherVolume", AudioListener.volume));

            SetLabel("AccountName", string.IsNullOrWhiteSpace(GameSession.Fullname)
                ? PlayerPrefs.GetString("Fullname", "Giáo viên") : GameSession.Fullname);
            Bind("BackButton", () => Navigate(view.screen == TeacherScreen.Home ? "SampleScene" : "GiaoVien"));
            Bind("HomeButton", () => Navigate("GiaoVien"));
            Bind("SettingsButton", OpenSettings);
            Bind("RankButton", () => Navigate("XepHang"));
            Bind("LogoutButton", Logout);
            Bind("Navigate_02_Trac_nghiem", () => Navigate("GiaoVienTracNghiem"));
            Bind("Navigate_03_Tu_luan", () => Navigate("GiaoVienTuLuan"));
            Bind("Navigate_04_Tien_do", () => Navigate("GiaoVienTienDo"));
            Bind("ResetButton", ResetForm);
            Bind("SaveButton", Save);
            Bind("PickImage", PickImage);
            Bind("ImageUploadArea", PickImage);
            Bind("RemoveImage", RemoveImage);
            Bind("RefreshProgress", () => LoadProgress(true));
            SetStatus("");
            if (view.screen == TeacherScreen.Home) return;

            if (view.screen == TeacherScreen.Progress)
            {
                var search = view.Input("StudentSearch");
                if (search != null) search.onValueChanged.AddListener(_ => RenderStudents());
                Listen("ClassFilter", _ => RenderStudents());
                Listen("ChapterFilter", _ => RenderProgress());
                Listen("StatusFilter", _ => RenderProgress());
                SetOptions("ClassFilter", new[] { "Tất cả lớp" });
                SetOptions("ChapterFilter", new[] { "Tất cả chương / chủ đề" });
                SetOptions("StatusFilter", new[] { "Tất cả trạng thái" });
                ClearProgressSelection();
                LoadProgress();
            }
            else
            {
                SetOptions("difficulty", new[] { "Dễ", "Trung bình", "Khó" });
                SetOptions("TrangThai", new[] { "Đã duyệt", "Chờ duyệt", "Bản nháp" });
                SetOptions("monID", TeacherSheetService.StandardSubjects);
                subjectIds.Clear();
                subjectIds.AddRange(TeacherSheetService.StandardSubjects);
                SetOptions(ChapterControl, new[] { "Chọn chương / chủ đề" });
                Listen("monID", _ => { UpdateChapters(); UpdateAutoId(); });
                Listen(ChapterControl, _ => UpdateAutoId());
                Listen("difficulty", _ => UpdateAutoId());
                SetupCorrectOptions();
                ResetForm();
                LoadCatalog();
            }
        }

        private string ChapterControl { get { return view.screen == TeacherScreen.Essay ? "Chương/Chủ đề" : "Chương / Chủ đề"; } }

        private void Bind(string name, UnityAction action)
        {
            var button = view.Button(name);
            if (button != null) button.onClick.AddListener(action);
        }

        private void Listen(string name, UnityAction<int> action)
        {
            var dropdown = view.Dropdown(name);
            if (dropdown != null) dropdown.onValueChanged.AddListener(value => { if (!changingOptions) action(value); });
        }

        private void SetOptions(string name, IEnumerable<string> options)
        {
            var control = view.Dropdown(name);
            if (control == null) return;
            bool wasChanging = changingOptions;
            changingOptions = true;
            control.ClearOptions();
            control.AddOptions(options.ToList());
            control.SetValueWithoutNotify(0);
            control.RefreshShownValue();
            changingOptions = wasChanging;
        }

        private void SetupCorrectOptions()
        {
            var group = GetComponent<ToggleGroup>();
            if (group == null) group = gameObject.AddComponent<ToggleGroup>();
            group.allowSwitchOff = true;
            foreach (var toggle in view.GetComponentsInChildren<Toggle>(true))
                if (toggle.name.EndsWith("_Correct", StringComparison.Ordinal))
                {
                    toggle.group = group;
                    toggle.SetIsOnWithoutNotify(false);
                }
        }

        private void LoadCatalog()
        {
            SetBusy(true);
            SetStatus("Đang tải danh mục từ Google Sheet…");
            service.LoadCatalog((data, error) =>
            {
                if (this == null) return;
                catalog = data;
                subjectIds.Clear();
                var names = new List<string>();
                if (data != null && data.Subjects != null && data.Subjects.Count > 0)
                {
                    foreach (var subject in data.Subjects)
                    {
                        subjectIds.Add(subject.Id);
                        names.Add(subject.Name);
                    }
                }
                else
                {
                    subjectIds.AddRange(TeacherSheetService.StandardSubjects);
                    names.AddRange(TeacherSheetService.StandardSubjects);
                }
                SetOptions("monID", names);
                UpdateChapters();
                UpdateAutoId();
                Put("GiaoVienID", service.CurrentTeacherId);
                SetBusy(false);
                SetStatus(error ?? "", !string.IsNullOrEmpty(error));
            });
        }

        private void UpdateAutoId()
        {
            if (view == null) return;
            if (view.screen == TeacherScreen.Quiz)
            {
                Put("id", TeacherSheetService.GenerateNextQuestionId(catalog));
            }
            else if (view.screen == TeacherScreen.Essay)
            {
                Put("CauHoiID", TeacherSheetService.GenerateNextEssayId(SelectedSubject(), SelectedChapter(), Choice("difficulty"), catalog));
            }
        }

        private void UpdateChapters()
        {
            chapters.Clear();
            string subject = SelectedSubject();
            if (catalog != null && !string.IsNullOrEmpty(subject)) chapters.AddRange(catalog.ChaptersFor(subject));
            SetOptions(ChapterControl, chapters.Count > 0 ? chapters : new List<string> { "Chưa có chương / chủ đề" });
        }

        private string SelectedSubject()
        {
            var control = view.Dropdown("monID");
            return control != null && control.value < subjectIds.Count ? subjectIds[control.value] : "";
        }

        private string SelectedChapter()
        {
            var control = view.Dropdown(ChapterControl);
            return control != null && control.value < chapters.Count ? chapters[control.value] : "";
        }

        private void ResetForm()
        {
            if (busy) return;
            foreach (var field in view.GetComponentsInChildren<TMP_InputField>(true))
                if (!IsSettingsControl(field.transform)) field.SetTextWithoutNotify("");
            foreach (var toggle in view.GetComponentsInChildren<Toggle>(true))
                if (toggle.name.EndsWith("_Correct", StringComparison.Ordinal)) toggle.SetIsOnWithoutNotify(false);
            Put("points", "10");
            Put("DiemToiDa", "10");
            Put("NguonTao", "Giáo viên biên soạn");
            Put("GiaoVienID", service.CurrentTeacherId);
            Put("NgayTao", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            UpdateAutoId();
            ReadOnly("id"); ReadOnly("CauHoiID"); ReadOnly("GiaoVienID"); ReadOnly("NgayTao");
            var points = view.Input(view.screen == TeacherScreen.Quiz ? "points" : "DiemToiDa");
            if (points != null) points.contentType = view.screen == TeacherScreen.Quiz
                ? TMP_InputField.ContentType.IntegerNumber : TMP_InputField.ContentType.DecimalNumber;
            var difficulty = view.Dropdown("difficulty");
            if (difficulty != null) difficulty.SetValueWithoutNotify(0);
            var status = view.Dropdown("TrangThai");
            if (status != null) status.SetValueWithoutNotify(0);
            RemoveImage();
            SetStatus("");
        }

        private void ReadOnly(string name)
        {
            var field = view.Input(name);
            if (field != null) field.readOnly = true;
        }

        private string Read(string name)
        {
            var input = view.Input(name);
            return input != null ? input.text.Trim() : "";
        }

        private void Put(string name, string value)
        {
            var input = view.Input(name);
            if (input != null) input.SetTextWithoutNotify(value ?? "");
        }

        private string Choice(string name)
        {
            var dropdown = view.Dropdown(name);
            return dropdown != null && dropdown.options.Count > dropdown.value ? dropdown.options[dropdown.value].text : "";
        }

        private bool Require(string name, string title)
        {
            if (!string.IsNullOrWhiteSpace(Read(name))) return true;
            SetStatus("Vui lòng nhập " + title + ".", true);
            var field = view.Input(name);
            if (field != null) field.ActivateInputField();
            return false;
        }

        private void Save()
        {
            if (busy) return;
            if (string.IsNullOrEmpty(SelectedSubject()) || string.IsNullOrEmpty(SelectedChapter()))
            { SetStatus("Vui lòng chọn môn học và chương / chủ đề từ danh mục.", true); return; }
            string pointControl = view.screen == TeacherScreen.Quiz ? "points" : "DiemToiDa";
            double points;
            if (!double.TryParse(Read(pointControl).Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out points)
                || double.IsNaN(points) || double.IsInfinity(points) || points <= 0 || points > 10000)
            { SetStatus("Điểm phải là số lớn hơn 0 và không quá 10.000.", true); return; }
            if (view.screen == TeacherScreen.Quiz && points != Math.Floor(points))
            { SetStatus("Điểm câu hỏi trắc nghiệm phải là số nguyên lớn hơn 0.", true); return; }

            if (view.screen == TeacherScreen.Quiz)
            {
                if (!Require("question", "nội dung câu hỏi") || !Require("option_a", "phương án A")
                    || !Require("option_b", "phương án B") || !Require("option_c", "phương án C")
                    || !Require("option_d", "phương án D")) return;
                string correct = "";
                foreach (var toggle in view.GetComponentsInChildren<Toggle>(true))
                    if (toggle.name.EndsWith("_Correct", StringComparison.Ordinal) && toggle.isOn)
                        correct = toggle.name.Substring(7, 1).ToUpperInvariant();
                if (correct.Length == 0) { SetStatus("Vui lòng chọn một đáp án đúng A, B, C hoặc D.", true); return; }
                var question = new TeacherQuestion
                {
                    id = Read("id"), monID = SelectedSubject(), chapter = SelectedChapter(), difficulty = Choice("difficulty"),
                    points = points.ToString(CultureInfo.InvariantCulture), question = Read("question"),
                    option_a = Read("option_a"), option_b = Read("option_b"), option_c = Read("option_c"), option_d = Read("option_d"),
                    correct_option = correct, explanation = Read("explanation")
                };
                SetBusy(true); SetStatus("Đang lưu câu hỏi…");
                service.SaveQuestion(question, result => Saved(result, "id"));
            }
            else if (view.screen == TeacherScreen.Essay)
            {
                if (!Require("Question", "nội dung câu hỏi") || !Require("DapAnThamKhao", "đáp án tham khảo")
                    || !Require("HuongDanChamAI", "hướng dẫn chấm AI") || !Require("NguonTao", "nguồn tạo")) return;
                var essay = new TeacherEssay
                {
                    CauHoiID = Read("CauHoiID"), monID = SelectedSubject(), chapter = SelectedChapter(), difficulty = Choice("difficulty"),
                    DiemToiDa = points.ToString(CultureInfo.InvariantCulture), TrangThai = Choice("TrangThai"), NguonTao = Read("NguonTao"),
                    Question = Read("Question"), DapAnThamKhao = Read("DapAnThamKhao"), HuongDanChamAI = Read("HuongDanChamAI"),
                    HinhAnh = imageData, GiaoVienID = Read("GiaoVienID"), NgayTao = Read("NgayTao")
                };
                SetBusy(true); SetStatus("Đang lưu câu hỏi…");
                service.SaveEssay(essay, result => Saved(result, "CauHoiID"));
            }
        }

        private void Saved(TeacherSaveResult result, string idField)
        {
            if (this == null) return;
            SetBusy(false);
            if (result == null) { SetStatus("Không nhận được phản hồi lưu. Nội dung vẫn được giữ trên màn hình.", true); return; }
            bool remoteSaved = result.Success && result.RemoteSaved;
            if ((remoteSaved || result.LocalDraftSaved) && !string.IsNullOrEmpty(result.Id))
            {
                Put(idField, result.Id);
                if (catalog != null)
                {
                    if (view.screen == TeacherScreen.Quiz)
                    {
                        catalog.ExistingQuestionIds.Add(result.Id);
                        int num = TeacherSheetService.ExtractQuizNumber(result.Id);
                        if (num > catalog.MaxQuizNumber) catalog.MaxQuizNumber = num;
                    }
                    else if (view.screen == TeacherScreen.Essay)
                    {
                        catalog.ExistingEssayIds.Add(result.Id);
                    }
                }
            }
            SetStatus(result.Message, !remoteSaved);
            if (result.LocalDraftSaved && !string.IsNullOrEmpty(result.Path))
                Debug.Log("Bản nháp câu hỏi: " + result.Path);
        }

        private void PickImage()
        {
            if (busy) return;
            try
            {
                string path = TeacherImagePicker.Pick();
                if (string.IsNullOrEmpty(path)) return;
                LoadImageFile(path);
            }
            catch (Exception exception) { SetStatus(exception.Message, true); }
        }

        // Keep file decoding and preview layout independent from the native file dialog.
        private void LoadImageFile(string path)
        {
            Texture2D nextTexture;
            string nextData = TeacherImagePicker.Read(path, out nextTexture);
            if (imageTexture != null) Destroy(imageTexture);
            imageTexture = nextTexture;
            imageData = nextData;
            if (view.imagePreview != null)
            {
                view.imagePreview.texture = imageTexture;
                view.imagePreview.gameObject.SetActive(true);
                var rect = view.imagePreview.rectTransform;
                float scale = Mathf.Min(previewSize.x / imageTexture.width, previewSize.y / imageTexture.height);
                Vector2 fitted = new Vector2(imageTexture.width * scale, imageTexture.height * scale);
                Vector2 center = previewPosition + Vector2.Scale(new Vector2(0.5f, 0.5f) - rect.pivot, previewSize);
                rect.sizeDelta = fitted;
                rect.anchoredPosition = center - Vector2.Scale(new Vector2(0.5f, 0.5f) - rect.pivot, fitted);
            }
            SetLabel("UploadHint", Path.GetFileName(path));
            SetUploadIcon(false);
            var remove = view.Button("RemoveImage");
            if (remove != null) remove.gameObject.SetActive(true);
            SetStatus("Đã chọn ảnh minh họa. Ảnh sẽ đi cùng câu hỏi khi lưu.");
        }

        private void RemoveImage()
        {
            if (busy) return;
            imageData = "";
            if (imageTexture != null) Destroy(imageTexture);
            imageTexture = null;
            if (view.imagePreview != null)
            {
                view.imagePreview.texture = null;
                view.imagePreview.rectTransform.sizeDelta = previewSize;
                view.imagePreview.rectTransform.anchoredPosition = previewPosition;
                view.imagePreview.gameObject.SetActive(false);
            }
            SetLabel("UploadHint", "Chưa chọn ảnh");
            SetUploadIcon(true);
            var remove = view.Button("RemoveImage");
            if (remove != null) remove.gameObject.SetActive(false);
        }

        private void LoadProgress(bool forceRefresh = false)
        {
            if (busy) return;
            SetBusy(true); SetStatus("Đang tải danh sách học sinh và tiến độ từ Google Sheet…");
            service.LoadCatalog((data, catalogError) =>
            {
                if (this == null) return;
                catalog = data;
                students.Clear();
                if (data != null && data.Students != null) students.AddRange(data.Students);
                service.LoadProgress((rows, progressError) =>
                {
                    if (this == null) return;
                    progress = rows ?? new List<TeacherProgress>();
                    foreach (string id in progress.Select(row => row.UserID).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct())
                        if (!students.Any(student => Same(student.UserID, id)))
                            students.Add(new TeacherStudent { UserID = id, Fullname = id, Username = id });
                    var classes = students.Select(student => student.Class).Where(value => !string.IsNullOrWhiteSpace(value))
                        .Distinct().OrderBy(value => value).ToList();
                    classes.Insert(0, "Tất cả lớp");
                    SetOptions("ClassFilter", classes);
                    var filter = view.Dropdown("ClassFilter");
                    if (filter != null) filter.interactable = classes.Count > 1;
                    var chapterOptions = progress.Select(row => row.ChuongChuDe).Where(value => !string.IsNullOrWhiteSpace(value))
                        .Distinct().OrderBy(value => value).ToList();
                    chapterOptions.Insert(0, "Tất cả chương / chủ đề");
                    SetOptions("ChapterFilter", chapterOptions);
                    var statuses = progress.Select(row => row.TrangThai).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct().ToList();
                    statuses.Insert(0, "Tất cả trạng thái");
                    SetOptions("StatusFilter", statuses);
                    SetBusy(false);
                    RenderStudents();
                    string error = string.Join("  ", new[] { catalogError, progressError }.Where(value => !string.IsNullOrWhiteSpace(value)));
                    SetStatus(error.Length > 0 ? error : "Đã cập nhật dữ liệu từ Google Sheet.", error.Length > 0);
                });
            }, forceRefresh);
        }

        private static bool Same(string left, string right) { return string.Equals(left, right, StringComparison.OrdinalIgnoreCase); }

        private void RenderStudents()
        {
            if (view.studentContent == null) return;
            ClearChildren(view.studentContent);
            studentRows.Clear();
            string search = Read("StudentSearch");
            var classFilter = view.Dropdown("ClassFilter");
            string className = classFilter != null && classFilter.value > 0 ? Choice("ClassFilter") : "";
            var visible = students.Where(student =>
                    (search.Length == 0 || (student.Fullname ?? "").IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
                    || (student.UserID ?? "").IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
                    || (student.Username ?? "").IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                    && (className.Length == 0 || Same(student.Class, className)))
                .OrderBy(student => student.Fullname).ToList();
            SetLabel("StudentCount", visible.Count + " học sinh");
            selectedStudent = selectedStudent == null ? visible.FirstOrDefault()
                : visible.FirstOrDefault(student => Same(student.UserID, selectedStudent.UserID)) ?? visible.FirstOrDefault();
            for (int i = 0; i < visible.Count; i++)
            {
                TeacherStudent student = visible[i];
                var row = MakeImage(view.studentContent, "Student_" + student.UserID, 0, i * 80, 374, 70,
                    selectedStudent != null && Same(selectedStudent.UserID, student.UserID) ? view.rowSelected : view.rowNormal);
                var button = row.gameObject.AddComponent<Button>();
                row.raycastTarget = true;
                button.targetGraphic = row;
                button.onClick.AddListener(() => SelectStudent(student));
                MakeImage(row.transform, "Avatar", 18, 17, 34, 34, view.iconPerson);
                MakeText(row.transform, "Name", string.IsNullOrWhiteSpace(student.Fullname) ? student.UserID : student.Fullname,
                    67, 9, 288, 30, 21, Cream, true);
                MakeText(row.transform, "Id", student.UserID + (string.IsNullOrWhiteSpace(student.Class) ? "" : "  ·  " + student.Class),
                    67, 38, 288, 24, 16, Muted);
                studentRows[student.UserID ?? ""] = row;
            }
            view.studentContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(80, visible.Count * 80));
            ScrollToTop(view.studentContent);
            if (visible.Count == 0) MakeText(view.studentContent, "Empty", "Không có học sinh phù hợp.", 16, 14, 345, 90, 20, Muted);
            SelectStudent(selectedStudent);
        }

        private void SelectStudent(TeacherStudent student)
        {
            selectedStudent = student;
            foreach (var row in studentRows)
                if (row.Value != null) row.Value.sprite = student != null && Same(row.Key, student.UserID) ? view.rowSelected : view.rowNormal;
            if (student == null) { ClearProgressSelection(); return; }
            SetLabel("StudentName", string.IsNullOrWhiteSpace(student.Fullname) ? student.UserID : student.Fullname);
            SetLabel("UserID", "Mã học sinh: " + student.UserID + (string.IsNullOrWhiteSpace(student.Class) ? "" : "  ·  " + student.Class));
            RenderProgress();
        }

        private void ClearProgressSelection()
        {
            SetLabel("StudentName", "Chọn học sinh"); SetLabel("UserID", "Mã học sinh: —");
            SetLabel("TopicsCount", "0"); SetLabel("CompletedCount", "0 / 0"); SetLabel("AttemptsCount", "0");
            SetLabel("ProgressSummary", "Chọn học sinh để xem tiến độ học tập.");
            if (view.progressContent != null)
            {
                ClearChildren(view.progressContent);
                ScrollToTop(view.progressContent);
            }
        }

        private static bool Completed(TeacherProgress row)
        {
            string status = (row.TrangThai ?? "").ToLowerInvariant();
            if (status.Contains("chưa") || status.Contains("không")) return false;
            return status.Contains("hoàn thành") || status.Contains("hoanthanh") || status == "completed" || status == "complete";
        }

        private void RenderProgress()
        {
            if (selectedStudent == null || view.progressContent == null) return;
            var all = progress.Where(row => Same(row.UserID, selectedStudent.UserID)).ToList();
            SetLabel("TopicsCount", all.Count.ToString());
            SetLabel("CompletedCount", all.Count(Completed) + " / " + all.Count);
            SetLabel("AttemptsCount", all.Sum(row => Number(row.SoLanLam)).ToString("0", CultureInfo.InvariantCulture));
            var chapterFilter = view.Dropdown("ChapterFilter");
            var statusFilter = view.Dropdown("StatusFilter");
            string chapter = chapterFilter != null && chapterFilter.value > 0 ? Choice("ChapterFilter") : "";
            string status = statusFilter != null && statusFilter.value > 0 ? Choice("StatusFilter") : "";
            var rows = all.Where(row => (chapter.Length == 0 || Same(row.ChuongChuDe, chapter))
                && (status.Length == 0 || Same(row.TrangThai, status))).ToList();
            ClearChildren(view.progressContent);
            for (int i = 0; i < rows.Count; i++)
            {
                var record = rows[i];
                var row = MakeImage(view.progressContent, "Progress_" + record.TienDoID, 0, i * 112, 1290, 104, view.rowNormal);
                MakeText(row.transform, "TienDoID", record.TienDoID, 18, 10, 350, 24, 16, Mint, true);
                MakeText(row.transform, "ChuongChuDe", record.ChuongChuDe, 18, 39, 352, 59, 18, Cream);
                MakeText(row.transform, "SoLanLam", record.SoLanLam, 396, 37, 76, 34, 22, Cream, false, TextAlignmentOptions.Center);
                MakeText(row.transform, "DiemCaoNhat", record.DiemCaoNhat, 492, 37, 88, 34, 22, Cream, true, TextAlignmentOptions.Center);
                MakeText(row.transform, "DiemGanNhat", record.DiemGanNhat, 600, 37, 88, 34, 22, Cream, true, TextAlignmentOptions.Center);
                float percent = Percentage(record.TyLeDung);
                MakeText(row.transform, "TyLeDung", percent.ToString("0.#", CultureInfo.InvariantCulture) + "%", 708, 21, 122, 30, 21, Mint, true, TextAlignmentOptions.Center);
                MakeImage(row.transform, "AccuracyTrack", 718, 60, 104, 13, view.progressTrack);
                if (percent > 0) MakeImage(row.transform, "AccuracyFill", 718, 60, 104 * percent / 100, 13, view.progressFill);
                bool completed = Completed(record);
                MakeImage(row.transform, "Status", 858, 32, 204, 42, completed ? view.tagGreen : view.tagAmber);
                MakeText(row.transform, "TrangThai", record.TrangThai, 866, 37, 188, 33, 16, completed ? Cream : Gold, true, TextAlignmentOptions.Center);
                MakeText(row.transform, "LanHocCuoi", FormatDate(record.LanHocCuoi), 1086, 23, 192, 63, 18, Cream, false, TextAlignmentOptions.Center);
            }
            view.progressContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(112, rows.Count * 112));
            ScrollToTop(view.progressContent);
            if (rows.Count == 0) MakeText(view.progressContent, "Empty", all.Count == 0 ? "Học sinh chưa có bản ghi tiến độ học tập." : "Không có tiến độ phù hợp với bộ lọc.", 20, 22, 1240, 65, 23, Muted);
            SetLabel("ProgressSummary", "Hiển thị " + rows.Count + " / " + all.Count + " bản ghi  ·  Tỷ lệ đúng thể hiện độ chính xác khi làm bài.");
        }

        private static double Number(string value)
        {
            double number;
            return double.TryParse((value ?? "").Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out number)
                && !double.IsNaN(number) && !double.IsInfinity(number) ? number : 0;
        }

        private static float Percentage(string value)
        {
            bool explicitPercent = (value ?? "").Contains("%");
            double number = Number((value ?? "").Replace("%", "").Trim());
            if (!explicitPercent && number >= 0 && number <= 1) number *= 100;
            return Mathf.Clamp((float)number, 0, 100);
        }

        private static string FormatDate(string value)
        {
            DateTime date;
            return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out date)
                ? date.ToString("dd/MM/yyyy\nHH:mm", CultureInfo.InvariantCulture) : (value ?? "—");
        }

        private void SetBusy(bool value)
        {
            busy = value;
            if (view.screen == TeacherScreen.Quiz || view.screen == TeacherScreen.Essay)
            {
                foreach (var input in view.GetComponentsInChildren<TMP_InputField>(true))
                    if (!IsSettingsControl(input.transform)) input.interactable = !value;
                foreach (var dropdown in view.GetComponentsInChildren<TMP_Dropdown>(true)) dropdown.interactable = !value;
                foreach (var toggle in view.GetComponentsInChildren<Toggle>(true)) toggle.interactable = !value;
            }
            foreach (string name in new[] { "SaveButton", "ResetButton", "PickImage", "ImageUploadArea", "RemoveImage", "RefreshProgress", "BackButton", "HomeButton", "LogoutButton", "RankButton" })
            {
                var button = view.Button(name);
                if (button != null) button.interactable = !value;
            }
        }

        private void SetStatus(string message, bool error = false)
        {
            var label = view.Label("StatusMessage");
            if (label != null) { label.text = message ?? ""; label.color = error ? new Color32(255, 207, 142, 255) : Cream; label.richText = false; }
        }

        private void SetLabel(string name, string value)
        {
            var label = view.Label(name);
            if (label != null) { label.text = value ?? ""; label.richText = false; }
        }

        private void Navigate(string scene)
        {
            if (busy) return;
            if (!Application.CanStreamedLevelBeLoaded(scene)) { SetStatus("Màn hình " + scene + " chưa có trong danh sách Scene của bản chạy.", true); return; }
            SceneManager.LoadScene(scene);
        }

        private void Logout()
        {
            if (busy) return;
            GameSession.ClearUserSession();
            TeacherSheetService.ClearWriteSession();
            foreach (string key in new[] { "Role", "Username", "Fullname", "UserID", "TeacherID" }) PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
            Navigate("SampleScene");
        }

        private void OpenSettings()
        {
            if (settingsPanel != null) { settingsPanel.SetActive(true); settingsPanel.transform.SetAsLastSibling(); return; }
            var shade = MakeImage(view.transform, "TeacherSettings", 0, 0, 1920, 1080, null);
            shade.color = new Color(0, 0, 0, 0.75f); shade.raycastTarget = true;
            settingsPanel = shade.gameObject;
            var panel = MakeImage(shade.transform, "Panel", 460, 135, 1000, 810, view.rowNormal);
            MakeText(panel.transform, "Title", "Cài đặt giáo viên", 35, 25, 930, 60, 36, Gold, true, TextAlignmentOptions.Center);
            MakeText(panel.transform, "VolumeLabel", "Âm lượng", 50, 110, 500, 38, 25, Cream);
            var track = MakeImage(panel.transform, "Volume", 50, 177, 590, 20, view.progressTrack);
            var fill = MakeImage(track.transform, "Fill", 0, 0, 590, 20, view.progressFill);
            var handle = MakeImage(track.transform, "Handle", 0, -7, 30, 34, null);
            handle.color = Gold;
            var slider = track.gameObject.AddComponent<Slider>();
            track.raycastTarget = true; handle.raycastTarget = true;
            slider.fillRect = fill.rectTransform; slider.handleRect = handle.rectTransform; slider.targetGraphic = handle;
            slider.minValue = 0; slider.maxValue = 1; slider.value = AudioListener.volume;
            slider.onValueChanged.AddListener(value => { AudioListener.volume = value; PlayerPrefs.SetFloat("TeacherVolume", value); });
            var mute = MakeImage(panel.transform, "Mute", 705, 154, 245, 58, view.tagAmber);
            var muteText = MakeText(mute.transform, "Text", AudioListener.volume > 0 ? "Tắt âm" : "Bật âm", 10, 6, 225, 46, 24, Cream, true, TextAlignmentOptions.Center);
            var muteButton = mute.gameObject.AddComponent<Button>(); muteButton.targetGraphic = mute;
            mute.raycastTarget = true;
            muteButton.onClick.AddListener(() => { slider.value = AudioListener.volume > 0 ? 0 : 1; muteText.text = AudioListener.volume > 0 ? "Tắt âm" : "Bật âm"; });
            MakeText(panel.transform, "ConnectionTitle", "Kết nối lưu câu hỏi vào Google Sheet", 50, 235, 900, 42, 26, Gold, true);
            var endpoint = MakeInput(panel.transform, "TeacherEndpoint", service.ApiUrl, "URL dịch vụ giáo viên (Apps Script /exec)", 50, 286, 900, 58);
            MakeText(panel.transform, "UsernameLabel", "Tài khoản giáo viên", 50, 359, 420, 32, 21, Cream);
            MakeText(panel.transform, "PasswordLabel", "Mật khẩu", 525, 359, 425, 32, 21, Cream);
            var username = MakeInput(panel.transform, "TeacherUsername", string.IsNullOrEmpty(GameSession.Username)
                ? PlayerPrefs.GetString("Username", "") : GameSession.Username, "Tên đăng nhập", 50, 397, 425, 58);
            var password = MakeInput(panel.transform, "TeacherPassword", "", "Mật khẩu không được lưu", 525, 397, 425, 58);
            password.contentType = TMP_InputField.ContentType.Password;
            password.ForceLabelUpdate();
            var connectionStatus = MakeText(panel.transform, "ConnectionStatus", service.HasWriteSession
                ? "Đã xác thực dịch vụ lưu câu hỏi."
                : "Dữ liệu học tập được đọc từ Sheet đã liên kết. Nhập dịch vụ lưu và xác thực để ghi câu hỏi trực tiếp.",
                50, 561, 900, 112, 21, Muted);
            var connect = MakeImage(panel.transform, "Connect", 50, 485, 330, 60, view.tagGreen);
            connect.raycastTarget = true;
            MakeText(connect.transform, "Text", "Kết nối Google Sheet", 12, 6, 306, 48, 23, Cream, true, TextAlignmentOptions.Center);
            var connectButton = connect.gameObject.AddComponent<Button>(); connectButton.targetGraphic = connect;
            connectButton.onClick.AddListener(() =>
            {
                Uri uri;
                string url = endpoint.text.Trim();
                if (!Uri.TryCreate(url, UriKind.Absolute, out uri) || uri.Scheme != "https"
                    || uri.Host != "script.google.com" || !uri.AbsolutePath.EndsWith("/exec", StringComparison.OrdinalIgnoreCase))
                { connectionStatus.text = "Nhập URL triển khai Apps Script dạng https://script.google.com/macros/s/…/exec."; return; }
                service.teacherApiUrl = url;
                PlayerPrefs.SetString(TeacherSheetService.EndpointPreference, url);
                PlayerPrefs.Save();
                connectButton.interactable = false;
                connectionStatus.text = "Đang kiểm tra kết nối Google Sheet…";
                string secret = password.text;
                password.SetTextWithoutNotify("");
                service.AuthenticateTeacher(username.text.Trim(), secret, (ok, message) =>
                {
                    if (this == null) return;
                    connectButton.interactable = true;
                    connectionStatus.text = message;
                    connectionStatus.color = ok ? Mint : Gold;
                });
            });
            var close = MakeImage(panel.transform, "Close", 705, 700, 245, 58, view.tagGreen);
            MakeText(close.transform, "Text", "Đóng", 10, 6, 225, 46, 24, Cream, true, TextAlignmentOptions.Center);
            var closeButton = close.gameObject.AddComponent<Button>(); closeButton.targetGraphic = close;
            close.raycastTarget = true;
            closeButton.onClick.AddListener(() => { password.SetTextWithoutNotify(""); PlayerPrefs.Save(); settingsPanel.SetActive(false); });
        }

        private TMP_InputField MakeInput(Transform parent, string name, string text, string hint, float x, float y, float width, float height)
        {
            var background = MakeImage(parent, name, x, y, width, height, null);
            background.color = new Color32(247, 236, 210, 255); background.raycastTarget = true;
            var area = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            area.transform.SetParent(background.transform, false);
            SetRect((RectTransform)area.transform, 15, 5, width - 30, height - 10);
            var content = MakeText(area.transform, "Text", text, 0, 0, width - 30, height - 10, 22, new Color32(38, 60, 60, 255));
            var placeholder = MakeText(area.transform, "Placeholder", hint, 0, 0, width - 30, height - 10, 20, new Color32(111, 121, 109, 255));
            var field = background.gameObject.AddComponent<TMP_InputField>();
            field.textViewport = (RectTransform)area.transform;
            field.textComponent = content; field.placeholder = placeholder; field.targetGraphic = background;
            field.lineType = TMP_InputField.LineType.SingleLine;
            field.text = text ?? "";
            return field;
        }

        private void SetUploadIcon(bool visible)
        {
            foreach (var item in view.GetComponentsInChildren<Transform>(true))
                if (item.name == "UploadImageIcon") { item.gameObject.SetActive(visible); break; }
        }

        private Image MakeImage(Transform parent, string name, float x, float y, float width, float height, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            SetRect((RectTransform)go.transform, x, y, width, height);
            var image = go.GetComponent<Image>(); image.sprite = sprite; image.raycastTarget = false;
            if (sprite != null) image.type = Image.Type.Sliced;
            return image;
        }

        private TMP_Text MakeText(Transform parent, string name, string text, float x, float y, float width, float height,
            int size, Color color, bool bold = false, TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false); SetRect((RectTransform)go.transform, x, y, width, height);
            var label = go.GetComponent<TextMeshProUGUI>();
            label.font = bold && view.boldFont != null ? view.boldFont : view.regularFont;
            label.fontSize = size; label.color = color; label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.Normal; label.overflowMode = TextOverflowModes.Ellipsis;
            if (name == "Name" || name == "ChuongChuDe" || name == "TrangThai")
            {
                label.enableAutoSizing = true;
                label.fontSizeMin = name == "TrangThai" ? 14 : 16;
                label.fontSizeMax = size;
                if (name != "ChuongChuDe") label.textWrappingMode = TextWrappingModes.NoWrap;
            }
            label.richText = false; label.raycastTarget = false; label.text = text ?? "";
            return label;
        }

        private static void SetRect(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height);
        }

        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--) { var child = parent.GetChild(i).gameObject; child.SetActive(false); Destroy(child); }
        }

        private bool IsSettingsControl(Transform control)
        {
            return settingsPanel != null && control.IsChildOf(settingsPanel.transform);
        }

        private static void ScrollToTop(RectTransform content)
        {
            var scroll = content.GetComponentInParent<ScrollRect>();
            if (scroll != null)
            {
                scroll.StopMovement();
                scroll.verticalNormalizedPosition = 1;
            }
            var position = content.anchoredPosition;
            position.y = 0;
            content.anchoredPosition = position;
        }

        private void OnDestroy()
        {
            if (imageTexture != null) Destroy(imageTexture);
        }
    }
}
