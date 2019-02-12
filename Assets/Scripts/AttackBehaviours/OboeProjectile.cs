using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OboeProjectile : ProjectileAttack
{
    public float _rotationSpeed = 3;
    private Vector3 _rotationAxis;

    // Use this for initialization
    protected override void Start()
    {
        base.Start();
        _rotationAxis = new Vector3(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f)).normalized;
    }

    // Update is called once per frame
    void Update()
    {
        if (_translating)
        {
            transform.Rotate(_rotationAxis, Time.deltaTime * _rotationSpeed);
            transform.position += (_targetTransform.position - transform.position).normalized * Time.deltaTime * _movementSpeed;
        }
    }
}
