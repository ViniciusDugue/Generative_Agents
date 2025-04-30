using UnityEngine;
using UnityEngine.AI;

public class GatherBehavior : AgentBehavior
{
    public NavMeshAgent agent;
    public Vector3 target;
    public float wanderRadius = 15f;  // Radius for random wandering

    // These variables are assumed to be defined or set in the editor.
    private float m_EffectTime;
    public Material goodMaterial;
    public bool contribute = true;
    public EnvironmentSettings m_EnvironmentSettings; // Custom class holding environmental variables, e.g., foodScore
    public Vector3 rotationSpeed = new Vector3(0, 50, 0); // Rotation speed in degrees per second

    /// <summary>
    /// Called when the script instance is loaded.
    /// </summary>
    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        target = transform.position;
    }
    
    // Start is called before the first frame update.
    void OnEnable()
    {
        if (agent != null)
        {
            agent.isStopped = false;
        }
        // Choose an initial random destination.
        target = GetRandomDestination();
        agent.SetDestination(target);
    }

    void  OnDisable()
    {
        if (agent != null)
        {
            agent.isStopped = true;
        }
    }

    // Update is called once per frame.
    void Update()
    {
        // If the agent is close enough to its destination, choose a new target.
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            // Optionally, you can decide here whether to look for food using other methods
            // (e.g., finding the closest food object using a tag) or continue wandering.
            target = GetRandomDestination();
            agent.SetDestination(target);
        }
    }

    void FixedUpdate()
    {
        transform.Rotate(rotationSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Generates a random position on the NavMesh within a certain radius.
    /// </summary>
    /// <returns>A random navigable position</returns>
    Vector3 GetRandomDestination()
    {
        Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
        randomDirection += transform.position;
        NavMeshHit navHit;
        NavMesh.SamplePosition(randomDirection, out navHit, wanderRadius, NavMesh.AllAreas);
        return navHit.position;
    }

    /// <summary>
    /// Sets a new target destination to the food location.
    /// Can be called by the BehaviorManager when a food spawn is detected.
    /// </summary>
    /// <param name="foodLocation">Position of the food target</param>
    public void SetFoodTarget(Vector3 foodLocation)
    {
        // Overrides any current destination with the food target.
        if (this.gameObject.GetComponent<BehaviorManager>().canCarryMoreFood())
        {
            target = foodLocation;
            agent.SetDestination(target);
            Debug.Log("New food target set at: " + target);
        }   
    }

    /// <summary>
    /// Called when the agent collides with another object. 
    /// Triggers food pickup logic if the collided object is food.
    /// </summary>
    /// <param name="collision">Collision information</param>
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("food") && this.gameObject.GetComponent<BehaviorManager>().canCarryMoreFood())
        {
            Debug.Log("food collision");
            if (collision.gameObject.GetComponent<FoodScript>() == null)
            {
                Debug.LogError("FoodScript is null on collided object!");
            }

            EndSimMetricsUI.Instance.IncrementFoodCollected();
            
            Satiate();
            collision.gameObject.GetComponent<FoodScript>().OnEaten();
            this.gameObject.GetComponent<BehaviorManager>().updateFoodCount();
        }

    }


    /// <summary>
    /// Sets the agent state to satiated and provides visual feedback by changing material.
    /// </summary>
    void Satiate()
    {
        m_EffectTime = Time.time;
        GetComponentInChildren<Renderer>().material = goodMaterial;
    }

}
