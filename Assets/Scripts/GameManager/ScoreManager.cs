using UnityEngine;
using Fusion;
using GNW2.Events;
using System.Collections.Generic;

namespace GNW2.GameManager
{
    /// <summary>
    /// Tracks player scores across rounds using the event bus pattern.
    /// Demonstrates how to add features without modifying existing systems.
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        /// <summary>
        /// Data structure to track individual player statistics
        /// </summary>
        [System.Serializable]
        public class PlayerScore
        {
            public int Wins;
            public int Losses;
            public int Draws;

            public int TotalGames => Wins + Losses + Draws;
            public float WinRate => TotalGames > 0 ? (float)Wins / TotalGames : 0f;
        }

        // Track scores for all players
        private Dictionary<PlayerRef, PlayerScore> _playerScores = new Dictionary<PlayerRef, PlayerScore>();

        // PlayerPrefs keys for score storage
        private const string SCORE_WINS_KEY_PREFIX = "SCORE_WINS_";
        private const string SCORE_LOSSES_KEY_PREFIX = "SCORE_LOSSES_";
        private const string SCORE_DRAWS_KEY_PREFIX = "SCORE_DRAWS_";

        private void OnEnable()
        {
            // Subscribe to relevant events
            EventBus.Subscribe<RoundEndedEvent>(OnRoundEnded);
            EventBus.Subscribe<PlayerJoinedEvent>(OnPlayerJoined);
            EventBus.Subscribe<PlayerLeftEvent>(OnPlayerLeft);
        }

        private void OnDisable()
        {
            // Always unsubscribe to prevent memory leaks
            EventBus.Unsubscribe<RoundEndedEvent>(OnRoundEnded);
            EventBus.Unsubscribe<PlayerJoinedEvent>(OnPlayerJoined);
            EventBus.Unsubscribe<PlayerLeftEvent>(OnPlayerLeft);
        }

        /// <summary>
        /// Initialize score tracking for new players and load their saved scores
        /// </summary>
        private void OnPlayerJoined(PlayerJoinedEvent evt)
        {
            if (!_playerScores.ContainsKey(evt.Player))
            {
                // Try to load saved scores for this player, or create new if none exist
                _playerScores[evt.Player] = LoadPlayerScore(evt.Player);
                Debug.Log($"[ScoreManager] Tracking started for Player {evt.Player.PlayerId}. " +
                         $"W:{_playerScores[evt.Player].Wins} L:{_playerScores[evt.Player].Losses} D:{_playerScores[evt.Player].Draws}");
                
                // Reset For Showcase
                ResetAllScores();
            }
        }

        /// <summary>
        /// Clean up score tracking for disconnected players and save their scores
        /// </summary>
        private void OnPlayerLeft(PlayerLeftEvent evt)
        {
            if (_playerScores.ContainsKey(evt.Player))
            {
                var score = _playerScores[evt.Player];
                SavePlayerScore(evt.Player, score);
                Debug.Log($"[ScoreManager] Player {evt.Player.PlayerId} final stats - " +
                         $"W:{score.Wins} L:{score.Losses} D:{score.Draws}");
                _playerScores.Remove(evt.Player);
            }
        }

        /// <summary>
        /// Update scores when a round ends and save immediately
        /// </summary>
        private void OnRoundEnded(RoundEndedEvent evt)
        {
            if (evt.IsDraw)
            {
                // Both players get a draw
                foreach (var kvp in _playerScores)
                {
                    kvp.Value.Draws++;
                    SavePlayerScore(kvp.Key, kvp.Value);
                    PublishScoreUpdate(kvp.Key, kvp.Value);
                }
                Debug.Log("[ScoreManager] Round ended in draw");
            }
            else
            {
                // Winner gets a win
                if (_playerScores.ContainsKey(evt.Winner))
                {
                    _playerScores[evt.Winner].Wins++;
                    SavePlayerScore(evt.Winner, _playerScores[evt.Winner]);
                    PublishScoreUpdate(evt.Winner, _playerScores[evt.Winner]);
                    Debug.Log($"[ScoreManager] Player {evt.Winner.PlayerId} wins!");
                }

                // Everyone else gets a loss
                foreach (var kvp in _playerScores)
                {
                    if (kvp.Key != evt.Winner)
                    {
                        kvp.Value.Losses++;
                        SavePlayerScore(kvp.Key, kvp.Value);
                        PublishScoreUpdate(kvp.Key, kvp.Value);
                    }
                }
            }
        }

        /// <summary>
        /// Save player score to PlayerPrefs using their username as identifier
        /// </summary>
        private void SavePlayerScore(PlayerRef player, PlayerScore score)
        {
            string username = GetUsernameForPlayer(player);
            if (!string.IsNullOrEmpty(username))
            {
                PlayerPrefs.SetInt($"{SCORE_WINS_KEY_PREFIX}{username}", score.Wins);
                PlayerPrefs.SetInt($"{SCORE_LOSSES_KEY_PREFIX}{username}", score.Losses);
                PlayerPrefs.SetInt($"{SCORE_DRAWS_KEY_PREFIX}{username}", score.Draws);
                PlayerPrefs.Save();
                
                Debug.Log($"[ScoreManager] Saved scores for {username}: W:{score.Wins} L:{score.Losses} D:{score.Draws}");
            }
        }

        /// <summary>
        /// Load player score from PlayerPrefs using their username as identifier
        /// </summary>
        private PlayerScore LoadPlayerScore(PlayerRef player)
        {
            string username = GetUsernameForPlayer(player);
            if (!string.IsNullOrEmpty(username))
            {
                return new PlayerScore
                {
                    Wins = PlayerPrefs.GetInt($"{SCORE_WINS_KEY_PREFIX}{username}", 0),
                    Losses = PlayerPrefs.GetInt($"{SCORE_LOSSES_KEY_PREFIX}{username}", 0),
                    Draws = PlayerPrefs.GetInt($"{SCORE_DRAWS_KEY_PREFIX}{username}", 0)
                };
            }
            
            // Return new score if no saved data found
            return new PlayerScore();
        }

        /// <summary>
        /// Get the username for a PlayerRef by checking the username mappings
        /// </summary>
        private string GetUsernameForPlayer(PlayerRef player)
        {
            // Try to get from GameManager's username mappings
            var gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager != null && gameManager.GetPlayerUsername(player, out string username))
            {
                return username;
            }

            // Fallback: use player ID if no username is available
            Debug.LogWarning($"[ScoreManager] No username found for Player {player.PlayerId}, using player ID as fallback");
            return $"Player_{player.PlayerId}";
        }

        /// <summary>
        /// Publish score update event for UI systems to consume
        /// </summary>
        private void PublishScoreUpdate(PlayerRef player, PlayerScore score)
        {
            EventBus.Publish(new ScoreUpdatedEvent
            {
                Player = player,
                Wins = score.Wins,
                Losses = score.Losses,
                Draws = score.Draws
            });
        }

        /// <summary>
        /// Public API to get a player's current score
        /// </summary>
        public PlayerScore GetPlayerScore(PlayerRef player)
        {
            return _playerScores.ContainsKey(player) ? _playerScores[player] : null;
        }

        /// <summary>
        /// Public API to get all scores (for leaderboard, etc.)
        /// </summary>
        public Dictionary<PlayerRef, PlayerScore> GetAllScores()
        {
            return new Dictionary<PlayerRef, PlayerScore>(_playerScores);
        }

        /// <summary>
        /// Reset all scores (for new tournament, etc.)
        /// </summary>
        public void ResetAllScores()
        {
            // Reset scores for currently connected players
            foreach (var kvp in _playerScores)
            {
                kvp.Value.Wins = 0;
                kvp.Value.Losses = 0;
                kvp.Value.Draws = 0;
                SavePlayerScore(kvp.Key, kvp.Value);
                PublishScoreUpdate(kvp.Key, kvp.Value);
            }
    
            // Reset scores for all registered users in PlayerPrefs
            List<string> allUsers = GetAllUsersWithScores();
            foreach (string username in allUsers)
            {
                PlayerPrefs.SetInt($"{SCORE_WINS_KEY_PREFIX}{username}", 0);
                PlayerPrefs.SetInt($"{SCORE_LOSSES_KEY_PREFIX}{username}", 0);
                PlayerPrefs.SetInt($"{SCORE_DRAWS_KEY_PREFIX}{username}", 0);
            }
    
            PlayerPrefs.Save();
            Debug.Log("[ScoreManager] All scores reset for all users");
        }

        /// <summary>
        /// Get score for a specific username (useful for displaying leaderboards)
        /// </summary>
        public PlayerScore GetScoreByUsername(string username)
        {
            if (string.IsNullOrEmpty(username))
                return null;

            return new PlayerScore
            {
                Wins = PlayerPrefs.GetInt($"{SCORE_WINS_KEY_PREFIX}{username}", 0),
                Losses = PlayerPrefs.GetInt($"{SCORE_LOSSES_KEY_PREFIX}{username}", 0),
                Draws = PlayerPrefs.GetInt($"{SCORE_DRAWS_KEY_PREFIX}{username}", 0)
            };
        }

        /// <summary>
        /// Get all usernames that have score data
        /// </summary>
        public List<string> GetAllUsersWithScores()
        {
            List<string> usersWithScores = new List<string>();
            
            // Get all registered users from GameAuthManager
            string usernameList = PlayerPrefs.GetString("USERNAMES", "");
            if (!string.IsNullOrEmpty(usernameList))
            {
                string[] usernames = usernameList.Split(';');
                foreach (string username in usernames)
                {
                    if (!string.IsNullOrEmpty(username) && HasScoreData(username))
                    {
                        usersWithScores.Add(username);
                    }
                }
            }
            
            return usersWithScores;
        }

        /// <summary>
        /// Check if a username has any score data saved
        /// </summary>
        private bool HasScoreData(string username)
        {
            return PlayerPrefs.HasKey($"{SCORE_WINS_KEY_PREFIX}{username}") ||
                   PlayerPrefs.HasKey($"{SCORE_LOSSES_KEY_PREFIX}{username}") ||
                   PlayerPrefs.HasKey($"{SCORE_DRAWS_KEY_PREFIX}{username}");
        }
    }
}