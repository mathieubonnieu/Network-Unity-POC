using Unity.Netcode;
using UnityEngine.InputSystem;
using UnityEngine;
using System.Collections.Generic;

public class TakeItem : NetworkBehaviour // Utiliser NetworkBehaviour au lieu de MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform itemSpot;
    [SerializeField] private InputActionAsset inputActions;

    [Header("Pickup Settings")]
    [SerializeField] private float pickupRadius = 3f;
    [SerializeField] private float throwStrength = 15f;
    [SerializeField] private float upwardForce = 2f; // Pour l'effet de parabole
    [SerializeField] private float visionThreshold = 0.85f;

    private InputAction interactAction;
    private InputAction trajectoryAction;
    private bool isTrajectoryActive;
    private TakableItem carriedItem;
    private NetworkTransformTest movementController;

    [Header("ThrowItem")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Transform releasePosition;

    [Header("Display Controls")]
    [SerializeField]
    [Range(0, 100)]
    private int linePoints = 25;
    [SerializeField]
    [Range(0.01f, 0.25f)]
    private float timeBetweenPoints = 0.1f;

    // Liste des items en range du player
    private List<TakableItem> itemsInRange = new List<TakableItem>();
    private TakableItem nearestItem;
    private Collider pickupCollider;
    private Vector3 lastPosition;
    private Vector3 lastForward;

    [Header("Recheck Settings")]
    [SerializeField] private float moveRecheckDistance = 0.05f;
    [SerializeField] private float rotateRecheckAngle = 2f;

    [Header("Status")]
    public bool isStuned = true;
    public StatusEffects currentPlayerEffects;
    private void Awake()
    {
        movementController = GetComponent<NetworkTransformTest>();
        currentPlayerEffects = GetComponent<StatusEffects>();
        
        // Créer une SphereCollider DÉDIÉE en trigger pour la détection
        // (séparée du collider de physique du player)
        SphereCollider detectionCollider = gameObject.AddComponent<SphereCollider>();
        detectionCollider.radius = pickupRadius;
        detectionCollider.isTrigger = true;
        detectionCollider.name = "PickupDetectionTrigger";
        pickupCollider = detectionCollider;

        lastPosition = transform.position;
        lastForward = transform.forward;
    }

    public void SetPickupRange(float newRange)
    {
        pickupRadius = newRange;
        if (pickupCollider is SphereCollider sphereCollider)
        {
            sphereCollider.radius = newRange;
        }
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return; // Seul le possesseur du perso gère les inputs

        var map = inputActions.FindActionMap("Player", true);
        interactAction = map.FindAction("Interact", true);
        interactAction.started += OnInteractPressed;
        interactAction.Enable();

        trajectoryAction = map.FindAction("Attack", true);
        trajectoryAction.started += OnTrajectoryStarted;
        trajectoryAction.canceled += OnTrajectoryCanceled;
        trajectoryAction.Enable();
    }

    public override void OnNetworkDespawn()
    {
        if (interactAction != null)
        {
            interactAction.started -= OnInteractPressed;
            interactAction.Disable();
            interactAction = null;
        }

        if (trajectoryAction != null)
        {
            trajectoryAction.started -= OnTrajectoryStarted;
            trajectoryAction.canceled -= OnTrajectoryCanceled;
            trajectoryAction.Disable();
            trajectoryAction = null;
        }

        isTrajectoryActive = false;
        ApplyCarryMass(0f);
        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }
        
        itemsInRange.Clear();
        nearestItem = null;
    }

    private void Update()
    {
        if (IsOwner && carriedItem == null && HasPlayerMovedOrRotated())
        {
            CalculateNearestItem();
        }
        
        // Seul celui qui possède le joueur voit la ligne de visée
        if (!IsOwner || carriedItem == null)
        {
            lineRenderer.enabled = false;
            return;
        }

        if (isTrajectoryActive)
        {
            DrawProjection();
        }
        else
        {
            lineRenderer.enabled = false;
        }
    }

    private bool HasPlayerMovedOrRotated()
    {
        float moveDelta = (transform.position - lastPosition).sqrMagnitude;
        float moveThreshold = moveRecheckDistance * moveRecheckDistance;
        bool hasMoved = moveDelta >= moveThreshold;

        float angleDelta = Vector3.Angle(lastForward, transform.forward);
        bool hasRotated = angleDelta >= rotateRecheckAngle;

        if (!hasMoved && !hasRotated)
        {
            return false;
        }

        lastPosition = transform.position;
        lastForward = transform.forward;
        return true;
    }

    private void DrawProjection()
    {
        lineRenderer.enabled = true;
        lineRenderer.positionCount = linePoints;

        Vector3 startPosition = releasePosition.position;
        Vector3 simVelocity = (releasePosition.forward * throwStrength + Vector3.up * upwardForce) / carriedItem.GetComponent<Rigidbody>().mass;

        for (int i = 0; i < linePoints; i++)
        {
            float t = i * timeBetweenPoints;
            Vector3 point = startPosition + simVelocity * t + 0.5f * Physics.gravity * t * t;
            lineRenderer.SetPosition(i, point);
        }
    }

    private void OnInteractPressed(InputAction.CallbackContext context)
    {
        if (carriedItem != null)
        {
            RequestDropServerRpc();
        }
        else if (nearestItem != null)
        {
            RequestPickupServerRpc(nearestItem.GetComponent<NetworkObject>().NetworkObjectId);
        }
    }

    private void OnTrajectoryStarted(InputAction.CallbackContext context)
    {
        isTrajectoryActive = true;
    }

    private void OnTrajectoryCanceled(InputAction.CallbackContext context)
    {
        isTrajectoryActive = false;
        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }
    }

    [ServerRpc]
    private void RequestPickupServerRpc(ulong networkObjectId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject targetNetObj))
        {
            if(currentPlayerEffects.isStuned()) return;
            TakableItem item = targetNetObj.GetComponent<TakableItem>();
            if (item != null && !item.isTaken)
            {
                PerformPickup(item);
            }
        }
    }

    private void PerformPickup(TakableItem item)
    {
        carriedItem = item;
        item.isTaken = true;
        item.SetHeldState(true);

        if (itemsInRange.Contains(item))
        {
            itemsInRange.Remove(item);
            CalculateNearestItem();
        }

        NetworkObject itemNetObj = item.GetComponent<NetworkObject>();
        Transform playerRoot = this.transform;

        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.mass = item.mass;
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        item.transform.SetPositionAndRotation(itemSpot.position, itemSpot.rotation);

        itemNetObj.TrySetParent(playerRoot, true);

        item.transform.localPosition = playerRoot.InverseTransformPoint(itemSpot.position);
        item.transform.localRotation = Quaternion.Inverse(playerRoot.rotation) * itemSpot.rotation;

        ApplyCarryMass(item.mass);
        NotifyClientPickupClientRpc(itemNetObj.NetworkObjectId);
    }

    [ServerRpc]
    private void RequestDropServerRpc()
    {
        if (carriedItem == null) return;
        carriedItem.GetComponent<NetworkObject>().TryRemoveParent(true);

        Rigidbody rb = carriedItem.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;

            carriedItem.transform.SetPositionAndRotation(releasePosition.position, releasePosition.rotation);

            Vector3 forceDir = releasePosition.forward * throwStrength + Vector3.up * upwardForce;
            rb.AddForce(forceDir, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * 10f, ForceMode.Impulse);
        }

        carriedItem.isTaken = false;
        carriedItem.isChosen = false;
        carriedItem.SetHeldState(false);
        
        float distance = Vector3.Distance(transform.position, carriedItem.transform.position);
        if (distance <= pickupRadius && !itemsInRange.Contains(carriedItem))
        {
            itemsInRange.Add(carriedItem);
            CalculateNearestItem();
        }
        
        ApplyCarryMass(0f);
        NotifyClientDropClientRpc();
        carriedItem = null;
    }

    [ClientRpc]
    private void NotifyClientPickupClientRpc(ulong id)
    {
        if (IsServer) return;
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(id, out NetworkObject obj))
        {
            carriedItem = obj.GetComponent<TakableItem>();
            if (carriedItem != null)
            {
                ApplyCarryMass(carriedItem.mass);
            }
        }
    }

    [ClientRpc]
    private void NotifyClientDropClientRpc()
    {
        if (IsServer) return;
        ApplyCarryMass(0f);
        carriedItem = null;
    }

    private void ApplyCarryMass(float mass)
    {
        if (movementController == null)
        {
            return;
        }
        movementController.SetCarriedMass(mass);
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (!IsOwner) return;

        TakableItem item = collision.GetComponent<TakableItem>();
        if (item != null && !item.isTaken && !itemsInRange.Contains(item))
        {
            itemsInRange.Add(item);
            CalculateNearestItem();
        }
    }

    private void OnTriggerExit(Collider collision)
    {
        if (!IsOwner) return;

        TakableItem item = collision.GetComponent<TakableItem>();
        if (item != null && itemsInRange.Contains(item))
        {
            itemsInRange.Remove(item);
            if (nearestItem == item)
            {
                nearestItem.isChosen = false;
            }
            CalculateNearestItem();
        }
    }

    private void CalculateNearestItem()
    {
        TakableItem previousNearestItem = nearestItem;
        nearestItem = null;
        float bestDot = visionThreshold;

        foreach (var item in itemsInRange)
        {
            if (item.isTaken)
                continue;
            Vector3 direction = (item.transform.position - transform.position).normalized;
            float dot = Vector3.Dot(direction, transform.forward);

            if (dot >= bestDot)
            {
                bestDot = dot;
                nearestItem = item;
            }
        }

        if (previousNearestItem != null && previousNearestItem != nearestItem)
        {
            previousNearestItem.isChosen = false;
        }

        if (nearestItem != null)
        {
            nearestItem.isChosen = true;
        }
    }
}
