using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Teleporter : MonoBehaviour
{
    public float _timeInsideTeleporterUntilTeleport = 2f;

    public ParticleSystem _onEnterParticles;
    public AudioClip _onEnterAudio;
    public Transform _teleportTargetTransform;

    private GameManager _gameManager;
    private AudioSource _audioSource;
    private bool _playerInTeleporter = false;
    private float _timer = 0f;

    private void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        _gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
    }

    private void Update()
    {
        if (!_playerInTeleporter)
        {
            return;
        }

        _timer += Time.deltaTime;
        if (_timer >= _timeInsideTeleporterUntilTeleport)
        {
            _gameManager._redirectionManager.transform.position = _teleportTargetTransform.position;
        }

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _playerInTeleporter = true;
            _onEnterParticles.Play();
            _audioSource.PlayOneShot(_onEnterAudio, 0.5f);

            // It would not make sense to spawn a distractor while inside the teleporter.
            _gameManager._redirectionManager.SetDistractorUsageState(false);

            // The light thingy should be attached to the player transform until it fades away
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _playerInTeleporter = false;
            _timer = 0f;
            _onEnterParticles.Stop();
            StartCoroutine(EnableDistractors());
        }
    }

    // HACK: Quick fix so that distractors dont trigger on teleport
    private IEnumerator EnableDistractors()
    {
        yield return new WaitForSeconds(1f);
        _gameManager._redirectionManager.SetDistractorUsageState(true);
    }
}
