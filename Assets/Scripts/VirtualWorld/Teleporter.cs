using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Teleporter : MonoBehaviour
{
    public ParticleSystem _onEnterParticles;
    public AudioClip _onEnterAudio;
    public GameManager _gameManager;

    private AudioSource _audioSource;

    private void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        _gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            _onEnterParticles.Play();
            _audioSource.PlayOneShot(_onEnterAudio, 0.5f);

            // It would not make sense to spawn a distractor while inside the teleporter.
            _gameManager._redirectionManager.SetDistractorUsageState(false);

            // Teleport and reorient accordingly after 1-2 secs
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _gameManager._redirectionManager.SetDistractorUsageState(true);
            _onEnterParticles.Stop();
        }
    }
}
