using UnityEngine;

public class PlayerHeatTracker : MonoBehaviour
{
    [Header("References")]
    public InfluenceMap influenceMap;

    [Header("Heat Generation")]
    [Tooltip("How much heat the player generates per second")]
    public float heatPerSecond = 2f;

    [Tooltip("Radius of heat generation around player")]
    public float heatRadius = 2f;

    [Tooltip("How often to add heat (seconds)")]
    public float heatUpdateInterval = 0.2f;

    [Header("Movement-Based Heat")]
    [Tooltip("Add extra heat when moving")]
    public bool addMovementHeat = true;

    [Tooltip("Speed threshold to count as 'moving'")]
    public float movementThreshold = 0.1f;

    [Tooltip("Extra heat multiplier when moving")]
    public float movementHeatMultiplier = 1.5f;

    [Header("Action-Based Heat")]
    [Tooltip("Heat added when player shoots/attacks")]
    public float actionHeatAmount = 5f;

    [Tooltip("Radius for action heat")]
    public float actionHeatRadius = 3f;

    private float heatTimer = 0f;
    private Vector3 lastPosition;

    void Start()
    {
        lastPosition = transform.position;

        if (influenceMap == null)
        {
            influenceMap = FindObjectOfType<InfluenceMap>();
        }

        if (influenceMap == null)
        {
            Debug.LogError("PlayerHeatTracker couldn't find InfluenceMap!");
        }
    }

    void Update()
    {
        if (influenceMap == null) return;

        heatTimer += Time.deltaTime;

        if (heatTimer >= heatUpdateInterval)
        {
            AddPassiveHeat();
            heatTimer = 0f;
        }
    }

    void AddPassiveHeat()
    {
        float heatAmount = heatPerSecond * heatUpdateInterval;

        // Check if player is moving
        if (addMovementHeat)
        {
            float distanceMoved = Vector3.Distance(transform.position, lastPosition);
            float speed = distanceMoved / heatUpdateInterval;

            if (speed > movementThreshold)
            {
                heatAmount *= movementHeatMultiplier;
            }
        }

        lastPosition = transform.position;

        // Add heat to the map
        influenceMap.AddHeat(transform.position, heatAmount, heatRadius);
    }

    // Call this when player performs actions (shooting, jumping, etc.)
    public void AddActionHeat()
    {
        if (influenceMap != null)
        {
            influenceMap.AddHeat(transform.position, actionHeatAmount, actionHeatRadius);
        }
    }

    // Call this for specific locations (dropped items, opened doors, etc.)
    public void AddHeatAtLocation(Vector3 location, float amount, float radius)
    {
        if (influenceMap != null)
        {
            influenceMap.AddHeat(location, amount, radius);
        }
    }
}