using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyPhase", menuName = "EnsembleRetriever/EnemyPhase")]
public class EnemyPhase : ScriptableObject
{
    public float _attackCooldown = 5f;
    public bool _containsMovement;
    public Vector2Int _activeInHealthPercentage;
    public bool _usesPhaseTransitionAnimation = false;
    public List<EnemyAttack> _enemyAttacks = new List<EnemyAttack>();
    public bool _randomAttackOrder;

    // COULD REFACTOR: Feels a bit redundant with _enemyAttacks present, but it works for now
    public List<EnemyAttack> _attackOrder = new List<EnemyAttack>();

    public bool IsWithinPhaseThreshold(float healthValue)
    {
        return (healthValue >= _activeInHealthPercentage.x && healthValue <= _activeInHealthPercentage.y);
    }
}