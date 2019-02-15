using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BatonUpgrades", menuName = "EnsembleRetriever/BatonUpgrades")]
public class BatonUpgrades : ScriptableObject
{
    public List<BatonUpgrade> _batonUpgrades = new List<BatonUpgrade>();

    [System.Serializable]
    public class BatonUpgrade
    {
        [ColorUsage(true, true)]
        public Color _emissionColor;
        public int _damageValue;
    }
}
