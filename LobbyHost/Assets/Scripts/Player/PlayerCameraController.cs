using UnityEngine;

[DisallowMultipleComponent]
public class PlayerCameraController : MonoBehaviour
{
    [Tooltip("Transform de la caméra utilisée pour orienter le mouvement (optionnel)")]
    public Transform cameraTransform;

    [Tooltip("Si vrai, activera/désactivera également les composants Cinemachine détectés.")]
    public bool manageCinemachine = true;

    /// <summary>
    /// Retourne le transform de la caméra assignée (peut être null).
    /// </summary>
    public Transform CameraTransform => cameraTransform;

    /// <summary>
    /// Appelé côté propriétaire pour activer la caméra locale et les composants liés.
    /// </summary>
    public void SetupForOwner()
    {
        if (cameraTransform == null)
        {
            Camera ownerCam = GetComponentInChildren<Camera>(true);
            if (ownerCam != null)
            {
                cameraTransform = ownerCam.transform;
                ownerCam.enabled = true;
            }
            else
            {
                Debug.LogWarning($"PlayerCameraController: no child Camera found on {gameObject.name}");
            }
        }

        if (manageCinemachine)
        {
            MonoBehaviour[] cine = GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < cine.Length; i++)
            {
                var comp = cine[i];
                if (comp == null) continue;
                var n = comp.GetType().Name;
                if (n == "CinemachineBrain" || n == "CinemachineCamera") comp.enabled = true;
            }
        }
    }

    /// <summary>
    /// Appelé pour désactiver les caméras / audio / cinemachine pour les joueurs distants.
    /// </summary>
    public void DisableForRemote()
    {
        Camera[] cams = GetComponentsInChildren<Camera>(true);
        for (int i = 0; i < cams.Length; i++) cams[i].enabled = false;

        AudioListener[] listeners = GetComponentsInChildren<AudioListener>(true);
        for (int i = 0; i < listeners.Length; i++) listeners[i].enabled = false;

        if (manageCinemachine)
        {
            MonoBehaviour[] cine = GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < cine.Length; i++)
            {
                var comp = cine[i];
                if (comp == null) continue;
                var n = comp.GetType().Name;
                if (n == "CinemachineBrain" || n == "CinemachineCamera") comp.enabled = false;
            }
        }
    }
}
