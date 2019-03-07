using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public delegate void OnDistractorStateChangeCallback();

/// <summary>
/// Extension of RedirectionManager from the Redirected Walking Toolkit that facilitates the use of distractors and other interfacing for the EnsembleRetriever game.
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

    // A new distractor cannot trigger until the user has moved enough to hit this threshold. 
    // This is meant to deal with cases where the user is standing still at the edge of the distractor collider
    // and doing some small movements that would retrigger a new one right after the first one dies. 
    // This value will most likely need to be tweaked depending on room size, as such it is not an ideal solution. 
    public float _distractorMagnitudeCooldownAfterDeath = 5f;
    private float _distractorCooldownMagnitudeAccumulation = 0f;

    [Tooltip("Should be null for normal behaviour or a specific distractor if you wish to always spawn that one")]
    public GameObject _debugDistractor;

    [HideInInspector]
    public bool _distractorIsActive = false;

    [HideInInspector]
    public Vector3 _futureVirtualWalkingDirection = Vector3.zero;

    [HideInInspector]
    public GameManager _gameManager;

    [HideInInspector]
    public PlayerManager _playerManager;

    [HideInInspector]
    public RedirectionAlgorithms _currentActiveRedirectionAlgorithmType = RedirectionAlgorithms.S2C;

    [HideInInspector]
    public DistractorEnemy _currentActiveDistractor = null;

    private List<GameObject> _distractorPrefabPool = new List<GameObject>();
    private List<GameObject> _randomDistractorPoolList = new List<GameObject>();
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

    private bool _distractorsEnabled = true;
    private bool _alignmentResetComplete = false;

    private OnDistractorStateChangeCallback _distractorTriggerCallback;
    private OnDistractorStateChangeCallback _distractorEndCallback;
    private OnDistractorStateChangeCallback _centreAlignedCallback;

    [HideInInspector]
    public Vector3 _centreToHead = Vector3.zero;

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

        // By setting _ZWrite to 1 we avoid some sorting issues.
        // Setting the render queue a bit higher than normal should help with some transparency issues between the chaperone and fade cube too.
        _trackingSpaceFloorVisuals.material.SetInt("_ZWrite", 1);
        _chaperoneVisuals.material.SetInt("_ZWrite", 1);
        _chaperoneVisuals.material.renderQueue = 4000;

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
        _distractorCooldownMagnitudeAccumulation = _distractorMagnitudeCooldownAfterDeath;

        RepopulateRandomDistractorList();
    }

    protected override void LateUpdate()
    {
        base.LateUpdate();

        _distractorCooldownMagnitudeAccumulation += deltaPos.magnitude * Time.deltaTime;
        _centreToHead = Redirection.Utilities.FlattenedDir3D(headTransform.position - trackedSpace.position);

        if (_distractorIsActive && !_alignmentResetComplete && FutureDirectionIsAlignedToCentre() && !inReset)
        {
            _alignmentResetComplete = true;
            _baseMaximumRotationGain = MAX_ROT_GAIN;
            _baseMinimumRotationGain = MIN_ROT_GAIN;
            _AC2FRedirector.DisableGains();
            _centreAlignedCallback?.Invoke();
        }

        _sampleTimer += Time.deltaTime;
        if (_sampleTimer >= 1 / _positionSamplesPerSecond)
        {
            _sampleTimer -= 1 / _positionSamplesPerSecond;
            _positionSamples.PushBack(deltaPos);
        }
    }

    /// <summary>
    /// Function that sets gains to their initial value which is defined in the inspector.
    /// The gains are disabled at the start of the game for the sake of the tutorial, hence why this function exists. 
    /// </summary>
    public void ActivateRotationAndCurvatureGains()
    {
        MAX_ROT_GAIN = _baseMaximumRotationGain;
        MIN_ROT_GAIN = _baseMinimumRotationGain;
        CURVATURE_RADIUS = _baseCurvatureRadius;
    }

    /// <summary>
    /// Callback that happens whenever the user has moved outside of the safe area in the physical space. 
    /// It initialises and spawns a distractor to help reorient the user back towards the centre of the physical space. 
    /// </summary>
    public void OnDistractorTrigger()
    {
        if (!_distractorsEnabled || _distractorCooldownMagnitudeAccumulation <= _distractorMagnitudeCooldownAfterDeath)
            return;
        if (_distractorIsActive || !_gameManager._gameStarted)
            return;
        _distractorIsActive = true;
        _distractorTriggerCallback?.Invoke();
        _alignmentResetComplete = false;

        var _averageFuture = Vector3.zero;
        for (int i = 0; i < _positionSamples.Size; i++)
        {
            _averageFuture += _positionSamples[i];
        }
        _futureVirtualWalkingDirection = (_averageFuture / _positionSamples.Size).normalized;

        RequestAlgorithmSwitch(true);
        if (_debugDistractor != null)
        {
            _currentActiveDistractor = Instantiate(_debugDistractor).GetComponent<DistractorEnemy>();
        }
        else
        {
            // This approach should allow for semi random distractor choices, which mostly avoids repeats of the same one.
            var chosenPrefab = _randomDistractorPoolList[Random.Range(0, _randomDistractorPoolList.Count)];
            var newDistractor = Instantiate(chosenPrefab).GetComponent<DistractorEnemy>();
            _currentActiveDistractor = newDistractor;
            _randomDistractorPoolList.Remove(chosenPrefab);

            if(_randomDistractorPoolList.Count == 0)
            {
                RepopulateRandomDistractorList();
            }
        }
        _currentActiveDistractor.InitialiseDistractor(this);
    }

    /// <summary>
    /// Callback that happens whenever an active distractor has finished.
    /// This is mostly where cleanup happens. 
    /// </summary>
    public void OnDistractorEnd()
    {
        // If AC2F managed to align the user, then we have to return to the gains that were used right before alignment. 
        if(_alignmentResetComplete)
        {
            MAX_ROT_GAIN = _baseMaximumRotationGain;
            MIN_ROT_GAIN = _baseMinimumRotationGain;
        }

        _distractorIsActive = false;
        _distractorEndCallback?.Invoke();
        _futureVirtualWalkingDirection = Vector3.zero;
        RequestAlgorithmSwitch(false);
        _currentActiveDistractor.FinaliseDistractor();
        _currentActiveDistractor = null;
        _gameManager.StopBattleTheme();
        _distractorCooldownMagnitudeAccumulation = 0f;
    }

    /// <summary>
    /// Function for triggering a pause state callback on every relevant pausable object.
    /// This is used by the Pause Turn Centre algorithm to pause things like projectiles, distractors, animations and so on when a reset is active. 
    /// </summary>
    /// <param name="isPaused">Whether to pause the virtual world or not</param>
    public void SetWorldPauseState(bool isPaused)
    {
        // NOTE: This approach might not be ideal performance wise.
        // A better option could be to have every pausable "subscribe" to this list on initialise and remove itself OnDestroy()
        _pausables.Clear();
        _pausables.AddRange(FindObjectsOfType<Pausable>());
        foreach (var pausable in _pausables)
        {
            pausable.SetPauseState(isPaused);
        }
    }

    /// <summary>
    /// Function for setting the state of whether distractors should be spawned or not. 
    /// </summary>
    /// <param name="state">Whether to spawn distractors when the user is leaving the safe area or not</param>
    public void SetDistractorUsageState(bool state)
    {
        _distractorsEnabled = state;
    }

    /// <summary>
    /// Function for subscribing to a callback that happens whenever distractors trigger.
    /// </summary>
    /// <param name="function">Function to be called back on distractor trigger</param>
    public void SubscribeToDistractorTriggerCallback(OnDistractorStateChangeCallback function)
    {
        _distractorTriggerCallback += function;
    }

    /// <summary>
    /// Function for subscribing to a callback that happens whenever distractors finish.
    /// </summary>
    /// <param name="function">Function to be called back on distractor finish</param>
    public void SubscribeToDistractorEndCallback(OnDistractorStateChangeCallback function)
    {
        _distractorEndCallback += function;
    }

    /// <summary>
    /// Function for subscribing to a callback that happens when AC2F has finished alignment.
    /// </summary>
    /// <param name="function">Function to be called back when alignment is complete.</param>
    public void SubscribeToAlignmentCallback(OnDistractorStateChangeCallback function)
    {
        _centreAlignedCallback += function;
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
    /// In cases where you want to do some reorientation and check centreToHead after some changes within the same frame you can use this function.
    /// </summary>
    /// <returns>A recalculated _centreToHead</returns>
    public Vector3 GetUpdatedCentreToHead()
    {
        _centreToHead = Redirection.Utilities.FlattenedDir3D(headTransform.position - trackedSpace.position);
        return _centreToHead;
    }

    /// <summary>
    /// Interpolates the alpha values for the physical tracking space so it can fade in or out. 
    /// </summary>
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
    /// <returns>True if we are within the alignment threshold. False otherwise.</returns>
    private bool FutureDirectionIsAlignedToCentre()
    {
        var dotProduct = Vector3.Dot(_centreToHead, _futureVirtualWalkingDirection);
        return dotProduct <= _alignmentThreshold;
    }

    /// <summary>
    /// Starts the coroutine for switching redirection algorithm.
    /// The coroutine waits until the user's head is relatively stable before switching algorithms to prevent any potential jarring changes. 
    /// </summary>
    /// <param name="toAC2F">Whether to switch to AC2F or not(moving back to S2C)</param>
    private void RequestAlgorithmSwitch(bool toAC2F)
    {
        if (_switchToAC2FOnDistractor)
        {
            StartCoroutine(SwapRedirectionAlgorithm(toAC2F));
        }
    }

    /// <summary>
    /// Coroutine that waits until the user's head is relatively stable before switching redirection algorithm.
    /// </summary>
    /// <param name="toAC2F">Whether to switch to AC2F or not(moving back to S2C)</param>
    private IEnumerator SwapRedirectionAlgorithm(bool toAC2F)
    {
        // Wait until the head is relatively stable, then switch
        while (Mathf.Abs(deltaDir) / Time.deltaTime >= AC2FRedirector._ROTATION_THRESHOLD)
        {
            yield return null;
        }

        //Debug.Log("Algorithm Switched To: " + (toAC2F ? "AC2F" : "S2C"));

        if (toAC2F)
        {
            redirector = _AC2FRedirector;
            _AC2FRedirector.OnRedirectionMethodSwitch();
            _currentActiveRedirectionAlgorithmType = RedirectionAlgorithms.AC2F;
            // Keeping this disabled has generally provided a better VR experience so far
            //_AC2FRedirector._lastRotationApplied = _S2CRedirector._lastRotationApplied;
        }
        else
        {
            redirector = _S2CRedirector;
            _currentActiveRedirectionAlgorithmType = RedirectionAlgorithms.S2C;
            //_S2CRedirector._lastRotationApplied = _AC2FRedirector._lastRotationApplied;
        }
    }

    /// <summary>
    /// Utility function for repopulating the semi-random distractor list.
    /// A distractor is randomly chosen from this list each time it is needed and then removed from the list.
    /// This limits potential repeats of the same distractor.
    /// </summary>
    private void RepopulateRandomDistractorList()
    {
        foreach(var element in _distractorPrefabPool)
        {
            _randomDistractorPoolList.Add(element);
        }
    }
}