using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if TEXTMESHPRO_ENABLED
using TMPro;
#endif

namespace ZeroEngine.Dialog.UI
{
    /// <summary>
    /// Sample Dialog UI component. Attach to a Canvas with required child elements.
    /// </summary>
    public class DialogUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject _dialogPanel;
        
#if TEXTMESHPRO_ENABLED
        [SerializeField] private TextMeshProUGUI _speakerText;
        [SerializeField] private TextMeshProUGUI _dialogText;
#else
        [SerializeField] private Text _speakerText;
        [SerializeField] private Text _dialogText;
#endif
        
        [SerializeField] private Transform _choicesContainer;
        [SerializeField] private Button _choiceButtonPrefab;
        [SerializeField] private Button _continueButton;

        private readonly List<Button> _activeChoiceButtons = new List<Button>();

        private void OnEnable()
        {
            DialogManager.Instance.OnDialogStart += OnDialogStart;
            DialogManager.Instance.OnDialogEnd += OnDialogEnd;
            DialogManager.Instance.OnLineDisplayed += OnLineDisplayed;
            DialogManager.Instance.OnTypewriterUpdate += OnTypewriterUpdate;
            DialogManager.Instance.OnChoicesPresented += OnChoicesPresented;
            
            if (_continueButton != null)
                _continueButton.onClick.AddListener(OnContinueClicked);
        }

        private void OnDisable()
        {
            if (DialogManager.Instance != null)
            {
                DialogManager.Instance.OnDialogStart -= OnDialogStart;
                DialogManager.Instance.OnDialogEnd -= OnDialogEnd;
                DialogManager.Instance.OnLineDisplayed -= OnLineDisplayed;
                DialogManager.Instance.OnTypewriterUpdate -= OnTypewriterUpdate;
                DialogManager.Instance.OnChoicesPresented -= OnChoicesPresented;
            }
            
            if (_continueButton != null)
                _continueButton.onClick.RemoveListener(OnContinueClicked);
        }

        private void OnDialogStart()
        {
            if (_dialogPanel != null)
                _dialogPanel.SetActive(true);
            ClearChoices();
        }

        private void OnDialogEnd()
        {
            if (_dialogPanel != null)
                _dialogPanel.SetActive(false);
        }

        private void OnLineDisplayed(DialogLine line)
        {
            if (_speakerText != null)
                _speakerText.text = line.Speaker;
            
            // Text will be updated by typewriter
            ClearChoices();
            
            if (_continueButton != null)
                _continueButton.gameObject.SetActive(true);
        }

        private void OnTypewriterUpdate(string text)
        {
            if (_dialogText != null)
                _dialogText.text = text;
        }

        private void OnChoicesPresented(List<DialogChoice> choices)
        {
            if (_continueButton != null)
                _continueButton.gameObject.SetActive(false);
            
            ClearChoices();

            for (int i = 0; i < choices.Count; i++)
            {
                var choice = choices[i];
                var btn = Instantiate(_choiceButtonPrefab, _choicesContainer);
                btn.gameObject.SetActive(true);
                
#if TEXTMESHPRO_ENABLED
                var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
#else
                var txt = btn.GetComponentInChildren<Text>();
#endif
                if (txt != null) txt.text = choice.Text;
                
                btn.interactable = choice.IsEnabled;
                
                int index = i; // Capture for closure
                btn.onClick.AddListener(() => OnChoiceClicked(index));
                
                _activeChoiceButtons.Add(btn);
            }
        }

        private void ClearChoices()
        {
            foreach (var btn in _activeChoiceButtons)
            {
                if (btn != null)
                    Destroy(btn.gameObject);
            }
            _activeChoiceButtons.Clear();
        }

        private void OnContinueClicked()
        {
            DialogManager.Instance.DisplayNext();
        }

        private void OnChoiceClicked(int index)
        {
            DialogManager.Instance.SelectChoice(index);
        }
    }
}
