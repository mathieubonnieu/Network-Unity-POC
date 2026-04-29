using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public void StartHost()
    {
        if (!CanStartNetwork("host") || !CanStartHost())
        {
            return;
        }

        PlayersManager.Instance?.ResetLobbyState();

        if (NetworkManager.Singleton.StartHost())
        {
            Debug.Log("Hosting Success to start host");
            NetworkManager.Singleton.SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
        }
        else
        {
            Debug.LogError("Failed to start host");
        }
    }

    public void StartJoin()
    {
        if (!CanStartNetwork("join"))
        {
            return;
        }

        PlayersManager.Instance?.ResetLobbyState();

        if (NetworkManager.Singleton.StartClient())
        {
            Debug.Log("Join Success to start host");
            NetworkManager.Singleton.SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
        }
        else
        {
            Debug.LogError("Failed to start join");
        }
    }

    private static bool CanStartNetwork(string mode)
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("No NetworkManager singleton found in the active scene.");
            return false;
        }

        if (NetworkManager.Singleton.IsListening)
        {
            Debug.LogWarning(
                $"Cannot start {mode}: a network instance is still running. " +
                "Disconnect first (NetworkManager.Shutdown) and retry.");
            return false;
        }

        return true;
    }

    private static bool CanStartHost()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("No NetworkManager singleton found in the active scene.");
            return false;
        }

        var playerPrefab = NetworkManager.Singleton.NetworkConfig?.PlayerPrefab;
        if (playerPrefab != null && playerPrefab.GetComponent<NetworkObject>() == null)
        {
            Debug.LogError(
                $"PlayerPrefab '{playerPrefab.name}' is missing a NetworkObject component on its root GameObject. " +
                "Add NetworkObject to the prefab root before starting host.");
            return false;
        }
        return true;
    }

    
}
