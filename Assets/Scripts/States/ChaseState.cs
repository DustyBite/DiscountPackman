using UnityEngine;
using System.Collections.Generic;

public class ChaseState : AIState
{
    private GameObject target;
    private float lostTargetTimer = 0f;
    private float maxLostTime = 3f;

    public ChaseState(EnemyAI _enemy, GameObject _target) : base(_enemy, "Chase")
    {
        target = _target;
    }

    public override void EnterState()
    {
        Debug.Log($"{enemy.name} entering CHASE state");
        enemy.isChasing = true;
        enemy.currentTarget = target;
        lostTargetTimer = 0f;
    }

    public override void UpdateState()
    {
        if (target == null)
        {
            lostTargetTimer += Time.deltaTime;
            return;
        }

        float distance = Vector3.Distance(enemy.transform.position, target.transform.position);

        // Can still see target - reset timer
        if (distance < enemy.chaseRange)
        {
            lostTargetTimer = 0f;
        }
        else
        {
            lostTargetTimer += Time.deltaTime;
        }

        // Chase the target
        Vector3 targetPos = target.transform.position;

        if (enemy.usePathfinding && enemy.pathfinding != null)
        {
            enemy.UpdatePathfinding(targetPos);
            enemy.FollowPath();
        }
        else
        {
            Vector3 desiredDir = (targetPos - enemy.transform.position).normalized;
            Vector3 moveDir = enemy.ChooseBestDirection(desiredDir);
            moveDir = enemy.AdvancedObstacleAvoidance(moveDir);
            enemy.MoveInDirection(moveDir);
        }
    }

    public override void ExitState()
    {
        Debug.Log($"{enemy.name} exiting CHASE state");
        enemy.isChasing = false;
        enemy.currentTarget = null;
    }

    public override AIState CheckTransitions()
	{
		if (target == null || lostTargetTimer > maxLostTime)
		{
			if (target != null)
			{
				return new InvestigateState(enemy, target.transform.position);
			}
			return new PatrolState(enemy);
		}

		// Use fuzzy logic for flee decision
		if (enemy.fuzzyLogic != null && enemy.fuzzyLogic.useFuzzyLogic)
		{
			if (enemy.fuzzyLogic.ShouldFlee())
			{
				return new FleeState(enemy, target);
			}

			// Transition to attack if close enough and aggressive
			float distance = Vector3.Distance(enemy.transform.position, target.transform.position);
			if (distance <= enemy.attackRange && enemy.fuzzyLogic.ShouldAttack())
			{
				return new AttackState(enemy, target);
			}
		}
		else
		{
			// Fallback to original logic
			EnemyAI targetAI = target.GetComponent<EnemyAI>();
			float targetScary = targetAI != null ? targetAI.scary : 4f;
			
			if (targetScary > enemy.bravey || enemy.currentHealth < enemy.fleeHealthThreshold)
			{
				return new FleeState(enemy, target);
			}
		}

		return null;
	}
}