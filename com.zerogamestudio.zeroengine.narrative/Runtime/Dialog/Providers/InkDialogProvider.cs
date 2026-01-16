#if INK_PRESENT
using System.Collections.Generic;
using UnityEngine;
using Ink.Runtime;

namespace ZeroEngine.Dialog.Providers
{
    /// <summary>
    /// Provider implementation for Ink stories.
    /// Requires Ink Runtime (com.inklestudios.ink-unity-integration).
    /// </summary>
    public class InkDialogProvider : IDialogProvider
    {
        private readonly Story _story;
        private readonly Dictionary<string, object> _externalVariables = new();

        public InkDialogProvider(TextAsset inkJsonAsset)
        {
            _story = new Story(inkJsonAsset.text);
        }

        public InkDialogProvider(Story story)
        {
            _story = story;
        }

        public void Begin()
        {
            _story.ResetState();
        }

        public void End()
        {
            // Ink doesn't need explicit cleanup
        }

        public bool CanContinue => _story.canContinue;

        public bool HasChoices => _story.currentChoices.Count > 0;

        public DialogLine Continue()
        {
            if (!_story.canContinue) return default;

            string text = _story.Continue().Trim();

            // Parse tags for speaker, portrait, etc.
            string speaker = "";
            string portraitKey = "";
            
            foreach (var tag in _story.currentTags)
            {
                if (tag.StartsWith("speaker:"))
                    speaker = tag.Substring("speaker:".Length).Trim();
                else if (tag.StartsWith("portrait:"))
                    portraitKey = tag.Substring("portrait:".Length).Trim();
            }

            return new DialogLine
            {
                Speaker = speaker,
                Text = text,
                PortraitKey = portraitKey
            };
        }

        public List<DialogChoice> GetChoices()
        {
            var choices = new List<DialogChoice>();

            for (int i = 0; i < _story.currentChoices.Count; i++)
            {
                var inkChoice = _story.currentChoices[i];
                choices.Add(new DialogChoice
                {
                    Text = inkChoice.text.Trim(),
                    TargetIndex = inkChoice.index,
                    IsEnabled = true
                });
            }

            return choices;
        }

        public void SelectChoice(int index)
        {
            if (index >= 0 && index < _story.currentChoices.Count)
            {
                _story.ChooseChoiceIndex(index);
            }
        }

        public void SetVariable(string name, object value)
        {
            _story.variablesState[name] = value;
        }

        public object GetVariable(string name)
        {
            return _story.variablesState[name];
        }
    }
}
#endif
