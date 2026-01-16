using System;
using System.Collections;
using UnityEngine;
using ZeroEngine.Core;

#if ZEROENGINE_NETCODE
using Unity.Netcode;
#endif

namespace ZeroEngine.Network
{
    /// <summary>
    /// Reconnection state for tracking reconnection progress.
    /// </summary>
    public enum ReconnectionState
    {
        Idle,
        Attempting,
        Success,
        Failed
    }

    /// <summary>
    /// Event args for reconnection events.
    /// </summary>
    public struct ReconnectionEventArgs
    {
        public ReconnectionState State;
        public int AttemptNumber;
        public int MaxAttempts;
        public float NextAttemptIn;
        public string FailReason;

        public ReconnectionEventArgs(ReconnectionState state, int attempt = 0, int max = 0, float nextIn = 0, string reason = null)
        {
            State = state;
            AttemptNumber = attempt;
            MaxAttempts = max;
            NextAttemptIn = nextIn;
            FailReason = reason;
        }
    }

    /// <summary>
    /// Handles automatic reconnection when connection is lost.
    /// Attach to the same GameObject as ZeroNetworkManager.
    /// </summary>
    public class ReconnectionHandler : MonoBehaviour
    {
        [Header("Reconnection Settings")]
        [SerializeField] private bool _enableAutoReconnect = true;
        [SerializeField] private int _maxAttempts = 5;
        [SerializeField] private float _initialDelay = 1f;
        [SerializeField] private float _maxDelay = 30f;
        [SerializeField] private float _delayMultiplier = 2f;

        public bool EnableAutoReconnect
        {
            get => _enableAutoReconnect;
            set => _enableAutoReconnect = value;
        }

        public ReconnectionState CurrentState { get; private set; } = ReconnectionState.Idle;

        /// <summary>
        /// Event fired on reconnection state changes.
        /// </summary>
        public event Action<ReconnectionEventArgs> OnReconnectionStateChanged;

        private int _currentAttempt;
        private float _currentDelay;
        private Coroutine _reconnectCoroutine;
        private bool _wasConnected;
        private string _lastIP;
        private ushort _lastPort;
        private bool _wasHost;

#if ZEROENGINE_NETCODE
        private void OnEnable()
        {
            EventManager.Subscribe(GameEvents.NetworkDisconnected, OnDisconnected);
            EventManager.Subscribe(GameEvents.NetworkConnected, OnConnected);
        }

        private void OnDisable()
        {
            EventManager.Unsubscribe(GameEvents.NetworkDisconnected, OnDisconnected);
            EventManager.Unsubscribe(GameEvents.NetworkConnected, OnConnected);
            StopReconnection();
        }

        private void OnConnected()
        {
            _wasConnected = true;

            // Store connection info for potential reconnection
            if (ZeroNetworkManager.Instance != null)
            {
                _lastIP = ZeroNetworkManager.Instance.CurrentIP;
                _lastPort = ZeroNetworkManager.Instance.CurrentPort;
                _wasHost = ZeroNetworkManager.Instance.IsHost;
            }

            // If we were reconnecting, mark as success
            if (CurrentState == ReconnectionState.Attempting)
            {
                CurrentState = ReconnectionState.Success;
                OnReconnectionStateChanged?.Invoke(new ReconnectionEventArgs(ReconnectionState.Success, _currentAttempt, _maxAttempts));
                Debug.Log("[ReconnectionHandler] Reconnection successful!");
                ResetAttempts();
            }
        }

        private void OnDisconnected()
        {
            // Only attempt reconnect if we were previously connected and auto-reconnect is enabled
            if (_wasConnected && _enableAutoReconnect && CurrentState != ReconnectionState.Attempting)
            {
                Debug.Log("[ReconnectionHandler] Connection lost. Starting reconnection...");
                StartReconnection();
            }
        }

        /// <summary>
        /// Manually start reconnection attempts.
        /// </summary>
        public void StartReconnection()
        {
            if (_reconnectCoroutine != null)
            {
                StopCoroutine(_reconnectCoroutine);
            }

            ResetAttempts();
            _reconnectCoroutine = StartCoroutine(ReconnectCoroutine());
        }

        /// <summary>
        /// Stop reconnection attempts.
        /// </summary>
        public void StopReconnection()
        {
            if (_reconnectCoroutine != null)
            {
                StopCoroutine(_reconnectCoroutine);
                _reconnectCoroutine = null;
            }

            if (CurrentState == ReconnectionState.Attempting)
            {
                CurrentState = ReconnectionState.Idle;
                OnReconnectionStateChanged?.Invoke(new ReconnectionEventArgs(ReconnectionState.Idle));
            }
        }

        private void ResetAttempts()
        {
            _currentAttempt = 0;
            _currentDelay = _initialDelay;
            CurrentState = ReconnectionState.Idle;
        }

        private IEnumerator ReconnectCoroutine()
        {
            CurrentState = ReconnectionState.Attempting;

            while (_currentAttempt < _maxAttempts)
            {
                _currentAttempt++;

                Debug.Log($"[ReconnectionHandler] Attempt {_currentAttempt}/{_maxAttempts}...");
                OnReconnectionStateChanged?.Invoke(new ReconnectionEventArgs(
                    ReconnectionState.Attempting,
                    _currentAttempt,
                    _maxAttempts,
                    _currentDelay
                ));

                // Attempt to connect
                if (ZeroNetworkManager.Instance != null)
                {
                    if (_wasHost)
                    {
                        ZeroNetworkManager.Instance.StartHostGame();
                    }
                    else
                    {
                        ZeroNetworkManager.Instance.StartClientGame();
                    }
                }

                // Wait for connection or timeout
                float timeout = Mathf.Min(_currentDelay * 3f, 15f);
                float elapsed = 0f;

                while (elapsed < timeout)
                {
                    if (ZeroNetworkManager.Instance != null &&
                        ZeroNetworkManager.Instance.CurrentConnectionState == ConnectionState.Connected)
                    {
                        // Success! Event handler will update state
                        yield break;
                    }

                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }

                // Connection failed, prepare for next attempt
                if (_currentAttempt < _maxAttempts)
                {
                    Debug.Log($"[ReconnectionHandler] Attempt {_currentAttempt} failed. Waiting {_currentDelay}s before next attempt...");

                    // Wait before next attempt
                    float remaining = _currentDelay;
                    while (remaining > 0)
                    {
                        OnReconnectionStateChanged?.Invoke(new ReconnectionEventArgs(
                            ReconnectionState.Attempting,
                            _currentAttempt,
                            _maxAttempts,
                            remaining
                        ));

                        yield return new WaitForSecondsRealtime(0.5f);
                        remaining -= 0.5f;
                    }

                    // Exponential backoff
                    _currentDelay = Mathf.Min(_currentDelay * _delayMultiplier, _maxDelay);
                }
            }

            // All attempts exhausted
            CurrentState = ReconnectionState.Failed;
            OnReconnectionStateChanged?.Invoke(new ReconnectionEventArgs(
                ReconnectionState.Failed,
                _currentAttempt,
                _maxAttempts,
                0,
                "Max reconnection attempts reached"
            ));

            Debug.LogWarning("[ReconnectionHandler] All reconnection attempts failed.");
            _reconnectCoroutine = null;
        }
#else
        // Stubs for non-Netcode builds
        public void StartReconnection()
        {
            Debug.LogWarning("[ReconnectionHandler] Netcode not available.");
        }

        public void StopReconnection() { }
#endif
    }
}
