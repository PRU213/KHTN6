using UnityEngine;

/// <summary>
/// Di chuyển nhân vật Nv bằng WASD + Space.
/// Detect va chạm với các UI Image địa hình bằng world-space rect overlap.
/// Chạy trên: Go, Dat, cay, Re, DayCau  →  animation chạy
/// Leo trên:  DayLeo                      →  animation leo
/// </summary>
public class NvMover : MonoBehaviour
{
    [Header("Animator")]
    public Animator playerAnimator;

    [Header("Địa hình — Chạy (Go, Dat, cay, Re, DayCau)")]
    public RectTransform[] runSurfaces;

    [Header("Địa hình — Leo (DayLeo)")]
    public RectTransform[] climbSurfaces;

    [Header("Di chuyển")]
    public float moveSpeed  = 400f;
    public float climbSpeed = 300f;
    public float jumpForce  = 600f;
    public float gravity    = 1200f;

    [Header("Căn chỉnh tiếp đất")]
    [Tooltip("Khoảng cách (pixel Canvas) hạ chân nhân vật xuống để chạm đúng hình vẽ của địa hình thay vì lơ lửng trên đỉnh khung UI")]
    public float groundOffset = 48f;

    // ───── constants ─────
    const string P_RUN   = "isRunning";
    const string P_CLIMB = "isClimbing";

    // ───── runtime ─────
    RectTransform selfRect;
    RectTransform canvasRect;
    float velocityY;
    bool  isGrounded;
    bool  isOnClimb;
    bool  hasRunParam;
    bool  hasClimbParam;

    // reuse arrays — tránh GC
    readonly Vector3[] myC  = new Vector3[4];
    readonly Vector3[] tmpC = new Vector3[4];
    readonly Vector3[] canC = new Vector3[4];

    void Awake()
    {
        selfRect   = GetComponent<RectTransform>();
        canvasRect = GetComponentInParent<Canvas>().GetComponent<RectTransform>();

        if (!playerAnimator)
            playerAnimator = GetComponent<Animator>();

        // Cache parameter check
        if (playerAnimator)
        {
            foreach (var p in playerAnimator.parameters)
            {
                if (p.name == P_RUN   && p.type == AnimatorControllerParameterType.Bool) hasRunParam   = true;
                if (p.name == P_CLIMB && p.type == AnimatorControllerParameterType.Bool) hasClimbParam = true;
            }
        }
    }

    void Update()
    {
        // ══════ Input ══════
        float h = 0f, v = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))  h = -1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) h =  1f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))    v =  1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))  v = -1f;
        bool jumpDown = Input.GetKeyDown(KeyCode.Space);

        // ══════ Bounds hiện tại (world pixel) ══════
        selfRect.GetWorldCorners(myC);
        float feetY   = myC[0].y;                       // bottom
        float headY   = myC[1].y;                       // top
        float leftX   = myC[0].x;                       // left
        float rightX  = myC[2].x;                       // right
        float centerX = (leftX + rightX) * 0.5f;
        float myH     = headY - feetY;
        float myW     = rightX - leftX;

        // ══════ Detect climb ══════
        isOnClimb = OverlapsAny(climbSurfaces);

        float dx = 0f, dy = 0f;

        // ─────────────────────────────────────
        //              CHẾ ĐỘ LEO
        // ─────────────────────────────────────
        if (isOnClimb)
        {
            velocityY  = 0f;
            isGrounded = true;
            dx = h * climbSpeed * Time.deltaTime;
            dy = v * climbSpeed * Time.deltaTime;
        }
        // ─────────────────────────────────────
        //          CHẾ ĐỘ BÌNH THƯỜNG
        // ─────────────────────────────────────
        else
        {
            // Di chuyển ngang
            dx = h * moveSpeed * Time.deltaTime;

            // Nhảy — chỉ khi đang đứng trên mặt đất
            if (isGrounded && jumpDown)
            {
                velocityY  = jumpForce;
                isGrounded = false;
            }

            // Trọng lực — chỉ khi trên không
            if (!isGrounded)
                velocityY -= gravity * Time.deltaTime;

            dy = velocityY * Time.deltaTime;

            // ══════ Va chạm mặt đất ══════
            float newFeetY   = feetY + dy;
            float newCenterX = centerX + dx;

            if (velocityY <= 0f)
            {
                float landY = FindLandingY(newCenterX, feetY, newFeetY);
                if (landY > float.MinValue)
                {
                    // Snap chân lên đỉnh surface
                    dy         = landY - feetY;
                    velocityY  = 0f;
                    isGrounded = true;
                }
                else
                {
                    isGrounded = false;
                }
            }
            else
            {
                // Đang bay lên — không ở mặt đất
                isGrounded = false;
            }
        }

        // ══════ Apply di chuyển ══════
        Vector3 pos = transform.position;
        pos.x += dx;
        pos.y += dy;

        // ══════ Giới hạn trong màn hình ══════
        canvasRect.GetWorldCorners(canC);
        float scrL = canC[0].x;
        float scrB = canC[0].y;
        float scrR = canC[2].x;
        float scrT = canC[2].y;

        pos.x = Mathf.Clamp(pos.x, scrL + myW * 0.5f, scrR - myW * 0.5f);
        pos.y = Mathf.Clamp(pos.y, scrB + myH * 0.5f, scrT - myH * 0.5f);

        transform.position = pos;

        // ══════ Lật sprite theo hướng ══════
        if (h != 0f)
        {
            Vector3 sc = selfRect.localScale;
            sc.x = (h < 0f) ? -Mathf.Abs(sc.x) : Mathf.Abs(sc.x);
            selfRect.localScale = sc;
        }

        // ══════ Animation ══════
        if (playerAnimator)
        {
            bool moving = (h != 0f) || (isOnClimb && v != 0f);
            if (hasRunParam)   playerAnimator.SetBool(P_RUN,   moving && !isOnClimb);
            if (hasClimbParam) playerAnimator.SetBool(P_CLIMB, isOnClimb && (v != 0f || h != 0f));
        }
    }

    // ═══════════════════════════════════════════
    //             VA CHẠM HELPERS
    // ═══════════════════════════════════════════

    /// <summary>
    /// Tìm đỉnh surface cao nhất mà nhân vật đáp xuống được.
    /// Trả float.MinValue nếu không có surface dưới chân.
    /// </summary>
    float FindLandingY(float cx, float oldFeetY, float newFeetY)
    {
        if (runSurfaces == null) return float.MinValue;

        float bestY = float.MinValue;

        foreach (var s in runSurfaces)
        {
            if (s == null) continue;
            s.GetWorldCorners(tmpC);

            float sL   = tmpC[0].x;
            float sB   = tmpC[0].y;
            float sR   = tmpC[2].x;
            float sT   = tmpC[1].y;

            // Surface phải có chiều cao dương
            if (sT <= sB) continue;

            // Tâm nhân vật phải nằm trong khoảng ngang
            if (cx < sL || cx > sR) continue;

            // Tính bề mặt tiếp đất thực tế (trừ offset độ lún)
            float offset = groundOffset;
            if (s.TryGetComponent<SurfaceOffset>(out var so) && so.customOffset >= 0f)
            {
                offset = so.customOffset;
            }

            float walkY = sT - offset;
            if (walkY < sB) walkY = sT;

            float tol = 20f;

            // Đang rơi qua mặt surface? (chân cũ ≥ mặt, chân mới ≤ mặt)
            bool falling = (oldFeetY >= walkY - tol) && (newFeetY <= walkY + tol);

            // Chân đang bên trong surface? (spawn / lọt vào)
            bool inside = (newFeetY >= sB) && (newFeetY <= walkY + tol);

            if ((falling || inside) && walkY > bestY)
                bestY = walkY;
        }
        return bestY;
    }

    /// <summary>Nhân vật có overlap với bất kỳ surface nào?</summary>
    bool OverlapsAny(RectTransform[] surfaces)
    {
        if (surfaces == null) return false;
        Rect me = R(myC);
        foreach (var s in surfaces)
        {
            if (s == null) continue;
            s.GetWorldCorners(tmpC);
            if (me.Overlaps(R(tmpC))) return true;
        }
        return false;
    }

    Rect R(Vector3[] c)
    {
        return new Rect(c[0].x, c[0].y, c[2].x - c[0].x, c[2].y - c[0].y);
    }

    void OnDrawGizmosSelected()
    {
        // Đường đáy chân của nhân vật (Màu xanh lá)
        RectTransform rt = selfRect != null ? selfRect : GetComponent<RectTransform>();
        if (rt != null)
        {
            Vector3[] c = new Vector3[4];
            rt.GetWorldCorners(c);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(new Vector3(c[0].x, c[0].y, transform.position.z),
                            new Vector3(c[2].x, c[0].y, transform.position.z));
        }

        // Đường bề mặt tiếp đất thực tế của từng địa hình (Màu vàng)
        if (runSurfaces != null)
        {
            Gizmos.color = Color.yellow;
            Vector3[] c = new Vector3[4];
            foreach (var s in runSurfaces)
            {
                if (s == null) continue;
                s.GetWorldCorners(c);
                float offset = groundOffset;
                if (s.TryGetComponent<SurfaceOffset>(out var so) && so.customOffset >= 0f)
                    offset = so.customOffset;

                float walkY = c[1].y - offset;
                Gizmos.DrawLine(new Vector3(c[0].x, walkY, transform.position.z),
                                new Vector3(c[2].x, walkY, transform.position.z));
            }
        }
    }
}
