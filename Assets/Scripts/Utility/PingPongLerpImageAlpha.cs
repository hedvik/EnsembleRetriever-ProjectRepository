using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PingPongLerpImageAlpha : MonoBehaviour
{
    public float _lerpSpeed = 5f;

    private Image _image;
    private float _lerpTimer;

    // Start is called before the first frame update
    void Start()
    {
        _image = GetComponent<Image>();
    }

    // Update is called once per frame
    void Update()
    {
        _lerpTimer += Time.deltaTime * _lerpSpeed;
        _image.color = new Color(_image.color.r, _image.color.g, _image.color.b, Mathf.PingPong(_lerpTimer, 1f));
    }
}
