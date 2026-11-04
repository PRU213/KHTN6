using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Instructions attached only by the Biology essay controller.</summary>
public sealed class BiologyEssayGuide : MonoBehaviour
{
    GameObject shade;
    TMP_Text instructions, actionLabel;
    Action remediation;
    TMP_FontAsset font;

    public static BiologyEssayGuide Attach(Canvas parent, TMP_FontAsset font)
    {
        if (parent == null) return null;
        var go = new GameObject("Biology essay instructions", typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        var canvas = go.AddComponent<Canvas>(); canvas.overrideSorting = true; canvas.sortingOrder = 1400;
        go.AddComponent<GraphicRaycaster>();
        var guide = go.AddComponent<BiologyEssayGuide>(); guide.font = font; guide.Build();
        return guide;
    }

    public void SetRemediationAction(Action action)
    {
        remediation = action;
        instructions.text = action == null
            ? "1. Đọc kỹ câu hỏi, viết câu trả lời hoặc đính kèm ảnh bài làm rồi bấm Nộp bài.\n\n2. Điểm từ 5 trở lên: tiếp tục màn chơi như trước; 6 điểm vẫn được qua.\n\n3. Dưới 5 điểm: AI hướng dẫn từng bước. Em trả lời các câu kiểm tra để hiểu bài.\n\n4. Sau khi học lại, viết tay nội dung chương và đáp án đúng một lần. Có thể chụp nhiều trang; nộp đủ các trang để AI kiểm tra. Chỉ khi đạt cả hai phần mới được sang câu tiếp theo."
            : "Bài tự luận dưới 5 điểm cần hoàn thành trước khi chuyển câu.\n\nAI sẽ hướng dẫn từng bước và kiểm tra lại kiến thức. Sau đó, em viết tay nội dung chương cùng đáp án đúng một lần.\n\nNộp đủ ảnh các trang đã viết để AI xác nhận. Đóng cửa sổ hoặc vào lại màn này vẫn giữ nhiệm vụ đang làm.";
        actionLabel.text = action == null ? "Đã hiểu • Bắt đầu làm bài" : "Tiếp tục học cùng AI";
    }

    public void Show() { if (shade != null) { shade.SetActive(true); transform.SetAsLastSibling(); } }

    void Build()
    {
        shade = Box(transform, "Guide shade", 0, 0, 1, 1, new Color(0, .035f, .025f, .94f)).gameObject;
        var card = Box(shade.transform, "Instructions", .14f, .11f, .86f, .89f, new Color(.08f, .19f, .14f));
        Label(card, "HƯỚNG DẪN TỰ LUẬN SINH HỌC", 34, .04f, .84f, .96f, .96f, new Color(1, .86f, .48f), TextAlignmentOptions.Center);
        var viewport = Box(card, "Viewport", .05f, .20f, .95f, .82f, new Color(.97f, .96f, .87f));
        viewport.gameObject.AddComponent<RectMask2D>();
        var scroll = viewport.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false;
        scroll.viewport = viewport; scroll.movementType = ScrollRect.MovementType.Clamped;
        var content = Box(viewport, "Content", 0, 1, 1, 1, Color.clear);
        content.pivot = new Vector2(.5f, 1); content.sizeDelta = Vector2.zero;
        var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(26, 26, 20, 20); layout.childControlHeight = layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        instructions = Label(content, "", 25, 0, 0, 1, 1, new Color(.11f, .19f, .12f), TextAlignmentOptions.TopLeft);
        instructions.gameObject.AddComponent<LayoutElement>(); scroll.content = content;
        var action = Box(card, "Start or resume", .20f, .055f, .80f, .16f, new Color(.18f, .40f, .25f));
        actionLabel = Label(action, "", 27, .025f, .03f, .975f, .97f, Color.white, TextAlignmentOptions.Center);
        var button = action.gameObject.AddComponent<Button>(); button.targetGraphic = action.GetComponent<Image>();
        button.onClick.AddListener(() => { shade.SetActive(false); remediation?.Invoke(); });
        SetRemediationAction(null); shade.SetActive(false);
    }

    RectTransform Box(Transform parent, string name, float x1, float y1, float x2, float y2, Color color)
    {
        var rect = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); rect.anchorMin = new Vector2(x1, y1); rect.anchorMax = new Vector2(x2, y2);
        rect.offsetMin = rect.offsetMax = Vector2.zero; rect.GetComponent<Image>().color = color;
        rect.GetComponent<Image>().raycastTarget = color.a > 0;
        return rect;
    }

    TMP_Text Label(Transform parent, string value, float size, float x1, float y1, float x2, float y2, Color color, TextAlignmentOptions align)
    {
        var rect = new GameObject("Text", typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(x1, y1); rect.anchorMax = new Vector2(x2, y2); rect.offsetMin = rect.offsetMax = Vector2.zero;
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>(); if (font != null) text.font = font;
        text.text = value; text.fontSize = size; text.color = color; text.alignment = align;
        text.richText = false; text.raycastTarget = false; text.textWrappingMode = TextWrappingModes.Normal;
        return text;
    }
}
