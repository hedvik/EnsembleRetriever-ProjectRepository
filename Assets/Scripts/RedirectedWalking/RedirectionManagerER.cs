using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// TODO: Documentation for all redirection related scripts should be in full doxygen.
/// </summary>
public class RedirectionManagerER : RedirectionManager
{
    [Header("Ensemble Retriever Related")]
    public float _trackingSpaceFadeSpeed = 5f;
    public bool _alwaysDisplayTrackingFloor = false;
    public bool _switchToAC2FOnDistractor = true;
    public int _positionSamplesPerSecond = 60;
    [Range(-1, 0)]
    public float _alignmentThreshold = -0.9f;

    [HideInInspector]
    public bool _distractorIsActive = false;

    [HideInInspector]
    public Vector3 _futureVirtualWalkingDirection = Vector3.zero;

    [HideInInspector]
    public Vector3 _centreToHead = Vector3.zero;

    [HideInInspector]
    public GameManager _gameManager;

    [HideInInspector]
    public PlayerManager _playerManager;

    // TODO: The choice of distractor can probably be semi random by using a stack or queue. Pick one randomly, remove it from the container, pick next one randomly etc
    //       This container is then reset once everything has been picked once.
    private List<GameObject> _distractorPrefabPool = new List<GameObject>();
    private DistractorEnemy _currentActiveDistractor = null;
    private float _baseMinimumRotationGain = 0f;
    private float _baseMaximumRotationGain = 0f;
    private float _baseCurvatureRadius = 0f;
    private AC2FRedirector _AC2FRedirector;
    private S2CRedirectorER _S2CRedirector;

    private DistractorTrigger _distractorTrigger;
    private List<Pausable> _pausables = new List<Pausable>();

    private MeshRenderer _environmentFadeVisuals;
    private MeshRenderer _trackingSpaceFloorVisuals;
    private MeshRenderer _chaperoneVisuals;
    private GameObject _virtualWorld;
    private UIManager _uiManager;

    private CircularBuffer.CircularBuffer<Vector3> _positionSamples;
    private float _sampleTimer = 0f;

    protected override void Awake()
    {
        base.Awake();
        _environmentFadeVisuals = base.trackedSpace.Find("EnvironmentFadeCube").GetComponent<MeshRenderer>();
        _trackingSpaceFloorVisuals = base.trackedSpace.Find("Plane").GetComponent<MeshRenderer>();
        _chaperoneVisuals = base.trackedSpace.Find("Chaperone").GetComponent<MeshRenderer>();

        _baseMaximumRotationGain = MAX_ROT_GAIN;
        _baseMinimumRotationGain = MIN_ROT_GAIN;
        _baseCurvatureRadius = CURVATURE_RADIUS;

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

        _virtualWorld = GameObject.Find("Virtual Environment");
        _uiManager = GameObject.Find("UIManager").GetComponent<UIManager>();

        _uiManager._redirectorManager = this;

        _positionSamples = new CircularBuffer.CircularBuffer<Vector3>(_positionSamplesPerSecond);

        _gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();

        var floorColour = _trackingSpaceFloorVisuals.material.color;
        floorColour.a = _alwaysDisplayTrackingFloor ? 1f : 0f;
        _trackingSpaceFloorVisuals.material.color = floorColour;

        _playerManager = GetComponentInChildren<PlayerManager>();
    }

    /// <summary>
    /// More or less the same as its parent, but with distractors taken into account
    /// </summary>
    protected override void LateUpdate()
    {
        base.LateUpdate();

        if (_distractorIsActive && FutureDirectionIsAlignedToCentre() && !inReset)
        {
            _AC2FRedirector.DisableGains();
        }

        _sampleTimer += Time.deltaTime;
        if (_sampleTimer >= 1 / _positionSamplesPerSecond)
        {
            _sampleTimer -= 1 / _positionSamplesPerSecond;
            _positionSamples.PushBack(deltaPos);
        }
    }

    public void ActivateRotationAndCurvatureGains()
    {
        MAX_ROT_GAIN = _baseMaximumRotationGain;
        MIN_ROT_GAIN = _baseMinimumRotationGain;
        CURVATURE_RADIUS = _baseCurvatureRadius;
    }

    public void OnDistractorTrigger()
    {
        if (_distractorIsActive || !_gameManager._gameStarted)
            return;
        _distractorIsActive = true;

        var _averageFuture = Vector3.zero;
        for (int i = 0; i < _positionSamples.Size; i++)
        {
            _averageFuture += _positionSamples[i];
        }
        _futureVirtualWalkingDirection = (_averageFuture / _positionSamples.Size).normalized;

        _baseMaximumRotationGain = MAX_ROT_GAIN;
        _baseMinimumRotationGain = MIN_ROT_GAIN;
        SwapRedirectionAlgorithm(true);
        // TODO: Request gain increase
        _currentActiveDistractor = Instantiate(_distractorPrefabPool[Random.Range(0, _distractorPrefabPool.Count)]).GetComponent<DistractorEnemy>();
        _currentActiveDistractor.InitialiseDistractor(this);
    }

    public void OnDistractorEnd()
    {
        _distractorIsActive = false;
        _futureVirtualWalkingDirection = Vector3.zero;
        // TODO: Request gain decrease instead of setting them here. Might not want to change it until user has moved towards future
        MAX_ROT_GAIN = _baseMaximumRotationGain;
        MIN_ROT_GAIN = _baseMinimumRotationGain;
        // TODO: This swap should also be similar to a gain change request. It should only swap when deltaDir is low enough(user it not moving their head around)
        SwapRedirectionAlgorithm(false);
        _currentActiveDistractor.FinaliseDistractor();
        _currentActiveDistractor = null;
    }

    public void SetWorldPauseState(bool isPaused)
    {
        // NOTE: This approach might not be ideal performance wise.
        _pausables.Clear();
        _pausables.AddRange(FindObjectsOfType<Pausable>());
        foreach (var pausable in _pausables)
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
        while (lerpTimer < 1f)
        {
            lerpTimer += Time.deltaTime * _trackingSpaceFadeSpeed;

            // To make sure that the environment is slightly visible, 0.99 is used as the max alpha.
            cubeColorTemp.a = Mathf.Lerp(cubeAlphaStart, fadePhysicalSpaceIn ? 0.99f : 0, lerpTimer);
            floorColorTemp.a = Mathf.Lerp(floorAlphaStart, fadePhysicalSpaceIn ? 1 : 0, lerpTimer);
            chaperoneColorTemp.a = floorColorTemp.a;
            _environmentFadeVisuals.material.color = cubeColorTemp;
            _chaperoneVisuals.material.color = chaperoneColorTemp;

            if (!_alwaysDisplayTrackingFloor)
            {
                _trackingSpaceFloorVisuals.material.color = floorColorTemp;
            }

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
        var dotProduct = Vector3.Dot(_centreToHead, _futureVirtualWalkingDirection);

        return dotProduct <= _alignmentThreshold;
    }

    private void SwapRedirectionAlgorithm(bool toAC2F)
    {
        if (_switchToAC2FOnDistractor)
        {
            if (toAC2F)
            {
                redirector = _AC2FRedirector;
                _AC2FRedirector.OnRedirectionMethodSwitch();
                _AC2FRedirector._lastRotationApplied = _S2CRedirector._lastRotationApplied;
            }
            else
            {
                redirector = _S2CRedirector;
                _S2CRedirector._lastRotationApplied = _AC2FRedirector._lastRotationApplied;
            }
        }
    }
}