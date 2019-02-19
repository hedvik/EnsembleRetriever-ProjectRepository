using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "Quiz", menuName = "EnsembleRetriever/Quiz")]
public class MultipleChoiceQuiz : ScriptableObject
{
    [TextArea]
    public string _startQuizText;

    [TextArea]
    public string _endQuizAllCorrectText;

    [TextArea]
    public string _endQuizSomeCorrectText;

    [TextArea]
    public string _endQuizNoCorrectText;

    public List<Question> _questions = new List<Question>();
    public Sprite _correctAnswerIcon;
    public Sprite _wrongAnswerIcon;

    [System.Serializable]
    public class Question
    {
        [TextArea]
        public string _questionText;
        public MultipleChoiceAnswer[] _answers = new MultipleChoiceAnswer[4];
        public int _correctAnswerID;
    }

    [System.Serializable]
    public class MultipleChoiceAnswer
    {
        [TextArea]
        public string _text = "";
    }
}
