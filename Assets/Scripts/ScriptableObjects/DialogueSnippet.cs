using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "NewDialogueSnippet", menuName = "EnsembleRetriever/DialogueSnippet", order = 0)]
public class DialogueSnippet : ScriptableObject
{
    [TextArea]
    public string _text;
    public AudioClip _voiceLine;
    public string _animationTrigger;
    public string _functionTrigger;
}
