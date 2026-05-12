using System.Collections;
using UnityEngine;
using Unity.Netcode;

[DisallowMultipleComponent]
public class CameraFollowAssignedPlayer : MonoBehaviour
{
    [Tooltip("Vitesse d'interpolation de la rotation (plus grand = plus rapide).")]
    public float rotationSpeed = 6f;

    [Tooltip("Offset appliqué au centre de la cible pour le point visé (ex: hauteur du joueur).")]
    public Vector3 lookAtOffset = new Vector3(0f, 1.2f, 0f);

    [Tooltip("Si vrai, la caméra suivra aussi la position (sinon seule la rotation est ajustée).")]
    public bool followPosition = false;

    [Tooltip("Vitesse d'interpolation de la position si `followPosition` est activé.")]
    public float positionSpeed = 6f;

    Transform target;
    ulong assignedClientId;
    bool hasAssignedClient;

    public Transform TargetTransform => target;
    public bool HasAssignedTarget => hasAssignedClient;

    IEnumerator Start()
    {
        float timeout = 5f;
        float t = 0f;
        while (NetworkManager.Singleton == null && t < timeout)
        {
            t += Time.deltaTime; yield return null;
        }

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientsChanged;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientsChanged;
        }

        TryResolveTarget();
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
        TryResolveTarget();
    }

    public void SetAssignedTarget(ulong clientId, Transform assignedTarget)
    {
        assignedClientId = clientId;
        hasAssignedClient = true;
        target = assignedTarget;

        if (target == null)
        {
            TryResolveTarget();
        }
    }

    public void ClearAssignedTarget()
    {
        hasAssignedClient = false;
        target = null;
    }

    private void TryResolveTarget()
    {
        if (!hasAssignedClient)
        {
            target = null;
            return;
        }

        if (NetworkManager.Singleton == null)
        {
            target = null;
            return;
        }

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(assignedClientId, out var networkClient))
        {
            if (networkClient != null && networkClient.PlayerObject != null)
            {
                target = networkClient.PlayerObject.transform;
                return;
            }
        }

        target = null;
    }

    void Update()
    {
        // PlayerObject can spawn slightly after assignment; retry resolve lazily.
        if (target == null && hasAssignedClient)
        {
            TryResolveTarget();
        }

        if (target == null) return;

        Vector3 lookPoint = target.position + lookAtOffset;

        if (followPosition)
        {
            Vector3 desiredPos = Vector3.Lerp(transform.position, lookPoint, positionSpeed * Time.deltaTime);
            transform.position = desiredPos;
        }

        Vector3 dir = lookPoint - transform.position;
        if (dir.sqrMagnitude < 0.0001f) return;
        Quaternion desiredRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, rotationSpeed * Time.deltaTime);
    }
}
