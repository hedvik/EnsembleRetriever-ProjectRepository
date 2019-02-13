using UnityEngine;

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
}
