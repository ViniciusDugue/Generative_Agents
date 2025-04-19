using UnityEngine;

public class AgentController : MonoBehaviour
{
    public float moveSpeed = 5f;  // Adjust speed in Inspector
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Ensure Rigidbody exists and is set properly for movement
        if (rb == null)
        {
            Debug.LogError("Rigidbody component missing from the agent!");
        }
    }

    void Update()
    {
        MoveAgent();
    }

    void MoveAgent()
    {
        float moveX = Input.GetAxis("Horizontal"); // A/D or Left/Right Arrow
        float moveZ = Input.GetAxis("Vertical");   // W/S or Up/Down Arrow

        Vector3 moveDirection = new Vector3(moveX, 0f, moveZ) * moveSpeed * Time.deltaTime;
        transform.Translate(moveDirection, Space.World);
    }
}
