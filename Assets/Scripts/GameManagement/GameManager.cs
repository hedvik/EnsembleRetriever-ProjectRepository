using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using UnityEngine.XR;
using System.Linq;

public class GameManager : MonoBehaviour
{
    public GameObject _startGameTextBox;
    public AnimatedCharacterInterface _tutorialInstrument;
    public bool _skipTutorial;

    [HideInInspector]
    public bool _gameStarted = false;

    private UIManager _uiManager;
    private Queue<DialogueSnippet> _startGameDialogue;
    private Queue<DialogueSnippet> _tutorialDialogue;
    private RedirectionManagerER _redirectionManager;

    private bool _shieldEventTriggered = false;

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
    }

    public void StartTutorial()
    {
        if (!_skipTutorial)
        {
            _tutorialInstrument.gameObject.SetActive(true);
            _uiManager.ActivateDialogue(_tutorialInstrument, typeof(AnimatedCharacterInterface).GetTypeInfo(), _tutorialInstrument.transform.GetChild(0).gameObject, _tutorialDialogue);
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

        if (!_skipTutorial)
        {
            _tutorialInstrument.AnimationTrigger("Leave");
        }
    }

    public void EventTriggerDialogue()
    {
        _uiManager.EventTriggerSnippet();
    }

    public void ShieldEventTriggerDialogue()
    {
        if (!_shieldEventTriggered)
        {
            _uiManager.EventTriggerSnippet();
            _shieldEventTriggered = true;
        }
    }

    public PlayerManager GetCurrentPlayerManager()
    {
        return _redirectionManager.GetComponentInChildren<PlayerManager>();
    }
}
