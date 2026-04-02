using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZeroEngine.AbilitySystem
{
    /// <summary>
    /// 技能处理器 — 管理技能实例、冷却和施放
    /// 
    /// 通过 Initialize() 注入执行策略和效果解析器:
    ///   handler.Initialize(myResolver);                          // 实时模式(默认)
    ///   handler.Initialize(myResolver, new TickAbilityRuntime(myResolver)); // Tick模式
    /// 
    /// 不调用 Initialize() 则使用默认 Realtime + DebugResolver
    /// </summary>
    public class AbilityHandler : MonoBehaviour, IAbilitySource
    {
        [SerializeField] private List<AbilityInstance> _abilities = new List<AbilityInstance>();

        private IAbilityRuntime _runtime;
        private IEffectResolver _resolver;
        private bool _initialized;

        // ================ Properties ================

        public AbilityCastState CurrentState => _runtime?.CurrentState ?? AbilityCastState.Idle;
        public float CastProgress => _runtime?.CastProgress ?? 0f;
        public Transform Transform => transform;
        public IReadOnlyList<AbilityInstance> Abilities => _abilities;
        public AbilityInstance CurrentCastingAbility { get; private set; }

        // ================ Events ================

        public event Action<AbilityStateChangedArgs> OnAbilityStateChanged;
        public event Action<AbilityInterruptedArgs> OnAbilityInterrupted;
        public event Action<AbilityExecutedArgs> OnAbilityExecuted;

        // ================ Initialization ================

        /// <summary>
        /// 初始化（推荐在 Awake/Start 中调用）
        /// 不传参数 = 默认实时模式 + Debug解析器
        /// </summary>
        public void Initialize(IEffectResolver resolver = null, IAbilityRuntime runtime = null)
        {
            _resolver = resolver ?? new DebugEffectResolver();
            _runtime = runtime ?? new RealtimeAbilityRuntime(this, _resolver);
            SubscribeRuntimeEvents();
            _initialized = true;
        }

        private void Awake()
        {
            if (!_initialized) Initialize();
        }

        private void SubscribeRuntimeEvents()
        {
            if (_runtime is RealtimeAbilityRuntime realtime)
            {
                realtime.OnStateChanged += HandleStateChanged;
                realtime.OnInterrupted += HandleInterrupted;
                realtime.OnExecuted += HandleExecuted;
            }
            else if (_runtime is TickAbilityRuntime tick)
            {
                tick.OnStateChanged += HandleStateChanged;
                tick.OnInterrupted += HandleInterrupted;
                tick.OnExecuted += HandleExecuted;
            }
        }

        private void HandleStateChanged(AbilityStateChangedArgs args)
        {
            CurrentCastingAbility = args.NewState == AbilityCastState.Idle ? null : args.Ability;
            OnAbilityStateChanged?.Invoke(args);
        }

        private void HandleInterrupted(AbilityInterruptedArgs args)
        {
            CurrentCastingAbility = null;
            OnAbilityInterrupted?.Invoke(args);
        }

        private void HandleExecuted(AbilityExecutedArgs args)
        {
            OnAbilityExecuted?.Invoke(args);
        }

        // ================ Update ================

        private void Update()
        {
            float dt = Time.deltaTime;

            // 更新冷却
            int count = _abilities.Count;
            for (int i = 0; i < count; i++)
            {
                _abilities[i].UpdateCooldown(dt);
            }

            // 更新运行时（实时模式此处由协程驱动，tick模式需要外部调 UpdateTick）
            _runtime?.Update(dt);
        }

        /// <summary>
        /// Tick 模式专用 — 用外部 tick 间隔驱动而非 Time.deltaTime
        /// 适用于自走棋等 tick-based 战斗
        /// </summary>
        public void UpdateTick(float tickDeltaTime)
        {
            int count = _abilities.Count;
            for (int i = 0; i < count; i++)
            {
                _abilities[i].UpdateCooldown(tickDeltaTime);
            }

            _runtime?.Update(tickDeltaTime);
        }

        // ================ Ability Management ================

        public AbilityInstance AddAbility(AbilityDataSO data, int level = 1)
        {
            var instance = new AbilityInstance(data, level);
            _abilities.Add(instance);
            return instance;
        }

        public bool RemoveAbility(AbilityDataSO data)
        {
            int count = _abilities.Count;
            for (int i = count - 1; i >= 0; i--)
            {
                if (_abilities[i].Data == data)
                {
                    _abilities.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        public AbilityInstance GetAbility(AbilityDataSO data)
        {
            int count = _abilities.Count;
            for (int i = 0; i < count; i++)
            {
                if (_abilities[i].Data == data)
                    return _abilities[i];
            }
            return null;
        }

        public AbilityInstance GetAbility(int index)
        {
            if (index < 0 || index >= _abilities.Count) return null;
            return _abilities[index];
        }

        // ================ Casting ================

        public bool CanCast(AbilityInstance ability)
        {
            if (ability == null || ability.Data == null) return false;
            if (ability.IsOnCooldown) return false;
            if (CurrentState != AbilityCastState.Idle) return false;
            return true;
        }

        public bool TryCastAbility(AbilityDataSO abilityData, IAbilityTarget target = null)
        {
            var instance = GetAbility(abilityData);
            if (instance == null)
            {
                Debug.LogWarning($"[AbilityHandler] Ability {abilityData.AbilityName} not found.");
                return false;
            }
            return TryCastAbility(instance, target);
        }

        public bool TryCastAbility(AbilityInstance ability, IAbilityTarget target = null)
        {
            if (!CanCast(ability)) return false;

            var ctx = AbilityContext.Get(this, target, ability);
            return _runtime.TryCast(ability, ctx);
        }

        /// <summary>
        /// 带自定义上下文的施放（高级用法）
        /// 用于需要传递 UserData 的场景
        /// </summary>
        public bool TryCastAbility(AbilityInstance ability, IAbilityContext context)
        {
            if (!CanCast(ability)) return false;
            return _runtime.TryCast(ability, context);
        }

        public bool TryInterrupt(string reason = "Interrupted")
            => _runtime?.TryInterrupt(reason) ?? false;

        // ================ Level & Cooldown ================

        public bool TryLevelUpAbility(AbilityDataSO data)
        {
            var instance = GetAbility(data);
            if (instance == null) return false;
            return instance.TryLevelUp();
        }

        public void ResetCooldown(AbilityDataSO data)
        {
            GetAbility(data)?.ResetCooldown();
        }

        public void ResetAllCooldowns()
        {
            for (int i = 0; i < _abilities.Count; i++)
            {
                _abilities[i].ResetCooldown();
            }
        }

        // ================ Cleanup ================

        private void OnDestroy()
        {
            _runtime?.Reset();
        }
    }
}
