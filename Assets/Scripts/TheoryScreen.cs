using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TheoryScreen : MonoBehaviour
{
    [Header("3 thẻ (ex1, ex2, ex3)")]
    [SerializeField] Button[] cards;
    [TextArea(2, 6)]
    [SerializeField] string[] contents;        // nội dung giải thích cho từng thẻ

    [Header("Thanh giải thích")]
    [SerializeField] TMP_Text explainText;     // chữ nằm trong object explain
    [SerializeField] float typeSpeed = 0.02f;  // giây cho mỗi chữ

    [Header("Cô giáo")]
    [SerializeField] RectTransform teacher;
    [SerializeField] float bobAmount = 8f;     // độ nhún lên xuống
    [SerializeField] float bobSpeed = 2f;
    [SerializeField] float swayAngle = 1.5f;   // độ nghiêng qua lại
    [SerializeField] float talkBoost = 2f;     // nói thì cử động nhanh hơn

    [Header("Nút quay lại")]
    [SerializeField] Button backButton;
    [SerializeField] GameObject mapPanel;      // màn bản đồ chặng để quay về

    Vector2 teacherStart;
    bool talking;
    Coroutine typing;

    void Awake()
    {
        if (teacher) teacherStart = teacher.anchoredPosition;
        for (int i = 0; i < cards.Length; i++)
        {
            int index = i;
            cards[i].onClick.AddListener(() => ShowContent(index));
        }
        if (backButton) backButton.onClick.AddListener(Back);
    }

    void OnEnable()
    {
        if (explainText) explainText.text = "Bấm vào một thẻ để xem giải thích nhé!";
    }

    void Update()
    {
        if (!teacher) return;
        float k = talking ? talkBoost : 1f;
        float t = Time.time * bobSpeed * k;
        teacher.anchoredPosition = teacherStart + new Vector2(0, Mathf.Sin(t) * bobAmount);
        teacher.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 0.5f) * swayAngle);
        float s = 1f + Mathf.Sin(t) * 0.008f;
        teacher.localScale = new Vector3(s, s, 1f);
    }

    void ShowContent(int index)
    {
        if (index >= contents.Length || !explainText) return;
        if (typing != null) StopCoroutine(typing);
        typing = StartCoroutine(TypeRoutine(contents[index]));
        StartCoroutine(Pop(cards[index].transform));
    }

    IEnumerator TypeRoutine(string message)
    {
        talking = true;
        explainText.text = "";
        foreach (char c in message)
        {
            explainText.text += c;
            yield return new WaitForSeconds(typeSpeed);
        }
        talking = false;
    }

    IEnumerator Pop(Transform card)
    {
        for (float t = 0; t < 1f; t += Time.deltaTime / 0.25f)
        {
            card.localScale = Vector3.one * (1f + Mathf.Sin(t * Mathf.PI) * 0.12f);
            yield return null;
        }
        card.localScale = Vector3.one;
    }

    void Back()
    {
        gameObject.SetActive(false);
        if (mapPanel) mapPanel.SetActive(true);
    }
}