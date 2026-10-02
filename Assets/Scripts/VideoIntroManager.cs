using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(VideoPlayer))]
public class VideoIntroManager : MonoBehaviour
{
    public string nextSceneName = "bean_tong"; // Chuyển sang màn bean_tong sau khi hết video

    private VideoPlayer videoPlayer;

    void Start()
    {
        videoPlayer = GetComponent<VideoPlayer>();

        // Lắng nghe sự kiện khi video kết thúc
        videoPlayer.loopPointReached += OnVideoEnd;
    }

    void Update()
    {
        // Nhấn nút bất kỳ để bỏ qua video
        if (Input.anyKeyDown)
        {
            LoadNextScene();
        }
    }

    private void OnVideoEnd(VideoPlayer vp)
    {
        LoadNextScene();
    }

    private void LoadNextScene()
    {
        // Gỡ sự kiện để tránh gọi nhiều lần
        videoPlayer.loopPointReached -= OnVideoEnd;
        SceneManager.LoadScene(nextSceneName);
    }
}
