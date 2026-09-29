using UnityEngine;

public class CloudMove : MonoBehaviour
{
    public float speed = 30f;         // tốc độ trôi, số càng lớn trôi càng nhanh
    public float leftLimit = -1000f;  // ra khỏi màn hình bên trái thì...
    public float rightLimit = 1000f;  // ...nhảy về lại bên phải

    void Update()
    {
        transform.localPosition += Vector3.right * speed * Time.deltaTime;

        if (transform.localPosition.x > rightLimit)
        {
            Vector3 pos = transform.localPosition;
            pos.x = leftLimit;
            transform.localPosition = pos;
        }
    }
}