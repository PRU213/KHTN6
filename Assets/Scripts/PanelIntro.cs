using UnityEngine;

public class PanelIntro : MonoBehaviour
{
    public float duration = 0.6f; // thời gian hiện ra (giây)

    private CanvasGroup canvasGroup;
    private Vector3 targetScale;
    private float timer = 0f;

    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        targetScale = transform.localScale; // lưu lại kích thước gốc lúc thiết kế

        // bắt đầu từ trạng thái nhỏ + trong suốt
        transform.localScale = Vector3.zero;
        canvasGroup.alpha = 0f;
    }

    void Update()
    {
        if (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            t = 1f - Mathf.Pow(1f - t, 3f); // ease-out cho mượt

            transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, t);
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
        }
    }
}