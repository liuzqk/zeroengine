using UnityEngine;
using UnityEngine.InputSystem;
using ZeroEngine.Core;

namespace ZeroEngine.InputSystem
{
    /// <summary>
    /// Global Input Manager (Singleton)
    /// Responsible for holding and managing InputActionAsset.
    /// Follows Unity 6 Standards with Action Maps.
    /// </summary>
    public class InputManager : Singleton<InputManager>
    {
        [Header("Configuration")]
        [SerializeField] private InputActionAsset _inputActionAsset;

        // Public Action Maps
        public InputActionMap PlayerActions { get; private set; }
        public InputActionMap UIActions { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;

            if (_inputActionAsset == null)
            {
                Debug.LogError("[InputManager] InputActionAsset is not assigned! Please assign a valid Input Action Asset in the Inspector.");
                return;
            }

            // Find and cache Action Maps
            // Ensure your Action Maps are named "Player" and "UI" in the Input Actions Asset
            PlayerActions = _inputActionAsset.FindActionMap("Player");
            UIActions = _inputActionAsset.FindActionMap("UI");

            if (PlayerActions == null) Debug.LogError("[InputManager] 'Player' Action Map not found!");
            if (UIActions == null) Debug.LogError("[InputManager] 'UI' Action Map not found!");
        }

        private void OnEnable()
        {
            EnableAllActions();
        }

        private void OnDisable()
        {
            DisableAllActions();
        }

        /// <summary>
        /// Enables all Action Maps (Gameplay + UI)
        /// </summary>
        public void EnableAllActions()
        {
            PlayerActions?.Enable();
            UIActions?.Enable();
        }

        /// <summary>
        /// Disables all Action Maps
        /// </summary>
        public void DisableAllActions()
        {
            PlayerActions?.Disable();
            UIActions?.Disable();
        }

        /// <summary>
        /// Switch to Gameplay Control Mode (Disables UI input, Enables Player input)
        /// </summary>
        public void SwitchToGameplayMode()
        {
            PlayerActions?.Enable();
            UIActions?.Disable();
        }

        /// <summary>
        /// Switch to UI Control Mode (Disables Player input, Enables UI input)
        /// </summary>
        public void SwitchToUIMode()
        {
            PlayerActions?.Disable();
            UIActions?.Enable();
        }
    }
}
