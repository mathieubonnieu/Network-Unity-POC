using Unity.Netcode;
using UnityEngine.InputSystem;
using UnityEngine;

public class TakeItem : NetworkBehaviour // Utiliser NetworkBehaviour au lieu de MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform itemSpot;
    [SerializeField] private InputActionAsset inputActions;

    [Header("Pickup Settings")]
    [SerializeField] private float pickupRadius = 3f;
    [SerializeField] private float throwStrength = 15f;
    [SerializeField] private float upwardForce = 2f; // Pour l'effet de parabole

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

    // --- INITIALISATION ---

    private void Awake()
    {
        movementController = GetComponent<NetworkTransformTest>();
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
    }

    private void Update()
    {
        // Seul celui qui possède le joueur voit la ligne de visée
        if (!IsOwner || carriedItem == null)
        {
            lineRenderer.enabled = false;
            return;
        }

        // Même logique que Interact: l'état est piloté par callbacks Input System
        if (isTrajectoryActive)
        {
            DrawProjection();
        }
        else
        {
            lineRenderer.enabled = false;
        }
    }

    private void DrawProjection()
    {
        // Utilise exactement la même logique que ton ancien script
        // Mais assure-toi d'utiliser 'throwStrength' et 'upwardForce' 
        // pour que la ligne corresponde au futur lancer !

        lineRenderer.enabled = true;
        lineRenderer.positionCount = linePoints;

        Vector3 startPosition = releasePosition.position;
        // On simule la force du lancer : (Forward * Strength) + (Up * UpwardForce)
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
            // Si on porte déjà, on lance
            RequestDropServerRpc();
        }
        else
        {
            // Sinon, on cherche l'objet le plus proche
            TakableItem nearest = FindNearestItem();
            if (nearest != null)
            {
                // On demande au serveur de nous donner l'objet
                RequestPickupServerRpc(nearest.GetComponent<NetworkObject>().NetworkObjectId);
            }
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

    // --- LOGIQUE RAMASSAGE (SERVER SIDE) ---

    [ServerRpc]
    private void RequestPickupServerRpc(ulong networkObjectId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject targetNetObj))
        {
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

        NetworkObject itemNetObj = item.GetComponent<NetworkObject>();
        // On prend le transform du script actuel (qui est sur la racine du joueur avec le NetworkObject)
        Transform playerRoot = this.transform;

        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.mass = item.mass;
            rb.mass = item.mass; // Appliquer la masse de l'objet
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Place d'abord l'objet en main en world-space, puis parente en conservant la pose world.
        item.transform.SetPositionAndRotation(itemSpot.position, itemSpot.rotation);

        // On utilise playerRoot au lieu de itemSpot
        itemNetObj.TrySetParent(playerRoot, true);

        // Ajuste l'ancrage local pour qu'il colle exactement au point de main.
        item.transform.localPosition = playerRoot.InverseTransformPoint(itemSpot.position);
        item.transform.localRotation = Quaternion.Inverse(playerRoot.rotation) * itemSpot.rotation;

        ApplyCarryMass(item.mass);

        NotifyClientPickupClientRpc(itemNetObj.NetworkObjectId);
    }

    // --- LOGIQUE LANCER (SERVER SIDE) ---

    [ServerRpc]
    private void RequestDropServerRpc()
    {
        if (carriedItem == null) return;

        // On retire le parent
        carriedItem.GetComponent<NetworkObject>().TryRemoveParent(true);

        Rigidbody rb = carriedItem.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;

            carriedItem.transform.SetPositionAndRotation(releasePosition.position, releasePosition.rotation);

            // CALCUL DU LANCER (Moving Out Style)
            // On prend l'avant du joueur + un peu de hauteur
            Vector3 forceDir = releasePosition.forward * throwStrength + Vector3.up * upwardForce;

            rb.AddForce(forceDir, ForceMode.Impulse);

            // Rotation aléatoire pour le côté chaotique
            rb.AddTorque(Random.insideUnitSphere * 10f, ForceMode.Impulse);
        }

        carriedItem.isTaken = false;
        carriedItem.SetHeldState(false);
        ApplyCarryMass(0f);
        NotifyClientDropClientRpc();
        carriedItem = null;
    }

    // --- SYNCHRONISATION DES VARIABLES LOCALES ---

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

    private TakableItem FindNearestItem()
    {
        TakableItem[] items = FindObjectsByType<TakableItem>(FindObjectsSortMode.None);
        TakableItem nearestItem = null;
        float bestDistanceSqr = pickupRadius * pickupRadius;

        foreach (var item in items)
        {
            if (item.isTaken) continue;
            float distSqr = (item.transform.position - transform.position).sqrMagnitude;
            if (distSqr < bestDistanceSqr)
            {
                bestDistanceSqr = distSqr;
                nearestItem = item;
            }
        }
        return nearestItem;
    }
}