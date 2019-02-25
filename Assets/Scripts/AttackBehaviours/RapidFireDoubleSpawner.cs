using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RapidFireDoubleSpawner : ProjectileAttack
{
    public int _numberOfProjectiles = 5;
    public float _cooldownBetweenShots = 0.5f;
    public Transform _spawnPositionA;
    public Transform _spawnPositionB;
    public GameObject _projectilePrefab;
    public float _spawnSpeed = 5f;

    private AudioSource _audioSource;
    private float _timer = 0f;
    private int _numberOfShotProjectiles = 0;
    private float _spawnAnimationTimer = 0f;
    private float _projectileSpeedMultiplier;

    public override void Initialise(EnemyAttack attack, Transform target, float speedMultiplier)
    {
        base.Initialise(attack, target, speedMultiplier);
        transform.LookAt(target, Vector3.up);
        _audioSource = GetComponent<AudioSource>();
        _projectileSpeedMultiplier = speedMultiplier;
    }

    private void Update()
    {
        if(!_translating)
        {
            return;
        }
        _spawnAnimationTimer += Time.deltaTime * _spawnSpeed;
        _spawnPositionA.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, _spawnAnimationTimer);
        _spawnPositionB.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, _spawnAnimationTimer);

        _timer += Time.deltaTime;

        if(_timer >= _cooldownBetweenShots)
        {
            _timer -= _cooldownBetweenShots;
            _numberOfShotProjectiles++;

            var newProjectileObject = Instantiate(_projectilePrefab, _numberOfShotProjectiles % 2 == 0 ? _spawnPositionA.position : _spawnPositionB.position, Quaternion.identity);
            var projectileSettings = newProjectileObject.GetComponent<BasicProjectile>();
            projectileSettings.Initialise(_attackSettings, _targetTransform, _projectileSpeedMultiplier);
            _audioSource.PlayOneShot(_attackSettings._spawnAudio, _attackSettings._spawnAudioScale);
        }

        if(_numberOfShotProjectiles > _numberOfProjectiles)
        {
            Destroy();
        }
    }
}
