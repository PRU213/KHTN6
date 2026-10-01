using UnityEngine;

/// <summary>
/// Gắn component này lên GameObject Image địa hình (Go, cay, Re, DayCau...)
/// nếu bạn muốn tùy chỉnh độ cao tiếp đất riêng cho vật thể đó.
/// Nếu không gắn, NvMover sẽ tự động dùng giá trị groundOffset chung.
/// </summary>
public class SurfaceOffset : MonoBehaviour
{
    [Tooltip("Độ lún / hạ thấp tiếp đất riêng cho bề mặt này (pixel Canvas). Nếu để < 0 sẽ dùng groundOffset chung của NvMover.")]
    public float customOffset = -1f;
}
