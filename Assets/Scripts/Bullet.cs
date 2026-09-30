using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 900f;      // pixel mỗi giây
    public float margin = 100f;     // ra khỏi màn hình bao nhiêu thì xóa

    Vector2 direction;
    RectTransform rt;
    RectTransform canvasRect;

    public void Init(Vector2 dir, RectTransform canvas)
    {
        rt = (RectTransform)transform;
        canvasRect = canvas;
        direction = dir.normalized;

        // Xoay đạn theo hướng bay (ảnh đạn hướng sang phải thì để 0, hướng lên thì trừ 90)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        rt.localRotation = Quaternion.Euler(0f, 0f, angle + rotationOffset);
    }

    [Tooltip("Ảnh đạn hướng sang phải = 0, hướng lên trên = -90")]
    public float rotationOffset = 0f;

    void Update()
    {
        rt.localPosition += (Vector3)(direction * speed * Time.deltaTime);

        Rect r = canvasRect.rect;
        Vector3 p = rt.localPosition;
        if (p.x < r.xMin - margin || p.x > r.xMax + margin ||
            p.y < r.yMin - margin || p.y > r.yMax + margin)
        {
            Destroy(gameObject);
        }
    }
}