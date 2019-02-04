using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Redirection;

/// <summary>
/// Align Centre To Future Redirector.
/// Based on what Peck et al. described for their modified S2C algorithm.
/// This redirector makes use of rotation gains to attempt aligning the future virtual walking direction through the centre of the tracking space. 
/// </summary>
public class AC2FRedirector : Redirector
{
    // User Experience Improvement Parameters
    private const float _ROTATION_THRESHOLD = 1.5f; // degrees per second
    private const float _ROTATION_GAIN_CAP_DEGREES_PER_SECOND = 30;  // degrees per second
    private const float _SMOOTHING_FACTOR = 0.125f; // Smoothing factor for redirection rotations

    // Reference Parameters
    protected RedirectionManagerER _redirectionManagerER;

    // Auxiliary Parameters
    private float _rotationFromRotationGain; // Proposed rotation gain based on head's yaw

    [HideInInspector]
    public float _lastRotationApplied = 0f;

    private void Start()
    {
        _redirectionManagerER = redirectionManager as RedirectionManagerER;
    }

    public override void ApplyRedirection()
    {
        // Get Required Data
        var deltaPos = redirectionManager.deltaPos;
        var deltaDir = redirectionManager.deltaDir;

        // Compute proposed rotation gain
        _rotationFromRotationGain = 0;

        // The steering direction is used to determine whether rotations are clockwise or counter clockwise. 
        var desiredSteeringDirection = (-1) * (int)Mathf.Sign(Utilities.GetSignedAngle(_redirectionManagerER._centreToHead, _redirectionManagerER._futureVirtualWalkingDirection));

        // If user is rotating
        if (Mathf.Abs(deltaDir) / redirectionManager.GetDeltaTime() >= _ROTATION_THRESHOLD)
        {
            var againstGain = Mathf.Min(Mathf.Abs(deltaDir * redirectionManager.MIN_ROT_GAIN), _ROTATION_GAIN_CAP_DEGREES_PER_SECOND * redirectionManager.GetDeltaTime());
            var withGain = Mathf.Min(Mathf.Abs(deltaDir * redirectionManager.MAX_ROT_GAIN), _ROTATION_GAIN_CAP_DEGREES_PER_SECOND * redirectionManager.GetDeltaTime());

            // The resulting dot products from applying gains to the vector from the centre of the physical space to the user head
            var dotFromAgainst = Vector3.Dot(Quaternion.AngleAxis(againstGain, Vector3.up) * _redirectionManagerER._centreToHead, _redirectionManagerER._futureVirtualWalkingDirection);
            var dotFromWith = Vector3.Dot(Quaternion.AngleAxis(withGain, Vector3.up) * _redirectionManagerER._centreToHead, _redirectionManagerER._futureVirtualWalkingDirection);

            // The the gain that provides the closest dot product to the target is chosen. 
            // The target in this case is aligning the future virtual direction with (trackingSpaceCentre - headPosition)
            _rotationFromRotationGain = (dotFromAgainst < dotFromWith) ? againstGain : withGain;
        }

        var rotationProposed = desiredSteeringDirection * _rotationFromRotationGain;

        // Prevent having gains if user has not moved their head
        if (Mathf.Approximately(rotationProposed, 0))
        {
            return;
        }

        // TODO: Can I make use of some dampening? Just to make the transition less jarring

        // Implement additional rotation with smoothing
        float finalRotation = (1.0f - _SMOOTHING_FACTOR) * _lastRotationApplied + _SMOOTHING_FACTOR * rotationProposed;
        _lastRotationApplied = finalRotation;
        InjectRotation(finalRotation);
    }
}
