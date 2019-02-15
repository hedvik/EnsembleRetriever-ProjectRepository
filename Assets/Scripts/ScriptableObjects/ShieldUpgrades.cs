using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ShieldUpgrades", menuName = "EnsembleRetriever/ShieldUpgrades")]
public class ShieldUpgrades : ScriptableObject
{
    public List<ShieldUpgrade> _shieldUpgrades = new List<ShieldUpgrade>();

    [System.Serializable]
    public class ShieldUpgrade
    {
        public Vector3 _shieldScale;
    }
}
