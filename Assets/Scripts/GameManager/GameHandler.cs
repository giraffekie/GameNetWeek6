using UnityEngine;
using Fusion;
using GNW2.GameManager;
using GNW2.Events;
using System.Collections.Generic;
using System.Linq;

public class GameHandler : NetworkBehaviour
{
    public static GameHandler Instance;

    private GameStateMachine _stateMachine;
    private List<PlayerTurn> playerTurn = new();
    
    private Dictionary<PlayerRef, NetworkString<_16>> _playerUsernames = new Dictionary<PlayerRef, NetworkString<_16>>();

    struct PlayerTurn
    {
        public PlayerRef player;
        public int PlayerSelection;
    }
    
    [System.Serializable]
    public struct PlayerInfo
    {
        public PlayerRef playerRef;
        public NetworkString<_16> username;
    }

    public override void Spawned()
    {
        base.Spawned();
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            // Prevent multiple GameHandler instances
            Runner.Despawn(Object);
            return;
        }

        // Get or create state machine
        _stateMachine = GetComponent<GameStateMachine>();
        if (_stateMachine == null)
        {
            _stateMachine = gameObject.AddComponent<GameStateMachine>();
        }

        if (Object.HasStateAuthority)
        {
            _stateMachine.Initialize();
        }
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();
        // State machine handles game flow now
    }
    
    
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SendUserInfo(NetworkString<_16> username, RpcInfo info = default)
    {
        var player = info.Source;
        if (!player.IsRealPlayer)
        {
            player = Runner.LocalPlayer;
            Debug.LogWarning($"[GameHandler] info.Source invalid, fallback to LocalPlayer: {player.PlayerId}");
        }

        if (!_playerUsernames.ContainsKey(player))
        {
            _playerUsernames.Add(player, username);
            Debug.Log($"[GameHandler] Player {player.PlayerId} set username: {username}");

            // Broadcast join to all
            RPC_BroadcastPlayerJoined(player, username);
        }
    }
    
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_AssignOpponents(PlayerRef[] players, NetworkString<_16>[] opponentNames)
    {
        // Publish per-player opponent assignment
        for (int i = 0; i < players.Length; i++)
        {
            EventBus.Publish(new OpponentAssignedEvent
            {
                Player = players[i],
                OpponentUsername = opponentNames[i].ToString()
            });

            Debug.Log($"[GameHandler] Player {players[i].PlayerId} opponent: {opponentNames[i]}");
        }
    }
    
    public bool AllUsernamesAssigned()
    {
        int connectedPlayers = Runner.ActivePlayers.Count();
        int assignedUsernames = _playerUsernames.Count;

        Debug.Log($"[GameHandler] Username check: {assignedUsernames}/{connectedPlayers}");
        return assignedUsernames >= connectedPlayers && assignedUsernames > 0;
    }
    
    public string GetUsername(PlayerRef player)
    {
        if (_playerUsernames.TryGetValue(player, out NetworkString<_16> username))
        {
            return username.ToString();
        }
    
        Debug.LogWarning($"[GameHandler] Username not found for player {player.PlayerId}");
        return $"Player {player.PlayerId}";
    }
    
    
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_BroadcastPlayerJoined(PlayerRef player, NetworkString<_16> username)
    {
        EventBus.Publish(new PlayerJoinedEvent
        {
            Player = player,
            PlayerObject = null, // This will be set by GameManager
            Username = username.ToString()
        });
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_SendTurn(int type, PlayerRef player)
    {
        playerTurn.Add(new PlayerTurn
        {
            PlayerSelection = type,
            player = player
        });

        // Publish selection event
        RPC_BroadcastPlayerSelection(player, type);

        // Update state machine
        _stateMachine.PlayersReady++;

        if(playerTurn.Count == 2)
        {
            Evaluate();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_BroadcastPlayerSelection(PlayerRef player, int selection)
    {
        EventBus.Publish(new PlayerMadeSelectionEvent
        {
            Player = player,
            Selection = selection
        });
    }

    /// <summary>
    /// Public method for UI to call when player makes a selection
    /// </summary>
    public void SendPlayerSelection(int selection)
    {
        if (Runner != null && Runner.LocalPlayer.IsRealPlayer)
        {
            RPC_SendTurn(selection, Runner.LocalPlayer);
        }
    }

    private void Evaluate()
    {
        var p1result = playerTurn[0];
        var p2result = playerTurn[1];

        // Rock = 0, Paper = 1, Scissors = 2
        // Rock beats Scissors, Scissors beats Paper, Paper beats Rock

        if (p1result.PlayerSelection == p2result.PlayerSelection)
        {
            // Draw - show draw UI to all players
            _stateMachine.RPC_ShowDrawUI();
            RPC_BroadcastRoundEnded(PlayerRef.None, true);
        }
        else if ((p1result.PlayerSelection == 0 && p2result.PlayerSelection == 2) ||  // Rock beats Scissors
                 (p1result.PlayerSelection == 1 && p2result.PlayerSelection == 0) ||  // Paper beats Rock
                 (p1result.PlayerSelection == 2 && p2result.PlayerSelection == 1))    // Scissors beats Paper
        {
            // Player 1 wins - show win to p1, lose to p2
            _stateMachine.RPC_ShowWinUI(p1result.player);
            _stateMachine.RPC_ShowLoseUI(p2result.player);
            RPC_BroadcastRoundEnded(p1result.player, false);
        }
        else
        {
            // Player 2 wins - show lose to p1, win to p2
            _stateMachine.RPC_ShowLoseUI(p1result.player);
            _stateMachine.RPC_ShowWinUI(p2result.player);
            RPC_BroadcastRoundEnded(p2result.player, false);
        }

        // Transition to showing results state
        _stateMachine.TransitionToState(GameState.ShowingResults);

        // Reset for next round
        playerTurn.Clear();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_BroadcastRoundEnded(PlayerRef winner, NetworkBool isDraw)
    {
        EventBus.Publish(new RoundEndedEvent
        {
            Winner = winner,
            IsDraw = isDraw
        });
    }


}