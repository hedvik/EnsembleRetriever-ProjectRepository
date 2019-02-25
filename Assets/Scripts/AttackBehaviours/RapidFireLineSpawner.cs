using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RapidFireLineSpawner : ProjectileAttack
{
    public int _numberOfProjectiles = 5;
    public float _cooldownBetweenShots = 0.5f;
    public Transform _spawnerTransform;
    public GameObject _projectilePrefab;

    private AudioSource _audioSource;
    private float _timer = 0f;
    private float _spawnAnimationTimer = 0f;
    private float _spawnAnimationSpeed = 5f;
    private float _movementTimer = 0f;
    private int _numberOfShotProjectiles = 0;
    private Vector3 _startPosition;
    private float _projectileSpeedMultiplier = 1f;

    public override void Initialise(EnemyAttack attack, Transform target, float speedMultiplier)
    {
        base.Initialise(attack, target, speedMultiplier);
        transform.LookAt(target, Vector3.up);
        _audioSource = GetComponent<AudioSource>();
        _startPosition = _spawnerTransform.localPosition;
        _projectileSpeedMultiplier = speedMultiplier;
    }

    private void Update()
    {
        if(!_translating)
        {
            return;
        }
        _spawnAnimationTimer += Time.deltaTime * _spawnAnimationSpeed;
        _spawnerTransform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, _spawnAnimationTimer);

        _movementTimer += Time.deltaTime;
        _spawnerTransform.localPosition = Vector3.Lerp(_startPosition, _startPosition - Vector3.right * _startPosition.x * 2, UtilitiesER.Remap(0, _cooldownBetweenShots * (_numberOfProjectiles + 1), 0, 1, _movementTimer));

        _timer += Time.deltaTime;
        if (_timer >= _cooldownBetweenShots)
        {
            _timer -= _cooldownBetweenShots;
            _numberOfShotProjectiles++;

            var newProjectileObject = Instantiate(_projectilePrefab, _spawnerTransform.position, Quaternion.identity);
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
