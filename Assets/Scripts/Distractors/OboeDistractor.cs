using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OboeDistractor : DistractorEnemy
{
    public override void InitialiseDistractor(RedirectionManagerER redirectionManager, bool findSpawnPosition = true)
    {
        base.InitialiseDistractor(redirectionManager);
        _animatedInterface.AnimationTrigger("Jumps");
        InitialisePhases("ScriptableObjects/EnemyPhases/AngryOboe");
        StartCoroutine(BeginCombat(_timeUntilStartAfterSpawn));
    }
}
