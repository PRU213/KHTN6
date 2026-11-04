using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Persistent retry message, used only by the Biology essay screen.</summary>
public sealed class BiologySubmissionNotice : MonoBehaviour
{
    TMP_Text message;
    Action retry;
    public static BiologySubmissionNotice Attach(Canvas parent, TMP_FontAsset font)
    {
        if (parent == null) return null;
        var go = new GameObject("Biology grading message", typeof(RectTransform), typeof(Image), typeof(Canvas), typeof(GraphicRaycaster));
        var rect = (RectTransform)go.transform; rect.SetParent(parent.transform, false);
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = new Color(0, .035f, .025f, .94f);
        var canvas = go.GetComponent<Canvas>(); canvas.overrideSorting = true; canvas.sortingOrder = 1450;
        var notice = go.AddComponent<BiologySubmissionNotice>();
        notice.message = Text(go.transform, "", .17f, .38f, .83f, .72f, 29, font);
        Text(go.transform, "CHƯA NHẬN ĐƯỢC KẾT QUẢ CHẤM", .12f, .76f, .88f, .86f, 35, font).color = new Color(1, .86f, .48f);
        Button(go.transform, "Về bài làm", .17f, .20f, .46f, .30f, font, () => go.SetActive(false));
        Button(go.transform, "Thử chấm lại", .54f, .20f, .83f, .30f, font, () => { go.SetActive(false); notice.retry?.Invoke(); });
        go.SetActive(false); return notice;
    }
    public void Show(string text, Action tryAgain) { retry = tryAgain; message.text = text; gameObject.SetActive(true); transform.SetAsLastSibling(); }
    static TMP_Text Text(Transform parent, string value, float x1,float y1,float x2,float y2,float size,TMP_FontAsset font)
    {
        var rect = new GameObject("Text",typeof(RectTransform),typeof(TextMeshProUGUI)).GetComponent<RectTransform>();
        rect.SetParent(parent,false); rect.anchorMin=new Vector2(x1,y1);rect.anchorMax=new Vector2(x2,y2);rect.offsetMin=rect.offsetMax=Vector2.zero;
        var text=rect.GetComponent<TextMeshProUGUI>();if(font!=null)text.font=font;
        text.text=value;text.fontSize=size;text.color=Color.white;text.alignment=TextAlignmentOptions.Center;text.richText=false;text.raycastTarget=false;
        return text;
    }
    static void Button(Transform parent,string caption,float x1,float y1,float x2,float y2,TMP_FontAsset font, UnityEngine.Events.UnityAction action)
    {
        var rect=new GameObject(caption,typeof(RectTransform),typeof(Image),typeof(Button)).GetComponent<RectTransform>();rect.SetParent(parent,false);
        rect.anchorMin=new Vector2(x1,y1);rect.anchorMax=new Vector2(x2,y2);rect.offsetMin=rect.offsetMax=Vector2.zero;
        var image=rect.GetComponent<Image>();image.color=new Color(.17f,.39f,.24f);
        var button=rect.GetComponent<Button>();button.targetGraphic=image;button.onClick.AddListener(action);
        Text(rect,caption,.02f,.02f,.98f,.98f,27,font);
    }
}
