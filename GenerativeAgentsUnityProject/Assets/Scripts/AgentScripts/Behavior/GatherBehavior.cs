using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using UnityEngine;
using UnityEngine.AI;

public class GatherBehavior : AgentBehavior
{
    // --- Prevent two agents from chasing the same food ---
    private static HashSet<GameObject> _reservedFood = new HashSet<GameObject>();

    [Header("Wander Settings")]
    public float wanderRadius = 15f;               // Radius for random wandering
    public LayerMask groundLayerMask;              // Assign “Default” or a dedicated Ground layer
    public int maxSampleAttempts = 10;             // How many times to retry sampling

    [Header("References")]
    public Material goodMaterial;
    public EnvironmentSettings m_EnvironmentSettings;

    private NavMeshAgent agent;
    private Vector3 curPos;
    private float m_EffectTime;
    private BehaviorManager manager;
    [HideInInspector]public bool isGathering = false;

    [Header("target")]
    [SerializeField] private Vector3 target;
    private Coroutine resetTargetCoroutine;
    private float _lastFoodTargetTime = 0f;
    private const float kFoodTargetTimeout = 0.5f;
    private GameObject currentFood = null;  // track what we’re chasing

    // Misc
    private Vector3 rotationSpeed = new Vector3(0, 100, 0); // Rotation speed in degrees per second

    protected override void Awake()
    {
        agent   = GetComponent<NavMeshAgent>();
        manager = GetComponent<BehaviorManager>();
        curPos  = transform.position;

        // Let the agent handle rotation/position 
        agent.updateRotation = true;
        agent.updatePosition = true;

        // 1) Turn off auto-braking so it never slows before goal
        // agent.autoBraking = false;

        // 2) Make the brake zone almost zero
        // agent.stoppingDistance = 2f;

        // 3) Crank up acceleration so it snaps to speed
        // agent.acceleration = 20f;

        // ** Avoidance tweaks **
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        agent.avoidancePriority = Random.Range(0, 100);
        agent.autoBraking = false;

        target = Vector3.positiveInfinity;
    }
    
    // Start is called before the first frame update.
    protected override void  OnEnable()
    {
        if (agent != null)
        {
            agent.isStopped = false;
        }
        // Choose an initial random destination.
        resetTargetCoroutine = StartCoroutine(ResetTarget());
        if(!isGathering) {
            PickNewWanderTarget();
        }
        
    }

    protected override void  OnDisable()
    {
        if (agent != null)
        {
            agent.isStopped = true;
        }
        if (resetTargetCoroutine != null)
            StopCoroutine(resetTargetCoroutine);
        
        if (currentFood != null)
            _reservedFood.Remove(currentFood);

        // If we were gathering food, release it
        currentFood = null;
    }

    void FixedUpdate()
    {
        if (!manager.canCarryMoreFood())
            transform.Rotate(rotationSpeed * Time.deltaTime);
    }

    // Update is called once per frame.
    void Update()
    {
        // When arrived or stuck, pick a new wander point
        if (!agent.pathPending &&
            (!agent.hasPath || agent.remainingDistance <= agent.stoppingDistance * 2) &&
            agent.velocity.sqrMagnitude < 0.01f && !isGathering)
        {
            // Debug.Log("Picking new wander target");
            PickNewWanderTarget();
        }

        // if not already waiting, start the timer
        if (resetTargetCoroutine == null)
            resetTargetCoroutine = StartCoroutine(ResetTarget());
        
        if (isGathering && currentFood != null)
        {           
            if (Time.time - _lastFoodTargetTime > kFoodTargetTimeout)
            {
              Debug.Log("Food target timed out (>0.5s), clearing currentFood");
              // either just null it:
              currentFood = null;
              isGathering = false;
            }
        }
    }
        /// <summary>
    /// Sets a new random destination on the NavMesh.
    /// </summary>
    void PickNewWanderTarget()
    {
        Vector3 candidate = GetRandomDestination();
        if (candidate != Vector3.positiveInfinity)
        {
            // Only set if NavMeshAgent can fully reach it
            NavMeshPath testPath = new NavMeshPath();
            if (agent.CalculatePath(candidate, testPath) &&
                testPath.status == NavMeshPathStatus.PathComplete &&
                candidate != target)
            {
                target = candidate;
                 Debug.Log($"Agent ({manager.agentID}) has set wander target set at: " + target);
                agent.SetDestination(target);
            }
        }
        // else: no new target found—keep going to previous one
    }

    /// <summary>
    /// Generates a random position on the NavMesh within a certain radius.
    /// </summary>
    /// <returns>A random navigable position</returns>
    Vector3 GetRandomDestination()
    {
        for (int i = 0; i < maxSampleAttempts; i++)
        {
            // 1) Pick a random XZ offset
            Vector2 offset2D = Random.insideUnitCircle * wanderRadius;
            Vector3 flatPos  = transform.position 
                                + new Vector3(offset2D.x, 0f, offset2D.y);

            // 2) Optional: raycast down to hit the terrain collider exactly
            RaycastHit floorHit;
            if (Physics.Raycast(flatPos + Vector3.up * 50f,
                                Vector3.down, 
                                out floorHit,
                                100f,
                                groundLayerMask))
            {
                flatPos = floorHit.point;
            }

            // 3) Snap onto NavMesh
            NavMeshHit navHit;
            if (NavMesh.SamplePosition(flatPos,
                                       out navHit,
                                       wanderRadius,
                                       NavMesh.AllAreas))
            {
                // 4) Verify the agent can actually walk there
                NavMeshPath path = new NavMeshPath();
                if (agent.CalculatePath(navHit.position, path) &&
                    path.status       == NavMeshPathStatus.PathComplete &&
                    path.corners.Length > 1)
                {
                    return navHit.position;
                }
            }
        }

        // Fallback: stay in place
        return transform.position;
    }

    /// <summary>
    /// Sets a new target destination to the food location.
    /// Can be called by the BehaviorManager when a food spawn is detected.
    /// </summary>
    /// <param name="foodLocation">Position of the food target</param>
    public void SetFoodTarget(GameObject food)
    {
        // 1) if already claimed, bail
        if (_reservedFood.Contains(food)) return;

        // 2) release old claim
        if (currentFood != null)
            _reservedFood.Remove(currentFood);

        // 3) claim this one
        _reservedFood.Add(food);

        // 4) stop wander
        isGathering = true;
        if (resetTargetCoroutine != null)
        {
            StopCoroutine(resetTargetCoroutine);
            resetTargetCoroutine = null;
        }

        // 5) check carry‐capacity
        if (!manager.canCarryMoreFood())
        {
            Debug.Log("Agent is full, cannot carry more food");
            return;
        }

        // 6) chase the new food
        currentFood = food;
        target      = food.transform.position;
        Debug.Log($"Agent ({manager.agentID}) chasing food @ {target}");
        agent.SetDestination(target);
    }

    /// <summary>
    /// Called when the agent collides with another object. 
    /// Triggers food pickup logic if the collided object is food.
    /// </summary>
    /// <param name="collision">Collision information</param>
    void OnCollisionEnter(Collision collision)
    {
        if (!manager.canCarryMoreFood())
            ClearFoodTarget();
            

        if (collision.gameObject.CompareTag("food") && manager.canCarryMoreFood())
        {
            GameObject go = collision.gameObject;
            Debug.Log("food collision");
            if (collision.gameObject.GetComponent<FoodScript>() == null)
            {
                Debug.LogError("FoodScript is null on collided object!");
            }

            EndSimMetricsUI.Instance.IncrementFoodCollected();
            
            Satiate();
            go.GetComponent<FoodScript>().OnEaten();
            manager.updateFoodCount();
            // collision.gameObject.GetComponent<FoodScript>().OnEaten();
            // this.gameObject.GetComponent<BehaviorManager>().updateFoodCount();

            // ← Release your claim
            _reservedFood.Remove(go);

            // Remove this food so you don’t chase it again
            currentFood = null;
            isGathering  = false;

            // If you can still carry more *and* there are other active food spawns, BehaviorManager
            // will raycast them and call SetFoodTarget again automatically.
            // Otherwise, we’ve got nothing left (or we’re full), so clear gathering:
        }

    }

    /// <summary>
    /// Call this when we’ve eaten the last food or canCarryMoreFood() is false.
    /// </summary>
    public void ClearFoodTarget()
    {

        // ← If you had reserved something, let it go
        if (currentFood != null)
            _reservedFood.Remove(currentFood);

        // Stop chasing
        isGathering   = false;
        currentFood   = null;
        agent.SetDestination(this.transform.position);
        // // restart wander logic
        // if (resetTargetCoroutine == null)
        //     resetTargetCoroutine = StartCoroutine(ResetTarget());
        // PickNewWanderTarget();
    }

    /// <summary>
    /// Sets the agent state to satiated and provides visual feedback by changing material.
    /// </summary>
    void Satiate()
    {
        m_EffectTime = Time.time;
        GetComponentInChildren<Renderer>().material = goodMaterial;
    }

    // Optional: draw the entire path in green for complete paths
    void OnDrawGizmos()
    {
        if (agent == null || !agent.hasPath) return;

        Gizmos.color = Color.green;
        var corners = agent.path.corners;
        for (int i = 0; i < corners.Length - 1; i++)
        {
            Gizmos.DrawLine(corners[i], corners[i + 1]);
        }
    }

    /// <summary>
    /// Every `stuckCheckInterval` seconds, if the agent
    /// hasn't moved beyond `stuckRadius` from the last origin,
    /// forces a new wander target.
    /// </summary>
    private IEnumerator ResetTarget()
    {
        float stuckRadius = 1.5f;
        while (!isGathering)
        {
            // 1) Remember where we are now
            Vector3 stuckOrigin = transform.position;

            // 2) Wait the interval
            yield return new WaitForSeconds(3);

            // 3) If still within stuckRadius, pick a new target
            if (Vector3.Distance(transform.position, stuckOrigin) <= stuckRadius)
            {
                Debug.Log("Agent is stuck, picking new wander target.");
                PickNewWanderTarget();
            }
            // loop and refresh origin automatically
        }
    }



}
