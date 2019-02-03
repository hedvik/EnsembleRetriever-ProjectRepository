using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// TODO: Documentation
/// </summary>
public class RedirectionManagerER : RedirectionManager
{
    [Header("Ensemble Retriever Related")]
    public float _trackingSpaceFadeSpeed = 5f;

    private MeshRenderer _environmentFadeVisuals;
    private MeshRenderer _trackingSpaceFloorVisuals;
    private MeshRenderer _chaperoneVisuals;

    [HideInInspector]
    public bool _distractorIsActive = false;

    [HideInInspector]
    public Vector3 _futureRealWalkingDirection = Vector2.zero;

    private List<GameObject> _distractorPrefabPool = new List<GameObject>();
    private GameObject _currentActiveDistractor = null;
    private float _baseMinimumRotationGain = 0f;
    private float _baseMaximumRotationGain = 0f;

    private DistractorTrigger _distractorTrigger;

    private void Start()
    {
        _environmentFadeVisuals = base.trackedSpace.Find("EnvironmentFadeCube").GetComponent<MeshRenderer>();
        _trackingSpaceFloorVisuals = base.trackedSpace.Find("Plane").GetComponent<MeshRenderer>();
        _chaperoneVisuals = base.trackedSpace.Find("Chaperone").GetComponent<MeshRenderer>();

        _baseMaximumRotationGain = MAX_ROT_GAIN;
        _baseMinimumRotationGain = MIN_ROT_GAIN;

        // By setting _ZWrite to 1 we avoid some sorting issues
        _trackingSpaceFloorVisuals.material.SetInt("_ZWrite", 1);
        _chaperoneVisuals.material.SetInt("_ZWrite", 1);

        _distractorTrigger = trackedSpace.GetComponentInChildren<DistractorTrigger>();
        _distractorTrigger._bodyCollider = body.GetComponentInChildren<CapsuleCollider>();
        _distractorTrigger._redirectionManagerER = this;
        _distractorTrigger.Initialize();
    }

    /// <summary>
    /// More or less the same as its parent, but with distractors taken into account
    /// </summary>
    protected override void LateUpdate()
    {
        base.LateUpdate();

        if(_distractorIsActive && FutureDirectionIsAlignedToCentre())
        {
            // This approach should keep the smoothing which is nice. 
            MAX_ROT_GAIN = 0f;
            MIN_ROT_GAIN = 0f;
            Debug.Log("Aligned!");

            // For debug!
            OnDistractorEnd();
        }
    }

    public void OnDistractorTrigger()
    {
        if (_distractorIsActive)
            return;
        _distractorIsActive = true;
        // TODO: This should be an average over the last second.
        //       Does it need to though? might be more accurate
        _futureRealWalkingDirection = Redirection.Utilities.FlattenedDir3D(deltaPos);
        _baseMaximumRotationGain = MAX_ROT_GAIN;
        _baseMinimumRotationGain = MIN_ROT_GAIN;
        // Increase gains
        // Spawn distractor
        Debug.Log("Distractor Triggered!");
    }

    public void OnDistractorEnd()
    {
        _distractorIsActive = false;
        _futureRealWalkingDirection = Vector3.zero;
        MAX_ROT_GAIN = _baseMaximumRotationGain;
        MIN_ROT_GAIN = _baseMinimumRotationGain;
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

    /// <summary>
    /// Returns true whenever the future real walking direction is mostly aligned with the centre of the tracking space.
    /// </summary>
    /// <returns></returns>
    private bool FutureDirectionIsAlignedToCentre()
    {
        var dotProduct = Vector3.Dot(_futureRealWalkingDirection, Redirection.Utilities.FlattenedDir3D(headTransform.position - trackedSpace.position));

        return dotProduct <= -0.975;
    }
}
