using UnityEngine;

public class BeanController : MonoBehaviour
{
    [SerializeField] private RectTransform playerRect;
    [SerializeField] private GameObject beanIcon; // Icon hạt đậu trên HUD
    [SerializeField] private int requiredKills = 6;
    
    private bool pickedUp = false;
    private RectTransform myRect;

    void Start()
    {
        myRect = GetComponent<RectTransform>();

        if (GameSession.hasBean)
        {
            gameObject.SetActive(false);
            if (beanIcon != null) beanIcon.SetActive(true);
            pickedUp = true;
        }
        else if (GameSession.killCount >= requiredKills)
        {
            gameObject.SetActive(true);
            GameSession.beanSpawned = true;
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (gameObject.activeSelf && !pickedUp)
        {
            if (CheckOverlap(myRect, playerRect))
            {
                pickedUp = true;
                GameSession.hasBean = true;
                gameObject.SetActive(false);
                if (beanIcon != null) beanIcon.SetActive(true);
            }
        }
    }

    private bool CheckOverlap(RectTransform rect1, RectTransform rect2)
    {
        if (rect1 == null || rect2 == null) return false;

        Vector3[] corners1 = new Vector3[4];
        rect1.GetWorldCorners(corners1);

        Vector3[] corners2 = new Vector3[4];
        rect2.GetWorldCorners(corners2);

        float minX1 = Mathf.Min(corners1[0].x, corners1[2].x);
        float maxX1 = Mathf.Max(corners1[0].x, corners1[2].x);
        float minY1 = Mathf.Min(corners1[0].y, corners1[2].y);
        float maxY1 = Mathf.Max(corners1[0].y, corners1[2].y);

        float minX2 = Mathf.Min(corners2[0].x, corners2[2].x);
        float maxX2 = Mathf.Max(corners2[0].x, corners2[2].x);
        float minY2 = Mathf.Min(corners2[0].y, corners2[2].y);
        float maxY2 = Mathf.Max(corners2[0].y, corners2[2].y);

        return minX1 < maxX2 && maxX1 > minX2 && minY1 < maxY2 && maxY1 > minY2;
    }
}
