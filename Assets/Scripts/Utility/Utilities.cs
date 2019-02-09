using UnityEngine;

public static class UtilitiesER
{
    public static float Remap(float xMin, float xMax, float yMin, float yMax, float value)
    {
        return Mathf.Lerp(yMin, yMax, Mathf.InverseLerp(xMin, xMax, value));
    }
}
