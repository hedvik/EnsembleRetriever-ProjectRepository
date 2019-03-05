using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Package Link: https://www.nuget.org/packages/MersenneTwister/
using MersenneTwister;
using Valve.VR;

/// <summary>
/// Increments gains at a random time step with a random value given the following conditions:
/// Gains cannot be increased or halved when:
///    Curvature:  AC2F is active
///    Rotation:   Future is aligned with centre
/// </summary>
public class GainIncrementer : MonoBehaviour
{
    [Header("Start And End Gain Values")]
    public float _startRotationGain = 0f;
    public float _startCurvatureRadius = 25f;

    // In this case, these values are percentages. So maximum rotation gain is 100% increased head rotation/gain of 2
    // while minimum rotation gain is 50% decreased head rotation/gain of 0.5. 
    public float _maximumPositiveGain = 1.0f;
    public float _maximumNegativeGain = -0.5f;

    public float _minimumCurvatureRadius = 5f;

    [Header("Increment Variables")]
    public float _timeStepBase = 5f;
    public float _timeStepNoise = 2.5f;

    public float _rotationGainBaseIncrement = 0.02f;
    public float _rotationGainIncrementNoise = 0.01f;

    public float _curvatureRadiusBaseIncrement = 1f;
    public float _curvatureRadiusIncrementNoise = 0.5f;

    private ExperimentDataManager _experimentDataManager;
    private bool _incrementTimerActive = false;
    private float _incrementTimer = 0f;
    private float _currentTimestep = 0f;

    private void Start()
    {
        _experimentDataManager = GetComponent<ExperimentDataManager>();
        GenerateRandomTimeStep();
    }

    private void Update()
    {
        if(!_incrementTimerActive)
        {
            return;
        }

        _incrementTimer += Time.deltaTime;
        if(_incrementTimer >= _currentTimestep)
        {
            _incrementTimer -= _currentTimestep;
            IncrementGains();
            GenerateRandomTimeStep();
        }
    }

    public void InitialiseIncrementer()
    {
        _experimentDataManager._redirectionManager.MAX_ROT_GAIN = _startRotationGain;
        _experimentDataManager._redirectionManager.MIN_ROT_GAIN = _startRotationGain;
        _experimentDataManager._redirectionManager.CURVATURE_RADIUS = _startCurvatureRadius;
        _experimentDataManager._redirectionManager.SubscribeToAlignmentCallback(DeactivateGainIncrements);
        _experimentDataManager._redirectionManager.SubscribeToDistractorEndCallback(ActivateGainIncrements);
    }

    public void ActivateGainIncrements()
    {
        _incrementTimerActive = true;
    }

    /// <summary>
    /// Used to disable the timer for incrementing gains.
    /// It should for example be disabled during alignment with AC2F as we do not want to accumulate gains when they are disabled. 
    /// </summary>
    /// <param name="state"></param>
    public void DeactivateGainIncrements()
    {
        _incrementTimerActive = false;
    }

    public void Reset(RecordedGainTypes detectedGain)
    {
        // TODO: Decide whether to reset all gains or the latest
        _incrementTimer = 0f;
        //_experimentDataManager._redirectionManager.MAX_ROT_GAIN *= 0.5f;
        //_experimentDataManager._redirectionManager.MIN_ROT_GAIN *= 0.5f;

        //if(_experimentDataManager._redirectionManager._currentActiveRedirectionAlgorithmType == RedirectionAlgorithms.S2C)
        //{
        //    _experimentDataManager._redirectionManager.CURVATURE_RADIUS *= 1.25f;
        //}
        switch(detectedGain)
        {
            case RecordedGainTypes.rotationAgainstHead: _experimentDataManager._redirectionManager.MIN_ROT_GAIN *= 0.5f; break;
            case RecordedGainTypes.rotationWithHead: _experimentDataManager._redirectionManager.MAX_ROT_GAIN *= 0.5f; break;
            case RecordedGainTypes.curvature: _experimentDataManager._redirectionManager.CURVATURE_RADIUS *= 1.25f; break;
        }

    }

    private void IncrementGains()
    {
        // The upper bound is exclusive while the lower is inclusive.
        // Curvature radius can not be changed as long as AC2F is active.
        var gainChoice = Randoms.Next(0, (_experimentDataManager._redirectionManager._currentActiveRedirectionAlgorithmType == RedirectionAlgorithms.AC2F) ? 2 : 3);
        var newGain = 0f;
        if (gainChoice == 0)
        {
            newGain = Mathf.Clamp(_experimentDataManager._redirectionManager.MIN_ROT_GAIN - _rotationGainBaseIncrement + UtilitiesER.Remap(0, 1, -_rotationGainIncrementNoise, _rotationGainIncrementNoise, (float)Randoms.NextDouble()), _maximumNegativeGain, 0);
            _experimentDataManager._redirectionManager.MIN_ROT_GAIN = newGain;
        }
        else if (gainChoice == 1)
        {
            newGain = Mathf.Clamp(_experimentDataManager._redirectionManager.MAX_ROT_GAIN + _rotationGainBaseIncrement + UtilitiesER.Remap(0, 1, -_rotationGainIncrementNoise, _rotationGainIncrementNoise, (float)Randoms.NextDouble()), 0, _maximumPositiveGain);
            _experimentDataManager._redirectionManager.MAX_ROT_GAIN = newGain;
        }
        else
        {
            newGain = Mathf.Clamp(_experimentDataManager._redirectionManager.CURVATURE_RADIUS - _curvatureRadiusBaseIncrement + UtilitiesER.Remap(0, 1, -_curvatureRadiusIncrementNoise, _curvatureRadiusIncrementNoise, (float)Randoms.NextDouble()), _minimumCurvatureRadius, 1000);
            _experimentDataManager._redirectionManager.CURVATURE_RADIUS = newGain;
        }
    }

    private void GenerateRandomTimeStep()
    {
        _currentTimestep = _timeStepBase + UtilitiesER.Remap(0, 1, -_timeStepNoise, _timeStepNoise, (float)Randoms.NextDouble());
    }
}
