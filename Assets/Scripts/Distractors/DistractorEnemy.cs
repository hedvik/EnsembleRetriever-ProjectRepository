using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DistractorEnemy : Pausable
{
    protected RedirectionManagerER _redirectionManager;
    protected AnimatedCharacterInterface _animatedInterface;

    public virtual void TakeDamage(float damageValue)
    {
        // TODO: decrease health and stuff
    }

    public virtual void InitialiseDistractor(RedirectionManagerER redirectionManager)
    {
        this._redirectionManager = redirectionManager;
        _animatedInterface = GetComponent<AnimatedCharacterInterface>();
    }

    public virtual void FinaliseDistractor()
    {
        Destroy(gameObject);
    }
}
