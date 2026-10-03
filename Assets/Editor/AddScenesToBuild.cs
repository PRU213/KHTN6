using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class AddScenesToBuild : EditorWindow
{
    [MenuItem("Tools/Fix Build Settings (Thêm Scene)")]
    public static void FixBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();
        
        // Cố gắng giữ lại các scene cũ hợp lệ
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (scene.path != "Assets/Scenes/HomePage.unity") // Sẽ add lại sau
                scenes.Add(scene);
        }

        // Đảm bảo có các scene quan trọng
        AddSceneIfMissing(scenes, "Assets/Scenes/HomePage.unity");
        AddSceneIfMissing(scenes, "Assets/Scenes/Biology/ChonBai.unity");
        AddSceneIfMissing(scenes, "Assets/Scenes/Biology/victory.unity");
        AddSceneIfMissing(scenes, "Assets/Scenes/Biology/bean_1.unity");
        
        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log("Đã tự động thêm các Scene vào Build Settings thành công!");
    }

    private static void AddSceneIfMissing(List<EditorBuildSettingsScene> scenes, string path)
    {
        foreach (var s in scenes)
        {
            if (s.path == path) return;
        }
        scenes.Add(new EditorBuildSettingsScene(path, true));
    }
}
