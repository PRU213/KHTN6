using UnityEngine;

/// <summary>
/// Camera theo dõi nhân vật với hiệu ứng smooth.
/// Gắn script này vào Main Camera.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("=== MỤC TIÊU ===")]
    [Tooltip("Kéo thả Player vào đây")]
    public Transform target;

    [Header("=== THIẾT LẬP ===")]
    [Tooltip("Độ mượt khi camera di chuyển (càng nhỏ càng mượt)")]
    public float smoothSpeed = 5f;

    [Tooltip("Offset so với nhân vật (x, y, z)")]
    public Vector3 offset = new Vector3(0f, 1f, -10f);

    [Header("=== GIỚI HẠN MAP ===")]
    [Tooltip("Bật giới hạn camera theo map")]
    public bool useBounds = false;
    public float minX = -10f;
    public float maxX = 10f;
    public float minY = -5f;
    public float maxY = 5f;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;

        // Giới hạn camera nếu bật
        if (useBounds)
        {
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, minY, maxY);
        }

        // Smooth lerp
        Vector3 smoothedPosition = Vector3.Lerp(
            transform.position,
            desiredPosition,
            smoothSpeed * Time.deltaTime
        );

        transform.position = smoothedPosition;
    }
}
