using UnityEngine;

/// <summary>
/// Di chuyển nhân vật Nv bằng WASD + Space.
/// Detect va chạm với các UI Image địa hình bằng world-space RectTransform.
///
/// Chạy trên: Go, Dat, cay, Re, DayCau
/// Leo trên:  DayLeo
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
    public float moveSpeed = 400f;
    public float climbSpeed = 300f;
    public float jumpForce = 600f;
    public float gravity = 1200f;

    [Header("Căn chỉnh tiếp đất")]
    [Tooltip("Khoảng cách hạ mặt đứng thực tế xuống dưới đỉnh RectTransform.")]
    public float groundOffset = 48f;

    // ───── Animator Parameters ─────
    const string P_RUN = "isRunning";
    const string P_CLIMB = "isClimbing";

    // ───── Runtime ─────
    RectTransform selfRect;
    RectTransform canvasRect;

    float velocityY;

    bool isGrounded;
    bool isOnClimb;

    bool hasRunParam;
    bool hasClimbParam;

    // Reuse arrays — tránh GC
    readonly Vector3[] myC = new Vector3[4];
    readonly Vector3[] tmpC = new Vector3[4];
    readonly Vector3[] canC = new Vector3[4];

    void Awake()
    {
        selfRect = GetComponent<RectTransform>();

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
            canvasRect = canvas.GetComponent<RectTransform>();

        if (!playerAnimator)
            playerAnimator = GetComponent<Animator>();

        // Cache Animator parameters
        if (playerAnimator)
        {
            foreach (var p in playerAnimator.parameters)
            {
                if (p.name == P_RUN &&
                    p.type == AnimatorControllerParameterType.Bool)
                {
                    hasRunParam = true;
                }

                if (p.name == P_CLIMB &&
                    p.type == AnimatorControllerParameterType.Bool)
                {
                    hasClimbParam = true;
                }
            }
        }
    }

    void Update()
    {
        // ═══════════════════════════════════════
        // INPUT
        // ═══════════════════════════════════════

        float h = 0f;
        float v = 0f;

        if (Input.GetKey(KeyCode.A) ||
            Input.GetKey(KeyCode.LeftArrow))
        {
            h = -1f;
        }

        if (Input.GetKey(KeyCode.D) ||
            Input.GetKey(KeyCode.RightArrow))
        {
            h = 1f;
        }

        if (Input.GetKey(KeyCode.W) ||
            Input.GetKey(KeyCode.UpArrow))
        {
            v = 1f;
        }

        if (Input.GetKey(KeyCode.S) ||
            Input.GetKey(KeyCode.DownArrow))
        {
            v = -1f;
        }

        bool jumpDown = Input.GetKeyDown(KeyCode.Space);

        // ═══════════════════════════════════════
        // RECT HIỆN TẠI CỦA PLAYER
        // ═══════════════════════════════════════

        selfRect.GetWorldCorners(myC);

        Rect myRect = GetSafeRect(myC);

        float feetY = myRect.yMin;
        float headY = myRect.yMax;

        float leftX = myRect.xMin;
        float rightX = myRect.xMax;

        float myH = myRect.height;
        float myW = myRect.width;

        // ═══════════════════════════════════════
        // DETECT CLIMB
        // ═══════════════════════════════════════

        isOnClimb = OverlapsAny(climbSurfaces);

        float dx = 0f;
        float dy = 0f;

        // ═══════════════════════════════════════
        // CHẾ ĐỘ LEO
        // ═══════════════════════════════════════

        if (isOnClimb)
        {
            velocityY = 0f;

            // Leo không đồng nghĩa grounded
            isGrounded = false;

            dx = h * climbSpeed * Time.deltaTime;
            dy = v * climbSpeed * Time.deltaTime;

            // Space khi leo: không làm gì
        }

        // ═══════════════════════════════════════
        // CHẾ ĐỘ BÌNH THƯỜNG
        // ═══════════════════════════════════════

        else
        {
            // ─────────────────────────────
            // DI CHUYỂN NGANG
            // ─────────────────────────────

            dx = h * moveSpeed * Time.deltaTime;

            // QUAN TRỌNG:
            // Chặn Player xuyên qua cạnh trái/phải của địa hình.
            dx = ResolveHorizontalCollision(
                dx,
                leftX,
                rightX,
                feetY,
                headY
            );

            float newLeftX = leftX + dx;
            float newRightX = rightX + dx;

            // ─────────────────────────────
            // NHẢY
            // ─────────────────────────────

            if (isGrounded && jumpDown)
            {
                velocityY = jumpForce;
                isGrounded = false;
            }

            // ─────────────────────────────
            // GRAVITY
            // ─────────────────────────────

            if (!isGrounded)
                velocityY -= gravity * Time.deltaTime;

            dy = velocityY * Time.deltaTime;

            // ─────────────────────────────
            // VA CHẠM MẶT TRÊN
            // ─────────────────────────────

            float newFeetY = feetY + dy;

            if (velocityY <= 0f)
            {
                float landY = FindLandingY(
                    newLeftX,
                    newRightX,
                    feetY,
                    newFeetY
                );

                if (landY > float.MinValue)
                {
                    // Snap chân Player đúng lên mặt surface
                    dy = landY - feetY;

                    velocityY = 0f;
                    isGrounded = true;
                }
                else
                {
                    isGrounded = false;
                }
            }
            else
            {
                // Đang đi lên
                isGrounded = false;
            }
        }

        // ═══════════════════════════════════════
        // APPLY MOVEMENT
        // ═══════════════════════════════════════

        Vector3 pos = transform.position;

        pos.x += dx;
        pos.y += dy;

        // ═══════════════════════════════════════
        // GIỚI HẠN TRONG CANVAS
        // ═══════════════════════════════════════

        if (canvasRect != null)
        {
            canvasRect.GetWorldCorners(canC);

            Rect canRect = GetSafeRect(canC);

            float scrL = canRect.xMin;
            float scrB = canRect.yMin;
            float scrR = canRect.xMax;
            float scrT = canRect.yMax;

            pos.x = Mathf.Clamp(
                pos.x,
                scrL + myW * 0.5f,
                scrR - myW * 0.5f
            );

            pos.y = Mathf.Clamp(
                pos.y,
                scrB + myH * 0.5f,
                scrT - myH * 0.5f
            );
        }

        transform.position = pos;

        // ═══════════════════════════════════════
        // LẬT NHÂN VẬT
        // ═══════════════════════════════════════

        if (h != 0f)
        {
            Vector3 sc = selfRect.localScale;

            sc.x = (h < 0f)
                ? -Mathf.Abs(sc.x)
                : Mathf.Abs(sc.x);

            selfRect.localScale = sc;
        }

        // ═══════════════════════════════════════
        // ANIMATION
        // ═══════════════════════════════════════

        if (playerAnimator)
        {
            bool moving =
                (h != 0f) ||
                (isOnClimb && v != 0f);

            if (hasRunParam)
            {
                playerAnimator.SetBool(
                    P_RUN,
                    moving && !isOnClimb
                );
            }

            if (hasClimbParam)
            {
                playerAnimator.SetBool(
                    P_CLIMB,
                    isOnClimb && (v != 0f || h != 0f)
                );
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // VA CHẠM CẠNH TRÁI / PHẢI
    // ═══════════════════════════════════════════════════════════════

    float ResolveHorizontalCollision(
        float desiredDx,
        float playerLeft,
        float playerRight,
        float playerBottom,
        float playerTop)
    {
        if (runSurfaces == null)
            return desiredDx;

        if (Mathf.Approximately(desiredDx, 0f))
            return 0f;

        float resolvedDx = desiredDx;

        foreach (var s in runSurfaces)
        {
            if (s == null)
                continue;

            s.GetWorldCorners(tmpC);

            Rect sRect = GetSafeRect(tmpC);

            float sLeft = sRect.xMin;
            float sRight = sRect.xMax;
            float sBottom = sRect.yMin;

            // Mặt trên thực tế mà Player đứng được
            float walkY = GetWalkY(s, sRect);

            // Vật thể được coi là "solid"
            // từ đáy Rect tới walkY.
            float solidTop = walkY;

            // Player phải đang nằm ngang phần thân vật thể.
            //
            // Nếu chân Player đang đúng trên walkY,
            // playerBottom < solidTop sẽ FALSE,
            // nên Player vẫn chạy bình thường trên mặt.
            bool verticalOverlap =
                playerTop > sBottom &&
                playerBottom < solidTop;

            if (!verticalOverlap)
                continue;

            // ─────────────────────────────
            // PLAYER ĐI SANG PHẢI
            // ─────────────────────────────

            if (desiredDx > 0f)
            {
                float futureRight =
                    playerRight + resolvedDx;

                // Player hiện đang ở bên trái surface
                // nhưng frame tiếp theo định xuyên qua cạnh trái.
                if (playerRight <= sLeft &&
                    futureRight > sLeft)
                {
                    float allowedDx =
                        sLeft - playerRight;

                    if (allowedDx < resolvedDx)
                        resolvedDx = allowedDx;
                }
            }

            // ─────────────────────────────
            // PLAYER ĐI SANG TRÁI
            // ─────────────────────────────

            else if (desiredDx < 0f)
            {
                float futureLeft =
                    playerLeft + resolvedDx;

                // Player hiện đang ở bên phải surface
                // nhưng frame tiếp theo định xuyên qua cạnh phải.
                if (playerLeft >= sRight &&
                    futureLeft < sRight)
                {
                    float allowedDx =
                        sRight - playerLeft;

                    if (allowedDx > resolvedDx)
                        resolvedDx = allowedDx;
                }
            }
        }

        return resolvedDx;
    }

    // ═══════════════════════════════════════════════════════════════
    // TÌM MẶT TIẾP ĐẤT
    // ═══════════════════════════════════════════════════════════════

    float FindLandingY(
        float leftX,
        float rightX,
        float oldFeetY,
        float newFeetY)
    {
        if (runSurfaces == null)
            return float.MinValue;

        float bestY = float.MinValue;

        foreach (var s in runSurfaces)
        {
            if (s == null)
                continue;

            s.GetWorldCorners(tmpC);

            Rect sRect = GetSafeRect(tmpC);

            float sL = sRect.xMin;
            float sR = sRect.xMax;

            // Phải overlap theo chiều ngang
            if (rightX <= sL || leftX >= sR)
                continue;

            float walkY = GetWalkY(s, sRect);

            // QUAN TRỌNG:
            // Player phải thật sự từ phía trên đi xuống qua mặt walkY.
            //
            // Không dùng tolerance 20px nữa,
            // tránh đứng bên cạnh mà bị kéo lên mặt trên.
            bool wasAbove =
                oldFeetY >= walkY;

            bool crossedSurface =
                newFeetY <= walkY;

            if (wasAbove &&
                crossedSurface &&
                walkY > bestY)
            {
                bestY = walkY;
            }
        }

        return bestY;
    }

    // ═══════════════════════════════════════════════════════════════
    // LẤY MẶT ĐỨNG THỰC TẾ
    // ═══════════════════════════════════════════════════════════════

    float GetWalkY(
        RectTransform surface,
        Rect surfaceRect)
    {
        float offset = groundOffset;

        if (surface.TryGetComponent<SurfaceOffset>(out var so) &&
            so.customOffset >= 0f)
        {
            offset = so.customOffset;
        }

        float walkY =
            surfaceRect.yMax - offset;

        // Nếu offset lớn quá làm walkY xuống dưới Rect
        // thì fallback về đỉnh Rect.
        if (walkY < surfaceRect.yMin)
            walkY = surfaceRect.yMax;

        return walkY;
    }

    // ═══════════════════════════════════════════════════════════════
    // DETECT CLIMB
    // ═══════════════════════════════════════════════════════════════

    bool OverlapsAny(RectTransform[] surfaces)
    {
        if (surfaces == null)
            return false;

        selfRect.GetWorldCorners(myC);

        Rect me = GetSafeRect(myC);

        foreach (var s in surfaces)
        {
            if (s == null)
                continue;

            s.GetWorldCorners(tmpC);

            if (me.Overlaps(GetSafeRect(tmpC)))
                return true;
        }

        return false;
    }

    // ═══════════════════════════════════════════════════════════════
    // RECT AN TOÀN KHI SCALE X ÂM
    // ═══════════════════════════════════════════════════════════════

    Rect GetSafeRect(Vector3[] c)
    {
        float xMin =
            Mathf.Min(c[0].x, c[1].x, c[2].x, c[3].x);

        float xMax =
            Mathf.Max(c[0].x, c[1].x, c[2].x, c[3].x);

        float yMin =
            Mathf.Min(c[0].y, c[1].y, c[2].y, c[3].y);

        float yMax =
            Mathf.Max(c[0].y, c[1].y, c[2].y, c[3].y);

        return new Rect(
            xMin,
            yMin,
            xMax - xMin,
            yMax - yMin
        );
    }

    // ═══════════════════════════════════════════════════════════════
    // DEBUG GIZMOS
    // ═══════════════════════════════════════════════════════════════

    void OnDrawGizmosSelected()
    {
        RectTransform rt =
            selfRect != null
                ? selfRect
                : GetComponent<RectTransform>();

        if (rt != null)
        {
            Vector3[] c = new Vector3[4];

            rt.GetWorldCorners(c);

            Rect r = GetSafeRect(c);

            // Đường xanh = chân Player
            Gizmos.color = Color.green;

            Gizmos.DrawLine(
                new Vector3(
                    r.xMin,
                    r.yMin,
                    transform.position.z
                ),
                new Vector3(
                    r.xMax,
                    r.yMin,
                    transform.position.z
                )
            );
        }

        if (runSurfaces != null)
        {
            Gizmos.color = Color.yellow;

            Vector3[] c = new Vector3[4];

            foreach (var s in runSurfaces)
            {
                if (s == null)
                    continue;

                s.GetWorldCorners(c);

                Rect sRect =
                    GetSafeRect(c);

                float walkY =
                    GetWalkY(s, sRect);

                // Đường vàng = mặt Player đứng
                Gizmos.DrawLine(
                    new Vector3(
                        sRect.xMin,
                        walkY,
                        transform.position.z
                    ),
                    new Vector3(
                        sRect.xMax,
                        walkY,
                        transform.position.z
                    )
                );
            }
        }
    }
}