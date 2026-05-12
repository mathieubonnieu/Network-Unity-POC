using UnityEngine;

public class TakableItem : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private Renderer childRenderer;


    private ItemProximityDetector proximityDetector;
    private bool childRendererDefaultEnabled = false;
    private bool isPlayerNearby;
    
    private Collider[] cachedColliders;
    private bool[] cachedColliderStates;

    public bool isChosen = false;
    public bool isTaken = false;
    public float mass;

    private void Awake()
    {
        if (childRenderer == null)
        {
            childRenderer = FindChildRenderer();
        }

        if (childRenderer != null)
        {
            childRendererDefaultEnabled = childRenderer.enabled;
        }
        childRenderer.enabled = false;

        proximityDetector = GetComponent<ItemProximityDetector>();
        if (proximityDetector == null)
        {
            proximityDetector = gameObject.AddComponent<ItemProximityDetector>();
        }

        cachedColliders = GetComponentsInChildren<Collider>(true);
        cachedColliderStates = new bool[cachedColliders.Length];
        for (int i = 0; i < cachedColliders.Length; i++)
        {
            cachedColliderStates[i] = cachedColliders[i] != null && cachedColliders[i].enabled;
        }
    }

    private void OnEnable()
    {
        if (proximityDetector == null)
        {
            proximityDetector = GetComponent<ItemProximityDetector>();
        }

        if (proximityDetector != null)
        {
            proximityDetector.PlayerNearbyChanged += OnPlayerNearbyChanged;
            OnPlayerNearbyChanged(proximityDetector.IsPlayerNearby);
        }
    }

    private void OnDisable()
    {
        if (proximityDetector != null)
        {
            proximityDetector.PlayerNearbyChanged -= OnPlayerNearbyChanged;
        }
    }

    private void Update()
    {
        // Recalculer la visibilité du child à chaque frame en fonction du nouvel état isChosen
        UpdateChildVisibility();
    }

    private void UpdateChildVisibility()
    {
        bool shouldBeVisible = isPlayerNearby && !isTaken && isChosen;
        SetChildVisibility(shouldBeVisible);
    }

    private void OnPlayerNearbyChanged(bool hasNearbyPlayer)
    {
        if (childRenderer == null)
        {
            return;
        }

        if (hasNearbyPlayer == isPlayerNearby)
        {
            return;
        }

        isPlayerNearby = hasNearbyPlayer;
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

    private Renderer FindChildRenderer()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers.Length > 0)
        {
            return renderers[0];
        }

        return null;
    }

    private void SetChildVisibility(bool visible)
    {
        if (childRenderer == null)
        {
            return;
        }

        childRenderer.enabled = visible ? childRendererDefaultEnabled : false;
    }
}
