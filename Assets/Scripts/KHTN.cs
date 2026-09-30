using UnityEngine;
using System.Collections;

public class NPCDropBounce : MonoBehaviour
{
    public float dropHeight = 3f;      // Độ cao bắt đầu rơi
    public float dropDuration = 0.8f;  // Thời gian rơi

    public float bounceHeight = 0.35f; // Độ cao nảy lần đầu
    public int bounceCount = 3;        // Số lần nảy
    public float bounceDuration = 0.25f;

    private Vector3 targetPosition;

    void Start()
    {
        targetPosition = transform.position;

        // Đưa NPC lên cao trước khi bắt đầu
        transform.position = targetPosition + Vector3.up * dropHeight;

        StartCoroutine(DropAndBounce());
    }

    IEnumerator DropAndBounce()
    {
        // RƠI XUỐNG
        Vector3 startPosition = transform.position;

        float time = 0f;

        while (time < dropDuration)
        {
            time += Time.deltaTime;

            float t = time / dropDuration;

            // Rơi nhanh dần
            t = t * t;

            transform.position =
                Vector3.Lerp(startPosition, targetPosition, t);

            yield return null;
        }

        transform.position = targetPosition;

        // NẢY TƯNG TƯNG
        float currentBounceHeight = bounceHeight;

        for (int i = 0; i < bounceCount; i++)
        {
            // Bay lên
            time = 0f;

            while (time < bounceDuration)
            {
                time += Time.deltaTime;

                float t = time / bounceDuration;

                transform.position = Vector3.Lerp(
                    targetPosition,
                    targetPosition + Vector3.up * currentBounceHeight,
                    t
                );

                yield return null;
            }

            // Rơi xuống
            time = 0f;

            while (time < bounceDuration)
            {
                time += Time.deltaTime;

                float t = time / bounceDuration;

                transform.position = Vector3.Lerp(
                    targetPosition + Vector3.up * currentBounceHeight,
                    targetPosition,
                    t
                );

                yield return null;
            }

            // Mỗi lần nảy thấp dần
            currentBounceHeight *= 0.5f;
        }

        transform.position = targetPosition;
    }
}