using UnityEngine;

public class SwitchScreen : MonoBehaviour
{
    public GameObject fromScreen; // màn hiện tại (vd: theory_chemistry)
    public GameObject toScreen;   // màn muốn quay về (vd: Chemistry_map_game)

    public void Go()
    {
        fromScreen.SetActive(false);
        toScreen.SetActive(true);
    }
}