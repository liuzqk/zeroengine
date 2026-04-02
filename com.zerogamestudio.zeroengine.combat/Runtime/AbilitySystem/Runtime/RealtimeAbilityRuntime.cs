using System;
using System.Collections;
using UnityEngine;

namespace ZeroEngine.AbilitySystem
{
    /// <summary>
    /// 实时执行模式 — 基于 MonoBehaviour 协程
    /// 支持前摇(Casting)、执行(Executing)、后摇(Recovering)三阶段
    /// 适用于 POB/LLS 等实时战斗
    /// </summary>
    public class RealtimeAbilityRuntime : IAbilityRuntime
    {
        private readonly MonoBehaviour _host;
        private readonly IEffectResolver _resolver;
        private Coroutine _castCoroutine;
        private IAbilityContext _currentContext;

        public AbilityCastState CurrentState { get; private set; } = AbilityCastState.Idle;
        public float CastProgress { get; private set; }

        public event Action<AbilityStateChangedArgs> OnStateChanged;
        public event Action<AbilityInterruptedArgs> OnInterrupted;
        public event Action<AbilityExecutedArgs> OnExecuted;

        public RealtimeAbilityRuntime(MonoBehaviour host, IEffectResolver resolver)
        {
            _host = host;
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

            _currentContext = context;
            if (_castCoroutine != null) _host.StopCoroutine(_castCoroutine);
            _castCoroutine = _host.StartCoroutine(CastCoroutine(ability, context));
            return true;
        }

        public bool TryInterrupt(string reason = "Interrupted")
        {
            if (CurrentState != AbilityCastState.Casting) return false;
            if (_currentContext?.Ability?.Data != null && !_currentContext.Ability.Data.Interruptible)
                return false;

            if (_castCoroutine != null)
            {
                _host.StopCoroutine(_castCoroutine);
                _castCoroutine = null;
            }

            var ability = _currentContext?.Ability;
            SetState(AbilityCastState.Idle);
            CastProgress = 0;
            _currentContext = null;
            OnInterrupted?.Invoke(new AbilityInterruptedArgs(ability, reason));
            return true;
        }

        public void Update(float deltaTime)
        {
            // 实时模式由协程驱动，外部Update无需额外操作
        }

        public void Reset()
        {
            if (_castCoroutine != null)
            {
                _host.StopCoroutine(_castCoroutine);
                _castCoroutine = null;
            }
            CurrentState = AbilityCastState.Idle;
            CastProgress = 0;
            _currentContext = null;
        }

        private IEnumerator CastCoroutine(AbilityInstance ability, IAbilityContext context)
        {
            var data = ability.Data;

            // 前摇
            if (data.CastTime > 0)
            {
                SetState(AbilityCastState.Casting);
                CastProgress = 0;
                float elapsed = 0;
                while (elapsed < data.CastTime)
                {
                    elapsed += Time.deltaTime;
                    CastProgress = elapsed / data.CastTime;
                    yield return null;
                }
            }

            // 执行
            SetState(AbilityCastState.Executing);
            int count = ExecuteEffects(ability, context);
            ability.StartCooldown();
            OnExecuted?.Invoke(new AbilityExecutedArgs(ability, context, count));

            // 后摇
            if (data.RecoveryTime > 0)
            {
                SetState(AbilityCastState.Recovering);
                yield return new WaitForSeconds(data.RecoveryTime);
            }

            SetState(AbilityCastState.Idle);
            CastProgress = 0;
            _currentContext = null;
            _castCoroutine = null;
        }

        private int ExecuteEffects(AbilityInstance ability, IAbilityContext context)
        {
            int count = 0;
            var effects = ability.Data.Effects;
            for (int i = 0; i < effects.Count; i++)
            {
                if (_resolver.ResolveEffect(effects[i], context))
                    count++;
            }
            return count;
        }

        private void SetState(AbilityCastState newState)
        {
            var prev = CurrentState;
            CurrentState = newState;
            OnStateChanged?.Invoke(new AbilityStateChangedArgs(
                _currentContext?.Ability, prev, newState, _currentContext));
        }
    }
}
