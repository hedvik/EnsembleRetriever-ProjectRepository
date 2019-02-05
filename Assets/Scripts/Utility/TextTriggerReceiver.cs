using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface TextTriggerReceiver
{
    void TriggerAnimation(string trigger);
    void TriggerFunction(string trigger);
}
