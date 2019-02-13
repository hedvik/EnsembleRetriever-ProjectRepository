using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OboeDistractor : DistractorEnemy
{
    public float _forwardOffsetFromPlayer = 2f;
    public float _timeUntilStartAfterSpawn = 2f;

    public override void InitialiseDistractor(RedirectionManagerER redirectionManager)
    {
        base.InitialiseDistractor(redirectionManager);

        var spawnPosition = _redirectionManager.headTransform.position + _redirectionManager.headTransform.forward * _forwardOffsetFromPlayer;
        spawnPosition.y = _redirectionManager.headTransform.position.y;
        _animatedInterface.TeleportToPosition(spawnPosition);
        transform.LookAt(_redirectionManager.headTransform.position);

        _animatedInterface.AnimationTrigger("Jumps");

        InitialisePhases("ScriptableObjects/EnemyPhases/AngryOboe");

        StartCoroutine(BeginCombat(_timeUntilStartAfterSpawn));
    }
}
