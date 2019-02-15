using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class LevelUpBox : MonoBehaviour
{
    public GameObject _shieldOption;
    public GameObject _batonOption;

    private PlayerManager _playerManager;
    private GameManager _gameManager;
    private Transform _playerHeadTransform;
    private bool _batonUpgradeAvailable = true;
    private bool _shieldUpgradeAvailable = true;

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
        if (_batonUpgradeAvailable && Input.GetKeyDown(KeyCode.L))
        {
            _playerManager.LevelUp(true);
            Cleanup();
        }
        else if (_shieldUpgradeAvailable && Input.GetKeyDown(KeyCode.K))
        {
            _playerManager.LevelUp(false);
            Cleanup();
        }
        #endif

        if (SteamVR.active && _batonUpgradeAvailable && SteamVR_Actions._default.Teleport.GetStateDown(_playerManager._batonHand))
        {
            _playerManager.LevelUp(true);
            Cleanup();
        }
        else if (SteamVR.active && _shieldUpgradeAvailable && SteamVR_Actions._default.Teleport.GetStateDown(_playerManager._shieldHand))
        {
            _playerManager.LevelUp(false);
            Cleanup();
        }
    }

    public void UpdateAvailableUpgradeOptions(bool batonUpgradeAvailable, bool shieldUpgradeAvailable)
    {
        _batonUpgradeAvailable = batonUpgradeAvailable;
        _shieldUpgradeAvailable = shieldUpgradeAvailable;

        _batonOption.SetActive(batonUpgradeAvailable);
        _shieldOption.SetActive(shieldUpgradeAvailable);
    }

    private void Cleanup()
    {
        enabled = false;
        _gameManager._uiManager.ChangeTextBoxVisibility(false, transform);
    }
}
