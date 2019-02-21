using UnityEngine;
using System.Collections;
using Redirection;

public class S2CRedirectorER : SteerToRedirectorER
{
    // Testing Parameters
    private bool _dontUseTempTargetInS2C = false;

    private const float _S2C_BEARING_ANGLE_THRESHOLD_IN_DEGREE = 160;
    private const float _S2C_TEMP_TARGET_DISTANCE = 4;

    public override void PickRedirectionTarget()
    {
        var trackingAreaPosition = Utilities.FlattenedPos3D(redirectionManager.trackedSpace.position);
        var userToCenter = trackingAreaPosition - redirectionManager.currPos;

        // Compute steering target for S2C
        var bearingToCenter = Vector3.Angle(userToCenter, redirectionManager.currDir);
        var directionToCenter = Utilities.GetSignedAngle(redirectionManager.currDir, userToCenter);
        if (bearingToCenter >= _S2C_BEARING_ANGLE_THRESHOLD_IN_DEGREE && !_dontUseTempTargetInS2C)
        {
            // Generate temporary target
            if (_noTemporaryTarget)
            {
                _temporaryTarget = new GameObject("S2C Temp Target");
                _temporaryTarget.transform.position = redirectionManager.currPos + _S2C_TEMP_TARGET_DISTANCE * (Quaternion.Euler(0, directionToCenter * 90, 0) * redirectionManager.currDir);
                _temporaryTarget.transform.parent = transform;
                _noTemporaryTarget = false;
            }
            _currentTarget = _temporaryTarget.transform;
        }
        else
        {
            _currentTarget = redirectionManager.trackedSpace;
            if (!_noTemporaryTarget)
            {
                GameObject.Destroy(_temporaryTarget);
                _noTemporaryTarget = true;
            }
        }
    }
}
