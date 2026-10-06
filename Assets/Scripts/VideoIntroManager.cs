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

    // Đã xóa hàm Update chứa Input.anyKeyDown để bắt buộc xem hết video

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
