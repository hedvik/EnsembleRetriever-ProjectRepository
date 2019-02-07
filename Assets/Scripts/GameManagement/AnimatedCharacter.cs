using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;


// Scriptable interface for programmatic animation and movement
public class AnimatedCharacter : MonoBehaviour
{
    public ParticleSystem _teleportParticles;
    public AudioClip _teleportSound;

    private AudioSource _audioSource;
    private Animator _animator;

    private void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        _animator = GetComponent<Animator>();
    }

    public void AnimationTrigger(string trigger)
    {
        _animator.SetTrigger(trigger);
    }

    public void TeleportTo(string position)
    {
        _teleportParticles.Play();
        _audioSource.PlayOneShot(_teleportSound);

        transform.position = StringToVector3(position);
    }

    public void LookTowards(string target)
    {
        // TODO: use coroutine for it
    }

    // https://answers.unity.com/questions/1134997/string-to-vector3.html
    private static Vector3 StringToVector3(string sVector)
    {
        // Remove the parentheses
        if (sVector.StartsWith("(") && sVector.EndsWith(")"))
        {
            sVector = sVector.Substring(1, sVector.Length - 2);
        }

        // split the items
        string[] sArray = sVector.Split(',');

        // store as a Vector3
        Vector3 result = new Vector3(
            float.Parse(sArray[0], CultureInfo.InvariantCulture),
            float.Parse(sArray[1], CultureInfo.InvariantCulture),
            float.Parse(sArray[2], CultureInfo.InvariantCulture)
            );

        return result;
    }
}
