using UnityEditor;
using UnityEngine;

namespace ZeroEngine.Audio.Editor
{
    [CustomEditor(typeof(AudioDebugger))]
    public class AudioDebuggerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            AudioDebugger debugger = (AudioDebugger)target;

            GUILayout.Space(10);
            GUILayout.Label("Actions", EditorStyles.boldLabel);

            if (GUILayout.Button("Play SFX Cue"))
            {
                debugger.PlaySfxCue();
            }

            if (GUILayout.Button("Play Legacy Clip"))
            {
                debugger.PlayLegacyClip();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Play Music"))
            {
                debugger.PlayMusic();
            }

            if (GUILayout.Button("Stop Music"))
            {
                debugger.StopMusic();
            }
        }
    }
}
