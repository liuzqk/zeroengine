using UnityEngine;
using UnityEngine.UI;

#if UNITY_LOCALIZATION
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
#endif

#if TEXTMESHPRO_ENABLED
using TMPro;
#endif

namespace ZeroEngine.Localization
{
    /// <summary>
    /// Auto-updates UI Text with localized string.
    /// Attach to GameObject with Text or TMP_Text component.
    /// </summary>
    public class LocalizedText : MonoBehaviour
    {
        [Header("Localization Key")]
        [SerializeField] private string _tableName = "UI";
        [SerializeField] private string _entryName;

        [Header("Components (Auto-detected if empty)")]
        [SerializeField] private Text _legacyText;
#if TEXTMESHPRO_ENABLED
        [SerializeField] private TMP_Text _tmpText;
#endif

        private void Awake()
        {
            // Auto-detect components
            if (_legacyText == null)
                _legacyText = GetComponent<Text>();
            
#if TEXTMESHPRO_ENABLED
            if (_tmpText == null)
                _tmpText = GetComponent<TMP_Text>();
#endif
        }

        private void OnEnable()
        {
            UpdateText();
            
#if UNITY_LOCALIZATION
            if (LocalizationManager.Instance != null)
                LocalizationManager.Instance.OnLocaleChanged += OnLocaleChanged;
#endif
        }

        private void OnDisable()
        {
#if UNITY_LOCALIZATION
            if (LocalizationManager.Instance != null)
                LocalizationManager.Instance.OnLocaleChanged -= OnLocaleChanged;
#endif
        }

#if UNITY_LOCALIZATION
        private void OnLocaleChanged(Locale locale)
        {
            UpdateText();
        }
#endif

        /// <summary>
        /// Set localization key and update text.
        /// </summary>
        public void SetKey(string tableName, string entryName)
        {
            _tableName = tableName;
            _entryName = entryName;
            UpdateText();
        }

        /// <summary>
        /// Update text with current localized string.
        /// </summary>
        public void UpdateText()
        {
            if (string.IsNullOrEmpty(_entryName)) return;

#if UNITY_LOCALIZATION
            string text = LocalizationManager.Instance.GetString(_tableName, _entryName);
#else
            string text = $"[{_tableName}/{_entryName}]";
#endif

            if (_legacyText != null)
                _legacyText.text = text;

#if TEXTMESHPRO_ENABLED
            if (_tmpText != null)
                _tmpText.text = text;
#endif
        }
    }
}
