using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectivePointer : MonoBehaviour
{
    public List<Transform> _objectiveList = new List<Transform>();

    private GameManager _gameManager;
    private PlayerManager _playerManager;
    private bool _active = false;
    private MeshRenderer _visuals;

    private void Start()
    {
        _gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        _playerManager = _gameManager.GetCurrentPlayerManager();
        _visuals = GetComponentInChildren<MeshRenderer>();
        _visuals.enabled = _active;
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.O))
        {
            _active = !_active;
            _visuals.enabled = _active;
        }

        if(!_active)
        {
            return;
        }

        var mininumMagnitude = float.PositiveInfinity;
        var indexOfMinimumMagnitude = -1;
        for(int i = 0; i < _objectiveList.Count; i++)
        {
            var newMagnitude = (_objectiveList[i].position - _playerManager.transform.position).sqrMagnitude;
            if (newMagnitude < mininumMagnitude)
            {
                indexOfMinimumMagnitude = i;
                mininumMagnitude = newMagnitude;
            }
        }

        transform.LookAt(_objectiveList[indexOfMinimumMagnitude], Vector3.up);
    }

    public void RemoveObjectiveFromList(Transform objectiveToRemove)
    {
        _objectiveList.Remove(objectiveToRemove);
    }

    public void Disable()
    {
        _active = false;
        gameObject.SetActive(false);
    }
}
