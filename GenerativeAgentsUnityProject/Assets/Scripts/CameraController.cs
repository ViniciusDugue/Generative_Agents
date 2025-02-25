using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField]
    private float moveSpeed = 20f;        // Increased speed for faster movement
    public float rotateSpeed = 100f;       // Speed of camera rotation

    [Header("Zoom Settings")]
    public float zoomSpeed = 50f;          // Speed of zooming
    public float minZoom = 5f;             // Minimum zoom distance
    public float maxZoom = 50f;            // Maximum zoom distance

    [Header("Lock-On Settings")]
    public KeyCode lockOnKey = KeyCode.Space;
    public KeyCode nextAgentKey = KeyCode.Tab;
    public KeyCode prevAgentKey = KeyCode.LeftShift;

    private bool isLockedOn = false;
    private Transform lockedAgent;
    private GameObject[] agents;
    private int currentAgentIndex = -1;

    [Header("Rotation Settings")]
    public float smoothSpeed = 5f;          // For smooth transitions

    void Start()
    {
        // Corrected tag from "Agent" to "agent"
        agents = GameObject.FindGameObjectsWithTag("agent");
    }

    void Update()
    {
        HandleMovement();
        HandleZoom();
        HandleRotation();
        HandleLockOn();
        HandleAgentSelection();
    }

    void HandleMovement()
    {
        if (isLockedOn && lockedAgent != null)
        {
            // Optionally, restrict movement when locked on
            // For example, orbit around the agent
            // This example allows free movement
        }

        // Get input from WASD or Arrow Keys via Input Axes
        float h = Input.GetAxis("Horizontal"); // A, D keys or Left, Right Arrow Keys
        float v = Input.GetAxis("Vertical");   // W, S keys or Up, Down Arrow Keys

        Vector3 move = transform.right * h + transform.forward * v;
        transform.position += move.normalized * moveSpeed * Time.deltaTime;
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0.0f)
        {
            Vector3 zoom = transform.forward * scroll * zoomSpeed;
            transform.position += zoom * Time.deltaTime;
        }

        // Clamp the distance if locked on
        if (isLockedOn && lockedAgent != null)
        {
            float distance = Vector3.Distance(transform.position, lockedAgent.position);
            if (distance < minZoom)
            {
                transform.position = lockedAgent.position + (transform.position - lockedAgent.position).normalized * minZoom;
            }
            else if (distance > maxZoom)
            {
                transform.position = lockedAgent.position + (transform.position - lockedAgent.position).normalized * maxZoom;
            }
        }
    }

    void HandleRotation()
    {
        if (Input.GetMouseButton(1)) // Right mouse button held down
        {
            float mouseX = Input.GetAxis("Mouse X") * rotateSpeed * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * rotateSpeed * Time.deltaTime;

            transform.Rotate(Vector3.up, mouseX, Space.World);
            transform.Rotate(Vector3.right, -mouseY, Space.Self);
        }
    }

    void HandleLockOn()
    {
        if (Input.GetKeyDown(lockOnKey))
        {
            if (!isLockedOn)
            {
                LockOnToNearestAgent();
            }
            else
            {
                UnlockFromAgent();
            }
        }

        if (isLockedOn && lockedAgent != null)
        {
            // Smoothly follow the agent
            Vector3 desiredPosition = lockedAgent.position + (transform.position - lockedAgent.position).normalized * Vector3.Distance(transform.position, lockedAgent.position);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * smoothSpeed);

            // Smoothly rotate to look at the agent
            Quaternion desiredRotation = Quaternion.LookRotation(lockedAgent.position - transform.position);
            transform.rotation = Quaternion.Lerp(transform.rotation, desiredRotation, Time.deltaTime * smoothSpeed);
        }
    }

    void HandleAgentSelection()
    {
        if (Input.GetKeyDown(nextAgentKey))
        {
            if (agents.Length == 0)
                return;

            currentAgentIndex = (currentAgentIndex + 1) % agents.Length;
            LockOnToAgent(agents[currentAgentIndex].transform);
        }

        if (Input.GetKeyDown(prevAgentKey))
        {
            if (agents.Length == 0)
                return;

            currentAgentIndex = (currentAgentIndex - 1 + agents.Length) % agents.Length;
            LockOnToAgent(agents[currentAgentIndex].transform);
        }
    }

    void LockOnToNearestAgent()
    {
        if (agents.Length == 0)
            return;

        GameObject nearest = null;
        float minDist = Mathf.Infinity;
        Vector3 currentPos = transform.position;

        foreach (GameObject agent in agents)
        {
            float dist = Vector3.Distance(agent.transform.position, currentPos);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = agent;
            }
        }

        if (nearest != null)
        {
            lockedAgent = nearest.transform;
            isLockedOn = true;
        }
    }

    void LockOnToAgent(Transform agent)
    {
        lockedAgent = agent;
        isLockedOn = true;
    }

    void UnlockFromAgent()
    {
        isLockedOn = false;
        lockedAgent = null;
    }
}
