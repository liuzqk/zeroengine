using System;
using UnityEngine;
using UnityEngine.AI;

namespace ZeroEngine.AI.NPCSchedule
{
    /// <summary>
    /// 移动到位置行动
    /// </summary>
    [Serializable]
    public class MoveToScheduleAction : ScheduleAction
    {
        [SerializeField] private float _stoppingDistance = 0.5f;
        [SerializeField] private float _timeout = 60f;

        private NavMeshAgent _navAgent;
        private Vector3 _targetPosition;
        private float _elapsed;
        private bool _hasArrived;

        public MoveToScheduleAction()
        {
            _actionName = "MoveTo";
            _description = "Move to schedule location";
        }

        public override void Start(AIContext context, ScheduleEntry entry)
        {
            State = ScheduleActionState.Starting;
            _elapsed = 0f;
            _hasArrived = false;

            _targetPosition = entry?.TargetPosition ?? context.TargetPosition;
            _navAgent = context.GetCachedComponent<NavMeshAgent>();

            if (_navAgent != null)
            {
                _navAgent.SetDestination(_targetPosition);
                _navAgent.stoppingDistance = _stoppingDistance;
            }

            context.IsMoving = true;
            State = ScheduleActionState.Running;
        }

        public override bool Update(AIContext context, float deltaTime)
        {
            if (_hasArrived) return false; // 已到达，继续待在原地

            _elapsed += deltaTime;

            // 超时检查
            if (_timeout > 0f && _elapsed >= _timeout)
            {
                _hasArrived = true;
                context.IsMoving = false;
                return false;
            }

            // 检查是否到达
            float distance = Vector3.Distance(context.Transform.position, _targetPosition);
            if (distance <= _stoppingDistance)
            {
                _hasArrived = true;
                context.IsMoving = false;
            }

            return false; // 永不自动完成，等待日程时间结束
        }

        public override void End(AIContext context, bool interrupted)
        {
            context.IsMoving = false;
            _navAgent?.ResetPath();
            State = interrupted ? ScheduleActionState.Interrupted : ScheduleActionState.Completed;
        }

        public override void Reset()
        {
            base.Reset();
            _elapsed = 0f;
            _hasArrived = false;
        }
    }

    /// <summary>
    /// 睡眠行动
    /// </summary>
    [Serializable]
    public class SleepScheduleAction : ScheduleAction
    {
        [SerializeField] private string _sleepAnimationState = "Sleep";
        [SerializeField] private bool _recoverHealth = true;
        [SerializeField] private float _healthRecoveryRate = 5f; // 每小时恢复百分比

        private Animator _animator;
        private float _sleepStartTime;

        public SleepScheduleAction()
        {
            _actionName = "Sleep";
            _description = "Sleep and recover";
            _isInterruptible = false; // 睡眠不可中断
        }

        public override void Start(AIContext context, ScheduleEntry entry)
        {
            State = ScheduleActionState.Running;
            _sleepStartTime = Time.time;

            _animator = context.GetCachedComponent<Animator>();
            if (_animator != null && !string.IsNullOrEmpty(_sleepAnimationState))
            {
                _animator.CrossFade(_sleepAnimationState, 0.3f);
            }

            // 设置状态
            context.Blackboard?.Set("IsSleeping", true);
        }

        public override bool Update(AIContext context, float deltaTime)
        {
            // 恢复生命值
            if (_recoverHealth)
            {
                float recoveryPerSecond = _healthRecoveryRate / 3600f; // 转换为每秒
                context.Blackboard?.IncrementFloat(BlackboardKeys.HealthPercent, recoveryPerSecond * deltaTime);

#if ZEROENGINE_COMBAT
                var health = context.GetCachedComponent<Combat.HealthComponent>();
                if (health != null)
                {
                    health.Heal(recoveryPerSecond * deltaTime * health.MaxHealth);
                }
#endif
            }

            return false; // 睡眠永不自动完成
        }

        public override void End(AIContext context, bool interrupted)
        {
            context.Blackboard?.Set("IsSleeping", false);
            State = interrupted ? ScheduleActionState.Interrupted : ScheduleActionState.Completed;
        }
    }

    /// <summary>
    /// 工作行动
    /// </summary>
    [Serializable]
    public class WorkScheduleAction : ScheduleAction
    {
        [SerializeField] private string _workAnimationState = "Work";
        [SerializeField] private string _workType = "";
        [SerializeField] private float _productivityRate = 1f;

        private Animator _animator;

        public WorkScheduleAction()
        {
            _actionName = "Work";
            _description = "Perform work activity";
        }

        public override void Start(AIContext context, ScheduleEntry entry)
        {
            State = ScheduleActionState.Running;

            _animator = context.GetCachedComponent<Animator>();
            if (_animator != null && !string.IsNullOrEmpty(_workAnimationState))
            {
                _animator.CrossFade(_workAnimationState, 0.3f);
            }

            context.Blackboard?.Set("IsWorking", true);
            context.Blackboard?.Set("WorkType", _workType);
        }

        public override bool Update(AIContext context, float deltaTime)
        {
            // 积累工作进度
            float progress = context.Blackboard?.GetFloat("WorkProgress", 0f) ?? 0f;
            progress += _productivityRate * deltaTime;
            context.Blackboard?.Set("WorkProgress", progress);

            return false;
        }

        public override void End(AIContext context, bool interrupted)
        {
            context.Blackboard?.Set("IsWorking", false);
            State = interrupted ? ScheduleActionState.Interrupted : ScheduleActionState.Completed;
        }
    }

    /// <summary>
    /// 进食行动
    /// </summary>
    [Serializable]
    public class EatScheduleAction : ScheduleAction
    {
        [SerializeField] private string _eatAnimationState = "Eat";
        [SerializeField] private float _eatDuration = 30f; // 秒
        [SerializeField] private float _hungerRecovery = 50f;

        private Animator _animator;
        private float _elapsed;
        private bool _hasEaten;

        public EatScheduleAction()
        {
            _actionName = "Eat";
            _description = "Eat a meal";
        }

        public override void Start(AIContext context, ScheduleEntry entry)
        {
            State = ScheduleActionState.Running;
            _elapsed = 0f;
            _hasEaten = false;

            _animator = context.GetCachedComponent<Animator>();
            if (_animator != null && !string.IsNullOrEmpty(_eatAnimationState))
            {
                _animator.CrossFade(_eatAnimationState, 0.3f);
            }

            context.Blackboard?.Set("IsEating", true);
        }

        public override bool Update(AIContext context, float deltaTime)
        {
            if (_hasEaten) return false;

            _elapsed += deltaTime;

            if (_elapsed >= _eatDuration)
            {
                _hasEaten = true;

                // 恢复饥饿值
                float hunger = context.Blackboard?.GetFloat("Hunger", 100f) ?? 100f;
                hunger = Mathf.Clamp(hunger + _hungerRecovery, 0f, 100f);
                context.Blackboard?.Set("Hunger", hunger);
            }

            return false;
        }

        public override void End(AIContext context, bool interrupted)
        {
            context.Blackboard?.Set("IsEating", false);
            State = interrupted ? ScheduleActionState.Interrupted : ScheduleActionState.Completed;
        }

        public override void Reset()
        {
            base.Reset();
            _elapsed = 0f;
            _hasEaten = false;
        }
    }

    /// <summary>
    /// 社交行动
    /// </summary>
    [Serializable]
    public class SocializeScheduleAction : ScheduleAction
    {
        [SerializeField] private string _socializeAnimationState = "Talk";
        [SerializeField] private float _socialRadius = 5f;
        [SerializeField] private LayerMask _npcLayer;

        private Animator _animator;
        private Transform _socialPartner;

        public SocializeScheduleAction()
        {
            _actionName = "Socialize";
            _description = "Socialize with nearby NPCs";
        }

        public override void Start(AIContext context, ScheduleEntry entry)
        {
            State = ScheduleActionState.Running;

            // 寻找附近的 NPC
            FindSocialPartner(context);

            _animator = context.GetCachedComponent<Animator>();
            if (_animator != null && !string.IsNullOrEmpty(_socializeAnimationState))
            {
                _animator.CrossFade(_socializeAnimationState, 0.3f);
            }

            context.Blackboard?.Set("IsSocializing", true);
        }

        private void FindSocialPartner(AIContext context)
        {
            var colliders = Physics.OverlapSphere(
                context.Transform.position,
                _socialRadius,
                _npcLayer
            );

            foreach (var col in colliders)
            {
                if (col.gameObject != context.Owner)
                {
                    _socialPartner = col.transform;
                    break;
                }
            }
        }

        public override bool Update(AIContext context, float deltaTime)
        {
            // 面向社交对象
            if (_socialPartner != null)
            {
                Vector3 direction = (_socialPartner.position - context.Transform.position).normalized;
                direction.y = 0;
                if (direction != Vector3.zero)
                {
                    context.Transform.rotation = Quaternion.Slerp(
                        context.Transform.rotation,
                        Quaternion.LookRotation(direction),
                        deltaTime * 3f
                    );
                }
            }

            return false;
        }

        public override void End(AIContext context, bool interrupted)
        {
            context.Blackboard?.Set("IsSocializing", false);
            _socialPartner = null;
            State = interrupted ? ScheduleActionState.Interrupted : ScheduleActionState.Completed;
        }
    }

    /// <summary>
    /// 商店行动 - 经营商店
    /// </summary>
    [Serializable]
    public class ShopkeeperScheduleAction : ScheduleAction
    {
        [SerializeField] private string _shopId = "";
        [SerializeField] private string _idleAnimationState = "ShopIdle";

        private Animator _animator;

        public ShopkeeperScheduleAction()
        {
            _actionName = "Shopkeeper";
            _description = "Run the shop";
        }

        public override void Start(AIContext context, ScheduleEntry entry)
        {
            State = ScheduleActionState.Running;

            _animator = context.GetCachedComponent<Animator>();
            if (_animator != null && !string.IsNullOrEmpty(_idleAnimationState))
            {
                _animator.CrossFade(_idleAnimationState, 0.3f);
            }

            context.Blackboard?.Set("IsShopOpen", true);
            context.Blackboard?.Set("CurrentShopId", _shopId);
        }

        public override bool Update(AIContext context, float deltaTime)
        {
            // 商店逻辑由其他系统处理
            return false;
        }

        public override void End(AIContext context, bool interrupted)
        {
            context.Blackboard?.Set("IsShopOpen", false);
            State = interrupted ? ScheduleActionState.Interrupted : ScheduleActionState.Completed;
        }
    }

    /// <summary>
    /// 巡逻行动
    /// </summary>
    [Serializable]
    public class PatrolScheduleAction : ScheduleAction
    {
        [SerializeField] private Transform[] _patrolPoints;
        [SerializeField] private float _waitTime = 2f;
        [SerializeField] private float _stoppingDistance = 0.5f;

        private NavMeshAgent _navAgent;
        private int _currentIndex;
        private bool _isWaiting;
        private float _waitTimer;

        public PatrolScheduleAction()
        {
            _actionName = "Patrol";
            _description = "Patrol between points";
        }

        public override void Start(AIContext context, ScheduleEntry entry)
        {
            State = ScheduleActionState.Running;
            _currentIndex = 0;
            _isWaiting = false;

            _navAgent = context.GetCachedComponent<NavMeshAgent>();

            if (_patrolPoints != null && _patrolPoints.Length > 0)
            {
                MoveToCurrentPoint();
            }

            context.IsMoving = true;
        }

        public override bool Update(AIContext context, float deltaTime)
        {
            if (_patrolPoints == null || _patrolPoints.Length == 0)
            {
                return false;
            }

            if (_isWaiting)
            {
                _waitTimer -= deltaTime;
                if (_waitTimer <= 0f)
                {
                    _isWaiting = false;
                    NextPoint();
                    MoveToCurrentPoint();
                }
                return false;
            }

            // 检查是否到达
            if (_navAgent != null && !_navAgent.pathPending)
            {
                if (_navAgent.remainingDistance <= _stoppingDistance)
                {
                    _isWaiting = true;
                    _waitTimer = _waitTime;
                }
            }

            return false;
        }

        private void MoveToCurrentPoint()
        {
            if (_navAgent != null && _patrolPoints != null && _currentIndex < _patrolPoints.Length)
            {
                var point = _patrolPoints[_currentIndex];
                if (point != null)
                {
                    _navAgent.SetDestination(point.position);
                }
            }
        }

        private void NextPoint()
        {
            _currentIndex = (_currentIndex + 1) % _patrolPoints.Length;
        }

        public override void End(AIContext context, bool interrupted)
        {
            context.IsMoving = false;
            _navAgent?.ResetPath();
            State = interrupted ? ScheduleActionState.Interrupted : ScheduleActionState.Completed;
        }

        public override void Reset()
        {
            base.Reset();
            _currentIndex = 0;
            _isWaiting = false;
        }

        /// <summary>
        /// 设置巡逻点
        /// </summary>
        public void SetPatrolPoints(Transform[] points)
        {
            _patrolPoints = points;
        }
    }
}
