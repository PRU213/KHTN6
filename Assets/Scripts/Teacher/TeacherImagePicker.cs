using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace KHTN6.Teacher
{
    /// <summary>A native image chooser with no additional Unity package dependency.</summary>
    public static class TeacherImagePicker
    {
        public static string Pick()
        {
#if UNITY_EDITOR
            return UnityEditor.EditorUtility.OpenFilePanelWithFilters("Chọn ảnh minh họa", "", new[] { "Ảnh PNG, JPG", "png,jpg,jpeg" });
#elif UNITY_STANDALONE_WIN
            var dialog = new OpenFileName();
            dialog.structSize = Marshal.SizeOf(typeof(OpenFileName));
            dialog.filter = "Ảnh PNG, JPG\0*.png;*.jpg;*.jpeg\0\0";
            dialog.file = new StringBuilder(4096);
            dialog.maxFile = dialog.file.Capacity;
            dialog.title = "Chọn ảnh minh họa";
            dialog.flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000008;
            return GetOpenFileName(dialog) ? dialog.file.ToString() : "";
#else
            throw new NotSupportedException("Chọn ảnh hiện hỗ trợ Unity Editor và bản Windows.");
#endif
        }

        public static string Read(string path, out Texture2D texture)
        {
            texture = null;
            var file = new FileInfo(path);
            if (!file.Exists) throw new IOException("Không tìm thấy ảnh đã chọn.");
            if (file.Length > 4 * 1024 * 1024) throw new IOException("Vui lòng chọn ảnh nhỏ hơn 4 MB.");
            byte[] bytes = File.ReadAllBytes(path);
            var loaded = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!loaded.LoadImage(bytes))
            {
                UnityEngine.Object.Destroy(loaded);
                throw new IOException("Không đọc được ảnh. Vui lòng chọn tệp PNG hoặc JPG.");
            }
            if (loaded.width > 8192 || loaded.height > 8192)
            {
                UnityEngine.Object.Destroy(loaded);
                throw new IOException("Kích thước ảnh tối đa là 8192 × 8192 pixel.");
            }
            texture = loaded;
            string mime = Path.GetExtension(path).Equals(".png", StringComparison.OrdinalIgnoreCase) ? "image/png" : "image/jpeg";
            return "data:" + mime + ";base64," + Convert.ToBase64String(bytes);
        }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private sealed class OpenFileName
        {
            public int structSize;
            public IntPtr owner, instance;
            public string filter, customFilter;
            public int maxCustomFilter, filterIndex;
            public StringBuilder file;
            public int maxFile;
            public string fileTitle;
            public int maxFileTitle;
            public string initialDirectory, title;
            public int flags;
            public short fileOffset, fileExtension;
            public string defaultExtension;
            public IntPtr customData, hook;
            public string templateName;
            public IntPtr reserved;
            public int reserved2, flagsEx;
        }

        [DllImport("comdlg32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetOpenFileName([In, Out] OpenFileName dialog);
#endif
    }
}
