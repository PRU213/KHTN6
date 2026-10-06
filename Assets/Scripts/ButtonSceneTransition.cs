using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonSceneTransition : MonoBehaviour
{
    public string targetScene = "luatchoisinhhoc";

    void Start()
    {
        Button btn = GetComponent<Button>();
        btn.onClick.AddListener(() => {
            SceneManager.LoadScene(targetScene);
        });
    }
}
