using UnityEngine;
using System.Collections.Generic;

public static class UtilitiesER
{
    public static float Remap(float xMin, float xMax, float yMin, float yMax, float value)
    {
        return Mathf.Lerp(yMin, yMax, Mathf.InverseLerp(xMin, xMax, value));
    }

    // https://answers.unity.com/questions/532297/rotate-a-vector-around-a-certain-point.html
    public static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Quaternion rotation)
    {
        return rotation * (point - pivot) + pivot;
    }

    public static readonly Dictionary<AttackTypeSpeed, string> AttackTypeSpeedTriggers = new Dictionary<AttackTypeSpeed, string>
    {
        { AttackTypeSpeed.slow, "SlowAttackTelegraph" },
        { AttackTypeSpeed.medium, "MediumAttackTelegraph" },
        { AttackTypeSpeed.fast, "FastAttackTelegraph" }
    };

    public static readonly Dictionary<AttackTypeInstrument, string> AttackTypeInstrumentTriggers = new Dictionary<AttackTypeInstrument, string>
    {
        { AttackTypeInstrument.contrabass, "ContrabassTelegraph" },
        { AttackTypeInstrument.oboe, "OboeTelegraph" },
        { AttackTypeInstrument.harpsichord, "HarpsichordTelegraph" },
        { AttackTypeInstrument.glockenspiel, "GlockenspielTelegraph" },
        { AttackTypeInstrument.violin, "ViolinTelegraph" },
        { AttackTypeInstrument.mountainKing, "MountainKingTelegraph" }
    };
}
