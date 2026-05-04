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
            // Ensure remote instances do not have an active camera or audio listener
            Camera remoteCam = GetComponentInChildren<Camera>(true);
            if (remoteCam != null) remoteCam.enabled = false;
            AudioListener remoteAl = GetComponentInChildren<AudioListener>(true);
            if (remoteAl != null) remoteAl.enabled = false;

            return;
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

        MovePlayerServerRpc(inputDirection.normalized);
    }

    private void ConfigureRigidbody()
    {
        if (playerRigidbody == null)
        {
            return;
        }

        // playerRigidbody.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        // playerRigidbody.angularVelocity = Vector3.zero;

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
                playerRigidbody.linearVelocity = Vector3.zero;
                playerRigidbody.angularVelocity = Vector3.zero;
            }

            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(serverMoveDirection, Vector3.up);
        playerRigidbody.MoveRotation(Quaternion.Slerp(playerRigidbody.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime));

        float speedMultiplier = 1f / (1f + carriedMass * carryMassSlowFactor);
        speedMultiplier = Mathf.Max(minSpeedMultiplier, speedMultiplier);

        Vector3 desiredVelocity = serverMoveDirection * (moveSpeed * speedMultiplier);
        desiredVelocity.y = 0f;
        playerRigidbody.MovePosition(playerRigidbody.position + desiredVelocity * Time.fixedDeltaTime);
//        playerRigidbody.linearVelocity = desiredVelocity;
        playerRigidbody.angularVelocity = Vector3.zero;
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
