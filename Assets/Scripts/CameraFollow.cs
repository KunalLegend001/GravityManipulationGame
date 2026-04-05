using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;

    [Header("Offsets")]
    public float distance = 4f;
    public float height = 2f;

    [Header("Mouse Free Look")]
    public float mouseSensitivityX = 120f;
    public float mouseSensitivityY = 120f;
    public float minPitch = -40f;
    public float maxPitch = 70f;

    Vector3 currentForward; 
    float pitch; 

    [Header("Follow")]
    public float followSmooth = 8f;

    [Header("Collision")]
    public float collisionSmooth = 12f;
    public float minDistance = 0.5f;
    public LayerMask wallMask;

    [Header("Gravity Transition")]
    public float gravityRotationSmooth = 6f;

    Vector3 currentOffset;
    Vector3 targetGravityUp;
    Vector3 currentGravityUp;

    void Start()
    {
        targetGravityUp = Vector3.up;        
        currentGravityUp = Vector3.up;
        currentOffset = new Vector3(0, height, -distance);

        pitch = transform.eulerAngles.x;
        if (pitch > 180f) pitch -= 360f;
        
        currentForward = Vector3.ProjectOnPlane(transform.forward, currentGravityUp).normalized;
        if (currentForward.sqrMagnitude < 0.001f) 
            currentForward = Vector3.forward;
    }

    void LateUpdate()
    {
        if (!target) return;

        // Smoothly rotate the gravity up vector for smooth transition
        currentGravityUp = Vector3.Slerp(currentGravityUp, targetGravityUp, Time.deltaTime * gravityRotationSmooth).normalized;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivityX * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivityY * Time.deltaTime;

        if (mouseX != 0f)
        {
            currentForward = Quaternion.AngleAxis(mouseX, currentGravityUp) * currentForward;
        }

        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // Keep currentForward orthogonal to the transitioning gravity up
        currentForward = Vector3.ProjectOnPlane(currentForward, currentGravityUp).normalized;
        if (currentForward.sqrMagnitude < 0.001f)
        {
            currentForward = Vector3.ProjectOnPlane(target.forward, currentGravityUp).normalized;
            if (currentForward.sqrMagnitude < 0.001f)
                currentForward = Vector3.right; // Ultimate fallback
        }

        // Build base rotation
        Quaternion yawRot = Quaternion.LookRotation(currentForward, currentGravityUp);
        Quaternion pitchRotLocal = Quaternion.Euler(pitch, 0, 0);
        Quaternion rot = yawRot * pitchRotLocal;

        Vector3 desiredOffset = rot * new Vector3(0, height, -distance);

        Vector3 targetPos = target.position;
        Vector3 idealPos = targetPos + desiredOffset;

        Vector3 direction = (idealPos - targetPos).normalized;
        float maxDist = desiredOffset.magnitude;

        if (Physics.Raycast(targetPos, direction, out RaycastHit hit, maxDist, wallMask))
        {
            float dist = Mathf.Clamp(hit.distance - 0.1f, minDistance, maxDist);
            currentOffset = Vector3.Lerp(
                currentOffset,
                direction * dist,
                Time.deltaTime * collisionSmooth
            );
        }
        else
        {
            currentOffset = Vector3.Lerp(
                currentOffset,
                desiredOffset,
                Time.deltaTime * collisionSmooth
            );
        }

        Vector3 finalPos = targetPos + currentOffset;

        transform.position = Vector3.Lerp(
            transform.position,
            finalPos,
            Time.deltaTime * followSmooth
        );

        // Final look rotation aiming exactly at the target but aligned firmly with currentGravityUp
        Vector3 lookDir = (target.position - transform.position).normalized;
        if (lookDir.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(lookDir, currentGravityUp);
        }
    }

    public void RotateCameraToGravity(Vector3 newGravityDir)
    {
        targetGravityUp = -newGravityDir.normalized;
        // Do not touch currentGravityUp or currentForward here, letting LateUpdate() transition them smoothly.
    }

    public void ResetCameraState()
    {
        currentForward = Vector3.forward;
        pitch = 0f;

        targetGravityUp = Vector3.up;
        currentGravityUp = Vector3.up;

        transform.rotation = Quaternion.identity;

        currentOffset = new Vector3(0, height, -distance);
    }
}
