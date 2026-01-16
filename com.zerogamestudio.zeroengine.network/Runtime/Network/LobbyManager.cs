using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using ZeroEngine.Core;

#if ZEROENGINE_UGS && ZEROENGINE_NETCODE
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
#endif

namespace ZeroEngine.Network
{
    /// <summary>
    /// Manages Unity Gaming Services (Lobby & Relay).
    /// Handles Authentication, Lobby Creation, Joining, and NGO Bootstrap.
    /// </summary>
#if ZEROENGINE_UGS && ZEROENGINE_NETCODE
    public class LobbyManager : Singleton<LobbyManager>
    {
        private Lobby _currentLobby;
        private float _heartbeatTimer;
        private const float HeartbeatInterval = 15f;

        public bool IsSignedIn => AuthenticationService.Instance.IsSignedIn;

        public event Action<List<Lobby>> OnLobbyListUpdated;

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;
        }

        private void Update()
        {
            HandleLobbyHeartbeat();
        }

        public async Task InitializeAsync()
        {
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                await UnityServices.InitializeAsync();
                Debug.Log("[LobbyManager] Unity Services Initialized.");
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"[LobbyManager] Signed in: {AuthenticationService.Instance.PlayerId}");
            }
        }

        #region Lobby Operations

        public async Task CreateLobby(string lobbyName, int maxPlayers, bool isPrivate = false)
        {
            try
            {
                Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers);
                string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

                var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                transport.SetHostRelayData(
                    allocation.RelayServer.IpV4,
                    (ushort)allocation.RelayServer.Port,
                    allocation.AllocationIdBytes,
                    allocation.Key,
                    allocation.ConnectionData
                );

                CreateLobbyOptions options = new CreateLobbyOptions
                {
                    IsPrivate = isPrivate,
                    Player = new Player
                    {
                        Data = new Dictionary<string, PlayerDataObject>
                        {
                            { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, ZeroNetworkManager.Instance.GetPlayerName(0)) }
                        }
                    },
                    Data = new Dictionary<string, DataObject>
                    {
                        { "JoinCode", new DataObject(DataObject.VisibilityOptions.Member, joinCode) }
                    }
                };

                _currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
                Debug.Log($"[LobbyManager] Created Lobby: {_currentLobby.Name} [{_currentLobby.Id}] Code: {_currentLobby.LobbyCode}");

                ZeroNetworkManager.Instance.StartHostGame();
            }
            catch (Exception e)
            {
                Debug.LogError($"[LobbyManager] Create Lobby Failed: {e.Message}");
            }
        }

        public async Task RefreshLobbyList()
        {
            try
            {
                QueryLobbiesOptions options = new QueryLobbiesOptions
                {
                    Count = 20,
                    Filters = new List<QueryFilter>
                    {
                        new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                    },
                    Order = new List<QueryOrder>
                    {
                        new QueryOrder(true, QueryOrder.FieldOptions.Created)
                    }
                };

                QueryResponse response = await Lobbies.Instance.QueryLobbiesAsync(options);
                OnLobbyListUpdated?.Invoke(response.Results);
            }
            catch (Exception e)
            {
                Debug.LogError($"[LobbyManager] Query Failed: {e.Message}");
            }
        }

        public async Task JoinLobby(string lobbyId)
        {
            try
            {
                JoinLobbyByIdOptions options = new JoinLobbyByIdOptions
                {
                    Player = new Player
                    {
                        Data = new Dictionary<string, PlayerDataObject>
                        {
                            { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "ClientPlayer") }
                        }
                    }
                };

                _currentLobby = await Lobbies.Instance.JoinLobbyByIdAsync(lobbyId, options);

                string relayJoinCode = _currentLobby.Data["JoinCode"].Value;

                JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);

                var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                transport.SetClientRelayData(
                    joinAllocation.RelayServer.IpV4,
                    (ushort)joinAllocation.RelayServer.Port,
                    joinAllocation.AllocationIdBytes,
                    joinAllocation.Key,
                    joinAllocation.ConnectionData,
                    joinAllocation.HostConnectionData
                );

                ZeroNetworkManager.Instance.StartClientGame();
            }
            catch (Exception e)
            {
                Debug.LogError($"[LobbyManager] Join Failed: {e.Message}");
            }
        }

        public async Task LeaveLobby()
        {
            if (_currentLobby != null)
            {
                try
                {
                    await LobbyService.Instance.RemovePlayerAsync(_currentLobby.Id, AuthenticationService.Instance.PlayerId);
                    _currentLobby = null;
                    ZeroNetworkManager.Instance.DisconnectGame();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[LobbyManager] Leave Failed: {e.Message}");
                }
            }
        }

        #endregion

        private async void HandleLobbyHeartbeat()
        {
            if (_currentLobby != null && _currentLobby.HostId == AuthenticationService.Instance.PlayerId)
            {
                _heartbeatTimer -= Time.deltaTime;
                if (_heartbeatTimer <= 0)
                {
                    _heartbeatTimer = HeartbeatInterval;
                    try
                    {
                        await LobbyService.Instance.SendHeartbeatPingAsync(_currentLobby.Id);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[LobbyManager] Heartbeat failed: {e.Message}");
                    }
                }
            }
        }
    }
#else
    public class LobbyManager : Singleton<LobbyManager>
    {
        public bool IsSignedIn => false;

        public event Action<List<object>> OnLobbyListUpdated;

        public Task InitializeAsync()
        {
            Debug.LogWarning("[LobbyManager] Unity Services packages are not installed.");
            return Task.CompletedTask;
        }

        public Task CreateLobby(string lobbyName, int maxPlayers, bool isPrivate = false)
        {
            Debug.LogWarning("[LobbyManager] Unity Services packages are not installed.");
            return Task.CompletedTask;
        }

        public Task RefreshLobbyList()
        {
            Debug.LogWarning("[LobbyManager] Unity Services packages are not installed.");
            OnLobbyListUpdated?.Invoke(new List<object>());
            return Task.CompletedTask;
        }

        public Task JoinLobby(string lobbyId)
        {
            Debug.LogWarning("[LobbyManager] Unity Services packages are not installed.");
            return Task.CompletedTask;
        }

        public Task LeaveLobby()
        {
            Debug.LogWarning("[LobbyManager] Unity Services packages are not installed.");
            return Task.CompletedTask;
        }
    }
#endif
}
