using UnityEngine;

public class RememberScreen : MonoBehaviour
{
    public static GameObject previous;

    public GameObject thisScreen; // màn lý thuyết chứa nút này

    public void Remember()
    {
        previous = thisScreen;
    }
}