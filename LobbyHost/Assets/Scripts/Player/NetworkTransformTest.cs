using Unity.Netcode;
using UnityEngine.InputSystem;
using UnityEngine;

public class NetworkTransformTest : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float turnSpeed = 12f;
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private float carryMassSlowFactor = 0.12f;
    [SerializeField] private float minSpeedMultiplier = 0.45f;
    [SerializeField] private PhysicsMaterial playerContactMaterial;

    private InputAction moveAction;
    private Rigidbody playerRigidbody;
    private CapsuleCollider playerCollider;
    private float carriedMass;
    private Vector3 serverMoveDirection;

    [SerializeField] private bool shouldFaceMoveDirection = false;

    private void Awake()
    {
        playerRigidbody = GetComponent<Rigidbody>();
        playerCollider = GetComponent<CapsuleCollider>();
    }

    public override void OnNetworkSpawn()
    {
        ConfigureRigidbody();

        if (!IsOwner)
        {
            var camCtrl = GetComponentInChildren<PlayerCameraController>(true);
            if (camCtrl != null)
            {
                camCtrl.DisableForRemote();
            }

            return;
        }
        // For owner: delegate camera activation to PlayerCameraController if present
        var cameraController = GetComponentInChildren<PlayerCameraController>(true);
        if (cameraController != null)
        {
            cameraController.SetupForOwner();
        }

        InitializeInputAction();
    }

    public override void OnNetworkDespawn()
    {
        if (moveAction != null)
        {
            moveAction.Disable();
            moveAction = null;
        }
    }

    private void Update()
    {
        if (!IsOwner || moveAction == null)
        {
            return;
        }

        Vector2 moveInput = moveAction.ReadValue<Vector2>();

        Vector3 inputDirection = new(moveInput.x, 0f, moveInput.y);
        if (inputDirection.sqrMagnitude < 0.001f)
        {
            MovePlayerServerRpc(Vector3.zero);
            return;
        }

        Vector3 moveDirection = inputDirection.normalized;
        Transform camTransform = null;
        var camCtrl = GetComponentInChildren<PlayerCameraController>(true);
        if (camCtrl != null)
        {
            camTransform = camCtrl.CameraTransform;
        }
        if (camTransform == null)
        {
            var ownerCam = GetComponentInChildren<Camera>(true);
            if (ownerCam != null) camTransform = ownerCam.transform;
        }

        if (camTransform != null)
        {
            Vector3 forward = camTransform.forward;
            Vector3 right = camTransform.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            moveDirection = right * inputDirection.x + forward * inputDirection.z;
            if (moveDirection.sqrMagnitude > 0.001f)
            {
                moveDirection.Normalize();
            }
        }

        MovePlayerServerRpc(moveDirection);
    }

    private void ConfigureRigidbody()
    {
        if (playerRigidbody == null)
        {
            return;
        }

        playerRigidbody.isKinematic = false;  // Allow gravity and physics
        // playerRigidbody.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        SetupContactMaterial();
    }

    private void SetupContactMaterial()
    {
        if (playerCollider == null)
        {
            return;
        }

        playerCollider.material = playerContactMaterial;
    }

    private void DisableLocalViewComponents()
    {
        Camera[] cameras = GetComponentsInChildren<Camera>(true);
        for (int i = 0; i < cameras.Length; i++)
        {
            cameras[i].enabled = false;
        }

        AudioListener[] audioListeners = GetComponentsInChildren<AudioListener>(true);
        for (int i = 0; i < audioListeners.Length; i++)
        {
            audioListeners[i].enabled = false;
        }

        // Disable Cinemachine components for remote players
        MonoBehaviour[] cinemachineComponents = GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < cinemachineComponents.Length; i++)
        {
            MonoBehaviour component = cinemachineComponents[i];
            if (component != null && (component.GetType().Name == "CinemachineBrain" || component.GetType().Name == "CinemachineCamera"))
            {
                component.enabled = false;
            }
        }
    }

    private void InitializeInputAction()
    {
        if (inputActions == null)
        {
            Debug.LogWarning("NetworkTransformTest: Input Actions asset is not assigned.");
            return;
        }

        moveAction = inputActions.FindActionMap("Player", true).FindAction("Move", true);
        moveAction.Enable();
    }

    

    private void FixedUpdate()
    {
        if (!IsServer || playerRigidbody == null || serverMoveDirection.sqrMagnitude < 0.0001f)
        {
            if (IsServer && playerRigidbody != null)
            {
                // Zero horizontal velocity but preserve vertical (gravity)
                playerRigidbody.linearVelocity = new Vector3(0, playerRigidbody.linearVelocity.y, 0);
                playerRigidbody.angularVelocity = Vector3.zero;
            }

            return;
        }

        float speedMultiplier = 1f / (1f + carriedMass * carryMassSlowFactor);
        speedMultiplier = Mathf.Max(minSpeedMultiplier, speedMultiplier);

        Vector3 moveDirection = serverMoveDirection;
        Vector3 horizontalVelocity = moveDirection * moveSpeed * speedMultiplier;
        
        // Apply velocity: horizontal movement + preserve vertical (gravity)
        Vector3 newVelocity = new Vector3(horizontalVelocity.x, playerRigidbody.linearVelocity.y, horizontalVelocity.z);
        playerRigidbody.linearVelocity = newVelocity;

        if(shouldFaceMoveDirection && moveDirection.sqrMagnitude > 0.001f)
        {
            Quaternion toRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            playerRigidbody.rotation = Quaternion.Slerp(playerRigidbody.rotation, toRotation, turnSpeed * Time.fixedDeltaTime);
        }
    }

    [Rpc(SendTo.Server)]
    private void MovePlayerServerRpc(Vector3 direction, RpcParams rpcParams = default)
    {
        serverMoveDirection = direction;
    }

    public void SetCarriedMass(float mass)
    {
        carriedMass = Mathf.Max(0f, mass);
    }
}
