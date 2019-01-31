using UnityEngine;
using System.Collections;

/// <summary>
/// This type of reset injects a 180 rotation. It will show a prompt to the user once at the full rotation is applied and the user is roughly looking at the original direction.
/// The method is simply doubling the rotation amount. No smoothing is applied. No specific rotation is enforced this way.
/// This is more or less just a modified version of TwoOneTurnResetter.cs to account for environment fading. 
/// </summary>
public class TwoOneTurnResetterER : Resetter {

    ///// <summary>
    ///// The user must return to her original orientation for the reset to let go. Up to this amount of error is allowed.
    ///// </summary>
    //float MAX_ORIENTATION_RETURN_ERROR = 15;

    private float _overallInjectedRotation;
    private Transform _prefabHUD = null;
    private Transform _instanceHUD;
    private RedirectionManagerER _redirectionManagerER;

    private void Start()
    {
        _redirectionManagerER = base.redirectionManager as RedirectionManagerER;
    }

    public override bool IsResetRequired()
    {
        return !isUserFacingAwayFromWall();
    }

    public override void InitializeReset()
    {
        _overallInjectedRotation = 0;
        SetHUD();
        _redirectionManagerER.FadeTrackingSpace(true);
    }

    public override void ApplyResetting()
    {
        if (Mathf.Abs(_overallInjectedRotation) < 180)
        {
            // The idea is that we will keep going in this direction until we reach the objective
            float remainingRotation = redirectionManager.deltaDir > 0 ? 180 - _overallInjectedRotation : -180 - _overallInjectedRotation; 
            if (Mathf.Abs(remainingRotation) < Mathf.Abs(redirectionManager.deltaDir))
            {
                InjectRotation(remainingRotation);
                redirectionManager.OnResetEnd();
                _overallInjectedRotation += remainingRotation;
            }
            else
            {
                InjectRotation(redirectionManager.deltaDir);
                _overallInjectedRotation += redirectionManager.deltaDir;
            }
        }
    }

    public override void FinalizeReset()
    {
        Destroy(_instanceHUD.gameObject);
        _redirectionManagerER.FadeTrackingSpace(false);
    }

    public void SetHUD()
    {
        if (_prefabHUD == null)
            _prefabHUD = Resources.Load<Transform>("TwoOneTurnResetterER HUD");
        _instanceHUD = Instantiate(_prefabHUD);
        _instanceHUD.parent = redirectionManager.headTransform;
        _instanceHUD.localPosition = _instanceHUD.position;
        _instanceHUD.localRotation = _instanceHUD.rotation;
    }

    public override void SimulatedWalkerUpdate()
    {
        // Act is if there's some dummy target a meter away from you requiring you to rotate
        //redirectionManager.simulatedWalker.RotateIfNecessary(180 - overallInjectedRotation, Vector3.forward);
        redirectionManager.simulatedWalker.RotateInPlace();
        //print("overallInjectedRotation: " + overallInjectedRotation);
    }
}
