using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Optimisation class to avoid having world space canvases waste frame time on searching for the main camera. 
/// </summary>
public class SetCanvasEventCamera : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        var canvas = GetComponent<Canvas>();
        var gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        canvas.worldCamera = gameManager._redirectionManager.headTransform.GetComponent<Camera>();
    }
}
