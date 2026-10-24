using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

public class CreateListUI : EditorWindow
{
    [MenuItem("Tools/Tạo Nhanh Khung Xếp Hạng")]
    public static void FixUI()
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/Biology/XepHang.unity");
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas != null)
        {
            Transform oldList = canvas.transform.Find("List");
            if (oldList != null) DestroyImmediate(oldList.gameObject);

            // 1. Create List (ScrollRect + Image)
            GameObject listObj = new GameObject("List", typeof(RectTransform));
            listObj.transform.SetParent(canvas.transform, false);
            
            RectTransform listRt = listObj.GetComponent<RectTransform>();
            listRt.anchorMin = new Vector2(0.05f, 0.05f);
            listRt.anchorMax = new Vector2(0.95f, 0.65f);
            listRt.offsetMin = Vector2.zero;
            listRt.offsetMax = Vector2.zero;

            Image listImg = listObj.AddComponent<Image>();
            listImg.color = new Color(1f, 1f, 1f, 0.4f);

            ScrollRect scrollRect = listObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 35f;

            // 2. Create Viewport (Mask)
            GameObject viewportObj = new GameObject("Viewport", typeof(RectTransform));
            viewportObj.transform.SetParent(listObj.transform, false);
            
            RectTransform viewportRt = viewportObj.GetComponent<RectTransform>();
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = Vector2.zero;
            viewportRt.offsetMax = Vector2.zero;
            viewportRt.pivot = new Vector2(0, 1);

            viewportObj.AddComponent<Image>().color = new Color(1,1,1,0.01f);
            viewportObj.AddComponent<Mask>().showMaskGraphic = false;

            // 3. Create Content (VerticalLayoutGroup)
            GameObject contentObj = new GameObject("Content", typeof(RectTransform));
            contentObj.transform.SetParent(viewportObj.transform, false);
            
            RectTransform contentRt = contentObj.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot = new Vector2(0.5f, 1);
            contentRt.offsetMin = new Vector2(0, 0);
            contentRt.offsetMax = new Vector2(0, 0);

            VerticalLayoutGroup vlg = contentObj.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 15;
            vlg.padding = new RectOffset(30, 30, 30, 30);

            ContentSizeFitter csf = contentObj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewportRt;
            scrollRect.content = contentRt;

            EditorSceneManager.SaveScene(scene);
            Debug.Log("Đã tạo thành công ScrollView chuẩn cho XepHang!");
        }
    }
}
