using UnityEngine;
using UnityEngine.UI;

public class Twinkle : MonoBehaviour
{
    public float speed = 3f;        // nhấp nháy nhanh hay chậm
    public float minAlpha = 0.3f;   // độ mờ nhất
    public float maxAlpha = 1f;     // độ sáng nhất
    public float scalePulse = 0.15f; // phồng to thêm bao nhiêu khi sáng nhất
    public float rotateSpeed = 20f;  // tốc độ xoay (để 0 nếu không muốn xoay)

    private Image img;
    private Vector3 baseScale;

    void Start()
    {
        img = GetComponent<Image>();
        baseScale = transform.localScale;
    }

    void Update()
    {
        // t chạy từ 0 -> 1 -> 0 liên tục theo hình sin
        float t = (Mathf.Sin(Time.time * speed) + 1f) / 2f;

        // chỉnh độ trong suốt (nhấp nháy sáng-mờ)
        if (img != null)
        {
            Color c = img.color;
            c.a = Mathf.Lerp(minAlpha, maxAlpha, t);
            img.color = c;
        }

        // phồng to nhẹ khi sáng nhất
        float s = 1f + t * scalePulse;
        transform.localScale = baseScale * s;

        // xoay tròn chậm (tùy chọn, tạo cảm giác lấp lánh xoay)
        transform.Rotate(0, 0, rotateSpeed * Time.deltaTime);
    }
}