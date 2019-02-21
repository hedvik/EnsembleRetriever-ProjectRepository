using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using UnityEngine.XR;
using System.Linq;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public bool _skipTutorial;
    public float _levelUpDialogueBoxOffsetFromPlayer = 5f;

    [Header("References")]
    public GameObject _startGameTextBox;
    public AnimatedCharacterInterface _tutorialInstrument;
    public MountainKing _mountainKing;
    public GameObject _mountainKingEnsembleContainer;
    public AudioSource _mountainKingAudioSource;
    public Text _scoreText;
    public Text _leaderboardText;

    [Header("Scoring Parameters")]
    public int _scorePotentialDamageTaken = 1000;
    public int _scoreLossPerDamageTaken = 50;
    public int _scorePotentialQuizAnswers = 500;
    public int _scorePotentialTime = 1000;
    public int _maximumTimeInMinutes = 30;

    [SerializeField]
    private GameObject _levelUpDialoguePrefab = null;

    [HideInInspector]
    public bool _gameStarted = false;

    [HideInInspector]
    public LevelUpBox _levelUpDialogueBox;

    [HideInInspector]
    public UIManager _uiManager;
    [HideInInspector]
    public RedirectionManagerER _redirectionManager;

    private Queue<DialogueSnippet> _startGameDialogue;
    private Queue<DialogueSnippet> _tutorialDialogue;

    private bool _shieldEventTriggered = false;
    private bool _resetEventTriggered = false;
    private bool _attackEventTriggered = false;
    private AnimatedCharacterInterface _mountainKingAnimatedInterface;
    private AnimatedCharacterInterface[] _ensembleInstruments;
    private CaveQuizManager _quizManager;

    private int _scoreTime = 0;
    private int _scoreDamageTaken = 0;
    private int _scoreQuizAnswers = 0;
    private float _startTime;

    private void Awake()
    {
        _uiManager = GameObject.Find("UIManager").GetComponent<UIManager>();
        _startGameDialogue = new Queue<DialogueSnippet>(Resources.LoadAll<DialogueSnippet>("ScriptableObjects/Dialogue/StartGame"));

        var tutorialDialogue = Resources.LoadAll<DialogueSnippet>("ScriptableObjects/Dialogue/Tutorial");
        _tutorialDialogue = new Queue<DialogueSnippet>(tutorialDialogue.OrderBy(c => c.name.Length).ThenBy(c => c.name));

        _uiManager.ActivateDialogue(this, typeof(GameManager).GetTypeInfo(), _startGameTextBox, _startGameDialogue);

        _redirectionManager = GameObject.Find(!XRSettings.enabled ? "Redirected Walker (Debug)" : "Redirected Walker (VR)").GetComponent<RedirectionManagerER>();
        _redirectionManager.MIN_ROT_GAIN = 0f;
        _redirectionManager.MAX_ROT_GAIN = 0f;

        // Setting curvature radius to 1000 is a rather hacky way of disabling it.
        // Curvature gains are technically disabled at infinite radius, 1000 is just an "approximation" of that.
        _redirectionManager.CURVATURE_RADIUS = 1000f;

        _levelUpDialogueBox = Instantiate(_levelUpDialoguePrefab).GetComponent<LevelUpBox>();
        _levelUpDialogueBox.gameObject.SetActive(false);
        _levelUpDialogueBox.transform.localScale = Vector3.zero;
        _mountainKing.InitialiseDistractor(_redirectionManager);
        _mountainKingAnimatedInterface = _mountainKing.GetComponent<AnimatedCharacterInterface>();
        _ensembleInstruments = _mountainKingEnsembleContainer.GetComponentsInChildren<AnimatedCharacterInterface>();

        _quizManager = GameObject.Find("CaveQuizManager").GetComponent<CaveQuizManager>();

        _uiManager.ChangeTextBoxVisibility(false, _scoreText.transform.parent.parent.parent);
        _uiManager.ChangeTextBoxVisibility(false, _leaderboardText.transform.parent.parent.parent);
    }

    public void StartTutorial()
    {
        if (!_skipTutorial)
        {
            _tutorialInstrument.gameObject.SetActive(true);
            _uiManager.ActivateDialogue(_tutorialInstrument, typeof(AnimatedCharacterInterface).GetTypeInfo(), _tutorialInstrument.transform.Find("TextBox").gameObject, _tutorialDialogue);
            _tutorialInstrument.GetComponent<TutorialNPC>().InitialiseDistractor(_redirectionManager);
        }
        else
        {
            StartGame();
        }
    }

    public void StartGame()
    {
        _gameStarted = true;
        _redirectionManager.ActivateRotationAndCurvatureGains();
        _startTime = Time.realtimeSinceStartup;

        if (!_skipTutorial)
        {
            _tutorialInstrument.GetComponent<TutorialNPC>().FinishTutorial();
        }
    }
    
    public void StartFinalBossAnimations()
    {
        _mountainKingAudioSource.Stop();
        _mountainKingAnimatedInterface.LookAtPosition(_redirectionManager.GetUserHeadTransform().position);
        foreach(var instrument in _ensembleInstruments)
        {
            instrument.AnimationTrigger("Idle");
            instrument.LookAtPosition(_redirectionManager.GetUserHeadTransform().position);
        }

        _mountainKingAnimatedInterface.AnimationTriggerWithCallback("Surprised", FinaliseFinalBossAnimations);
    }

    public void FinaliseFinalBossAnimations()
    {
        foreach (var instrument in _ensembleInstruments)
        {
            instrument.AnimationTrigger("Jumps");
        }
        _mountainKingAnimatedInterface.AnimationTriggerWithCallback("PhaseTransition", StartFinalBoss);
    }

    public void StartFinalBoss()
    {
        _mountainKingAudioSource.Play();
        _mountainKing.StartMountainKing();
    }

    public void EndGame()
    {
        foreach(var instrument in _ensembleInstruments)
        {
            instrument._eyeRenderer.material = _mountainKing._normalEyes;
        }
        FetchScores();
        _uiManager.ChangeTextBoxVisibility(true, _scoreText.transform.parent.parent.parent);
        _uiManager.ChangeTextBoxVisibility(true, _leaderboardText.transform.parent.parent.parent);
        // TODO: Record data
    }

    public void ResetEventTriggerDialogue()
    {
        if (!_resetEventTriggered)
        {
            _uiManager.EventTriggerSnippet();
            _resetEventTriggered = true;
        }
    }

    public void ShieldEventTriggerDialogue()
    {
        if (!_shieldEventTriggered)
        {
            _uiManager.EventTriggerSnippet();
            _shieldEventTriggered = true;
        }
    }

    public void AttackEventTriggerDialogue()
    {
        if(!_attackEventTriggered)
        {
            _uiManager.EventTriggerSnippet();
            _attackEventTriggered = true;
        }
    }

    public PlayerManager GetCurrentPlayerManager()
    {
        return _redirectionManager.GetComponentInChildren<PlayerManager>();
    }

    private void FetchScores()
    {
        var currentTime = Time.realtimeSinceStartup;

        _scoreDamageTaken = Mathf.Clamp(_scorePotentialDamageTaken - (_redirectionManager._playerManager._numberOfHitsTaken * _scoreLossPerDamageTaken), 0, _scorePotentialDamageTaken);
        _scoreQuizAnswers = Mathf.Clamp(_quizManager._numberOfCorrectAnswers * (_scorePotentialQuizAnswers / _quizManager._correctnessDisplays.Count), 0, _scorePotentialQuizAnswers);
        _scoreTime = Mathf.Clamp(_scorePotentialTime - (int)UtilitiesER.Remap(0, _maximumTimeInMinutes * 60f, 0, _scorePotentialTime, currentTime - _startTime), 0, _scorePotentialTime);
        var finalScore = _scoreDamageTaken + _scoreTime + _scoreQuizAnswers;

        _scoreText.text = "Score(<color=#00ff00ff>Time Taken</color>):\n" + _scoreTime + "\n" +
                          "Score(<color=#ff0000ff>Damage Taken</color>):\n" + _scoreDamageTaken + "\n" +
                          "Score(<color=#a52a2aff>Quiz Answers</color>):\n" + _scoreQuizAnswers + "\n" +
                          "Score(<color=#ffa500ff>Total</color>):\n" + finalScore;

        // TODO: Add participant ID from current experiment
        // TODO: Write/Read scores for leaderboard
        _leaderboardText.text = "Participant ID:\nDEBUG\nScore Placement:\n1/100";
    }
}