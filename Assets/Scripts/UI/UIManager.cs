using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using Valve.VR;

public class UIManager : MonoBehaviour
{
    public float _menuDisplaySpeed = 5f;

    [HideInInspector]
    public RedirectionManagerER _redirectorManager;

    private bool _inDialogue = false;
    private Queue<DialogueSnippet> _currentDialogueList = new Queue<DialogueSnippet>();
    private GameObject _currentlyActiveTextBox;
    private Text _currentlyActiveText;
    private object _currentTriggerReceiver;
    private TypeInfo _currentTriggerReceiverType;
    private DialogueSnippet _currentDialogueSnippet;
    private PlayerManager _playerManager;

    private void Start()
    {
        _playerManager = _redirectorManager._playerManager;
    }

    private void Update()
    {
        if (_inDialogue)
        {
#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.T))
            {
                NextDialogueSnippet(false);
            }
#endif

            if (SteamVR.active && SteamVR_Actions._default.Teleport.GetStateDown(_playerManager._batonHand))
            {
                NextDialogueSnippet(false);
            }
        }
    }

    public void ActivateDialogue(object triggerReceiver, TypeInfo typeInfo, GameObject dialogueBox, Queue<DialogueSnippet> textLines)
    {
        StartCoroutine(QueueDialogueSourceChange(triggerReceiver, typeInfo, dialogueBox, textLines));
    }

    public void EventTriggerSnippet()
    {
        StartCoroutine(ChangeMenuVisibilityAnimation(true));
        NextDialogueSnippet(true);
    }

    public void ChangeTextBoxVisibility(bool displaying, Transform target)
    {
        StartCoroutine(ChangeMenuVisibilityAnimation(displaying, target));
    }

    private void NextDialogueSnippet(bool eventTrigger)
    {
        // If the current textbox makes use of an eventTrigger, then controller/keyboard inputs are not allowed to advance it
        if(!eventTrigger && (_currentDialogueSnippet != null && _currentDialogueSnippet._eventTriggered))
        {
            return;
        }
        CheckEndTriggers();
        if (_currentDialogueList.Count != 0)
        {
            _currentDialogueSnippet = _currentDialogueList.Dequeue();
            _currentlyActiveText.text = _currentDialogueSnippet._text;
            CheckStartTriggers();
        }
        else
        {
            StartCoroutine(ChangeMenuVisibilityAnimation(false));
        }
    }

    private void CheckStartTriggers()
    {
        // Running the start function trigger on a dialogue snippet if available
        if (!string.IsNullOrEmpty(_currentDialogueSnippet._functionTriggerStart) && _currentTriggerReceiver != null)
        {
            TriggerFunction(_currentDialogueSnippet._functionTriggerStart, _currentDialogueSnippet._stringParameterStart);
        }
    }

    private void CheckEndTriggers()
    {
        // Running the end function trigger on a dialogue snippet if available
        if (_currentDialogueSnippet != null && !string.IsNullOrEmpty(_currentDialogueSnippet._functionTriggerEnd) && _currentTriggerReceiver != null)
        {
            TriggerFunction(_currentDialogueSnippet._functionTriggerEnd, _currentDialogueSnippet._stringParameterEnd);
        }
    }

    private IEnumerator ChangeMenuVisibilityAnimation(bool displaying)
    {
        var lerpTimer = 0f;
        var currentScale = _currentlyActiveTextBox.transform.localScale;
        var startScale = currentScale.y;
        var targetScale = displaying ? 1f : 0f;

        if (Mathf.Approximately(startScale, targetScale))
        {
            yield break;
        }

        while (lerpTimer <= 1f)
        {
            lerpTimer += Time.deltaTime * _menuDisplaySpeed;
            currentScale.y = Mathf.Lerp(startScale, targetScale, lerpTimer);

            _currentlyActiveTextBox.transform.localScale = currentScale;
            yield return null;
        }


        if (!displaying && _currentDialogueList.Count == 0)
        {
            Cleanup();
        }
    }

    // Alternative for displaying text boxes that do not use the regular dialogue system
    private IEnumerator ChangeMenuVisibilityAnimation(bool displaying, Transform target)
    {
        var lerpTimer = 0f;
        var currentScale = target.localScale;
        var startScale = currentScale;
        var targetScale = displaying ? Vector3.one : Vector3.zero;

        while (lerpTimer <= 1f)
        {
            lerpTimer += Time.deltaTime * _menuDisplaySpeed;
            currentScale = Vector3.Lerp(startScale, targetScale, lerpTimer);

            target.localScale = currentScale;
            yield return null;
        }
    }

    private IEnumerator QueueDialogueSourceChange(object triggerReceiver, TypeInfo typeInfo, GameObject dialogueBox, Queue<DialogueSnippet> textLines)
    {
        while (_inDialogue)
        {
            yield return null;
        }

        _inDialogue = true;
        _currentlyActiveTextBox = dialogueBox;
        _currentDialogueList = textLines;
        _currentlyActiveText = _currentlyActiveTextBox.GetComponentInChildren<Text>();
        _currentTriggerReceiver = triggerReceiver;
        _currentTriggerReceiverType = typeInfo;
        NextDialogueSnippet(false);
        StartCoroutine(ChangeMenuVisibilityAnimation(true));
    }

    private void TriggerFunction(string functionName, string param)
    {
        // Using reflection to allow for some amount of scripting when writing dialogue snippets.
        var method = _currentTriggerReceiverType.GetMethod(functionName);
        method.Invoke(_currentTriggerReceiver, string.IsNullOrEmpty(param) ? null : new object[] { param });
    }

    private void Cleanup()
    {
        _currentlyActiveTextBox = null;
        _currentDialogueList = null;
        _currentlyActiveText = null;
        _currentTriggerReceiver = null;
        _currentTriggerReceiverType = null;
        _currentDialogueSnippet = null;

        _inDialogue = false;
    }
}