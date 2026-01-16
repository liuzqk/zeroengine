using System;
using System.Collections.Generic;

namespace ZeroEngine.Dialog
{
    /// <summary>
    /// Represents a single line of dialogue.
    /// </summary>
    [Serializable]
    public struct DialogLine
    {
        /// <summary>Speaker name (can be empty for narration).</summary>
        public string Speaker;
        
        /// <summary>The dialogue text content.</summary>
        public string Text;
        
        /// <summary>Optional localization key for the text.</summary>
        public string LocalizationKey;
        
        /// <summary>Optional audio clip key for voice acting.</summary>
        public string VoiceKey;
        
        /// <summary>Optional portrait/expression key.</summary>
        public string PortraitKey;
        
        /// <summary>Custom metadata (e.g., emotion, animation trigger).</summary>
        public Dictionary<string, string> Metadata;
    }

    /// <summary>
    /// Represents a choice option in a dialogue.
    /// </summary>
    [Serializable]
    public struct DialogChoice
    {
        /// <summary>Display text for the choice.</summary>
        public string Text;
        
        /// <summary>Optional localization key.</summary>
        public string LocalizationKey;
        
        /// <summary>Index to jump to after selection (provider-specific).</summary>
        public int TargetIndex;
        
        /// <summary>Optional condition expression (provider evaluates).</summary>
        public string Condition;
        
        /// <summary>Is this choice currently available?</summary>
        public bool IsEnabled;
    }

    /// <summary>
    /// Interface for dialogue data providers.
    /// Allows DialogManager to work with different backends (SO, Ink, etc.)
    /// </summary>
    public interface IDialogProvider
    {
        /// <summary>Start a dialogue session.</summary>
        void Begin();

        /// <summary>End the current dialogue session.</summary>
        void End();

        /// <summary>Is there more content to display?</summary>
        bool CanContinue { get; }

        /// <summary>Does the current state have choices?</summary>
        bool HasChoices { get; }

        /// <summary>Get the next line of dialogue.</summary>
        DialogLine Continue();

        /// <summary>Get available choices at current state.</summary>
        List<DialogChoice> GetChoices();

        /// <summary>Select a choice by index.</summary>
        void SelectChoice(int index);

        /// <summary>Set a variable in the dialogue state.</summary>
        void SetVariable(string name, object value);

        /// <summary>Get a variable from the dialogue state.</summary>
        object GetVariable(string name);
    }
}
