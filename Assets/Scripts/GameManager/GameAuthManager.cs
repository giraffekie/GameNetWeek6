using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using GNW2.GameManager;

namespace GameManager
{
    public class GameAuthManager : MonoBehaviour
    {
        public static GameAuthManager Instance;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
            
            //ClearAllUsers();
        }

        [Header("Login UI References")]
        [SerializeField] private GameObject loginPanel;
        [SerializeField] private TMP_InputField loginUsernameInput;
        [SerializeField] private TextMeshProUGUI loginErrorText;
        [SerializeField] private TMP_InputField loginPasswordInput;
        [SerializeField] private Button loginButton;
        
        [Header("Registration UI References")]
        [SerializeField] private GameObject registerPanel;
        [SerializeField] private TMP_InputField registerUsernameInput;
        [SerializeField] private TMP_InputField registerEmailInput;
        [SerializeField] private TextMeshProUGUI registerErrorText;
        [SerializeField] private TMP_InputField registerPasswordInput;
        [SerializeField] private TMP_InputField registerRepeatPasswordInput;
        [SerializeField] private Button registerButton;

        [Header("Navigation Buttons")]
        [SerializeField] private Button goToRegisterButton;
        [SerializeField] private Button goToLoginButton;
        
        public Button LoginButton => loginButton;

        // PlayerPrefs Keys
        private const string USERNAME_KEY_PREFIX = "USER_";
        private const string EMAIL_KEY_PREFIX = "EMAIL_";
        private const string PASSWORD_KEY_PREFIX = "PASS_";
        private const string USERS_COUNT_KEY = "TOTAL_USERS";
        private const string USERNAME_LIST_KEY = "USERNAMES";
        private const string EMAIL_LIST_KEY = "EMAILS";

        // Validation
        private const int MIN_USERNAME_LENGTH = 3;
        private const int MAX_USERNAME_LENGTH = 20;
        private const int MIN_PASSWORD_LENGTH = 6;

        private void Start()
        {
            InitializeUI();
            ShowLoginPanel();
        }

        private void InitializeUI()
        {
            // Login button events
            loginButton.onClick.AddListener(OnLoginAttempt);
            registerButton.onClick.AddListener(OnRegisterAttempt);

            // Navigation events
            if (goToRegisterButton != null)
                goToRegisterButton.onClick.AddListener(ShowRegisterPanel);
            if (goToLoginButton != null)
                goToLoginButton.onClick.AddListener(ShowLoginPanel);

            // Input field validation
            registerUsernameInput.onValueChanged.AddListener(_ => ClearRegisterError());
            registerEmailInput.onValueChanged.AddListener(_ => ClearRegisterError());
            registerPasswordInput.onValueChanged.AddListener(_ => ClearRegisterError());
            registerRepeatPasswordInput.onValueChanged.AddListener(_ => ClearRegisterError());
            
            loginUsernameInput.onValueChanged.AddListener(_ => ClearLoginError());
            loginPasswordInput.onValueChanged.AddListener(_ => ClearLoginError());

            // Enter key submission
            registerRepeatPasswordInput.onSubmit.AddListener(_ => OnRegisterAttempt());
            loginPasswordInput.onSubmit.AddListener(_ => OnLoginAttempt());
        }

        public void ShowLoginPanel()
        {
            loginPanel.SetActive(true);
            registerPanel.SetActive(false);
            ClearAllErrors();
            ClearInputFields();
        }

        public void ShowRegisterPanel()
        {
            loginPanel.SetActive(false);
            registerPanel.SetActive(true);
            ClearAllErrors();
            ClearInputFields();
        }

        #region Registration System
        public void OnRegisterAttempt()
        {
            string username = registerUsernameInput.text.Trim();
            string email = registerEmailInput.text.Trim();
            string password = registerPasswordInput.text;
            string repeatPassword = registerRepeatPasswordInput.text;

            // Validate inputs
            string validationError = ValidateRegistration(username, email, password, repeatPassword);
            
            if (!string.IsNullOrEmpty(validationError))
            {
                SetRegisterError(validationError);
                return;
            }

            // Check if username already exists
            if (UsernameExists(username))
            {
                SetRegisterError("Username already exists!");
                return;
            }

            // Check if email already exists
            if (EmailExists(email))
            {
                SetRegisterError("Email already registered!");
                return;
            }

            // Register the user
            if (RegisterUser(username, email, password))
            {
                SetRegisterError("Registration successful!", Color.green);
                ClearInputFields();
                // Auto-switch to login after successful registration
                Invoke(nameof(ShowLoginPanel), 1.5f);
            }
            else
            {
                SetRegisterError("Registration failed! Please try again.");
            }
        }

        private string ValidateRegistration(string username, string email, string password, string repeatPassword)
        {
            // Username validation
            if (string.IsNullOrEmpty(username))
                return "Username cannot be empty!";
            
            if (username.Length < MIN_USERNAME_LENGTH || username.Length > MAX_USERNAME_LENGTH)
                return $"Username must be between {MIN_USERNAME_LENGTH} and {MAX_USERNAME_LENGTH} characters!";
            
            if (!Regex.IsMatch(username, @"^[a-zA-Z0-9_]+$"))
                return "Username can only contain letters, numbers, and underscores!";

            // Email validation
            if (string.IsNullOrEmpty(email))
                return "Email cannot be empty!";
            
            if (!IsValidEmail(email))
                return "Please enter a valid email address!";

            // Password validation
            if (string.IsNullOrEmpty(password))
                return "Password cannot be empty!";
            
            if (password.Length < MIN_PASSWORD_LENGTH)
                return $"Password must be at least {MIN_PASSWORD_LENGTH} characters long!";
            
            if (password != repeatPassword)
                return "Passwords do not match!";

            return null;
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                return regex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }

        private bool RegisterUser(string username, string email, string password)
        {
            try
            {
                // Get current user count
                int userCount = PlayerPrefs.GetInt(USERS_COUNT_KEY, 0);
                int userId = userCount + 1;

                // Store user data with unique keys
                PlayerPrefs.SetString($"{USERNAME_KEY_PREFIX}{userId}", username);
                PlayerPrefs.SetString($"{EMAIL_KEY_PREFIX}{userId}", email);
                PlayerPrefs.SetString($"{PASSWORD_KEY_PREFIX}{userId}", HashPassword(password));

                // Update user lists
                AddToUserList(USERNAME_LIST_KEY, username);
                AddToUserList(EMAIL_LIST_KEY, email);

                // Update user count
                PlayerPrefs.SetInt(USERS_COUNT_KEY, userId);

                // Save all changes
                PlayerPrefs.Save();

                Debug.Log($"User registered successfully: {username} (ID: {userId})");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Registration failed: {e.Message}");
                return false;
            }
        }

        private void AddToUserList(string listKey, string value)
        {
            string currentList = PlayerPrefs.GetString(listKey, "");
            List<string> items = new List<string>();

            if (!string.IsNullOrEmpty(currentList))
            {
                items.AddRange(currentList.Split(';'));
            }

            if (!items.Contains(value))
            {
                items.Add(value);
                PlayerPrefs.SetString(listKey, string.Join(";", items));
            }
        }
        #endregion

        #region Login System
        public void OnLoginAttempt()
        {
            string username = loginUsernameInput.text.Trim();
            string password = loginPasswordInput.text;

            // Validate inputs
            string validationError = ValidateLogin(username, password);
    
            if (!string.IsNullOrEmpty(validationError))
            {
                SetLoginError(validationError);
                return;
            }

            // Attempt login
            if (AuthenticateUser(username, password))
            {
                SetLoginError("Login successful!", Color.green);
                ClearInputFields();

                var gameManager = FindFirstObjectByType<GNW2.GameManager.GameManager>();
                if (gameManager != null)
                {
                    gameManager.CallStartGame();
                    loginPanel.SetActive(false);
                    StartCoroutine(gameManager.SendUsernameWhenReady(username));
                }
                else
                {
                    Debug.LogError("GameManager not found in scene!");
                }

                Debug.Log($"User logged in: {username}");
            }
            else
            {
                SetLoginError("Invalid username or password!");
            }
        }

        private string ValidateLogin(string username, string password)
        {
            if (string.IsNullOrEmpty(username))
                return "Username cannot be empty!";
            
            if (string.IsNullOrEmpty(password))
                return "Password cannot be empty!";

            return null;
        }

        private bool AuthenticateUser(string username, string password)
        {
            int userCount = PlayerPrefs.GetInt(USERS_COUNT_KEY, 0);

            for (int i = 1; i <= userCount; i++)
            {
                string storedUsername = PlayerPrefs.GetString($"{USERNAME_KEY_PREFIX}{i}", "");
                string storedPassword = PlayerPrefs.GetString($"{PASSWORD_KEY_PREFIX}{i}", "");

                if (storedUsername == username && VerifyPassword(password, storedPassword))
                {
                    // Store current user session
                    PlayerPrefs.SetString("CURRENT_USER", username);
                    PlayerPrefs.SetInt("CURRENT_USER_ID", i);
                    PlayerPrefs.Save();
                    return true;
                }
            }

            return false;
        }
        #endregion

        #region Utility Methods
        private bool UsernameExists(string username)
        {
            string usernameList = PlayerPrefs.GetString(USERNAME_LIST_KEY, "");
            return usernameList.Contains(username + ";") || usernameList.EndsWith(username);
        }

        private bool EmailExists(string email)
        {
            string emailList = PlayerPrefs.GetString(EMAIL_LIST_KEY, "");
            return emailList.Contains(email + ";") || emailList.EndsWith(email);
        }

        private string HashPassword(string password)
        {
            // Simple hashing for demonstration - consider using stronger encryption for production
            // This is a basic hash, you might want to use proper encryption like bcrypt
            return (password.GetHashCode() * 397).ToString();
        }

        private bool VerifyPassword(string inputPassword, string storedPassword)
        {
            return HashPassword(inputPassword) == storedPassword;
        }

        #endregion

        #region UI Methods
        private void SetLoginError(string message, Color? color = null)
        {
            if (loginErrorText != null)
            {
                loginErrorText.text = message;
                loginErrorText.color = color ?? Color.red;
            }
        }

        private void SetRegisterError(string message, Color? color = null)
        {
            if (registerErrorText != null)
            {
                registerErrorText.text = message;
                registerErrorText.color = color ?? Color.red;
            }
        }

        private void ClearLoginError()
        {
            SetLoginError("");
        }

        private void ClearRegisterError()
        {
            SetRegisterError("");
        }

        private void ClearAllErrors()
        {
            ClearLoginError();
            ClearRegisterError();
        }

        private void ClearInputFields()
        {
            loginUsernameInput.text = "";
            loginPasswordInput.text = "";
            registerUsernameInput.text = "";
            registerEmailInput.text = "";
            registerPasswordInput.text = "";
            registerRepeatPasswordInput.text = "";
        }
        #endregion
        
        #region Admin Methods
        /// <summary>
        /// Clears all user data from PlayerPrefs (for testing/reset purposes)
        /// </summary>
        public void ClearAllUsers()
        {
            try
            {
                int userCount = PlayerPrefs.GetInt(USERS_COUNT_KEY, 0);
        
                // Clear all user data
                for (int i = 1; i <= userCount; i++)
                {
                    PlayerPrefs.DeleteKey($"{USERNAME_KEY_PREFIX}{i}");
                    PlayerPrefs.DeleteKey($"{EMAIL_KEY_PREFIX}{i}");
                    PlayerPrefs.DeleteKey($"{PASSWORD_KEY_PREFIX}{i}");
                }
        
                // Clear lists and counters
                PlayerPrefs.DeleteKey(USERS_COUNT_KEY);
                PlayerPrefs.DeleteKey(USERNAME_LIST_KEY);
                PlayerPrefs.DeleteKey(EMAIL_LIST_KEY);
                PlayerPrefs.DeleteKey("CURRENT_USER");
                PlayerPrefs.DeleteKey("CURRENT_USER_ID");
        
                // Also clear all score data
                string usernameList = PlayerPrefs.GetString("USERNAMES", "");
                if (!string.IsNullOrEmpty(usernameList))
                {
                    string[] usernames = usernameList.Split(';');
                    foreach (string username in usernames)
                    {
                        if (!string.IsNullOrEmpty(username))
                        {
                            PlayerPrefs.DeleteKey($"SCORE_WINS_{username}");
                            PlayerPrefs.DeleteKey($"SCORE_LOSSES_{username}");
                            PlayerPrefs.DeleteKey($"SCORE_DRAWS_{username}");
                        }
                    }
                }
        
                PlayerPrefs.Save();
        
                Debug.Log("[GameAuthManager] All user data and scores cleared successfully!");
        
                // Show confirmation
                if (loginErrorText != null)
                {
                    SetLoginError("All users and scores cleared!", Color.yellow);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GameAuthManager] Error clearing users: {e.Message}");
                SetLoginError("Error clearing users!", Color.red);
            }
        }
        #endregion
    }
}