using UnityEngine;
using System.Collections.Generic;

public class PatrolState : AIState
{
    private List<Vector3> patrolPoints = new List<Vector3>();
    private int currentPatrolIndex = 0;
    private float patrolWaitTimer = 0f;
    private float patrolWaitTime = 2f;
    private bool waiting = false;

    public PatrolState(EnemyAI _enemy) : base(_enemy, "Patrol") { }

    public override void EnterState()
    {
        Debug.Log($"{enemy.name} entering PATROL state");
        
        // Generate random patrol points around spawn
        GeneratePatrolPoints();
        currentPatrolIndex = 0;
        waiting = false;
    }

    public override void UpdateState()
    {
        if (patrolPoints.Count == 0)
        {
            GeneratePatrolPoints();
            return;
        }

        if (waiting)
        {
            patrolWaitTimer -= Time.deltaTime;
            if (patrolWaitTimer <= 0f)
            {
                waiting = false;
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Count;
            }
            return;
        }

        // Move to current patrol point
        Vector3 targetPoint = patrolPoints[currentPatrolIndex];
        Vector3 desiredDirection = (targetPoint - enemy.transform.position).normalized;
        desiredDirection.y = 0f;

        // Check if reached patrol point
        if (Vector3.Distance(enemy.transform.position, targetPoint) < 1f)
        {
            waiting = true;
            patrolWaitTimer = patrolWaitTime;
        }

        // Use pathfinding if available
        if (enemy.usePathfinding && enemy.pathfinding != null)
        {
            enemy.UpdatePathfinding(targetPoint);
            enemy.FollowPath();
        }
        else
        {
            Vector3 moveDir = enemy.ChooseBestDirection(desiredDirection);
            moveDir = enemy.AdvancedObstacleAvoidance(moveDir);
            enemy.MoveInDirection(moveDir);
        }
    }

    public override void ExitState()
    {
        Debug.Log($"{enemy.name} exiting PATROL state");
    }

    public override AIState CheckTransitions()
    {
        // Transition to Chase if enemy spotted
        GameObject target = enemy.DetectTargets();
        if (target != null)
        {
            float distance = Vector3.Distance(enemy.transform.position, target.transform.position);
            
            if (distance < enemy.chaseRange)
            {
                EnemyAI targetAI = target.GetComponent<EnemyAI>();
                float targetScary = targetAI != null ? targetAI.scary : 4f;

                // If target is scary, flee instead
                if (targetScary > enemy.bravey)
                {
                    return new FleeState(enemy, target);
                }
                else
                {
                    return new ChaseState(enemy, target);
                }
            }
        }

        // Transition to Investigate if heat detected
        if (enemy.useHeatMapInvestigation && enemy.influenceMap != null)
        {
            Vector3 heatSpot = enemy.FindHottestSpot();
            if (heatSpot != Vector3.zero)
            {
                return new InvestigateState(enemy, heatSpot);
            }
        }

        return null;
    }

    void GeneratePatrolPoints()
    {
        patrolPoints.Clear();
        Vector3 center = enemy.spawnPosition;
        
        for (int i = 0; i < 4; i++)
        {
            float angle = i * 90f + Random.Range(-30f, 30f);
            float distance = Random.Range(5f, enemy.patrolRadius);
            
            Vector3 point = center + Quaternion.Euler(0, angle, 0) * Vector3.forward * distance;
            patrolPoints.Add(point);
        }
    }
}