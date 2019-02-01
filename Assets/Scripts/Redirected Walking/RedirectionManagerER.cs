using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RedirectionManagerER : RedirectionManager
{
    [Header("Ensemble Retriever Related")]
    public float _trackingSpaceFadeSpeed = 5f;

    private MeshRenderer _environmentFadeVisuals;
    private MeshRenderer _trackingSpaceFloorVisuals;
    private MeshRenderer _chaperoneVisuals;

    [HideInInspector]
    public bool _distractorActive = false;

    [HideInInspector]
    public Vector3 _futureWalkingDirection = Vector3.zero;

    private List<GameObject> _distractorPrefabPool = new List<GameObject>();
    private GameObject _activeDistractor = null;

    private void Start()
    {
        _environmentFadeVisuals = base.trackedSpace.Find("EnvironmentFadeCube").GetComponent<MeshRenderer>();
        _trackingSpaceFloorVisuals = base.trackedSpace.Find("Plane").GetComponent<MeshRenderer>();
        _chaperoneVisuals = base.trackedSpace.Find("Chaperone").GetComponent<MeshRenderer>();

        // By setting _ZWrite to 1 we avoid some sorting issues
        _trackingSpaceFloorVisuals.material.SetInt("_ZWrite", 1);
        _chaperoneVisuals.material.SetInt("_ZWrite", 1);
    }

    /// <summary>
    /// More or less the same as its parent, but with distractors taken into account
    /// </summary>
    protected override void LateUpdate()
    {
        simulatedTime += 1.0f / targetFPS;

        UpdateCurrentUserState();
        CalculateStateChanges();

        if (inReset)
        {
            if (resetter != null)
            {
                resetter.ApplyResetting();
            }
        }
        else
        {
            // TODO: Also check if distractor is active + whether future is aligned to centre
            // Might not need to edit the redirectors that way
            if (redirector != null)
            {
                redirector.ApplyRedirection();
            }
        }

        statisticsLogger.UpdateStats();

        UpdatePreviousUserState();

        UpdateBodyPose();
    }

    public void OnDistractorTrigger()
    {
        if (_distractorActive)
            return;
        _distractorActive = true;
        // TODO: This should be an average over the last second 
        //_futureWalkingDirection = deltaPos;
        // Increase gains
        // Spawn distractor
    }

    public void OnDistractorEnd()
    {
        _distractorActive = false;
        _futureWalkingDirection = Vector3.zero;
        // Decrease gains
        // Tell distractor to finish
    }

    /// <summary>
    /// Can be used to fade the environment out and the physical space in.
    /// The primary use case for this would be custom reset methods. 
    /// </summary>
    /// <param name="fadePhysicalSpaceIn">Whether to fade in or out.</param>
    public void FadeTrackingSpace(bool fadePhysicalSpaceIn)
    {
        StartCoroutine(FadeCoroutine(fadePhysicalSpaceIn));

        // TODO: might somehow want to disable any objects intersecting with the floor
    }

    /// <summary>
    /// Interpolates the alpha values for the physical tracking space. 
    /// </summary>
    /// <param name="environmentAlphaTarget"></param>
    /// <param name="floorAlphaTarget"></param>
    private IEnumerator FadeCoroutine(bool fadePhysicalSpaceIn)
    {
        var cubeColorTemp = _environmentFadeVisuals.material.color;
        var floorColorTemp = _trackingSpaceFloorVisuals.material.color;
        var chaperoneColorTemp = _chaperoneVisuals.material.color;

        var cubeAlphaStart = _environmentFadeVisuals.material.color.a;
        var floorAlphaStart = _trackingSpaceFloorVisuals.material.color.a;

        float lerpTimer = 0f;
        while(lerpTimer < 1f)
        {
            lerpTimer += Time.deltaTime * _trackingSpaceFadeSpeed;

            // To make sure that the environment is slightly visible, 0.99 is used as the max alpha. 
            cubeColorTemp.a = Mathf.Lerp(cubeAlphaStart, fadePhysicalSpaceIn ? 0.99f : 0, lerpTimer);
            floorColorTemp.a = Mathf.Lerp(floorAlphaStart, fadePhysicalSpaceIn ? 1 : 0, lerpTimer);
            chaperoneColorTemp.a = floorColorTemp.a;
            _environmentFadeVisuals.material.color = cubeColorTemp;
            _trackingSpaceFloorVisuals.material.color = floorColorTemp;
            _chaperoneVisuals.material.color = chaperoneColorTemp;

            yield return null;
        }
    }
}
