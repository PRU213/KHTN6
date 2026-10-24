using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class KickBall : MonoBehaviour
{
    [Header("Bóng")]
    [SerializeField] RectTransform ball;
    [SerializeField] RectTransform[] goalTargets;
    [SerializeField] Button[] answerButtons;
    [SerializeField] GameObject goalText;

    [SerializeField] float flyTime = 0.85f;
    [SerializeField] float arcHeight = 120f;
    [SerializeField] float endScale = 0.5f;

    [Header("Khi sút trượt")]
    [SerializeField] GameObject missText;

    [Header("Thủ môn")]
    [SerializeField] Animator goalkeeperAnimator;

    Vector2 startPos;
    Vector3 startScale;

    bool busy;

    public bool IsBusy => busy;


    void Start()
    {
        startPos = ball.anchoredPosition;
        startScale = ball.localScale;
    }


    // =========================================================
    // TRẢ LỜI ĐÚNG
    // =========================================================

    public void Goal()
    {
        if (!busy)
        {
            StartCoroutine(
                KickRoutine(true)
            );
        }
    }


    // =========================================================
    // TRẢ LỜI SAI
    // =========================================================

    public void Miss()
    {
        if (!busy)
        {
            StartCoroutine(
                KickRoutine(false)
            );
        }
    }


    // =========================================================
    // SÚT BÓNG
    // =========================================================

    IEnumerator KickRoutine(bool scored)
    {
        busy = true;

        SetButtons(false);


        // =====================================================
        // KIỂM TRA TARGET
        // =====================================================

        if (
            goalTargets == null
            ||
            goalTargets.Length < 2
        )
        {
            Debug.LogError(
                "KickBall: Cần ít nhất 2 Goal Target!"
            );

            SetButtons(true);

            busy = false;

            yield break;
        }


        // =====================================================
        // THỦ MÔN CHỌN HƯỚNG NGẪU NHIÊN
        // =====================================================

        bool keeperDiveRight =
            Random.value < 0.5f;


        // =====================================================
        // CHẠY ANIMATION THỦ MÔN
        // =====================================================

        PlayKeeperAnimation(
            keeperDiveRight
        );


        // =====================================================
        // XÁC ĐỊNH TARGET TRÁI / PHẢI
        // =====================================================

        RectTransform leftTarget =
            goalTargets[0];

        RectTransform rightTarget =
            goalTargets[
                goalTargets.Length - 1
            ];


        Vector2 end;


        // =====================================================
        // TRẢ LỜI ĐÚNG
        // BÓNG BAY NGƯỢC HƯỚNG THỦ MÔN
        // =====================================================

        if (scored)
        {
            if (keeperDiveRight)
            {
                // Thủ môn sang phải
                // bóng sang trái

                end =
                    leftTarget
                        .anchoredPosition;

                Debug.Log(
                    "ĐÚNG: Keeper Right -> Ball Left"
                );
            }
            else
            {
                // Thủ môn sang trái
                // bóng sang phải

                end =
                    rightTarget
                        .anchoredPosition;

                Debug.Log(
                    "ĐÚNG: Keeper Left -> Ball Right"
                );
            }
        }

        // =====================================================
        // TRẢ LỜI SAI
        // BÓNG BAY CÙNG HƯỚNG THỦ MÔN
        // =====================================================

        else
        {
            if (keeperDiveRight)
            {
                // Thủ môn sang phải
                // bóng bay ra ngoài bên phải
                end = rightTarget.anchoredPosition;
                end.x += 250f;

                Debug.Log("SAI: bóng bay ra ngoài bên phải");
            }
            else
            {
                // Thủ môn sang trái
                // bóng bay ra ngoài bên trái
                end = leftTarget.anchoredPosition;
                end.x -= 250f;

                Debug.Log("SAI: bóng bay ra ngoài bên trái");
            }
        }


        // =====================================================
        // HƯỚNG XOAY BÓNG
        // =====================================================

        float dir =
            end.x >= startPos.x
                ? 1f
                : -1f;


        // =====================================================
        // BÓNG BAY
        // =====================================================

        for (
            float t = 0;
            t < 1f;
            t += Time.deltaTime / flyTime
        )
        {
            float e =
                1f
                -
                (1f - t)
                *
                (1f - t);


            Vector2 pos =
                Vector2.Lerp(
                    startPos,
                    end,
                    e
                );


            // đường cong bay
            pos.y +=
                Mathf.Sin(
                    t * Mathf.PI
                )
                *
                arcHeight;


            ball.anchoredPosition =
                pos;


            // bóng nhỏ dần
            ball.localScale =
                Vector3.Lerp(
                    startScale,
                    startScale * endScale,
                    e
                );


            // bóng xoay
            ball.localRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    -dir * 720f * t
                );


            yield return null;
        }


        // =====================================================
        // BÓNG ĐẾN ĐÍCH
        // =====================================================

        ball.anchoredPosition =
            end;


        // =====================================================
        // HIỆN KẾT QUẢ
        // =====================================================

        if (scored)
        {
            if (goalText != null)
            {
                goalText.SetActive(true);
            }
        }
        else
        {
            if (missText != null)
            {
                missText.SetActive(true);
            }
        }


        yield return
            new WaitForSeconds(1f);


        // =====================================================
        // TẮT TEXT
        // =====================================================

        if (goalText != null)
        {
            goalText.SetActive(false);
        }


        if (missText != null)
        {
            missText.SetActive(false);
        }


        // =====================================================
        // RESET BÓNG
        // =====================================================

        ball.anchoredPosition =
            startPos;


        ball.localScale =
            startScale;


        ball.localRotation =
            Quaternion.identity;


        // =====================================================
        // MỞ LẠI BUTTON
        // =====================================================

        SetButtons(true);


        busy = false;
    }


    // =========================================================
    // ANIMATION THỦ MÔN
    // =========================================================

    void PlayKeeperAnimation(
        bool keeperDiveRight
    )
    {
        if (goalkeeperAnimator == null)
        {
            Debug.LogWarning(
                "KickBall: Chưa gán Goalkeeper Animator!"
            );

            return;
        }


        // Reset trigger cũ
        goalkeeperAnimator.ResetTrigger(
            "DiveLeft"
        );

        goalkeeperAnimator.ResetTrigger(
            "DiveRight"
        );


        // =====================================================
        // ĐỔ PHẢI
        // =====================================================

        if (keeperDiveRight)
        {
            goalkeeperAnimator.SetTrigger(
                "DiveRight"
            );

            Debug.Log(
                "KEEPER -> DiveRight"
            );
        }

        // =====================================================
        // ĐỔ TRÁI
        // =====================================================

        else
        {
            goalkeeperAnimator.SetTrigger(
                "DiveLeft"
            );

            Debug.Log(
                "KEEPER -> DiveLeft"
            );
        }
    }


    // =========================================================
    // KHÓA / MỞ BUTTON ĐÁP ÁN
    // =========================================================

    void SetButtons(bool on)
    {
        if (answerButtons == null)
        {
            return;
        }


        foreach (
            Button b
            in answerButtons
        )
        {
            if (b != null)
            {
                b.interactable =
                    on;
            }
        }
    }
}