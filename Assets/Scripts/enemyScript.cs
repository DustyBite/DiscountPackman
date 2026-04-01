using UnityEngine;
using System.Collections.Generic;

public class EnemyAI : MonoBehaviour
{
    [Header("Pathfinding")]
	public bool usePathfinding = true;
	public Pathfinding pathfinding;
	public float pathUpdateInterval = 0.5f;
	public float waypointReachDistance = 0.5f;
	public bool showPath = true;

	private List<Vector3> currentPath = new List<Vector3>();
	private int currentWaypointIndex = 0;
	private float pathUpdateTimer = 0f;
	private Vector3 lastPathTarget = Vector3.zero;
	
	[Header("Personality")]
    public float scary = 5f;
    public float bravey = 7f;

    [Header("References")]
    public Transform enemyMesh;
    public InfluenceMap influenceMap;
    public CharacterController controller; // Add this

    [Header("Movement")]
    public float moveSpeed = 3f;
    public float rotationSpeed = 10f;
    public float stopDistance = 1.5f;

    [Header("Behavior")]
    public float chaseRange = 8f;
    public float wanderRadius = 5f;
    public float wanderTime = 2f;

    [Header("Influence Tuning")]
    [Tooltip("How strongly the enemy avoids influence")]
    public float influenceWeight = 0.5f;

    [Tooltip("How strongly the enemy follows its goal (seek/flee)")]
    public float goalWeight = 4f;

    [Tooltip("How far ahead we sample directions")]
    public float sampleDistance = 2.5f;

    [Tooltip("How smooth direction changes are")]
    public float smoothing = 5f;

    [Header("Chase Mode")]
    [Tooltip("Number of direction samples while wandering")]
    public int wanderSamples = 8;

    [Tooltip("Number of direction samples while chasing (narrower focus)")]
    public int chaseSamples = 5;

    [Tooltip("Angle spread for samples in chase mode (degrees)")]
    public float chaseAngleSpread = 60f;

    [Tooltip("Angle spread for samples in wander mode (degrees)")]
    public float wanderAngleSpread = 180f;

    [Header("Collision Detection")]
    [Tooltip("Distance to check for obstacles ahead")]
    public float obstacleCheckDistance = 2f;

    [Tooltip("Number of rays to cast for wall detection")]
    public int obstacleRayCount = 5;

    [Tooltip("Angle spread for obstacle detection rays")]
    public float obstacleRaySpread = 60f;

    [Tooltip("Layers to treat as obstacles")]
    public LayerMask obstacleLayer;

    [Tooltip("How much to slide along walls")]
    public float wallSlideAmount = 0.7f;
	
	[Header("Heat Map Investigation")]
	[Tooltip("Enable heat map investigation when no target")]
	public bool useHeatMapInvestigation = true;

	[Tooltip("Minimum heat value to be interesting")]
	public float heatInterestThreshold = 2f;

	[Tooltip("How far to search for heat")]
	public float heatSearchRadius = 10f;

	[Tooltip("How strongly attracted to heat")]
	public float heatAttractionWeight = 2f;

	private Vector3 investigationTarget;
	private bool isInvestigating = false;

    [Header("Debug")]
    public bool showDebugRays = true;
    public bool showObstacleRays = true;

    // State
    private Vector3 wanderTarget;
    private float wanderTimer = 0f;
    private Vector3 currentMoveDirection = Vector3.forward;
    private bool isChasing = false;
    private GameObject currentTarget;

    void Start()
    {
        // Auto-assign CharacterController if not set
        if (controller == null)
            controller = GetComponent<CharacterController>();

        if (controller == null)
        {
            Debug.LogError($"{gameObject.name} needs a CharacterController component!");
        }
    }

    void Update()
	{
		Vector3 desiredDirection = Vector3.zero;
		bool hasTarget = false;

		GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
		GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

		List<GameObject> targets = new List<GameObject>();
		if (enemies != null) targets.AddRange(enemies);
		if (playerObj != null) targets.Add(playerObj);

		// Find closest valid target
		GameObject closestTarget = null;
		float closestDistance = Mathf.Infinity;

		foreach (GameObject t in targets)
		{
			if (t == this.gameObject) continue;

			float distance = Vector3.Distance(transform.position, t.transform.position);
			
			if (distance < chaseRange && distance < closestDistance)
			{
				closestTarget = t;
				closestDistance = distance;
			}
		}

		// Update chase state
		isChasing = (closestTarget != null);
		currentTarget = closestTarget;

		Vector3 targetPosition = Vector3.zero;

		// Process the closest target
		if (closestTarget != null)
		{
			EnemyAI targetAI = closestTarget.GetComponent<EnemyAI>();
			float targetScary = targetAI != null ? targetAI.scary : 4f;

			targetPosition = closestTarget.transform.position;
			hasTarget = true;
		}
		// No target in range - investigate heat or wander
		else if (useHeatMapInvestigation && influenceMap != null)
		{
			Vector3 heatTarget = FindHottestSpot();
			
			if (heatTarget != Vector3.zero)
			{
				targetPosition = heatTarget;
				isInvestigating = true;
				investigationTarget = heatTarget;
				hasTarget = true;
			}
			else
			{
				isInvestigating = false;
			}
		}
		else
		{
			isInvestigating = false;
		}

		// PATHFINDING MODE
		if (usePathfinding && pathfinding != null && hasTarget)
		{
			pathUpdateTimer += Time.deltaTime;

			// Recalculate path periodically or if target moved significantly
			bool needsNewPath = currentPath.Count == 0 || 
							   pathUpdateTimer >= pathUpdateInterval ||
							   Vector3.Distance(targetPosition, lastPathTarget) > 2f;

			if (needsNewPath)
			{
				currentPath = pathfinding.FindPath(transform.position, targetPosition);
				currentWaypointIndex = 0;
				pathUpdateTimer = 0f;
				lastPathTarget = targetPosition;
			}

			// Follow path
			if (currentPath.Count > 0)
			{
				Vector3 currentWaypoint = currentPath[currentWaypointIndex];
				Vector3 dirToWaypoint = currentWaypoint - transform.position;
				dirToWaypoint.y = 0f;

				// Reached waypoint
				if (dirToWaypoint.magnitude < waypointReachDistance)
				{
					currentWaypointIndex++;
					
					// Reached end of path
					if (currentWaypointIndex >= currentPath.Count)
					{
						currentPath.Clear();
						currentWaypointIndex = 0;
					}
				}

				desiredDirection = dirToWaypoint.normalized;
			}
			else
			{
				// No path - use direct approach
				desiredDirection = (targetPosition - transform.position).normalized;
			}

			// Use influence map to fine-tune direction
			Vector3 moveDirection = ChooseBestDirection(desiredDirection);
			moveDirection = AdvancedObstacleAvoidance(moveDirection);

			// Smooth movement
			if (moveDirection.magnitude > 0.01f)
			{
				currentMoveDirection = Vector3.Lerp(
					currentMoveDirection,
					moveDirection,
					smoothing * Time.deltaTime
				).normalized;
			}
		}
		// NON-PATHFINDING MODE (Original behavior)
		else
		{
			if (hasTarget)
			{
				desiredDirection = (targetPosition - transform.position).normalized;
			}
			else
			{
				desiredDirection = Wander();
			}

			Vector3 moveDirection = ChooseBestDirection(desiredDirection);
			moveDirection = AdvancedObstacleAvoidance(moveDirection);

			if (moveDirection.magnitude > 0.01f)
			{
				currentMoveDirection = Vector3.Lerp(
					currentMoveDirection,
					moveDirection,
					smoothing * Time.deltaTime
				).normalized;
			}
		}

		// MOVE WITH CHARACTER CONTROLLER
		if (controller != null)
		{
			Vector3 moveVelocity = currentMoveDirection * moveSpeed * Time.deltaTime;
			moveVelocity.y = -2f * Time.deltaTime;
			
			CollisionFlags collisionFlags = controller.Move(moveVelocity);

			if ((collisionFlags & CollisionFlags.Sides) != 0)
			{
				Vector3 slideDir = Vector3.Cross(Vector3.up, currentMoveDirection);
				slideDir = Vector3.Cross(slideDir, Vector3.up).normalized;
				
				Vector3 slideVelocity = slideDir * moveSpeed * wallSlideAmount * Time.deltaTime;
				slideVelocity.y = -2f * Time.deltaTime;
				
				controller.Move(slideVelocity);
			}
		}
		else
		{
			transform.position += currentMoveDirection * moveSpeed * Time.deltaTime;
		}

		// Rotate mesh
		if (currentMoveDirection.magnitude > 0.01f && enemyMesh != null)
			RotateMesh(currentMoveDirection);
	}

    // =========================
    // BEHAVIORS
    // =========================

    Vector3 Seek(Vector3 targetPos)
    {
        Vector3 dir = targetPos - transform.position;
        dir.y = 0f;
        
        if (dir.magnitude < stopDistance) 
            return Vector3.zero;
            
        return dir.normalized;
    }

    Vector3 Flee(Vector3 threatPos)
    {
        Vector3 dir = transform.position - threatPos;
        dir.y = 0f;
        return dir.normalized;
    }

    Vector3 Wander()
    {
        wanderTimer -= Time.deltaTime;

        if (wanderTimer <= 0f)
        {
            Vector2 circle = Random.insideUnitCircle * wanderRadius;
            wanderTarget = transform.position + new Vector3(circle.x, 0f, circle.y);
            wanderTimer = wanderTime;
        }

        Vector3 dir = wanderTarget - transform.position;
        dir.y = 0f;

        if (dir.magnitude < 0.5f)
        {
            Vector2 circle = Random.insideUnitCircle * wanderRadius;
            wanderTarget = transform.position + new Vector3(circle.x, 0f, circle.y);
            wanderTimer = wanderTime;
            dir = wanderTarget - transform.position;
            dir.y = 0f;
        }

        return dir.magnitude > 0.01f ? dir.normalized : currentMoveDirection;
    }


	Vector3 FindHottestSpot()
	{
		float maxHeat = heatInterestThreshold;
		Vector3 hottestPosition = Vector3.zero;

		// Sample positions in a grid around the enemy
		int samples = 8;
		for (int i = 0; i < samples; i++)
		{
			float angle = i * (360f / samples);
			Vector3 dir = Quaternion.Euler(0, angle, 0) * Vector3.forward;
			Vector3 samplePos = transform.position + dir * heatSearchRadius;

			float heat = influenceMap.GetHeat(samplePos);

			if (heat > maxHeat)
			{
				maxHeat = heat;
				hottestPosition = samplePos;
			}
		}
	
		// Also check some random nearby spots
		for (int i = 0; i < 5; i++)
		{
			Vector2 randomOffset = Random.insideUnitCircle * heatSearchRadius;
			Vector3 samplePos = transform.position + new Vector3(randomOffset.x, 0, randomOffset.y);
	
			float heat = influenceMap.GetHeat(samplePos);
	
			if (heat > maxHeat)
			{
				maxHeat = heat;
				hottestPosition = samplePos;
			}
		}

		return hottestPosition;
	}

    // =========================
    // INFLUENCE + DECISION
    // =========================

    Vector3 ChooseBestDirection(Vector3 desiredDir)
    {
        if (desiredDir.magnitude < 0.01f)
            return currentMoveDirection;

        if (influenceMap == null)
        {
            Debug.LogWarning("No influence map assigned to " + gameObject.name);
            return desiredDir;
        }

        // Adaptive sampling based on chase state
        int sampleCount = isChasing ? chaseSamples : wanderSamples;
        float angleSpread = isChasing ? chaseAngleSpread : wanderAngleSpread;

        Vector3 bestDir = desiredDir;
        float bestScore = -Mathf.Infinity;

        // Sample directions AROUND the desired direction
        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)(sampleCount - 1);
            float angleOffset = (t * 2f - 1f) * (angleSpread * 0.5f);
            
            // Rotate desired direction by offset
            Vector3 dir = Quaternion.Euler(0, angleOffset, 0) * desiredDir;

            Vector3 samplePos = transform.position + dir * sampleDistance;
            float influence = influenceMap.GetInfluence(samplePos);

            // Score this direction
            float score = 0f;
            
            // Penalize high influence (danger)
            score -= influence * influenceWeight;
            
            // Reward alignment with desired direction
            float alignment = Vector3.Dot(dir.normalized, desiredDir);
            score += alignment * goalWeight;

            // Debug visualization
            if (showDebugRays)
            {
                Color rayColor = Color.Lerp(Color.red, Color.green, (score + 5f) / 10f);
                Debug.DrawRay(transform.position, dir * sampleDistance, rayColor);
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestDir = dir;
            }
        }

        return bestDir.normalized;
    }

    // =========================
    // ADVANCED OBSTACLE AVOIDANCE
    // =========================

    Vector3 AdvancedObstacleAvoidance(Vector3 moveDir)
    {
        if (moveDir.magnitude < 0.01f) 
            return moveDir;

        Vector3 rayStart = transform.position + Vector3.up * 0.5f;
        
        // Multi-ray obstacle detection
        List<Vector3> clearDirections = new List<Vector3>();
        List<float> directionScores = new List<float>();

        for (int i = 0; i < obstacleRayCount; i++)
        {
            float t = i / (float)(obstacleRayCount - 1);
            float angleOffset = (t * 2f - 1f) * (obstacleRaySpread * 0.5f);
            
            Vector3 testDir = Quaternion.Euler(0, angleOffset, 0) * moveDir;
            RaycastHit hit;

            bool hitObstacle = Physics.Raycast(
                rayStart, 
                testDir, 
                out hit, 
                obstacleCheckDistance,
                obstacleLayer
            );

            // Debug visualization
            if (showObstacleRays)
            {
                Color rayColor = hitObstacle ? Color.red : Color.cyan;
                Debug.DrawRay(rayStart, testDir * obstacleCheckDistance, rayColor);
            }

            if (!hitObstacle)
            {
                clearDirections.Add(testDir);
                
                // Score based on alignment with desired direction
                float alignment = Vector3.Dot(testDir.normalized, moveDir.normalized);
                directionScores.Add(alignment);
            }
            else
            {
                // Check if it's an entity we should ignore
                if (hit.collider.CompareTag("Enemy") || hit.collider.CompareTag("Player"))
                {
                    clearDirections.Add(testDir);
                    float alignment = Vector3.Dot(testDir.normalized, moveDir.normalized);
                    directionScores.Add(alignment);
                }
            }
        }

        // If we have clear paths, pick the best one
        if (clearDirections.Count > 0)
        {
            int bestIndex = 0;
            float bestScore = directionScores[0];

            for (int i = 1; i < directionScores.Count; i++)
            {
                if (directionScores[i] > bestScore)
                {
                    bestScore = directionScores[i];
                    bestIndex = i;
                }
            }

            return clearDirections[bestIndex].normalized;
        }

        // All paths blocked - try to escape
        return FindEscapeDirection(rayStart);
    }

    Vector3 FindEscapeDirection(Vector3 rayStart)
    {
        // Try 8 cardinal/diagonal directions to find escape route
        Vector3[] escapeDirections = new Vector3[]
        {
            Vector3.forward,
            Vector3.back,
            Vector3.left,
            Vector3.right,
            (Vector3.forward + Vector3.left).normalized,
            (Vector3.forward + Vector3.right).normalized,
            (Vector3.back + Vector3.left).normalized,
            (Vector3.back + Vector3.right).normalized
        };

        foreach (Vector3 dir in escapeDirections)
        {
            if (!Physics.Raycast(rayStart, dir, obstacleCheckDistance, obstacleLayer))
            {
                return dir;
            }
        }

        // Completely stuck - reverse direction
        return -currentMoveDirection;
    }

    // =========================
    // ROTATION
    // =========================

    void RotateMesh(Vector3 moveDirection)
    {
        Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
        enemyMesh.rotation = Quaternion.Slerp(
            enemyMesh.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    // =========================
    // DEBUG
    // =========================

    void OnDrawGizmosSelected()
	{
		// Draw chase range
		Gizmos.color = isChasing ? Color.red : Color.yellow;
		Gizmos.DrawWireSphere(transform.position, chaseRange);

		// Draw current move direction
		Gizmos.color = Color.green;
		Gizmos.DrawRay(transform.position, currentMoveDirection * 2f);

		// Draw wander target
		if (!isChasing && !isInvestigating)
		{
			Gizmos.color = Color.cyan;
			Gizmos.DrawWireSphere(wanderTarget, 0.5f);
			Gizmos.DrawLine(transform.position, wanderTarget);
		}

		// Draw investigation target
		if (isInvestigating)
		{
			Gizmos.color = Color.yellow;
			Gizmos.DrawWireSphere(investigationTarget, 0.5f);
			Gizmos.DrawLine(transform.position, investigationTarget);
		}

		// Draw target connection in chase mode
		if (isChasing && currentTarget != null)
		{
			Gizmos.color = Color.magenta;
			Gizmos.DrawLine(transform.position, currentTarget.transform.position);
		}

		// Draw pathfinding path
		if (showPath && currentPath != null && currentPath.Count > 0)
		{
			Gizmos.color = Color.blue;
			
			// Draw line to first waypoint
			Gizmos.DrawLine(transform.position, currentPath[0]);
			
			// Draw path segments
			for (int i = 0; i < currentPath.Count - 1; i++)
			{
				Gizmos.DrawLine(currentPath[i], currentPath[i + 1]);
				Gizmos.DrawWireSphere(currentPath[i], 0.3f);
			}
			
			// Draw current waypoint larger
			if (currentWaypointIndex < currentPath.Count)
			{
				Gizmos.color = Color.cyan;
				Gizmos.DrawWireSphere(currentPath[currentWaypointIndex], 0.5f);
			}
		}

		// Draw stop distance
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(transform.position, stopDistance);

		// Draw obstacle detection range
		Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
		Gizmos.DrawWireSphere(transform.position, obstacleCheckDistance);

		// Draw heat search radius
		if (useHeatMapInvestigation)
		{
			Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
			Gizmos.DrawWireSphere(transform.position, heatSearchRadius);
		}
	}
}