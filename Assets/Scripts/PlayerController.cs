using UnityEngine;

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
    [Tooltip("Số DƯƠNG: Kéo hộp va chạm lên. Số ÂM: Kéo hộp xuống.")]
    public float feetOffset = 45f;
    [Tooltip("Thu hẹp hộp va chạm 2 bên.")]
    public float sideShrink = 15f;

    // --- Components ---
    private RectTransform rectTransform;
    private Animator animator;

    // --- State ---
    private Vector2 velocity;
    private bool isGrounded;
    private bool isOnLadder;
    private bool isClimbing;
    private float moveInput;
    private float climbInput;

    private Vector2 startPos;
    private LadderZone currentLadder; // Thang đang bám
    private float dropCooldown = 0f;  // Thời gian tạm xuyên nền để chui xuống thang

    // --- Animator Parameter Hashes ---
    private static readonly int AnimSpeed = Animator.StringToHash("Speed");
    private static readonly int AnimIsJumping = Animator.StringToHash("IsJumping");
    private static readonly int AnimIsClimbing = Animator.StringToHash("IsClimbing");
    private static readonly int AnimIsGrounded = Animator.StringToHash("IsGrounded");

    void Awake()
    {
        if (moveSpeed == 280f || moveSpeed == 400f) moveSpeed = 300f;
        if (jumpForce == 620f || jumpForce == 800f) jumpForce = 600f;
        // override removed

        rectTransform = GetComponent<RectTransform>();
        animator = GetComponent<Animator>();
        startPos = rectTransform.anchoredPosition;
    }

    void Start()
    {
        if (Platform.AllPlatforms.Count == 0)
            Debug.LogWarning("⚠️ Không tìm thấy Platform nào! Chạy Tools > Tự động Setup Scene bean_1");
    }

    void Update()
    {
        if (dropCooldown > 0f) dropCooldown -= Time.deltaTime;

        // Hồi sinh nếu rớt vực
        if (rectTransform.anchoredPosition.y < -2000f)
        {
            rectTransform.anchoredPosition = startPos;
            velocity = Vector2.zero;
            isClimbing = false;
            currentLadder = null;
        }

        moveInput = Input.GetAxisRaw("Horizontal");
        climbInput = Input.GetAxisRaw("Vertical");

        LadderZone activeLadder = GetActiveLadder();
        isOnLadder = (activeLadder != null);

        // === BẮT ĐẦU LEO THANG ===
        if (isOnLadder && Mathf.Abs(climbInput) > 0.01f)
        {
            if (!isClimbing)
            {
                // Hút nhân vật vào giữa thang
                Vector3 myPos = rectTransform.position;
                myPos.x = activeLadder.RectTransform.position.x;
                rectTransform.position = myPos;

                // Muốn leo XUỐNG từ trên nền đá -> tạm xuyên nền 0.25s
                if (climbInput < -0.01f && isGrounded)
                    dropCooldown = 0.25f;
            }
            isClimbing = true;
            currentLadder = activeLadder;
        }

        // Rời thang nếu ra khỏi vùng
        if (!isOnLadder)
        {
            isClimbing = false;
            currentLadder = null;
        }

        // Thoát thang nếu bấm ngang mà không bấm lên/xuống
        if (isClimbing && Mathf.Abs(moveInput) > 0.01f && Mathf.Abs(climbInput) < 0.01f)
        {
            isClimbing = false;
            currentLadder = null;
        }

        // === TÍNH VẬN TỐC ===
        if (isClimbing)
        {
            velocity.x = 0;
            velocity.y = climbInput * climbSpeed;
        }
        else
        {
            velocity.x = moveInput * moveSpeed;
            velocity.y += gravity * Time.deltaTime;
            velocity.y = Mathf.Max(velocity.y, -maxFallSpeed);
        }

        // Nhảy
        if (Input.GetButtonDown("Jump") && (isGrounded || isClimbing))
        {
            isClimbing = false;
            currentLadder = null;
            velocity.y = jumpForce;
            isGrounded = false;
        }

        // === DI CHUYỂN ===
        Vector2 pos = rectTransform.anchoredPosition;
        pos.x += velocity.x * Time.deltaTime;
        pos.y += velocity.y * Time.deltaTime;
        rectTransform.anchoredPosition = pos;

        // === GIỚI HẠN TRONG PHẠM VI THANG (FIX LỖI RỚT MAP) ===
        if (isClimbing && currentLadder != null)
        {
            ClampToLadder();
        }

        // === VA CHẠM ===
        isGrounded = false;
        ResolveCollisions();

        FlipSprite();
        UpdateAnimator();
    }

    /// <summary>
    /// KHÓA CỨNG nhân vật trong phạm vi cái thang.
    /// Không thể tụt thấp hơn chân thang, không thể leo cao hơn đỉnh thang.
    /// Khi chạm đáy thang → tự động thoát leo, để trọng lực đáp xuống nền bên dưới.
    /// </summary>
    private void ClampToLadder()
    {
        float canvasScale = rectTransform.lossyScale.y;
        if (canvasScale == 0) canvasScale = 1f;

        Rect ladderRect = GetWorldRect(currentLadder.RectTransform);

        // Lấy vị trí chân nhân vật (world space)
        Rect playerRect = GetWorldRect(rectTransform);
        float playerFeetWorld = playerRect.yMin + (feetOffset * canvasScale);

        // CHÂN THANG: Nếu chân nhân vật chạm hoặc thấp hơn đáy thang → DỪNG
        if (playerFeetWorld <= ladderRect.yMin)
        {
            // Đẩy nhân vật lên sao cho chân đúng bằng đáy thang
            float diff = ladderRect.yMin - playerFeetWorld;
            Vector2 newPos = rectTransform.anchoredPosition;
            newPos.y += diff / canvasScale;
            rectTransform.anchoredPosition = newPos;

            velocity.y = 0f;
            isClimbing = false;
            currentLadder = null;
        }

        // ĐỈNH THANG: Nếu chân nhân vật cao hơn đỉnh thang → DỪNG
        float ladderTopExtended = ladderRect.yMax + (80f * canvasScale);
        if (playerFeetWorld >= ladderTopExtended)
        {
            float diff = playerFeetWorld - ladderTopExtended;
            Vector2 newPos = rectTransform.anchoredPosition;
            newPos.y -= diff / canvasScale;
            rectTransform.anchoredPosition = newPos;

            velocity.y = 0f;
            isClimbing = false;
            currentLadder = null;
        }
    }

    private void ResolveCollisions()
    {
        // Bay lên hoặc leo lên → xuyên nền
        if (velocity.y > 0) return;
        // Đang leo thang và chủ động di chuyển → bỏ qua va chạm
        if (isClimbing && Mathf.Abs(climbInput) > 0.01f) return;
        // Đang chui qua nền đá để vào thang
        if (dropCooldown > 0f) return;

        float canvasScale = rectTransform.lossyScale.y;
        if (canvasScale == 0) canvasScale = 1f;

        Rect playerRect = GetWorldRect(rectTransform);
        playerRect.yMin += feetOffset * canvasScale;
        playerRect.xMin += sideShrink * canvasScale;
        playerRect.xMax -= sideShrink * canvasScale;

        float velocityWorldY = velocity.y * canvasScale;
        float prevYMin = playerRect.yMin - (velocityWorldY * Time.deltaTime);
        float playerCenterY = (playerRect.yMin + playerRect.yMax) * 0.5f;

        foreach (var platform in Platform.AllPlatforms)
        {
            if (platform == null) continue;

            Rect platRect = GetWorldRect(platform.RectTransform);
            float platformTop = platRect.yMax - (platform.surfaceOffset * canvasScale);

            if (playerRect.xMax <= platRect.xMin || playerRect.xMin >= platRect.xMax)
                continue;

            bool fellFromAbove = prevYMin >= platformTop - 15f;
            bool isCenterAbove = playerCenterY >= platformTop;

            if (playerRect.yMin <= platformTop && (fellFromAbove || isCenterAbove))
            {
                float overlapWorld = platformTop - playerRect.yMin;

                Vector2 newPos = rectTransform.anchoredPosition;
                newPos.y += overlapWorld / canvasScale;
                rectTransform.anchoredPosition = newPos;

                velocity.y = 0f;
                isGrounded = true;

                // Chạm đất thì thoát leo
                if (isClimbing)
                {
                    isClimbing = false;
                    currentLadder = null;
                }

                playerRect = GetWorldRect(rectTransform);
                playerRect.yMin += feetOffset * canvasScale;
                playerRect.xMin += sideShrink * canvasScale;
                playerRect.xMax -= sideShrink * canvasScale;
                prevYMin = playerRect.yMin;
                playerCenterY = (playerRect.yMin + playerRect.yMax) * 0.5f;
            }
        }
    }

    private LadderZone GetActiveLadder()
    {
        float canvasScale = rectTransform.lossyScale.y;
        if (canvasScale == 0) canvasScale = 1f;

        Rect playerRect = GetWorldRect(rectTransform);
        float shrinkX = playerRect.width * 0.35f;
        playerRect.xMin += shrinkX;
        playerRect.xMax -= shrinkX;

        foreach (var ladder in LadderZone.AllLadders)
        {
            if (ladder == null) continue;
            Rect ladderRect = GetWorldRect(ladder.RectTransform);
            ladderRect.yMax += 80f * canvasScale;

            if (playerRect.xMax > ladderRect.xMin && playerRect.xMin < ladderRect.xMax &&
                playerRect.yMax > ladderRect.yMin && playerRect.yMin < ladderRect.yMax)
            {
                return ladder;
            }
        }
        return null;
    }

    private Rect GetWorldRect(RectTransform rt)
    {
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        float xMin = Mathf.Min(corners[0].x, corners[1].x, corners[2].x, corners[3].x);
        float xMax = Mathf.Max(corners[0].x, corners[1].x, corners[2].x, corners[3].x);
        float yMin = Mathf.Min(corners[0].y, corners[1].y, corners[2].y, corners[3].y);
        float yMax = Mathf.Max(corners[0].y, corners[1].y, corners[2].y, corners[3].y);
        return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
    }

    private void FlipSprite()
    {
        Vector3 scale = rectTransform.localScale;
        if (!isClimbing)
        {
            if (moveInput > 0.01f) scale.x = Mathf.Abs(scale.x);
            else if (moveInput < -0.01f) scale.x = -Mathf.Abs(scale.x);
            rectTransform.localScale = scale;
        }
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;
        animator.SetFloat(AnimSpeed, Mathf.Abs(moveInput));
        animator.SetBool(AnimIsGrounded, isGrounded);
        animator.SetBool(AnimIsJumping, !isGrounded && !isClimbing);
        animator.SetBool(AnimIsClimbing, isClimbing);

        if (isClimbing)
        {
            animator.speed = (Mathf.Abs(climbInput) < 0.01f) ? 0f : 1f;
        }
        else
        {
            animator.speed = 1f;
        }
    }
}
