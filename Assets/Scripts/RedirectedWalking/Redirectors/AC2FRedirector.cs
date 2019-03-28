using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Redirection;

public enum RotationGainTypes { none = -1, against, with };

/// <summary>
/// Align Centre To Future Redirector.
/// Based on what Peck et al. mentioned for their modified S2C algorithm.
/// 
/// This redirector makes use of rotation gains to attempt aligning the 
/// future virtual walking direction through the centre of the physical tracking space.
/// </summary>
public class AC2FRedirector : Redirector
{
    public bool _superSmoothingEnabled = true;
    public float _superSmoothSpeed = 0.5f;

    [HideInInspector]
    public float _lastRotationApplied = 0f;

    protected RedirectionManagerER _redirectionManagerER;

    // A rotation has to exceed this threshold in degrees per second for gains to be applied
    [HideInInspector]
    public float _rotationThreshold = 12.5f;
    
    // Capping value for large head movements. Used in the same way as Azmandian et al.'s S2C implementation.
    // TODO: This might need to be higher for experiment 1.
    private const float _ROTATION_GAIN_CAP_DEGREES_PER_SECOND = 30;
   
    // Smoothing factor for Azmandian et al.'s smoothing method
    private const float SMOOTHING_FACTOR = 0.125f;

    private float _rotationFromRotationGain;

    private bool _transitioningBetweenGains = false;
    private RotationGainTypes _previousRotationGainType;
    private float _smoothedRotation = 0f;
    private float _lerpTimer = 0f;

    private bool _isAligned = false;

    private void Start()
    {
        _redirectionManagerER = redirectionManager as RedirectionManagerER;
    }

    /// <summary>
    /// Called as a initialisation function whenever this redirector is activated again. 
    /// </summary>
    public void OnRedirectionMethodSwitch()
    {
        _previousRotationGainType = RotationGainTypes.none;
        _isAligned = false;
    }

    /// <summary>
    /// Main function for calculating and applying camera injection angle. 
    /// </summary>
    public override void ApplyRedirection()
    {
        // If the SteamVR menu is entered or tracking somehow is lost, it will break the 
        // smoothing algorithm and turn _smoothedRotation and _lastRotationApplied into NaNs. 
        // This has to be reset if so.
        if(float.IsNaN(_smoothedRotation) || float.IsNaN(_lastRotationApplied))
        {
            _smoothedRotation = 0;
            _lastRotationApplied = 0;
        }

        if (_transitioningBetweenGains && _lerpTimer >= 1f)
        {
            _transitioningBetweenGains = false;
            //Debug.Log("Dampening done!");
        }

        var deltaDir = redirectionManager.deltaDir;

        _rotationFromRotationGain = 0;

        // The steering direction is used to determine whether rotations are clockwise or counter clockwise.
        // In this case it can be considered the desired way we want to shift the physical space. 
        var desiredSteeringDirection = (-1) * (int)Mathf.Sign(Utilities.GetSignedAngle(_redirectionManagerER._centreToHead, _redirectionManagerER._futureVirtualWalkingDirection));

        var currentGainType = RotationGainTypes.none;
        // If user is rotating above the threshold
        if (Mathf.Abs(deltaDir) / Time.deltaTime >= _rotationThreshold)
        {
            // Calculate gains
            var againstGain = deltaDir * redirectionManager.MIN_ROT_GAIN;
            var withGain = deltaDir * redirectionManager.MAX_ROT_GAIN;

            // The resulting dot products from applying gains to the vector from the centre of the physical space to the user head
            var dotFromAgainst = Vector3.Dot(Quaternion.AngleAxis(againstGain, Vector3.up) * _redirectionManagerER._centreToHead, _redirectionManagerER._futureVirtualWalkingDirection);
            var dotFromWith = Vector3.Dot(Quaternion.AngleAxis(withGain, Vector3.up) * _redirectionManagerER._centreToHead, _redirectionManagerER._futureVirtualWalkingDirection);

            // The the gain that provides the closest dot product to the target is chosen.
            // The target in this case is aligning the future virtual direction with a vector from the tracking centre to the user's head. 
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
        // If the head movement is below the threshold for applying gains, the smoothing function will move back towards natural head rotation.
        // This helps with one particular edge case:
        // After a head movement has finished, the user's head will slightly bob in the opposite direction,
        // This small bob is usually below the threshold for applying gains which can result in a somewhat
        // jarring difference between just having used gains to not using them. 
        // By allowing the smoothing function to smooth back to natural head rotation we avoid this issue.

        var rotationProposed = desiredSteeringDirection * _rotationFromRotationGain;
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
            // Azmandian et al.'s smoothing method 
            _smoothedRotation = (1.0f - SMOOTHING_FACTOR) * _lastRotationApplied + SMOOTHING_FACTOR * rotationProposed;
        }

        _lastRotationApplied = _smoothedRotation;
        _previousRotationGainType = currentGainType;
        InjectRotation(_smoothedRotation);

        _currentlyAppliedGainType = _isAligned ? RecordedGainTypes.none : (RecordedGainTypes)currentGainType;
    }

    /// <summary>
    /// Utility function for checking whether the algorithm wants to apply the opposite type of rotation gain.
    /// If so, smoothing is activated to improve the transition. 
    /// </summary>
    /// <param name="newGain">The new type of rotation gain we want to apply this frame.</param>
    /// <param name="rotationProposed">The proposed camera angle injection</param>
    /// <param name="deltaDir">Just a straight copy of redirectionManager's deltaDir</param>
    private void CheckForGainDifference(RotationGainTypes newGain, float rotationProposed, float deltaDir)
    {
        // The approximately check is used to make sure that we dont transition between gains if they have been set to 0
        if (newGain != RotationGainTypes.none && newGain != _previousRotationGainType && !Mathf.Approximately(rotationProposed, deltaDir))
        {
            _transitioningBetweenGains = true;
            _lerpTimer = 0f;
        }
    }

    /// <summary>
    /// Called when alignment is complete to disable gains and activating smoothing towards natural head rotation.
    /// </summary>
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

    /// <summary>
    /// Highly smooth interpolation function for moving targets. 
    /// I'd recommend checking out the source for the function if extra documentation is necessary: https://forum.unity.com/threads/how-to-smooth-damp-towards-a-moving-target-without-causing-jitter-in-the-movement.130920/
    /// </summary>
    /// <returns>An interpolated value.</returns>
    private float SuperSmoothLerp(float followerOld, float targetOld, float targetNew, float t, float speed)
    {
        var f = followerOld - targetOld + (targetNew - targetOld) / (speed * t);
        return targetNew - (targetNew - targetOld) / (speed * t) + f * Mathf.Exp(-speed * t);
    }
}
