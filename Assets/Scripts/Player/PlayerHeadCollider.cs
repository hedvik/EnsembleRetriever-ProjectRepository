using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHeadCollider : MonoBehaviour
{
    private PlayerManager _playerManager;

    private void Start()
    {
        _playerManager = transform.parent.gameObject.GetComponent<PlayerManager>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("projectile"))
        {
            var projectile = other.gameObject.GetComponent<ProjectileAttack>();
            projectile.Destroy();
            _playerManager._audioSource.PlayOneShot(_playerManager._takeDamageSound);
            _playerManager.TakeDamage();
        }
    }
}
