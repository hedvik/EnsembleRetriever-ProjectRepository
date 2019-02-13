using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Redirection;

public enum RotationGainTypes {none = -1, with, against };

/// <summary>
/// Align Centre To Future Redirector.
/// Based on what Peck et al. described for their modified S2C algorithm.
/// This redirector makes use of rotation gains to attempt aligning the future virtual walking direction through the centre of the tracking space.
/// </summary>
public class AC2FRedirector : Redirector
{
    public bool _superSmoothingEnabled = true;
    public float _superSmoothSpeed = 0.5f;

    [HideInInspector]
    public float _lastRotationApplied = 0f;

    // Reference Parameters
    protected RedirectionManagerER _redirectionManagerER;

    // User Experience Improvement Parameters
    private const float _ROTATION_THRESHOLD = 1.5f; // degrees per second
    private const float _ROTATION_GAIN_CAP_DEGREES_PER_SECOND = 30;  // degrees per second
    private const float _SMOOTHING_FACTOR = 0.125f; // Smoothing factor for redirection rotations

    // Auxiliary Parameters
    private float _rotationFromRotationGain; // Proposed rotation gain based on head's yaw

    private bool _transitioningBetweenGains = false;
    private RotationGainTypes _previousRotationGainType;
    private float _smoothedRotation = 0f;
    private float _lerpTimer = 0f;

    private bool _isAligned = false;

    private void Start()
    {
        _redirectionManagerER = redirectionManager as RedirectionManagerER;
    }

    public void OnRedirectionMethodSwitch()
    {
        _previousRotationGainType = RotationGainTypes.none;
        _isAligned = false;
    }

    public override void ApplyRedirection()
    {
        if(_transitioningBetweenGains && _lerpTimer >= 1f)
        {
            _transitioningBetweenGains = false;
            //Debug.Log("Dampening done!");
        }

        // Get Required Data
        var deltaDir = redirectionManager.deltaDir;

        _rotationFromRotationGain = 0;

        // The steering direction is used to determine whether rotations are clockwise or counter clockwise.
        var desiredSteeringDirection = (-1) * (int)Mathf.Sign(Utilities.GetSignedAngle(_redirectionManagerER._centreToHead, _redirectionManagerER._futureVirtualWalkingDirection));

        var currentGainType = RotationGainTypes.none;
        // If user is rotating
        if (Mathf.Abs(deltaDir) / Time.deltaTime >= _ROTATION_THRESHOLD)
        {
            // Calculate gains
            var againstGain = deltaDir * redirectionManager.MIN_ROT_GAIN;
            var withGain = deltaDir * redirectionManager.MAX_ROT_GAIN;

            // The resulting dot products from applying gains to the vector from the centre of the physical space to the user head
            var dotFromAgainst = Vector3.Dot(Quaternion.AngleAxis(againstGain, Vector3.up) * _redirectionManagerER._centreToHead, _redirectionManagerER._futureVirtualWalkingDirection);
            var dotFromWith = Vector3.Dot(Quaternion.AngleAxis(withGain, Vector3.up) * _redirectionManagerER._centreToHead, _redirectionManagerER._futureVirtualWalkingDirection);

            // The the gain that provides the closest dot product to the target is chosen.
            // The target in this case is aligning the future virtual direction with (trackingSpaceCentre - headPosition)
            if (dotFromAgainst < dotFromWith)
            {
                _rotationFromRotationGain = Mathf.Min(Mathf.Abs(deltaDir * redirectionManager.MIN_ROT_GAIN), _ROTATION_GAIN_CAP_DEGREES_PER_SECOND * redirectionManager.GetDeltaTime());
                currentGainType = RotationGainTypes.against;
            }
            else
            {
                _rotationFromRotationGain = Mathf.Min(Mathf.Abs(deltaDir * redirectionManager.MAX_ROT_GAIN), _ROTATION_GAIN_CAP_DEGREES_PER_SECOND * redirectionManager.GetDeltaTime());
                currentGainType = RotationGainTypes.with;
            }
        }

        var rotationProposed = desiredSteeringDirection * _rotationFromRotationGain;

        // Prevent having gains if user has not moved their head
        if (Mathf.Approximately(rotationProposed, 0))
        {
            return;
        }

        CheckForGainDifference(currentGainType, rotationProposed, deltaDir);

        if (_superSmoothingEnabled && _transitioningBetweenGains)
        {
            _lerpTimer += Time.deltaTime;

            // Whenever the gain type changes, we smoothly interpolate from the injected rotation at the time of changing towards the current one
            _smoothedRotation = SuperSmoothLerp(_smoothedRotation, _lastRotationApplied, rotationProposed, _lerpTimer, _superSmoothSpeed);
            //Debug.Log(_smoothedRotation);
        }
        else
        {
            _smoothedRotation = rotationProposed;
        }
        
        _lastRotationApplied = _smoothedRotation;
        _previousRotationGainType = currentGainType;
        InjectRotation(_smoothedRotation);
    }

    private void CheckForGainDifference(RotationGainTypes newGain, float rotationProposed, float deltaDir)
    {
        // The approximately check is used to make sure that we dont transition between gains if they have been set to 0
        if(newGain != RotationGainTypes.none && newGain != _previousRotationGainType && !Mathf.Approximately(rotationProposed, deltaDir))
        {
            _transitioningBetweenGains = true;
            _lerpTimer = 0f;
        }
    }

    // This function is mostly relevant during the distractor active period. 
    // In particular to smooth out gain changes when alignment is finished. 
    public void DisableGains()
    {
        if (!_isAligned)
        {
            _isAligned = true;
            _redirectionManagerER.MAX_ROT_GAIN = 0;
            _redirectionManagerER.MIN_ROT_GAIN = 0;
            _transitioningBetweenGains = true;
            _lerpTimer = 0f;
        }
    }

    // https://forum.unity.com/threads/how-to-smooth-damp-towards-a-moving-target-without-causing-jitter-in-the-movement.130920/
    // Smoothing towards a moving target
    private float SuperSmoothLerp(float followerOld, float targetOld, float targetNew, float t, float speed)
    {
        var f = followerOld - targetOld + (targetNew - targetOld) / (speed * t);
        return targetNew - (targetNew - targetOld) / (speed * t) + f * Mathf.Exp(-speed * t);
    }
}
