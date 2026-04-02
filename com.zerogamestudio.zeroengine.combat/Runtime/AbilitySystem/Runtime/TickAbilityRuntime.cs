using System;

namespace ZeroEngine.AbilitySystem
{
    /// <summary>
    /// Tick-based 执行模式 — 由外部 Tick() 驱动
    /// 无协程、无 Time.deltaTime 依赖
    /// 适用于 P6 自走棋等离散步进战斗
    ///
    /// 使用方式:
    ///   runtime.Update(tickInterval);  // 每个 tick 调用一次
    /// </summary>
    public class TickAbilityRuntime : IAbilityRuntime
    {
        private readonly IEffectResolver _resolver;

        private AbilityInstance _currentAbility;
        private IAbilityContext _currentContext;
        private float _stateTimer;
        private float _totalStateTime;

        public AbilityCastState CurrentState { get; private set; } = AbilityCastState.Idle;
        public float CastProgress { get; private set; }

        public event Action<AbilityStateChangedArgs> OnStateChanged;
        public event Action<AbilityInterruptedArgs> OnInterrupted;
        public event Action<AbilityExecutedArgs> OnExecuted;

        public TickAbilityRuntime(IEffectResolver resolver)
        {
            _resolver = resolver;
        }

        public bool TryCast(AbilityInstance ability, IAbilityContext context)
        {
            if (CurrentState != AbilityCastState.Idle) return false;
            if (ability.IsOnCooldown) return false;

            // 检查条件
            var conditions = ability.Data.Conditions;
            for (int i = 0; i < conditions.Count; i++)
            {
                if (!_resolver.CheckCondition(conditions[i], context))
                    return false;
            }

            _currentAbility = ability;
            _currentContext = context;

            if (ability.Data.CastTime > 0)
            {
                // 有前摇 → 进入 Casting 状态
                _totalStateTime = ability.Data.CastTime;
                _stateTimer = 0;
                SetState(AbilityCastState.Casting);
            }
            else
            {
                // 无前摇 → 直接执行
                ExecuteAndTransition();
            }

            return true;
        }

        public void Update(float deltaTime)
        {
            if (CurrentState == AbilityCastState.Idle) return;

            _stateTimer += deltaTime;

            switch (CurrentState)
            {
                case AbilityCastState.Casting:
                    CastProgress = _totalStateTime > 0
                        ? _stateTimer / _totalStateTime
                        : 1f;
                    if (_stateTimer >= _totalStateTime)
                        ExecuteAndTransition();
                    break;

                case AbilityCastState.Recovering:
                    if (_stateTimer >= _totalStateTime)
                    {
                        SetState(AbilityCastState.Idle);
                        CastProgress = 0;
                        _currentAbility = null;
                        _currentContext = null;
                    }
                    break;
            }
        }

        public bool TryInterrupt(string reason = "Interrupted")
        {
            if (CurrentState != AbilityCastState.Casting) return false;
            if (_currentAbility?.Data != null && !_currentAbility.Data.Interruptible)
                return false;

            var ability = _currentAbility;
            SetState(AbilityCastState.Idle);
            CastProgress = 0;
            _currentAbility = null;
            _currentContext = null;
            OnInterrupted?.Invoke(new AbilityInterruptedArgs(ability, reason));
            return true;
        }

        public void Reset()
        {
            CurrentState = AbilityCastState.Idle;
            CastProgress = 0;
            _stateTimer = 0;
            _totalStateTime = 0;
            _currentAbility = null;
            _currentContext = null;
        }

        private void ExecuteAndTransition()
        {
            SetState(AbilityCastState.Executing);

            int count = 0;
            var effects = _currentAbility.Data.Effects;
            for (int i = 0; i < effects.Count; i++)
            {
                if (_resolver.ResolveEffect(effects[i], _currentContext))
                    count++;
            }

            _currentAbility.StartCooldown();
            OnExecuted?.Invoke(new AbilityExecutedArgs(_currentAbility, _currentContext, count));

            if (_currentAbility.Data.RecoveryTime > 0)
            {
                _totalStateTime = _currentAbility.Data.RecoveryTime;
                _stateTimer = 0;
                SetState(AbilityCastState.Recovering);
            }
            else
            {
                SetState(AbilityCastState.Idle);
                CastProgress = 0;
                _currentAbility = null;
                _currentContext = null;
            }
        }

        private void SetState(AbilityCastState newState)
        {
            var prev = CurrentState;
            CurrentState = newState;
            OnStateChanged?.Invoke(new AbilityStateChangedArgs(
                _currentAbility, prev, newState, _currentContext));
        }
    }
}
