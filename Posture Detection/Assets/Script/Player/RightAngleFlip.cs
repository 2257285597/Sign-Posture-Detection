using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RightAngleFlip : MonoBehaviour
{
    private void OnTriggerEnter(Collider col)
    {
        FlipDirection(transform.forward, col.attachedRigidbody.transform);
    }

    void FlipDirection(Vector3 newUpdirection, Transform tr)
    {
        float angleBetweenUpdirection = Vector3.Angle(newUpdirection, tr.up);
        float angleThreshold = 0.001f;

        if(angleBetweenUpdirection < angleThreshold)
        {
            return;
        }   
        
        Quaternion rotationDiffrernce = Quaternion.FromToRotation(tr.up, newUpdirection);   
        tr.rotation = rotationDiffrernce * tr.rotation;
    }
}
