using UnityEngine;
using System.Collections.Generic;

public class EnemyAI : MonoBehaviour
{
    [Header("Personality")]
    public float scary = 5f;
    public float bravey = 7f;

    [Header("Health")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;
    public float fleeHealthThreshold = 30f;

    [Header("References")]
    public Transform enemyMesh;
    public InfluenceMap influenceMap;
    public CharacterController controller;

    [Header("Movement")]
    public float moveSpeed = 3f;
    public float rotationSpeed = 10f;
    public float stopDistance = 1.5f;

    [Header("Behavior")]
    public float chaseRange = 8f;
    public float attackRange = 2f;
    public float patrolRadius = 15f;
    
    [Header("Patrol")]
    public Vector3 spawnPosition;
    
    [Header("Wander (Fallback)")]
    public float wanderRadius = 5f;
    public float wanderTime = 2f;

    [Header("Influence Tuning")]
    public float influenceWeight = 0.5f;
    public float goalWeight = 4f;
    public float sampleDistance = 2.5f;
    public float smoothing = 5f;

    [Header("Chase Mode")]
    public int wanderSamples = 8;
    public int chaseSamples = 5;
    public float chaseAngleSpread = 60f;
    public float wanderAngleSpread = 180f;

    [Header("Collision Detection")]
    public float obstacleCheckDistance = 2f;
    public int obstacleRayCount = 5;
    public float obstacleRaySpread = 60f;
    public LayerMask obstacleLayer;
    public float wallSlideAmount = 0.7f;

    [Header("Heat Map Investigation")]
    public bool useHeatMapInvestigation = true;
    public float heatInterestThreshold = 2f;
    public float heatSearchRadius = 10f;
    public float heatAttractionWeight = 2f;

    [Header("Pathfinding")]
    public bool usePathfinding = true;
    public Pathfinding pathfinding;
    public float pathUpdateInterval = 0.5f;
    public float waypointReachDistance = 0.5f;

    [Header("FSM Settings")]
    public AIStateType startingState = AIStateType.Patrol;
    public bool showCurrentState = true;

    [Header("Debug")]
    public bool showDebugRays = true;
    public bool showObstacleRays = true;
    public bool showPath = true;
	
	[Header("Fuzzy Logic")]
	public EnemyFuzzyLogic fuzzyLogic;

    // FSM
    private AIState currentState;
    public AIStateType currentStateType;

    // State flags
    [HideInInspector] public bool isChasing = false;
    [HideInInspector] public bool isFleeing = false;
    [HideInInspector] public bool isHiding = false;
    [HideInInspector] public bool isInvestigating = false;
    [HideInInspector] public bool isAttacking = false;
    [HideInInspector] public GameObject currentTarget;

    // Movement
    private Vector3 currentMoveDirection = Vector3.forward;
    private Vector3 investigationTarget;

    // Pathfinding
    [HideInInspector] public List<Vector3> currentPath = new List<Vector3>();
    private int currentWaypointIndex = 0;
    private float pathUpdateTimer = 0f;
    private Vector3 lastPathTarget = Vector3.zero;

    // Wander (fallback)
    private Vector3 wanderTarget;
    private float wanderTimer = 0f;

    // Look around
    private float lookAroundTimer = 0f;
    private float lookAroundInterval = 1f;
    private int lookDirection = 1;

    void Start()
    {
        if (controller == null)
            controller = GetComponent<CharacterController>();

        if (pathfinding == null)
            pathfinding = FindObjectOfType<Pathfinding>();

        spawnPosition = transform.position;
        currentHealth = maxHealth;

        // Initialize FSM with starting state
        SetState(CreateState(startingState));
		
		if (fuzzyLogic == null)
			fuzzyLogic = GetComponent<EnemyFuzzyLogic>();
    }

    void Update()
	{
		// Evaluate fuzzy logic every frame
		if (fuzzyLogic != null && fuzzyLogic.useFuzzyLogic)
		{
			float distToTarget = currentTarget != null ? 
				Vector3.Distance(transform.position, currentTarget.transform.position) : 20f;
			
			float threatLevel = 0f;
			if (currentTarget != null)
			{
				EnemyAI targetAI = currentTarget.GetComponent<EnemyAI>();
				threatLevel = targetAI != null ? targetAI.scary : 4f;
			}

			int nearbyAllies = CountNearbyAllies();
			float currentInfluence = influenceMap != null ? 
				influenceMap.GetInfluence(transform.position) : 0f;

			fuzzyLogic.EvaluateFuzzyLogic(
				currentHealth,
				distToTarget,
				threatLevel,
				bravey,
				nearbyAllies,
				currentInfluence
			);

			// Apply fuzzy modifiers
			ApplyFuzzyModifiers();
		}

		// Run FSM
		if (currentState != null)
		{
			currentState.UpdateState();

			AIState nextState = currentState.CheckTransitions();
			if (nextState != null)
			{
				SetState(nextState);
			}
		}

		UpdateStateType();
	}

	void ApplyFuzzyModifiers()
	{
		// Adjust move speed based on fuzzy logic
		float fuzzySpeedMod = fuzzyLogic.GetFuzzySpeedModifier();
		// Don't directly modify moveSpeed here - use in MoveInDirection instead

		// Adjust influence weight based on caution
		influenceWeight = fuzzyLogic.GetFuzzyInfluenceWeight();
	}

	int CountNearbyAllies()
	{
		int count = 0;
		GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
		
		foreach (GameObject enemy in enemies)
		{
			if (enemy == gameObject) continue;
			
			float dist = Vector3.Distance(transform.position, enemy.transform.position);
			if (dist < 8f) count++;
		}
		
		return count;
	}

    // ==========================================
    // FSM MANAGEMENT
    // ==========================================

    void SetState(AIState newState)
    {
        if (currentState != null)
        {
            currentState.ExitState();
        }

        currentState = newState;
        
        if (currentState != null)
        {
            currentState.EnterState();
        }
    }

    AIState CreateState(AIStateType stateType)
    {
        switch (stateType)
        {
            case AIStateType.Patrol:
                return new PatrolState(this);
            case AIStateType.Chase:
                return new ChaseState(this, currentTarget);
            case AIStateType.Flee:
                return new FleeState(this, currentTarget);
            case AIStateType.Hide:
                return new HideState(this, currentTarget);
            case AIStateType.Investigate:
                return new InvestigateState(this, investigationTarget);
            case AIStateType.Attack:
                return new AttackState(this, currentTarget);
            default:
                return new PatrolState(this);
        }
    }

    void UpdateStateType()
    {
        if (currentState == null) return;

        string stateName = currentState.GetStateName();
        
        if (stateName == "Patrol") currentStateType = AIStateType.Patrol;
        else if (stateName == "Chase") currentStateType = AIStateType.Chase;
        else if (stateName == "Flee") currentStateType = AIStateType.Flee;
        else if (stateName == "Hide") currentStateType = AIStateType.Hide;
        else if (stateName == "Investigate") currentStateType = AIStateType.Investigate;
        else if (stateName == "Attack") currentStateType = AIStateType.Attack;
    }

    // ==========================================
    // PUBLIC METHODS FOR STATES
    // ==========================================

    public GameObject DetectTargets()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        List<GameObject> targets = new List<GameObject>();
        if (enemies != null) targets.AddRange(enemies);
        if (playerObj != null) targets.Add(playerObj);

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

        return closestTarget;
    }

    public void UpdatePathfinding(Vector3 targetPosition)
    {
        pathUpdateTimer += Time.deltaTime;

        bool needsNewPath = currentPath.Count == 0 || 
                           pathUpdateTimer >= pathUpdateInterval ||
                           Vector3.Distance(targetPosition, lastPathTarget) > 2f;

        if (needsNewPath && pathfinding != null)
        {
            currentPath = pathfinding.FindPath(transform.position, targetPosition);
            currentWaypointIndex = 0;
            pathUpdateTimer = 0f;
            lastPathTarget = targetPosition;
        }
    }

    public void FollowPath()
    {
        if (currentPath.Count == 0)
        {
            return;
        }

        Vector3 currentWaypoint = currentPath[currentWaypointIndex];
        Vector3 dirToWaypoint = currentWaypoint - transform.position;
        dirToWaypoint.y = 0f;

        if (dirToWaypoint.magnitude < waypointReachDistance)
        {
            currentWaypointIndex++;
            
            if (currentWaypointIndex >= currentPath.Count)
            {
                currentPath.Clear();
                currentWaypointIndex = 0;
                return;
            }
        }

        Vector3 desiredDirection = dirToWaypoint.normalized;
        Vector3 moveDirection = ChooseBestDirection(desiredDirection);
        moveDirection = AdvancedObstacleAvoidance(moveDirection);
        MoveInDirection(moveDirection);
    }

    public void MoveInDirection(Vector3 moveDirection)
	{
		if (moveDirection.magnitude > 0.01f)
		{
			currentMoveDirection = Vector3.Lerp(
				currentMoveDirection,
				moveDirection,
				smoothing * Time.deltaTime
			).normalized;
		}

		// Apply fuzzy speed modifier
		float speedModifier = 1f;
		if (fuzzyLogic != null && fuzzyLogic.useFuzzyLogic)
		{
			speedModifier = fuzzyLogic.GetFuzzySpeedModifier();
		}

		if (controller != null)
		{
			Vector3 moveVelocity = currentMoveDirection * moveSpeed * speedModifier * Time.deltaTime;
			moveVelocity.y = -2f * Time.deltaTime;
			
			CollisionFlags collisionFlags = controller.Move(moveVelocity);

			if ((collisionFlags & CollisionFlags.Sides) != 0)
			{
				Vector3 slideDir = Vector3.Cross(Vector3.up, currentMoveDirection);
				slideDir = Vector3.Cross(slideDir, Vector3.up).normalized;
				
				Vector3 slideVelocity = slideDir * moveSpeed * speedModifier * wallSlideAmount * Time.deltaTime;
				slideVelocity.y = -2f * Time.deltaTime;
				
				controller.Move(slideVelocity);
			}
		}
		else
		{
			transform.position += currentMoveDirection * moveSpeed * speedModifier * Time.deltaTime;
		}

		if (currentMoveDirection.magnitude > 0.01f && enemyMesh != null)
		{
			RotateMesh(currentMoveDirection);
		}
	}

    public void LookAround()
    {
        lookAroundTimer += Time.deltaTime;

        if (lookAroundTimer > lookAroundInterval)
        {
            lookAroundTimer = 0f;
            lookDirection *= -1;
        }

        if (enemyMesh != null)
        {
            float rotationAmount = 30f * lookDirection * Time.deltaTime;
            enemyMesh.Rotate(Vector3.up, rotationAmount);
        }
    }

    public void PerformAttack(GameObject target)
    {
        Debug.Log($"{name} attacks {target.name}!");
        
        // Add your attack logic here
        // For example: deal damage, play animation, etc.
        
        EnemyAI targetAI = target.GetComponent<EnemyAI>();
        if (targetAI != null)
        {
            targetAI.TakeDamage(10f);
        }
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        Debug.Log($"{name} took {damage} damage. Health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log($"{name} died!");
        // Add death logic here
        Destroy(gameObject);
    }

    // ==========================================
    // INFLUENCE + DECISION
    // ==========================================

    public Vector3 ChooseBestDirection(Vector3 desiredDir)
    {
        if (desiredDir.magnitude < 0.01f)
            return currentMoveDirection;

        if (influenceMap == null)
            return desiredDir;

        int sampleCount = isChasing ? chaseSamples : wanderSamples;
        float angleSpread = isChasing ? chaseAngleSpread : wanderAngleSpread;

        Vector3 bestDir = desiredDir;
        float bestScore = -Mathf.Infinity;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)(sampleCount - 1);
            float angleOffset = (t * 2f - 1f) * (angleSpread * 0.5f);
            
            Vector3 dir = Quaternion.Euler(0, angleOffset, 0) * desiredDir;
            Vector3 samplePos = transform.position + dir * sampleDistance;
            
            float influence = influenceMap.GetInfluence(samplePos);
            float score = -influence * influenceWeight;
            float alignment = Vector3.Dot(dir.normalized, desiredDir);
            score += alignment * goalWeight;

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

    public Vector3 FindHottestSpot()
    {
        float maxHeat = heatInterestThreshold;
        Vector3 hottestPosition = Vector3.zero;

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

    // ==========================================
    // OBSTACLE AVOIDANCE
    // ==========================================

    public Vector3 AdvancedObstacleAvoidance(Vector3 moveDir)
    {
        if (moveDir.magnitude < 0.01f) 
            return moveDir;

        Vector3 rayStart = transform.position + Vector3.up * 0.5f;
        
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

            if (showObstacleRays)
            {
                Color rayColor = hitObstacle ? Color.red : Color.cyan;
                Debug.DrawRay(rayStart, testDir * obstacleCheckDistance, rayColor);
            }

            if (!hitObstacle)
            {
                clearDirections.Add(testDir);
                float alignment = Vector3.Dot(testDir.normalized, moveDir.normalized);
                directionScores.Add(alignment);
            }
            else
            {
                if (hit.collider.CompareTag("Enemy") || hit.collider.CompareTag("Player"))
                {
                    clearDirections.Add(testDir);
                    float alignment = Vector3.Dot(testDir.normalized, moveDir.normalized);
                    directionScores.Add(alignment);
                }
            }
        }

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

        return FindEscapeDirection(rayStart);
    }

    Vector3 FindEscapeDirection(Vector3 rayStart)
    {
        Vector3[] escapeDirections = new Vector3[]
        {
            Vector3.forward, Vector3.back, Vector3.left, Vector3.right,
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

        return -currentMoveDirection;
    }

    void RotateMesh(Vector3 moveDirection)
    {
        Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
        enemyMesh.rotation = Quaternion.Slerp(
            enemyMesh.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    // ==========================================
    // DEBUG VISUALIZATION
    // ==========================================

    void OnDrawGizmosSelected()
    {
        // State-based colors
        Color stateColor = Color.yellow;
        if (isChasing) stateColor = Color.red;
        else if (isFleeing) stateColor = Color.blue;
        else if (isHiding) stateColor = Color.gray;
        else if (isInvestigating) stateColor = Color.cyan;
        else if (isAttacking) stateColor = Color.magenta;

        // Chase range
        Gizmos.color = stateColor;
        Gizmos.DrawWireSphere(transform.position, chaseRange);

        // Attack range
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Current move direction
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, currentMoveDirection * 2f);

        // Investigation target
        if (isInvestigating)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(investigationTarget, 0.5f);
            Gizmos.DrawLine(transform.position, investigationTarget);
        }

        // Target connection
        if (currentTarget != null)
        {
            Gizmos.color = isChasing ? Color.red : Color.blue;
            Gizmos.DrawLine(transform.position, currentTarget.transform.position);
        }

        // Pathfinding path
        if (showPath && currentPath != null && currentPath.Count > 0)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, currentPath[0]);
            
            for (int i = 0; i < currentPath.Count - 1; i++)
            {
                Gizmos.DrawLine(currentPath[i], currentPath[i + 1]);
                Gizmos.DrawWireSphere(currentPath[i], 0.3f);
            }
            
            if (currentWaypointIndex < currentPath.Count)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(currentPath[currentWaypointIndex], 0.5f);
            }
        }

        // State label
        if (showCurrentState && currentState != null)
        {
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 3f,
                currentState.GetStateName(),
                new GUIStyle() { 
                    normal = new GUIStyleState() { textColor = stateColor },
                    fontSize = 14,
                    fontStyle = FontStyle.Bold
                }
            );
        }
    }
}

// ==========================================
// ENUM FOR STATE TYPES
// ==========================================
public enum AIStateType
{
    Patrol,
    Chase,
    Flee,
    Hide,
    Investigate,
    Attack
}