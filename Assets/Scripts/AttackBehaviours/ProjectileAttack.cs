using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileAttack : Pausable
{
    public MeshRenderer _mainMeshRenderer;
    public float _destructionSpeed = 5;

    [HideInInspector]
    public float _movementSpeed = 1;
    [HideInInspector]
    public float _chargeValue = 10f;
    [HideInInspector]
    public Transform _targetTransform;

    protected bool _translating;
    protected BoxCollider _collider;

    protected virtual void Start()
    {
        _translating = true;
        _collider = GetComponent<BoxCollider>();
    }

    public void Initialise(EnemyAttack attack, Transform target)
    {
        _movementSpeed = attack._attackSpeed;
        _chargeValue = attack._attackChargeAmount;
        transform.localScale = attack._visualsScale;
        _targetTransform = target;
    }

    public void Destroy()
    {
        _translating = false;
        _collider.enabled = false;
        StartCoroutine(DestructionAnimation());
    }

    private IEnumerator DestructionAnimation()
    {
        var timer = 0f;
        var baseScale = transform.localScale;
        while (timer <= 1)
        {
            timer += Time.deltaTime * _destructionSpeed;
            transform.localScale = Vector3.Lerp(baseScale, Vector3.zero, timer);
            yield return null;
        }

        Destroy(gameObject);
    }

    protected override void PauseStateChange()
    {
        _translating = !_isPaused;
    }
}
