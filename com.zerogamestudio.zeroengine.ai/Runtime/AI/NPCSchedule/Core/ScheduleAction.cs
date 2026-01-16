using System;
using UnityEngine;

namespace ZeroEngine.AI.NPCSchedule
{
    /// <summary>
    /// 日程行动基类
    /// 定义 NPC 在日程期间执行的行为
    /// </summary>
    [Serializable]
    public abstract class ScheduleAction
    {
        #region Serialized Fields

        [SerializeField] protected string _actionName = "";
        [SerializeField] protected string _description = "";
        [SerializeField] protected bool _isInterruptible = true;

        #endregion

        #region Properties

        /// <summary>行动名称</summary>
        public string ActionName => string.IsNullOrEmpty(_actionName) ? GetType().Name : _actionName;

        /// <summary>描述</summary>
        public string Description => _description;

        /// <summary>是否可中断</summary>
        public bool IsInterruptible => _isInterruptible;

        /// <summary>当前状态</summary>
        public ScheduleActionState State { get; protected set; } = ScheduleActionState.Idle;

        #endregion

        #region Abstract Methods

        /// <summary>
        /// 开始执行行动
        /// </summary>
        public abstract void Start(AIContext context, ScheduleEntry entry);

        /// <summary>
        /// 更新行动
        /// </summary>
        /// <returns>是否完成</returns>
        public abstract bool Update(AIContext context, float deltaTime);

        /// <summary>
        /// 结束行动
        /// </summary>
        public abstract void End(AIContext context, bool interrupted);

        #endregion

        #region Virtual Methods

        /// <summary>
        /// 检查行动是否可以执行
        /// </summary>
        public virtual bool CanExecute(AIContext context)
        {
            return true;
        }

        /// <summary>
        /// 重置行动状态
        /// </summary>
        public virtual void Reset()
        {
            State = ScheduleActionState.Idle;
        }

        #endregion
    }

    /// <summary>
    /// 日程行动状态
    /// </summary>
    public enum ScheduleActionState
    {
        Idle,
        Starting,
        Running,
        Completed,
        Interrupted
    }

    /// <summary>
    /// 空闲行动 - 什么都不做
    /// </summary>
    [Serializable]
    public class IdleScheduleAction : ScheduleAction
    {
        public IdleScheduleAction()
        {
            _actionName = "Idle";
            _description = "Stand idle";
        }

        public override void Start(AIContext context, ScheduleEntry entry)
        {
            State = ScheduleActionState.Running;
        }

        public override bool Update(AIContext context, float deltaTime)
        {
            // 空闲行动永不完成，直到时间结束
            return false;
        }

        public override void End(AIContext context, bool interrupted)
        {
            State = interrupted ? ScheduleActionState.Interrupted : ScheduleActionState.Completed;
        }
    }

    /// <summary>
    /// 等待行动 - 等待一段时间
    /// </summary>
    [Serializable]
    public class WaitScheduleAction : ScheduleAction
    {
        [SerializeField] private float _duration = 1f;
        private float _elapsed;

        public WaitScheduleAction()
        {
            _actionName = "Wait";
        }

        public WaitScheduleAction(float duration)
        {
            _duration = duration;
            _actionName = $"Wait({duration}s)";
        }

        public override void Start(AIContext context, ScheduleEntry entry)
        {
            State = ScheduleActionState.Running;
            _elapsed = 0f;
        }

        public override bool Update(AIContext context, float deltaTime)
        {
            _elapsed += deltaTime;
            return _elapsed >= _duration;
        }

        public override void End(AIContext context, bool interrupted)
        {
            State = interrupted ? ScheduleActionState.Interrupted : ScheduleActionState.Completed;
        }

        public override void Reset()
        {
            base.Reset();
            _elapsed = 0f;
        }
    }

    /// <summary>
    /// 播放动画行动
    /// </summary>
    [Serializable]
    public class AnimationScheduleAction : ScheduleAction
    {
        [SerializeField] private string _animationState = "";
        [SerializeField] private bool _loop = true;
        [SerializeField] private float _crossFadeDuration = 0.2f;

        private Animator _animator;

        public AnimationScheduleAction()
        {
            _actionName = "Animation";
        }

        public override void Start(AIContext context, ScheduleEntry entry)
        {
            State = ScheduleActionState.Running;

            _animator = context.GetCachedComponent<Animator>();
            if (_animator != null && !string.IsNullOrEmpty(_animationState))
            {
                _animator.CrossFade(_animationState, _crossFadeDuration);
            }
        }

        public override bool Update(AIContext context, float deltaTime)
        {
            // 如果循环，永不完成
            if (_loop) return false;

            // 检查动画是否播放完成
            if (_animator != null)
            {
                var stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
                if (stateInfo.IsName(_animationState) && stateInfo.normalizedTime >= 1f)
                {
                    return true;
                }
            }

            return false;
        }

        public override void End(AIContext context, bool interrupted)
        {
            State = interrupted ? ScheduleActionState.Interrupted : ScheduleActionState.Completed;
        }
    }

    /// <summary>
    /// 序列行动 - 按顺序执行多个行动
    /// </summary>
    [Serializable]
    public class SequenceScheduleAction : ScheduleAction
    {
        [SerializeReference]
        private ScheduleAction[] _actions = Array.Empty<ScheduleAction>();

        private int _currentIndex;

        public SequenceScheduleAction()
        {
            _actionName = "Sequence";
        }

        public override void Start(AIContext context, ScheduleEntry entry)
        {
            State = ScheduleActionState.Running;
            _currentIndex = 0;

            if (_actions.Length > 0)
            {
                _actions[0]?.Start(context, entry);
            }
        }

        public override bool Update(AIContext context, float deltaTime)
        {
            if (_currentIndex >= _actions.Length)
            {
                return true;
            }

            var currentAction = _actions[_currentIndex];
            if (currentAction == null)
            {
                _currentIndex++;
                return _currentIndex >= _actions.Length;
            }

            bool completed = currentAction.Update(context, deltaTime);

            if (completed)
            {
                currentAction.End(context, false);
                _currentIndex++;

                if (_currentIndex < _actions.Length)
                {
                    _actions[_currentIndex]?.Start(context, null);
                }
            }

            return _currentIndex >= _actions.Length;
        }

        public override void End(AIContext context, bool interrupted)
        {
            if (_currentIndex < _actions.Length)
            {
                _actions[_currentIndex]?.End(context, interrupted);
            }

            State = interrupted ? ScheduleActionState.Interrupted : ScheduleActionState.Completed;
        }

        public override void Reset()
        {
            base.Reset();
            _currentIndex = 0;

            foreach (var action in _actions)
            {
                action?.Reset();
            }
        }
    }

    /// <summary>
    /// 随机行动 - 随机选择一个行动执行
    /// </summary>
    [Serializable]
    public class RandomScheduleAction : ScheduleAction
    {
        [SerializeReference]
        private ScheduleAction[] _actions = Array.Empty<ScheduleAction>();

        private ScheduleAction _selectedAction;

        public RandomScheduleAction()
        {
            _actionName = "Random";
        }

        public override void Start(AIContext context, ScheduleEntry entry)
        {
            State = ScheduleActionState.Running;

            if (_actions.Length > 0)
            {
                int index = UnityEngine.Random.Range(0, _actions.Length);
                _selectedAction = _actions[index];
                _selectedAction?.Start(context, entry);
            }
        }

        public override bool Update(AIContext context, float deltaTime)
        {
            if (_selectedAction == null) return true;
            return _selectedAction.Update(context, deltaTime);
        }

        public override void End(AIContext context, bool interrupted)
        {
            _selectedAction?.End(context, interrupted);
            State = interrupted ? ScheduleActionState.Interrupted : ScheduleActionState.Completed;
        }

        public override void Reset()
        {
            base.Reset();
            _selectedAction = null;

            foreach (var action in _actions)
            {
                action?.Reset();
            }
        }
    }
}
