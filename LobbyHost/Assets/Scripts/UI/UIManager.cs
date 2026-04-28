using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIManager : MonoBehaviour
{
    public TMP_Text readyButtonText;
    public GameObject startButton;
    [SerializeField] private float enabledOpacity = 1f;
    [SerializeField] private float disabledOpacity = 0.35f;

    private Button startButtonComponent;
    private EventTrigger startButtonEventTrigger;
    private Graphic startButtonGraphic;
    private CanvasGroup startButtonCanvasGroup;
    private bool hasLoggedStartButtonWarning;

    private void OnEnable()
    {
        if (PlayersManager.Instance != null && PlayersManager.Instance.Players != null)
        {
            PlayersManager.Instance.Players.OnListChanged += OnPlayersListChanged;
        }

        UpdateReadyButtonText();
        UpdateStartButton();
    }

    private void OnDisable()
    {
        if (PlayersManager.Instance != null && PlayersManager.Instance.Players != null)
        {
            PlayersManager.Instance.Players.OnListChanged -= OnPlayersListChanged;
        }
    }

    private void OnPlayersListChanged(NetworkListEvent<PlayerData> _)
    {
        UpdateReadyButtonText();
        UpdateStartButton();
    }

    public void RefreshReadyButtonText()
    {
        UpdateReadyButtonText();
        UpdateStartButton();
    }

    private void UpdateReadyButtonText()
    {
        if (readyButtonText == null)
        {
            return;
        }

        if (PlayersManager.Instance == null || NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            readyButtonText.text = "ready";
            return;
        }

        ulong localClientId = NetworkManager.Singleton.LocalClientId;
        bool isLocalPlayerReady = PlayersManager.Instance.TryGetPlayer(localClientId, out var playerData) && playerData.IsReady;
        readyButtonText.text = isLocalPlayerReady ? "not ready" : "ready";
    }

    private void UpdateStartButton()
    {
        if (!NetworkManager.Singleton.IsHost)
        {
            startButton.gameObject.SetActive(false);
            return;
        }
        if (!TryResolveStartButton())
        {
            return;
        }

        startButton.SetActive(true);

        if (PlayersManager.Instance == null || NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            SetStartButtonState(false);
            return;
        }

        bool isHost = NetworkManager.Singleton.IsHost;
        bool everyoneReady = PlayersManager.Instance.Players.Count > 0 &&
                            PlayersManager.Instance.GetReadyPlayerCount() == PlayersManager.Instance.Players.Count;

        SetStartButtonState(isHost && everyoneReady);
    }

    private void SetStartButtonState(bool canStart)
    {
        if (startButtonComponent != null)
        {
            startButtonComponent.interactable = canStart;
        }

        if (startButtonEventTrigger != null)
        {
            startButtonEventTrigger.enabled = canStart;
        }

        if (startButtonGraphic != null)
        {
            startButtonGraphic.raycastTarget = canStart;
        }

        if (startButtonCanvasGroup == null)
        {
            startButtonCanvasGroup = startButton.GetComponent<CanvasGroup>();
            if (startButtonCanvasGroup == null)
            {
                startButtonCanvasGroup = startButton.AddComponent<CanvasGroup>();
            }
        }

        startButtonCanvasGroup.alpha = canStart ? enabledOpacity : disabledOpacity;
    }

    private bool TryResolveStartButton()
    {
        if (startButton == null)
        {
            return false;
        }

        if (startButtonComponent == null && startButtonEventTrigger == null)
        {
            if (startButtonComponent == null)
            {
                startButtonEventTrigger = startButton.GetComponent<EventTrigger>();
                if (startButtonEventTrigger == null)
                {
                    startButtonEventTrigger = startButton.GetComponentInParent<EventTrigger>();
                }
                if (startButtonEventTrigger == null)
                {
                    startButtonEventTrigger = startButton.GetComponentInChildren<EventTrigger>(true);
                }
            }

            if (startButtonComponent == null && startButtonEventTrigger == null)
            {
                if (!hasLoggedStartButtonWarning)
                {
                    Debug.LogWarning("UIManager: No Button or EventTrigger found on Start Button reference (self/parent/children).");
                    hasLoggedStartButtonWarning = true;
                }
                return false;
            }

            // Bind subsequent visual changes to the actual Button GameObject.
            if (startButtonComponent != null)
            {
                startButton = startButtonComponent.gameObject;
            }
            else
            {
                startButton = startButtonEventTrigger.gameObject;
            }

            startButtonGraphic = startButton.GetComponent<Graphic>();
            if (startButtonGraphic == null)
            {
                startButtonGraphic = startButton.GetComponentInChildren<Graphic>(true);
            }

            hasLoggedStartButtonWarning = false;
        }

        return true;
    }
}
