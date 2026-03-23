using System;
using System.Collections.Generic;

namespace ZeroEngine.Combat
{
    /// <summary>
    /// 仇恨表 — 每个战斗单位持有一个实例
    /// 记录对该单位产生仇恨的所有来源及其仇恨值
    ///
    /// 设计原则：
    /// - 纯数据，不依赖 MonoBehaviour，可独立单元测试
    /// - 用 string SourceId 做 key，避免对象引用带来的 GC 和序列化问题
    /// - 支持嘲讽（硬设仇恨）、减仇（乘法）、死亡转移等高级机制
    /// </summary>
    public class ThreatTable
    {
        private readonly Dictionary<string, float> _threats = new();

        // 缓存排序结果，避免每帧分配
        private readonly List<KeyValuePair<string, float>> _sortBuffer = new();
        private bool _dirty = true;

        /// <summary>
        /// 当前仇恨来源数量
        /// </summary>
        public int Count => _threats.Count;

        /// <summary>
        /// 仇恨表是否为空
        /// </summary>
        public bool IsEmpty => _threats.Count == 0;

        // ─────────────── 增减仇恨 ───────────────

        /// <summary>
        /// 增加仇恨值（累加）
        /// </summary>
        /// <param name="sourceId">仇恨来源 ID</param>
        /// <param name="amount">仇恨增量（正值）</param>
        public void AddThreat(string sourceId, float amount)
        {
            if (string.IsNullOrEmpty(sourceId) || amount <= 0) return;

            _threats.TryGetValue(sourceId, out float current);
            _threats[sourceId] = current + amount;
            _dirty = true;
        }

        /// <summary>
        /// 硬设仇恨值（嘲讽技能用 — 直接覆盖为指定值）
        /// </summary>
        public void SetThreat(string sourceId, float amount)
        {
            if (string.IsNullOrEmpty(sourceId)) return;

            _threats[sourceId] = Math.Max(0, amount);
            _dirty = true;
        }

        /// <summary>
        /// 乘法修改仇恨（减仇/增仇技能用）
        /// 例如：MultiplyThreat("id", 0.5f) 将仇恨减半
        /// </summary>
        public void MultiplyThreat(string sourceId, float multiplier)
        {
            if (!_threats.TryGetValue(sourceId, out float current)) return;

            _threats[sourceId] = current * multiplier;
            if (_threats[sourceId] <= 0) _threats.Remove(sourceId);
            _dirty = true;
        }

        /// <summary>
        /// 对所有仇恨来源乘以系数（全体减仇/增仇）
        /// </summary>
        public void MultiplyAll(float multiplier)
        {
            if (_threats.Count == 0) return;

            var keys = new List<string>(_threats.Keys);
            foreach (var key in keys)
            {
                _threats[key] *= multiplier;
                if (_threats[key] <= 0) _threats.Remove(key);
            }
            _dirty = true;
        }

        // ─────────────── 查询 ───────────────

        /// <summary>
        /// 获取指定来源的仇恨值
        /// </summary>
        public float GetThreat(string sourceId)
        {
            return _threats.TryGetValue(sourceId, out float val) ? val : 0f;
        }

        /// <summary>
        /// 获取仇恨最高的来源 ID（最优先攻击目标）
        /// </summary>
        /// <returns>仇恨最高的 SourceId，空表返回 null</returns>
        public string GetHighestThreatId()
        {
            if (_threats.Count == 0) return null;

            string highest = null;
            float maxThreat = float.MinValue;

            foreach (var kvp in _threats)
            {
                if (kvp.Value > maxThreat)
                {
                    maxThreat = kvp.Value;
                    highest = kvp.Key;
                }
            }

            return highest;
        }

        /// <summary>
        /// 获取仇恨最高的来源 ID 和对应的仇恨值
        /// </summary>
        public (string id, float threat) GetHighestThreat()
        {
            if (_threats.Count == 0) return (null, 0f);

            string highest = null;
            float maxThreat = float.MinValue;

            foreach (var kvp in _threats)
            {
                if (kvp.Value > maxThreat)
                {
                    maxThreat = kvp.Value;
                    highest = kvp.Key;
                }
            }

            return (highest, maxThreat);
        }

        /// <summary>
        /// 获取按仇恨值降序排列的列表
        /// 复用内部缓冲区避免每帧 GC
        /// </summary>
        public IReadOnlyList<KeyValuePair<string, float>> GetSorted()
        {
            if (_dirty)
            {
                _sortBuffer.Clear();
                _sortBuffer.AddRange(_threats);
                _sortBuffer.Sort((a, b) => b.Value.CompareTo(a.Value));
                _dirty = false;
            }
            return _sortBuffer;
        }

        /// <summary>
        /// 获取前 N 个仇恨目标
        /// </summary>
        public void GetTopN(int n, List<string> result)
        {
            result.Clear();
            var sorted = GetSorted();
            int count = Math.Min(n, sorted.Count);
            for (int i = 0; i < count; i++)
            {
                result.Add(sorted[i].Key);
            }
        }

        /// <summary>
        /// 检查是否存在指定来源的仇恨
        /// </summary>
        public bool HasThreatFrom(string sourceId)
        {
            return _threats.ContainsKey(sourceId);
        }

        // ─────────────── 维护 ───────────────

        /// <summary>
        /// 每 tick 调用：衰减所有仇恨并清理低值条目
        /// </summary>
        /// <param name="modifier">仇恨系数配置</param>
        public void Tick(ThreatModifier modifier)
        {
            if (_threats.Count == 0) return;

            modifier ??= ThreatModifier.Default;

            var keys = new List<string>(_threats.Keys);
            foreach (var key in keys)
            {
                _threats[key] *= modifier.DecayRate;

                // 低于阈值直接移除
                if (_threats[key] < modifier.PruneThreshold)
                {
                    _threats.Remove(key);
                }
            }
            _dirty = true;
        }

        /// <summary>
        /// 死亡时仇恨转移：将自身仇恨表按比例转移到接收者
        /// 典型场景：坦克死亡后仇恨转移给副坦
        /// </summary>
        /// <param name="receiver">接收仇恨的单位的 ThreatTable</param>
        /// <param name="ratio">转移比例 (0-1)</param>
        public void TransferTo(ThreatTable receiver, float ratio)
        {
            if (receiver == null || ratio <= 0) return;

            foreach (var kvp in _threats)
            {
                float transfer = kvp.Value * ratio;
                if (transfer > 0)
                {
                    receiver.AddThreat(kvp.Key, transfer);
                }
            }
        }

        /// <summary>
        /// 移除指定来源的仇恨（来源死亡时调用）
        /// </summary>
        public void RemoveSource(string sourceId)
        {
            if (_threats.Remove(sourceId))
            {
                _dirty = true;
            }
        }

        /// <summary>
        /// 清空仇恨表
        /// </summary>
        public void Clear()
        {
            _threats.Clear();
            _sortBuffer.Clear();
            _dirty = true;
        }

        /// <summary>
        /// 便捷方法：记录一次伤害行为产生的仇恨
        /// </summary>
        /// <param name="sourceId">攻击者 ID</param>
        /// <param name="damage">实际伤害值</param>
        /// <param name="isSkill">是否为技能</param>
        /// <param name="modifier">仇恨系数</param>
        public void RecordDamage(string sourceId, float damage, bool isSkill, ThreatModifier modifier)
        {
            modifier ??= ThreatModifier.Default;

            float threat = damage * modifier.DamageMul;
            if (isSkill) threat += damage * modifier.SkillBonusMul;

            AddThreat(sourceId, threat);
        }

        /// <summary>
        /// 便捷方法：记录一次治疗行为产生的仇恨
        /// 治疗仇恨均摊给调用方提供的敌方列表（由外部决定分摊逻辑）
        /// </summary>
        /// <param name="healerSourceId">治疗者 ID</param>
        /// <param name="healAmount">治疗量</param>
        /// <param name="modifier">仇恨系数</param>
        /// <returns>总仇恨量（由调用方分摊到各个敌方单位的 ThreatTable）</returns>
        public static float CalcHealThreat(float healAmount, ThreatModifier modifier)
        {
            modifier ??= ThreatModifier.Default;
            return healAmount * modifier.HealMul;
        }
    }
}
