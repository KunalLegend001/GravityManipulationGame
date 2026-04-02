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

    float yaw;   // Horizontal rotation
    float pitch; // Vertical rotation

    [Header("Follow")]
    public float followSmooth = 8f;

    [Header("Collision")]
    public float collisionSmooth = 12f;
    public float minDistance = 0.5f;
    public LayerMask wallMask;

    Vector3 currentOffset;
    Vector3 gravityUp;

    void Start()
    {
        gravityUp = Vector3.up;        // default
        currentOffset = new Vector3(0, height, -distance);

        // Initialize yaw/pitch from current camera rotation
        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;
    }

    void LateUpdate()
    {
        if (!target) return;

        // ==============================
        // 1) GET MOUSE INPUT
        // ==============================
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivityX * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivityY * Time.deltaTime;

        yaw += mouseX;
        pitch -= mouseY;

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // ==============================
        // 2) BUILD CAMERA ROTATION USING GRAVITY UP DIRECTION
        // ==============================
        // Horizontal rotation around gravity
        Quaternion yawRot = Quaternion.AngleAxis(yaw, gravityUp);

        // Stable right axis AFTER yaw rotation
        Vector3 stableRight = yawRot * Vector3.right;

        // Vertical rotation around stable axis
        Quaternion pitchRot = Quaternion.AngleAxis(pitch, stableRight);

        // Final rotation
        Quaternion rot = pitchRot * yawRot;


        Vector3 desiredOffset = rot * new Vector3(0, height, -distance);

        // ==============================
        // 3) COLLISION CHECK
        // ==============================
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

        // ==============================
        // 4) APPLY POSITION + LOOK AT PLAYER
        // ==============================
        Vector3 finalPos = targetPos + currentOffset;

        transform.position = Vector3.Lerp(
            transform.position,
            finalPos,
            Time.deltaTime * followSmooth
        );

        transform.rotation = Quaternion.LookRotation(
            (target.position - transform.position).normalized,
            gravityUp
        );
    }


    // =====================================================
    // GRAVITY CHANGE — ROTATE CAMERA SYSTEM
    // =====================================================
    public void RotateCameraToGravity(Vector3 newGravityDir)
    {
        // 1) Update gravity up direction
        gravityUp = -newGravityDir.normalized;

        // 2) Build a flat forward direction (projected on new up)
        Vector3 flatForward = Vector3.ProjectOnPlane(transform.forward, gravityUp).normalized;

        if (flatForward.sqrMagnitude < 0.001f)
            flatForward = Vector3.ProjectOnPlane(target.forward, gravityUp).normalized;

        // 3) Create a corrected rotation -- NO TILT
        Quaternion correctedRot = Quaternion.LookRotation(flatForward, gravityUp);

        // Apply rotation instantly
        transform.rotation = correctedRot;

        // 4) Extract yaw and pitch from corrected rotation
        Vector3 e = correctedRot.eulerAngles;

        yaw = e.y;

        // Recompute pitch by measuring angle between forward and its projected flat version
        Vector3 projected = Vector3.ProjectOnPlane(correctedRot * Vector3.forward, gravityUp);
        pitch = Vector3.SignedAngle(projected, correctedRot * Vector3.forward, correctedRot * Vector3.right);

        // Clamp pitch again
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }


    public void ResetCameraState()
    {
        yaw = 0f;
        pitch = 0f;

        gravityUp = Vector3.up;

        transform.rotation = Quaternion.identity;

        currentOffset = new Vector3(0, height, -distance);
    }

}
