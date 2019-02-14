using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// <summary>
/// "Scriptable"(from dialogue snippets) interface for programmatic animation and movement.
/// Also contains some tutorial related functionality.
/// </summary>
public class AnimatedCharacterInterface : Pausable
{
    public float _movementSpeed = 5f;
    public ParticleSystem _teleportParticles;
    public ParticleSystem _sweatParticles;
    public AudioClip _teleportSound;
    public AudioClip _groundCrashSound;
    public AudioClip _deathExplosionSound;
    public AudioClip _phaseTransitionSound;

    [HideInInspector]
    public AudioSource _audioSource;

    private Animator _animator;
    private RedirectionManagerER _redirectionManager;
    private BoxCollider _collider;
    private TrailRenderer _trailRenderer;

    private string _currentAnimation = null;
    private Dictionary<string, System.Action> _onAnimationEndCallbacks = new Dictionary<string, System.Action>();


    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _animator = GetComponent<Animator>();
        _redirectionManager = GameObject.FindGameObjectWithTag("RedirectionManager").GetComponent<RedirectionManagerER>();
        _collider = GetComponent<BoxCollider>();
        _trailRenderer = GetComponent<TrailRenderer>();
    }

    protected override void PauseStateChange()
    {
        _animator.enabled = !_isPaused;
    }

    public void CleanCallbacks()
    {
        _onAnimationEndCallbacks.Clear();
    }

    public void AnimationTrigger(string trigger)
    {
        if (_currentAnimation == trigger)
            return;

        if(_currentAnimation != null)
        {
            _animator.ResetTrigger(_currentAnimation);
        }
        _animator.SetTrigger(trigger);
        _currentAnimation = trigger;
    }

    public void AnimationTriggerWithCallback(string trigger, System.Action callback)
    {
        AnimationTrigger(trigger);
        _onAnimationEndCallbacks.Add(trigger, callback);
    }

    public void OnAnimationEnd(string trigger)
    {
        _onAnimationEndCallbacks[trigger]?.Invoke();
        _onAnimationEndCallbacks.Remove(trigger);
    }

    public void TeleportToStringPosition(string position)
    {
        _trailRenderer.enabled = false;

        _teleportParticles.Play();
        _audioSource.PlayOneShot(_teleportSound);

        transform.position = StringToVector3(position);

        _trailRenderer.enabled = true;
    }

    public void TeleportToPosition(Vector3 position)
    {
        _trailRenderer.enabled = false;

        _teleportParticles.Play();
        _audioSource.PlayOneShot(_teleportSound);

        transform.position = position;

        _trailRenderer.enabled = true;
    }

    public void LookAtStringPosition(string target)
    {
        StartCoroutine(LookTowardsPosition(StringToVector3(target)));
    }

    public void LookAtPosition(Vector3 target)
    {
        StartCoroutine(LookTowardsPosition(target));
    }

    public void MoveByStringVector(string vector)
    {
        StartCoroutine(MoveToPosition(transform.position + StringToVector3(vector)));
    }

    public void StartGame()
    {
        _redirectionManager._gameManager.StartGame();
    }

    public void StartTutorialAttacks()
    {
        var tutorialNPC = GetComponent<TutorialNPC>();
        tutorialNPC.InitialiseDistractor(_redirectionManager);
        tutorialNPC.StartTutorialAttacks();
    }

    public void RotateAroundPivot(float angleToRotate, Vector3 pivot)
    {
        StartCoroutine(RotateAroundPivotAnimation(angleToRotate, pivot));
    }

    public void TakeDamageAnimation(string fallingAnimationTrigger, string onGroundAnimationTrigger, float fallSpeed, System.Action callbackOnFinish, bool returnToStartPositionAfterEnd)
    {
        StartCoroutine(FallToFloorAnimation(fallingAnimationTrigger, onGroundAnimationTrigger, fallSpeed, callbackOnFinish, returnToStartPositionAfterEnd));
    }

    public void PlayDeathExplosionSound()
    {
        _audioSource.PlayOneShot(_deathExplosionSound, 1f);
    }

    public void PlayPhaseTransitionSound()
    {
        _audioSource.PlayOneShot(_phaseTransitionSound, 1f);
    }

    public void SetSweatState(bool state)
    {
        _sweatParticles.gameObject.SetActive(state);
    }

    private IEnumerator FallToFloorAnimation(string fallingAnimationTrigger, string onGroundAnimationTrigger, float fallSpeed, System.Action callbackOnFinish, bool returnToStartPositionAfterEnd)
    {
        _animator.SetFloat("speed", fallSpeed);

        // It is necessary to find the y value of the position that should be fallen to.
        // Layer 9 contains the virtual environment
        var positionToFallTowards = transform.position;
        var layerBitMask = 1 << 9;

        // Avoiding to raycast the collider of this GameObject
        _collider.enabled = false;
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 50, layerBitMask))
        {
            positionToFallTowards.y = hit.point.y + 0.75f;
        }
        _collider.enabled = true;

        // A small bounce at the start of the fall animation is given so the interpolation "starts" at a later stage in the animation before falling using a sine wave
        AnimationTrigger(fallingAnimationTrigger);
        var basePosition = transform.position;
        var offsetBasePosition = basePosition;
        offsetBasePosition.y += 1;
        // InverseLerp calculates the t parameter of the current position
        var positionLerpTimer = Mathf.InverseLerp(positionToFallTowards.y, offsetBasePosition.y, basePosition.y);
        while (Mathf.Sin(positionLerpTimer) > 0.0f)
        {
            positionLerpTimer += (Time.deltaTime * fallSpeed);
            transform.position = Vector3.Lerp(positionToFallTowards, offsetBasePosition, Mathf.Sin(positionLerpTimer));
            yield return null;
        }

        // As the boss faces down the position has to go down a bit as well so it looks like it is on the floor
        AnimationTrigger(onGroundAnimationTrigger);
        transform.position += Vector3.down * 0.5f;
        _audioSource.PlayOneShot(_groundCrashSound);

        // HACK: Could have waited for the length of the animation somehow, but that is tedious to find
        yield return new WaitForSeconds(3);

        _animator.SetFloat("speed", 1f);

        if (returnToStartPositionAfterEnd)
        {
            TeleportToPosition(basePosition);
            AnimationTrigger("Idle");
        }

        callbackOnFinish?.Invoke();
    }

    private IEnumerator MoveToPosition(Vector3 position)
    {
        var startPosition = transform.position;
        var lerpTimer = 0f;

        while(lerpTimer <= 1)
        {
            lerpTimer += Time.deltaTime * _movementSpeed;
            transform.position = Vector3.Lerp(startPosition, position, lerpTimer);
            yield return null;
        }
    }

    private IEnumerator LookTowardsPosition(Vector3 target)
    {
        var oldRotation = transform.rotation;
        transform.LookAt(target);
        var newRotation = transform.rotation;

        var lerpTimer = 0f;
        while(lerpTimer <= 1)
        {
            lerpTimer += Time.deltaTime * _movementSpeed;

            transform.rotation = Quaternion.Lerp(oldRotation, newRotation, lerpTimer);

            yield return null;
        }
    }

    private IEnumerator RotateAroundPivotAnimation(float angleToRotate, Vector3 pivot)
    {
        var currentlyRotatedAngle = 0f;
        var finishedRotating = false;
        while(!finishedRotating)
        {
            var deltaAngle = Time.deltaTime * _movementSpeed * Mathf.Sign(angleToRotate);
            transform.position = UtilitiesER.RotatePointAroundPivot(transform.position, pivot, Quaternion.AngleAxis(deltaAngle, Vector3.up));
            currentlyRotatedAngle += deltaAngle;
            transform.LookAt(pivot);

            if(angleToRotate > 0 && currentlyRotatedAngle >= angleToRotate)
            {
                finishedRotating = true;
            }
            else if(angleToRotate < 0 && currentlyRotatedAngle <= angleToRotate)
            {
                finishedRotating = true;
            }

            yield return null;
        }
    }

    #region Utilities
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
    #endregion
}
