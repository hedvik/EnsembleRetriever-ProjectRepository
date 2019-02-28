using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtPlayer : MonoBehaviour
{
    private GameManager _gameManager;
    private Transform _target;

    // Start is called before the first frame update
    void Start()
    {
        _gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        _target = _gameManager._redirectionManager.headTransform;
    }

    // Update is called once per frame
    void Update()
    {
        transform.LookAt(_target);
    }
}
