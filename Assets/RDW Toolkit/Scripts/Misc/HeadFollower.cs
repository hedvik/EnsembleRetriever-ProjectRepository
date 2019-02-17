using UnityEngine;
using System.Collections;

public class HeadFollower : MonoBehaviour
{
    [HideInInspector]
    public RedirectionManager redirectionManager;
    

    // Update is called once per frame
    void Update()
    {
        // This is needed to actually take 3D into account compared to what the toolkit originally did
        transform.position = redirectionManager.headTransform.position;
        var localPosition = transform.localPosition;
        localPosition.y = 0;
        transform.localPosition = localPosition;
        if (redirectionManager.currDir != Vector3.zero)
            this.transform.rotation = Quaternion.LookRotation(redirectionManager.currDir, Vector3.up);
    }
}
