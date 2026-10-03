using UnityEngine;

public class MoveLeftRight : MonoBehaviour
{
    public float distance = 3f; // Khoảng cách di chuyển
    public float speed = 2f;    // Tốc độ

    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        float x = Mathf.PingPong(Time.time * speed, distance * 2) - distance;

        transform.position = new Vector3(
            startPosition.x + x,
            startPosition.y,
            startPosition.z
        );
    }
}