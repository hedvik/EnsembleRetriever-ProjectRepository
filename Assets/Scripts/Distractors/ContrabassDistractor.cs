using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContrabassDistractor : DistractorEnemy
{
    public override void InitialiseDistractor(RedirectionManagerER redirectionManager, bool findSpawnPosition = true)
    {
        base.InitialiseDistractor(redirectionManager);
        _animatedInterface.AnimationTrigger("Jumps");
        InitialisePhases("ScriptableObjects/EnemyPhases/AngryContrabass");
        StartCoroutine(BeginCombat(_timeUntilStartAfterSpawn));
        _redirectionManager._gameManager.PlayBattleTheme();
    }

    public override void TakeDamage(float damageValue)
    {
        base.TakeDamage(damageValue);

        if(_health == 0)
        {
            _redirectionManager._gameManager.StopBattleTheme();
        }
    }
}
