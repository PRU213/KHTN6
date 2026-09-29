using UnityEngine;

public class DragonFloat : MonoBehaviour
{
    public float amplitude = 15f;
    public float frequency = 1.5f;
    public float swayAmplitude = 8f;

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.localPosition;
    }

    void Update()
    {
        float y = Mathf.Sin(Time.time * frequency) * amplitude;
        float x = Mathf.Sin(Time.time * frequency * 0.5f) * swayAmplitude;
        transform.localPosition = startPos + new Vector3(x, y, 0);
    }
}