# Combat System (v1.15.0)

战斗系统模块，提供完整的战斗单位管理、伤害计算、目标选择和生命值系统。

## 目录结构

```
Combat/
├── Core/
│   ├── ICombatant.cs          # 战斗单位接口
│   ├── CombatManager.cs       # 战斗管理器
│   ├── CombatContext.cs       # 战斗上下文
│   ├── CombatEvents.cs        # 战斗事件定义
│   └── CombatTeam.cs          # 队伍/阵营管理
├── Damage/
│   ├── DamageType.cs          # 伤害类型枚举
│   ├── DamageData.cs          # 伤害数据结构
│   ├── DamageResult.cs        # 伤害结果
│   ├── DamageCalculator.cs    # 伤害计算器
│   ├── DamageModifier.cs      # 伤害修正器
│   └── IDamageProcessor.cs    # 伤害处理器接口
├── Targeting/
│   ├── ITargetable.cs         # 可选中接口
│   ├── TargetSelector.cs      # 目标选择器
│   ├── TargetFilter.cs        # 目标过滤器
│   ├── TargetPriority.cs      # 优先级策略
│   └── RangeChecker.cs        # 范围检测工具
└── Health/
    ├── IHealth.cs             # 生命值接口
    ├── HealthComponent.cs     # 生命值组件
    ├── DamageReceiver.cs      # 伤害接收器
    └── DeathHandler.cs        # 死亡处理器
```

## 快速开始

### 1. 创建战斗单位

```csharp
using ZeroEngine.Combat;

public class Enemy : MonoBehaviour, ICombatant
{
    [SerializeField] private string _id;
    [SerializeField] private int _teamId = 1; // 敌方队伍

    private HealthComponent _health;

    public string CombatantId => _id;
    public string DisplayName => gameObject.name;
    public int TeamId => _teamId;
    public bool IsAlive => _health.IsAlive;
    public bool IsTargetable => _health.IsAlive;
    public GameObject GameObject => gameObject;
    public Transform Transform => transform;

    void Awake()
    {
        _health = GetComponent<HealthComponent>();
        _id = $"enemy_{GetInstanceID()}";
    }

    void Start()
    {
        CombatManager.Instance.RegisterCombatant(this);
    }

    void OnDestroy()
    {
        CombatManager.Instance?.UnregisterCombatant(this);
    }

    public Vector3 GetCombatPosition() => transform.position;

    public DamageResult TakeDamage(DamageData damage)
    {
        return _health.TakeDamage(damage);
    }

    public float ReceiveHeal(float amount, ICombatant source = null)
    {
        return _health.Heal(amount, source);
    }

    public void OnEnterCombat() { /* 进入战斗状态 */ }
    public void OnExitCombat() { /* 退出战斗状态 */ }
}
```

### 2. 造成伤害

```csharp
using ZeroEngine.Combat;

// 方式1: 直接使用 DamageData
var damage = DamageData.Physical(50f, attacker);
var result = target.TakeDamage(damage);

if (result.IsKill)
{
    Debug.Log($"击杀了 {target.DisplayName}!");
}

// 方式2: 通过 CombatManager
var result = CombatManager.Instance.DealDamage(
    DamageData.Magical(100f, attacker, DamageType.Fire),
    target,
    attackerStatGetter: stat => attacker.GetStat(stat),
    defenderStatGetter: stat => target.GetStat(stat)
);

// 方式3: 使用完整伤害数据
var damage = new DamageData
{
    BaseDamage = 100f,
    DamageType = DamageType.Physical | DamageType.Fire,
    SourceType = DamageSourceType.Ability,
    CritChance = 0.3f,
    CritMultiplier = 2.5f,
    Flags = DamageFlags.Lifesteal,
    Source = attacker
};
```

### 3. 目标选择

```csharp
using ZeroEngine.Combat;

// 简单选择：最近的敌人
var selector = new TargetSelector(new TargetSelectorConfig
{
    RelationType = TargetRelationType.Hostile,
    Strategy = TargetSelectionStrategy.Nearest,
    MaxRange = 10f,
    RequireLineOfSight = true
});
selector.SourceTeamId = player.TeamId;

var candidates = CombatManager.Instance.GetAllCombatants();
var target = selector.SelectTarget(candidates, player.transform.position);

// 选择多个目标
var targets = selector.SelectTargets(candidates, player.transform.position, maxCount: 5);

// 使用过滤器
var enemies = TargetFilter.FilterHostile(candidates, player.TeamId);
var inRange = TargetFilter.FilterInRange(enemies, player.transform.position, 10f);
var nearest = TargetPriority.GetNearest(inRange, player.transform.position);
```

### 4. 范围检测

```csharp
using ZeroEngine.Combat;

// 圆形范围
bool inRange = RangeChecker.IsInCircle(origin, radius, point);

// 扇形范围
bool inCone = RangeChecker.IsInCone(origin, direction, range, angle, point);

// 矩形范围
bool inRect = RangeChecker.IsInRect(center, size, rotation, point);

// 视线检测
bool hasLOS = RangeChecker.HasLineOfSight(origin, targetPos, obstacleMask);

// 范围形状配置
var shape = RangeShape.Cone(10f, 60f);
bool contains = shape.Contains(origin, forward, point);
```

### 5. 队伍关系

```csharp
using ZeroEngine.Combat;

// 设置队伍关系
CombatManager.Instance.SetTeamRelation(0, 1, TeamRelation.Hostile); // 玩家 vs 敌人
CombatManager.Instance.SetTeamRelation(0, 2, TeamRelation.Friendly); // 玩家 vs 友军
CombatManager.Instance.SetTeamRelation(1, 2, TeamRelation.Hostile); // 敌人 vs 友军

// 检查关系
bool isHostile = CombatManager.Instance.AreHostile(player, enemy);
bool isFriendly = CombatManager.Instance.AreFriendly(player, ally);
```

### 6. 生命值系统

```csharp
using ZeroEngine.Combat;

// HealthComponent 用法
var health = GetComponent<HealthComponent>();

// 监听事件
health.OnHealthChanged += args =>
{
    Debug.Log($"HP: {args.CurrentHealth}/{args.MaxHealth}");
};

health.OnDeath += args =>
{
    Debug.Log($"被 {args.Killer?.DisplayName ?? "未知"} 击杀");
};

// 治疗
float actualHeal = health.Heal(50f, healer);

// 百分比伤害/治疗
health.TakePercentDamage(0.1f); // 10% 最大生命值伤害
health.HealPercent(0.2f); // 20% 最大生命值治疗

// 复活
health.Revive(healthPercent: 0.5f); // 复活并恢复 50% HP
```

### 7. 伤害计算器配置

```csharp
using ZeroEngine.Combat;

// 配置伤害公式
var calculator = CombatManager.Instance.DamageCalculator;
calculator.FormulaConfig = new DamageFormulaConfig
{
    ArmorStatName = "Armor",
    MagicResistStatName = "MagicResist",
    CritChanceStatName = "CritChance",
    CritDamageStatName = "CritDamage",
    DodgeStatName = "DodgeChance",
    LifestealStatName = "Lifesteal",
    ArmorConstant = 100f,   // 护甲公式常数
    ResistanceConstant = 100f
};

// 注册自定义伤害处理器
public class ElementalDamageProcessor : DamageProcessorBase
{
    public override int Priority => 10;

    public override DamageData ProcessDamage(DamageData damage, ICombatant target, DamageCalculationContext context)
    {
        // 自定义元素伤害处理逻辑
        if (damage.HasDamageType(DamageType.Fire))
        {
            // 火焰伤害增幅
            context.DamageMultiplier *= 1.1f;
        }
        return damage;
    }
}

calculator.RegisterProcessor(new ElementalDamageProcessor());
```

### 8. 战斗上下文

```csharp
using ZeroEngine.Combat;

// 开始战斗
var context = CombatManager.Instance.StartCombat("boss_fight");
context.AddParticipant(player);
context.AddParticipant(boss);

// 监听战斗事件
context.OnParticipantJoined += combatant =>
{
    Debug.Log($"{combatant.DisplayName} 加入战斗");
};

// 记录统计
context.RecordDamageDealt(player, 100f, isCritical: true);
context.RecordKill(player);

// 获取统计
var stats = context.GetStatistics(player);
Debug.Log($"总伤害: {stats.TotalDamageDealt}, DPS: {stats.GetDPS(context.Duration)}");

// 结束战斗
CombatManager.Instance.EndCombat(context);
```

## 伤害类型

```csharp
[Flags]
public enum DamageType
{
    None = 0,
    Physical = 1 << 0,      // 物理
    Magical = 1 << 1,       // 魔法
    True = 1 << 2,          // 真实伤害
    Fire = 1 << 3,          // 火焰
    Ice = 1 << 4,           // 冰霜
    Lightning = 1 << 5,     // 雷电
    Poison = 1 << 6,        // 毒素
    Holy = 1 << 7,          // 神圣
    Dark = 1 << 8,          // 黑暗
    Nature = 1 << 9,        // 自然
    // ...
}

// 组合使用
var damage = DamageData.Create(100f, DamageType.Physical | DamageType.Fire);
```

## 伤害标记

```csharp
[Flags]
public enum DamageFlags
{
    None = 0,
    Critical = 1 << 0,        // 暴击
    IgnoreArmor = 1 << 1,     // 无视护甲
    IgnoreDodge = 1 << 2,     // 无视闪避
    IgnoreInvulnerable = 1 << 3, // 无视无敌
    Lifesteal = 1 << 4,       // 生命偷取
    CannotAbsorb = 1 << 5,    // 不可吸收
    Reflected = 1 << 6,       // 反射伤害
    OverTime = 1 << 7,        // 持续伤害
}
```

## 目标选择策略

| 策略 | 说明 |
|------|------|
| `Nearest` | 最近目标 |
| `Farthest` | 最远目标 |
| `LowestHealth` | 血量最低 |
| `HighestHealth` | 血量最高 |
| `HighestPriority` | 优先级最高 |
| `Random` | 随机选择 |
| `HighestThreat` | 威胁最高 |
| `Custom` | 自定义评分 |

## 事件监听

```csharp
// 全局伤害事件
CombatManager.Instance.OnDamageDealt += args =>
{
    Debug.Log($"{args.Source?.DisplayName} 对 {args.Target.DisplayName} 造成 {args.Result.FinalDamage} 伤害");
};

// 全局击杀事件
CombatManager.Instance.OnKillOccurred += args =>
{
    Debug.Log($"{args.Killer.DisplayName} 击杀了 {args.Victim.DisplayName}");
};

// 战斗单位注册事件
CombatManager.Instance.OnCombatantRegistered += combatant =>
{
    Debug.Log($"新战斗单位: {combatant.DisplayName}");
};
```

## 与其他系统集成

### StatSystem 集成

```csharp
// 使用 StatController 提供属性
var result = CombatManager.Instance.DealDamage(
    damage,
    target,
    attackerStatGetter: stat => attacker.StatController.GetStatValue(stat),
    defenderStatGetter: stat => target.StatController.GetStatValue(stat)
);
```

### BuffSystem 集成

```csharp
// Buff 修改伤害
public class DamageBuffProcessor : DamageProcessorBase
{
    public override int Priority => 5;

    public override DamageData ProcessDamage(DamageData damage, ICombatant target, DamageCalculationContext context)
    {
        // 获取攻击者增伤 Buff
        if (damage.Source is IBuffReceiver attacker)
        {
            float bonus = attacker.GetBuffValue("damage_bonus");
            context.DamageMultiplier *= (1f + bonus);
        }
        return damage;
    }
}
```

### AbilitySystem 集成

```csharp
// 技能造成伤害
public class FireballEffect : AbilityEffect
{
    public float BaseDamage = 100f;

    public override void Apply(AbilityContext context)
    {
        var damage = DamageData.Create(BaseDamage, DamageType.Magical | DamageType.Fire);
        damage.Source = context.Caster as ICombatant;
        damage.SourceType = DamageSourceType.Ability;

        foreach (var target in context.Targets)
        {
            if (target is ICombatant combatant)
            {
                combatant.TakeDamage(damage);
            }
        }
    }
}
```

## 性能优化

- 使用 `readonly struct` 事件参数避免 GC
- `TargetFilter` 使用 `yield return` 延迟求值
- `RangeChecker` 使用 `sqrMagnitude` 避免开方
- 物理检测使用 `NonAlloc` 方法
- `DamageCalculator` 复用 `DamageCalculationContext`

## API 参考

### CombatManager

| 方法 | 说明 |
|------|------|
| `RegisterCombatant(combatant)` | 注册战斗单位 |
| `UnregisterCombatant(combatant)` | 注销战斗单位 |
| `DealDamage(damage, target, ...)` | 造成伤害 |
| `Heal(target, amount, source)` | 治疗目标 |
| `StartCombat(contextId)` | 开始战斗 |
| `EndCombat(context)` | 结束战斗 |
| `AreHostile(a, b)` | 检查敌对关系 |
| `AreFriendly(a, b)` | 检查友好关系 |
| `GetHostileTargetsInRange(source, range)` | 获取范围内敌人 |

### HealthComponent

| 方法/属性 | 说明 |
|-----------|------|
| `CurrentHealth` | 当前生命值 |
| `MaxHealth` | 最大生命值 |
| `HealthPercent` | 生命值百分比 |
| `IsAlive` | 是否存活 |
| `IsInvulnerable` | 是否无敌 |
| `TakeDamage(damage)` | 受到伤害 |
| `Heal(amount, source)` | 接受治疗 |
| `Revive(percent)` | 复活 |

### TargetSelector

| 方法 | 说明 |
|------|------|
| `SelectTarget(candidates)` | 选择单个目标 |
| `SelectTargets(candidates, maxCount)` | 选择多个目标 |
| `AddExclude(target)` | 添加排除目标 |
| `ClearExcludes()` | 清空排除列表 |
