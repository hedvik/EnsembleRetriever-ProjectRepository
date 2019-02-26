using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public enum ExperimentType { none = 0, detection, effectiveness}

[System.Serializable]
public class IngameScoreData
{
    public int _id;
    public int _timeScore;
    public int _damageScore;
    public int _quizScore;
    public int _totalScore;
}

public class ExperimentDataManager : MonoBehaviour
{
    public ExperimentType _experimentType;
    public string _gameScoreFileName = "GameScoresEx1.dat";

    [HideInInspector]
    public int _currentParticipantId = 0;
    [HideInInspector]
    public List<int> _previousGameScores = new List<int>();

    private GameManager _gameManager;

    private void Start()
    {
        _gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        AcquireNewID();
    }

    public void WriteGamePerformanceToFile(IngameScoreData data)
    {
        if (File.Exists(Application.dataPath + "/" + _gameScoreFileName))
        {
            // Append
            using (var writer = File.AppendText(Application.dataPath + "/" + _gameScoreFileName))
            {
                var column1 = _currentParticipantId.ToString();
                var column2 = data._timeScore.ToString();
                var column3 = data._damageScore.ToString();
                var column4 = data._quizScore.ToString();
                var column5 = data._totalScore.ToString();
                var line = string.Format("{0},{1},{2},{3},{4}", column1, column2, column3, column4, column5);
                writer.WriteLine(line);
                writer.Flush();
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

    public void CancelExperiment()
    {
        // TODO: Fetch and write current data to file. Ingame scores will be N/A
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
