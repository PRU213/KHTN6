using UnityEngine;

public class UIFloat : MonoBehaviour
{
    public float amplitude = 15f;
    public float speed = 1f;
    public bool randomPhase = true;

    RectTransform rt;
    Vector2 startPos;
    float phase;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
    }

    void OnEnable()
    {
        startPos = rt.anchoredPosition;
        phase = randomPhase ? Random.Range(0f, Mathf.PI * 2f) : 0f;
    }

    void OnDisable()
    {
        rt.anchoredPosition = startPos;
    }

    void Update()
    {
        float y = Mathf.Sin(Time.time * speed + phase) * amplitude;
        rt.anchoredPosition = startPos + new Vector2(0f, y);
    }
}