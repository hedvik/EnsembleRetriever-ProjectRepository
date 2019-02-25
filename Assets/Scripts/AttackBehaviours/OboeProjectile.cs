using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OboeProjectile : ProjectileAttack
{
    public float _rotationSpeed = 3;
    public float _movementScaleAfterRotation = 0.1f;
    private Vector3 _rotationAxis;
    private Vector3 _startPosition;
    private bool _targetReached = false;
    private Vector3 _orbitAxis;

    public override void Initialise(EnemyAttack attack, Transform target, float speedMultiplier)
    {
        base.Initialise(attack, target, speedMultiplier);
        _rotationAxis = new Vector3(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f)).normalized;
        _startPosition = transform.position;
        _orbitAxis = Vector3.Cross(_targetTransform.position - transform.position, Vector3.up);
    }

    void Update()
    {
        if (_translating)
        {
            transform.Rotate(_rotationAxis, Time.deltaTime * _rotationSpeed);
            if(_targetReached)
            {
                MoveTowardsPlayer();
            }
            else
            {
                MoveBehindPlayer();
            }
        }
    }

    private void MoveBehindPlayer()
    {
        transform.position = UtilitiesER.RotatePointAroundPivot(transform.position, _targetTransform.position, Quaternion.AngleAxis(-Time.deltaTime * _movementSpeed, _orbitAxis));

        if(transform.position.y < _startPosition.y)
        {
            _targetReached = true;
            _movementSpeed *= _movementScaleAfterRotation;
        }
    }

    private void MoveTowardsPlayer()
    {
        transform.position += (_targetTransform.position - transform.position).normalized * Time.deltaTime * _movementSpeed;
    }
}
