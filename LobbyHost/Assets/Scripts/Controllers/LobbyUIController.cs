using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyUIController : MonoBehaviour
{
    public PlayerReadyItem playerReadyItem;
    public Transform rectPlayers;

    public TMP_Text playersReady;

    public void OnEnable()
    {
        PlayersManager.Instance.Players.OnListChanged += OnPlayersUpdate;
    }

    public void OnDisable()
    {
        PlayersManager.Instance.Players.OnListChanged -= OnPlayersUpdate;
    }

    private void Start()
    {
        UpdateListPlayer();
    }

    public void OnPlayersUpdate(NetworkListEvent<PlayerData> players)
    {
        UpdateListPlayer();
        UpdatePlayersReady();
    }

    private void UpdatePlayersReady()
    {
        playersReady.text =
            $"Players Ready {PlayersManager.Instance.GetReadyPlayerCount()}/{PlayersManager.Instance.Players.Count}";
    }

    private void UpdateListPlayer()
    {
        while (rectPlayers.childCount > 0)
        {
            var child = rectPlayers.GetChild(0);
            child.SetParent(null);
            Destroy(child.gameObject);
        }
        foreach (PlayerData player in PlayersManager.Instance.Players)
        {
            PlayerReadyItem item = Instantiate(playerReadyItem, rectPlayers);
            item.SetPlayerItem(player.PlayerName.ToString(), player.IsReady);
        }        
    }

    public void SetReady()
    {
        PlayersManager.Instance.RequestSetReady();
    }

    public void StartGame()
    {
        PlayersManager.Instance.RequestStartGame();
    }

    public void LeaveLobby()
    {
        StartCoroutine(LeaveLobbyRoutine());
    }

    private IEnumerator LeaveLobbyRoutine()
    {
        if (PlayersManager.Instance != null)
        {
            PlayersManager.Instance.BeginLeaveToMenu();
        }

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
            while (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                yield return null;
            }
        }

        SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
    }
}
