using UnityEngine;
using UnityEngine.InputSystem;

public class CameraRotation : MonoBehaviour
{
	[SerializeField] private float rotationSpeed = 12f;

	private Camera cachedCamera;

	private void Awake()
	{
		cachedCamera = GetComponent<Camera>();

		if (cachedCamera == null)
		{
			cachedCamera = Camera.main;
		}
	}

	private void LateUpdate()
	{
		if (cachedCamera == null)
		{
			return;
		}

		Ray mouseRay = cachedCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
		Vector3 targetLookPoint = transform.position + mouseRay.direction * 100f;

		if (Physics.Raycast(mouseRay, out RaycastHit hit, 1000f))
		{
			targetLookPoint = hit.point;
		}

		Vector3 lookDirection = targetLookPoint - transform.position;

		if (lookDirection.sqrMagnitude < 0.0001f)
		{
			return;
		}

		Quaternion targetRotation = Quaternion.LookRotation(lookDirection, Vector3.up);
		transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
	}
}
