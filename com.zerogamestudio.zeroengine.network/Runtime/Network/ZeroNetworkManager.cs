using System.Collections.Generic;
using UnityEngine;
using ZeroEngine.Core;
using ZeroEngine.Network.Config;
using ZeroEngine.Network.Core;

#if ZEROENGINE_NETCODE
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
#endif

namespace ZeroEngine.Network
{
    public enum ConnectionState
    {
        Offline,
        Connecting,
        Connected, // Client or Host
        Failed
    }

    /// <summary>
    /// ZeroEngine Wrapper for Unity Netcode for GameObjects (NGO).
    /// Inherits from NetworkManager to provide seamless integration.
    /// Manages Connection States and Configuration.
    /// </summary>
#if ZEROENGINE_NETCODE
    public class ZeroNetworkManager : NetworkManager
    {
        public static ZeroNetworkManager Instance => Singleton as ZeroNetworkManager;

        [Header("ZeroEngine Configuration")]
        [SerializeField] private ServerConfig _config;

        public ConnectionState CurrentConnectionState { get; private set; } = ConnectionState.Offline;

        // Player Name Registry (ClientId -> Name)
        private Dictionary<ulong, string> _playerNames = new Dictionary<ulong, string>();

        // Runtime Connection Info
        public ushort CurrentPort { get; private set; }
        public string CurrentIP { get; private set; }

        private void Start()
        {
            ApplyPerformanceSettings();

            // Register Callbacks
            OnClientConnectedCallback += HandleClientConnected;
            OnClientDisconnectCallback += HandleClientDisconnect;
            OnServerStarted += HandleServerStarted;

            // Log Arguments for Debugging
            if (Application.isEditor || Debug.isDebugBuild)
            {
                NetworkArgs.LogAllArgs();
            }
        }

        private void ApplyPerformanceSettings()
        {
            if (_config == null) return;

            // Check if we are running in Headless mode (Batchmode)
            if (Application.isBatchMode && _config.OptimizeForHeadless)
            {
                Debug.Log($"[ZeroNetwork] Running in Headless Mode. Setting TargetFrameRate to {_config.HeadlessTargetFrameRate}");
                Application.targetFrameRate = _config.HeadlessTargetFrameRate;
                QualitySettings.vSyncCount = 0;
            }
            else
            {
                Application.targetFrameRate = _config.TargetFrameRate;
                QualitySettings.vSyncCount = _config.EnableVSync ? 1 : 0;
            }
        }

        private void OnDestroy()
        {
            // Unregister Callbacks
            if (Singleton == this)
            {
                OnClientConnectedCallback -= HandleClientConnected;
                OnClientDisconnectCallback -= HandleClientDisconnect;
                OnServerStarted -= HandleServerStarted;
            }
        }

        #region Connection Methods

        /// <summary>
        /// Configures the UnityTransport with IP and Port.
        /// Priority: Command Line Args > ServerConfig > Default
        /// </summary>
        private void ConfigureTransport()
        {
            var transport = NetworkConfig.NetworkTransport as UnityTransport;
            if (transport == null)
            {
                Debug.LogError("[ZeroNetwork] Transport is not UnityTransport. Cannot Configure.");
                return;
            }

            // 1. Determine IP and Port
            // Priority: Args (-ip, -port) > Config > Default
            string ip = NetworkArgs.GetString("ip", _config ? _config.DefaultIP : "127.0.0.1");
            ushort port = NetworkArgs.GetUShort("port", _config ? _config.DefaultPort : (ushort)7777);

            // 2. Apply Configuration specific logic
            if (_config != null && _config.Environment == ServerEnvironment.Production)
            {
                // In Production, strict adherence to Args is usually preferred, 
                // but the logic above already handles overrides correctly.
                // We might bind to 0.0.0.0 for Server in production usually.
                if (NetworkArgs.HasCheck("server"))
                {
                    ip = "0.0.0.0"; // Bind all interfaces for server
                }
            }

            CurrentIP = ip;
            CurrentPort = port;

            Debug.Log($"[ZeroNetwork] Configuring Transport: {ip}:{port}");
            transport.SetConnectionData(ip, port);
        }

        public void StartHostGame()
        {
            if (CurrentConnectionState != ConnectionState.Offline && CurrentConnectionState != ConnectionState.Failed) return;

            ConfigureTransport();

            Debug.Log("[ZeroNetwork] Starting Host...");
            CurrentConnectionState = ConnectionState.Connecting;

            bool success = StartHost();
            if (!success)
            {
                CurrentConnectionState = ConnectionState.Failed;
                Debug.LogError("[ZeroNetwork] Failed to start Host.");
            }
        }

        public void StartClientGame()
        {
            if (CurrentConnectionState != ConnectionState.Offline && CurrentConnectionState != ConnectionState.Failed) return;

            ConfigureTransport();

            Debug.Log("[ZeroNetwork] Starting Client...");
            CurrentConnectionState = ConnectionState.Connecting;

            bool success = StartClient();
            if (!success)
            {
                CurrentConnectionState = ConnectionState.Failed;
                Debug.LogError("[ZeroNetwork] Failed to start Client.");
            }
        }

        public void StartServerGame()
        {
            if (CurrentConnectionState != ConnectionState.Offline && CurrentConnectionState != ConnectionState.Failed) return;

            ConfigureTransport();

            Debug.Log("[ZeroNetwork] Starting Dedicated Server...");
            CurrentConnectionState = ConnectionState.Connecting;

            bool success = StartServer();
            if (!success)
            {
                CurrentConnectionState = ConnectionState.Failed;
                Debug.LogError("[ZeroNetwork] Failed to start Server.");
            }
        }

        public void DisconnectGame()
        {
            if (CurrentConnectionState == ConnectionState.Offline) return;

            Shutdown();
            CurrentConnectionState = ConnectionState.Offline;
            Debug.Log("[ZeroNetwork] Disconnected.");

            EventManager.Trigger(GameEvents.NetworkDisconnected);
        }

        #endregion

        #region Callbacks

        private void HandleServerStarted()
        {
            if (IsServer && !IsHost)
            {
                CurrentConnectionState = ConnectionState.Connected; // Dedicated Server
                Debug.Log($"[ZeroNetwork] Server Started on Port: {CurrentPort}");
            }
        }

        private void HandleClientConnected(ulong clientId)
        {
            if (clientId == LocalClientId)
            {
                CurrentConnectionState = ConnectionState.Connected;
                Debug.Log("[ZeroNetwork] Connected to Server!");
                EventManager.Trigger(GameEvents.NetworkConnected);
            }

            Debug.Log($"[ZeroNetwork] Client Connected: {clientId}");
            EventManager.Trigger(GameEvents.NetworkPlayerJoined, clientId);
        }

        private void HandleClientDisconnect(ulong clientId)
        {
            if (clientId == LocalClientId)
            {
                CurrentConnectionState = ConnectionState.Offline;
                Debug.Log("[ZeroNetwork] Disconnected from Server.");
                EventManager.Trigger(GameEvents.NetworkDisconnected);
            }
            else
            {
                Debug.Log($"[ZeroNetwork] Client Disconnected: {clientId}");
                _playerNames.Remove(clientId);
                EventManager.Trigger(GameEvents.NetworkPlayerLeft, clientId);
            }
        }

        #endregion

        #region Player Names

        public void SetPlayerName(ulong clientId, string name)
        {
            if (_playerNames.ContainsKey(clientId))
                _playerNames[clientId] = name;
            else
                _playerNames.Add(clientId, name);
        }

        public string GetPlayerName(ulong clientId)
        {
            return _playerNames.TryGetValue(clientId, out var name) ? name : $"Player_{clientId}";
        }

        #endregion
    }
#else
    public class ZeroNetworkManager : MonoBehaviour
    {
        public static ZeroNetworkManager Instance { get; private set; }

        [Header("ZeroEngine Configuration")]
        [SerializeField] private ServerConfig _config;

        public ConnectionState CurrentConnectionState { get; private set; } = ConnectionState.Offline;

        private readonly Dictionary<ulong, string> _playerNames = new Dictionary<ulong, string>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (_config != null)
            {
                Application.targetFrameRate = _config.TargetFrameRate;
                QualitySettings.vSyncCount = _config.EnableVSync ? 1 : 0;
            }
            NetworkArgs.LogAllArgs();
        }

        public void StartHostGame() => LogMissingDependency();
        public void StartClientGame() => LogMissingDependency();
        public void StartServerGame() => LogMissingDependency();
        
        public void DisconnectGame()
        {
            CurrentConnectionState = ConnectionState.Offline;
            LogMissingDependency();
            EventManager.Trigger(GameEvents.NetworkDisconnected);
        }

        private void LogMissingDependency()
        {
            Debug.LogWarning("[ZeroNetwork] Netcode for GameObjects is not installed. Please check ZeroEngine documentation.");
        }

        public void SetPlayerName(ulong clientId, string name) => _playerNames[clientId] = name;
        public string GetPlayerName(ulong clientId) => _playerNames.TryGetValue(clientId, out var name) ? name : $"Player_{clientId}";
    }
#endif
}

