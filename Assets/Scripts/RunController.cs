using System.Collections;
using UnityEngine;
using TMPro;

public class RunController : MonoBehaviour
{
    [Header("Đối tượng")]
    public RectTransform player;        // nhân vật
    public RectTransform target;        // TargetPoint
    public TMP_Text milestoneText;      // NumberText
    public Animator playerAnimator;     // để trống nếu chưa có

    [Header("Thiết lập")]
    public float runDuration = 1.5f;
    public int startNumber = 1;
    public int step = 1;

    int currentNumber;
    bool isRunning;

    void Start()
    {
        currentNumber = startNumber;
        milestoneText.text = currentNumber.ToString("00");
    }

    // Gắn hàm này vào OnClick của nút Answer
    public void OnAnswerClicked()
    {
        if (!isRunning) StartCoroutine(RunRoutine());
    }

    IEnumerator RunRoutine()
    {
        isRunning = true;
        if (playerAnimator) playerAnimator.SetBool("isRunning", true);

        Vector3 from = player.position;
        Vector3 to = target.position;
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime / runDuration;
            player.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0, 1, t));
            yield return null;
        }

        int oldNumber = currentNumber;
        currentNumber += step;
        yield return StartCoroutine(CountUp(oldNumber, currentNumber, 0.5f));

        if (playerAnimator) playerAnimator.SetBool("isRunning", false);
        isRunning = false;
    }

    IEnumerator CountUp(int a, int b, float duration)
    {
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            int v = Mathf.RoundToInt(Mathf.Lerp(a, b, t));
            milestoneText.text = v.ToString("00");
            yield return null;
        }
        milestoneText.text = b.ToString("00");
    }
}