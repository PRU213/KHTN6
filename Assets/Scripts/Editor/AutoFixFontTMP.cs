using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

[InitializeOnLoad]
public class SwapToLegacyText
{
    static SwapToLegacyText()
    {
        EditorApplication.delayCall += DoSwap;
    }

    static void DoSwap()
    {
        if (EditorPrefs.GetBool("HasSwappedToLegacyText_Fix", false)) return;
        
        bool changed = false;
        Font arial = AssetDatabase.LoadAssetAtPath<Font>("Assets/arial.ttf");
        if (arial == null) arial = Resources.GetBuiltinResource<Font>("Arial.ttf");

        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var go in allObjects)
        {
            if (go.scene.isLoaded && (go.name.Contains("Fullname") || go.name.Contains("Role") || go.name.Contains("txtName") || go.name.Contains("txtRole") || go.name.Contains("profile")))
            {
                TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    string text = tmp.text;
                    Color color = tmp.color;
                    int fontSize = (int)tmp.fontSize;
                    TextAlignmentOptions alignment = tmp.alignment;
                    
                    Object.DestroyImmediate(tmp);
                    
                    Text legacyText = go.AddComponent<Text>();
                    legacyText.text = text;
                    legacyText.color = color;
                    legacyText.fontSize = fontSize;
                    if (arial != null) legacyText.font = arial;
                    
                    if (alignment == TextAlignmentOptions.Center) legacyText.alignment = TextAnchor.MiddleCenter;
                    
                    EditorUtility.SetDirty(go);
                    changed = true;
                }
            }
        }
        
        if (changed)
        {
            EditorPrefs.SetBool("HasSwappedToLegacyText_Fix", true);
            Debug.Log("<color=green>Đã tự động chuyển TextMeshPro sang Legacy Text để gõ tiếng Việt!</color>");
        }
    }
}
