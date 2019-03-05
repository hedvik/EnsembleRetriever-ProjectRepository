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
    private bool _batonUpgradeAvailable = true;
    private bool _shieldUpgradeAvailable = true;
    private float _viveControllerTimer = 0f;
    private const float _CONTROLLER_COOLDOWN_TIME = 0.2f;

    // Start is called before the first frame update
    void Start()
    {
        _gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        _playerManager = _gameManager.GetCurrentPlayerManager();
    }

    // Update is called once per frame
    void Update()
    {
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

        if (SteamVR.active && _batonUpgradeAvailable && SteamVR_Actions._default.Teleport.GetStateDown(_playerManager._batonHand) && _viveControllerTimer >= _CONTROLLER_COOLDOWN_TIME)
        {
            _playerManager.LevelUp(true);
            _viveControllerTimer = 0f;
            Cleanup();
        }
        else if (SteamVR.active && _shieldUpgradeAvailable && SteamVR_Actions._default.Teleport.GetStateDown(_playerManager._shieldHand) && _viveControllerTimer >= _CONTROLLER_COOLDOWN_TIME)
        {
            _playerManager.LevelUp(false);
            _viveControllerTimer = 0f;
            Cleanup();
        }
        _viveControllerTimer += Time.deltaTime;
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
        _gameManager._uiManager.ChangeTextBoxVisibility(false, transform);
    }
}
