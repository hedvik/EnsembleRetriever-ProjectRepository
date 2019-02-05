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

    [HideInInspector]
    public bool _distractorIsActive = false;

    [HideInInspector]
    public Vector3 _futureVirtualWalkingDirection = Vector3.zero;

    [HideInInspector]
    public Vector3 _centreToHead = Vector3.zero;

    // TODO: The choice of distractor can probably be semi random by using a stack or queue. Pick one randomly, remove it from the container, pick next one randomly etc
    //       This container is then reset once everything has been picked once. 
    private List<GameObject> _distractorPrefabPool = new List<GameObject>();
    private Distractor _currentActiveDistractor = null;
    private float _baseMinimumRotationGain = 0f;
    private float _baseMaximumRotationGain = 0f;
    private AC2FRedirector _AC2FRedirector;
    private S2CRedirectorER _S2CRedirector;

    private DistractorTrigger _distractorTrigger;
    private List<Pausable> _pausables = new List<Pausable>();

    private MeshRenderer _environmentFadeVisuals;
    private MeshRenderer _trackingSpaceFloorVisuals;
    private MeshRenderer _chaperoneVisuals;
    private GameObject _virtualWorld;

    protected override void Awake()
    {
        base.Awake();
        _environmentFadeVisuals = base.trackedSpace.Find("EnvironmentFadeCube").GetComponent<MeshRenderer>();
        _trackingSpaceFloorVisuals = base.trackedSpace.Find("Plane").GetComponent<MeshRenderer>();
        _chaperoneVisuals = base.trackedSpace.Find("Chaperone").GetComponent<MeshRenderer>();

        _baseMaximumRotationGain = MAX_ROT_GAIN;
        _baseMinimumRotationGain = MIN_ROT_GAIN;

        _AC2FRedirector = GetComponent<AC2FRedirector>();
        _AC2FRedirector.redirectionManager = this;

        _S2CRedirector = GetComponent<S2CRedirectorER>();

        // By setting _ZWrite to 1 we avoid some sorting issues
        _trackingSpaceFloorVisuals.material.SetInt("_ZWrite", 1);
        _chaperoneVisuals.material.SetInt("_ZWrite", 1);

        _distractorTrigger = trackedSpace.GetComponentInChildren<DistractorTrigger>();
        _distractorTrigger._bodyCollider = body.GetComponentInChildren<CapsuleCollider>();
        _distractorTrigger._redirectionManagerER = this;
        _distractorTrigger.Initialise();

        _distractorPrefabPool.AddRange(Resources.LoadAll<GameObject>("Distractors"));
        _pausables.AddRange(FindObjectsOfType<Pausable>());

        _virtualWorld = GameObject.Find("Virtual World");
    }

    /// <summary>
    /// More or less the same as its parent, but with distractors taken into account
    /// </summary>
    protected override void LateUpdate()
    {
        base.LateUpdate();

        if(_distractorIsActive && FutureDirectionIsAlignedToCentre() && !inReset)
        {
            // This approach should keep the smoothing which is nice
            // NOTE: This will run every frame once alignment is finished. 
            MAX_ROT_GAIN = 0f;
            MIN_ROT_GAIN = 0f;
        }
    }

    public void OnDistractorTrigger()
    {
        if (_distractorIsActive)
            return;
        _distractorIsActive = true;
        // TODO: This should be an average over the last second.
        //       Does it need to though? might be more accurate
        _futureVirtualWalkingDirection = Redirection.Utilities.FlattenedDir3D(deltaPos);
        _baseMaximumRotationGain = MAX_ROT_GAIN;
        _baseMinimumRotationGain = MIN_ROT_GAIN;
        SwapRedirectionAlgorithm(true);
        // TODO: Request gain increase
        _currentActiveDistractor = Instantiate(_distractorPrefabPool[Random.Range(0, _distractorPrefabPool.Count)], _virtualWorld.transform).GetComponent<Distractor>();
        _currentActiveDistractor.InitialiseDistractor(this);
        _pausables.Add(_currentActiveDistractor);
    }

    public void OnDistractorEnd()
    {
        _distractorIsActive = false;
        _futureVirtualWalkingDirection = Vector3.zero;
        // TODO: Request gain decrease instead of setting them here. Might not want to change it until user has moved towards future
        MAX_ROT_GAIN = _baseMaximumRotationGain;
        MIN_ROT_GAIN = _baseMinimumRotationGain;
        SwapRedirectionAlgorithm(false);
        _currentActiveDistractor.FinaliseDistractor();
        _pausables.Remove(_currentActiveDistractor);
        _currentActiveDistractor = null;
    }

    public void SetWorldPauseState(bool isPaused)
    {
        foreach(var pausable in _pausables)
        {
            pausable.SetPauseState(isPaused);
        }
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
        // TODO: Might refactor updating centreToHead away if necessary later as it wont change much. 
        _centreToHead = Redirection.Utilities.FlattenedDir3D(headTransform.position - trackedSpace.position);
        var dotProduct = Vector3.Dot(_futureVirtualWalkingDirection, _centreToHead);

        return dotProduct <= -0.975;
    }

    private void SwapRedirectionAlgorithm(bool toAC2F)
    {
        if(toAC2F)
        {
            redirector = _AC2FRedirector;
            _AC2FRedirector._lastRotationApplied = _S2CRedirector._lastRotationApplied;
        }
        else
        {
            redirector = _S2CRedirector;
            _S2CRedirector._lastRotationApplied = _AC2FRedirector._lastRotationApplied;
        }
    }
}
