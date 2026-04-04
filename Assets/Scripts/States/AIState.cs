using UnityEngine;

public abstract class AIState
{
    protected EnemyAI enemy;
    protected string stateName;

    public AIState(EnemyAI _enemy, string _stateName)
    {
        enemy = _enemy;
        stateName = _stateName;
    }

    // Called once when entering this state
    public virtual void EnterState() { }

    // Called every frame while in this state
    public virtual void UpdateState() { }

    // Called once when exiting this state
    public virtual void ExitState() { }

    // Check if we should transition to another state
    public virtual AIState CheckTransitions() { return null; }

    public string GetStateName() { return stateName; }
}