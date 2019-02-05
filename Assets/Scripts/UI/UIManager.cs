using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public GameObject _debugTextBox;

    public float _menuDisplaySpeed = 5f;

    private bool _inDialogue = false;
    private Queue<DialogueSnippet> _currentDialogueList = new Queue<DialogueSnippet>();
    private GameObject _currentlyActiveTextBox;
    private Text _currentlyActiveText;
    private TextTriggerReceiver _currentTriggerReceiver;

    [HideInInspector]
    public RedirectionManagerER _redirectorManager;

    private void Start()
    {
        // DEBUG: Currently used to test dialogue
        DebugTextTest();
    }

    private void Update()
    {
        if (_inDialogue)
        {
#if UNITY_EDITOR
            if(Input.GetKeyDown(KeyCode.T))
            {
                UpdateTextToNextSnippet();
            }
#endif
            _currentlyActiveTextBox.transform.LookAt(_redirectorManager.headTransform.position);
        }
    }

    public void ActivateDialogue(TextTriggerReceiver triggerReceiver, GameObject dialogueBox, Queue<DialogueSnippet> textLines)
    {
        _inDialogue = true;
        _currentlyActiveTextBox = dialogueBox;
        _currentDialogueList = textLines;
        _currentlyActiveText = _currentlyActiveTextBox.GetComponentInChildren<Text>();
        _currentTriggerReceiver = triggerReceiver;
        UpdateTextToNextSnippet();

        StartCoroutine(ChangeMenuVisibilityAnimation(true));
    }

    public void FinishDialogue()
    {
        StartCoroutine(ChangeMenuVisibilityAnimation(false));
        _inDialogue = false;
    }

    private void UpdateTextToNextSnippet()
    {
        if (_currentDialogueList.Count != 0) {
            var newDialogueSnippet = _currentDialogueList.Dequeue();
            _currentlyActiveText.text = newDialogueSnippet._text;

            if(newDialogueSnippet._animationTrigger != "" && _currentTriggerReceiver != null)
            {
                _currentTriggerReceiver.TriggerAnimation(newDialogueSnippet._animationTrigger);
            }
            if(newDialogueSnippet._functionTrigger != "" && _currentTriggerReceiver != null)
            {
                _currentTriggerReceiver.TriggerFunction(newDialogueSnippet._functionTrigger);
            }
        }
        else
        {
            FinishDialogue();
        }
    }

    private IEnumerator ChangeMenuVisibilityAnimation(bool displaying)
    {
        var lerpTimer = 0f;
        var currentScale = _currentlyActiveTextBox.transform.localScale;

        while (lerpTimer <= 1f)
        {
            lerpTimer += Time.deltaTime * _menuDisplaySpeed;
            currentScale.y = Mathf.Lerp(displaying ? 0 : 1, displaying ? 1 : 0, lerpTimer);

            _currentlyActiveTextBox.transform.localScale = currentScale;
            yield return null;
        }
    }

    private void DebugTextTest()
    {
        var testLines = new Queue<DialogueSnippet>(Resources.LoadAll<DialogueSnippet>("ScriptableObjects/Dialogue/Tutorial"));
        ActivateDialogue(null, _debugTextBox, testLines);
    }
}
