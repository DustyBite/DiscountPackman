using UnityEngine;
using System.Collections.Generic;

public class HideState : AIState
{
    private GameObject threat;
    private Vector3 hideSpot;
    private bool foundHideSpot = false;
    private float hideTimer = 0f;
    private float hideDuration = 5f;

    public HideState(EnemyAI _enemy, GameObject _threat) : base(_enemy, "Hide")
    {
        threat = _threat;
    }

    public override void EnterState()
    {
        Debug.Log($"{enemy.name} entering HIDE state");
        enemy.isHiding = true;
        hideTimer = 0f;
        
        // Find a hiding spot
        hideSpot = FindHidingSpot();
        foundHideSpot = (hideSpot != Vector3.zero);
    }

    public override void UpdateState()
    {
        if (!foundHideSpot)
        {
            // Couldn't find spot - just stay still
            hideTimer += Time.deltaTime;
            return;
        }

        float distanceToSpot = Vector3.Distance(enemy.transform.position, hideSpot);

        // Still moving to hide spot
        if (distanceToSpot > 1f)
        {
            if (enemy.usePathfinding && enemy.pathfinding != null)
            {
                enemy.UpdatePathfinding(hideSpot);
                enemy.FollowPath();
            }
            else
            {
                Vector3 desiredDir = (hideSpot - enemy.transform.position).normalized;
                Vector3 moveDir = enemy.ChooseBestDirection(desiredDir);
                moveDir = enemy.AdvancedObstacleAvoidance(moveDir);
                enemy.MoveInDirection(moveDir);
            }
        }
        else
        {
            // Hiding - stay still
            hideTimer += Time.deltaTime;
        }
    }

    public override void ExitState()
    {
        Debug.Log($"{enemy.name} exiting HIDE state");
        enemy.isHiding = false;
    }

    public override AIState CheckTransitions()
    {
        // Hid long enough
        if (hideTimer > hideDuration)
        {
            return new PatrolState(enemy);
        }

        // Threat got too close - flee again
        if (threat != null)
        {
            float distance = Vector3.Distance(enemy.transform.position, threat.transform.position);
            if (distance < 5f)
            {
                return new FleeState(enemy, threat);
            }
        }

        return null;
    }

    Vector3 FindHidingSpot()
    {
        if (threat == null) return Vector3.zero;

        // Cast rays to find cover
        Vector3 awayFromThreat = (enemy.transform.position - threat.transform.position).normalized;
        
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f;
            Vector3 direction = Quaternion.Euler(0, angle, 0) * awayFromThreat;
            
            RaycastHit hit;
            if (Physics.Raycast(enemy.transform.position, direction, out hit, 10f, enemy.obstacleLayer))
            {
                // Found cover - hide behind it
                Vector3 hidePosition = hit.point + direction * 2f;
                return hidePosition;
            }
        }

        // No cover found - just run far away
        return enemy.transform.position + awayFromThreat * 10f;
    }
}