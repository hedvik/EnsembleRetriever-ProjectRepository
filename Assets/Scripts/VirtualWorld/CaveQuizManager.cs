using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CaveQuizManager : MonoBehaviour
{
    public Transform _movableStage;
    public GameObject _stageMovementParticles;
    public AudioLowPassFilter _lowPassAudioFilter;

    public float _stageMovementSpeed = 1f;
    public float _stageShakeSpeed = 1f;
    public float _stageShakeAmount = 1f;
    public float _stageMovementYOffset = -2f;

    private void Start()
    {
        _stageMovementParticles.SetActive(false);
    }

    private void Update()
    {
        #if UNITY_EDITOR
        if(Input.GetKeyDown(KeyCode.C))
        {
            RemoveMovableStage();
        }
        #endif
    }

    public void RemoveMovableStage()
    {
        _stageMovementParticles.SetActive(true);
        StartCoroutine(RemoveStageAnimation());
    }

    private IEnumerator RemoveStageAnimation()
    {
        var lerpTimer = 0f;
        var startPosition = _movableStage.transform.position;
        var currentPosition = startPosition;
        var startCutoffFrequency = _lowPassAudioFilter.cutoffFrequency;
        while(lerpTimer <= 1f)
        {
            lerpTimer += Time.deltaTime * _stageMovementSpeed;
            currentPosition.y = Mathf.Lerp(startPosition.y, startPosition.y + _stageMovementYOffset, lerpTimer);
            currentPosition.z = startPosition.z + Mathf.Sin(Time.time * _stageShakeSpeed) * _stageShakeAmount;
            _movableStage.transform.position = currentPosition;
            _lowPassAudioFilter.cutoffFrequency = Mathf.Lerp(startCutoffFrequency, 5000, lerpTimer);

            yield return null;
        }

        _stageMovementParticles.SetActive(false);
    }
}
