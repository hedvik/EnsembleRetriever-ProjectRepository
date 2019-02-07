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

            if(SteamVR.active && SteamVR_Actions._default.Teleport[SteamVR_Input_Sources.Any].state)
            {
                UpdateTextToNextSnippet();
            }

            _currentlyActiveTextBox.transform.LookAt(_redirectorManager.headTransform.position);
        }
    }

    public void ActivateDialogue(object triggerReceiver, TypeInfo typeInfo, GameObject dialogueBox, Queue<DialogueSnippet> textLines)
    {
        _inDialogue = true;
        _currentlyActiveTextBox = dialogueBox;
        _currentDialogueList = textLines;
        _currentlyActiveText = _currentlyActiveTextBox.GetComponentInChildren<Text>();
        _currentTriggerReceiver = triggerReceiver;
        _currentTriggerReceiverType = typeInfo;
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
        // Running the end function trigger on a dialogue snippet if available
        if (_currentDialogueSnippet != null && _currentDialogueSnippet._functionTriggerEnd != null && _currentTriggerReceiver != null)
        {
            TriggerFunction(_currentDialogueSnippet._functionTriggerEnd, _currentDialogueSnippet._stringParameterEnd);
        }

        if (_currentDialogueList.Count != 0) {
            _currentDialogueSnippet = _currentDialogueList.Dequeue();
            _currentlyActiveText.text = _currentDialogueSnippet._text;

            // Running the start function trigger on a dialogue snippet if available
            if (_currentDialogueSnippet._functionTriggerStart != "" && _currentTriggerReceiver != null)
            {
                TriggerFunction(_currentDialogueSnippet._functionTriggerStart, _currentDialogueSnippet._stringParameterStart);
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

    private void TriggerFunction(string functionName, string param)
    {
        // Using reflection to allow for some amount of scripting when writing dialogue snippets.
        var method = _currentTriggerReceiverType.GetMethod(functionName);
        method.Invoke(_currentTriggerReceiver, param == "" ? null : new object[] { param });
    }
}