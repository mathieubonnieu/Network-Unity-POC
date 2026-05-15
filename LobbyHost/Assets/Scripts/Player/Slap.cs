using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class SlapAction : NetworkBehaviour
{
    [SerializeField] private InputActionAsset inputActions;
    private InputAction interactAction;
    public Transform arm;
    public Transform fxPoint;
    private List<TakeItem> nearPlayers = new List<TakeItem>();

    [SerializeField] private float stunDuration = 2f;
    [SerializeField] private float slapRotation = -100f;
    private bool isSlapping = false;


    [SerializeField] private float hitThreshold = 0.60f;


    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        var map = inputActions.FindActionMap("Player", true);
        interactAction = map.FindAction("Slap", true);
        interactAction.started += OnSlapPressed;
        interactAction.Enable();
    }

    private void OnSlapPressed(InputAction.CallbackContext context)
    {
        Slap();
    }

    private void Slap()
    {
        if (isSlapping) return;
        isSlapping = true;
        Debug.Log("Slap!");

        PlaySlapAnimationRpc();

        // GlobalController.Instance.fxController.Play(fxPoint.position);
        // arm.position = Vector3.Lerp(arm.position, new Vector3(arm.position.x, arm.position.y + 0.5f, arm.position.z), 0.5f);
        StartCoroutine(CheckHit());
    }

    [Rpc(SendTo.Everyone)]
    private void PlaySlapAnimationRpc()
    {
        arm.Rotate(Vector3.right, slapRotation);
        StartCoroutine(SlapCoroutine());
    }

    private IEnumerator SlapCoroutine()
    {
        yield return new WaitForSeconds(1f);
        arm.Rotate(Vector3.right, -slapRotation);
        // Vector3 originalPosition = arm.position;
        // arm.position = new Vector3(arm.position.x, arm.position.y - 0.5f, arm.position.z);
        // arm.position = Vector3.Lerp(arm.position, new Vector3(arm.position.x, arm.position.y - 0.5f, arm.position.z), 0.5f);
        Debug.Log("End Slap!");
        isSlapping = false;
    }


    private void OnTriggerEnter(Collider other)
    {
        if (!IsOwner) return;
        TakeItem player = other.GetComponent<TakeItem>();
        if (player != null && player != GetComponent<TakeItem>() && !nearPlayers.Contains(player))
        {
            nearPlayers.Add(player);
            // StartCoroutine(WaitTime());
            Debug.Log("j'ajoute un player : " + nearPlayers.Count);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsOwner) return;

        TakeItem player = other.GetComponent<TakeItem>();
        if (player != null && nearPlayers.Contains(player))
        {
            nearPlayers.Remove(player);
            Debug.Log("j'enlève un player : " + nearPlayers.Count);
        }
    }

    private IEnumerator CheckHit()
    {
        Debug.Log("Checking for hit... Near Players: " + nearPlayers.Count);
        foreach (TakeItem player in nearPlayers)
        {
            Vector3 directionToOther = (player.transform.position - transform.position).normalized;
            float dotProduct = Vector3.Dot(transform.forward, directionToOther);

            if (dotProduct > hitThreshold)
            {
                Debug.Log("Player Slapped!");
                StartCoroutine(UnstunPlayer(player));
            }
        }

        yield return null;
    }

    private IEnumerator UnstunPlayer(TakeItem player)
    {
        Debug.Log("Player Unstunned!");
        if(player.currentPlayerEffects.isStuned())
        {
            player.currentPlayerEffects.RemoveEffectServerRpc(0);
        } else
        {
            player.currentPlayerEffects.TryApplyEffect(0);
            // yield return new WaitForSeconds(stunDuration);
            // player.currentPlayerEffects.RemoveEffect(0);
        }
        yield return null;
    }

    private IEnumerable WaitTime()
    {
        yield return new WaitForSeconds(stunDuration);
    }
}
