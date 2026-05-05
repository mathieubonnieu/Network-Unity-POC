using UnityEngine;
using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine.InputSystem;

public class ThridPersonnCameraController : MonoBehaviour
{
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float zoomLerpSpeed = 10f;
    [SerializeField] private float minDistance = 3f;
    [SerializeField] private float maxDistance = 15f;

    private InputSystem_Actions controls;

    private CinemachineCamera cam;
    private CinemachineOrbitalFollow orbital;
    private Vector2 scrollDelta;

    private float targetZoom;
    private float currentZoom;


    void Start()
    {
       controls = new InputSystem_Actions(); 
       controls.Enable();
       controls.CameraControls.MouseZoom.performed += HandleMouseScroll;
    }

    private void HandleMouseScroll(InputAction.CallbackContext context)
    {
        scrollDelta = context.ReadValue<Vector2>();
        targetZoom -= scrollDelta.y * zoomSpeed * Time.deltaTime;
        targetZoom = Mathf.Clamp(targetZoom, minDistance, maxDistance);

        Cursor.lockState = CursorLockMode.Locked;

        cam = GetComponent<CinemachineCamera>();
        orbital = cam.GetComponent<CinemachineOrbitalFollow>();

        targetZoom = currentZoom = orbital.Radius;
    }

    void Update()
    {
        if (orbital == null) return;
        if (scrollDelta.y != 0)
        {
            if (orbital != null)
            {
                targetZoom = Mathf.Clamp(orbital.Radius - scrollDelta.y * zoomSpeed, minDistance, maxDistance);
                scrollDelta = Vector2.zero; 
            }
        }

        
        currentZoom = Mathf.Lerp(currentZoom, targetZoom, Time.deltaTime * zoomLerpSpeed);
        orbital.Radius = currentZoom;
    }
}
