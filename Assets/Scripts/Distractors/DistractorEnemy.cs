﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Primary distractor class for Ensemble Retriever. 
/// Contains the base functionality for all distractor enemies. 
/// </summary>
public class DistractorEnemy : Pausable
{
    public float _health = 50f;
    public float _healthBarDisplayDuration = 3f;
    public float _fallSpeedOnDamage = 3f;
    public AnimationCurve _healthBarScaleDuringAnimation;
    public int _awardedEXP = 50;

    public float _forwardOffsetFromPlayer = 10f;
    public float _timeUntilStartAfterSpawn = 2f;

    public DistractorType _distractorType;

    protected RedirectionManagerER _redirectionManager;
    protected AnimatedCharacterInterface _animatedInterface;

    protected RectTransform _healthBarTransform;
    protected Image _healthBarFillImage;
    protected float _maxHealth;

    protected float _attackTimer = 0f;
    protected EnemyPhase[] _phases;
    protected EnemyPhase _currentPhase;

    protected bool _attackingPhaseActive = false;
    protected int _attackOrderIndex = 0;

    protected EnemyAttack _queuedAttack = null;

    protected AudioClip[] _uniqueTelegraphAudioClips;
    protected AudioClip[] _speedTelegraphAudioClips;

    protected List<GameObject> _attackObjects = new List<GameObject>();

    public virtual void InitialiseDistractor(RedirectionManagerER redirectionManager, bool findSpawnPosition = true)
    {
        _maxHealth = _health;
        _attackingPhaseActive = false;

        this._redirectionManager = redirectionManager;
        _animatedInterface = GetComponent<AnimatedCharacterInterface>();

        _healthBarTransform = transform.Find("HealthBarCanvas")?.GetComponent<RectTransform>();
        _healthBarFillImage = _healthBarTransform?.GetChild(1)?.GetComponent<Image>();
        _healthBarTransform.localScale = Vector3.zero;

        _uniqueTelegraphAudioClips = Resources.LoadAll<AudioClip>("Audio/InstrumentTelegraphAudio/");
        _speedTelegraphAudioClips = Resources.LoadAll<AudioClip>("Audio/SpeedTelegraphAudio/");

        if (findSpawnPosition)
        {
            var spawnPosition = _redirectionManager.headTransform.position + _redirectionManager.headTransform.forward * _forwardOffsetFromPlayer;
            spawnPosition.y = _redirectionManager.headTransform.position.y;
            _animatedInterface.TeleportToPosition(spawnPosition);
            transform.LookAt(_redirectionManager.headTransform.position);
        }
    }

    // This callback will be called towards the end of RedirectionManager.OnDistractorEnd() and is aimed to be used for cleanup.
    public virtual void FinaliseDistractor()
    {
        Destroy(gameObject);
    }

    protected virtual void Update()
    {
        if (!_isPaused && _attackingPhaseActive)
        {
            _attackTimer += Time.deltaTime;

            if (_attackTimer >= _currentPhase._attackCooldown)
            {
                StartAttackTelegraphs();
            }
        }
    }

    protected void InitialisePhases(string path)
    {
        _phases = Resources.LoadAll<EnemyPhase>(path);
    }

    public virtual void TakeDamage(float damageValue)
    {
        if(!_attackingPhaseActive)
        {
            return;
        }

        _health = Mathf.Clamp(_health - damageValue, 0, _maxHealth);
        StartCoroutine(DisplayHealth());
        _attackingPhaseActive = false;
        _animatedInterface.CleanCallbacks();

        if (_health > 0)
        {
            _animatedInterface.TakeDamageAnimation("Fall", "GroundCrash", _fallSpeedOnDamage, CheckForPhaseChange, true);
        }
        else
        {
            _animatedInterface.TakeDamageAnimation("FallDeath", "DeathCrash", 0.75f, Die, false);
        }
    }

    public virtual void Die()
    {
        _redirectionManager._playerManager.ResetCharge();
        _animatedInterface.AnimationTriggerWithCallback("Death", AwardEXPAndFinish);

        foreach(var attackObject in _attackObjects)
        {
            if(attackObject != null)
            {
                attackObject.GetComponent<ProjectileAttack>().Destroy();
            }
        }
    }

    public void AwardEXPAndFinish()
    {
        _redirectionManager._playerManager.AddEXP(_awardedEXP);
        _redirectionManager.OnDistractorEnd();
    }

    public virtual void RestartAttacking()
    {
        _attackingPhaseActive = true;
        _animatedInterface.SetSweatState(_currentPhase._sweatState);
    }

    protected IEnumerator DisplayHealth()
    {
        var targetFill = UtilitiesER.Remap(0, _maxHealth, 0, 1, _health);
        var initialFill = _healthBarFillImage.fillAmount;
        var healthAnimationTimer = 0f;

        while (healthAnimationTimer <= _healthBarDisplayDuration)
        {
            yield return null;
            healthAnimationTimer += Time.deltaTime;
            _healthBarFillImage.fillAmount = Mathf.Lerp(initialFill, targetFill, healthAnimationTimer * 2f);
            _healthBarTransform.localScale = Vector3.one * _healthBarScaleDuringAnimation.Evaluate(UtilitiesER.Remap(0, _healthBarDisplayDuration, 0, 1, healthAnimationTimer));
        }

        _healthBarTransform.localScale = Vector3.zero;
        _healthBarFillImage.fillAmount = targetFill;
    }

    protected virtual void StartAttackTelegraphs()
    {
        _attackTimer -= _currentPhase._attackCooldown;
        EnemyAttack newAttack;
        if (_currentPhase._randomAttackOrder)
        {
            newAttack = _currentPhase._enemyAttacks[Random.Range(0, _currentPhase._enemyAttacks.Count)];
        }
        else
        {
            newAttack = _currentPhase._attackOrder[_attackOrderIndex];
            _attackOrderIndex++;
            if (_attackOrderIndex >= _currentPhase._attackOrder.Count)
            {
                _attackOrderIndex = 0;
            }
        }
        
        _queuedAttack = newAttack;

        if (newAttack._attackParentInstrument != AttackTypeInstrument.none)
        {
            // Run Unique Telegraph + audio
            var uniqueTelegraphAudio = FindAudioClipInArray(UtilitiesER.AttackTypeInstrumentTriggers[newAttack._attackParentInstrument], _uniqueTelegraphAudioClips);
            _animatedInterface._audioSource.PlayOneShot(uniqueTelegraphAudio, UtilitiesER.AttackTypeInstrumentAudioScales[_queuedAttack._attackParentInstrument]);
            _animatedInterface.AnimationTriggerWithCallback(UtilitiesER.AttackTypeInstrumentTriggers[newAttack._attackParentInstrument], StartAttackSpeedTelegraph);
        }
        else
        {
            StartAttackSpeedTelegraph();
        }
    }

    public void StartAttackSpeedTelegraph()
    {
        // Run attack speed type telegraph + audio
        var speedTelegraphAudio = FindAudioClipInArray(UtilitiesER.AttackTypeSpeedTriggers[_queuedAttack._attackType], _speedTelegraphAudioClips);
        _animatedInterface._audioSource.PlayOneShot(speedTelegraphAudio, UtilitiesER.AttackTypeSpeedAudioScales[_queuedAttack._attackType]);
        _animatedInterface.AnimationTriggerWithCallback(UtilitiesER.AttackTypeSpeedTriggers[_queuedAttack._attackType], Attack);
    }

    public virtual void Attack()
    {
        var newProjectile = Instantiate(_queuedAttack._attackPrefab, transform.position + transform.forward, Quaternion.identity).GetComponent<ProjectileAttack>();
        newProjectile.Initialise(_queuedAttack, _redirectionManager.headTransform, _currentPhase._speedMultiplier);
        _animatedInterface._audioSource.PlayOneShot(_queuedAttack._spawnAudio, _queuedAttack._spawnAudioScale);
        _queuedAttack = null;
        _attackObjects.Add(newProjectile.gameObject);

        if (_currentPhase._containsMovement)
        {
            _animatedInterface.RotateAroundPivot(Random.Range(-180f, 180f), _redirectionManager.headTransform.position);
        }

        _animatedInterface.AnimationTrigger("Idle");
    }

    public void CheckForPhaseChange()
    {
        var phaseChanged = false;
        foreach (var phase in _phases)
        {
            if (phase.IsWithinPhaseThreshold(UtilitiesER.Remap(0, _maxHealth, 0, 100, _health)) && phase != _currentPhase)
            {
                _currentPhase = phase;
                _attackOrderIndex = 0;
                _attackTimer = _currentPhase._attackCooldown;
                if (_currentPhase._usesPhaseTransitionAnimation)
                {
                    _animatedInterface.AnimationTriggerWithCallback("PhaseTransition", RestartAttacking);
                    phaseChanged = true;
                }
            }
        }
        
        if(!phaseChanged)
        {
            RestartAttacking();
        }
    }

    protected IEnumerator BeginCombat(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        _attackingPhaseActive = true;
        _animatedInterface.AnimationTrigger("Idle");
        CheckForPhaseChange();
    }

    protected AudioClip FindAudioClipInArray(string animationTriggerName, AudioClip[] audioClipArray)
    {
        foreach (var audioClip in audioClipArray)
        {
            if (audioClip.name == animationTriggerName)
            {
                return audioClip;
            }
        }
        return null;
    }
}
