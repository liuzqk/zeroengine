using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using ZeroEngine.Tutorial;

namespace ZeroEngine.Editor.Tutorial
{
    /// <summary>
    /// Inspector panel for tutorial step properties (v1.14.0+)
    /// </summary>
    public class TutorialNodeInspector
    {
        private readonly VisualElement _container;
        private TutorialStep _currentStep;
        private TutorialSequenceSO _currentSequence;
        private IMGUIContainer _imguiContainer;

        public TutorialNodeInspector(VisualElement container)
        {
            _container = container;
            ShowEmptyState();
        }

        public void ShowStepInspector(TutorialStep step, TutorialSequenceSO sequence)
        {
            _currentStep = step;
            _currentSequence = sequence;

            _container.Clear();

            if (step == null)
            {
                ShowEmptyState();
                return;
            }

            // Step type header
            var typeLabel = new Label(step.StepType);
            typeLabel.style.fontSize = 16;
            typeLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            typeLabel.style.marginBottom = 10;
            typeLabel.style.color = GetStepColor(step);
            _container.Add(typeLabel);

            // Use IMGUI for SerializedObject editing
            _imguiContainer = new IMGUIContainer(() => DrawStepGUI());
            _imguiContainer.style.flexGrow = 1;
            _container.Add(_imguiContainer);
        }

        public void ClearSelection()
        {
            _currentStep = null;
            _currentSequence = null;
            ShowEmptyState();
        }

        private void ShowEmptyState()
        {
            _container.Clear();

            var emptyLabel = new Label("Select a step to edit its properties");
            emptyLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
            emptyLabel.style.marginTop = 20;
            emptyLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _container.Add(emptyLabel);
        }

        private void DrawStepGUI()
        {
            if (_currentStep == null || _currentSequence == null)
            {
                return;
            }

            EditorGUI.BeginChangeCheck();

            // Common properties
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Common", EditorStyles.boldLabel);

            _currentStep.StepId = EditorGUILayout.TextField("Step ID", _currentStep.StepId);
            _currentStep.Description = EditorGUILayout.TextField("Description", _currentStep.Description);
            _currentStep.CanSkip = EditorGUILayout.Toggle("Can Skip", _currentStep.CanSkip);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Step Properties", EditorStyles.boldLabel);

            // Type-specific properties
            switch (_currentStep)
            {
                case DialogueStep dialogue:
                    DrawDialogueProperties(dialogue);
                    break;

                case HighlightStep highlight:
                    DrawHighlightProperties(highlight);
                    break;

                case WaitInputStep waitInput:
                    DrawWaitInputProperties(waitInput);
                    break;

                case WaitInteractionStep waitInteraction:
                    DrawWaitInteractionProperties(waitInteraction);
                    break;

                case WaitEventStep waitEvent:
                    DrawWaitEventProperties(waitEvent);
                    break;

                case MoveToStep moveTo:
                    DrawMoveToProperties(moveTo);
                    break;

                case DelayStep delay:
                    DrawDelayProperties(delay);
                    break;

                case CallbackStep callback:
                    DrawCallbackProperties(callback);
                    break;

                case CompositeStep composite:
                    DrawCompositeProperties(composite);
                    break;

                default:
                    EditorGUILayout.HelpBox($"No custom inspector for {_currentStep.GetType().Name}", MessageType.Info);
                    break;
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(_currentSequence);
            }
        }

        private void DrawDialogueProperties(DialogueStep step)
        {
            step.SpeakerName = EditorGUILayout.TextField("Speaker", step.SpeakerName);

            EditorGUILayout.LabelField("Dialogue Text");
            step.DialogueText = EditorGUILayout.TextArea(step.DialogueText, GUILayout.MinHeight(60));

            step.SpeakerIcon = (Sprite)EditorGUILayout.ObjectField("Speaker Icon", step.SpeakerIcon, typeof(Sprite), false);
            step.Position = (DialoguePosition)EditorGUILayout.EnumPopup("Position", step.Position);

            EditorGUILayout.Space(5);
            step.TypewriterSpeed = EditorGUILayout.FloatField("Typewriter Speed", step.TypewriterSpeed);
            EditorGUILayout.HelpBox("Set to 0 to disable typewriter effect", MessageType.None);

            EditorGUILayout.Space(5);
            step.WaitForConfirm = EditorGUILayout.Toggle("Wait For Confirm", step.WaitForConfirm);

            if (step.WaitForConfirm)
            {
                step.ConfirmKey = (KeyCode)EditorGUILayout.EnumPopup("Confirm Key", step.ConfirmKey);
            }
        }

        private void DrawHighlightProperties(HighlightStep step)
        {
            step.TargetPath = EditorGUILayout.TextField("Target Path", step.TargetPath);
            step.HighlightType = (HighlightType)EditorGUILayout.EnumPopup("Highlight Type", step.HighlightType);
            step.HintOffset = EditorGUILayout.Vector2Field("Hint Offset", step.HintOffset);

            EditorGUILayout.LabelField("Hint Text");
            step.HintText = EditorGUILayout.TextArea(step.HintText, GUILayout.MinHeight(40));

            step.WaitForClick = EditorGUILayout.Toggle("Wait For Click", step.WaitForClick);
            step.Timeout = EditorGUILayout.FloatField("Timeout (0=infinite)", step.Timeout);
        }

        private void DrawWaitInputProperties(WaitInputStep step)
        {
            int keyCount = step.RequiredKeys?.Length ?? 0;
            int newCount = EditorGUILayout.IntField("Key Count", keyCount);
            if (newCount != keyCount)
            {
                var newKeys = new KeyCode[Mathf.Max(0, newCount)];
                for (int i = 0; i < Mathf.Min(keyCount, newKeys.Length); i++)
                {
                    newKeys[i] = step.RequiredKeys[i];
                }
                step.RequiredKeys = newKeys;
            }

            if (step.RequiredKeys != null)
            {
                EditorGUI.indentLevel++;
                for (int i = 0; i < step.RequiredKeys.Length; i++)
                {
                    step.RequiredKeys[i] = (KeyCode)EditorGUILayout.EnumPopup($"Key {i + 1}", step.RequiredKeys[i]);
                }
                EditorGUI.indentLevel--;
            }

            step.RequireAll = EditorGUILayout.Toggle("Require All", step.RequireAll);

            EditorGUILayout.LabelField("Prompt Text");
            step.PromptText = EditorGUILayout.TextArea(step.PromptText, GUILayout.MinHeight(40));

            step.Timeout = EditorGUILayout.FloatField("Timeout (0=infinite)", step.Timeout);
            step.ShowKeyIcon = EditorGUILayout.Toggle("Show Key Icon", step.ShowKeyIcon);
        }

        private void DrawWaitInteractionProperties(WaitInteractionStep step)
        {
            step.InteractableId = EditorGUILayout.TextField("Interactable ID", step.InteractableId);
            step.InteractionRequirement = (InteractionRequirement)EditorGUILayout.EnumPopup("Requirement", step.InteractionRequirement);

            EditorGUILayout.LabelField("Prompt Text");
            step.PromptText = EditorGUILayout.TextArea(step.PromptText, GUILayout.MinHeight(40));

            step.Timeout = EditorGUILayout.FloatField("Timeout (0=infinite)", step.Timeout);
            step.HighlightTarget = EditorGUILayout.Toggle("Highlight Target", step.HighlightTarget);
            step.ShowPathGuide = EditorGUILayout.Toggle("Show Path Guide", step.ShowPathGuide);
        }

        private void DrawWaitEventProperties(WaitEventStep step)
        {
            step.EventKey = EditorGUILayout.TextField("Event Key", step.EventKey);
            step.ExpectedValue = EditorGUILayout.TextField("Expected Value", step.ExpectedValue);

            EditorGUILayout.LabelField("Prompt Text");
            step.PromptText = EditorGUILayout.TextArea(step.PromptText, GUILayout.MinHeight(40));

            step.Timeout = EditorGUILayout.FloatField("Timeout (0=infinite)", step.Timeout);
        }

        private void DrawMoveToProperties(MoveToStep step)
        {
            step.TargetObjectPath = EditorGUILayout.TextField("Target Object Path", step.TargetObjectPath);
            step.TargetPosition = EditorGUILayout.Vector3Field("Target Position", step.TargetPosition);
            step.ArrivalDistance = EditorGUILayout.FloatField("Arrival Distance", step.ArrivalDistance);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Visual", EditorStyles.boldLabel);
            step.ShowArrow = EditorGUILayout.Toggle("Show Arrow", step.ShowArrow);
            step.ShowPathLine = EditorGUILayout.Toggle("Show Path Line", step.ShowPathLine);
            step.ShowOnMinimap = EditorGUILayout.Toggle("Show On Minimap", step.ShowOnMinimap);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Text", EditorStyles.boldLabel);
            step.PromptText = EditorGUILayout.TextField("Prompt Text", step.PromptText);
            step.ArrivalText = EditorGUILayout.TextField("Arrival Text", step.ArrivalText);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
            step.Timeout = EditorGUILayout.FloatField("Timeout (0=infinite)", step.Timeout);
            step.ArrivalDelay = EditorGUILayout.FloatField("Arrival Delay", step.ArrivalDelay);
        }

        private void DrawDelayProperties(DelayStep step)
        {
            step.Duration = EditorGUILayout.FloatField("Duration (sec)", step.Duration);
            step.ShowProgress = EditorGUILayout.Toggle("Show Progress", step.ShowProgress);
            step.PromptText = EditorGUILayout.TextField("Prompt Text", step.PromptText);
        }

        private void DrawCallbackProperties(CallbackStep step)
        {
            step.CallbackId = EditorGUILayout.TextField("Callback ID", step.CallbackId);
            step.WaitForComplete = EditorGUILayout.Toggle("Wait For Complete", step.WaitForComplete);

            EditorGUILayout.LabelField("Parameters");

            // Parameters array
            int paramCount = step.Parameters?.Length ?? 0;
            int newCount = EditorGUILayout.IntField("Size", paramCount);

            if (newCount != paramCount)
            {
                var newParams = new string[newCount];
                for (int i = 0; i < Mathf.Min(paramCount, newCount); i++)
                {
                    newParams[i] = step.Parameters[i];
                }
                step.Parameters = newParams;
            }

            if (step.Parameters != null)
            {
                EditorGUI.indentLevel++;
                for (int i = 0; i < step.Parameters.Length; i++)
                {
                    step.Parameters[i] = EditorGUILayout.TextField($"[{i}]", step.Parameters[i]);
                }
                EditorGUI.indentLevel--;
            }
        }

        private void DrawCompositeProperties(CompositeStep step)
        {
            step.Mode = (CompositeStep.ExecutionMode)EditorGUILayout.EnumPopup("Execution Mode", step.Mode);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Sub-Steps", EditorStyles.boldLabel);

            int count = step.SubSteps?.Count ?? 0;
            EditorGUILayout.LabelField($"Count: {count}");

            EditorGUILayout.HelpBox(
                "Sub-steps can be edited by selecting this composite step in the main Inspector window.",
                MessageType.Info);

            if (step.SubSteps != null)
            {
                for (int i = 0; i < step.SubSteps.Count; i++)
                {
                    var subStep = step.SubSteps[i];
                    EditorGUILayout.LabelField($"  [{i}] {subStep?.StepType ?? "(null)"}");
                }
            }
        }

        private Color GetStepColor(TutorialStep step)
        {
            return step switch
            {
                DialogueStep => TutorialGraphView.DialogueNodeColor,
                HighlightStep => TutorialGraphView.HighlightNodeColor,
                WaitInputStep => TutorialGraphView.WaitNodeColor,
                WaitInteractionStep => TutorialGraphView.WaitNodeColor,
                WaitEventStep => TutorialGraphView.WaitNodeColor,
                MoveToStep => TutorialGraphView.MoveNodeColor,
                DelayStep => TutorialGraphView.DelayNodeColor,
                CallbackStep => TutorialGraphView.DelayNodeColor,
                CompositeStep => TutorialGraphView.CompositeNodeColor,
                _ => Color.gray
            };
        }
    }
}
