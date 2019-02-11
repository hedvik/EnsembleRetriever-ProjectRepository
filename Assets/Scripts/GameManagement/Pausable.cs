using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pausable : MonoBehaviour
{
    protected bool _isPaused = false;
    public void SetPauseState(bool isPaused)
    {
        _isPaused = isPaused;
        PauseStateChange();
    }

    protected virtual void PauseStateChange() { }
}
