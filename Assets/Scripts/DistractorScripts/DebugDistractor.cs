using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugDistractor : Distractor
{
    public float _forwardOffsetFromPlayer;
    public ParticleSystem _spawnParticles;
    public float _lifeTime = 5f;
    public float _rotationSpeed = 5f;

    private float _lifeTimer = 0f;

    public override void InitialiseDistractor(RedirectionManagerER redirectionManager)
    {
        base.InitialiseDistractor(redirectionManager);

        transform.position = _redirectionManagerER.headTransform.position + _redirectionManagerER.headTransform.forward * _forwardOffsetFromPlayer;
        _spawnParticles.Play();
    }

    void Update()
    {
        if (!_isPaused)
        {
            _lifeTimer += Time.deltaTime;
            transform.RotateAround(_redirectionManagerER.headTransform.position, Vector3.up, _rotationSpeed * Time.deltaTime);

            if (_lifeTimer >= _lifeTime)
            {
                _redirectionManagerER.OnDistractorEnd();
            }
        }
    }
}
