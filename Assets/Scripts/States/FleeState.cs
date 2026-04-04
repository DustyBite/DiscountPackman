using UnityEngine;
using System.Collections.Generic;

public class FleeState : AIState
{
    private GameObject threat;
    private float safeDistance = 15f;
    private float fleeTimer = 0f;
    private float maxFleeTime = 5f;

    public FleeState(EnemyAI _enemy, GameObject _threat) : base(_enemy, "Flee")
    {
        threat = _threat;
    }

    public override void EnterState()
    {
        Debug.Log($"{enemy.name} entering FLEE state");
        enemy.isFleeing = true;
        fleeTimer = 0f;
    }

    public override void UpdateState()
    {
        fleeTimer += Time.deltaTime;

        if (threat == null)
        {
            return;
        }

        // Run away from threat
        Vector3 fleeDirection = (enemy.transform.position - threat.transform.position).normalized;
        fleeDirection.y = 0f;

        Vector3 fleeTarget = enemy.transform.position + fleeDirection * safeDistance;

        if (enemy.usePathfinding && enemy.pathfinding != null)
        {
            enemy.UpdatePathfinding(fleeTarget);
            enemy.FollowPath();
        }
        else
        {
            Vector3 moveDir = enemy.ChooseBestDirection(fleeDirection);
            moveDir = enemy.AdvancedObstacleAvoidance(moveDir);
            enemy.MoveInDirection(moveDir);
        }
    }

    public override void ExitState()
    {
        Debug.Log($"{enemy.name} exiting FLEE state");
        enemy.isFleeing = false;
    }

    public override AIState CheckTransitions()
    {
        if (threat == null)
        {
            return new PatrolState(enemy);
        }

        float distance = Vector3.Distance(enemy.transform.position, threat.transform.position);

        // Escaped to safety
        if (distance > safeDistance || fleeTimer > maxFleeTime)
        {
            // Try to hide
            return new HideState(enemy, threat);
        }

        // Health restored - can fight again
        if (enemy.currentHealth > enemy.bravey)
        {
            EnemyAI threatAI = threat.GetComponent<EnemyAI>();
            float threatScary = threatAI != null ? threatAI.scary : 4f;
            
            if (threatScary < enemy.bravey)
            {
                return new ChaseState(enemy, threat);
            }
        }

        return null;
    }
}