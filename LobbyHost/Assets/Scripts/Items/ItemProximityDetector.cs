using System;
using UnityEngine;

public class ItemProximityDetector : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private float detectionRadius = 2f;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private LayerMask detectionLayers = ~0;

    public bool IsPlayerNearby { get; private set; }
    public event Action<bool> PlayerNearbyChanged;

    private void Update()
    {
        bool hasNearbyPlayer = HasNearbyPlayer();
        if (hasNearbyPlayer == IsPlayerNearby)
        {
            return;
        }

        IsPlayerNearby = hasNearbyPlayer;
        PlayerNearbyChanged?.Invoke(IsPlayerNearby);
    }

    private bool HasNearbyPlayer()
    {
        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, detectionRadius, detectionLayers, QueryTriggerInteraction.Collide);
        for (int i = 0; i < nearbyColliders.Length; i++)
        {
            if (nearbyColliders[i].CompareTag(playerTag))
            {
                return true;
            }
        }

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}