using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CaveQuizManager : MonoBehaviour
{
    [Header("Quiz Related")]
    public Text _quizTextBox;
    public List<Text> _answerBoxes = new List<Text>();
    public List<Image> _correctnessDisplays = new List<Image>();
    public GameObject _quizNPC;
    public float _waitTimeAfterQuizIsFinished = 4f;

    [Header("On Quiz Completion Data")]
    public Transform _movableStage;
    public GameObject _stageMovementParticles;
    public AudioLowPassFilter _lowPassAudioFilter;

    public float _stageMovementSpeed = 1f;
    public float _stageShakeSpeed = 1f;
    public float _stageShakeAmount = 1f;
    public float _stageMovementYOffset = -2f;

    [HideInInspector]
    public int _numberOfCorrectAnswers = 0;

    private MultipleChoiceQuiz _quizData;
    private GameObject _quizContainer;
    private GameManager _gameManager;
    private PlayerManager _playerManager;
    private bool _quizActive = false;
    private float _quizTimer = 0f;
    private int _currentQuestionIndex = 0;

    private AudioSource _quizAudioSource;

    private void Start()
    {
        _quizData = Resources.Load<MultipleChoiceQuiz>("ScriptableObjects/Dialogue/Quiz/Quiz");
        _stageMovementParticles.SetActive(false);
        _quizContainer = _quizTextBox.transform.parent.parent.parent.gameObject;
        _quizContainer.SetActive(false);

        _quizTextBox.text = _quizData._startQuizText;
        _gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        _playerManager = _gameManager.GetCurrentPlayerManager();

        _quizAudioSource = _quizNPC.GetComponent<AudioSource>();

        _gameManager._uiManager.ChangeTextBoxVisibility(false, _answerBoxes[0].transform.parent.parent);
    }

    private void Update()
    {
        if (!_quizActive)
        {
            return;
        }

        // This is primarily a fail safe if the player does not hit anything
        _quizTimer += Time.deltaTime;
        if (_quizTimer >= 1f)
        {
            _quizTimer -= 1f;
            _playerManager.AddCharge(100, false);
        }
    }

    public void StartQuiz()
    {
        UpdateQuizText(0);
        _gameManager._uiManager.ChangeTextBoxVisibility(true, _answerBoxes[0].transform.parent.parent);
        _quizContainer.GetComponent<LookAtPlayer>().enabled = true;
        _quizActive = true;
    }

    public void FinishQuiz()
    {
        _quizActive = false;
        _gameManager._uiManager.ChangeTextBoxVisibility(false, _answerBoxes[0].transform.parent.parent);
        _quizContainer.GetComponent<LookAtPlayer>().enabled = false;
        if (_numberOfCorrectAnswers == _quizData._questions.Count)
        {
            _quizTextBox.text = _quizData._endQuizAllCorrectText;
            _quizAudioSource.PlayOneShot(_quizData._endQuizAllCorrectAudio);
        } 
        else if (_numberOfCorrectAnswers == 0)
        {
            _quizTextBox.text = _quizData._endQuizNoCorrectText;
            _quizAudioSource.PlayOneShot(_quizData._endQuizNoCorrectAudio);
        }
        else
        {
            _quizTextBox.text = _quizData._endQuizSomeCorrectText;
            _quizAudioSource.PlayOneShot(_quizData._endQuizSomeCorrectAudio);
        }

        StartCoroutine(FinishWaiting());
    }

    public void GiveQuizAnswer(Collider hitCollider)
    {
        StartCoroutine(AnswerGivenAnimation(hitCollider.transform));
        var isAnswerCorrect = CheckAnswer(hitCollider);
        _numberOfCorrectAnswers += isAnswerCorrect ? 1 : 0;
        _correctnessDisplays[_currentQuestionIndex].sprite = (isAnswerCorrect ? _quizData._correctAnswerIcon : _quizData._wrongAnswerIcon);
        _correctnessDisplays[_currentQuestionIndex].gameObject.SetActive(true);

        if (_currentQuestionIndex + 1 < _quizData._questions.Count)
        {
            UpdateQuizText(_currentQuestionIndex + 1);
        }
        else
        {
            FinishQuiz();
        }
    }

    public void SetVisibilityState(bool state)
    {
        _quizContainer.SetActive(state);
    }

    public void RemoveMovableStage()
    {
        _stageMovementParticles.SetActive(true);
        StartCoroutine(RemoveStageAnimation());
    }

    private bool CheckAnswer(Collider hitCollider)
    {
        for (int i = 0; i < _answerBoxes.Count; i++)
        {
            if (_answerBoxes[i].transform.parent.gameObject == hitCollider.gameObject)
            {
                return (i == _quizData._questions[_currentQuestionIndex]._correctAnswerID);
            }
        }
        return false;
    }

    private void UpdateQuizText(int questionIndex)
    {
        _currentQuestionIndex = questionIndex;
        _playerManager.AddCharge(100, false);
        _quizTextBox.text = _quizData._questions[questionIndex]._questionText;
        for (int i = 0; i < _quizData._questions[questionIndex]._answers.Length; i++)
        {
            _answerBoxes[i].text = _quizData._questions[questionIndex]._answers[i]._text;
        }
    }

    private IEnumerator RemoveStageAnimation()
    {
        var lerpTimer = 0f;
        var startPosition = _movableStage.transform.position;
        var currentPosition = startPosition;
        var startCutoffFrequency = _lowPassAudioFilter.cutoffFrequency;
        while (lerpTimer <= 1f)
        {
            lerpTimer += Time.deltaTime * _stageMovementSpeed;
            currentPosition.y = Mathf.Lerp(startPosition.y, startPosition.y + _stageMovementYOffset, lerpTimer);
            currentPosition.z = startPosition.z + Mathf.Sin(Time.time * _stageShakeSpeed) * _stageShakeAmount;
            _movableStage.transform.position = currentPosition;
            _lowPassAudioFilter.cutoffFrequency = Mathf.Lerp(startCutoffFrequency, 5000, lerpTimer);

            yield return null;
        }

        _stageMovementParticles.SetActive(false);
        _gameManager.StartFinalBossAnimations();
    }

    private IEnumerator AnswerGivenAnimation(Transform answerTransform)
    {
        var lerpTimer = 0f;
        var sineTimer = 0f;
        var startScale = answerTransform.localScale;
        var targetScale = startScale * 0.8f;
        var currentScale = startScale;

        while (sineTimer >= 0f)
        {
            lerpTimer += Time.deltaTime * 5f;
            sineTimer = Mathf.Sin(lerpTimer);
            currentScale = Vector3.Lerp(startScale, targetScale, sineTimer);
            answerTransform.localScale = currentScale;
            yield return null;
        }
    }

    private IEnumerator FinishWaiting()
    {
        // Supposedly WaitForSeconds generates a lot of garbage so this wait approach is better
        var waitTimer = 0f;
        while (waitTimer <= _waitTimeAfterQuizIsFinished)
        {
            waitTimer += Time.deltaTime;
            yield return null;
        }

        _gameManager._uiManager.ChangeTextBoxVisibility(false, _quizNPC.transform);
        _gameManager._uiManager.ChangeTextBoxVisibility(false, _quizContainer.transform);
        RemoveMovableStage();
    }
}
