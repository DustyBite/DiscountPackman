using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("Personality")]
    public float scary = 5f;
    public float bravey = 7f;

    [Header("AI Settings")]
    public Transform enemyMesh;
    public float moveSpeed = 3f;
    public float rotationSpeed = 10f;
    public float stopDistance = 1.5f;
    public float wanderRadius = 5f;
    public float wanderTime = 2f;
    public float chaseRange = 8f; // max distance to chase/flee

    private Vector3 wanderTarget;
    private float wanderTimer = 0f;

    void Update()
    {
        Vector3 moveDirection = Vector3.zero;
        bool hasTarget = false;

        // Find all targets: enemies + player
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        // Combine targets
        System.Collections.Generic.List<GameObject> targets = new System.Collections.Generic.List<GameObject>();
        if (enemies != null) targets.AddRange(enemies);
        if (playerObj != null) targets.Add(playerObj);

        // Decide behavior
        foreach (GameObject t in targets)
        {
            if (t == this.gameObject) continue; // skip self
            EnemyAI targetAI = t.GetComponent<EnemyAI>();
            float targetScary = targetAI != null ? targetAI.scary : 4f; // default for player
            float distance = Vector3.Distance(transform.position, t.transform.position);

            if (distance > chaseRange) continue; // skip targets out of range

            if (targetScary < bravey)
            {
                moveDirection = Seek(t.transform.position);
                hasTarget = true;
                break;
            }
            else if (targetScary > bravey)
            {
                moveDirection = Flee(t.transform.position);
                hasTarget = true;
                break;
            }
        }

        if (!hasTarget)
            moveDirection = Wander();

        // Obstacle avoidance
        moveDirection = ObstacleAvoidance(moveDirection);

        // Move
        transform.position += moveDirection * moveSpeed * Time.deltaTime;

        // Rotate mesh
        if (moveDirection != Vector3.zero && enemyMesh != null)
            RotateMesh(moveDirection);
    }

    Vector3 Seek(Vector3 targetPos)
    {
        Vector3 dir = targetPos - transform.position;
        dir.y = 0f;
        if (dir.magnitude < stopDistance) return Vector3.zero;
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
        return dir.normalized;
    }

    Vector3 ObstacleAvoidance(Vector3 moveDir)
    {
        float rayDistance = 1f;
        RaycastHit hit;

        if (Physics.Raycast(transform.position, moveDir, out hit, rayDistance))
        {
            if (!hit.collider.CompareTag("Enemy") && !hit.collider.CompareTag("Player"))
            {
                Vector3 avoidDir = Vector3.Cross(Vector3.up, hit.normal).normalized;
                return avoidDir;
            }
        }

        return moveDir;
    }

    void RotateMesh(Vector3 moveDirection)
    {
        Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
        enemyMesh.rotation = Quaternion.Slerp(enemyMesh.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
}
