using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>Strict result decoding for the Biology submission endpoint and its Sheet fallback.</summary>
public static class BiologyGradingStatus
{
    [Serializable] class Reply {
        public bool success, completed;
        public string submissionId, status, gradingStatus, message, feedback, improvements, criteria;
    }

    public static string FailureMessage(string detail)
    {
        detail = detail ?? "";
        if (detail.IndexOf("script.external_request", StringComparison.OrdinalIgnoreCase) >= 0
            || detail.IndexOf("UrlFetchApp", StringComparison.OrdinalIgnoreCase) >= 0)
            return "Dịch vụ chấm chưa được cấp quyền gọi AI. Bài làm đã được giữ nguyên; người quản lý cần kích hoạt lại dịch vụ chấm.";
        return "AI chưa chấm được bài. Bài làm đã được giữ nguyên; em có thể thử lại sau ít phút.";
    }

    public static bool Terminal(string state) => string.Equals(state, "Failed", StringComparison.OrdinalIgnoreCase)
        || string.Equals(state, "Invalid", StringComparison.OrdinalIgnoreCase);

    public static bool ReadJson(string json, string expectedId, out TuLuanGradingResult result, out string state, out string error)
    {
        result = null; state = "Unknown"; error = null;
        Reply reply;
        try { reply = JsonUtility.FromJson<Reply>(json); } catch (Exception) { return false; }
        if (reply == null || !reply.success || reply.submissionId != expectedId) return false;
        state = string.IsNullOrWhiteSpace(reply.gradingStatus) ? reply.status : reply.gradingStatus;
        if (Terminal(state)) { error = FailureMessage(reply.message); return false; }
        if (!string.Equals(state, "Completed", StringComparison.OrdinalIgnoreCase)) return false;
        var score = Regex.Match(json, "\"score\"\\s*:\\s*(-?(?:0|[1-9]\\d*)(?:\\.\\d+)?(?:[eE][+-]?\\d+)?)(?=\\s*[,}])");
        return Completed(score.Success ? score.Groups[1].Value : "", reply.feedback, reply.improvements, reply.criteria, out result, out state, out error);
    }

    public static bool ReadRows(List<string[]> rows, string expectedId, out TuLuanGradingResult result, out string state, out string error)
    {
        result = null; state = "Pending"; error = null;
        if (rows == null || rows.Count < 2) return false;
        var headers = rows[0].Select(value => (value ?? "").Trim().Trim('\uFEFF').Replace(" ", "").ToLowerInvariant()).ToArray();
        Func<string[], string, string> cell = (row, name) => {
            int column = Array.IndexOf(headers, name.ToLowerInvariant());
            return column >= 0 && column < row.Length ? row[column].Trim() : "";
        };
        var matches = rows.Skip(1).Where(row => cell(row, "SubmissionID") == expectedId).ToArray();
        if (matches.Length == 0) return false;
        if (matches.Length != 1) { state = "Invalid"; error = "Mã bài nộp bị trùng trong Sheet; chưa thể nhận điểm."; return false; }
        var data = matches[0]; state = cell(data, "TrangThaiCham");
        if (Terminal(state)) { error = FailureMessage(cell(data, "NhatKyLoiChamAI")); return false; }
        if (!string.Equals(state, "Completed", StringComparison.OrdinalIgnoreCase)) return false;
        return Completed(cell(data, "DiemAI"), cell(data, "NhanXetAI"), cell(data, "GoiYCaiThien"), cell(data, "ChiTietCham"), out result, out state, out error);
    }

    static bool Completed(string rawScore, string feedback, string improvements, string criteria,
        out TuLuanGradingResult result, out string state, out string error)
    {
        result = null; error = null; state = "Invalid";
        float score;
        if (!float.TryParse((rawScore ?? "").Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out score)
            || float.IsNaN(score) || float.IsInfinity(score) || score < 0 || score > 10 || string.IsNullOrWhiteSpace(feedback)) {
            error = "Dịch vụ báo chấm xong nhưng chưa trả đủ điểm và nhận xét hợp lệ. Bài làm vẫn được giữ để thử lại.";
            return false;
        }
        result = new TuLuanGradingResult { score = score, maxScore = 10, reviewText = feedback,
            improvementSuggestion = improvements ?? "", detailJson = criteria ?? "", status = score >= 5 ? "ĐẠT" : "CHƯA ĐẠT", isMock = false };
        state = "Completed"; return true;
    }
}
