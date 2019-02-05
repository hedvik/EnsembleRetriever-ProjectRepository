using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Distractor : Pausable
{
    protected RedirectionManagerER _redirectionManagerER;

    public virtual void InitialiseDistractor(RedirectionManagerER redirectionManager)
    {
        _redirectionManagerER = redirectionManager;
    }

    public virtual void FinaliseDistractor()
    {
        Destroy(gameObject);
    }
}
