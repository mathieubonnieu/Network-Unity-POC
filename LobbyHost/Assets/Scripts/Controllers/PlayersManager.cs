using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public struct PlayerData : INetworkSerializable, IEquatable<PlayerData>
{
    public ulong ClientId;
    public FixedString64Bytes PlayerName;
    public bool IsReady;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref PlayerName);
        serializer.SerializeValue(ref IsReady);
    }

    public bool Equals(PlayerData other) =>
        ClientId == other.ClientId &&
        PlayerName.Equals(other.PlayerName) &&
        IsReady == other.IsReady;

    public override bool Equals(object obj) => obj is PlayerData o && Equals(o);
    public override int GetHashCode() => HashCode.Combine(ClientId, PlayerName, IsReady);
}

public class PlayersManager : NetworkBehaviour
{
    public static PlayersManager Instance { get; private set; }
    
    public NetworkList<PlayerData> Players;
    private bool isLeavingToMenu;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Players = new NetworkList<PlayerData>();
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log("PlayersManager OnNetworkSpawn");

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        NetworkManager.Singleton.OnTransportFailure += OnServerTransportFailed;
        NetworkManager.Singleton.OnServerStopped += OnServerStopped;
        NetworkManager.Singleton.OnServerStarted += OnServerStarted;
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoaded;

        // Optionnel : réagir aux changements côté client
        Players.OnListChanged += OnPlayersListChanged;

        if (NetworkManager.Singleton.IsServer)
        {
            foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                if (IndexOf(clientId) < 0)
                    OnClientConnected(clientId);
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        if (Players != null) Players.OnListChanged -= OnPlayersListChanged;

        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        NetworkManager.Singleton.OnServerStopped -= OnServerStopped;
        NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
        NetworkManager.Singleton.OnTransportFailure -= OnServerTransportFailed;
        if (NetworkManager.Singleton.SceneManager != null)
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoaded;
    }

    public void BeginLeaveToMenu()
    {
        isLeavingToMenu = true;
    }

    public void ResetLobbyState()
    {
        isLeavingToMenu = false;

        if (Players != null)
        {
            Players.Clear();
        }
        else
        {
            Players = new NetworkList<PlayerData>();
        }
    }

    private void OnPlayersListChanged(NetworkListEvent<PlayerData> change)
    {
        // Exemple : notifier l'UI ici
        // NetworkManagerUI.Instance?.UpdateFixedSlots();
    }

    // ---------- Helpers ----------

    private int IndexOf(ulong clientId)
    {
        for (int i = 0; i < Players.Count; i++)
            if (Players[i].ClientId == clientId) return i;
        return -1;
    }

    public bool TryGetPlayer(ulong clientId, out PlayerData data)
    {
        int idx = IndexOf(clientId);
        if (idx < 0) { data = default; return false; }
        data = Players[idx];
        return true;
    }

    public PlayerData GetPlayer(ulong clientId)
    {
        int idx = IndexOf(clientId);
        return idx >= 0 ? Players[idx] : default;
    }

    public int GetReadyPlayerCount()
    {
        int n = 0;
        for (int i = 0; i < Players.Count; i++)
            if (Players[i].IsReady) n++;
        return n;
    }

    public bool AreAllPlayersReady()
    {
        if (Players.Count == 0) return false;
        for (int i = 0; i < Players.Count; i++)
            if (!Players[i].IsReady) return false;
        return true;
    }

    // ---------- Connexion / Déconnexion ----------

    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;

        Debug.Log($"Client connecté : {clientId}");
        if (IndexOf(clientId) >= 0) return;

        Players.Add(new PlayerData
        {
            ClientId = clientId,
            PlayerName = $"Player {clientId}",
            IsReady = false,
        });
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"Client déconnecté : {clientId}");

        if (IsServer)
        {
            int idx = IndexOf(clientId);
            if (idx >= 0) Players.RemoveAt(idx);
        }

        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            if (isLeavingToMenu)
            {
                return;
            }

            ReturnToMenu("Disconnected");
        }
    }

    // ---------- Lifecycle serveur ----------

    private void OnServerStarted()
    {
        if (!IsServer) return;

        // L'hôte se rajoute/initialise ici
        ulong hostId = NetworkManager.Singleton.LocalClientId;
        if (IndexOf(hostId) < 0)
        {
            Players.Add(new PlayerData
            {
                ClientId = hostId,
                PlayerName = $"Player {hostId}",
                IsReady = false,
            });
        }
    }

    private void OnServerStopped(bool isHost)
    {
        Debug.Log("OnServerStopped Called");
        CleanupAndDestroy();
        isLeavingToMenu = false;
    }

    private void OnServerTransportFailed()
    {
        Debug.Log("OnServerTransportFailed Called");
        ReturnToMenu("Transport failure");
    }

    private void OnSceneLoaded(
        string sceneName,
        LoadSceneMode mode,
        List<ulong> clientsCompleted,
        List<ulong> clientsTimedOut)
    {
        Debug.LogWarning("Start OnSceneLoaded");
        Debug.Log($"ClientsCompleted:{clientsCompleted.Count} TimedOut:{clientsTimedOut.Count} Scene:{sceneName} Mode:{mode}");
        if (!IsServer) return;

        if (clientsTimedOut.Count > 0)
        {
            Debug.LogWarning("Some clients failed to load scene");
            return;
        }

        Debug.Log("All clients loaded GameScene");
    }

    private void ReturnToMenu(string reason)
    {
        Debug.Log($"[Network] Return to menu: {reason}");

        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.Shutdown();

        CleanupAndDestroy();
        SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
    }

    private void CleanupAndDestroy()
    {
        Debug.Log("Netcode stopped → PlayerManager cleanup");
        ResetLobbyState();
    }

    // ---------- API Ready ----------

    // À appeler depuis n'importe où : route automatiquement vers le serveur
    public void RequestSetReady()
    {
        if (IsServer)
            SetPlayerReady(NetworkManager.Singleton.LocalClientId);
        else
            SetPlayerReadyServerRpc();
    }

    public void RequestStartGame()
    {
        if (IsServer)
        {
            if (AreAllPlayersReady())
            {
                NetworkManager.Singleton.SceneManager.LoadScene("SceneGame_0", LoadSceneMode.Single);
            }
            else
            {
                Debug.LogWarning("Cannot start game, not all players are ready");
            }
        }
        else
        {
            Debug.LogWarning("Only the server can start the game");
        }
    }
    
    [Rpc(SendTo.ClientsAndHost)]
    private void SetPlayerReadyServerRpc(RpcParams rpcParams = default)
    {
        SetPlayerReady(rpcParams.Receive.SenderClientId);
    }

    // Écriture : serveur uniquement
    public void SetPlayerReady(ulong clientId)
    {
        if (!IsServer) return;

        int idx = IndexOf(clientId);
        if (idx < 0) return;

        var p = Players[idx];
        p.IsReady = !p.IsReady;
        Players[idx] = p;
    }

    public void RequestFromPlayer(ulong clientId)
    {
    }

    // Idem pour le nom, exemple
    public void RequestSetName(string name)
    {
        if (IsServer)
            SetPlayerName(NetworkManager.Singleton.LocalClientId, name);
        else
            SetPlayerNameServerRpc(name);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SetPlayerNameServerRpc(string name, RpcParams rpcParams = default)
    {
        SetPlayerName(rpcParams.Receive.SenderClientId, name);
    }

    public void SetPlayerName(ulong clientId, string name)
    {
        if (!IsServer) return;
        int idx = IndexOf(clientId);
        if (idx < 0) return;

        var p = Players[idx];
        p.PlayerName = name ?? string.Empty; // conversion implicite string -> FixedString64Bytes
        Players[idx] = p;
    }
}