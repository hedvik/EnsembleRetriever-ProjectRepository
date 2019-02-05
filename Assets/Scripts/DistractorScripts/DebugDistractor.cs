using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugDistractor : Distractor
{
    public float _forwardOffsetFromPlayer;
    public ParticleSystem _spawnParticles;
    public float _lifeTime = 5f;
    public float _rotationSpeed = 5f;

    public override void InitialiseDistractor(RedirectionManagerER redirectionManager)
    {
        base.InitialiseDistractor(redirectionManager);

        transform.position = _redirectionManagerER.headTransform.position + _redirectionManagerER.headTransform.forward * _forwardOffsetFromPlayer;
        _spawnParticles.Play();

        StartCoroutine(RotateAroundPlayer());
    }

    private IEnumerator RotateAroundPlayer()
    {
        var timer = 0f;

        while(timer < _lifeTime)
        {
            if (!_isPaused)
            {
                timer += Time.deltaTime;
                transform.RotateAround(_redirectionManagerER.headTransform.position, Vector3.up, _rotationSpeed * Time.deltaTime);
            }
            yield return null;
        }

        _redirectionManagerER.OnDistractorEnd();
    }
}
