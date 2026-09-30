using UnityEngine;

public class BlackHoleMover : MonoBehaviour
{
    public enum Mode { Bounce, Wander }

    [Header("Chế độ")]
    public Mode mode = Mode.Bounce;

    [Header("Giới hạn di chuyển (tọa độ Canvas)")]
    public float minX = -810f;
    public float maxX = 810f;
    public float minY = -390f;
    public float maxY = 390f;

    [Header("Bounce: tốc độ (pixel mỗi giây)")]
    public float speed = 200f;

    [Header("Wander: độ nhanh chậm của đường trôi")]
    public float wanderSpeedX = 0.35f;
    public float wanderSpeedY = 0.5f;

    [Header("Xoay chậm (đặt 0 nếu không muốn xoay)")]
    public float rotateSpeed = 20f;

    RectTransform rt;
    Vector2 dir;
    float seedX, seedY;

    void Awake()
    {
        rt = GetComponent<RectTransform>();

        // Hướng chéo ngẫu nhiên (tránh gần như ngang hoặc gần như dọc)
        float angle = Random.Range(25f, 65f) * Mathf.Deg2Rad;
        dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        if (Random.value < 0.5f) dir.x = -dir.x;
        if (Random.value < 0.5f) dir.y = -dir.y;

        seedX = Random.Range(0f, 100f);
        seedY = Random.Range(0f, 100f);
    }

    void Update()
    {
        Vector2 pos = rt.anchoredPosition;

        if (mode == Mode.Bounce)
        {
            pos += dir * speed * Time.deltaTime;

            if (pos.x < minX) { pos.x = minX; dir.x = Mathf.Abs(dir.x); }
            else if (pos.x > maxX) { pos.x = maxX; dir.x = -Mathf.Abs(dir.x); }

            if (pos.y < minY) { pos.y = minY; dir.y = Mathf.Abs(dir.y); }
            else if (pos.y > maxY) { pos.y = maxY; dir.y = -Mathf.Abs(dir.y); }
        }
        else
        {
            float nx = Mathf.PerlinNoise(seedX + Time.time * wanderSpeedX, 0f);
            float ny = Mathf.PerlinNoise(0f, seedY + Time.time * wanderSpeedY);
            pos = new Vector2(Mathf.Lerp(minX, maxX, nx), Mathf.Lerp(minY, maxY, ny));
        }

        rt.anchoredPosition = pos;
        rt.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);
    }
}