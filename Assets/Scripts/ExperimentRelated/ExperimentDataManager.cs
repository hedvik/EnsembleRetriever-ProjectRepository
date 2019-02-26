using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using Valve.VR;

public enum ExperimentType { none = 0, detection, effectiveness}
public enum RedirectionAlgorithms { S2C = 0, AC2F }
public enum RecordedGainTypes { none = -1, rotationAgainstHead, rotationWithHead, curvature }

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
public class RedirectionFrameData
{
    public int _id;
    public bool _gainDetected;
    
    public Vector3 _deltaPos;
    public float _deltaDir;
    public float _deltaTime;

    public bool _inReset;

    // It should be noted that the rest of the data below wont change during reset. 
    public RedirectionAlgorithms _currentActiveAlgorithm;
    public string _currentActiveDistractor;

    public RecordedGainTypes _currentlyAppliedGain;
    public float _currentRotationGainAgainst;
    public float _currentRotationGainWith;
    public float _currentCurvatureGain;

    //public float _injectedRotation; // the information we can glean from here is covered by _currentlyAppliedGain for now
}
#endregion

public class ExperimentDataManager : MonoBehaviour
{
    public ExperimentType _experimentType;
    public string _gameScoreFileName = "GameScoresEx1.dat";
    public string _detectionDataFileName = "DetectionDataEx1.dat";

    [HideInInspector]
    public int _currentParticipantId = 0;
    [HideInInspector]
    public List<int> _previousGameScores = new List<int>();
    [HideInInspector]
    public bool _recordingActive = false;

    private GameManager _gameManager;
    private List<RedirectionFrameData> _detectionFrameData = new List<RedirectionFrameData>();
    private PlayerManager _playerManager;
    private RedirectionManagerER _redirectionManager;

    private void Start()
    {
        _gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        _playerManager = _gameManager.GetCurrentPlayerManager();
        _redirectionManager = _gameManager._redirectionManager;
        AcquireNewID();
    }

    private void Update()
    {
        if(!_recordingActive)
        {
            return;
        }

        if(Input.GetKeyDown(KeyCode.Escape))
        {
            CancelExperiment();
        }

        var newData = new RedirectionFrameData();
        newData._id = _currentParticipantId;
        newData._gainDetected = (SteamVR.active && SteamVR_Actions._default.MenuButton.GetStateDown(_playerManager._batonHand)) ? true : false;
        newData._deltaPos = _redirectionManager.deltaPos;
        newData._deltaDir = _redirectionManager.deltaDir;
        newData._deltaTime = Time.deltaTime;
        newData._inReset = _redirectionManager.inReset;
        newData._currentActiveAlgorithm = _redirectionManager._currentActiveRedirectionAlgorithmType;
        newData._currentActiveDistractor = (_redirectionManager._distractorIsActive) ? _redirectionManager._currentActiveDistractor.name : "N/A";
        newData._currentlyAppliedGain = _redirectionManager.redirector._currentlyAppliedGainType;
        newData._currentRotationGainAgainst = _redirectionManager.MIN_ROT_GAIN;
        newData._currentRotationGainWith = _redirectionManager.MAX_ROT_GAIN;
        newData._currentCurvatureGain = _redirectionManager.CURVATURE_RADIUS;
        _detectionFrameData.Add(newData);
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
        WriteDetectionPerformanceToFile();
    }

    public void WriteGamePerformanceToFile(IngameScoreData data)
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

    public void WriteDetectionPerformanceToFile()
    {
        if (File.Exists(Application.dataPath + "/" + _detectionDataFileName))
        {
            // Append
            using (var appender = File.AppendText(Application.dataPath + "/" + _detectionDataFileName))
            {
                string column1, column2, column3, column4, column5, column6, column7, column8, column9, column10, column11, column12, line;
                foreach (var frame in _detectionFrameData)
                {
                    column1 = frame._id.ToString();
                    column2 = frame._gainDetected.ToString();
                    column3 = frame._deltaPos.magnitude.ToString(CultureInfo.InvariantCulture);
                    column4 = frame._deltaDir.ToString(CultureInfo.InvariantCulture);
                    column5 = frame._deltaTime.ToString(CultureInfo.InvariantCulture);
                    column6 = frame._inReset.ToString();
                    column7 = frame._currentActiveAlgorithm.ToString();
                    column8 = frame._currentActiveDistractor;
                    column9 = frame._currentlyAppliedGain.ToString();
                    column10 = frame._currentRotationGainAgainst.ToString(CultureInfo.InvariantCulture);
                    column11 = frame._currentRotationGainWith.ToString(CultureInfo.InvariantCulture);
                    column12 = frame._currentCurvatureGain.ToString(CultureInfo.InvariantCulture);

                    line = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11}", column1, column2, column3, column4, column5, column6, column7, column8, column9, column10, column11, column12);
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
                var column7 = "CurrentActiveRedirectionAlgorithm";
                var column8 = "CurrentActiveDistractor";
                var column9 = "CurrentlyAppliedGainType";
                var column10 = "CurrentRotationGainAgainst";
                var column11 = "CurrentRotationGainWith";
                var column12 = "CurrentCurvatureGain";

                var line = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11}", column1, column2, column3, column4, column5, column6, column7, column8, column9, column10, column11, column12);
                writer.WriteLine(line);
                writer.Flush();

                foreach (var frame in _detectionFrameData)
                {
                    column1 = frame._id.ToString();
                    column2 = frame._gainDetected.ToString();
                    column3 = frame._deltaPos.magnitude.ToString(CultureInfo.InvariantCulture);
                    column4 = frame._deltaDir.ToString(CultureInfo.InvariantCulture);
                    column5 = frame._deltaTime.ToString(CultureInfo.InvariantCulture);
                    column6 = frame._inReset.ToString();
                    column7 = frame._currentActiveAlgorithm.ToString();
                    column8 = frame._currentActiveDistractor;
                    column9 = frame._currentlyAppliedGain.ToString();
                    column10 = frame._currentRotationGainAgainst.ToString(CultureInfo.InvariantCulture);
                    column11 = frame._currentRotationGainWith.ToString(CultureInfo.InvariantCulture);
                    column12 = frame._currentCurvatureGain.ToString(CultureInfo.InvariantCulture);

                    line = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11}", column1, column2, column3, column4, column5, column6, column7, column8, column9, column10, column11, column12);
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
}
