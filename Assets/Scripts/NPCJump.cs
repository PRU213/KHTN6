using UnityEngine;

public class NPCJump : MonoBehaviour
{
    public float jumpHeight = 0.3f;
    public float jumpSpeed = 3f;

    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        float jump = Mathf.Abs(Mathf.Sin(Time.time * jumpSpeed)) * jumpHeight;

        transform.position = new Vector3(
            startPosition.x,
            startPosition.y + jump,
            startPosition.z
        );
    }
}