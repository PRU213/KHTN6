using UnityEngine;
using UnityEngine.SceneManagement;

public class VictoryChecker : MonoBehaviour
{
    private bool isLoadingVictory = false;

    void Update()
    {
        if (isLoadingVictory) return;

        string currentScene = SceneManager.GetActiveScene().name;
        
        // Chỉ kiểm tra ở màn bean_tong
        if (currentScene.Contains("bean_tong") || currentScene.Contains("beantong"))
        {
            // Không tính màn câu hỏi
            if (currentScene.Contains("question")) return;

#pragma warning disable CS0618
            Enemy[] activeEnemies = FindObjectsOfType<Enemy>();
#pragma warning restore CS0618
            
            // Nếu không còn quái vật nào -> Chuyển sang màn victory
            if (activeEnemies.Length == 0)
            {
                isLoadingVictory = true;
                Invoke(nameof(LoadVictory), 1f); // Đợi 1 giây rồi chuyển
            }
        }
    }

    private void LoadVictory()
    {
        SceneManager.LoadScene("victory");
    }
}
