﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileAttack : Pausable
{
    public List<MeshRenderer> _mainMeshRenderers = new List<MeshRenderer>();
    public float _destructionSpeed = 5;

    [HideInInspector]
    public float _movementSpeed = 1;
    [HideInInspector]
    public float _chargeValue = 10f;
    [HideInInspector]
    public Transform _targetTransform;

    protected bool _translating;
    protected BoxCollider _collider;
    protected EnemyAttack _attackSettings;

    public virtual void Initialise(EnemyAttack attack, Transform target, float speedMultiplier)
    {
        _attackSettings = attack;
        _movementSpeed = attack._attackSpeed * speedMultiplier;
        _chargeValue = attack._attackChargeAmount;
        transform.localScale = attack._visualsScale;
        _targetTransform = target;

        if(attack._attackMaterial != null)
        {
            foreach (var renderer in _mainMeshRenderers)
            {
                var materials = renderer.materials;
                materials[0] = attack._attackMaterial;
                renderer.materials = materials;
            }
        }

        _translating = true;
        _collider = GetComponent<BoxCollider>();
    }

    public void Destroy()
    {
        _translating = false;

        if (_collider != null)
        {
            _collider.enabled = false;
        }
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
