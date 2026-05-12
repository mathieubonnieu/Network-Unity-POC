using System.Collections;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

[DisallowMultipleComponent]
public class CameraAssigner : MonoBehaviour
{
    [Tooltip("Liste des caméras (CameraMinPlayers). Laisser vide pour les récupérer automatiquement.")]
    public CameraMinPlayers[] cameras;

    [Tooltip("Si vrai, recherche automatiquement toutes les CameraMinPlayers dans la scène si la liste est vide.")]
    public bool autoFind = true;

    [Tooltip("Tri des caméras par index hiérarchie (true) ou par nom (false) lors de la recherche automatique.")]
    public bool sortByHierarchy = true;

    IEnumerator Start()
    {
        float timeout = 5f; float t = 0f;
        while (NetworkManager.Singleton == null && t < timeout)
        {
            t += Time.deltaTime; yield return null;
        }

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientsChanged;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientsChanged;
        }

        AssignCamerasToLocalClient();
        yield break;
    }

    void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientsChanged;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientsChanged;
        }
    }

    private void OnClientsChanged(ulong id)
    {
        AssignCamerasToLocalClient();
    }

    private void EnsureCamerasList()
    {
        if ((cameras == null || cameras.Length == 0) && autoFind)
        {
            var list = FindObjectsOfType<CameraMinPlayers>();
            if (sortByHierarchy)
                cameras = list.OrderBy(c => c.transform.root.GetSiblingIndex()).ThenBy(c => c.transform.GetSiblingIndex()).ThenBy(c => c.name).ToArray();
            else
                cameras = list.OrderBy(c => c.name).ToArray();
        }
    }

    public void AssignCamerasToLocalClient()
    {
        EnsureCamerasList();

        if (NetworkManager.Singleton == null) return;
        if (!NetworkManager.Singleton.IsClient) return;

        var clients = NetworkManager.Singleton.ConnectedClientsList;
        ulong localId = NetworkManager.Singleton.LocalClientId;

        for (int i = 0; i < cameras.Length; i++)
        {
            var cam = cameras[i];
            if (cam == null) continue;

            bool hasClientForThisCamera = i < clients.Count;
            bool isAssignedToLocalClient = hasClientForThisCamera && clients[i].ClientId == localId;
            cam.SetAssigned(isAssignedToLocalClient);

            var follow = cam.GetComponent<CameraFollowAssignedPlayer>();
            if (follow == null) continue;

            if (hasClientForThisCamera)
            {
                ulong clientId = clients[i].ClientId;
                Transform playerTarget = null;
                if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client) && client?.PlayerObject != null)
                {
                    playerTarget = client.PlayerObject.transform;
                }
                follow.SetAssignedTarget(clientId, playerTarget);
            }
            else
            {
                follow.ClearAssignedTarget();
            }
        }
    }
}
