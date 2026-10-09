using UnityEngine;

public class TheoryBackController : MonoBehaviour
{
    [Header("Theory Screens")]
    public GameObject theoryPhysics;
    public GameObject theoryChemistry;

    [Header("Map Screens")]
    public GameObject physicalMapGame;
    public GameObject chemistryMapGame;

    // Quay từ lý thuyết Vật lý về map Vật lý
    public void BackToPhysicsMap()
    {
        if (theoryPhysics != null)
            theoryPhysics.SetActive(false);

        if (theoryChemistry != null)
            theoryChemistry.SetActive(false);

        if (chemistryMapGame != null)
            chemistryMapGame.SetActive(false);

        if (physicalMapGame != null)
            physicalMapGame.SetActive(true);
    }

    // Quay từ lý thuyết Hóa học về map Hóa học
    public void BackToChemistryMap()
    {
        if (theoryPhysics != null)
            theoryPhysics.SetActive(false);

        if (theoryChemistry != null)
            theoryChemistry.SetActive(false);

        if (physicalMapGame != null)
            physicalMapGame.SetActive(false);

        if (chemistryMapGame != null)
            chemistryMapGame.SetActive(true);
    }
}
