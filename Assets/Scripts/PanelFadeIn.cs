using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class PanelFadeIn : MonoBehaviour
{
    public float duration = 0.5f;

    CanvasGroup group;

    void Awake()
    {
        group = GetComponent<CanvasGroup>();
    }

    void OnEnable()
    {
        StartCoroutine(Fade());
    }

    IEnumerator Fade()
    {
        group.alpha = 0f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            group.alpha = Mathf.Clamp01(t / duration);
            yield return null;
        }
        group.alpha = 1f;
    }
}