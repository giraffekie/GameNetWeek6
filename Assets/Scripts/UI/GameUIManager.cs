using UnityEngine;
using Fusion;
using GNW2.Events;
using UnityEngine.UI;

namespace GNW2.UI
{
    /// <summary>
    /// Centralized UI manager that listens to game events and updates UI accordingly.
    /// This decouples UI from game logic by using the event bus pattern.
    /// </summary>
    public class GameUIManager : MonoBehaviour
    {
        [Header("Selection UI")]
        [SerializeField] private GameObject selectionUI;
        [SerializeField] private Button rockButton;
        [SerializeField] private Button paperButton;
        [SerializeField] private Button scissorButton;

        [Header("Result UI")]
        [SerializeField] private GameObject winUI;
        [SerializeField] private GameObject loseUI;
        [SerializeField] private GameObject drawUI;

        [Header("Game Info UI")]
        [SerializeField] private TMPro.TextMeshProUGUI roundNumberText;
        [SerializeField] private TMPro.TextMeshProUGUI playerCountText;
        [SerializeField] private TMPro.TextMeshProUGUI opponentNameText;

        [Header("Root UI Container")]
        [SerializeField] private GameObject gameUIRoot;

        private bool isConnectedToNetwork = false;

        private void Start()
        {
            // Hide all game UI until connected to network
            HideAllGameUI();
        }

        private void OnEnable()
        {
            // Subscribe to all relevant UI events
            EventBus.Subscribe<ShowSelectionUIEvent>(OnShowSelectionUI);
            EventBus.Subscribe<HideSelectionUIEvent>(OnHideSelectionUI);
            EventBus.Subscribe<ShowResultUIEvent>(OnShowResultUI);
            EventBus.Subscribe<HideResultUIEvent>(OnHideResultUI);
            EventBus.Subscribe<RoundStartedEvent>(OnRoundStarted);
            EventBus.Subscribe<GameStartedEvent>(OnGameStarted);
            EventBus.Subscribe<PlayerJoinedEvent>(OnPlayerJoined);
            EventBus.Subscribe<PlayerLeftEvent>(OnPlayerLeft);
            EventBus.Subscribe<NetworkConnectedEvent>(OnNetworkConnected);
            EventBus.Subscribe<NetworkDisconnectedEvent>(OnNetworkDisconnected);
            EventBus.Subscribe<OpponentAssignedEvent>(OnOpponentAssigned);

            // Setup button listeners
            if (rockButton != null)
                rockButton.onClick.AddListener(() => OnSelectionButtonClicked(0));
            if (paperButton != null)
                paperButton.onClick.AddListener(() => OnSelectionButtonClicked(1));
            if (scissorButton != null)
                scissorButton.onClick.AddListener(() => OnSelectionButtonClicked(2));
        }

        private void OnDisable()
        {
            // Always unsubscribe when disabled to prevent memory leaks
            EventBus.Unsubscribe<ShowSelectionUIEvent>(OnShowSelectionUI);
            EventBus.Unsubscribe<HideSelectionUIEvent>(OnHideSelectionUI);
            EventBus.Unsubscribe<ShowResultUIEvent>(OnShowResultUI);
            EventBus.Unsubscribe<HideResultUIEvent>(OnHideResultUI);
            EventBus.Unsubscribe<RoundStartedEvent>(OnRoundStarted);
            EventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);
            EventBus.Unsubscribe<PlayerJoinedEvent>(OnPlayerJoined);
            EventBus.Unsubscribe<PlayerLeftEvent>(OnPlayerLeft);
            EventBus.Unsubscribe<NetworkConnectedEvent>(OnNetworkConnected);
            EventBus.Unsubscribe<NetworkDisconnectedEvent>(OnNetworkDisconnected);
            EventBus.Unsubscribe<OpponentAssignedEvent>(OnOpponentAssigned);

            // Remove button listeners
            if (rockButton != null)
                rockButton.onClick.RemoveAllListeners();
            if (paperButton != null)
                paperButton.onClick.RemoveAllListeners();
            if (scissorButton != null)
                scissorButton.onClick.RemoveAllListeners();
        }
        
        private void OnOpponentAssigned(OpponentAssignedEvent evt)
        {
            var runner = NetworkRunner.GetRunnerForGameObject(gameObject);
            if (runner == null) return;

            // Only update UI for the local player
            if (evt.Player == runner.LocalPlayer)
            {
                if (opponentNameText != null)
                {
                    opponentNameText.text = $"VERSUS {evt.OpponentUsername}";
                    ShowOpponentNameDisplay();
                    Debug.Log($"[UI] Opponent assigned: {evt.OpponentUsername}");
                }
            }
        }
        
        

        /// <summary>
        /// Called when connected to network - enables game UI
        /// </summary>
        private void OnNetworkConnected(NetworkConnectedEvent evt)
        {
            isConnectedToNetwork = true;
            ShowAllGameUI();
            Debug.Log($"[UI] Network connected as {(evt.IsHost ? "Host" : "Client")} - Game UI enabled");
        }

        /// <summary>
        /// Called when disconnected from network - disables game UI
        /// </summary>
        private void OnNetworkDisconnected(NetworkDisconnectedEvent evt)
        {
            isConnectedToNetwork = false;
            HideAllGameUI();
            Debug.Log("[UI] Network disconnected - Game UI disabled");
        }

        /// <summary>
        /// Shows the rock-paper-scissors selection UI when a round starts
        /// Only shows UI for the local player
        /// </summary>
        private void OnShowSelectionUI(ShowSelectionUIEvent evt)
        {
            if (!isConnectedToNetwork) return;

            // Only show UI if this is for the local player
            var runner = NetworkRunner.GetRunnerForGameObject(gameObject);
            if (runner == null || evt.TargetPlayer != runner.LocalPlayer)
                return;

            if (selectionUI != null)
            {
                selectionUI.SetActive(true);
        
                // Hide opponent name when selection UI appears
                if (opponentNameText != null)
                {
                    opponentNameText.gameObject.SetActive(false);
                }
        
                Debug.Log("[UI] Showing selection UI for local player");
            }
        }

        /// <summary>
        /// Hides the selection UI after player makes a choice
        /// </summary>
        private void OnHideSelectionUI(HideSelectionUIEvent evt)
        {
            if (selectionUI != null)
            {
                selectionUI.SetActive(false);
                Debug.Log("[UI] Hiding selection UI");
            }
        }

        /// <summary>
        /// Shows the appropriate result UI (win/lose/draw) based on the round outcome
        /// Only shows UI for the local player
        /// </summary>
        private void OnShowResultUI(ShowResultUIEvent evt)
        {
            // Only show result for local player
            if (evt.TargetPlayer != NetworkRunner.GetRunnerForGameObject(gameObject)?.LocalPlayer)
                return;

            // Hide all result UIs first
            HideAllResultUIs();

            // Show appropriate UI based on result
            if (evt.IsDraw)
            {
                if (drawUI != null)
                {
                    drawUI.SetActive(true);
                    Debug.Log("[UI] Showing draw UI");
                }
            }
            else if (evt.IsWin)
            {
                if (winUI != null)
                {
                    winUI.SetActive(true);
                    Debug.Log("[UI] Showing win UI");
                }
            }
            else
            {
                if (loseUI != null)
                {
                    loseUI.SetActive(true);
                    Debug.Log("[UI] Showing lose UI");
                }
            }
        }

        /// <summary>
        /// Hides all result UIs
        /// </summary>
        private void OnHideResultUI(HideResultUIEvent evt)
        {
            HideAllResultUIs();
            Debug.Log("[UI] Hiding all result UIs");
        }

        /// <summary>
        /// Updates the round number display
        /// </summary>
        private void OnRoundStarted(RoundStartedEvent evt)
        {
            if (roundNumberText != null)
            {
                roundNumberText.text = $"Round {evt.RoundNumber}";
                Debug.Log($"[UI] Round {evt.RoundNumber} started");
            }
        }

        /// <summary>
        /// Updates the player count display when game starts
        /// </summary>
        private void OnGameStarted(GameStartedEvent evt)
        {
            if (playerCountText != null)
            {
                playerCountText.text = $"{evt.PlayerCount} Players";
                Debug.Log($"[UI] Game started with {evt.PlayerCount} players");
            }
        }

        /// <summary>
        /// Updates player count when a player joins
        /// </summary>
        private void OnPlayerJoined(PlayerJoinedEvent evt)
        {
            UpdatePlayerCount();
        }

        /// <summary>
        /// Updates player count when a player leaves
        /// </summary>
        private void OnPlayerLeft(PlayerLeftEvent evt)
        {
            UpdatePlayerCount();
        }

        /// <summary>
        /// Updates the player count display
        /// </summary>
        private void UpdatePlayerCount()
        {
            if (playerCountText != null && GNW2.GameManager.GameManager.Instance != null)
            {
                int count = GNW2.GameManager.GameManager.Instance.activePlayers.Count;
                playerCountText.text = $"{count} Player{(count != 1 ? "s" : "")}";
            }
        }

        /// <summary>
        /// Called when a player clicks a selection button (Rock/Paper/Scissors)
        /// </summary>
        private void OnSelectionButtonClicked(int selection)
        {
            // Find GameHandler and send selection to server
            // Server will handle hiding UI via RPC when appropriate
            if (GameHandler.Instance != null)
            {
                GameHandler.Instance.SendPlayerSelection(selection);
                Debug.Log($"[UI] Player selected: {selection}");
            }
            else
            {
                Debug.LogError("[UI] GameHandler instance not found!");
            }
        }

        /// <summary>
        /// Helper method to hide all result UIs
        /// </summary>
        private void HideAllResultUIs()
        {
            if (winUI != null) winUI.SetActive(false);
            if (loseUI != null) loseUI.SetActive(false);
            if (drawUI != null) drawUI.SetActive(false);
        }
        
        /// <summary>
        /// Shows opponent name display before selection UI
        /// </summary>
        private void ShowOpponentNameDisplay()
        {
            if (opponentNameText != null)
            {
                // Make sure the opponent name is visible and emphasized
                opponentNameText.gameObject.SetActive(true);
        
                // You could add some visual emphasis here like scaling or color change
                var originalScale = opponentNameText.transform.localScale;
                opponentNameText.transform.localScale = originalScale * 1.2f; // Slight scale up
        
                // Return to normal scale after a moment
                Invoke(nameof(ResetOpponentNameScale), 1.5f);
        
                Debug.Log("[UI] Displaying opponent name before selection");
            }
        }

        /// <summary>
        /// Reset opponent name scale to normal
        /// </summary>
        private void ResetOpponentNameScale()
        {
            if (opponentNameText != null)
            {
                opponentNameText.transform.localScale = Vector3.one;
            }
        }

        /// <summary>
        /// Hides all game UI elements (used when not connected to network)
        /// </summary>
        private void HideAllGameUI()
        {
            // Hide the root UI container if assigned
            if (gameUIRoot != null)
            {
                gameUIRoot.SetActive(false);
            }
            else
            {
                // Otherwise hide individual elements
                if (selectionUI != null) selectionUI.SetActive(false);
                HideAllResultUIs();
                if (roundNumberText != null) roundNumberText.gameObject.SetActive(false);
                if (opponentNameText != null) opponentNameText.gameObject.SetActive(false);
                if (playerCountText != null) playerCountText.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Shows all game UI elements (used when connected to network)
        /// </summary>
        private void ShowAllGameUI()
        {
            // Show the root UI container if assigned
            if (gameUIRoot != null)
            {
                gameUIRoot.SetActive(true);
            }
            else
            {
                // Otherwise show individual elements (except selection/result UIs - those are controlled by game state)
                if (roundNumberText != null) roundNumberText.gameObject.SetActive(true);
                if (playerCountText != null) playerCountText.gameObject.SetActive(true);
            }
        }
    }
}