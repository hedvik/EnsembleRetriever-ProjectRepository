using UnityEngine;
using System.Collections.Generic;

public static class UtilitiesER
{
    public static float Remap(float xMin, float xMax, float yMin, float yMax, float value)
    {
        return Mathf.Lerp(yMin, yMax, Mathf.InverseLerp(xMin, xMax, value));
    }

    // https://denisrizov.com/2016/06/02/bezier-curves-unity-package-included/
    public static Vector3 GetPointOnBezierCurve(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        t = Mathf.Clamp01(t);
        float u = 1f - t;
        float t2 = t * t;
        float u2 = u * u;
        float u3 = u2 * u;
        float t3 = t2 * t;

        Vector3 result =
            (u3) * p0 +
            (3f * u2 * t) * p1 +
            (3f * u * t2) * p2 +
            (t3) * p3;

        return result;
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

    public static readonly Dictionary<AttackTypeSpeed, float> AttackTypeSpeedAudioScales = new Dictionary<AttackTypeSpeed, float>
    {
        { AttackTypeSpeed.slow, 1 },
        { AttackTypeSpeed.medium, 1 },
        { AttackTypeSpeed.fast, 0.5f }
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

    public static readonly Dictionary<AttackTypeInstrument, float> AttackTypeInstrumentAudioScales = new Dictionary<AttackTypeInstrument, float>
    {
        { AttackTypeInstrument.contrabass, 1 },
        { AttackTypeInstrument.oboe, 0.6f },
        { AttackTypeInstrument.harpsichord, 1 },
        { AttackTypeInstrument.glockenspiel, 1 },
        { AttackTypeInstrument.violin, 1 },
        { AttackTypeInstrument.mountainKing, 1 }
    };
}
