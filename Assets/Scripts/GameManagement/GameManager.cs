using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using UnityEngine.XR;

public class GameManager : MonoBehaviour
{
    public GameObject _startGameTextBox;
    public AnimatedCharacter _tutorialInstrument;

    [HideInInspector]
    public bool _gameStarted = false;

    private UIManager _uiManager;
    private Queue<DialogueSnippet> _startGameDialogue;
    private Queue<DialogueSnippet> _tutorialDialogue;
    private RedirectionManagerER _redirectionManager;


    private void Start()
    {
        _uiManager = GameObject.Find("UIManager").GetComponent<UIManager>();
        _startGameDialogue = new Queue<DialogueSnippet>(Resources.LoadAll<DialogueSnippet>("ScriptableObjects/Dialogue/StartGame"));
        _tutorialDialogue = new Queue<DialogueSnippet>(Resources.LoadAll<DialogueSnippet>("ScriptableObjects/Dialogue/Tutorial"));
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
        _tutorialInstrument.gameObject.SetActive(true);
        _uiManager.ActivateDialogue(_tutorialInstrument, typeof(AnimatedCharacter).GetTypeInfo(), _tutorialInstrument.transform.GetChild(0).gameObject, _tutorialDialogue);
    }

    public void StartGame()
    {
        _gameStarted = true;
        _redirectionManager.ActivateRotationAndCurvatureGains();
    }
}
