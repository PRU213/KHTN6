using UnityEngine;

public class UIPulse : MonoBehaviour
{
    public float scaleAmount = 0.08f; // 0.08 = phóng thêm 8%
    public float speed = 2f;

    Vector3 startScale;
    float phase;

    void OnEnable()
    {
        startScale = transform.localScale;
        phase = Random.Range(0f, Mathf.PI * 2f);
    }

    void OnDisable()
    {
        transform.localScale = startScale;
    }

    void Update()
    {
        float s = 1f + Mathf.Sin(Time.time * speed + phase) * scaleAmount;
        transform.localScale = startScale * s;
    }
}