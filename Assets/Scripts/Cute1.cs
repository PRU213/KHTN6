using UnityEngine;

public class Cute1 : MonoBehaviour
{
    public float scaleAmount = 0.08f; // phồng to bao nhiêu %, số càng lớn phồng càng mạnh
    public float speed = 2f;          // tốc độ nhún nhảy

    private Vector3 baseScale;

    void Start()
    {
        baseScale = transform.localScale;
    }

    void Update()
    {
        float s = 1f + Mathf.Sin(Time.time * speed) * scaleAmount;
        transform.localScale = baseScale * s;
    }
}