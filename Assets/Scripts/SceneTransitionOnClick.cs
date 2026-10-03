using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionOnClick : MonoBehaviour
{
    public string nextSceneName = "bean_1";

    void Update()
    {
        if (Input.GetMouseButtonDown(0) || Input.anyKeyDown)
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
