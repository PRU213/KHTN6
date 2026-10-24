using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class VideoEndTransition : MonoBehaviour
{
    public string nextSceneName = "bean_tong";
    private VideoPlayer vp;

    void Start()
    {
        vp = GetComponent<VideoPlayer>();
        if (vp == null) vp = FindAnyObjectByType<VideoPlayer>();
        
        if (vp != null)
        {
            vp.loopPointReached += OnVideoEnd;
        }
    }

    void OnVideoEnd(VideoPlayer player)
    {
        SceneManager.LoadScene(nextSceneName);
    }
}
