using Unity.Netcode;
using UnityEngine.InputSystem;
using UnityEngine;

public class NetworkTransformTest : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float turnSpeed = 12f;
    [SerializeField] private InputActionAsset inputActions;

    private InputAction moveAction;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
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

    private void Update()
    {
        if (!IsOwner || moveAction == null)
        {
            return;
        }

        Vector2 moveInput = moveAction.ReadValue<Vector2>();

        Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y);
        if (inputDirection.sqrMagnitude < 0.001f)
        {
            return;
        }

        MovePlayerServerRpc(inputDirection.normalized);
    }

    [Rpc(SendTo.Server)]
    private void MovePlayerServerRpc(Vector3 direction, RpcParams rpcParams = default)
    {
        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);

        Vector3 movement = direction * moveSpeed * Time.deltaTime;
        transform.position += movement;
    }
}
