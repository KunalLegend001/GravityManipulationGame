using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float jumpForce = 6f;

    [Header("Gravity Control")]
    public float gravityStrength = 9.81f;
    public GameObject hologramPrefab;

    [Header("Hologram Adjustments")]
    public float hologramYOffset = 0.0f;           // move hologram up/down
    public Vector3 hologramExtraRotation = Vector3.zero; // fine-tune rotation


    [Header("Ground Check")]
    public float groundCheckRadius = 0.35f;
    public float groundCheckOffset = 0.2f;
    public LayerMask groundLayer;

    [Header("References")]
    public Animator animator;
    public Transform cameraTransform;
    public CameraFollow cameraFollow;

    Rigidbody rb;
    GameObject hologram;

    Vector3 moveInput = Vector3.zero;
    public bool isGrounded = true;

    Vector3 selectedGravity = Vector3.down;
    public GameManager manager;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;
    }

    void Start()
    {
        if (hologramPrefab != null)
        {
            hologram = Instantiate(hologramPrefab);
            hologram.SetActive(false);
        }
    }

    void Update()
    {
        ReadMovementInput();
        HandleGravitySelection();
        HandleJump();
        PlayAnimations();
    }

    void FixedUpdate()
    {
        CheckGround();
        MovePlayer();
    }

    // ---------------------------------------------------------
    // MOVEMENT INPUT (WASD ONLY)
    // ---------------------------------------------------------
    void ReadMovementInput()
    {
        float h = 0f;
        float v = 0f;

        if (Input.GetKey(KeyCode.W)) v = 1f;
        if (Input.GetKey(KeyCode.S)) v = -1f;
        if (Input.GetKey(KeyCode.A)) h = -1f;
        if (Input.GetKey(KeyCode.D)) h = 1f;

        Vector3 up = -Physics.gravity.normalized;

        Vector3 camForward = Vector3.ProjectOnPlane(cameraTransform.forward, up).normalized;
        Vector3 camRight = Vector3.ProjectOnPlane(cameraTransform.right, up).normalized;

        moveInput = (camForward * v + camRight * h).normalized;
    }

    // ---------------------------------------------------------
    // MOVE
    // ---------------------------------------------------------
    void MovePlayer()
    {
        Vector3 currentVel = rb.linearVelocity;

        Vector3 horizontal = moveInput * moveSpeed;
        Vector3 vertical = Vector3.Project(currentVel, Physics.gravity.normalized);

        rb.linearVelocity = horizontal + vertical;

        Vector3 up = -Physics.gravity.normalized;
        Vector3 flatInput = Vector3.ProjectOnPlane(moveInput, up).normalized;

        if (flatInput.sqrMagnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(flatInput, up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.fixedDeltaTime * 10f);
        }
    }

    // ---------------------------------------------------------
    // JUMP
    // ---------------------------------------------------------
    void HandleJump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.AddForce(-Physics.gravity.normalized * jumpForce, ForceMode.Impulse);
            animator.SetTrigger("Jump");
        }
    }

    // ---------------------------------------------------------
    // GRAVITY ARROW SYSTEM
    // ---------------------------------------------------------
    void HandleGravitySelection()
    {
        Vector3 up = -Physics.gravity.normalized;

        // CAMERA-BASED AXIS that stays stable relative to gravity
        Vector3 forward = Vector3.ProjectOnPlane(cameraTransform.forward, up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(cameraTransform.right, up).normalized;

        Vector3 newDir = Vector3.zero;
        bool pressed = false;

        if (Input.GetKey(KeyCode.UpArrow)) { newDir = forward; pressed = true; }
        if (Input.GetKey(KeyCode.DownArrow)) { newDir = -forward; pressed = true; }
        if (Input.GetKey(KeyCode.LeftArrow)) { newDir = -right; pressed = true; }
        if (Input.GetKey(KeyCode.RightArrow)) { newDir = right; pressed = true; }

        if (pressed)
        {
            selectedGravity = SnapToCardinal(newDir);
            ShowHologram(-selectedGravity);   // place hologram opposite gravity
        }
        else
        {
            HideHologram();
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            ApplyGravity(selectedGravity);
            HideHologram();
        }
    }

    Vector3 SnapToCardinal(Vector3 dir)
    {
        Vector3[] cardinals = new Vector3[] {
            Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back
        };

        Vector3 best = dir;
        float maxDot = -1f;

        foreach (var c in cardinals)
        {
            float d = Vector3.Dot(dir, c);
            if (d > maxDot)
            {
                maxDot = d;
                best = c;
            }
        }
        return best;
    }





    // ---------------------------------------------------------
    // MANUAL HOLOGRAM POSITION
    // ---------------------------------------------------------
    void ShowHologram(Vector3 dir)
    {
        if (hologram == null) return;

        hologram.SetActive(true);

        Vector3 up = -Physics.gravity.normalized;

        // Base spawn distance
        float distance = -1f;

        // Move hologram in the chosen direction
        Vector3 basePos = transform.position + (dir.normalized * distance);

        // Apply Y offset (relative to gravity)
        basePos += up * hologramYOffset;

        hologram.transform.position = basePos;

        // Make hologram face opposite of direction OR your preference
        hologram.transform.rotation = Quaternion.LookRotation(dir, up);

        // Rotate 90 degrees to lie flat on wall
        hologram.transform.rotation *= Quaternion.Euler(90, 0, 0);

        // Apply user-defined rotation offset
        hologram.transform.rotation *= Quaternion.Euler(hologramExtraRotation);
    }



    void HideHologram()
    {
        if (hologram != null)
            hologram.SetActive(false);
    }

    // ---------------------------------------------------------
    // APPLY GRAVITY
    // ---------------------------------------------------------
    void ApplyGravity(Vector3 dir)
    {
        Physics.gravity = dir.normalized * gravityStrength;

        StartCoroutine(RotateToNewUp());

        if (cameraFollow != null)
            cameraFollow.RotateCameraToGravity(dir);
    }

    System.Collections.IEnumerator RotateToNewUp()
    {
        Quaternion start = transform.rotation;
        Quaternion end = Quaternion.FromToRotation(transform.up, -Physics.gravity.normalized) * transform.rotation;

        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * 4f;
            transform.rotation = Quaternion.Slerp(start, end, t);
            yield return null;
        }
    }

    // ---------------------------------------------------------
    // GROUND CHECK (WIRESPHERE)
    // ---------------------------------------------------------
    void CheckGround()
    {
        Vector3 origin = transform.position + (-Physics.gravity.normalized) * groundCheckOffset;

        isGrounded = Physics.CheckSphere(origin, groundCheckRadius, groundLayer);
    }

    // ---------------------------------------------------------
    // ANIMATIONS
    // ---------------------------------------------------------
    void PlayAnimations()
    {
        Vector3 gravUp = Physics.gravity.normalized;

        float verticalVel = Vector3.Dot(rb.linearVelocity, -gravUp);

        bool running = moveInput.magnitude > 0.1f && isGrounded;
       // bool jumping = !isGrounded && verticalVel > 0.1f;
        bool falling = !isGrounded && verticalVel < -5f;

        animator.SetBool("IsRunning", running);
        
        animator.SetBool("IsFalling", falling);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("GameOvertrigger"))
        {
            manager.GameOver("");
        }
    }
}
