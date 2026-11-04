using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>Sheet mapping and filters used only by the four Biology quiz scenes.</summary>
public static class BiologyQuestionBank
{
    public const string CsvUrl = "https://docs.google.com/spreadsheets/d/1DcJwlN_fUDdcaQ6IfnlkMJApgWuEUqMhyZv9Kn8vfWo/gviz/tq?tqx=out:csv&sheet=Questions&headers=1";

    static string Normalize(string text)
    {
        var value = new StringBuilder();
        foreach (char c in (text ?? "").Trim().Normalize(NormalizationForm.FormD))
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                value.Append(c == 'đ' || c == 'Đ' ? 'd' : char.ToLowerInvariant(c));
        return Regex.Replace(value.ToString().Normalize(NormalizationForm.FormC), @"\s+", " ");
    }
    static string Header(string value) { return Regex.Replace(Normalize(value), "[^a-z0-9]", ""); }
    public static bool IsBiology(string value) { return new[] { "sinh hoc", "sinhhoc", "sinh", "biology", "bio" }.Contains(Normalize(value)); }

    public static int CorrectIndex(QuestionData question)
    {
        if (question == null) return -1;
        string correct = (question.correct ?? "").Trim().ToUpperInvariant();
        if (correct.Length == 1) return "ABCD".IndexOf(correct[0]);
        var letter = Regex.Match(correct, @"^([ABCD])\s*[).:\-]\s*");
        return letter.Success ? "ABCD".IndexOf(letter.Groups[1].Value[0]) : -1;
    }

    public static bool MatchesChapter(string topic, string selected)
    {
        string filter = Normalize(selected), actual = Normalize(topic);
        if (filter.Length == 0 || filter == "all" || filter.StartsWith("tat ca")) return true;
        if (actual.Length == 0) return false;
        var a = Regex.Match(actual, @"(?:chuong|chapter)\s*(\d+)");
        var b = Regex.Match(filter, @"(?:chuong|chapter)\s*(\d+)");
        return a.Success && b.Success ? int.Parse(a.Groups[1].Value) == int.Parse(b.Groups[1].Value) : actual == filter;
    }

    public static List<QuestionData> Select(IEnumerable<QuestionData> source, string chapter, string difficulty)
    {
        string filter = Normalize(difficulty);
        bool all = filter.Length == 0 || filter == "all" || filter == "tong hop" || filter.StartsWith("tat ca");
        return (source ?? Enumerable.Empty<QuestionData>()).Where(q => q != null && IsBiology(q.monId)
            && !string.IsNullOrWhiteSpace(q.id) && !string.IsNullOrWhiteSpace(q.question)
            && new[] { q.optionA, q.optionB, q.optionC, q.optionD }.All(o => !string.IsNullOrWhiteSpace(o))
            && CorrectIndex(q) >= 0 && MatchesChapter(q.topic, chapter)
            && (all || Normalize(q.difficulty) == filter)).ToList();
    }

    public static List<QuestionData> ParseCsv(string csv)
    {
        if (string.IsNullOrWhiteSpace(csv) || csv.TrimStart().StartsWith("<", StringComparison.Ordinal)) throw new FormatException("Google Sheet chưa trả dữ liệu trắc nghiệm hợp lệ.");
        var rows = Rows(csv.TrimStart('\uFEFF'));
        if (rows.Count == 0) throw new FormatException("Google Sheet chưa có tiêu đề câu hỏi.");
        var columns = rows[0].Select((value, index) => new { value = Header(value), index })
            .Where(c => c.value.Length > 0).GroupBy(c => c.value).ToDictionary(c => c.Key, c => c.First().index);
        Func<string[], string[], string> valueOf = (row, names) => {
            foreach (string name in names) { int index; if (columns.TryGetValue(Header(name), out index) && index < row.Length) return row[index].Trim(); }
            return "";
        };
        foreach (string name in new[] { "id", "monID", "question", "option_a", "option_b", "option_c", "option_d", "correct_option" })
            if (!columns.ContainsKey(Header(name)) && !(name == "correct_option" && columns.ContainsKey("correct")))
                throw new FormatException("Bảng Questions thiếu cột " + name + ".");
        var questions = new List<QuestionData>();
        foreach (var row in rows.Skip(1)) {
            int points;
            questions.Add(new QuestionData {
                id = valueOf(row, new[] { "id" }), monId = valueOf(row, new[] { "monID" }),
                topic = valueOf(row, new[] { "Chương / Chủ đề", "ChuongChuDe", "topic" }), difficulty = valueOf(row, new[] { "difficulty" }),
                question = valueOf(row, new[] { "question" }), optionA = valueOf(row, new[] { "option_a" }), optionB = valueOf(row, new[] { "option_b" }),
                optionC = valueOf(row, new[] { "option_c" }), optionD = valueOf(row, new[] { "option_d" }), correct = valueOf(row, new[] { "correct_option", "correct" }),
                explanation = valueOf(row, new[] { "explanation" }), points = int.TryParse(valueOf(row, new[] { "points" }), out points) ? points : 10
            });
        }
        return Select(questions, "Tất cả", "Tổng hợp");
    }

    static List<string[]> Rows(string csv)
    {
        var rows = new List<string[]>(); var row = new List<string>(); var field = new StringBuilder(); bool quoted = false;
        for (int i = 0; i < csv.Length; i++) {
            char c = csv[i];
            if (c == '"') { if (quoted && i + 1 < csv.Length && csv[i + 1] == '"') { field.Append('"'); i++; } else quoted = !quoted; }
            else if (!quoted && (c == ',' || c == '\r' || c == '\n')) {
                row.Add(field.ToString()); field.Clear();
                if (c != ',') { if (c == '\r' && i + 1 < csv.Length && csv[i + 1] == '\n') i++; rows.Add(row.ToArray()); row.Clear(); }
            } else field.Append(c);
        }
        if (quoted) throw new FormatException("Dữ liệu CSV chưa đóng ngoặc kép.");
        if (field.Length > 0 || row.Count > 0) { row.Add(field.ToString()); rows.Add(row.ToArray()); }
        return rows;
    }
}
