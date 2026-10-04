using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(RectTransform))]
public class PlayerController : MonoBehaviour
{
    [Header("=== DI CHUYỂN ===")]
    public float moveSpeed = 300f;
    public float jumpForce = 600f;
    public float gravity = -2000f;
    public float maxFallSpeed = 1500f;

    [Header("=== LEO THANG ===")]
    public float climbSpeed = 300f;

    [Header("=== TINH CHỈNH VA CHẠM ===")]

    [Tooltip(
        "Khoảng cách từ đáy RectTransform của Player lên vị trí BÀN CHÂN thật.\n" +
        "Nếu Player còn bay trên nền: TĂNG giá trị này.\n" +
        "Nếu Player bị lún vào nền: GIẢM giá trị này."
    )]
    public float feetOffset = 45f;

    [Tooltip("Thu hẹp vùng va chạm ngang của Player.")]
    public float sideShrink = 15f;

    [Tooltip(
        "Dung sai khi kiểm tra Player vừa đi xuyên qua mặt Platform.\n" +
        "Thông thường để 15-25."
    )]
    public float landingTolerance = 20f;

    [Tooltip(
        "Độ sâu tối đa cho phép sửa khi Player đang hơi lún vào Platform."
    )]
    public float maxGroundSnap = 100f;


    // =========================
    // COMPONENTS
    // =========================

    private RectTransform rectTransform;
    private Animator animator;


    // =========================
    // STATE
    // =========================

    private Vector2 velocity;

    private bool isGrounded;
    private bool isOnLadder;
    private bool isClimbing;

    private float moveInput;
    private float climbInput;

    private Vector2 startPos;

    private LadderZone currentLadder;

    // Thời gian tạm bỏ va chạm nền khi leo xuống
    private float dropCooldown = 0f;

    // Moving Platform Player đang đứng trên
    private MovingPlatform currentMovingPlatform;

    // Platform di chuyển dọc
    private VerticalMovingPlatform currentVerticalPlatform;

    // Cloud Player đang đứng trên
    private CloudMove currentCloud;


    // =========================
    // ANIMATOR HASH
    // =========================

    private static readonly int AnimSpeed = Animator.StringToHash("Speed");
    private static readonly int AnimIsRunning = Animator.StringToHash("isRunning");
    private static readonly int AnimIsJumping = Animator.StringToHash("IsJumping");
    private static readonly int AnimIsClimbing = Animator.StringToHash("IsClimbing");
    private static readonly int AnimIsClimbingLower = Animator.StringToHash("isClimbing");
    private static readonly int AnimIsGrounded = Animator.StringToHash("IsGrounded");


    // =========================
    // UNITY
    // =========================

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        animator = GetComponent<Animator>();

        startPos = rectTransform.anchoredPosition;
    }


    void Start()
    {
        if (feetOffset == -80f || Mathf.Abs(feetOffset) > rectTransform.sizeDelta.y)
        {
            // Tự động tính feetOffset dựa trên nửa chiều cao, lui lên 1 tí (khoảng 45% chiều cao tính từ tâm)
            feetOffset = -(rectTransform.sizeDelta.y * 0.45f);
        }

        if (GameData.Instance != null && GameData.Instance.hasSavedPosition)
        {
            rectTransform.anchoredPosition = GameData.Instance.lastPlayerPosition;
            GameData.Instance.hasSavedPosition = false;
        }

        if (Platform.AllPlatforms.Count == 0)
        {
            Debug.LogWarning(
                "⚠️ Không tìm thấy Platform nào trong Scene!"
            );
        }
    }


    void Update()
    {
        // =========================
        // DROP COOLDOWN
        // =========================

        if (dropCooldown > 0f)
        {
            dropCooldown -= Time.deltaTime;
        }


        // =========================
        // RƠI KHỎI MAP -> RESPAWN
        // =========================
        
        float deathY = -2000f;
        CameraFollow camFollow = Object.FindAnyObjectByType<CameraFollow>();
        if (camFollow != null)
        {
            // Điểm chết là dưới mép dưới của camera ảo 200px
            deathY = camFollow.GetVirtualBottomY() - 200f;
        }

        // Chết khi rớt xuống dưới điểm chết (tính theo anchoredPosition vì nó không đổi khi WorldContainer kéo xuống)
        if (rectTransform.anchoredPosition.y < deathY || rectTransform.anchoredPosition.y < -2000f)
        {
            Respawn();
            return;
        }


        // =========================
        // INPUT
        // =========================

        moveInput = Input.GetAxisRaw("Horizontal");
        climbInput = Input.GetAxisRaw("Vertical");


        // =========================
        // KIỂM TRA THANG
        // =========================

        LadderZone activeLadder = GetActiveLadder();

        isOnLadder = activeLadder != null;


        // =========================
        // KIỂM TRA LEO & NHẢY BẰNG W
        // =========================

        bool isWPressed = Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
        bool canClimbUp = true;

        if (activeLadder != null && climbInput > 0.01f)
        {
            float feetY = GetFeetWorldY();
            float ladderTop = GetWorldRect(activeLadder.RectTransform).yMax;
            if (feetY >= ladderTop - 10f)
            {
                canClimbUp = false;
            }
        }

        bool shouldStartClimbing = isOnLadder && Mathf.Abs(climbInput) > 0.01f;
        if (climbInput > 0.01f && !canClimbUp) shouldStartClimbing = false;

        // =========================
        // BẮT ĐẦU LEO
        // =========================

        if (shouldStartClimbing)
        {
            if (!isClimbing)
            {
                // Hút Player vào giữa thang
                Vector3 playerWorldPos = rectTransform.position;

                playerWorldPos.x =
                    activeLadder.RectTransform.position.x;

                rectTransform.position = playerWorldPos;


                // Leo xuống từ trên Platform
                if (climbInput < -0.01f && isGrounded)
                {
                    dropCooldown = 0.25f;
                }
            }

            isClimbing = true;
            currentLadder = activeLadder;
        }


        // =========================
        // RỜI THANG
        // =========================

        if (!isOnLadder)
        {
            isClimbing = false;
            currentLadder = null;
        }


        // Bấm ngang -> rời thang
        if (
            isClimbing &&
            Mathf.Abs(moveInput) > 0.01f &&
            Mathf.Abs(climbInput) < 0.01f
        )
        {
            isClimbing = false;
            currentLadder = null;
        }


        // =========================
        // VELOCITY
        // =========================

        if (isClimbing)
        {
            velocity.x = 0f;
            velocity.y = climbInput * climbSpeed;
        }
        else
        {
            velocity.x = moveInput * moveSpeed;

            velocity.y += gravity * Time.deltaTime;

            velocity.y = Mathf.Max(
                velocity.y,
                -maxFallSpeed
            );
        }


        // =========================
        // JUMP
        // =========================

        bool allowWJump = (!isOnLadder) || (isOnLadder && !canClimbUp);
        bool jumpRequested = Input.GetButtonDown("Jump") || (allowWJump && isWPressed);

        if (
            jumpRequested &&
            (isGrounded || isClimbing)
        )
        {
            isClimbing = false;
            currentLadder = null;

            velocity.y = jumpForce;

            isGrounded = false;
        }


        // =====================================================
        // LƯU VỊ TRÍ CHÂN TRƯỚC KHI DI CHUYỂN
        // =====================================================

        // Đây là điểm rất quan trọng.
        // Ta dùng vị trí chân frame trước để xác định
        // Player có vừa đi xuyên qua mặt Platform hay không.
        float previousFeetWorldY = GetFeetWorldY();


        // =========================
        // DI CHUYỂN
        // =========================

        Vector2 pos = rectTransform.anchoredPosition;

        pos.x += velocity.x * Time.deltaTime;
        pos.y += velocity.y * Time.deltaTime;

        rectTransform.anchoredPosition = pos;


        // Reset trạng thái mặt đất cho frame mới.
        // ResolveCollisions / ClampToLadder sẽ bật lại.
        isGrounded = false;
        currentMovingPlatform = null;
        currentVerticalPlatform = null;
        currentCloud = null;

        // =========================
        // GIỚI HẠN THANG
        // =========================

        if (isClimbing && currentLadder != null)
        {
            ClampToLadder();
        }

        // =========================
        // VA CHẠM PLATFORM
        // =========================

        ResolveCollisions(previousFeetWorldY);

        // =========================
        // VA CHẠM TƯỜNG (TRANG TRÍ)
        // =========================

        ResolveWallCollisions();

        // =========================
        // MOVING PLATFORM
        // =========================

        if (
            currentMovingPlatform != null &&
            isGrounded
        )
        {
            Vector2 ridePos =
                rectTransform.anchoredPosition;

            ridePos +=
                currentMovingPlatform.FrameDelta;

            rectTransform.anchoredPosition =
                ridePos;
        }

        if (
            currentVerticalPlatform != null &&
            isGrounded
        )
        {
            Vector2 ridePos =
                rectTransform.anchoredPosition;

            ridePos +=
                currentVerticalPlatform.FrameDelta;

            rectTransform.anchoredPosition =
                ridePos;
        }


        // =========================
        // CLOUD RIDE (trôi theo mây)
        // =========================

        if (
            currentCloud != null &&
            isGrounded &&
            currentMovingPlatform == null
        )
        {
            Vector2 ridePos =
                rectTransform.anchoredPosition;

            ridePos += currentCloud.FrameDelta;

            rectTransform.anchoredPosition =
                ridePos;
        }


        // =========================
        // VISUAL
        // =========================

        FlipSprite();
        UpdateAnimator();
    }


    // =========================================================
    // RESPAWN
    // =========================================================

    private void Respawn()
    {
        rectTransform.anchoredPosition = startPos;

        velocity = Vector2.zero;

        isClimbing = false;
        isGrounded = false;

        currentLadder = null;
        currentMovingPlatform = null;

        CameraFollow camFollow = Object.FindAnyObjectByType<CameraFollow>();
        if (camFollow != null)
        {
            camFollow.ResetCamera();
        }
    }


    // =========================================================
    // LADDER CLAMP
    // =========================================================

    private void ClampToLadder()
    {
        if (currentLadder == null)
            return;

        Rect ladderRect =
            GetWorldRect(currentLadder.RectTransform);

        float playerScaleY =
            GetPlayerScaleY();

        float playerFeetWorld =
            GetFeetWorldY();


        // =====================================================
        // TÌM ĐỈNH VÀ CHÂN THANG DỰA VÀO PLATFORM
        // =====================================================

        float maxClimbY = ladderRect.yMax;
        float minClimbY = ladderRect.yMin;
        
        bool foundTopPlatform = false;
        bool foundBottomPlatform = false;
        
        float highestPlatformY = float.NegativeInfinity;
        float lowestPlatformY = float.PositiveInfinity;

        foreach (Platform platform in Platform.AllPlatforms)
        {
            if (platform == null || platform.RectTransform == null)
                continue;

            Rect platformRect =
                GetWorldRect(platform.RectTransform);

            // Thang phải nằm ngang trong Platform (thêm dung sai cho thang ở mép)
            float expandX = 20f * playerScaleY;
            bool horizontalOverlap =
                ladderRect.xMax + expandX > platformRect.xMin &&
                ladderRect.xMin - expandX < platformRect.xMax;

            if (!horizontalOverlap)
                continue;

            float platformSurfaceY = platform.GetSurfaceWorldY();

            // Tìm Platform Đỉnh (Top)
            if (platformSurfaceY >= ladderRect.yMin && platformSurfaceY <= ladderRect.yMax + (150f * playerScaleY))
            {
                if (!foundTopPlatform || platformSurfaceY > highestPlatformY)
                {
                    highestPlatformY = platformSurfaceY;
                    foundTopPlatform = true;
                }
            }

            // Tìm Platform Chân (Bottom)
            if (platformSurfaceY >= ladderRect.yMin - (150f * playerScaleY) && platformSurfaceY <= ladderRect.yMax)
            {
                if (!foundBottomPlatform || platformSurfaceY < lowestPlatformY)
                {
                    lowestPlatformY = platformSurfaceY;
                    foundBottomPlatform = true;
                }
            }
        }

        if (foundTopPlatform) maxClimbY = highestPlatformY;
        if (foundBottomPlatform) minClimbY = lowestPlatformY;


        // =====================================================
        // XỬ LÝ KHI CHẠM CHÂN THANG
        // =====================================================

        if (playerFeetWorld <= minClimbY)
        {
            float difference =
                minClimbY - playerFeetWorld;

            MovePlayerWorldY(difference);

            velocity.y = 0f;

            isClimbing = false;
            currentLadder = null;
            isGrounded = true; // Bắt buộc chạm đất để không bị rơi
            
            // Xoá cooldown rơi xuyên nền để nhân vật không bị lọt xuống đất khi ấn nút xuống
            dropCooldown = 0f;

            return;
        }


        // =====================================================
        // XỬ LÝ KHI CHẠM ĐỈNH THANG
        // =====================================================

        if (playerFeetWorld >= maxClimbY)
        {
            float difference =
                playerFeetWorld - maxClimbY;

            // Hạ Player xuống để chân đúng mặt Platform
            MovePlayerWorldY(-difference);

            velocity.y = 0f;

            isClimbing = false;
            currentLadder = null;

            isGrounded = true;
        }
    }


    // =========================================================
    // PLATFORM COLLISION
    // =========================================================

    private void ResolveCollisions(
        float previousFeetWorldY
    )
    {
        // Player đang bay lên -> xuyên qua Platform
        if (velocity.y > 0f)
            return;


        // Đang chủ động leo -> không chặn bởi Platform
        if (
            isClimbing &&
            Mathf.Abs(climbInput) > 0.01f
        )
        {
            return;
        }


        // Đang leo xuống xuyên qua nền
        if (dropCooldown > 0f)
            return;


        float playerScaleY =
            GetPlayerScaleY();


        Rect playerRect =
            GetWorldRect(rectTransform);


        // Thu nhỏ vùng ngang
        float shrink =
            sideShrink * playerScaleY;

        playerRect.xMin += shrink;
        playerRect.xMax -= shrink;


        // Điểm chân hiện tại
        float currentFeetWorldY =
            playerRect.yMin +
            feetOffset * playerScaleY;


        float playerCenterY =
            (playerRect.yMin +
             playerRect.yMax) * 0.5f;


        float tolerance =
            landingTolerance * playerScaleY;


        float maxSnap =
            maxGroundSnap * playerScaleY;


        // =====================================================
        // TÌM PLATFORM PHÙ HỢP NHẤT
        // =====================================================

        Platform bestPlatform = null;

        float bestSurfaceY =
            float.NegativeInfinity;


        foreach (Platform platform in Platform.AllPlatforms)
        {
            if (platform == null)
                continue;

            if (platform.RectTransform == null)
                continue;


            Rect platformRect =
                GetWorldRect(
                    platform.RectTransform
                );


            // =============================================
            // CHECK NGANG
            // =============================================

            bool horizontalOverlap =
                playerRect.xMax > platformRect.xMin &&
                playerRect.xMin < platformRect.xMax;


            if (!horizontalOverlap)
                continue;


            // =============================================
            // MẶT PLATFORM THẬT
            // =============================================

            float surfaceY =
                platform.GetSurfaceWorldY();


            // =============================================
            // PLAYER VỪA ĐI QUA MẶT PLATFORM
            // =============================================

            bool crossedSurface =
                previousFeetWorldY >=
                surfaceY - tolerance
                &&
                currentFeetWorldY <=
                surfaceY + tolerance;


            // =============================================
            // FIX TRƯỜNG HỢP PLAYER HƠI LÚN
            // =============================================

            float penetration =
                surfaceY -
                currentFeetWorldY;


            bool shallowPenetration =
                currentFeetWorldY <= surfaceY
                &&
                penetration >= 0f
                &&
                penetration <= maxSnap
                &&
                playerCenterY >= surfaceY;


            if (
                !crossedSurface &&
                !shallowPenetration
            )
            {
                continue;
            }


            // Nếu nhiều Platform chồng nhau,
            // chọn mặt cao nhất.
            if (
                bestPlatform == null ||
                surfaceY > bestSurfaceY
            )
            {
                bestPlatform = platform;
                bestSurfaceY = surfaceY;
            }
        }


        // =====================================================
        // KHÔNG CÓ PLATFORM
        // =====================================================

        if (bestPlatform == null)
            return;


        // =====================================================
        // SNAP CHÂN PLAYER VÀO MẶT PLATFORM
        // =====================================================

        float worldDifference =
            bestSurfaceY -
            currentFeetWorldY;


        MovePlayerWorldY(
            worldDifference
        );


        velocity.y = 0f;
        isGrounded = true;


        // Đáp đất -> thoát leo
        if (isClimbing)
        {
            isClimbing = false;
            currentLadder = null;
        }


        // =====================================================
        // MOVING PLATFORM
        // =====================================================

        MovingPlatform movingPlatform =
            bestPlatform.GetComponent<MovingPlatform>();

        if (movingPlatform != null)
        {
            currentMovingPlatform =
                movingPlatform;
        }

        VerticalMovingPlatform vMovingPlatform = 
            bestPlatform.GetComponent<VerticalMovingPlatform>();

        if (vMovingPlatform != null)
        {
            currentVerticalPlatform = vMovingPlatform;
        }


        // =====================================================
        // CLOUD (trôi theo mây)
        // =====================================================

        CloudMove cloud =
            bestPlatform.GetComponent<CloudMove>();

        if (cloud != null)
        {
            currentCloud = cloud;
        }
    }


    // =========================================================
    // WALL COLLISION (chặn bởi vật trang trí)
    // =========================================================

    private void ResolveWallCollisions()
    {
        if (WallBlock.AllWalls.Count == 0) return;

        Rect playerRect = GetWorldRect(rectTransform);
        float shrink = sideShrink * GetPlayerScaleY();
        playerRect.xMin += shrink;
        playerRect.xMax -= shrink;

        // Thu nhỏ chiều dọc để tránh chặn khi nhảy qua đầu tường
        float vShrink = playerRect.height * 0.3f;
        playerRect.yMin += vShrink;

        foreach (WallBlock wall in WallBlock.AllWalls)
        {
            if (wall == null || wall.RectTransform == null) continue;

            Rect wallRect = GetWorldRect(wall.RectTransform);

            // Kiểm tra chồng chéo
            bool overlap = playerRect.xMin < wallRect.xMax && playerRect.xMax > wallRect.xMin &&
                           playerRect.yMin < wallRect.yMax && playerRect.yMax > wallRect.yMin;

            if (!overlap) continue;

            // Tính hướng đẩy ra: đẩy sang bên gần nhất
            float overlapLeft = playerRect.xMax - wallRect.xMin;
            float overlapRight = wallRect.xMax - playerRect.xMin;

            Vector2 pos = rectTransform.anchoredPosition;

            if (overlapLeft < overlapRight)
            {
                // Đẩy player sang TRÁI
                pos.x -= overlapLeft;
                if (velocity.x > 0) velocity.x = 0;
            }
            else
            {
                // Đẩy player sang PHẢI
                pos.x += overlapRight;
                if (velocity.x < 0) velocity.x = 0;
            }

            rectTransform.anchoredPosition = pos;
        }
    }


    // =========================================================
    // GET ACTIVE LADDER
    // =========================================================

    private LadderZone GetActiveLadder()
    {
        float playerScaleY =
            GetPlayerScaleY();


        Rect playerRect =
            GetWorldRect(rectTransform);


        // Chỉ lấy vùng giữa Player
        float shrinkX =
            playerRect.width * 0.35f;


        playerRect.xMin += shrinkX;
        playerRect.xMax -= shrinkX;


        foreach (
            LadderZone ladder
            in LadderZone.AllLadders
        )
        {
            if (ladder == null)
                continue;


            Rect ladderRect =
                GetWorldRect(
                    ladder.RectTransform
                );


            // Cho phép bắt thang cao hơn một chút
            ladderRect.yMax +=
                80f * playerScaleY;


            bool overlap =
                playerRect.xMax > ladderRect.xMin &&
                playerRect.xMin < ladderRect.xMax &&
                playerRect.yMax > ladderRect.yMin &&
                playerRect.yMin < ladderRect.yMax;


            if (overlap)
            {
                return ladder;
            }
        }


        return null;
    }


    // =========================================================
    // VỊ TRÍ CHÂN PLAYER
    // =========================================================

    private float GetFeetWorldY()
    {
        Rect playerRect =
            GetWorldRect(rectTransform);

        float playerScaleY =
            GetPlayerScaleY();

        return
            playerRect.yMin +
            feetOffset * playerScaleY;
    }


    // =========================================================
    // PLAYER SCALE
    // =========================================================

    private float GetPlayerScaleY()
    {
        float scaleY =
            Mathf.Abs(
                rectTransform.lossyScale.y
            );

        if (scaleY < 0.0001f)
            scaleY = 1f;

        return scaleY;
    }


    // =========================================================
    // DỊCH PLAYER THEO WORLD Y
    // =========================================================

    private void MovePlayerWorldY(
        float worldDeltaY
    )
    {
        Vector3 worldPos =
            rectTransform.position;

        worldPos.y += worldDeltaY;

        rectTransform.position =
            worldPos;
    }


    // =========================================================
    // WORLD RECT
    // =========================================================

    private Rect GetWorldRect(
        RectTransform rt
    )
    {
        Vector3[] corners =
            new Vector3[4];

        rt.GetWorldCorners(corners);


        float xMin = Mathf.Min(
            corners[0].x,
            corners[1].x,
            corners[2].x,
            corners[3].x
        );


        float xMax = Mathf.Max(
            corners[0].x,
            corners[1].x,
            corners[2].x,
            corners[3].x
        );


        float yMin = Mathf.Min(
            corners[0].y,
            corners[1].y,
            corners[2].y,
            corners[3].y
        );


        float yMax = Mathf.Max(
            corners[0].y,
            corners[1].y,
            corners[2].y,
            corners[3].y
        );


        return new Rect(
            xMin,
            yMin,
            xMax - xMin,
            yMax - yMin
        );
    }


    // =========================================================
    // FLIP
    // =========================================================

    private void FlipSprite()
    {
        Vector3 scale =
            rectTransform.localScale;


        if (!isClimbing)
        {
            if (moveInput > 0.01f)
            {
                scale.x =
                    Mathf.Abs(scale.x);
            }
            else if (moveInput < -0.01f)
            {
                scale.x =
                    -Mathf.Abs(scale.x);
            }


            rectTransform.localScale =
                scale;
        }
    }


    // =========================================================
    // ANIMATOR
    // =========================================================

    private void UpdateAnimator()
    {
        if (animator == null)
            return;


        animator.SetFloat(AnimSpeed, Mathf.Abs(moveInput));
        animator.SetBool(AnimIsRunning, Mathf.Abs(moveInput) > 0.01f);

        animator.SetBool(AnimIsGrounded, isGrounded);

        animator.SetBool(AnimIsJumping, !isGrounded && !isClimbing);

        animator.SetBool(AnimIsClimbing, isClimbing);
        animator.SetBool(AnimIsClimbingLower, isClimbing);


        if (isClimbing)
        {
            animator.speed =
                Mathf.Abs(climbInput) < 0.01f
                ? 0f
                : 1f;
        }
        else
        {
            animator.speed = 1f;
        }
    }
}