using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// APEX RUSH - Milestone 1
/// First-person player controller built on Unity's CharacterController.
/// Uses the Unity Input System (package "com.unity.inputsystem"); the input actions are
/// created in code so there is no input asset to configure.
///
/// Features: walk, sprint, jump, crouch (with headroom check), mouse look,
/// optional gamepad, acceleration/deceleration, adjustable sensitivity.
///
/// Setup: attach to the "Player" root GameObject (feet at the root's origin),
/// next to a CharacterController, and assign "Camera Root" in the Inspector.
/// </summary>
[RequireComponent(typeof(CharacterController))]
[DisallowMultipleComponent]
public class PlayerController : MonoBehaviour
{
    private const string SensitivityPrefKey = "ApexRush.MouseSensitivity";

    [Header("References")]
    [Tooltip("Child object that holds the Camera. It is pitched up/down; the Player root is turned left/right.")]
    [SerializeField] private Transform cameraRoot;

    [Header("Movement (metres per second)")]
    [SerializeField] private float walkSpeed = 4.5f;
    [SerializeField] private float sprintSpeed = 7.5f;
    [SerializeField] private float crouchSpeed = 2.2f;
    [Tooltip("How quickly the player speeds up (m/s per second).")]
    [SerializeField] private float acceleration = 25f;
    [Tooltip("How quickly the player slows down when no key is held.")]
    [SerializeField] private float deceleration = 30f;
    [Tooltip("Fraction of acceleration available while in the air (0 = none, 1 = full).")]
    [Range(0f, 1f)] [SerializeField] private float airControl = 0.35f;
    [SerializeField] private float jumpHeight = 1.1f;
    [Tooltip("Negative number. Stronger than real gravity feels better in games.")]
    [SerializeField] private float gravity = -22f;

    [Header("Crouch")]
    [SerializeField] private float standingHeight = 1.8f;
    [SerializeField] private float crouchHeight = 1.1f;
    [SerializeField] private float standingEyeHeight = 1.65f;
    [SerializeField] private float crouchEyeHeight = 0.95f;
    [Tooltip("Metres per second the capsule grows/shrinks while changing stance.")]
    [SerializeField] private float crouchTransitionSpeed = 5f;
    [Tooltip("ON: press once to crouch, again to stand. OFF: hold to crouch.")]
    [SerializeField] private bool crouchIsToggle = true;

    [Header("Look")]
    [Tooltip("Degrees of rotation per pixel of mouse movement. Change in play mode with [ and ] keys.")]
    [SerializeField] private float mouseSensitivity = 0.12f;
    [Tooltip("Degrees per second at full right-stick deflection.")]
    [SerializeField] private float gamepadLookSpeed = 140f;
    [SerializeField] private float maxPitch = 85f;
    [SerializeField] private bool invertY = false;

    [Header("Safety")]
    [Tooltip("If the player falls below this world height they are respawned at their start position.")]
    [SerializeField] private float killHeight = -30f;

    // ---- Runtime state ----
    private CharacterController controller;
    private InputAction moveAction, lookAction, jumpAction, sprintAction, crouchAction;
    private Vector3 horizontalVelocity;
    private float verticalVelocity;
    private float pitch;
    private bool wantsCrouch;
    private bool isCrouching;
    private bool isSprinting;
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private readonly Collider[] overlapBuffer = new Collider[8];

    // ---- Read-only info for HUD/debug/other systems ----
    public float CurrentSpeed => horizontalVelocity.magnitude;
    public bool IsGrounded => controller != null && controller.isGrounded;
    public bool IsCrouching => isCrouching;
    public bool IsSprinting => isSprinting;
    public float MouseSensitivity => mouseSensitivity;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        mouseSensitivity = PlayerPrefs.GetFloat(SensitivityPrefKey, mouseSensitivity);
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
        BuildInputActions();

        if (cameraRoot == null)
        {
            Debug.LogError("PlayerController: 'Camera Root' is not assigned in the Inspector.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        moveAction?.Enable();
        lookAction?.Enable();
        jumpAction?.Enable();
        sprintAction?.Enable();
        crouchAction?.Enable();
    }

    private void OnDisable()
    {
        moveAction?.Disable();
        lookAction?.Disable();
        jumpAction?.Disable();
        sprintAction?.Disable();
        crouchAction?.Disable();
    }

    private void OnDestroy()
    {
        moveAction?.Dispose();
        lookAction?.Dispose();
        jumpAction?.Dispose();
        sprintAction?.Dispose();
        crouchAction?.Dispose();
    }

    private void Start()
    {
        SetCursorLocked(true);
        ApplyCrouchShape(isCrouching ? crouchHeight : standingHeight);
    }

    /// <summary>Creates all input actions in code (keyboard + mouse + optional gamepad).</summary>
    private void BuildInputActions()
    {
        moveAction = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        moveAction.AddBinding("<Gamepad>/leftStick");

        lookAction = new InputAction("Look", InputActionType.Value, expectedControlType: "Vector2");
        lookAction.AddBinding("<Mouse>/delta");
        lookAction.AddBinding("<Gamepad>/rightStick");

        jumpAction = new InputAction("Jump", InputActionType.Button);
        jumpAction.AddBinding("<Keyboard>/space");
        jumpAction.AddBinding("<Gamepad>/buttonSouth");

        sprintAction = new InputAction("Sprint", InputActionType.Button);
        sprintAction.AddBinding("<Keyboard>/leftShift");
        sprintAction.AddBinding("<Gamepad>/leftStickPress");

        crouchAction = new InputAction("Crouch", InputActionType.Button);
        crouchAction.AddBinding("<Keyboard>/leftCtrl");
        crouchAction.AddBinding("<Keyboard>/c");
        crouchAction.AddBinding("<Gamepad>/buttonEast");
    }

    private void Update()
    {
        HandleCursorAndHotkeys();

        // Ignore gameplay input while the cursor is released (e.g. after pressing Esc).
        bool hasFocus = Cursor.lockState == CursorLockMode.Locked;
        Vector2 moveInput = hasFocus ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        bool jumpPressed = hasFocus && jumpAction.WasPressedThisFrame();

        if (hasFocus)
        {
            HandleLook();
        }

        HandleCrouch(hasFocus);
        HandleMovement(moveInput, jumpPressed);

        if (transform.position.y < killHeight)
        {
            Respawn();
        }
    }

    // ------------------------------------------------------------------ cursor / hotkeys
    private void HandleCursorAndHotkeys()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        if (keyboard != null)
        {
            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                SetCursorLocked(false);
            }
            // Testing aid: adjust sensitivity while playing.
            if (keyboard.leftBracketKey.wasPressedThisFrame) SetMouseSensitivity(mouseSensitivity - 0.02f);
            if (keyboard.rightBracketKey.wasPressedThisFrame) SetMouseSensitivity(mouseSensitivity + 0.02f);
        }

        if (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
        {
            SetCursorLocked(true);
        }
    }

    private static void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    /// <summary>Sets and saves mouse sensitivity (degrees per pixel). Call this from a settings menu later.</summary>
    public void SetMouseSensitivity(float value)
    {
        mouseSensitivity = Mathf.Clamp(value, 0.02f, 1f);
        PlayerPrefs.SetFloat(SensitivityPrefKey, mouseSensitivity);
    }

    // ------------------------------------------------------------------ look
    private void HandleLook()
    {
        Vector2 look = lookAction.ReadValue<Vector2>();
        if (look == Vector2.zero) return;

        // Mouse delta is already "per frame", the gamepad stick is a rate and needs deltaTime.
        bool fromGamepad = lookAction.activeControl != null && lookAction.activeControl.device is Gamepad;
        Vector2 delta = fromGamepad ? look * (gamepadLookSpeed * Time.deltaTime) : look * mouseSensitivity;

        transform.Rotate(0f, delta.x, 0f);

        pitch -= invertY ? -delta.y : delta.y;
        pitch = Mathf.Clamp(pitch, -maxPitch, maxPitch);
        cameraRoot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    // ------------------------------------------------------------------ crouch
    private void HandleCrouch(bool hasFocus)
    {
        if (hasFocus)
        {
            if (crouchIsToggle)
            {
                if (crouchAction.WasPressedThisFrame()) wantsCrouch = !wantsCrouch;
            }
            else
            {
                wantsCrouch = crouchAction.IsPressed();
            }
        }

        if (wantsCrouch)
        {
            isCrouching = true;
        }
        else if (isCrouching && HasHeadroomToStand())
        {
            isCrouching = false;
        }

        float targetHeight = isCrouching ? crouchHeight : standingHeight;
        float newHeight = Mathf.MoveTowards(controller.height, targetHeight, crouchTransitionSpeed * Time.deltaTime);
        ApplyCrouchShape(newHeight);
    }

    /// <summary>Resizes the capsule (feet stay fixed) and moves the camera to match.</summary>
    private void ApplyCrouchShape(float height)
    {
        controller.height = height;
        controller.center = new Vector3(0f, height * 0.5f, 0f);

        float t = Mathf.InverseLerp(crouchHeight, standingHeight, height);
        float eyeHeight = Mathf.Lerp(crouchEyeHeight, standingEyeHeight, t);
        cameraRoot.localPosition = new Vector3(0f, eyeHeight, 0f);
    }

    /// <summary>True if there is nothing solid above the player that would block standing up.</summary>
    private bool HasHeadroomToStand()
    {
        float radius = controller.radius * 0.95f;
        Vector3 topSphere = transform.position + Vector3.up * (standingHeight - controller.radius);
        int count = Physics.OverlapSphereNonAlloc(topSphere, radius, overlapBuffer, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            if (overlapBuffer[i] != controller) return false; // ignore our own capsule
        }
        return true;
    }

    // ------------------------------------------------------------------ movement
    private void HandleMovement(Vector2 moveInput, bool jumpPressed)
    {
        bool grounded = controller.isGrounded;

        isSprinting = grounded && !isCrouching && moveInput.y > 0.1f && sprintAction.IsPressed();

        float targetSpeed = isCrouching ? crouchSpeed : (isSprinting ? sprintSpeed : walkSpeed);
        Vector3 wishDirection = Vector3.ClampMagnitude(transform.right * moveInput.x + transform.forward * moveInput.y, 1f);
        Vector3 targetVelocity = wishDirection * targetSpeed;

        float rate = targetVelocity.sqrMagnitude > 0.0001f ? acceleration : deceleration;
        if (!grounded) rate *= airControl;
        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, rate * Time.deltaTime);

        if (grounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f; // small push keeps the controller glued to slopes/steps
        }
        if (jumpPressed && grounded && !isCrouching)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = horizontalVelocity + Vector3.up * verticalVelocity;
        CollisionFlags flags = controller.Move(velocity * Time.deltaTime);

        if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0f)
        {
            verticalVelocity = 0f; // bumped our head
        }
    }

    // ------------------------------------------------------------------ respawn
    /// <summary>Teleports the player back to where they started. PlayerHealth will call this in a later milestone.</summary>
    public void Respawn()
    {
        controller.enabled = false; // CharacterController must be off to be teleported
        transform.SetPositionAndRotation(spawnPosition, spawnRotation);
        controller.enabled = true;
        horizontalVelocity = Vector3.zero;
        verticalVelocity = 0f;
        pitch = 0f;
        cameraRoot.localRotation = Quaternion.identity;
    }
}
