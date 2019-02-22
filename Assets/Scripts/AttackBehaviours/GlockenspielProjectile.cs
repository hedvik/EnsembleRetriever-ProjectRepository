using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlockenspielProjectile : ProjectileAttack
{
    public float _rotationSpeed = 30f;
    public float _bezierAnchorYOffset = 5f;
    public float _bezierAnchorXOffset = 5f;

    private Vector3 _rotationAxis;
    private Vector3 _startPosition;
    private Vector3 _anchorOffset;
    private float _lerpTimer;

    private void Start()
    {
        _rotationAxis = new Vector3(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f)).normalized;
        _startPosition = transform.position;
        transform.LookAt(_targetTransform, Vector3.up);
        _anchorOffset = (transform.right * _bezierAnchorXOffset) + new Vector3(0, _bezierAnchorYOffset, 0);
    }

    private void Update()
    {
        if (!_translating)
        {
            return;
        }
        _lerpTimer += Time.deltaTime * _movementSpeed;
        transform.Rotate(_rotationAxis, Time.deltaTime * _rotationSpeed);
        transform.position = UtilitiesER.GetPointOnBezierCurve
                                (
                                    _startPosition,
                                    _startPosition + (_anchorOffset / 2),
                                    _targetTransform.position + _anchorOffset,
                                    _targetTransform.position,
                                    _lerpTimer
                                );
    }
}
