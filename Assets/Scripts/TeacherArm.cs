using UnityEngine;

public class TeacherArm : MonoBehaviour
{
    [Header("Điểm xoay (0-1 trên ảnh cánh tay)")]
    public Vector2 pivotPoint = new Vector2(0.15f, 0.2f); // gần vai/tay cầm

    [Header("Kiểu chỉ chỉ")]
    public float baseAngle = 0f;      // góc nghỉ
    public float angle = -8f;         // biên độ mỗi lần chỉ (âm = chúi xuống, dương = ngược lại)
    public float tapDuration = 0.35f; // thời gian 1 lần chỉ
    public int tapCount = 3;          // số lần chỉ liên tiếp
    public float pauseTime = 1.5f;    // nghỉ giữa các đợt

    RectTransform rect;
    float cycle;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        SetPivotKeepPosition(pivotPoint);
        cycle = tapDuration * tapCount + pauseTime;
    }

    void Update()
    {
        float t = Time.time % cycle;
        float a = 0f;

        if (t < tapDuration * tapCount)
        {
            float p = t / tapDuration;
            a = Mathf.Abs(Mathf.Sin(p * Mathf.PI)) * angle;
        }

        rect.localRotation = Quaternion.Euler(0, 0, baseAngle + a);
    }

    // Đổi pivot nhưng giữ nguyên vị trí hiển thị của ảnh
    void SetPivotKeepPosition(Vector2 newPivot)
    {
        Vector2 size = rect.rect.size;
        Vector2 deltaPivot = newPivot - rect.pivot;
        Vector2 deltaPos = new Vector2(
            deltaPivot.x * size.x * rect.localScale.x,
            deltaPivot.y * size.y * rect.localScale.y);

        rect.pivot = newPivot;
        rect.anchoredPosition += deltaPos;
    }
}