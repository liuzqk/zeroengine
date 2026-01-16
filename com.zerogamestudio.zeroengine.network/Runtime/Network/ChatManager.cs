using System;
using UnityEngine;

#if ZEROENGINE_NETCODE
using Unity.Netcode;
#endif

namespace ZeroEngine.Network
{
    public enum ChatChannel
    {
        World,
        Team,
        Private,
        System
    }

#if ZEROENGINE_NETCODE
    public class ChatManager : NetworkBehaviour
    {
        public static ChatManager Instance { get; private set; }

        public static event Action<ulong, string, ChatChannel> OnMessageReceived;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SendMessage(string text, ChatChannel channel = ChatChannel.World)
        {
            if (string.IsNullOrEmpty(text)) return;

            if (IsServer)
            {
                BroadcastMessageClientRpc(NetworkManager.Singleton.LocalClientId, text, channel);
            }
            else
            {
                SendMessageServerRpc(text, channel);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void SendMessageServerRpc(string text, ChatChannel channel, ServerRpcParams rpcParams = default)
        {
            ulong senderId = rpcParams.Receive.SenderClientId;
            BroadcastMessageClientRpc(senderId, text, channel);
        }

        [ClientRpc]
        private void BroadcastMessageClientRpc(ulong senderId, string text, ChatChannel channel)
        {
            OnMessageReceived?.Invoke(senderId, text, channel);
        }
    }
#else
    public class ChatManager : MonoBehaviour
    {
        public static ChatManager Instance { get; private set; }

        public static event Action<ulong, string, ChatChannel> OnMessageReceived;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SendMessage(string text, ChatChannel channel = ChatChannel.World)
        {
            if (string.IsNullOrEmpty(text)) return;

            Debug.LogWarning("[ChatManager] Netcode for GameObjects is not installed.");
            OnMessageReceived?.Invoke(0, text, channel);
        }
    }
#endif
}
