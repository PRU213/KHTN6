using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class ShipShooter : MonoBehaviour
{
    public RectTransform canvasRect;   // kéo Planet_Game_3 vào
    public RectTransform ship;         // kéo ship_vu_tru vào
    public Bullet bulletPrefab;        // kéo prefab Bullet vào
    public float fireCooldown = 0.2f;  // giây giữa hai phát bắn

    float nextFireTime;

    void Update()
    {
        if (Time.time < nextFireTime) return;
        if (!TryGetClick(out Vector2 screenPos)) return;

        Fire(screenPos);
        nextFireTime = Time.time + fireCooldown;
    }

    bool TryGetClick(out Vector2 screenPos)
    {
#if ENABLE_INPUT_SYSTEM
        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            screenPos = mouse.position.ReadValue();
            return true;
        }
#else
        if (Input.GetMouseButtonDown(0))
        {
            screenPos = Input.mousePosition;
            return true;
        }
#endif
        screenPos = default;
        return false;
    }

    void Fire(Vector2 screenPos)
    {
        Canvas canvas = canvasRect.GetComponentInParent<Canvas>();
        Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                     ? canvas.worldCamera : null;

        // Đổi vị trí click trên màn hình sang tọa độ của Canvas
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPos, cam, out Vector2 target)) return;

        // Vị trí phi thuyền theo cùng hệ tọa độ
        Vector2 start = canvasRect.InverseTransformPoint(ship.position);

        Vector2 dir = target - start;
        if (dir.sqrMagnitude < 1f) return;

        Bullet b = Instantiate(bulletPrefab, canvasRect);
        b.transform.localPosition = start;
        b.Init(dir, canvasRect);
    }
}