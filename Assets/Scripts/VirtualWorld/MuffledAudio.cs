using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MuffledAudio : MonoBehaviour
{
    private AudioSource _audioSource;
    private AudioLowPassFilter _lowPassFilter;

    // Start is called before the first frame update
    void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        _lowPassFilter = GetComponent<AudioLowPassFilter>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            _audioSource.Play();
        }
    }
}
