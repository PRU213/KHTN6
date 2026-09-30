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

    Vector2 startPos;
    Vector3 startScale;
    bool busy;

    void Start()
    {
        startPos = ball.anchoredPosition;
        startScale = ball.localScale;
        foreach (var b in answerButtons)
            b.onClick.AddListener(Kick);
    }

    public void Kick()
    {
        if (!busy) StartCoroutine(KickRoutine());
    }

    IEnumerator KickRoutine()
    {
        busy = true;
        SetButtons(false);

        Vector2 end = goalTargets[Random.Range(0, goalTargets.Length)].anchoredPosition;
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
        goalText.SetActive(true);
        yield return new WaitForSeconds(1f);

        // reset về chấm phạt đền
        goalText.SetActive(false);
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