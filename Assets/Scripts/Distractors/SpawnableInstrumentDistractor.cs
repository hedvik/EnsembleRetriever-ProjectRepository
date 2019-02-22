using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnableInstrumentDistractor : DistractorEnemy
{
    public string _distractorName = "AngryOboe";

    public override void InitialiseDistractor(RedirectionManagerER redirectionManager, bool findSpawnPosition = true)
    {
        base.InitialiseDistractor(redirectionManager);
        _animatedInterface.AnimationTrigger("Jumps");
        InitialisePhases("ScriptableObjects/EnemyPhases/" + _distractorName);
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
