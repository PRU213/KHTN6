using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KHTN6.Teacher
{
    public enum TeacherScreen { Home, Quiz, Essay, Progress }

    /// <summary>References to the editable, scene-owned Canvas controls.</summary>
    public sealed class TeacherCanvasView : MonoBehaviour
    {
        public TeacherScreen screen;
        public TMP_FontAsset regularFont;
        public TMP_FontAsset boldFont;
        public Sprite rowNormal, rowSelected, iconPerson, progressTrack, progressFill, tagGreen, tagAmber;
        public RectTransform studentContent, progressContent;
        public RawImage imagePreview;

        public TMP_InputField Input(string controlName) => Find<TMP_InputField>(controlName);
        public TMP_Dropdown Dropdown(string controlName) => Find<TMP_Dropdown>(controlName);
        public Button Button(string controlName) => Find<Button>(controlName);
        public TMP_Text Label(string controlName) => Find<TMP_Text>(controlName);

        T Find<T>(string controlName) where T : Component
        {
            foreach (var component in GetComponentsInChildren<T>(true))
                if (component.name == controlName) return component;
            return null;
        }
    }
}
