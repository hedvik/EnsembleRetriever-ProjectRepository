using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using UnityEngine.XR;

public class GameManager : MonoBehaviour
{
    public GameObject _startGameTextBox;

    [HideInInspector]
    public bool _gameStarted = false;

    private UIManager _uiManager;
    private Queue<DialogueSnippet> _startGameDialogue;
    private RedirectionManagerER _redirectionManager;


    private void Start()
    {
        _uiManager = GameObject.Find("UIManager").GetComponent<UIManager>();
        _startGameDialogue = new Queue<DialogueSnippet>(Resources.LoadAll<DialogueSnippet>("ScriptableObjects/Dialogue/StartGame"));
        _uiManager.ActivateDialogue(this, typeof(GameManager).GetTypeInfo(), _startGameTextBox, _startGameDialogue);

        _redirectionManager = GameObject.Find(!XRSettings.enabled ? "Redirected Walker (Debug)" : "Redirected Walker (VR)").GetComponent<RedirectionManagerER>();
        _redirectionManager.MIN_ROT_GAIN = 0f;
        _redirectionManager.MAX_ROT_GAIN = 0f;
        _redirectionManager.CURVATURE_RADIUS = 1000f;
    }

    public void StartTutorial()
    {
        Debug.Log("Tutorial Start!");
    }

    public void StartGame()
    {
        _gameStarted = true;
        _redirectionManager.ActivateRotationAndCurvatureGains();
    }
}
