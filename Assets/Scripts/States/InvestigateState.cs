using UnityEngine;
using System.Collections.Generic;

public class InvestigateState : AIState
{
    private Vector3 investigationPoint;
    private float investigateTimer = 0f;
    private float investigateDuration = 3f;
    private bool reachedPoint = false;

    public InvestigateState(EnemyAI _enemy, Vector3 _point) : base(_enemy, "Investigate")
    {
        investigationPoint = _point;
    }

    public override void EnterState()
    {
        Debug.Log($"{enemy.name} entering INVESTIGATE state");
        enemy.isInvestigating = true;
        investigateTimer = 0f;
        reachedPoint = false;
    }

    public override void UpdateState()
    {
        float distance = Vector3.Distance(enemy.transform.position, investigationPoint);

        if (distance < 2f)
        {
            reachedPoint = true;
            investigateTimer += Time.deltaTime;
            
            // Look around
            enemy.LookAround();
        }
        else
        {
            // Move to investigation point
            if (enemy.usePathfinding && enemy.pathfinding != null)
            {
                enemy.UpdatePathfinding(investigationPoint);
                enemy.FollowPath();
            }
            else
            {
                Vector3 desiredDir = (investigationPoint - enemy.transform.position).normalized;
                Vector3 moveDir = enemy.ChooseBestDirection(desiredDir);
                moveDir = enemy.AdvancedObstacleAvoidance(moveDir);
                enemy.MoveInDirection(moveDir);
            }
        }
    }

    public override void ExitState()
    {
        Debug.Log($"{enemy.name} exiting INVESTIGATE state");
        enemy.isInvestigating = false;
    }

    public override AIState CheckTransitions()
    {
        // Spotted target while investigating
        GameObject target = enemy.DetectTargets();
        if (target != null)
        {
            EnemyAI targetAI = target.GetComponent<EnemyAI>();
            float targetScary = targetAI != null ? targetAI.scary : 4f;

            if (targetScary > enemy.bravey)
            {
                return new FleeState(enemy, target);
            }
            else
            {
                return new ChaseState(enemy, target);
            }
        }

        // Finished investigating
        if (reachedPoint && investigateTimer > investigateDuration)
        {
            return new PatrolState(enemy);
        }

        return null;
    }
}