using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class GoToSampleScenePanel : MonoBehaviour
{
    public string subjectName = "Physic"; // "Physic" hoặc "Chemistry"

    void Start()
    {
        Button btn = GetComponent<Button>();
        btn.onClick.AddListener(() => {
            PlayerPrefs.SetString("TargetSubject", subjectName);
            PlayerPrefs.Save();
            SceneManager.LoadScene("SampleScene");
        });
    }
}
