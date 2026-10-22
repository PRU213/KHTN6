using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

[InitializeOnLoad]
public static class FixSelectableBug
{
    static FixSelectableBug()
    {
        AssemblyReloadEvents.afterAssemblyReload += ResetSelectableArray;
        EditorApplication.delayCall += ResetSelectableArray;
        EditorApplication.playModeStateChanged += (state) => ResetSelectableArray();
        ResetSelectableArray();
    }

    [MenuItem("Tools/Reset UI Selectables (Fix IndexOutOfRange)")]
    public static void ResetSelectableArray()
    {
        try
        {
            var countField = typeof(Selectable).GetField("s_SelectableCount", BindingFlags.Static | BindingFlags.NonPublic);
            var arrayField = typeof(Selectable).GetField("s_Selectables", BindingFlags.Static | BindingFlags.NonPublic);

            if (countField != null && arrayField != null)
            {
                int count = (int)countField.GetValue(null);
                var array = (Selectable[])arrayField.GetValue(null);

                if (array == null || count < 0 || count >= array.Length)
                {
                    countField.SetValue(null, 0);
                    arrayField.SetValue(null, new Selectable[100]);
                    Debug.Log("[FixSelectableBug] Đã khôi phục mảng s_Selectables để khắc phục lỗi IndexOutOfRangeException.");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[FixSelectableBug] " + e.Message);
        }
    }
}
