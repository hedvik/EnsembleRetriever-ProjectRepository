using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class employing a modified version of the Freeze - Turn reset method.
/// World rotation is only "frozen" on the y axis, the physical bounds are faded in and the user is asked to look towards the centre of the room. 
/// </summary>
public class FreezeTurnCentre : Resetter
{
    private GameObject _resetTextPrefab = null;
    private GameObject _resetVisualObjectPrefab = null;
    private GameObject _resetTextInstance;
    private GameObject _resetVisualObjectInstance;
    private RedirectionManagerER _redirectionManagerER;

    private void Start()
    {
        _redirectionManagerER = base.redirectionManager as RedirectionManagerER;
        _resetTextPrefab = Resources.Load<GameObject>("ResetText/FreezeTurnCentreER HUD");
        _resetVisualObjectPrefab = Resources.Load<GameObject>("ResetObjects/FreezeTurnCentreER Object");
    }

    public override bool IsResetRequired()
    {
        return !isUserFacingAwayFromWall();
    }

    public override void InitializeReset()
    {
        SetResetVisuals();
        _redirectionManagerER.FadeTrackingSpace(true);
    }

    public override void ApplyResetting()
    {
        // The scene is now synchronised with the y rotation of the head
        InjectRotation(-_redirectionManagerER.deltaDir);
        var dotProductFaceAndCentre = Vector2.Dot(Redirection.Utilities.FlattenedDir2D(_redirectionManagerER.headTransform.forward), 
                                                  Redirection.Utilities.FlattenedDir2D(_redirectionManagerER.headTransform.position - _redirectionManagerER.trackedSpace.position));

        // -1 would in this case mean pointing directly at the centre. Some error is acceptable
        if (dotProductFaceAndCentre <= -0.975)
        {
            _redirectionManagerER.OnResetEnd();
        }
    }

    public override void FinalizeReset()
    {
        // If this results in too much garbage collection it is always possible to just disable the objects instead of deleting them
        Destroy(_resetTextInstance.gameObject);
        Destroy(_resetVisualObjectInstance.gameObject);
        _redirectionManagerER.FadeTrackingSpace(false);
    }

    public void SetResetVisuals()
    {
        _resetTextInstance = Instantiate(_resetTextPrefab);
        _resetTextInstance.transform.parent = _redirectionManagerER.headTransform;
        _resetTextInstance.transform.localPosition = _resetTextInstance.transform.position;
        _resetTextInstance.transform.localRotation = _resetTextInstance.transform.rotation;

        _resetVisualObjectInstance = Instantiate(_resetVisualObjectPrefab);
        _resetVisualObjectInstance.transform.parent = _redirectionManagerER.trackedSpace;
        _resetVisualObjectInstance.transform.localPosition = new Vector3(0, _redirectionManagerER.headTransform.position.y, 0);
    }

    public override void SimulatedWalkerUpdate()
    {
        Debug.LogError("Simulated behaviour is not implemented for this resetter!");
    }
}
