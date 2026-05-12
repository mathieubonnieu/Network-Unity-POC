using UnityEngine;

/// <summary>
/// Contrôle adaptif de la caméra comme dans Mario Tennis Open.
/// La caméra retourne à sa position d'origine si le joueur est proche,
/// et le suit si le joueur s'éloigne trop.
/// </summary>
public class AdaptiveCameraController : MonoBehaviour
{
    [Header("Paramètres de distance Z")]
    [SerializeField] private float followDistanceThresholdZ = 8f;
    [SerializeField] private float returnDistanceThresholdZ = 7f;

    [Header("Vitesse de mouvement")]
    [SerializeField] private float followSpeed = 4f;
    [SerializeField] private float returnSpeed = 5f;

    [Header("Position d'origine")]
    [SerializeField] private bool useCurrentPositionAsHome = true;

    private Transform playerTransform;
    private Vector3 targetPosition;
    private bool isFollowing = false;
    private Vector3 storedHomePosition;
    private CameraFollowAssignedPlayer cameraFollowAssigned;

    private void Start()
    {
        cameraFollowAssigned = GetComponent<CameraFollowAssignedPlayer>();

        storedHomePosition = useCurrentPositionAsHome ? transform.position : transform.position;
        targetPosition = storedHomePosition;
    }

    private void TryAssignPlayerTarget()
    {
        if (playerTransform != null)
            return;

        if (cameraFollowAssigned != null)
        {
            playerTransform = cameraFollowAssigned.TargetTransform;
            if (playerTransform != null)
                return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    private void LateUpdate()
    {
        TryAssignPlayerTarget();

        if (playerTransform == null) return;

        float playerDeltaX = playerTransform.position.x - storedHomePosition.x;
        float absDeltaX = Mathf.Abs(playerDeltaX);

        if (!isFollowing && absDeltaX > followDistanceThresholdZ)
        {
            isFollowing = true;
        }

        if (isFollowing && absDeltaX < returnDistanceThresholdZ)
        {
            isFollowing = false;
        }

        float targetX = storedHomePosition.x;

        if (isFollowing)
        {
            float followWeight = Mathf.InverseLerp(followDistanceThresholdZ, followDistanceThresholdZ * 2.5f, absDeltaX);
            targetX = Mathf.Lerp(storedHomePosition.x, playerTransform.position.x, followWeight);
        }

        targetPosition = new Vector3(targetX, storedHomePosition.y, storedHomePosition.z);

        float speed = isFollowing ? followSpeed : returnSpeed;
        transform.position = Vector3.Lerp(transform.position, targetPosition, speed * Time.deltaTime);
    }

    public void SetHomePosition(Vector3 newHomePosition)
    {
        storedHomePosition = newHomePosition;
        if (!isFollowing)
            targetPosition = storedHomePosition;
    }

    public void ResetToHome()
    {
        isFollowing = false;
        targetPosition = storedHomePosition;
    }
}
