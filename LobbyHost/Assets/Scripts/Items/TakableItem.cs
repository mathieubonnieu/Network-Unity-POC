using UnityEngine;

public class TakableItem : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private float detectionRadius = 2f;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private LayerMask detectionLayers = ~0;

    [Header("Visual")]
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private Color nearColor = Color.yellow;

    private Material runtimeMaterial;
    private Color defaultColor;
    private bool isPlayerNearby;
    private Collider[] cachedColliders;
    private bool[] cachedColliderStates;

    public bool isTaken = false;
    public float mass = 3f;

    private void Awake()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponentInChildren<Renderer>();
        }

        if (targetRenderer != null)
        {
            runtimeMaterial = targetRenderer.material;
            defaultColor = runtimeMaterial.color;
        }

        cachedColliders = GetComponentsInChildren<Collider>(true);
        cachedColliderStates = new bool[cachedColliders.Length];
        for (int i = 0; i < cachedColliders.Length; i++)
        {
            cachedColliderStates[i] = cachedColliders[i] != null && cachedColliders[i].enabled;
        }
    }

    private void Update()
    {
        bool hasNearbyPlayer = HasNearbyPlayer();
        if (hasNearbyPlayer == isPlayerNearby)
        {
            return;
        }

        isPlayerNearby = hasNearbyPlayer;
        ApplyColor(isPlayerNearby && !isTaken ? nearColor : defaultColor);
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

    private void ApplyColor(Color targetColor)
    {
        if (runtimeMaterial == null)
        {
            return;
        }

        runtimeMaterial.color = targetColor;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }

    public void SetHeldState(bool held)
    {
        if (cachedColliders == null || cachedColliderStates == null)
        {
            cachedColliders = GetComponentsInChildren<Collider>(true);
            cachedColliderStates = new bool[cachedColliders.Length];
            for (int i = 0; i < cachedColliders.Length; i++)
            {
                cachedColliderStates[i] = cachedColliders[i] != null && cachedColliders[i].enabled;
            }
        }

        for (int i = 0; i < cachedColliders.Length; i++)
        {
            if (cachedColliders[i] == null)
            {
                continue;
            }

            cachedColliders[i].enabled = held ? false : cachedColliderStates[i];
        }
    }
}
