﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerShield : MonoBehaviour
{
    public float _absorbptionSoundScale = 1.25f;

    private GameManager _gameManager;
    private PlayerManager _playerManager;
    private ParticleSystem _particleSystem;

    private void Start()
    {
        _gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        _playerManager = _gameManager.GetCurrentPlayerManager();
        _particleSystem = transform.parent.GetComponentInChildren<ParticleSystem>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("projectile"))
        {
            var projectile = other.gameObject.GetComponent<ProjectileAttack>();
            _playerManager.AddCharge(projectile._chargeValue);
            projectile.Destroy();

            _particleSystem.transform.position = other.transform.position;
            _particleSystem.Play();
            _playerManager._audioSource.PlayOneShot(_playerManager._absorbSound, _absorbptionSoundScale);

            if(!_gameManager._gameStarted)
            {
                _gameManager.ShieldEventTriggerDialogue();
            }
        }
    }
}
