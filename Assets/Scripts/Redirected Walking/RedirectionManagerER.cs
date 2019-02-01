using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RedirectionManagerER : RedirectionManager
{
    [Header("Ensemble Retriever Related")]
    public float _trackingSpaceFadeSpeed = 5f;

    private MeshRenderer _environmentFadeVisuals;
    private MeshRenderer _trackingSpaceFloorVisuals;
    private MeshRenderer _chaperoneVisuals;

    private void Start()
    {
        _environmentFadeVisuals = base.trackedSpace.Find("EnvironmentFadeCube").GetComponent<MeshRenderer>();
        _trackingSpaceFloorVisuals = base.trackedSpace.Find("Plane").GetComponent<MeshRenderer>();
        _chaperoneVisuals = base.trackedSpace.Find("Chaperone").GetComponent<MeshRenderer>();

        // By setting _ZWrite to 1 we avoid some sorting issues between the floor and the box around it
        _trackingSpaceFloorVisuals.material.SetInt("_ZWrite", 1);
        _chaperoneVisuals.material.SetInt("_ZWrite", 1);
    }

    /// <summary>
    /// Can be used to fade the environment out and the physical space in.
    /// The primary use case for this would be custom reset methods. 
    /// </summary>
    /// <param name="fadePhysicalSpaceIn">Whether to fade in or out.</param>
    public void FadeTrackingSpace(bool fadePhysicalSpaceIn)
    {
        StartCoroutine(FadeCoroutine(fadePhysicalSpaceIn));

        // TODO: might somehow want to disable any objects intersecting with the floor
    }

    /// <summary>
    /// Interpolates the alpha values for the physical tracking space. 
    /// </summary>
    /// <param name="environmentAlphaTarget"></param>
    /// <param name="floorAlphaTarget"></param>
    private IEnumerator FadeCoroutine(bool fadePhysicalSpaceIn)
    {
        var cubeColorTemp = _environmentFadeVisuals.material.color;
        var floorColorTemp = _trackingSpaceFloorVisuals.material.color;
        var chaperoneColorTemp = _chaperoneVisuals.material.color;

        var cubeAlphaStart = _environmentFadeVisuals.material.color.a;
        var floorAlphaStart = _trackingSpaceFloorVisuals.material.color.a;

        float lerpTimer = 0f;
        while(lerpTimer < 1f)
        {
            lerpTimer += Time.deltaTime * _trackingSpaceFadeSpeed;

            cubeColorTemp.a = Mathf.Lerp(cubeAlphaStart, fadePhysicalSpaceIn ? 0.99f : 0, lerpTimer);
            floorColorTemp.a = Mathf.Lerp(floorAlphaStart, fadePhysicalSpaceIn ? 1 : 0, lerpTimer);
            chaperoneColorTemp.a = floorColorTemp.a;
            _environmentFadeVisuals.material.color = cubeColorTemp;
            _trackingSpaceFloorVisuals.material.color = floorColorTemp;
            _chaperoneVisuals.material.color = chaperoneColorTemp;

            yield return null;
        }
    }
}
