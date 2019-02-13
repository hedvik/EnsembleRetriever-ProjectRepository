using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DistractorEnemy : Pausable
{
    public float _health = 50f;
    public float _healthBarDisplayDuration = 3f;
    public float _fallSpeedOnDamage = 3f;
    public AnimationCurve _healthBarScaleDuringAnimation;

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

    public virtual void InitialiseDistractor(RedirectionManagerER redirectionManager)
    {
        _maxHealth = _health;

        this._redirectionManager = redirectionManager;
        _animatedInterface = GetComponent<AnimatedCharacterInterface>();

        _healthBarTransform = transform.Find("HealthBarCanvas")?.GetComponent<RectTransform>();
        _healthBarFillImage = _healthBarTransform?.GetChild(1)?.GetComponent<Image>();
        _healthBarTransform.localScale = Vector3.zero;
    }

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
                TelegraphAttack();
            }
        }
    }

    protected void InitialisePhases(string path)
    {
        _phases = Resources.LoadAll<EnemyPhase>(path);
        CheckForPhaseChange();
    }

    public virtual void TakeDamage(float damageValue)
    {
        _health = Mathf.Clamp(_health - damageValue, 0, _maxHealth);
        StartCoroutine(DisplayHealth());
        _attackingPhaseActive = false;
        CheckForPhaseChange();
        // Play particles etc

        _animatedInterface.TakeDamageAnimation("Fall", "GroundCrash", _fallSpeedOnDamage, RestartAttacking);
    }

    public virtual void RestartAttacking()
    {
        _attackingPhaseActive = true;
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

    protected virtual void TelegraphAttack(bool resetTimer = true)
    {
        if (resetTimer)
        {
            _attackTimer -= _currentPhase._attackCooldown;
        }

        EnemyAttack newAttack;
        if(_currentPhase._randomAttackOrder)
        {
            newAttack = _currentPhase._enemyAttacks[Random.Range(0, _currentPhase._enemyAttacks.Count)];
        }
        else
        {
            newAttack = _currentPhase._attackOrder[_attackOrderIndex];
            _attackOrderIndex++;
            if(_attackOrderIndex >= _currentPhase._attackOrder.Count)
            {
                _attackOrderIndex = 0;
            }
        }

        _queuedAttack = newAttack;

        if(!string.IsNullOrEmpty(newAttack._telegraphAnimationTrigger))
        {
            _animatedInterface._audioSource.PlayOneShot(newAttack._telegraphAudio, newAttack._telegraphAudioScale);
            _animatedInterface.AnimationTriggerWithCallback(newAttack._telegraphAnimationTrigger, Attack);
        }
        else
        {
            Attack();
        }
    }

    public virtual void Attack()
    {
        var newProjectile = Instantiate(_queuedAttack._attackPrefab, transform.position + transform.forward, Quaternion.identity).GetComponent<ProjectileAttack>();
        newProjectile.Initialise(_queuedAttack, _redirectionManager.headTransform);
        _animatedInterface._audioSource.PlayOneShot(_queuedAttack._spawnAudio, _queuedAttack._spawnAudioScale);
        _queuedAttack = null;

        if (_currentPhase._containsMovement)
        {
            _animatedInterface.RotateAroundPivot(Random.Range(-180f, 180f), _redirectionManager.headTransform.position);
        }
    }

    protected bool CheckForPhaseChange()
    {
        foreach (var phase in _phases)
        {
            if (phase.IsWithinPhaseThreshold(UtilitiesER.Remap(0, _maxHealth, 0, 100, _health)) && phase != _currentPhase)
            {
                _currentPhase = phase;
                _attackOrderIndex = 0;
                if (_currentPhase._usesPhaseTransitionAnimation)
                {
                    // TODO: Trigger a phase change animation
                }
                return true;
            }
        }
        return false;
    }

    protected IEnumerator BeginCombat(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        _attackingPhaseActive = true;
        _animatedInterface.AnimationTrigger("Idle");
        TelegraphAttack(false);
    }
}
