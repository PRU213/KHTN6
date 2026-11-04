using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

// The submission lifecycle belongs only to the Biology TuLuan screen.
public partial class TuLuanController
{
    private Coroutine submissionCoroutine;
    private UnityWebRequest activeSubmissionRequest;
    private BiologySubmissionNotice submissionNotice;
    private float submissionDeadline;
    [SerializeField] private bool submissionControlsLocked;
    [SerializeField] private bool pauseWasInteractable, answerWasInteractable, imageWasInteractable;
    private string lastSaveResponseJson;
    [SerializeField] private string submittedAnswerText;
    [SerializeField] private string submittedQuestionId;
    [SerializeField] private string submittedUserId;

    private void BeginBiologySubmission(string answer, bool hasImage)
    {
        // A retry of the same body retains its ID; an edited answer is a new submission.
        string questionId = currentQuestion != null ? currentQuestion.id : "";
        string userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(questionId)) { ShowGradingFailure("Chưa tải được câu hỏi. Hãy mở lại bài trước khi nộp."); return; }
        if (submittedAnswerText != null && (submittedAnswerText != answer || submittedQuestionId != questionId || submittedUserId != userId))
            currentSubmissionId = Guid.NewGuid().ToString();
        submittedAnswerText = answer; submittedQuestionId = questionId; submittedUserId = userId;
        submissionDeadline = Time.realtimeSinceStartup + (hasImage ? 105f : 65f);
        isSubmitting = true;
        SetSubmissionControls(true);
        submissionCoroutine = StartCoroutine(GuardBiologySubmission(SubmitRoutine(answer, hasImage)));
    }

    private void SetSubmissionControls(bool locked)
    {
        if (locked && !submissionControlsLocked) {
            pauseWasInteractable = btnTamDung != null && btnTamDung.interactable;
            answerWasInteractable = inputCauTraLoi != null && inputCauTraLoi.interactable;
            imageWasInteractable = btnTaiAnh != null && btnTaiAnh.interactable;
        }
        if (btnNopBai != null) btnNopBai.interactable = !locked;
        if (btnTamDung != null) btnTamDung.interactable = !locked && pauseWasInteractable;
        if (inputCauTraLoi != null) inputCauTraLoi.interactable = !locked && answerWasInteractable;
        if (btnTaiAnh != null) btnTaiAnh.interactable = !locked && imageWasInteractable;
        submissionControlsLocked = locked;
    }

    private void FinishSubmissionUI()
    {
        isSubmitting = false;
        submissionCoroutine = null;
        activeSubmissionRequest = null;
        HideLoading();
        if (submissionControlsLocked) SetSubmissionControls(false);
        else if (btnNopBai != null) btnNopBai.interactable = true;
    }

    // Flatten nested iterators so exceptions in upload/save/polling also release the loading screen.
    private IEnumerator GuardBiologySubmission(IEnumerator routine)
    {
        var stack = new Stack<IEnumerator>(); stack.Push(routine);
        string failure = null;
        try {
            while (stack.Count > 0) {
                object next = null; bool moved = false;
                try { moved = stack.Peek().MoveNext(); if (moved) next = stack.Peek().Current; }
                catch (Exception error) { failure = "Không đọc được kết quả chấm. Bài làm vẫn được giữ để thử lại."; Debug.LogException(error); }
                if (failure != null) break;
                if (!moved) { (stack.Pop() as IDisposable)?.Dispose(); continue; }
                if (next is IEnumerator child) stack.Push(child);
                else yield return next;
            }
        } finally {
            while (stack.Count > 0) (stack.Pop() as IDisposable)?.Dispose();
            FinishSubmissionUI();
        }
        if (failure != null) ShowGradingFailure(failure);
    }

    private void Update()
    {
        if (!isSubmitting) return;
        if (submissionCoroutine == null) {
            // Unity reloads scripts without keeping a running coroutine.
            FinishSubmissionUI();
            ShowGradingFailure("Lượt chấm bị gián đoạn. Bài làm đã được giữ; hãy bấm Thử chấm lại để nhận kết quả.");
        } else if (Time.realtimeSinceStartup >= submissionDeadline) {
            activeSubmissionRequest?.Abort();
            StopCoroutine(submissionCoroutine);
            FinishSubmissionUI();
            ShowGradingFailure("Dịch vụ chấm phản hồi quá lâu. Bài làm được giữ nguyên; em có thể thử lại để nhận điểm và nhận xét.");
        }
    }

    private void OnDisable()
    {
        if (!isSubmitting) return;
        activeSubmissionRequest?.Abort();
        if (submissionCoroutine != null) StopCoroutine(submissionCoroutine);
        FinishSubmissionUI();
    }

    private void ShowGradingFailure(string message)
    {
        HideLoading();
        if (submissionNotice == null) {
            var canvas = GetComponentInParent<UnityEngine.Canvas>();
            if (canvas == null && inputCauTraLoi != null) canvas = inputCauTraLoi.GetComponentInParent<UnityEngine.Canvas>();
            if (canvas == null) canvas = FindAnyObjectByType<UnityEngine.Canvas>();
            submissionNotice = BiologySubmissionNotice.Attach(canvas, GetPreferredFont());
        }
        if (submissionNotice != null) submissionNotice.Show(message, OnSubmitClicked);
        else ShowNotification(message);
    }

    private void AcceptBiologyGrade(TuLuanSubmissionData submission, TuLuanGradingResult result)
    {
        submission.diemAI = result.score; submission.nhanXetAI = result.reviewText;
        submission.goiYCaiThien = result.improvementSuggestion; submission.chiTietCham = result.detailJson;
        submission.ketQua = result.status; submission.trangThaiCham = "Completed";
        lastSubmission = submission;
        FinishSubmissionUI();
        // ShowResultPopup captures this ID for the required AI lesson and copying.
        currentSubmissionId = submission.submissionId;
        ShowResultPopup(result);
    }
}
