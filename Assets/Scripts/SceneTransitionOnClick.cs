using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionOnClick : MonoBehaviour
{
    public string nextSceneName = "bean_1";

    void Update()
    {
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
