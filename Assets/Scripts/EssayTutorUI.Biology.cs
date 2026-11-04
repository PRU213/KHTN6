using System;

// Biology opts into the existing authenticated tutor; other subjects keep their original entry points.
public sealed partial class EssayTutorUI
{
    public static void OpenBiology(string submissionId)
    {
        if (string.IsNullOrWhiteSpace(submissionId)) return;
        var ui = Get();
        ui.Cancel(); ui.teacher = false; ui.username = CurrentUsername;
        ui.planetReviewLoaded = null; ui.quizReviewBatches = null; ui.playAllowed = null;
        ui.current = null; ui.listMode = false; ui.Clear(); ui.retryAnswer = null;
        ui.gameObject.SetActive(true); ui.SetPlayGateVisible(false);
        ui.title.text = "SINH HỌC • AI HƯỚNG DẪN VÀ CHÉP LẠI";
        ui.StartCoroutine(ui.Load("biology-start", new Request { id = submissionId.Trim(), username = ui.username }));
    }

    TuLuanController FindBiologyForCurrentLesson()
    {
        if (current == null || !(current.sourceId ?? "").StartsWith("biology:", StringComparison.Ordinal)) return null;
        foreach (var biology in FindObjectsByType<TuLuanController>(UnityEngine.FindObjectsInactive.Exclude))
            if (biology.BindBiologyTutorial(current.id, current.sourceId)) return biology;
        return null;
    }
}
