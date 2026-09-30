using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ClimbController : MonoBehaviour
{
    [Header("Đối tượng")]
    public RectTransform player;
    public RectTransform milestone;
    public TMP_Text milestoneText;
    public Animator playerAnimator;      // để trống nếu chưa có

    [Header("Đường đi (từ chân núi lên đỉnh)")]
    public RectTransform[] points;
    public Vector2 milestoneOffset = new Vector2(70, 50); // cột mốc lệch khỏi điểm
    public float secondsPerSegment = 1.5f;

    [Header("Chuyển màn (tuỳ chọn)")]
    public CanvasGroup fade;             // ảnh đen phủ màn hình
    public Image background;             // ảnh BG
    public Sprite[] stageBackgrounds;    // các nền của màn 2, 3... (để trống nếu chưa có)

    int index;      // nhân vật đang đứng ở points[index]
    int level = 1;  // số hiển thị trên cột mốc
    int stage;
    bool busy;

    void Start()
    {
        player.position = points[0].position;
        milestoneText.text = level.ToString("00");
        ShowMilestone();
    }

    void ShowMilestone()
    {
        if (index + 1 >= points.Length) return;
        milestone.position = points[index + 1].position;
        milestone.localPosition += (Vector3)milestoneOffset;
    }

    // Gắn vào OnClick của nút Answer
    public void OnAnswerClicked()
    {
        if (!busy) StartCoroutine(Climb());
    }

    IEnumerator Climb()
    {
        busy = true;
        if (playerAnimator) playerAnimator.SetBool("isRunning", true);

        Vector3 from = player.position;
        Vector3 to = points[index + 1].position;
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime / secondsPerSegment;
            player.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0, 1, t));
            yield return null;
        }
        index++;

        if (playerAnimator) playerAnimator.SetBool("isRunning", false);

        level++;
        milestoneText.text = level.ToString("00");

        if (index >= points.Length - 1) yield return NextStage();
        else ShowMilestone();

        busy = false;
    }

    IEnumerator NextStage()
    {
        yield return Fade(0, 1);

        stage++;
        if (background && stageBackgrounds != null && stage - 1 < stageBackgrounds.Length)
            background.sprite = stageBackgrounds[stage - 1];

        index = 0;
        player.position = points[0].position;
        ShowMilestone();

        yield return Fade(1, 0);
    }

    IEnumerator Fade(float a, float b)
    {
        if (!fade) yield break;
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.5f;
            fade.alpha = Mathf.Lerp(a, b, t);
            yield return null;
        }
        fade.alpha = b;
    }
}