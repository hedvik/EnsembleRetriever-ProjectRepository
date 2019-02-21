using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MountainKing : DistractorEnemy
{
    public Material _normalEyes;
    public AudioClip _fanfareOnDefeatAudio;
    private Vector3 _startPosition;

    public override void InitialiseDistractor(RedirectionManagerER redirectionManager, bool findSpawnPosition = true)
    {
        base.InitialiseDistractor(redirectionManager, false);
        _animatedInterface.AnimationTrigger("Conducting");
        _startPosition = transform.position;
    }

    public void StartMountainKing()
    {
        InitialisePhases("ScriptableObjects/EnemyPhases/MountainKing");
        StartCoroutine(BeginCombat(_timeUntilStartAfterSpawn));
    }

    public override void TakeDamage(float damageValue)
    {
        base.TakeDamage(damageValue);

        if (_health == 0)
        {
            _redirectionManager._gameManager._mountainKingAudioSource.Stop();
        }
    }

    public override void Die()
    {
        _animatedInterface._eyeRenderer.material = _normalEyes;
        _animatedInterface._audioSource.PlayOneShot(_fanfareOnDefeatAudio);
        _animatedInterface.TeleportToPosition(_startPosition);
        _animatedInterface.AnimationTrigger("Idle");
        _redirectionManager._gameManager.EndGame();
    }
}
