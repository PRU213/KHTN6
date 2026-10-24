using UnityEngine;

public class SampleScenePanelManager : MonoBehaviour
{
    public GameObject physicsMap;
    public GameObject chemistryMap;
    public GameObject theoryPhysics;
    public GameObject theoryChemistry;

    void Start()
    {
        // Tắt tất cả các panel trước
        if (physicsMap != null) physicsMap.SetActive(false);
        if (chemistryMap != null) chemistryMap.SetActive(false);
        if (theoryPhysics != null) theoryPhysics.SetActive(false);
        if (theoryChemistry != null) theoryChemistry.SetActive(false);

        // Kiểm tra xem được gọi từ môn nào ở HomePage
        string target = PlayerPrefs.GetString("TargetSubject", "");

        if (target == "Physic")
        {
            if (physicsMap != null) physicsMap.SetActive(true);
        }
        else if (target == "Chemistry")
        {
            if (chemistryMap != null) chemistryMap.SetActive(true);
        }
    }

    // Gắn hàm này vào nút "Lý Thuyết" trong Physical_map_game
    public void OpenTheoryPhysics()
    {
        if (physicsMap != null) physicsMap.SetActive(false);
        if (theoryPhysics != null) theoryPhysics.SetActive(true);
    }

    // Gắn hàm này vào nút "Lý Thuyết" trong Chemistry_map_game
    public void OpenTheoryChemistry()
    {
        if (chemistryMap != null) chemistryMap.SetActive(false);
        if (theoryChemistry != null) theoryChemistry.SetActive(true);
    }

    // Gắn hàm này vào nút "Chọn Game" trong theory_physic (hoặc ở đâu bạn muốn)
    public void OpenGamesList()
    {
        // Chỗ này bạn có thể bật object chứa danh sách game (ví dụ Sheep_Game, Cave_dao)
        // Hiện tại tạm ẩn màn lý thuyết đi
        if (theoryPhysics != null) theoryPhysics.SetActive(false);
        if (theoryChemistry != null) theoryChemistry.SetActive(false);
        
        Debug.Log("Mở danh sách game!");
    }
}
