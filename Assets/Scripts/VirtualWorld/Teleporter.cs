using Redirection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Teleporter : MonoBehaviour
{
    public float _timeInsideTeleporterUntilTeleport = 2f;

    public ParticleSystem _onEnterParticles;
    public AudioClip _onEnterAudio;
    public Transform _teleportTargetTransform;

    // Used to align the physical space with where you want the player to go after teleporting
    public Transform _alignmentTargetOnTeleport;

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

            // Whenever the player is teleported, they are reoriented so the path to the centre is aligned with the future direction they are expected to go.
            // For this game in particular, it would be preferable to not fight the mountain king at the  
            // physical room edge so the player needs to walk a little bit after the teleport to get closer to the middle. 
            var centreToHead = _gameManager._redirectionManager._centreToHead;
            var headToTarget = Utilities.FlattenedDir3D(_alignmentTargetOnTeleport.position - _gameManager._redirectionManager.GetUserHeadTransform().position);
            var angleBetweenVectors = Vector3.Angle(headToTarget, centreToHead);
            _gameManager._redirectionManager.transform.rotation = Quaternion.AngleAxis(180 - angleBetweenVectors, Vector3.up);

            _playerInTeleporter = false;
            _timer = 0f;
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

            // TODO: The teleport light should be attached to the player transform until it fades away
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _playerInTeleporter = false;
            _timer = 0f;
            _onEnterParticles.Stop();
            _gameManager._redirectionManager.SetDistractorUsageState(true);
        }
    }
}
