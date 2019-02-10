using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;


// Scriptable interface for programmatic animation and movement
public class AnimatedCharacterInterface : Pausable
{
    public float _movementSpeed = 5f;
    public ParticleSystem _teleportParticles;
    public AudioClip _teleportSound;

    private AudioSource _audioSource;
    private Animator _animator;
    private RedirectionManagerER _redirectionManager;

    #region TutorialRelated
    private bool _attackTutorialActive = false;
    #endregion

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _animator = GetComponent<Animator>();
        _redirectionManager = GameObject.FindGameObjectWithTag("RedirectionManager").GetComponent<RedirectionManagerER>();
    }

    protected override void PauseStateChange()
    {
        _animator.enabled = !_isPaused;
    }

    public void AnimationTrigger(string trigger)
    {
        _animator.SetTrigger(trigger);
    }

    public void TeleportToStringPosition(string position)
    {
        _teleportParticles.Play();
        _audioSource.PlayOneShot(_teleportSound);

        transform.position = StringToVector3(position);
    }

    public void TeleportToPosition(Vector3 position)
    {
        _teleportParticles.Play();
        _audioSource.PlayOneShot(_teleportSound);

        transform.position = position;
    }

    public void LookAtStringPositon(string target)
    {
        Debug.LogError("This function is not yet implemented!");
    }

    public void LookAtPosition(Vector3 target)
    {
        transform.LookAt(target);
    }

    public void MoveByStringVector(string vector)
    {
        StartCoroutine(MoveToPosition(transform.position + StringToVector3(vector)));
    }

    public void StartGame()
    {
        _redirectionManager._gameManager.StartGame();
    }

    #region TutorialSpecificFunctions
    public void StartTutorialAttacks()
    {
        _attackTutorialActive = true;
        StartCoroutine(TutorialAttackPhase());
    }

    // A quick and dirty coroutine so the tutorial NPC can throw attacks
    private IEnumerator TutorialAttackPhase()
    {
        var tutorialPhase = Resources.Load<EnemyPhase>("ScriptableObjects/EnemyPhases/Tutorial/Normal");
        var attackTimer = 0f;
        var tutorialAttack = tutorialPhase._enemyAttacks[0];

        while(_attackTutorialActive)
        {
            if (!_isPaused)
            {
                attackTimer += Time.deltaTime;

                if (attackTimer >= tutorialPhase._attackCooldown)
                {
                    attackTimer -= tutorialPhase._attackCooldown;
                    var projectile = Instantiate(tutorialAttack._attackPrefab, transform.position + transform.forward, Quaternion.identity).GetComponent<BasicProjectile>();
                    projectile.Initialise(tutorialAttack, _redirectionManager.headTransform);
                    _audioSource.PlayOneShot(tutorialAttack._spawnAudio, tutorialAttack._spawnAudioScale);
                }
            }
            yield return null;
        }
    }
    #endregion

    private IEnumerator MoveToPosition(Vector3 position)
    {
        var startPosition = transform.position;
        var lerpTimer = 0f;

        while(lerpTimer <= 1)
        {
            lerpTimer += Time.deltaTime * _movementSpeed;
            transform.position = Vector3.Lerp(startPosition, position, lerpTimer);
            yield return null;
        }
    }

    // https://answers.unity.com/questions/1134997/string-to-vector3.html
    private static Vector3 StringToVector3(string sVector)
    {
        // Remove the parentheses
        if (sVector.StartsWith("(") && sVector.EndsWith(")"))
        {
            sVector = sVector.Substring(1, sVector.Length - 2);
        }

        // split the items
        string[] sArray = sVector.Split(',');

        // store as a Vector3
        Vector3 result = new Vector3(
            float.Parse(sArray[0], CultureInfo.InvariantCulture),
            float.Parse(sArray[1], CultureInfo.InvariantCulture),
            float.Parse(sArray[2], CultureInfo.InvariantCulture)
            );

        return result;
    }
}
