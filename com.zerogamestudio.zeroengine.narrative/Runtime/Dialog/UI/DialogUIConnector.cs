using System.Collections.Generic;
using UnityEngine;

namespace ZeroEngine.Dialog.UI
{
    /// <summary>
    /// Connects DialogRunner events to DialogBoxUI.
    /// Attach this to the same GameObject as DialogRunner or configure references.
    /// </summary>
    [RequireComponent(typeof(DialogRunner))]
    public class DialogUIConnector : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DialogRunner _dialogRunner;
        [SerializeField] private DialogBoxUI _dialogBoxUI;

        [Header("Settings")]
        [SerializeField] private bool _autoShow = true;
        [SerializeField] private bool _autoHide = true;

        private void Awake()
        {
            if (_dialogRunner == null)
                _dialogRunner = GetComponent<DialogRunner>();
        }

        private void OnEnable()
        {
            if (_dialogRunner == null || _dialogBoxUI == null) return;

            _dialogRunner.OnDialogStart += HandleDialogStart;
            _dialogRunner.OnDialogEnd += HandleDialogEnd;
            _dialogRunner.OnLineDisplay += HandleLineDisplay;
            _dialogRunner.OnTypewriterUpdate += HandleTypewriterUpdate;
            _dialogRunner.OnTypewriterComplete += HandleTypewriterComplete;
            _dialogRunner.OnChoicesAvailable += HandleChoicesAvailable;

            _dialogBoxUI.OnContinueClicked += HandleContinue;
            _dialogBoxUI.OnChoiceSelected += HandleChoiceSelected;
        }

        private void OnDisable()
        {
            if (_dialogRunner != null)
            {
                _dialogRunner.OnDialogStart -= HandleDialogStart;
                _dialogRunner.OnDialogEnd -= HandleDialogEnd;
                _dialogRunner.OnLineDisplay -= HandleLineDisplay;
                _dialogRunner.OnTypewriterUpdate -= HandleTypewriterUpdate;
                _dialogRunner.OnTypewriterComplete -= HandleTypewriterComplete;
                _dialogRunner.OnChoicesAvailable -= HandleChoicesAvailable;
            }

            if (_dialogBoxUI != null)
            {
                _dialogBoxUI.OnContinueClicked -= HandleContinue;
                _dialogBoxUI.OnChoiceSelected -= HandleChoiceSelected;
            }
        }

        private void HandleDialogStart()
        {
            if (_autoShow)
                _dialogBoxUI.Show();
        }

        private void HandleDialogEnd(string endTag)
        {
            if (_autoHide)
                _dialogBoxUI.Hide();
        }

        private void HandleLineDisplay(DialogLine line)
        {
            _dialogBoxUI.ShowLine(line);
        }

        private void HandleTypewriterUpdate(string text, float progress)
        {
            _dialogBoxUI.UpdateText(text);
            _dialogBoxUI.SetContinueIndicatorVisible(false);
        }

        private void HandleTypewriterComplete()
        {
            _dialogBoxUI.SetContinueIndicatorVisible(true);
        }

        private void HandleChoicesAvailable(List<DialogChoice> choices)
        {
            _dialogBoxUI.ShowChoices(choices);
        }

        private void HandleContinue()
        {
            _dialogRunner.Advance();
        }

        private void HandleChoiceSelected(int index)
        {
            _dialogRunner.SelectChoice(index);
        }
    }
}
