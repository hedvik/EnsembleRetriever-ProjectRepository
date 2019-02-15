using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HintGiver : MonoBehaviour
{
    private GameManager _gameManager;
    private GameObject _textBoxObject;

    private void Start()
    {
        _gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        _textBoxObject = transform.Find("TextBox").gameObject;
        _textBoxObject.transform.localScale = Vector3.zero;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _gameManager._uiManager.ChangeTextBoxVisibility(true, _textBoxObject.transform);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _gameManager._uiManager.ChangeTextBoxVisibility(false, _textBoxObject.transform);
        }
    }
}
