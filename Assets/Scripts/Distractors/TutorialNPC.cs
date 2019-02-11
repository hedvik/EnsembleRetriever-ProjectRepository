﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialNPC : DistractorEnemy
{
    private bool _attackTutorialActive = false;

    public void StartTutorialAttacks()
    {
        _attackTutorialActive = true;
        StartCoroutine(TutorialAttackPhase());
    }

    public override void TakeDamage(float damageValue)
    {
        base.TakeDamage(damageValue);
        _animatedInterface.TakeDamageAnimation("Fall", "GroundCrash", 3f, _redirectionManager._gameManager.AttackEventTriggerDialogue);
        _attackTutorialActive = false;
    }

    private IEnumerator TutorialAttackPhase()
    {
        var tutorialPhase = Resources.Load<EnemyPhase>("ScriptableObjects/EnemyPhases/Tutorial/Normal");
        var attackTimer = 0f;
        var tutorialAttack = tutorialPhase._enemyAttacks[0];

        while (_attackTutorialActive)
        {
            if (!_isPaused)
            {
                attackTimer += Time.deltaTime;

                if (attackTimer >= tutorialPhase._attackCooldown)
                {
                    attackTimer -= tutorialPhase._attackCooldown;
                    var projectile = Instantiate(tutorialAttack._attackPrefab, transform.position + transform.forward, Quaternion.identity).GetComponent<BasicProjectile>();
                    projectile.Initialise(tutorialAttack, _redirectionManager.headTransform);
                    _animatedInterface._audioSource.PlayOneShot(tutorialAttack._spawnAudio, tutorialAttack._spawnAudioScale);
                }
            }
            yield return null;
        }
    }
}