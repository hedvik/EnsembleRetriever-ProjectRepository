using UnityEngine;
using System.Collections;
using Redirection;

/// <summary>
/// SteerToRedirector.cs which originates from Azmandian et al. but with some minor changes to facilitate data collection.
/// </summary>
public abstract class SteerToRedirectorER : Redirector
{
    // Testing Parameters
    private bool _useBearingThresholdBasedRotationDampeningTimofey = true;
    private bool _dontUseDampening = false;

    // User Experience Improvement Parameters
    private const float _MOVEMENT_THRESHOLD = 0.2f; // meters per second

    [HideInInspector]
    public float _rotationThreshold = 1.5f; // degrees per second. This value should only be used when dampening is enabled, otherwise it should be the same as AC2F for parity. 
    // public float _ROTATION_THRESHOLD = 12.5f;
    private const float _CURVATURE_GAIN_CAP_DEGREES_PER_SECOND = 15;  // degrees per second
    private const float _ROTATION_GAIN_CAP_DEGREES_PER_SECOND = 30;  // degrees per second
    private const float _DISTANCE_THRESHOLD_FOR_DAMPENING = 1.25f; // Distance threshold to apply dampening (meters)
    private const float _BEARING_THRESHOLD_FOR_DAMPENING = 45f; // TIMOFEY: 45.0f; // Bearing threshold to apply dampening (degrees) MAHDI: WHERE DID THIS VALUE COME FROM?
    private const float _SMOOTHING_FACTOR = 0.125f; // Smoothing factor for redirection rotations

    // Reference Parameters
    protected Transform _currentTarget; // Where the participant  is currently directed?
    protected GameObject _temporaryTarget;
    protected RedirectionManagerER _redirectionManagerER;

    // State Parameters
    protected bool _noTemporaryTarget = true;

    // Auxiliary Parameters
    private float _rotationFromCurvatureGain; // Proposed curvature gain based on user speed
    private float _rotationFromRotationGain; // Proposed rotation gain based on head's yaw

    [HideInInspector]
    public float _lastRotationApplied = 0f;

    public abstract void PickRedirectionTarget();

    private void Start()
    {
        _redirectionManagerER = redirectionManager as RedirectionManagerER;
    }

    public override void ApplyRedirection()
    {
        PickRedirectionTarget();

        // Get Required Data
        var deltaPos = redirectionManager.deltaPos;
        var deltaDir = redirectionManager.deltaDir;

        _rotationFromCurvatureGain = 0;

        // User is moving
        if (deltaPos.magnitude / redirectionManager.GetDeltaTime() > _MOVEMENT_THRESHOLD) 
        {
            _rotationFromCurvatureGain = Mathf.Rad2Deg * (deltaPos.magnitude / redirectionManager.CURVATURE_RADIUS);
            _rotationFromCurvatureGain = Mathf.Min(_rotationFromCurvatureGain, _CURVATURE_GAIN_CAP_DEGREES_PER_SECOND * redirectionManager.GetDeltaTime());
        }

        // Compute desired facing vector for redirection
        var desiredFacingDirection = Utilities.FlattenedPos3D(_currentTarget.position) - redirectionManager.currPos;

        // We have to steer to the opposite direction so when the user counters this steering, she steers in right direction
        var desiredSteeringDirection = (-1) * (int)Mathf.Sign(Utilities.GetSignedAngle(redirectionManager.currDir, desiredFacingDirection)); 

        // Compute proposed rotation gain
        _rotationFromRotationGain = 0;

        var currentRotationGainType = RecordedGainTypes.none;
        // If user is rotating
        if (Mathf.Abs(deltaDir) / redirectionManager.GetDeltaTime() >= _rotationThreshold)  
        {
            // Determine if we need to rotate with or against the user
            if (deltaDir * desiredSteeringDirection < 0)
            {
                // Rotating against the user
                _rotationFromRotationGain = Mathf.Min(Mathf.Abs(deltaDir * redirectionManager.MIN_ROT_GAIN), _ROTATION_GAIN_CAP_DEGREES_PER_SECOND * redirectionManager.GetDeltaTime());
                currentRotationGainType = RecordedGainTypes.rotationAgainstHead;
            }
            else
            {
                // Rotating with the user
                _rotationFromRotationGain = Mathf.Min(Mathf.Abs(deltaDir * redirectionManager.MAX_ROT_GAIN), _ROTATION_GAIN_CAP_DEGREES_PER_SECOND * redirectionManager.GetDeltaTime());
                currentRotationGainType = RecordedGainTypes.rotationWithHead;
            }
        }

        // Note: This means that one of the gains is chosen to be used this frame. They are not combined (which is nice for keeping track of things)
        var rotationProposed = desiredSteeringDirection * Mathf.Max(_rotationFromRotationGain, _rotationFromCurvatureGain);
        var curvatureGainUsed = _rotationFromCurvatureGain > _rotationFromRotationGain;

        // Prevent having gains if user is stationary. To clarify: if the user has not translated and rotated
        if (Mathf.Approximately(rotationProposed, 0))
        {
            _currentlyAppliedGainType = RecordedGainTypes.none;
            return;
        }

        if (!_dontUseDampening)
        {
            // DAMPENING METHODS
            // MAHDI: Sinusiodally scaling the rotation when the bearing is near zero
            var bearingToTarget = Vector3.Angle(redirectionManager.currDir, desiredFacingDirection);
            if (_useBearingThresholdBasedRotationDampeningTimofey)
            {
                // TIMOFEY
                if (bearingToTarget <= _BEARING_THRESHOLD_FOR_DAMPENING)
                    rotationProposed *= Mathf.Sin(Mathf.Deg2Rad * 90 * bearingToTarget / _BEARING_THRESHOLD_FOR_DAMPENING);
            }
            else
            {
                // MAHDI
                // The algorithm first is explained to be similar to above but at the end it is explained like this. Also the BEARING_THRESHOLD_FOR_DAMPENING value was never mentioned which make me want to use the following even more.
                rotationProposed *= Mathf.Sin(Mathf.Deg2Rad * bearingToTarget);
            }

            // MAHDI: Linearly scaling the rotation when the distance is near zero
            if (desiredFacingDirection.magnitude <= _DISTANCE_THRESHOLD_FOR_DAMPENING)
            {
                rotationProposed *= desiredFacingDirection.magnitude / _DISTANCE_THRESHOLD_FOR_DAMPENING;
            }
        }

        // Implement additional rotation with smoothing
        float finalRotation = (1.0f - _SMOOTHING_FACTOR) * _lastRotationApplied + _SMOOTHING_FACTOR * rotationProposed;
        _lastRotationApplied = finalRotation;
        if (!curvatureGainUsed)
        {
            InjectRotation(finalRotation);
            _currentlyAppliedGainType = currentRotationGainType;
        }
        else
        {
            InjectCurvature(finalRotation);
            _currentlyAppliedGainType = RecordedGainTypes.curvature;
        }
    }

    public void DisableDampening()
    {
        _dontUseDampening = true;
    }
}
