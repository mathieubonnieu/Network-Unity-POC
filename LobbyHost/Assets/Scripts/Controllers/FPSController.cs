using UnityEngine;
using UnityEngine.InputSystem; // Indispensable pour le nouveau système

public class FPSController : MonoBehaviour
{
    [Header("Composants")]
    public CharacterController controller;
    public Transform mainCamera;

    [Header("Mouvement")]
    public float speed = 5f;

    public InputActionReference moveAction;

    void Start()
    {
        // Cursor.lockState = CursorLockMode.Locked;
        // Cursor.visible = false;

        if (mainCamera == null)
        {
            mainCamera = Camera.main.transform;
        }
    }

    void Update()
    {
        if (mainCamera == null)
        {
            if (Camera.main != null) mainCamera = Camera.main.transform;
            return;
        }
        // 1. Rotation du corps alignée sur la caméra (Y uniquement)
        transform.rotation = Quaternion.Euler(0, mainCamera.eulerAngles.y, 0);

        // 2. Lire la valeur du Input System (Vector2)
        Vector2 inputVector = moveAction.action.ReadValue<Vector2>();

        // 3. Convertir le Vector2 en mouvement 3D par rapport au joueur
        Vector3 move = transform.right * inputVector.x + transform.forward * inputVector.y;

        // 4. Appliquer le mouvement
        controller.Move(move * speed * Time.deltaTime);
    }
}
