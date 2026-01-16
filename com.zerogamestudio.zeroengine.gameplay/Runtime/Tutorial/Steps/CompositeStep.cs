using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZeroEngine.Tutorial
{
    /// <summary>
    /// 组合步骤 (v1.14.0+)
    /// 包含多个子步骤，支持顺序或并行执行
    /// </summary>
    [Serializable]
    public class CompositeStep : TutorialStep
    {
        /// <summary>执行模式</summary>
        public enum ExecutionMode
        {
            Sequential,     // 顺序执行
            Parallel,       // 并行执行
            Any             // 任一完成即可
        }

        [Header("Composite Settings")]
        [Tooltip("执行模式")]
        public ExecutionMode Mode = ExecutionMode.Sequential;

        [Tooltip("子步骤列表")]
        [SerializeReference]
        public List<TutorialStep> SubSteps = new();

        // 运行时状态
        [NonSerialized]
        private int _currentSubStepIndex;
        [NonSerialized]
        private bool[] _subStepCompleted;
        [NonSerialized]
        private bool _isCompleted;

        public override string StepType => $"Composite ({Mode})";

        public override void OnEnter(TutorialContext ctx)
        {
            _currentSubStepIndex = 0;
            _isCompleted = false;
            _subStepCompleted = new bool[SubSteps.Count];

            switch (Mode)
            {
                case ExecutionMode.Sequential:
                    // 开始第一个子步骤
                    if (SubSteps.Count > 0)
                    {
                        SubSteps[0]?.OnEnter(ctx);
                    }
                    else
                    {
                        _isCompleted = true;
                    }
                    break;

                case ExecutionMode.Parallel:
                case ExecutionMode.Any:
                    // 同时开始所有子步骤
                    foreach (var step in SubSteps)
                    {
                        step?.OnEnter(ctx);
                    }
                    break;
            }
        }

        public override void OnUpdate(TutorialContext ctx)
        {
            if (_isCompleted) return;

            switch (Mode)
            {
                case ExecutionMode.Sequential:
                    UpdateSequential(ctx);
                    break;

                case ExecutionMode.Parallel:
                    UpdateParallel(ctx);
                    break;

                case ExecutionMode.Any:
                    UpdateAny(ctx);
                    break;
            }
        }

        private void UpdateSequential(TutorialContext ctx)
        {
            if (_currentSubStepIndex >= SubSteps.Count)
            {
                _isCompleted = true;
                return;
            }

            var currentStep = SubSteps[_currentSubStepIndex];
            if (currentStep == null)
            {
                AdvanceToNextSubStep(ctx);
                return;
            }

            // 更新当前子步骤
            currentStep.OnUpdate(ctx);

            // 检查完成
            if (currentStep.IsCompleted(ctx))
            {
                currentStep.OnExit(ctx);
                AdvanceToNextSubStep(ctx);
            }
        }

        private void AdvanceToNextSubStep(TutorialContext ctx)
        {
            _currentSubStepIndex++;

            if (_currentSubStepIndex >= SubSteps.Count)
            {
                _isCompleted = true;
            }
            else
            {
                SubSteps[_currentSubStepIndex]?.OnEnter(ctx);
            }
        }

        private void UpdateParallel(TutorialContext ctx)
        {
            bool allCompleted = true;

            for (int i = 0; i < SubSteps.Count; i++)
            {
                if (_subStepCompleted[i]) continue;

                var step = SubSteps[i];
                if (step == null)
                {
                    _subStepCompleted[i] = true;
                    continue;
                }

                step.OnUpdate(ctx);

                if (step.IsCompleted(ctx))
                {
                    step.OnExit(ctx);
                    _subStepCompleted[i] = true;
                }
                else
                {
                    allCompleted = false;
                }
            }

            _isCompleted = allCompleted;
        }

        private void UpdateAny(TutorialContext ctx)
        {
            for (int i = 0; i < SubSteps.Count; i++)
            {
                var step = SubSteps[i];
                if (step == null) continue;

                step.OnUpdate(ctx);

                if (step.IsCompleted(ctx))
                {
                    // 任一完成即可
                    _isCompleted = true;

                    // 退出所有子步骤
                    foreach (var s in SubSteps)
                    {
                        s?.OnExit(ctx);
                    }
                    return;
                }
            }
        }

        public override void OnExit(TutorialContext ctx)
        {
            // 确保所有子步骤都已退出
            switch (Mode)
            {
                case ExecutionMode.Sequential:
                    // 当前步骤已在循环中退出
                    break;

                case ExecutionMode.Parallel:
                case ExecutionMode.Any:
                    // 已在 UpdateAny 中退出
                    break;
            }
        }

        public override bool IsCompleted(TutorialContext ctx)
        {
            return _isCompleted;
        }

        public override void OnSkip(TutorialContext ctx)
        {
            // 跳过所有子步骤
            foreach (var step in SubSteps)
            {
                step?.OnSkip(ctx);
            }
            _isCompleted = true;
        }

        public override string GetDisplayText()
        {
            return !string.IsNullOrEmpty(Description)
                ? Description
                : $"{Mode} ({SubSteps.Count} steps)";
        }

        public override bool Validate(out string error)
        {
            if (SubSteps == null || SubSteps.Count == 0)
            {
                error = "At least one sub-step is required";
                return false;
            }

            for (int i = 0; i < SubSteps.Count; i++)
            {
                var step = SubSteps[i];
                if (step == null)
                {
                    error = $"Sub-step {i} is null";
                    return false;
                }

                if (!step.Validate(out string subError))
                {
                    error = $"Sub-step {i}: {subError}";
                    return false;
                }
            }

            error = null;
            return true;
        }
    }
}
