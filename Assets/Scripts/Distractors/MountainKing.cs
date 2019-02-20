using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MountainKing : DistractorEnemy
{
    public override void InitialiseDistractor(RedirectionManagerER redirectionManager, bool findSpawnPosition = true)
    {
        base.InitialiseDistractor(redirectionManager, false);
        _animatedInterface.AnimationTrigger("Conducting");
    }
}
