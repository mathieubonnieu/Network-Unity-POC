using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

[System.Serializable]
    public enum EffectType
    {
        Stuned,
        Confused,
        // Add more effect types as needed
    }

public class StatusEffects : NetworkBehaviour
{
    
    public NetworkVariable<bool> currentEffects = new NetworkVariable<bool>(false);
    void Start()
    {
    }

    void Update()
    {
        
    }

    public bool isStuned()
    {
        if(currentEffects.Value)
        {
            return true;
        }
        return false;
    }

    public void TryApplyEffect(EffectType effect)
    {
        if (IsServer)
        {
            ApplyEffectServerRpc((int)effect);
        } else
        {
            ApplyEffectClientRpc(0);
        }
    }
    [Rpc(SendTo.Server)]
    public void ApplyEffectServerRpc(int effect)
    {
        currentEffects.Value = true;
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void ApplyEffectClientRpc(int effect)
    {
        ApplyEffectServerRpc(effect);
    }

  [Rpc(SendTo.ClientsAndHost)]
    public void RemoveEffectServerRpc(int effect)
    {
        currentEffects.Value = false;
    }
}
