using System.Collections;
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Camera))]
public class CameraMinPlayers : MonoBehaviour
{
    [Tooltip("Nombre minimum de joueurs requis pour activer cette caméra.")]
    public int minPlayers = 1;

    [Tooltip("Si vrai, tentera de désactiver l'objet cible (gameObject par défaut).")]
    public bool disableTargetGameObject = false;

    [Tooltip("Si vrai, désactive/active le composant Camera au lieu de l'objet.")]
    public bool toggleCameraComponent = true;

    [Tooltip("Objet cible à activer/désactiver. Laisser vide pour utiliser ce GameObject.")]
    public GameObject targetObject;

    [Tooltip("Si vrai, cette caméra ne s'activera que si elle est assignée au client local.")]
    public bool requireAssignment = false;

    [HideInInspector]
    public bool assignedToLocalClient = false;

    private Camera targetCamera;

    void Awake()
    {
        if (targetObject == null) targetObject = gameObject;
        targetCamera = GetComponent<Camera>();
    }

    void OnEnable()
    {
        StartCoroutine(WaitAndSubscribe());
    }

    void OnDisable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientChanged;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientChanged;
        }
    }

    IEnumerator WaitAndSubscribe()
    {
        float timeout = 5f;
        float t = 0f;
        while (NetworkManager.Singleton == null && t < timeout)
        {
            t += Time.deltaTime;
            yield return null;
        }

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientChanged;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientChanged;
        }

        UpdateState();
    }

    private void OnClientChanged(ulong id)
    {
        UpdateState();
    }

    private int GetPlayerCount()
    {
        if (NetworkManager.Singleton == null) return 0;

        try
        {
            // Preferred: ConnectedClientsList
            return NetworkManager.Singleton.ConnectedClientsList.Count;
        }
        catch
        {
            // Fallback to dictionary count
            try { return NetworkManager.Singleton.ConnectedClients.Count; } catch { return 0; }
        }
    }

    public void UpdateState()
    {
        bool countOk = GetPlayerCount() >= Mathf.Max(1, minPlayers);
        bool assignedOk = !requireAssignment || assignedToLocalClient;
        bool shouldBeActive = countOk && assignedOk;

        if (toggleCameraComponent && targetCamera != null)
        {
            targetCamera.enabled = shouldBeActive;
            var listener = targetCamera.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = shouldBeActive;
        }

        if (disableTargetGameObject && targetObject != null)
        {
            if (targetObject == gameObject)
            {
                if (targetCamera != null) targetCamera.enabled = shouldBeActive;
            }
            else
            {
                targetObject.SetActive(shouldBeActive);
            }
        }
    }

    public void SetAssigned(bool assigned)
    {
        assignedToLocalClient = assigned;
        UpdateState();
    }
}
