using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyAttack", menuName = "EnsembleRetriever/EnemyAttack")]
public class EnemyAttack : ScriptableObject
{
    [Header("Visuals and Animation")]
    public GameObject _attackPrefab;
    public Vector3 _visualsScale;

    [Tooltip("If null, keeps material of _attackPrefab. Otherwise overrides it.")]
    public Material _attackMaterial;
    public float _attackSpeed;
    public string _telegraphAnimationTrigger;

    [Header("Combat Values")]
    public float _attackChargeAmount;

    [Header("Audio")]
    public AudioClip _telegraphAudio;
    public float _telegraphAudioScale;
    public AudioClip _spawnAudio;
    public float _spawnAudioScale;
}
