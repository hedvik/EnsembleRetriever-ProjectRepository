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

    private CaveQuizManager _quizManager;

    private void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        _gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        _quizManager = GameObject.Find("CaveQuizManager").GetComponent<CaveQuizManager>();
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
            // This way the teleporting particles stay with the player as they are teleporting to create a more natural transition. 
            _onEnterParticles.transform.parent = _gameManager._redirectionManager.body;
            _gameManager._redirectionManager.transform.position = _teleportTargetTransform.position;

            // Whenever the player is teleported, they are reoriented so the path to the centre is aligned with the future direction they are expected to go.
            // For this game in particular, it would be preferable to not fight the mountain king at the physical room edge 
            // so the player needs to walk a little bit inwards into the cave after the teleport to get closer to the middle. 
            var centreToHead = _gameManager._redirectionManager._centreToHead;
            var headToTarget = Utilities.FlattenedDir3D(_alignmentTargetOnTeleport.position - _gameManager._redirectionManager.GetUserHeadTransform().position);
            var angleBetweenVectors = Vector3.Angle(headToTarget, centreToHead);
            _gameManager._redirectionManager.transform.rotation = Quaternion.AngleAxis(180 - angleBetweenVectors, Vector3.up);

            // At this stage in the game, there is no point in redirecting anymore.
            _gameManager._redirectionManager.MAX_ROT_GAIN = 0;
            _gameManager._redirectionManager.MIN_ROT_GAIN = 0;

            _playerInTeleporter = false;
            _timer = 0f;
            _onEnterParticles.Stop();

            _gameManager.GetCurrentPlayerManager().GetComponentInChildren<ObjectivePointer>().Disable();

            _quizManager.SetVisibilityState(true);
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
            if(_gameManager._redirectionManager._distractorIsActive)
            {
                _gameManager._redirectionManager.OnDistractorEnd();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _playerInTeleporter = false;
            _timer = 0f;
            _onEnterParticles.Stop();
        }
    }
}
