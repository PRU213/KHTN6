using UnityEngine;
using UnityEngine.UI;

public class DifficultyToggleVisual : MonoBehaviour
{
    public Toggle toggle;
    public Image targetImage;
    public Outline outline;

    public Color normalColor = Color.white;
    public Color selectedColor = new Color(1f, 1f, 0.6f, 1f);

    public float selectedScale = 1.08f;

    private Vector3 normalScale;

    void Awake()
    {
        normalScale = transform.localScale;

        if (toggle != null)
        {
            toggle.onValueChanged.AddListener(UpdateVisual);
        }
    }

    void Start()
    {
        if (toggle != null)
        {
            UpdateVisual(toggle.isOn);
        }
    }

    void UpdateVisual(bool isSelected)
    {
        if (targetImage != null)
        {
            targetImage.color =
                isSelected ? selectedColor : normalColor;
        }

        if (outline != null)
        {
            outline.enabled = isSelected;
        }

        transform.localScale =
            isSelected
                ? normalScale * selectedScale
                : normalScale;
    }
}