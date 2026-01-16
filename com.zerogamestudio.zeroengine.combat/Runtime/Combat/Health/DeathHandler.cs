using System;
using System.Collections;
using UnityEngine;

namespace ZeroEngine.Combat
{
    /// <summary>
    /// 死亡处理器 - 处理死亡后的行为
    /// </summary>
    public class DeathHandler : MonoBehaviour
    {
        [Header("配置")]
        [SerializeField] private HealthComponent _healthComponent;
        [SerializeField] private bool _autoFindHealth = true;

        [Header("死亡行为")]
        [SerializeField] private DeathBehavior _deathBehavior = DeathBehavior.Disable;
        [SerializeField] private float _destroyDelay = 0f;
        [SerializeField] private float _disableDelay = 0f;

        [Header("视觉效果")]
        [SerializeField] private GameObject _deathEffectPrefab;
        [SerializeField] private Vector3 _effectOffset;
        [SerializeField] private bool _detachChildren;

        [Header("物理")]
        [SerializeField] private bool _enableRagdoll;
        [SerializeField] private Rigidbody _ragdollRoot;
        [SerializeField] private float _ragdollForce = 10f;

        [Header("音效")]
        [SerializeField] private AudioClip _deathSound;
        [SerializeField] private AudioSource _audioSource;

        [Header("掉落")]
        [SerializeField] private bool _triggerLoot = true;
        [SerializeField] private string _lootTableId;

        /// <summary>死亡处理前事件</summary>
        public event Action<DeathEventArgs> OnBeforeDeath;

        /// <summary>死亡处理后事件</summary>
        public event Action<DeathEventArgs> OnAfterDeath;

        private Animator _animator;
        private Collider[] _colliders;
        private bool _isDead;

        protected virtual void Awake()
        {
            if (_autoFindHealth && _healthComponent == null)
            {
                _healthComponent = GetComponent<HealthComponent>();
            }

            _animator = GetComponent<Animator>();
            _colliders = GetComponentsInChildren<Collider>();

            if (_healthComponent != null)
            {
                _healthComponent.OnDeath += HandleDeath;
                _healthComponent.OnRevived += HandleRevive;
            }
        }

        protected virtual void OnDestroy()
        {
            if (_healthComponent != null)
            {
                _healthComponent.OnDeath -= HandleDeath;
                _healthComponent.OnRevived -= HandleRevive;
            }
        }

        /// <summary>
        /// 处理死亡
        /// </summary>
        private void HandleDeath(DeathEventArgs args)
        {
            if (_isDead) return;
            _isDead = true;

            OnBeforeDeath?.Invoke(args);

            // 播放死亡音效
            PlayDeathSound();

            // 生成死亡特效
            SpawnDeathEffect();

            // 播放死亡动画
            PlayDeathAnimation();

            // 启用布娃娃
            if (_enableRagdoll)
            {
                EnableRagdoll(args);
            }

            // 分离子对象
            if (_detachChildren)
            {
                DetachChildren();
            }

            // 触发掉落
            if (_triggerLoot)
            {
                TriggerLoot();
            }

            // 执行死亡行为
            StartCoroutine(ExecuteDeathBehavior());

            OnAfterDeath?.Invoke(args);
        }

        /// <summary>
        /// 处理复活
        /// </summary>
        private void HandleRevive()
        {
            _isDead = false;

            // 恢复碰撞体
            foreach (var col in _colliders)
            {
                if (col != null)
                {
                    col.enabled = true;
                }
            }

            // 禁用布娃娃
            if (_ragdollRoot != null)
            {
                _ragdollRoot.isKinematic = true;
            }

            // 重置动画
            if (_animator != null)
            {
                _animator.enabled = true;
                _animator.Rebind();
            }
        }

        /// <summary>
        /// 播放死亡音效
        /// </summary>
        protected virtual void PlayDeathSound()
        {
            if (_deathSound == null) return;

            if (_audioSource != null)
            {
                _audioSource.PlayOneShot(_deathSound);
            }
            else
            {
                AudioSource.PlayClipAtPoint(_deathSound, transform.position);
            }
        }

        /// <summary>
        /// 生成死亡特效
        /// </summary>
        protected virtual void SpawnDeathEffect()
        {
            if (_deathEffectPrefab == null) return;

            Vector3 position = transform.position + _effectOffset;
            Instantiate(_deathEffectPrefab, position, Quaternion.identity);
        }

        /// <summary>
        /// 播放死亡动画
        /// </summary>
        protected virtual void PlayDeathAnimation()
        {
            if (_animator == null || !_animator.enabled) return;

            // 尝试触发死亡动画
            _animator.SetTrigger("Death");
        }

        /// <summary>
        /// 启用布娃娃
        /// </summary>
        protected virtual void EnableRagdoll(DeathEventArgs args)
        {
            if (_ragdollRoot == null) return;

            // 禁用动画
            if (_animator != null)
            {
                _animator.enabled = false;
            }

            // 启用布娃娃物理
            _ragdollRoot.isKinematic = false;

            // 应用击退力
            if (args.Killer != null)
            {
                Vector3 direction = (transform.position - args.Killer.Transform.position).normalized;
                _ragdollRoot.AddForce(direction * _ragdollForce, ForceMode.Impulse);
            }
        }

        /// <summary>
        /// 分离子对象
        /// </summary>
        protected virtual void DetachChildren()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                transform.GetChild(i).SetParent(null);
            }
        }

        /// <summary>
        /// 触发掉落
        /// </summary>
        protected virtual void TriggerLoot()
        {
            if (string.IsNullOrEmpty(_lootTableId)) return;

            // 尝试使用 LootTableManager（如果存在）
            // LootTableManager.Instance?.RollAndGrant(lootTableId, transform.position);
        }

        /// <summary>
        /// 执行死亡行为
        /// </summary>
        private IEnumerator ExecuteDeathBehavior()
        {
            switch (_deathBehavior)
            {
                case DeathBehavior.None:
                    break;

                case DeathBehavior.Disable:
                    if (_disableDelay > 0)
                    {
                        yield return new WaitForSeconds(_disableDelay);
                    }
                    gameObject.SetActive(false);
                    break;

                case DeathBehavior.Destroy:
                    if (_destroyDelay > 0)
                    {
                        yield return new WaitForSeconds(_destroyDelay);
                    }
                    Destroy(gameObject);
                    break;

                case DeathBehavior.Pool:
                    if (_disableDelay > 0)
                    {
                        yield return new WaitForSeconds(_disableDelay);
                    }
                    // 尝试使用对象池
                    // PoolManager.Instance?.Return(gameObject);
                    gameObject.SetActive(false);
                    break;

                case DeathBehavior.DisableCollider:
                    foreach (var col in _colliders)
                    {
                        if (col != null)
                        {
                            col.enabled = false;
                        }
                    }
                    break;

                case DeathBehavior.Custom:
                    OnCustomDeathBehavior();
                    break;
            }
        }

        /// <summary>
        /// 自定义死亡行为（子类重写）
        /// </summary>
        protected virtual void OnCustomDeathBehavior()
        {
            // 子类实现
        }

        /// <summary>
        /// 强制触发死亡
        /// </summary>
        public void ForceDeath(ICombatant killer = null)
        {
            if (_healthComponent != null)
            {
                _healthComponent.SetHealth(0);
            }
            else
            {
                var args = new DeathEventArgs(null, killer, default);
                HandleDeath(args);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _destroyDelay = Mathf.Max(0, _destroyDelay);
            _disableDelay = Mathf.Max(0, _disableDelay);
        }
#endif
    }

    /// <summary>
    /// 死亡行为类型
    /// </summary>
    public enum DeathBehavior
    {
        /// <summary>不做任何处理</summary>
        None,
        /// <summary>禁用游戏对象</summary>
        Disable,
        /// <summary>销毁游戏对象</summary>
        Destroy,
        /// <summary>返回对象池</summary>
        Pool,
        /// <summary>仅禁用碰撞体</summary>
        DisableCollider,
        /// <summary>自定义行为</summary>
        Custom
    }
}
