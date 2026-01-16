using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ZeroEngine.Core;
using ZeroEngine.Dialog.Providers;

namespace ZeroEngine.Dialog
{
    /// <summary>
    /// Central manager for dialogue playback.
    /// Works with any IDialogProvider implementation.
    /// </summary>
    public class DialogManager : Singleton<DialogManager>
    {
        #region Settings

        [Header("Typewriter Effect")]
        [SerializeField] private bool _enableTypewriter = true;
        [SerializeField] private float _charactersPerSecond = 30f;

        [Header("Integration")]
        [SerializeField] private bool _autoPlayVoice = true;
        [SerializeField] private bool _autoResolveLocalization = true;

        #endregion

        #region Events

        /// <summary>Fired when a dialogue session starts.</summary>
        public event Action OnDialogStart;

        /// <summary>Fired when a dialogue session ends.</summary>
        public event Action OnDialogEnd;

        /// <summary>Fired when a new line is displayed (after localization resolved).</summary>
        public event Action<DialogLine> OnLineDisplayed;

        /// <summary>Fired for each character during typewriter effect.</summary>
        public event Action<string> OnTypewriterUpdate;

        /// <summary>Fired when typewriter finishes a line.</summary>
        public event Action OnTypewriterComplete;

        /// <summary>Fired when choices are presented.</summary>
        public event Action<List<DialogChoice>> OnChoicesPresented;

        /// <summary>Fired when a choice is selected.</summary>
        public event Action<int> OnChoiceSelected;

        #endregion

        #region State

        private IDialogProvider _currentProvider;
        private bool _isPlaying;
        private Coroutine _typewriterRoutine;
        private bool _isTypewriting;
        private string _currentFullText;

        /// <summary>Is a dialogue currently active?</summary>
        public bool IsPlaying => _isPlaying;

        /// <summary>Is typewriter effect currently running?</summary>
        public bool IsTypewriting => _isTypewriting;

        /// <summary>Current provider (null if not playing).</summary>
        public IDialogProvider CurrentProvider => _currentProvider;

        #endregion

        #region Public API

        /// <summary>
        /// Start a dialogue session with a DialogueSO.
        /// </summary>
        public void StartDialogue(DialogueSO dialogue)
        {
            if (dialogue == null)
            {
                Debug.LogWarning("[DialogManager] Cannot start null dialogue.");
                return;
            }

            var provider = new DialogueSOProvider(dialogue);
            StartDialogue(provider);
        }

        /// <summary>
        /// Start a dialogue session with a custom provider.
        /// </summary>
        public void StartDialogue(IDialogProvider provider)
        {
            if (_isPlaying)
            {
                Debug.LogWarning("[DialogManager] A dialogue is already playing. Call StopDialogue() first.");
                return;
            }

            _currentProvider = provider;
            _currentProvider.Begin();
            _isPlaying = true;

            OnDialogStart?.Invoke();

            // Automatically display first line
            DisplayNext();
        }

        /// <summary>
        /// Advance to the next line or present choices.
        /// If typewriter is running, skip to end of current line.
        /// </summary>
        public void DisplayNext()
        {
            if (!_isPlaying || _currentProvider == null) return;

            // If typewriter is running, skip to end
            if (_isTypewriting)
            {
                SkipTypewriter();
                return;
            }

            if (_currentProvider.HasChoices)
            {
                var choices = _currentProvider.GetChoices();
                // Resolve localization for choices
                if (_autoResolveLocalization)
                {
                    for (int i = 0; i < choices.Count; i++)
                    {
                        var c = choices[i];
                        if (!string.IsNullOrEmpty(c.LocalizationKey))
                        {
                            c.Text = ResolveLocalization(c.LocalizationKey, c.Text);
                            choices[i] = c;
                        }
                    }
                }
                OnChoicesPresented?.Invoke(choices);
            }
            else if (_currentProvider.CanContinue)
            {
                var line = _currentProvider.Continue();
                
                // Resolve localization
                if (_autoResolveLocalization && !string.IsNullOrEmpty(line.LocalizationKey))
                {
                    line.Text = ResolveLocalization(line.LocalizationKey, line.Text);
                }

                // Play voice
                if (_autoPlayVoice && !string.IsNullOrEmpty(line.VoiceKey))
                {
                    PlayVoice(line.VoiceKey);
                }

                OnLineDisplayed?.Invoke(line);

                // Start typewriter
                if (_enableTypewriter && !string.IsNullOrEmpty(line.Text))
                {
                    StartTypewriter(line.Text);
                }
                else
                {
                    OnTypewriterComplete?.Invoke();
                }
            }
            else
            {
                EndDialogue();
            }
        }

        /// <summary>
        /// Select a choice by index.
        /// </summary>
        public void SelectChoice(int index)
        {
            if (!_isPlaying || _currentProvider == null) return;
            if (_isTypewriting) SkipTypewriter();

            _currentProvider.SelectChoice(index);
            OnChoiceSelected?.Invoke(index);

            // Auto-continue after choice
            DisplayNext();
        }

        /// <summary>
        /// Force stop the current dialogue.
        /// </summary>
        public void StopDialogue()
        {
            if (!_isPlaying) return;
            StopTypewriter();
            EndDialogue();
        }

        /// <summary>
        /// Set a variable in the current dialogue state.
        /// </summary>
        public void SetVariable(string name, object value)
        {
            _currentProvider?.SetVariable(name, value);
        }

        /// <summary>
        /// Get a variable from the current dialogue state.
        /// </summary>
        public object GetVariable(string name)
        {
            return _currentProvider?.GetVariable(name);
        }

        /// <summary>
        /// Skip typewriter effect to show full text immediately.
        /// </summary>
        public void SkipTypewriter()
        {
            if (!_isTypewriting) return;
            StopTypewriter();
            OnTypewriterUpdate?.Invoke(_currentFullText);
            OnTypewriterComplete?.Invoke();
        }

        #endregion

        #region Internal

        private void EndDialogue()
        {
            if (_currentProvider != null)
            {
                _currentProvider.End();
                _currentProvider = null;
            }

            _isPlaying = false;
            OnDialogEnd?.Invoke();
        }

        private void StartTypewriter(string text)
        {
            StopTypewriter();
            _currentFullText = text;
            _typewriterRoutine = StartCoroutine(TypewriterRoutine(text));
        }

        private void StopTypewriter()
        {
            if (_typewriterRoutine != null)
            {
                StopCoroutine(_typewriterRoutine);
                _typewriterRoutine = null;
            }
            _isTypewriting = false;
        }

        private IEnumerator TypewriterRoutine(string text)
        {
            _isTypewriting = true;
            float delay = 1f / Mathf.Max(_charactersPerSecond, 1f);
            int charIndex = 0;

            while (charIndex < text.Length)
            {
                charIndex++;
                OnTypewriterUpdate?.Invoke(text.Substring(0, charIndex));
                yield return new WaitForSeconds(delay);
            }

            _isTypewriting = false;
            _typewriterRoutine = null;
            OnTypewriterComplete?.Invoke();
        }

        private string ResolveLocalization(string key, string fallback)
        {
#if LOCALIZATION_ENABLED
            // Try to use LocalizationManager if available
            if (Localization.LocalizationManager.Instance != null)
            {
                // Assuming key format is "TableName/EntryName" or just "EntryName" with default table
                var parts = key.Split('/');
                string localized;
                if (parts.Length >= 2)
                    localized = Localization.LocalizationManager.Instance.GetString(parts[0], parts[1]);
                else
                    localized = Localization.LocalizationManager.Instance.GetString("Dialog", key);
                if (!string.IsNullOrEmpty(localized)) return localized;
            }
#endif
            return fallback;
        }

        private void PlayVoice(string voiceKey)
        {
            // Hook for Audio integration
            // Subclasses or listeners can implement custom voice playback
            // For now, just log
            Debug.Log($"[DialogManager] PlayVoice: {voiceKey}");
            
            // TODO: Integrate with AudioManager when AudioCue supports string keys
            // AudioManager.Instance.PlaySFX(voiceKey);
        }

        #endregion
    }
}

