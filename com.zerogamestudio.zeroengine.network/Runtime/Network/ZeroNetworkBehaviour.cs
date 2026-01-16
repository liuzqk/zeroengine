using System;
using UnityEngine;
using ZeroEngine.Core;

#if ZEROENGINE_NETCODE
using Unity.Netcode;
#endif

namespace ZeroEngine.Network
{
    /// <summary>
    /// Base class for networked behaviours with common utilities.
    /// Provides simplified RPC patterns and ownership helpers.
    /// </summary>
#if ZEROENGINE_NETCODE
    public abstract class ZeroNetworkBehaviour : NetworkBehaviour
    {
        /// <summary>
        /// Returns true if this is the server/host.
        /// </summary>
        public bool IsAuthoritative => IsServer;

        /// <summary>
        /// Returns true if this client owns this object.
        /// </summary>
        public bool IsLocalOwner => IsOwner;

        /// <summary>
        /// Convenience: Execute action only on server.
        /// </summary>
        protected void ServerOnly(Action action)
        {
            if (IsServer) action?.Invoke();
        }

        /// <summary>
        /// Convenience: Execute action only on owning client.
        /// </summary>
        protected void OwnerOnly(Action action)
        {
            if (IsOwner) action?.Invoke();
        }

        /// <summary>
        /// Convenience: Execute action only on non-owner clients.
        /// </summary>
        protected void RemoteOnly(Action action)
        {
            if (!IsOwner && IsClient) action?.Invoke();
        }

        /// <summary>
        /// Request ownership from the server.
        /// </summary>
        public void RequestOwnership()
        {
            if (!IsOwner && IsClient)
            {
                RequestOwnershipServerRpc();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestOwnershipServerRpc(ServerRpcParams rpcParams = default)
        {
            var clientId = rpcParams.Receive.SenderClientId;
            NetworkObject.ChangeOwnership(clientId);
            Debug.Log($"[ZeroNetworkBehaviour] Ownership transferred to client {clientId}");
        }

        /// <summary>
        /// Called when the network spawns this object.
        /// Override for initialization logic.
        /// </summary>
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            OnNetworkReady();
        }

        /// <summary>
        /// Override this for post-spawn initialization.
        /// </summary>
        protected virtual void OnNetworkReady() { }

        /// <summary>
        /// Called when ownership changes.
        /// </summary>
        public override void OnGainedOwnership()
        {
            base.OnGainedOwnership();
            OnOwnershipGained();
        }

        public override void OnLostOwnership()
        {
            base.OnLostOwnership();
            OnOwnershipLost();
        }

        protected virtual void OnOwnershipGained() { }
        protected virtual void OnOwnershipLost() { }
    }
#else
    /// <summary>
    /// Fallback when Netcode is not available.
    /// </summary>
    public abstract class ZeroNetworkBehaviour : MonoBehaviour
    {
        public bool IsAuthoritative => true;
        public bool IsLocalOwner => true;
        public bool IsServer => true;
        public bool IsClient => true;
        public bool IsOwner => true;
        public bool IsSpawned => true;
        public ulong OwnerClientId => 0;

        protected void ServerOnly(Action action) => action?.Invoke();
        protected void OwnerOnly(Action action) => action?.Invoke();
        protected void RemoteOnly(Action action) { }
        public void RequestOwnership() { }

        protected virtual void OnNetworkReady() { }
        protected virtual void OnOwnershipGained() { }
        protected virtual void OnOwnershipLost() { }

        protected virtual void Start()
        {
            OnNetworkReady();
        }
    }
#endif
}
