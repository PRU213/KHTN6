using System.Collections.Generic;
using UnityEngine;

public class BlobDrift : MonoBehaviour
{
    [Header("Tốc độ")]
    public float minSpeed = 90f;
    public float maxSpeed = 190f;

    [Header("Vùng bay (đơn vị Canvas, gốc ở giữa màn hình)")]
    public Vector2 areaMin = new Vector2(-800f, -380f);
    public Vector2 areaMax = new Vector2(800f, 250f);

    [Header("Va chạm giữa các cục")]
    public float radius = 120f;

    static readonly List<BlobDrift> all = new List<BlobDrift>();
    RectTransform rt;
    Vector2 vel;

    void OnEnable()
    {
        rt = (RectTransform)transform;
        all.Add(this);

        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float speed = Random.Range(minSpeed, maxSpeed);
        vel = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
    }

    void OnDisable() => all.Remove(this);

    void Update()
    {
        Vector2 p = rt.anchoredPosition + vel * Time.deltaTime;

        // nảy khi chạm mép vùng bay
        if (p.x < areaMin.x) { p.x = areaMin.x; vel.x = Mathf.Abs(vel.x); }
        if (p.x > areaMax.x) { p.x = areaMax.x; vel.x = -Mathf.Abs(vel.x); }
        if (p.y < areaMin.y) { p.y = areaMin.y; vel.y = Mathf.Abs(vel.y); }
        if (p.y > areaMax.y) { p.y = areaMax.y; vel.y = -Mathf.Abs(vel.y); }

        // bật ra khi chạm cục khác
        foreach (var o in all)
        {
            if (o == this) continue;
            Vector2 d = p - o.rt.anchoredPosition;
            float dist = d.magnitude;
            if (dist < radius * 2f && dist > 0.01f)
            {
                Vector2 n = d / dist;
                if (Vector2.Dot(vel, n) < 0f)
                    vel = Vector2.Reflect(vel, n);
                p += n * ((radius * 2f - dist) * 0.5f);
            }
        }

        rt.anchoredPosition = p;
    }
}