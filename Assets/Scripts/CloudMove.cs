using UnityEngine;

/// <summary>
/// Mây trôi ngang màn hình.
/// Mây bên trái trôi sang phải, mây bên phải trôi sang trái.
/// Nhân vật có thể đứng trên mây (kết hợp Platform).
/// </summary>
public class CloudMove : MonoBehaviour
{
    public float speed = 50f;
    public float leftLimit = -1200f;
    public float rightLimit = 1200f;

    private float currentSpeed;

    /// <summary>Lượng di chuyển trong frame hiện tại (để PlayerController trôi theo)</summary>
    public Vector2 FrameDelta { get; private set; }

    void Start()
    {
        // Mây bên trái thì trôi sang phải (tốc độ dương)
        // Mây bên phải thì trôi sang trái (tốc độ âm)
        if (transform.localPosition.x < 0)
        {
            currentSpeed = speed;
        }
        else
        {
            currentSpeed = -speed;
        }
    }

    void Update()
    {
        float deltaX = currentSpeed * Time.deltaTime;
        transform.localPosition += Vector3.right * deltaX;

        // Lưu lại lượng di chuyển để Player trôi theo
        FrameDelta = new Vector2(deltaX, 0f);

        // Trôi qua phải thì quay lại trái
        if (currentSpeed > 0 && transform.localPosition.x > rightLimit)
        {
            Vector3 pos = transform.localPosition;
            pos.x = leftLimit;
            transform.localPosition = pos;
        }
        // Trôi qua trái thì quay lại phải
        else if (currentSpeed < 0 && transform.localPosition.x < leftLimit)
        {
            Vector3 pos = transform.localPosition;
            pos.x = rightLimit;
            transform.localPosition = pos;
        }
    }
}