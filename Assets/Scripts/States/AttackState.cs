using UnityEngine;
using System.Collections.Generic;

public class AttackState : AIState
{
    private GameObject target;
    private float attackCooldown = 0f;
    private float attackRate = 1f;

    public AttackState(EnemyAI _enemy, GameObject _target) : base(_enemy, "Attack")
    {
        target = _target;
    }

    public override void EnterState()
    {
        Debug.Log($"{enemy.name} entering ATTACK state");
        enemy.isAttacking = true;
        attackCooldown = 0f;
    }

    public override void UpdateState()
    {
        if (target == null) return;

        attackCooldown -= Time.deltaTime;

        float distance = Vector3.Distance(enemy.transform.position, target.transform.position);

        // In attack range
        if (distance <= enemy.attackRange)
        {
            // Face target
            Vector3 lookDir = target.transform.position - enemy.transform.position;
            lookDir.y = 0f;
            
            if (enemy.enemyMesh != null && lookDir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir);
                enemy.enemyMesh.rotation = Quaternion.Slerp(
                    enemy.enemyMesh.rotation,
                    targetRot,
                    enemy.rotationSpeed * Time.deltaTime
                );
            }

            // Attack
            if (attackCooldown <= 0f)
            {
                enemy.PerformAttack(target);
                attackCooldown = attackRate;
            }
        }
        else
        {
            // Move closer
            Vector3 desiredDir = (target.transform.position - enemy.transform.position).normalized;
            Vector3 moveDir = enemy.ChooseBestDirection(desiredDir);
            moveDir = enemy.AdvancedObstacleAvoidance(moveDir);
            enemy.MoveInDirection(moveDir);
        }
    }

    public override void ExitState()
    {
        Debug.Log($"{enemy.name} exiting ATTACK state");
        enemy.isAttacking = false;
    }

    public override AIState CheckTransitions()
    {
        if (target == null)
        {
            return new PatrolState(enemy);
        }

        float distance = Vector3.Distance(enemy.transform.position, target.transform.position);

        // Target too far - chase
        if (distance > enemy.attackRange * 1.5f)
        {
            return new ChaseState(enemy, target);
        }

        // Low health - flee
        if (enemy.currentHealth < enemy.fleeHealthThreshold)
        {
            return new FleeState(enemy, target);
        }

        // Target became too scary
        EnemyAI targetAI = target.GetComponent<EnemyAI>();
        float targetScary = targetAI != null ? targetAI.scary : 4f;
        
        if (targetScary > enemy.bravey)
        {
            return new FleeState(enemy, target);
        }

        return null;
    }
}