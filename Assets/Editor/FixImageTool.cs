using UnityEngine;
using UnityEditor;

public class FixImageTool
{
    [MenuItem("Tools/2. Làm Nét Hình Ảnh (Sửa lỗi mờ)")]
    public static void FixBlurryImages()
    {
        // Tìm tất cả ảnh trong thư mục Assets
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" });
        int fixedCount = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer != null)
            {
                bool needReimport = false;

                // Tắt bộ lọc làm mờ (Bilinear -> Point)
                if (importer.filterMode != FilterMode.Point)
                {
                    importer.filterMode = FilterMode.Point;
                    needReimport = true;
                }

                // Tắt nén ảnh (làm mất chất lượng)
                if (importer.textureCompression != TextureImporterCompression.Uncompressed)
                {
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    needReimport = true;
                }

                if (needReimport)
                {
                    importer.SaveAndReimport();
                    fixedCount++;
                }
            }
        }

        EditorUtility.DisplayDialog("Thành công!", 
            $"Đã tự động sửa độ nét cho {fixedCount} hình ảnh trong game.\n\n" +
            "Tất cả ảnh bây giờ sẽ sắc nét (No Filter, Uncompressed).", "OK");
    }
}
