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

    // TODO: Fireflies should stay away when a distractor is active. Try to add some sort of subscription callback for when distractors are triggered
    public void OnDistractorStart()
    {
        // _animator.SetTrigger("Despawn");
        _gameManager._uiManager.ChangeTextBoxVisibility(false, _textBoxObject.transform);
        _collider.enabled = false;
    }

    public void OnDistractorEnd()
    {
        // _animator.SetTrigger("Respawn");
        _collider.enabled = true;
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
