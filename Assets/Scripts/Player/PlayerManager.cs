using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using Valve.VR;

public class PlayerManager : MonoBehaviour
{
    public float _maxBatonCharge = 100f;
    public MeshRenderer _batonRenderer;
    public float _chargedAnimationSpeed = 5f;
    public float _pointerLineLength = 50f;
    public float _shotAnimationNoise = 1f;
    public float _takeDamageAnimationSpeed = 5f;
    public int _expNeededForLevelUp = 100;

    [Header("Audio")]
    public AudioClip _attackSound;
    public AudioClip _absorbSound;
    public AudioClip _takeDamageSound;
    public AudioClip _batonChargedSound;

    [HideInInspector]
    public AudioSource _audioSource;

    [Header("VR")]
    public SteamVR_Input_Sources _batonHand;
    public SteamVR_Input_Sources _shieldHand;

    private float _currentCharge = 0f;

    private float _chargedAnimationTimer = 0f;
    private LineRenderer _batonLineRenderer;
    private LineRenderer _attackLineRenderer;
    private Transform _pointerOrigin;
    private ParticleSystem _batonParticleSystem;
    private const float _DEBUG_HEAD_SPEED = 100;
    private Vignette _vignette;
    private Coroutine _takeDamageRoutine;

    private int _currentEXP = 0;
    private float _currentShotDamage = 0f;
    private ShieldUpgrades _shieldUpgrades;
    private BatonUpgrades _batonUpgrades;
    private int _currentBatonLevel = 0;
    private int _currentShieldLevel = 0;
    private Color _currentEmissionColor;
    private PlayerShield _playerShield;
    private GameManager _gameManager;
    private Transform _headTransform;
    private int _maxLevel = 0;
    private int _currentLevel = 0;

    private void Awake()
    {
        // Needed for runtime modification of emission colour
        _batonRenderer.material.EnableKeyword("_EMISSION");
        _batonLineRenderer = _batonRenderer.transform.parent.parent.GetChild(0).GetComponent<LineRenderer>();
        _batonLineRenderer.enabled = true;
        _pointerOrigin = _batonLineRenderer.transform;
        _attackLineRenderer = _batonRenderer.transform.parent.parent.GetChild(1).GetComponent<LineRenderer>();
        _batonParticleSystem = _pointerOrigin.transform.GetChild(0).GetComponent<ParticleSystem>();
        _audioSource = GetComponent<AudioSource>();

        _batonLineRenderer.SetPosition(0, _pointerOrigin.InverseTransformPoint(_pointerOrigin.position));
        _batonLineRenderer.SetPosition(1, _pointerOrigin.InverseTransformPoint(_pointerOrigin.position + _pointerOrigin.forward * _pointerLineLength));

        _vignette = GameObject.Find("PostProcessing").GetComponent<PostProcessVolume>().profile.GetSetting<Vignette>();

        _playerShield = GetComponentInChildren<PlayerShield>();

        _shieldUpgrades = Resources.Load<ShieldUpgrades>("ScriptableObjects/PlayerUpgrades/ShieldUpgrades");
        _batonUpgrades = Resources.Load<BatonUpgrades>("ScriptableObjects/PlayerUpgrades/BatonUpgrades");

        _maxLevel = _shieldUpgrades._shieldUpgrades.Count + _batonUpgrades._batonUpgrades.Count - 2;

        _gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();

        UpdatePlayerUpgrades();

        ResetBaton();
    }

    private void Start()
    {
        _headTransform = _gameManager._redirectionManager.GetUserHeadTransform();
    }

    private void Update()
    {
        #if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.P) || (SteamVR.active && SteamVR_Actions._default.GrabGrip.GetStateDown(_batonHand)))
        {
            AddCharge(100);
        }

        if(Input.GetKeyDown(KeyCode.X) || (SteamVR_Actions._default.GrabGrip.GetStateDown(_shieldHand)))
        {
            AddEXP(100);
        }
        #endif

        if (_currentCharge >= _maxBatonCharge)
        {
            if (Input.GetKeyDown(KeyCode.Space) || (SteamVR.active && SteamVR_Actions._default.GrabPinch.GetStateDown(_batonHand)))
            {
                Shoot();
                return;
            }

            _chargedAnimationTimer += Time.deltaTime * _chargedAnimationSpeed;
            var sineTimer = UtilitiesER.Remap(-1f, 1f, 0, 1, Mathf.Sin(_chargedAnimationTimer));

            // +1 is added to these calculations so the emission strength is a bit more noticeable
            _batonRenderer.material.SetColor("_EmissionColor", Color.Lerp(Color.black * 0, _currentEmissionColor, sineTimer));
        }
    }

    public void AddCharge(float amount)
    {
        _currentCharge = Mathf.Clamp(amount + _currentCharge, 0, _maxBatonCharge);
        if (_currentCharge < _maxBatonCharge)
        {
            var remappedValue = UtilitiesER.Remap(0, _maxBatonCharge, 0, 1, _currentCharge);
            _batonRenderer.material.SetColor("_EmissionColor", Color.Lerp(Color.black * 0, _currentEmissionColor, remappedValue));
        }
        else
        {
            _chargedAnimationTimer = 0f;
            _batonLineRenderer.enabled = true;
            _audioSource.PlayOneShot(_batonChargedSound, 0.7f);
        }
    }

    public void ResetCharge()
    {
        _currentCharge = 0f;

        var remappedValue = UtilitiesER.Remap(0, _maxBatonCharge, 0, 1, _currentCharge);
        _batonRenderer.material.SetColor("_EmissionColor", new Color(remappedValue, remappedValue, remappedValue, 1.0f) * (remappedValue + 1));
    }

    // TODO: Implement this
    public void AddEXP(int value)
    {
        var oldExp = _currentEXP;
        _currentEXP += value;
        // POLISH: Play exp animation
        // POLISH: OnAnimationEnd: If levelup, ask player what they want to upgrade

        if (_currentEXP >= _expNeededForLevelUp && _currentLevel < _maxLevel)
        {
            _currentEXP -= _expNeededForLevelUp;
            _currentLevel++;
            _gameManager._levelUpDialogueBox.enabled = true;
            _gameManager._levelUpDialogueBox.UpdateAvailableUpgradeOptions((_currentBatonLevel + 1 < _batonUpgrades._batonUpgrades.Count), (_currentShieldLevel + 1 < _shieldUpgrades._shieldUpgrades.Count));

            _gameManager._uiManager.ChangeTextBoxVisibility(true, _gameManager._levelUpDialogueBox.transform);
            var levelUpBoxSpawnPosition = _headTransform.position + _headTransform.forward * _gameManager._levelUpDialogueBoxOffsetFromPlayer;
            levelUpBoxSpawnPosition.y = _headTransform.position.y;
            _gameManager._levelUpDialogueBox.transform.position = levelUpBoxSpawnPosition;
        }
    }

    public void LevelUp(bool batonChosen)
    {
        if (batonChosen)
        {
            if (_currentBatonLevel + 1 < _batonUpgrades._batonUpgrades.Count)
            {
                _currentBatonLevel++;
            }
        }
        else
        {
            if (_currentShieldLevel + 1 < _shieldUpgrades._shieldUpgrades.Count)
            {
                _currentShieldLevel++;
            }
        }

        UpdatePlayerUpgrades();
    }

    public void TakeDamage()
    {
        // TODO: recordedNumberOfPlayerhits++
        if (_takeDamageRoutine != null)
        {
            StopCoroutine(_takeDamageRoutine);
        }
        _takeDamageRoutine = StartCoroutine(TakeDamageAnimation());
    }

    private void Shoot()
    {
        _audioSource.PlayOneShot(_attackSound);
        var attackEndPoint = _pointerOrigin.position + _pointerOrigin.forward * _pointerLineLength;

        // We do not want to potentially hit the distractor or reset colliders so we only raycast objects in the virtual world.
        // Layer 9 is in this case the VirtualWorld layer.
        var layerMask = 1 << 9;

        RaycastHit hit;
        if (Physics.Raycast(_pointerOrigin.position, _pointerOrigin.forward, out hit, _pointerLineLength, layerMask))
        {
            if (hit.collider.CompareTag("Enemy"))
            {
                hit.collider.gameObject.GetComponent<DistractorEnemy>().TakeDamage(_currentShotDamage);
                attackEndPoint = hit.point;
            }
        }

        ResetBaton();

        StartCoroutine(ShotAnimation(attackEndPoint));
    }

    private void ResetBaton()
    {
        _batonLineRenderer.enabled = false;
        _currentCharge = 0;
        // HACK: Hacky way to quickly reset stuff
        AddCharge(0);
    }

    private void UpdatePlayerUpgrades()
    {
        _currentEmissionColor = _batonUpgrades._batonUpgrades[_currentBatonLevel]._emissionColor;
        _currentShotDamage = _batonUpgrades._batonUpgrades[_currentBatonLevel]._damageValue;
        _playerShield.transform.localScale = _shieldUpgrades._shieldUpgrades[_currentShieldLevel]._shieldScale;
    }

    private IEnumerator ShotAnimation(Vector3 endPoint)
    {
        // Generating the lineRenderer positions for the attack
        for (int i = 0; i < _attackLineRenderer.positionCount; i++)
        {
            var currentPosition = (i != 0) ? Vector3.Lerp(_pointerOrigin.position, endPoint, ((i + 1f) / _attackLineRenderer.positionCount)) : _pointerOrigin.position;
            if (i != 0)
            {
                currentPosition += new Vector3(Random.Range(-_shotAnimationNoise, _shotAnimationNoise), Random.Range(-_shotAnimationNoise, _shotAnimationNoise), Random.Range(-_shotAnimationNoise, _shotAnimationNoise));
            }
            _attackLineRenderer.SetPosition(i, currentPosition);
        }

        _attackLineRenderer.enabled = true;
        _batonParticleSystem.Play();
        yield return new WaitForSeconds(0.25f);
        _attackLineRenderer.enabled = false;
    }

    private IEnumerator TakeDamageAnimation()
    {
        var lerpTimer = 0f;
        while (Mathf.Sin(lerpTimer) >= 0f)
        {
            yield return null;
            lerpTimer += Time.deltaTime * _takeDamageAnimationSpeed;
            _vignette.intensity.value = Mathf.Lerp(0f, 0.5f, Mathf.Clamp(Mathf.Sin(lerpTimer), 0, 1));
        }

        _vignette.intensity.value = 0f;
    }
}