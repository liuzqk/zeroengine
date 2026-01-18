using System;
using UnityEngine;
using ZeroEngine.AutoBattle.Grid;
using ZeroEngine.AutoBattle.Skill;
using ZeroEngine.AutoBattle.AI;

namespace ZeroEngine.AutoBattle.Battle
{
    /// <summary>
    /// 战斗单位基类，实现 IBattleUnit 接口
    /// </summary>
    public abstract class BattleUnitBase : IBattleUnit
    {
        /// <summary>
        /// 单位唯一ID
        /// </summary>
        public string UnitId { get; }

        /// <summary>
        /// 单位所属阵营
        /// </summary>
        public BattleTeam Team { get; }

        /// <summary>
        /// 当前所在格子
        /// </summary>
        public GridCell CurrentCell { get; private set; }

        /// <summary>
        /// 单位是否存活
        /// </summary>
        public bool IsAlive => CurrentHealth > 0;

        /// <summary>
        /// 当前生命值
        /// </summary>
        public float CurrentHealth { get; protected set; }

        /// <summary>
        /// 最大生命值
        /// </summary>
        public float MaxHealth { get; protected set; }

        /// <summary>
        /// 攻击力
        /// </summary>
        public float Attack { get; protected set; }

        /// <summary>
        /// 防御力
        /// </summary>
        public float Defense { get; protected set; }

        /// <summary>
        /// 攻击速度（每秒攻击次数）
        /// </summary>
        public float AttackSpeed { get; protected set; } = 1f;

        /// <summary>
        /// 攻击范围（格子数）
        /// </summary>
        public int AttackRange { get; protected set; } = 1;

        /// <summary>
        /// 技能槽位
        /// </summary>
        public SkillSlotManager SkillSlots { get; }

        /// <summary>
        /// AI 配置
        /// </summary>
        public UnitAIConfig AIConfig { get; set; }

        /// <summary>
        /// 攻击冷却计时器
        /// </summary>
        protected float _attackCooldown;

        // 事件
        public event Action<float, float> OnHealthChanged;
        public event Action OnDeath;

        protected BattleUnitBase(string unitId, BattleTeam team, int skillSlotCount = 4)
        {
            UnitId = unitId;
            Team = team;
            SkillSlots = new SkillSlotManager(skillSlotCount);
            AIConfig = new UnitAIConfig();
        }

        /// <summary>
        /// 设置单位所在格子
        /// </summary>
        public void SetCell(GridCell cell)
        {
            CurrentCell = cell;
        }

        /// <summary>
        /// 初始化属性
        /// </summary>
        public virtual void Initialize(float maxHealth, float attack, float defense)
        {
            MaxHealth = maxHealth;
            CurrentHealth = maxHealth;
            Attack = attack;
            Defense = defense;
        }

        /// <summary>
        /// 受到伤害
        /// </summary>
        public virtual void TakeDamage(float damage, IBattleUnit attacker)
        {
            // 计算实际伤害（考虑防御）
            float actualDamage = Mathf.Max(1f, damage - Defense * 0.5f);
            float oldHealth = CurrentHealth;
            CurrentHealth = Mathf.Max(0f, CurrentHealth - actualDamage);

            OnHealthChanged?.Invoke(oldHealth, CurrentHealth);

            if (CurrentHealth <= 0)
            {
                Die();
            }
        }

        /// <summary>
        /// 恢复生命
        /// </summary>
        public virtual void Heal(float amount)
        {
            float oldHealth = CurrentHealth;
            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
            OnHealthChanged?.Invoke(oldHealth, CurrentHealth);
        }

        /// <summary>
        /// 死亡
        /// </summary>
        protected virtual void Die()
        {
            OnDeath?.Invoke();
        }

        /// <summary>
        /// 战斗更新
        /// </summary>
        public virtual void BattleTick(float deltaTime, AutoBattleManager battleManager)
        {
            if (!IsAlive) return;

            // 更新攻击冷却
            if (_attackCooldown > 0)
            {
                _attackCooldown -= deltaTime;
            }

            // 更新技能冷却
            SkillSlots.UpdateCooldowns(deltaTime);

            // AI 决策
            ProcessAI(deltaTime, battleManager);
        }

        /// <summary>
        /// 处理 AI 决策
        /// </summary>
        protected virtual void ProcessAI(float deltaTime, AutoBattleManager battleManager)
        {
            // 获取目标
            var target = FindTarget(battleManager);
            if (target == null) return;

            // 尝试使用技能
            var availableSkill = SkillSlots.GetAvailableSkill(this, target);
            if (availableSkill != null)
            {
                UseSkill(availableSkill, target, battleManager);
                return;
            }

            // 普通攻击
            if (_attackCooldown <= 0 && IsInRange(target))
            {
                PerformAttack(target);
                _attackCooldown = 1f / AttackSpeed;
            }
        }

        /// <summary>
        /// 寻找目标
        /// </summary>
        protected virtual IBattleUnit FindTarget(AutoBattleManager battleManager)
        {
            var enemies = Team == BattleTeam.Player
                ? battleManager.GetAliveEnemyUnits()
                : battleManager.GetAlivePlayerUnits();

            if (enemies.Count == 0) return null;

            // 根据 AI 配置选择目标
            return AIConfig.TargetPriority switch
            {
                TargetPriority.Nearest => FindNearestTarget(enemies),
                TargetPriority.LowestHealth => FindLowestHealthTarget(enemies),
                TargetPriority.HighestHealth => FindHighestHealthTarget(enemies),
                TargetPriority.BackRow => FindBackRowTarget(enemies),
                TargetPriority.FrontRow => FindFrontRowTarget(enemies),
                _ => enemies[0]
            };
        }

        private IBattleUnit FindNearestTarget(System.Collections.Generic.IReadOnlyList<IBattleUnit> enemies)
        {
            IBattleUnit nearest = null;
            int minDistance = int.MaxValue;

            foreach (var enemy in enemies)
            {
                if (enemy.CurrentCell == null || CurrentCell == null) continue;
                int distance = GridBoard.GetManhattanDistance(CurrentCell, enemy.CurrentCell);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = enemy;
                }
            }

            return nearest ?? enemies[0];
        }

        private IBattleUnit FindLowestHealthTarget(System.Collections.Generic.IReadOnlyList<IBattleUnit> enemies)
        {
            IBattleUnit lowest = null;
            float lowestHealth = float.MaxValue;

            foreach (var enemy in enemies)
            {
                if (enemy is BattleUnitBase unit && unit.CurrentHealth < lowestHealth)
                {
                    lowestHealth = unit.CurrentHealth;
                    lowest = enemy;
                }
            }

            return lowest ?? enemies[0];
        }

        private IBattleUnit FindHighestHealthTarget(System.Collections.Generic.IReadOnlyList<IBattleUnit> enemies)
        {
            IBattleUnit highest = null;
            float highestHealth = 0f;

            foreach (var enemy in enemies)
            {
                if (enemy is BattleUnitBase unit && unit.CurrentHealth > highestHealth)
                {
                    highestHealth = unit.CurrentHealth;
                    highest = enemy;
                }
            }

            return highest ?? enemies[0];
        }

        private IBattleUnit FindBackRowTarget(System.Collections.Generic.IReadOnlyList<IBattleUnit> enemies)
        {
            IBattleUnit backRow = null;
            int maxColumn = -1;

            foreach (var enemy in enemies)
            {
                if (enemy.CurrentCell != null && enemy.CurrentCell.X > maxColumn)
                {
                    maxColumn = enemy.CurrentCell.X;
                    backRow = enemy;
                }
            }

            return backRow ?? enemies[0];
        }

        private IBattleUnit FindFrontRowTarget(System.Collections.Generic.IReadOnlyList<IBattleUnit> enemies)
        {
            IBattleUnit frontRow = null;
            int minColumn = int.MaxValue;

            foreach (var enemy in enemies)
            {
                if (enemy.CurrentCell != null && enemy.CurrentCell.X < minColumn)
                {
                    minColumn = enemy.CurrentCell.X;
                    frontRow = enemy;
                }
            }

            return frontRow ?? enemies[0];
        }

        /// <summary>
        /// 检查目标是否在攻击范围内
        /// </summary>
        protected bool IsInRange(IBattleUnit target)
        {
            if (CurrentCell == null || target.CurrentCell == null)
                return false;

            // 计算跨棋盘距离（简化：使用列差）
            int distance = Mathf.Abs(CurrentCell.X - target.CurrentCell.X);
            return distance <= AttackRange;
        }

        /// <summary>
        /// 执行普通攻击
        /// </summary>
        protected virtual void PerformAttack(IBattleUnit target)
        {
            if (target is BattleUnitBase targetUnit)
            {
                targetUnit.TakeDamage(Attack, this);
            }
        }

        /// <summary>
        /// 使用技能
        /// </summary>
        protected virtual void UseSkill(SkillData skill, IBattleUnit target, AutoBattleManager battleManager)
        {
            skill.Execute(this, target, battleManager);
            SkillSlots.StartCooldown(skill);
        }
    }
}
