using UnityEngine;

public class MapMarker : MonoBehaviour
{
    public static GameObject Current;   // map môn học vừa được mở gần nhất

    void OnEnable()
    {
        Current = gameObject;
    }
}