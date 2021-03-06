﻿using MersenneTwister;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using Valve.VR;

public enum ExperimentType { none = -1, detection, effectiveness }
public enum RedirectionAlgorithms { S2C = 0, AC2F }
public enum RecordedGainTypes { none = -1, rotationAgainstHead, rotationWithHead, curvature }
public enum DistractorType { none = 0, contrabass, oboe, harpsichord, violin, glockenspiel }

#region Serialized Data Classes
[System.Serializable]
public class IngameScoreData
{
    public int _id;
    public int _timeScore;
    public int _damageScore;
    public int _quizScore;
    public int _totalScore;
}

// Recorded per frame
[System.Serializable]
public class DetectionFrameData
{
    public int _id;
    public bool _gainDetected;

    public Vector3 _deltaPos;
    public float _deltaDir;
    public float _deltaTime;

    public bool _inReset;

    public RedirectionAlgorithms _currentActiveAlgorithm;
    public DistractorType _currentActiveDistractor;

    public RecordedGainTypes _currentlyAppliedGain;
    public float _currentRotationGainAgainst;
    public float _currentRotationGainWith;
    public float _currentCurvatureGain;

    public float _noGainRatioAtDetection = 0;
    public float _negativeRotationGainRatioAtDetection = 0;
    public float _positiveRotationGainRatioAtDetection = 0;
    public float _curvatureGainRatioAtDetection = 0;

    public float _timeSinceStart = 0;
    public float _mostLikelyDetectedGainValue = 0;
    public RecordedGainTypes _mostLikelyDetectedGainType;
}

[System.Serializable]
public class EffectivenessFrameData
{
    public int _id = 0;
    // Either 0 for control or 1 for experiment group
    public int _experimentGroup = 0;

    public float _timeSinceStart = 0f;
    public float _timeSpentWalking = 0f;
    public int _numberOfResets = 0;
    public int _numberOfDistractors = 0;

    public bool _resetActive = false;
    public bool _distractorActive = false;
    public DistractorType _currentActiveDistractor = DistractorType.none;

    public bool _alignmentComplete = false;

    // The booleans are used to find events where we can stop counting time at
    public bool _alignedThisFrame = false;
    public float _timeTakenUntilAlignment = 0f;
    public bool _defeatedDistractorThisFrame = false;
    public float _timeTakenUntilDistractorDefeat = 0f;

    public int _currentPlayerShieldLevel = 0;
    public int _currentPlayerBatonLevel = 0;

    public Vector3 _deltaPos;
    public float _deltaDir = 0f;
    public float _deltaTime = 0f;
}
#endregion

/// <summary>
/// Management class for data collection.
/// </summary>
public class ExperimentDataManager : MonoBehaviour
{
    public ExperimentType _experimentType;
    public string _gameScoreFileName = "GameScoresEx1.csv";
    public string _detectionDataFileName = "DetectionDataEx1.csv";
    public string _effectivenessDataFileName = "EffectivenessDataEx2.csv";

    [Header("Experiment 1 Specific")]
    public float _gainRatioSampleWindowInSeconds = 0.5f;
    public int _samplesPerSecond = 90;

    [Header("Experiment 2 Specific")]
    public MountainKing _mountainKing;

    [HideInInspector]
    public int _currentParticipantId = 0;
    [HideInInspector]
    public List<int> _previousGameScores = new List<int>();
    [HideInInspector]
    public CircularBuffer.CircularBuffer<RecordedGainTypes> _appliedGainsTimeSample;
    [HideInInspector]
    public RedirectionManagerER _redirectionManager;
    [HideInInspector]
    public PlayerManager _playerManager;

    private GameManager _gameManager;
    private List<DetectionFrameData> _detectionFrameData = new List<DetectionFrameData>();
    private List<EffectivenessFrameData> _effectivenessFrameData = new List<EffectivenessFrameData>();
    private GainIncrementer _gainIncrementer;

    private bool _recordingActive = false;
    private float _sampleTimer = 0f;
    private float _timeStarted = 0f;
    private bool _gainDetected = false;

    private int _experiment2Group = -1;
    private float _timeSpentWalking = 0f;
    private int _numberOfResets = 0;
    private int _numberOfDistractors = 0;
    private float _distractorTriggerTime = -1f;
    private float _timeTakenForAlignment = -1f;
    private float _timeTakenToDefeatDistractor = -1f;

    private void Awake()
    {
        if(_experimentType == ExperimentType.effectiveness)
        {
            _mountainKing._health *= 0.5f;
        }
    }

    private void Start()
    {
        _gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        _playerManager = _gameManager.GetCurrentPlayerManager();
        _redirectionManager = _gameManager._redirectionManager;
        AcquireNewID();

        _appliedGainsTimeSample = new CircularBuffer.CircularBuffer<RecordedGainTypes>((int)(_samplesPerSecond * _gainRatioSampleWindowInSeconds));

        _gainIncrementer = GetComponent<GainIncrementer>();

        if(_experimentType == ExperimentType.effectiveness)
        {
            _experiment2Group = Randoms.Next(0, 2);
            Debug.Log("Experiment Group: " + (_experiment2Group == 0 ? "Control" : "Experimental"));
            _redirectionManager._switchToAC2FOnDistractor = _experiment2Group == 0 ? false : true;
            _redirectionManager.SubscribeToDistractorTriggerCallback(OnDistractorTrigger);
            _redirectionManager.SubscribeToResetTriggerCallback(OnResetTrigger);
            _redirectionManager.SubscribeToAlignmentCallback(OnAlignmentCallback);
            _redirectionManager.SubscribeToDistractorEndCallback(OnDistractorEnd);
        }
    }

    private void Update()
    {
        if (!_recordingActive)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelExperiment();
        }

        if (_experimentType == ExperimentType.detection)
        {
            RecordDetectionData();
        }

        if (_experimentType == ExperimentType.effectiveness)
        {
            RecordEffectivenessData();
        }
    }

    public void StartRecording()
    {
        _recordingActive = true;
        _timeStarted = Time.realtimeSinceStartup;

        if (_experimentType == ExperimentType.detection)
        {
            _gainIncrementer.InitialiseIncrementer();
            _gainIncrementer.ActivateGainIncrements();
            _redirectionManager._S2CRedirector.DisableDampening();
            _redirectionManager._S2CRedirector._rotationThreshold = _redirectionManager._AC2FRedirector._rotationThreshold;
        }
        else
        {
            _gainIncrementer.enabled = false;
        }
    }

    public void StopRecording()
    {
        _recordingActive = false;
        if (_experimentType == ExperimentType.detection)
        {
            _gainIncrementer.DeactivateGainIncrements();
        }
    }

    public void CancelExperiment()
    {
        _recordingActive = false;
        var incompleteData = new IngameScoreData();
        incompleteData._id = _currentParticipantId;
        incompleteData._quizScore = -1;
        incompleteData._damageScore = -1;
        incompleteData._timeScore = -1;
        incompleteData._totalScore = -1;

        WriteGamePerformanceToFile(incompleteData);

        if (_experimentType == ExperimentType.detection)
        {
            WriteDetectionPerformanceToFile();
        }
        else if (_experimentType == ExperimentType.effectiveness)
        {
            WriteEffectivenessDataToFile();
        }

        _gainIncrementer.enabled = false;
        _redirectionManager.MAX_ROT_GAIN = 0;
        _redirectionManager.MIN_ROT_GAIN = 0;
        _redirectionManager.CURVATURE_RADIUS = 1000;

        Debug.Log("Experiment Cancellation is finished!");
    }

    public void FinishDataRecording(IngameScoreData data)
    {
        WriteGamePerformanceToFile(data);

        if (_experimentType == ExperimentType.detection)
        {
            WriteDetectionPerformanceToFile();
        }
        else if (_experimentType == ExperimentType.effectiveness)
        {
            WriteEffectivenessDataToFile();
        }
    }

    #region File Read/Write
    private void WriteGamePerformanceToFile(IngameScoreData data)
    {
        if (File.Exists(Application.dataPath + "/" + _gameScoreFileName))
        {
            // Append
            using (var appender = File.AppendText(Application.dataPath + "/" + _gameScoreFileName))
            {
                var column1 = _currentParticipantId.ToString();
                var column2 = data._timeScore.ToString();
                var column3 = data._damageScore.ToString();
                var column4 = data._quizScore.ToString();
                var column5 = data._totalScore.ToString();
                var line = string.Format("{0},{1},{2},{3},{4}", column1, column2, column3, column4, column5);
                appender.WriteLine(line);
                appender.Flush();
            }
        }
        else
        {
            // Write
            using (var writer = new StreamWriter(Application.dataPath + "/" + _gameScoreFileName))
            {
                var column1 = "ParticipantID";
                var column2 = "TimeScore";
                var column3 = "DamageScore";
                var column4 = "QuizScore";
                var column5 = "TotalScore";
                var line = string.Format("{0},{1},{2},{3},{4}", column1, column2, column3, column4, column5);
                writer.WriteLine(line);
                writer.Flush();

                column1 = _currentParticipantId.ToString();
                column2 = data._timeScore.ToString();
                column3 = data._damageScore.ToString();
                column4 = data._quizScore.ToString();
                column5 = data._totalScore.ToString();
                line = string.Format("{0},{1},{2},{3},{4}", column1, column2, column3, column4, column5);
                writer.WriteLine(line);
                writer.Flush();
            }
        }
    }

    private void WriteDetectionPerformanceToFile()
    {
        if (File.Exists(Application.dataPath + "/" + _detectionDataFileName))
        {
            // Append
            using (var appender = File.AppendText(Application.dataPath + "/" + _detectionDataFileName))
            {
                string column1, column2, column3, column4, column5, column6, column7, column8, column9, column10,
                       column11, column12, column13, column14, column15, column16, column17, column18, column19, column20,
                       column21, column22, column23, column24, column25, column26, column27, column28, column29, column30, column31, line;
                foreach (var frame in _detectionFrameData)
                {
                    column1 = frame._id.ToString();
                    column2 = (frame._gainDetected ? 1 : 0).ToString();
                    column3 = frame._deltaPos.magnitude.ToString(CultureInfo.InvariantCulture);
                    column4 = frame._deltaDir.ToString(CultureInfo.InvariantCulture);
                    column5 = frame._deltaTime.ToString(CultureInfo.InvariantCulture);
                    column6 = (frame._inReset ? 1 : 0).ToString();

                    column7 = (frame._currentActiveAlgorithm == RedirectionAlgorithms.S2C ? 1 : 0).ToString();
                    column8 = (frame._currentActiveAlgorithm == RedirectionAlgorithms.AC2F ? 1 : 0).ToString();

                    column9 = (frame._currentActiveDistractor == DistractorType.none ? 1 : 0).ToString();
                    column10 = (frame._currentActiveDistractor == DistractorType.contrabass ? 1 : 0).ToString();
                    column11 = (frame._currentActiveDistractor == DistractorType.oboe ? 1 : 0).ToString();
                    column12 = (frame._currentActiveDistractor == DistractorType.harpsichord ? 1 : 0).ToString();
                    column13 = (frame._currentActiveDistractor == DistractorType.violin ? 1 : 0).ToString();
                    column14 = (frame._currentActiveDistractor == DistractorType.glockenspiel ? 1 : 0).ToString();

                    column15 = (frame._currentlyAppliedGain == RecordedGainTypes.none ? 1 : 0).ToString();
                    column16 = (frame._currentlyAppliedGain == RecordedGainTypes.rotationAgainstHead ? 1 : 0).ToString();
                    column17 = (frame._currentlyAppliedGain == RecordedGainTypes.rotationWithHead ? 1 : 0).ToString();
                    column18 = (frame._currentlyAppliedGain == RecordedGainTypes.curvature ? 1 : 0).ToString();

                    column19 = frame._currentRotationGainAgainst.ToString(CultureInfo.InvariantCulture);
                    column20 = frame._currentRotationGainWith.ToString(CultureInfo.InvariantCulture);
                    column21 = frame._currentCurvatureGain.ToString(CultureInfo.InvariantCulture);

                    column22 = frame._noGainRatioAtDetection.ToString("F3", CultureInfo.InvariantCulture);
                    column23 = frame._negativeRotationGainRatioAtDetection.ToString("F3", CultureInfo.InvariantCulture);
                    column24 = frame._positiveRotationGainRatioAtDetection.ToString("F3", CultureInfo.InvariantCulture);
                    column25 = frame._curvatureGainRatioAtDetection.ToString("F3", CultureInfo.InvariantCulture);

                    column26 = frame._mostLikelyDetectedGainValue.ToString(CultureInfo.InvariantCulture);
                    column27 = frame._timeSinceStart.ToString(CultureInfo.InvariantCulture);

                    column28 = ((int)frame._currentActiveAlgorithm).ToString();
                    column29 = ((int)frame._currentlyAppliedGain).ToString();
                    column30 = ((int)frame._currentActiveDistractor).ToString();
                    column31 = ((int)frame._mostLikelyDetectedGainType).ToString();

                    line = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22},{23},{24},{25},{26},{27},{28},{29},{30}", column1, column2, column3, column4, column5, column6, column7, column8, column9, column10, column11, column12, column13, column14, column15, column16, column17, column18, column19, column20, column21, column22, column23, column24, column25, column26, column27, column28, column29, column30, column31);
                    appender.WriteLine(line);
                    appender.Flush();
                }
            }
        }
        else
        {
            // Write
            using (var writer = new StreamWriter(Application.dataPath + "/" + _detectionDataFileName))
            {
                var column1 = "ParticipantID";
                var column2 = "GainDetected";
                var column3 = "DeltaPosMagnitude";
                var column4 = "DeltaDir";
                var column5 = "DeltaTime";
                var column6 = "ResetActive";

                var column7 = "S2CActive";
                var column8 = "AC2FActive";

                var column9 = "NoDistractorActive";
                var column10 = "ContrabassDistractorActive";
                var column11 = "OboeDistractorActive";
                var column12 = "HarpsichordDistractorActive";
                var column13 = "ViolinDistractorActive";
                var column14 = "GlockenspielDistractorActive";

                var column15 = "NoGainApplied";
                var column16 = "NegativeRotationGainApplied";
                var column17 = "PositiveRotationGainApplied";
                var column18 = "CurvatureGainApplied";

                var column19 = "CurrentRotationGainAgainst";
                var column20 = "CurrentRotationGainWith";
                var column21 = "CurrentCurvatureGainRadius";

                var column22 = "NoGainRatioDuringDetection";
                var column23 = "NegativeRotationGainRatioDuringDetection";
                var column24 = "PositiveRotationGainRatioDuringDetection";
                var column25 = "CurvatureGainRatioDuringDetection";

                var column26 = "MostLikelyDetectedGain";
                var column27 = "TimeSinceExperimentStart";

                var column28 = "AlgorithmCategory";
                var column29 = "AppliedGainCategory";
                var column30 = "DistractorCategory";
                var column31 = "MostLikelyDetectedGainCategory";

                var line = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22},{23},{24},{25},{26},{27},{28},{29},{30}", column1, column2, column3, column4, column5, column6, column7, column8, column9, column10, column11, column12, column13, column14, column15, column16, column17, column18, column19, column20, column21, column22, column23, column24, column25, column26, column27, column28, column29, column30, column31);
                writer.WriteLine(line);
                writer.Flush();

                foreach (var frame in _detectionFrameData)
                {
                    column1 = frame._id.ToString();
                    column2 = (frame._gainDetected ? 1 : 0).ToString();
                    column3 = frame._deltaPos.magnitude.ToString(CultureInfo.InvariantCulture);
                    column4 = frame._deltaDir.ToString(CultureInfo.InvariantCulture);
                    column5 = frame._deltaTime.ToString(CultureInfo.InvariantCulture);
                    column6 = (frame._inReset ? 1 : 0).ToString();

                    column7 = (frame._currentActiveAlgorithm == RedirectionAlgorithms.S2C ? 1 : 0).ToString();
                    column8 = (frame._currentActiveAlgorithm == RedirectionAlgorithms.AC2F ? 1 : 0).ToString();

                    column9 = (frame._currentActiveDistractor == DistractorType.none ? 1 : 0).ToString();
                    column10 = (frame._currentActiveDistractor == DistractorType.contrabass ? 1 : 0).ToString();
                    column11 = (frame._currentActiveDistractor == DistractorType.oboe ? 1 : 0).ToString();
                    column12 = (frame._currentActiveDistractor == DistractorType.harpsichord ? 1 : 0).ToString();
                    column13 = (frame._currentActiveDistractor == DistractorType.violin ? 1 : 0).ToString();
                    column14 = (frame._currentActiveDistractor == DistractorType.glockenspiel ? 1 : 0).ToString();

                    column15 = (frame._currentlyAppliedGain == RecordedGainTypes.none ? 1 : 0).ToString();
                    column16 = (frame._currentlyAppliedGain == RecordedGainTypes.rotationAgainstHead ? 1 : 0).ToString();
                    column17 = (frame._currentlyAppliedGain == RecordedGainTypes.rotationWithHead ? 1 : 0).ToString();
                    column18 = (frame._currentlyAppliedGain == RecordedGainTypes.curvature ? 1 : 0).ToString();

                    column19 = frame._currentRotationGainAgainst.ToString(CultureInfo.InvariantCulture);
                    column20 = frame._currentRotationGainWith.ToString(CultureInfo.InvariantCulture);
                    column21 = frame._currentCurvatureGain.ToString(CultureInfo.InvariantCulture);

                    column22 = frame._noGainRatioAtDetection.ToString("F3", CultureInfo.InvariantCulture);
                    column23 = frame._negativeRotationGainRatioAtDetection.ToString("F3", CultureInfo.InvariantCulture);
                    column24 = frame._positiveRotationGainRatioAtDetection.ToString("F3", CultureInfo.InvariantCulture);
                    column25 = frame._curvatureGainRatioAtDetection.ToString("F3", CultureInfo.InvariantCulture);

                    column26 = frame._mostLikelyDetectedGainValue.ToString(CultureInfo.InvariantCulture);
                    column27 = frame._timeSinceStart.ToString(CultureInfo.InvariantCulture);

                    column28 = ((int)frame._currentActiveAlgorithm).ToString();
                    column29 = ((int)frame._currentlyAppliedGain).ToString();
                    column30 = ((int)frame._currentActiveDistractor).ToString();
                    column31 = ((int)frame._mostLikelyDetectedGainType).ToString();

                    line = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22},{23},{24},{25},{26},{27},{28},{29},{30}", column1, column2, column3, column4, column5, column6, column7, column8, column9, column10, column11, column12, column13, column14, column15, column16, column17, column18, column19, column20, column21, column22, column23, column24, column25, column26, column27, column28, column29, column30, column31);
                    writer.WriteLine(line);
                    writer.Flush();
                }
            }
        }
    }

    private void WriteEffectivenessDataToFile()
    {
        if (File.Exists(Application.dataPath + "/" + _effectivenessDataFileName))
        {
            // Append
            using (var appender = File.AppendText(Application.dataPath + "/" + _effectivenessDataFileName))
            {
                string column1, column2, column3, column4, column5, column6, column7, column8, column9, 
                       column10, column11, column12, column13, column14, column15, column16, column17, column18, column19;

                foreach (var frame in _effectivenessFrameData)
                {
                    column1 = frame._id.ToString();
                    column2 = frame._experimentGroup.ToString();
                    column3 = frame._timeSinceStart.ToString("F3", CultureInfo.InvariantCulture);
                    column4 = frame._timeSpentWalking.ToString("F3", CultureInfo.InvariantCulture);
                    column5 = frame._numberOfResets.ToString();
                    column6 = frame._numberOfDistractors.ToString();
                    column7 = (frame._resetActive ? 1 : 0).ToString();
                    column8 = (frame._distractorActive ? 1 : 0).ToString();
                    column9 = ((int)frame._currentActiveDistractor).ToString();
                    column10 = (frame._alignmentComplete ? 1 : 0).ToString();
                    column11 = (frame._alignedThisFrame ? 1 : 0).ToString();
                    column12 = frame._timeTakenUntilAlignment.ToString("F3", CultureInfo.InvariantCulture);
                    column13 = (frame._defeatedDistractorThisFrame ? 1 : 0).ToString();
                    column14 = frame._timeTakenUntilDistractorDefeat.ToString("F3", CultureInfo.InvariantCulture);
                    column15 = frame._currentPlayerShieldLevel.ToString();
                    column16 = frame._currentPlayerBatonLevel.ToString();

                    column17 = frame._deltaPos.magnitude.ToString(CultureInfo.InvariantCulture);
                    column18 = frame._deltaDir.ToString(CultureInfo.InvariantCulture);
                    column19 = frame._deltaTime.ToString(CultureInfo.InvariantCulture);

                    var line = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18}", column1, column2, column3, column4, column5, column6, column7, column8, column9, column10, column11, column12, column13, column14, column15, column16, column17, column18, column19);
                    appender.WriteLine(line);
                    appender.Flush();
                }
            }
        }
        else
        {
            // Write
            using (var writer = new StreamWriter(Application.dataPath + "/" + _effectivenessDataFileName))
            {
                var column1 = "ParticipantID";
                var column2 = "GroupID";
                var column3 = "TimeSinceExperimentStart";
                var column4 = "TimeSpentWalking";
                var column5 = "NumberOfResets";
                var column6 = "NumberOfDistractors";
                var column7 = "IsResetActive";
                var column8 = "IsDistractorActive";
                var column9 = "CurrentlyActiveDistractorType";
                var column10 = "AlignmentComplete";
                var column11 = "AlignedThisFrame";
                var column12 = "TimeTakenUntilAlignment";
                var column13 = "DistractorDefeatedThisFrame";
                var column14 = "TimeTakenToDefeatDistractor";
                var column15 = "CurrentPlayerShieldLevel";
                var column16 = "CurrentPlayerBatonLevel";
                var column17 = "DeltaPos";
                var column18 = "DeltaDir";
                var column19 = "DeltaTime";

                var line = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18}", column1, column2, column3, column4, column5, column6, column7, column8, column9, column10, column11, column12, column13, column14, column15, column16, column17, column18, column19);
                writer.WriteLine(line);
                writer.Flush();

                foreach (var frame in _effectivenessFrameData)
                {
                    column1 = frame._id.ToString();
                    column2 = frame._experimentGroup.ToString();
                    column3 = frame._timeSinceStart.ToString("F3", CultureInfo.InvariantCulture);
                    column4 = frame._timeSpentWalking.ToString("F3", CultureInfo.InvariantCulture);
                    column5 = frame._numberOfResets.ToString();
                    column6 = frame._numberOfDistractors.ToString();
                    column7 = (frame._resetActive ? 1 : 0).ToString();
                    column8 = (frame._distractorActive ? 1 : 0).ToString();
                    column9 = ((int)frame._currentActiveDistractor).ToString();
                    column10 = (frame._alignmentComplete ? 1 : 0).ToString();
                    column11 = (frame._alignedThisFrame ? 1 : 0).ToString();
                    column12 = frame._timeTakenUntilAlignment.ToString("F3", CultureInfo.InvariantCulture);
                    column13 = (frame._defeatedDistractorThisFrame ? 1 : 0).ToString();
                    column14 = frame._timeTakenUntilDistractorDefeat.ToString("F3", CultureInfo.InvariantCulture);
                    column15 = frame._currentPlayerShieldLevel.ToString();
                    column16 = frame._currentPlayerBatonLevel.ToString();

                    column17 = frame._deltaPos.magnitude.ToString(CultureInfo.InvariantCulture);
                    column18 = frame._deltaDir.ToString(CultureInfo.InvariantCulture);
                    column19 = frame._deltaTime.ToString(CultureInfo.InvariantCulture);

                    line = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18}", column1, column2, column3, column4, column5, column6, column7, column8, column9, column10, column11, column12, column13, column14, column15, column16, column17, column18, column19);
                    writer.WriteLine(line);
                    writer.Flush();
                }
            }
        }
    }

    private void AcquireNewID()
    {
        if (!File.Exists(Application.dataPath + "/" + _gameScoreFileName))
        {
            _currentParticipantId = 0;
            return;
        }

        using (var reader = new StreamReader(Application.dataPath + "/" + _gameScoreFileName))
        {
            var list1 = new List<string>();
            var list2 = new List<string>();
            var list3 = new List<string>();
            var list4 = new List<string>();
            var list5 = new List<string>();
            reader.ReadLine();

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var values = line.Split(',');

                list1.Add(values[0]);
                list2.Add(values[1]);
                list3.Add(values[2]);
                list4.Add(values[3]);
                list5.Add(values[4]);
            }

            _currentParticipantId = int.Parse(list1[list1.Count - 1]) + 1;
            _previousGameScores = list5.Select(int.Parse).ToList();
        }
    }
    #endregion

    #region Data Recording
    private void RecordDetectionData()
    {
        _gainDetected = false;
        var newData = new DetectionFrameData();
        newData._id = _currentParticipantId;
#if UNITY_EDITOR
        if (SteamVR.active && SteamVR_Actions._default.MenuButton.GetStateDown(_playerManager._shieldHand) || Input.GetKeyDown(KeyCode.E))
        {
            _gainDetected = true;
            Debug.Log("Detection!");
        }
#else
        if(SteamVR.active && SteamVR_Actions._default.MenuButton.GetStateDown(_playerManager._shieldHand))
        {
            _gainDetected = true;
        }
#endif
        newData._gainDetected = _gainDetected;
        newData._deltaPos = _redirectionManager.deltaPos;
        newData._deltaDir = _redirectionManager.deltaDir;
        newData._deltaTime = Time.deltaTime;
        newData._inReset = _redirectionManager.inReset;
        newData._currentActiveAlgorithm = _redirectionManager._currentActiveRedirectionAlgorithmType;
        newData._currentActiveDistractor = _redirectionManager._currentActiveDistractor != null ? _redirectionManager._currentActiveDistractor._distractorType : DistractorType.none;
        newData._currentlyAppliedGain = _redirectionManager.redirector._currentlyAppliedGainType;
        newData._currentRotationGainAgainst = _redirectionManager.MIN_ROT_GAIN;
        newData._currentRotationGainWith = _redirectionManager.MAX_ROT_GAIN;
        newData._currentCurvatureGain = _redirectionManager.CURVATURE_RADIUS;
        newData._timeSinceStart = Time.realtimeSinceStartup - _timeStarted;

        _sampleTimer += Time.deltaTime;
        if (_sampleTimer >= _gainRatioSampleWindowInSeconds / _samplesPerSecond)
        {
            _sampleTimer -= _gainRatioSampleWindowInSeconds / _samplesPerSecond;
            _appliedGainsTimeSample.PushFront(newData._currentlyAppliedGain);
        }

        newData._mostLikelyDetectedGainType = RecordedGainTypes.none;
        if (newData._gainDetected)
        {
            var noGainFrequency = 0;
            var negativeRotationGainFrequency = 0;
            var positiveRotationGainFrequency = 0;
            var curvatureGainFrequency = 0;
            foreach (var sampledGain in _appliedGainsTimeSample)
            {
                switch (sampledGain)
                {
                    case RecordedGainTypes.none: noGainFrequency++; break;
                    case RecordedGainTypes.rotationAgainstHead: negativeRotationGainFrequency++; break;
                    case RecordedGainTypes.rotationWithHead: positiveRotationGainFrequency++; break;
                    case RecordedGainTypes.curvature: curvatureGainFrequency++; break;
                }
            }

            newData._noGainRatioAtDetection = (float)noGainFrequency / _appliedGainsTimeSample.Size;
            newData._negativeRotationGainRatioAtDetection = (float)negativeRotationGainFrequency / _appliedGainsTimeSample.Size;
            newData._positiveRotationGainRatioAtDetection = (float)positiveRotationGainFrequency / _appliedGainsTimeSample.Size;
            newData._curvatureGainRatioAtDetection = (float)curvatureGainFrequency / _appliedGainsTimeSample.Size;

            // The gain that has the highest ratio/percentage within the short time sample is considered as the one that was detected. 
            var frequencyList = new List<int>();
            frequencyList.Add(negativeRotationGainFrequency);
            frequencyList.Add(positiveRotationGainFrequency);
            frequencyList.Add(curvatureGainFrequency);
            frequencyList.Add(noGainFrequency);
            frequencyList.Sort();
            frequencyList.Reverse();

            if (frequencyList[0] == negativeRotationGainFrequency)
            {
                newData._mostLikelyDetectedGainValue = _redirectionManager.MIN_ROT_GAIN;
                newData._mostLikelyDetectedGainType = RecordedGainTypes.rotationAgainstHead;
            }
            else if (frequencyList[0] == positiveRotationGainFrequency)
            {
                newData._mostLikelyDetectedGainValue = _redirectionManager.MAX_ROT_GAIN;
                newData._mostLikelyDetectedGainType = RecordedGainTypes.rotationWithHead;
            }
            else if (frequencyList[0] == curvatureGainFrequency)
            {
                newData._mostLikelyDetectedGainValue = _redirectionManager.CURVATURE_RADIUS;
                newData._mostLikelyDetectedGainType = RecordedGainTypes.curvature;
            }
            else
            {
                newData._mostLikelyDetectedGainValue = 0;
            }

            frequencyList.Clear();
        }

        _detectionFrameData.Add(newData);

        // Data collection should be finished before resetting any gains
        if (_gainDetected)
        {
            _gainIncrementer.Reset(newData._mostLikelyDetectedGainType);
        }
    }

    private void RecordEffectivenessData()
    {
        var newData = new EffectivenessFrameData();
        newData._id = _currentParticipantId;
        newData._experimentGroup = _experiment2Group;
        newData._timeSinceStart = Time.realtimeSinceStartup - _timeStarted;

        newData._deltaPos = _redirectionManager.deltaPos;
        newData._deltaDir = _redirectionManager.deltaDir;
        newData._deltaTime = Time.deltaTime;

        if (!_redirectionManager._distractorIsActive)
        {
            _timeSpentWalking += Time.deltaTime;
        }

        newData._timeSpentWalking = _timeSpentWalking;
        newData._numberOfResets = _numberOfResets;
        newData._numberOfDistractors = _numberOfDistractors;
        newData._resetActive = _redirectionManager.inReset;
        newData._distractorActive = _redirectionManager._distractorIsActive;
        newData._currentActiveDistractor = _redirectionManager._currentActiveDistractor != null ? _redirectionManager._currentActiveDistractor._distractorType : DistractorType.none;
        newData._alignmentComplete = (_redirectionManager._currentActiveDistractor != null && _redirectionManager._alignmentComplete) ? true : false;

        newData._currentPlayerShieldLevel = _playerManager._currentShieldLevel;
        newData._currentPlayerBatonLevel = _playerManager._currentBatonLevel;

        if(_timeTakenForAlignment > 0)
        {
            newData._alignedThisFrame = true;
            newData._timeTakenUntilAlignment = _timeTakenForAlignment;
            _timeTakenForAlignment = -1f;
        }

        if(_timeTakenToDefeatDistractor > 0)
        {
            newData._defeatedDistractorThisFrame = true;
            newData._timeTakenUntilDistractorDefeat = _timeTakenToDefeatDistractor;
            _timeTakenToDefeatDistractor = -1f;
        }

        _effectivenessFrameData.Add(newData);
    }
    #endregion

    #region Callbacks
    public void OnResetTrigger()
    {
        _numberOfResets++;
    }

    public void OnDistractorTrigger()
    {
        _numberOfDistractors++;
        _distractorTriggerTime = Time.realtimeSinceStartup;
    }

    public void OnDistractorEnd()
    {
        _timeTakenToDefeatDistractor = Time.realtimeSinceStartup - _distractorTriggerTime;
    }

    public void OnAlignmentCallback()
    {
        _timeTakenForAlignment = Time.realtimeSinceStartup - _distractorTriggerTime;
    }
    #endregion
}
