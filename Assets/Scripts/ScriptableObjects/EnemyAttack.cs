using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AttackTypeSpeed { slow = 0, medium, fast };
public enum AttackTypeInstrument { none = 0, contrabass, oboe, harpsichord, violin, glockenspiel, mountainKing}

[CreateAssetMenu(fileName = "NewEnemyAttack", menuName = "EnsembleRetriever/EnemyAttack")]
public class EnemyAttack : ScriptableObject
{
    [Header("Visuals and Animation")]
    public GameObject _attackPrefab;
    public Vector3 _visualsScale;

    [Tooltip("If null, keeps material of _attackPrefab. Otherwise overrides it.")]
    public Material _attackMaterial;
    public float _attackSpeed;

    // Both of these types have a separate telegraph. Both are used
    public AttackTypeSpeed _attackType;
    public AttackTypeInstrument _attackParentInstrument;

    [Header("Combat Values")]
    public float _attackChargeAmount;

    [Header("Audio")]
    public AudioClip _spawnAudio;
    public float _spawnAudioScale;
}
