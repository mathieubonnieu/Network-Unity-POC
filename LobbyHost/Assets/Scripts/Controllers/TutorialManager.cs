using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine.SceneManagement;

public class TutorialManager : MonoBehaviour
{
    private UnityTransport transport;
    public TMPro.TMP_InputField joinCodeInputField;
    public static string CurrentGameCode;

    async void Awake()
    {
        await Authenticate();
    }

    void Start()
    {
        if (NetworkManager.Singleton != null)
        {
            transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport as UnityTransport;

            if (transport == null)
            {
                transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            }
        }
    }

    private static async Task Authenticate()
    {
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    public async void CreateMultiplayerRelay()
    {
        if (transport == null)
        {
            Debug.LogError("No UnityTransport found on the active NetworkManager.");
            return;
        }

        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(2);
        joinCodeInputField.text = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        CurrentGameCode = joinCodeInputField.text;
        Debug.Log("code la room = " + joinCodeInputField.text);

        transport.SetRelayServerData(allocation.RelayServer.IpV4, (ushort)allocation.RelayServer.Port, allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData);

        NetworkManager.Singleton.StartHost();
        NetworkManager.Singleton.SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
    }

    public async void JoinSession()
    {
        if (transport == null)
        {
            Debug.LogError("No UnityTransport found on the active NetworkManager.");
            return;
        }

        JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCodeInputField.text);
        transport.SetRelayServerData(joinAllocation.RelayServer.IpV4, (ushort)joinAllocation.RelayServer.Port, joinAllocation.AllocationIdBytes, joinAllocation.Key, joinAllocation.ConnectionData, joinAllocation.HostConnectionData);

        NetworkManager.Singleton.StartClient();
        NetworkManager.Singleton.SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
    }

    public void StartClientRelay()
    {
        NetworkManager.Singleton.StartClient();
        Debug.Log("-------start");
        NetworkManager.Singleton.SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
        Debug.Log("pas sur la scene ?");

    }

    public void StartHostRelay()
    {
        Debug.Log("-------ici ?");

        NetworkManager.Singleton.StartHost();
        Debug.Log("-------start");

        NetworkManager.Singleton.SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
        Debug.Log("pas sur la scene ?");

    }
}
