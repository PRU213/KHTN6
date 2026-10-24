using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float yOffset = 0f;
    private RectTransform worldContainer;
    private Canvas canvas;

    private static CameraFollow instance;

    void Awake()
    {
        instance = this;
        GameObject canvasGo = GameObject.Find("Canvas");
        if (canvasGo != null)
        {
            canvas = canvasGo.GetComponent<Canvas>();
        }

        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            // Lấy danh sách tất cả các child hiện tại của Canvas
            Transform[] children = new Transform[canvas.transform.childCount];
            for (int i = 0; i < canvas.transform.childCount; i++)
            {
                children[i] = canvas.transform.GetChild(i);
            }

            // Tạo WorldContainer
            GameObject go = new GameObject("WorldContainer");
            worldContainer = go.AddComponent<RectTransform>();
            worldContainer.SetParent(canvas.transform, false);
            
            // Ép WorldContainer phủ kín Canvas
            worldContainer.anchorMin = Vector2.zero;
            worldContainer.anchorMax = Vector2.one;
            worldContainer.offsetMin = Vector2.zero;
            worldContainer.offsetMax = Vector2.zero;

            // Đưa toàn bộ object hiện tại vào trong Container
            Transform pauseBtn = null;
            foreach (Transform child in children)
            {
                if (child != worldContainer && child.name != "btnTamDung" && !child.name.Contains("TamDung"))
                {
                    child.SetParent(worldContainer, true);
                }
                else if (child.name.Contains("TamDung"))
                {
                    pauseBtn = child;
                }
            }

            // Đưa nút Tạm Dừng xuống cuối danh sách để nó đè lên trên tất cả cảnh vật
            if (pauseBtn != null)
            {
                pauseBtn.SetAsLastSibling();
            }
        }
    }

    void Start()
    {
        if (target == null)
        {
            GameObject p = GameObject.Find("Player");
            if (p == null) p = GameObject.Find("player");
            if (p != null) target = p.transform;
        }
    }

    void LateUpdate()
    {
        if (target == null || worldContainer == null) return;

        RectTransform pRt = target.GetComponent<RectTransform>();
        if (pRt != null)
        {
            float targetY = -pRt.anchoredPosition.y + yOffset;
            
            // Không cho cuộn camera xuống dưới âm (không nhìn thấy dưới đất)
            if (targetY > 0) targetY = 0;

            Vector2 pos = worldContainer.anchoredPosition;
            pos.y = targetY;
            worldContainer.anchoredPosition = pos;
        }
    }

    public void ResetCamera()
    {
        if (instance != null && this.worldContainer != null)
        {
            Vector2 pos = this.worldContainer.anchoredPosition;
            pos.y = 0;
            this.worldContainer.anchoredPosition = pos;
        }
    }

    public float GetVirtualBottomY()
    {
        if (instance != null && this.worldContainer != null)
        {
            // Trả về toạ độ Y tương đối ở sát mép dưới màn hình (để check rơi)
            return -this.worldContainer.anchoredPosition.y - 1200f;
        }
        return -2000f;
    }
}