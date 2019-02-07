using UnityEngine;
using System.Collections;

/// <summary>
/// The same as ResetTrigger.cs, but with a different function call on trigger.
/// TOTHINK: Might just end up combining them
/// </summary>
public class DistractorTrigger : MonoBehaviour
{
    [HideInInspector]
    public RedirectionManagerER _redirectionManagerER;
    [HideInInspector]
    public Collider _bodyCollider;

    [SerializeField, Range(0f, 5f)]
    public float _DISTRACTOR_TRIGGER_BUFFER = 0.5f;

    [HideInInspector]
    public float _xLength, _zLength;

    public void Initialise()
    {
        // Set Size of Collider
        float trimAmountOnEachSide = _bodyCollider.transform.localScale.x + 2 * _DISTRACTOR_TRIGGER_BUFFER;
        this.transform.localScale = new Vector3(1 - (trimAmountOnEachSide / this.transform.parent.localScale.x), 2 / this.transform.parent.localScale.y, 1 - (trimAmountOnEachSide / this.transform.parent.localScale.z));
        _xLength = this.transform.parent.localScale.x - trimAmountOnEachSide;
        _zLength = this.transform.parent.localScale.z - trimAmountOnEachSide;
    }

    void OnTriggerExit(Collider other)
    {
        if (other == _bodyCollider)
        {
            _redirectionManagerER.OnDistractorTrigger();
        }
    }
}
