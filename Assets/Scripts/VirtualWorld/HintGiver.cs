using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HintGiver : Pausable
{
    public float _randomAnimationTriggerCooldown = 4f;
    public float _randomAnimationTriggerNoise = 2f;

    private GameManager _gameManager;
    private GameObject _textBoxObject;
    private Animator _animator;
    private float _triggerTimer = 0f;
    private float _randomTimeTarget;
    private SphereCollider _collider;

    private void Start()
    {
        _gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        _textBoxObject = transform.Find("TextBox").gameObject;
        _textBoxObject.transform.localScale = Vector3.zero;
        _animator = GetComponentInChildren<Animator>();
        _animator.Play("Idle", -1, Random.Range(0f, 1f));
        _collider = GetComponent<SphereCollider>();

        _randomTimeTarget = Random.Range(_randomAnimationTriggerCooldown - _randomAnimationTriggerNoise, _randomAnimationTriggerCooldown + _randomAnimationTriggerNoise);
        _gameManager._redirectionManager.SubscribeToDistractorTriggerCallback(OnDistractorStart);
        _gameManager._redirectionManager.SubscribeToDistractorEndCallback(OnDistractorEnd);
    }

    private void Update()
    {
        _triggerTimer += Time.deltaTime;

        if(_triggerTimer >= _randomTimeTarget)
        {
            _triggerTimer = 0;
            _randomTimeTarget = Random.Range(_randomAnimationTriggerCooldown - _randomAnimationTriggerNoise, _randomAnimationTriggerCooldown + _randomAnimationTriggerNoise);
            _animator.SetTrigger("Break" + Random.Range(1, 4).ToString());
        }
    }
    
    public void OnDistractorStart()
    {
        // RATHER HACKY: The UI manager can technically scale any transform to 0. This is rather misused here.
        _gameManager._uiManager.ChangeTextBoxVisibility(false, transform);
    }

    public void OnDistractorEnd()
    {
        _gameManager._uiManager.ChangeTextBoxVisibility(true, transform);
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

    protected override void PauseStateChange()
    {
        _animator.enabled = !_isPaused;
    }
}
