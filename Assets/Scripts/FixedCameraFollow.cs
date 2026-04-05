using UnityEngine;

/// <summary>
/// A completely fixed camera that strictly follows the player's exact rotation and position 
/// from a predefined local offset.
/// </summary>
public class FixedCameraFollow : MonoBehaviour
{
    public Transform target;
    
    [Header("Local Positional Offset")]
    public Vector3 localOffset = new Vector3(0f, 2.5f, -5f);

    [Header("Look Down Angle")]
    public float lookDownPitch = 15f;

    [Header("Smoothing (Set to 0 for instant follow)")]
    public float positionSmooth = 15f;
    public float rotationSmooth = 15f;

    void LateUpdate()
    {
        if (!target) return;

        // 1. Calculate ideal position perfectly locked to the player's local transform
        Vector3 idealPos = target.TransformPoint(localOffset);

        // 2. Calculate ideal rotation matching the player's exact rotation, pitched down slightly to view them
        Quaternion idealRot = target.rotation * Quaternion.Euler(lookDownPitch, 0, 0);

        // 3. Apply changes (uses Lerp/Slerp if smooth > 0, otherwise snaps instantly)
        if (positionSmooth > 0)
            transform.position = Vector3.Lerp(transform.position, idealPos, Time.deltaTime * positionSmooth);
        else
            transform.position = idealPos;

        if (rotationSmooth > 0)
            transform.rotation = Quaternion.Slerp(transform.rotation, idealRot, Time.deltaTime * rotationSmooth);
        else
            transform.rotation = idealRot;
    }
}
