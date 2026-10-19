using UnityEngine;

public class ShipSway : MonoBehaviour
{
    public float centerAngle = 0f;    // góc giữa (0 = mũi hướng thẳng lên)
    public float swingAngle = 45f;    // lắc sang mỗi bên tối đa bao nhiêu độ
    public float speed = 1.6f;        // tốc độ lắc

    void Update()
    {
        float a = centerAngle + Mathf.Sin(Time.time * speed) * swingAngle;
        transform.localRotation = Quaternion.Euler(0f, 0f, a);
    }
}