using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class LevelUpBox : MonoBehaviour
{
    private PlayerManager _playerManager;
    private GameManager _gameManager;
    private Transform _playerHeadTransform;

    // Start is called before the first frame update
    void Start()
    {
        _gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        _playerManager = _gameManager.GetCurrentPlayerManager();
        _playerHeadTransform = _gameManager._redirectionManager.GetUserHeadTransform();
    }

    // Update is called once per frame
    void Update()
    {
        transform.LookAt(_playerHeadTransform);

        #if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.L))
        {
            _playerManager.LevelUp(true);
            Cleanup();
        }
        else if (Input.GetKeyDown(KeyCode.K))
        {
            _playerManager.LevelUp(false);
            Cleanup();
        }
        #endif

        if (SteamVR.active && SteamVR_Actions._default.Teleport.GetStateDown(_playerManager._batonHand))
        {
            _playerManager.LevelUp(true);
            Cleanup();
        }
        else if (SteamVR.active && SteamVR_Actions._default.Teleport.GetStateDown(_playerManager._shieldHand))
        {
            _playerManager.LevelUp(false);
            Cleanup();
        }
    }

    private void Cleanup()
    {
        enabled = false;
        _gameManager._uiManager.ChangeTextBoxVisibility(false, transform);
    }
}
