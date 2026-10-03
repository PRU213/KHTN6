using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class KickBall : MonoBehaviour
{
    [SerializeField] RectTransform ball;             // quả bóng (UI Image)
    [SerializeField] RectTransform[] goalTargets;    // các điểm trong khung thành (Empty UI object)
    [SerializeField] Button[] answerButtons;         // 4 nút A, B, C, D
    [SerializeField] GameObject goalText;            // chữ "VÀO!" (tắt sẵn)
    [SerializeField] float flyTime = 0.85f;
    [SerializeField] float arcHeight = 120f;
    [SerializeField] float endScale = 0.5f;

    [Header("Khi sút trượt")]
    [SerializeField] GameObject missText;            // chữ "TRƯỢT!" (tùy chọn, tắt sẵn)
    [SerializeField] Vector2 missOffset = new Vector2(450f, 150f); // lệch khỏi gôn bao nhiêu

    Vector2 startPos;
    Vector3 startScale;
    bool busy;

    public bool IsBusy => busy;

    void Start()
    {
        startPos = ball.anchoredPosition;
        startScale = ball.localScale;
        // Không còn tự sút khi bấm nút. FootballQuizController sẽ gọi Goal() hoặc Miss().
    }

    // Trả lời đúng: bóng vào gôn
    public void Goal()
    {
        if (!busy) StartCoroutine(KickRoutine(true));
    }

    // Trả lời sai: bóng bay trượt ra ngoài gôn
    public void Miss()
    {
        if (!busy) StartCoroutine(KickRoutine(false));
    }

    IEnumerator KickRoutine(bool scored)
    {
        busy = true;
        SetButtons(false);

        Vector2 end = goalTargets[Random.Range(0, goalTargets.Length)].anchoredPosition;
        if (!scored)
        {
            float side = Random.value < 0.5f ? -1f : 1f;
            end += new Vector2(side * missOffset.x, missOffset.y);
        }
        float dir = end.x >= startPos.x ? 1f : -1f;

        for (float t = 0; t < 1f; t += Time.deltaTime / flyTime)
        {
            float e = 1f - (1f - t) * (1f - t);                  // ease-out
            Vector2 pos = Vector2.Lerp(startPos, end, e);
            pos.y += Mathf.Sin(t * Mathf.PI) * arcHeight;         // đường cong bay lên
            ball.anchoredPosition = pos;
            ball.localScale = Vector3.Lerp(startScale, startScale * endScale, e);
            ball.localRotation = Quaternion.Euler(0, 0, -dir * 720f * t);
            yield return null;
        }

        ball.anchoredPosition = end;
        if (scored) { if (goalText) goalText.SetActive(true); }
        else { if (missText) missText.SetActive(true); }
        yield return new WaitForSeconds(1f);

        // reset về chấm phạt đền
        if (goalText) goalText.SetActive(false);
        if (missText) missText.SetActive(false);
        ball.anchoredPosition = startPos;
        ball.localScale = startScale;
        ball.localRotation = Quaternion.identity;
        SetButtons(true);
        busy = false;
    }

    void SetButtons(bool on)
    {
        foreach (var b in answerButtons) b.interactable = on;
    }
}