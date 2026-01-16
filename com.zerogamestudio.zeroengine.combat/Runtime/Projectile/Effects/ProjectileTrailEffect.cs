using UnityEngine;

namespace ZeroEngine.Projectile
{
    /// <summary>
    /// 弹道拖尾效果组件
    /// </summary>
    [RequireComponent(typeof(TrailRenderer))]
    public class ProjectileTrailEffect : MonoBehaviour
    {
        [Header("Trail Settings")]
        [SerializeField] private float _trailTime = 0.5f;
        [SerializeField] private float _startWidth = 0.1f;
        [SerializeField] private float _endWidth = 0f;
        [SerializeField] private Gradient _colorGradient;
        [SerializeField] private Material _trailMaterial;

        [Header("Trail Behavior")]
        [SerializeField] private bool _emitOnMove = true;
        [SerializeField] private float _minVelocity = 0.1f;
        [SerializeField] private bool _detachOnDestroy = true;
        [SerializeField] private float _detachFadeTime = 0.5f;

        [Header("Particle Trail (Optional)")]
        [SerializeField] private ParticleSystem _particleTrail;
        [SerializeField] private bool _useParticleTrail = false;

        private TrailRenderer _trailRenderer;
        private ProjectileBase _projectile;
        private Vector3 _lastPosition;
        private bool _isEmitting = true;

        #region Properties

        /// <summary>拖尾渲染器</summary>
        public TrailRenderer TrailRenderer => _trailRenderer;

        /// <summary>是否正在发射</summary>
        public bool IsEmitting => _isEmitting;

        #endregion

        private void Awake()
        {
            _trailRenderer = GetComponent<TrailRenderer>();
            _projectile = GetComponent<ProjectileBase>();

            InitializeTrail();
        }

        private void Start()
        {
            _lastPosition = transform.position;
        }

        private void OnEnable()
        {
            if (_projectile != null)
            {
                ProjectileEvents.OnProjectileDestroy += OnProjectileDestroy;
            }

            ResetTrail();
        }

        private void OnDisable()
        {
            ProjectileEvents.OnProjectileDestroy -= OnProjectileDestroy;
        }

        private void Update()
        {
            if (!_emitOnMove) return;

            // 根据速度控制发射
            float velocity = (transform.position - _lastPosition).magnitude / Time.deltaTime;
            bool shouldEmit = velocity >= _minVelocity;

            if (shouldEmit != _isEmitting)
            {
                SetEmitting(shouldEmit);
            }

            _lastPosition = transform.position;
        }

        /// <summary>
        /// 初始化拖尾
        /// </summary>
        private void InitializeTrail()
        {
            if (_trailRenderer == null) return;

            _trailRenderer.time = _trailTime;
            _trailRenderer.startWidth = _startWidth;
            _trailRenderer.endWidth = _endWidth;

            if (_colorGradient != null)
            {
                _trailRenderer.colorGradient = _colorGradient;
            }

            if (_trailMaterial != null)
            {
                _trailRenderer.material = _trailMaterial;
            }

            // 粒子拖尾
            if (_useParticleTrail && _particleTrail != null)
            {
                _particleTrail.Play();
            }
        }

        /// <summary>
        /// 重置拖尾
        /// </summary>
        public void ResetTrail()
        {
            if (_trailRenderer != null)
            {
                _trailRenderer.Clear();
                _trailRenderer.emitting = true;
            }

            _isEmitting = true;
            _lastPosition = transform.position;

            if (_useParticleTrail && _particleTrail != null)
            {
                _particleTrail.Clear();
                _particleTrail.Play();
            }
        }

        /// <summary>
        /// 设置是否发射
        /// </summary>
        public void SetEmitting(bool emit)
        {
            _isEmitting = emit;

            if (_trailRenderer != null)
            {
                _trailRenderer.emitting = emit;
            }

            if (_useParticleTrail && _particleTrail != null)
            {
                if (emit)
                {
                    _particleTrail.Play();
                }
                else
                {
                    _particleTrail.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
            }
        }

        /// <summary>
        /// 弹道销毁事件处理
        /// </summary>
        private void OnProjectileDestroy(ProjectileDestroyEventArgs args)
        {
            if (args.Projectile != _projectile) return;

            if (_detachOnDestroy)
            {
                DetachTrail();
            }
        }

        /// <summary>
        /// 分离拖尾（弹道销毁时保留拖尾淡出）
        /// </summary>
        public void DetachTrail()
        {
            if (_trailRenderer == null) return;

            // 创建独立的拖尾对象
            var detachedTrail = new GameObject("DetachedTrail");
            detachedTrail.transform.position = transform.position;
            detachedTrail.transform.rotation = transform.rotation;

            // 复制 TrailRenderer
            var newTrail = detachedTrail.AddComponent<TrailRenderer>();
            CopyTrailRenderer(_trailRenderer, newTrail);

            // 停止发射
            newTrail.emitting = false;

            // 添加淡出组件
            var fader = detachedTrail.AddComponent<TrailFader>();
            fader.Initialize(_detachFadeTime);

            // 粒子拖尾也分离
            if (_useParticleTrail && _particleTrail != null)
            {
                _particleTrail.transform.SetParent(null);
                _particleTrail.Stop(true, ParticleSystemStopBehavior.StopEmitting);

                // 等待粒子消失后销毁
                var main = _particleTrail.main;
                Destroy(_particleTrail.gameObject, main.startLifetime.constantMax);
            }

            // 清除原始拖尾
            _trailRenderer.Clear();
        }

        /// <summary>
        /// 复制 TrailRenderer 设置
        /// </summary>
        private void CopyTrailRenderer(TrailRenderer source, TrailRenderer dest)
        {
            dest.time = source.time;
            dest.startWidth = source.startWidth;
            dest.endWidth = source.endWidth;
            dest.widthCurve = source.widthCurve;
            dest.colorGradient = source.colorGradient;
            dest.material = source.material;
            dest.numCornerVertices = source.numCornerVertices;
            dest.numCapVertices = source.numCapVertices;
            dest.textureMode = source.textureMode;
            dest.alignment = source.alignment;
            dest.shadowCastingMode = source.shadowCastingMode;
            dest.receiveShadows = source.receiveShadows;
            dest.minVertexDistance = source.minVertexDistance;
            dest.autodestruct = false;
        }

        #region Configuration Methods

        /// <summary>
        /// 设置拖尾时间
        /// </summary>
        public void SetTrailTime(float time)
        {
            _trailTime = time;
            if (_trailRenderer != null)
            {
                _trailRenderer.time = time;
            }
        }

        /// <summary>
        /// 设置拖尾宽度
        /// </summary>
        public void SetTrailWidth(float startWidth, float endWidth)
        {
            _startWidth = startWidth;
            _endWidth = endWidth;

            if (_trailRenderer != null)
            {
                _trailRenderer.startWidth = startWidth;
                _trailRenderer.endWidth = endWidth;
            }
        }

        /// <summary>
        /// 设置拖尾颜色
        /// </summary>
        public void SetTrailColor(Color startColor, Color endColor)
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(startColor, 0f),
                    new GradientColorKey(endColor, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(startColor.a, 0f),
                    new GradientAlphaKey(endColor.a, 1f)
                }
            );

            _colorGradient = gradient;

            if (_trailRenderer != null)
            {
                _trailRenderer.colorGradient = gradient;
            }
        }

        /// <summary>
        /// 设置拖尾材质
        /// </summary>
        public void SetTrailMaterial(Material material)
        {
            _trailMaterial = material;
            if (_trailRenderer != null)
            {
                _trailRenderer.material = material;
            }
        }

        #endregion

        #region Static Factory

        /// <summary>
        /// 添加拖尾效果到 GameObject
        /// </summary>
        public static ProjectileTrailEffect AddTo(GameObject target, TrailEffectConfig config)
        {
            var trail = target.GetComponent<ProjectileTrailEffect>();
            if (trail == null)
            {
                trail = target.AddComponent<ProjectileTrailEffect>();
            }

            config.ApplyTo(trail);
            return trail;
        }

        #endregion
    }

    /// <summary>
    /// 拖尾淡出组件
    /// </summary>
    public class TrailFader : MonoBehaviour
    {
        private TrailRenderer _trail;
        private float _fadeDuration;
        private float _elapsed;
        private float _originalTime;

        public void Initialize(float duration)
        {
            _trail = GetComponent<TrailRenderer>();
            _fadeDuration = duration;
            _elapsed = 0f;

            if (_trail != null)
            {
                _originalTime = _trail.time;
            }
        }

        private void Update()
        {
            if (_trail == null)
            {
                Destroy(gameObject);
                return;
            }

            _elapsed += Time.deltaTime;
            float t = _elapsed / _fadeDuration;

            if (t >= 1f)
            {
                Destroy(gameObject);
                return;
            }

            // 渐渐缩短拖尾时间使其淡出
            _trail.time = Mathf.Lerp(_originalTime, 0f, t);
        }
    }

    /// <summary>
    /// 拖尾效果配置数据
    /// </summary>
    [System.Serializable]
    public class TrailEffectConfig
    {
        public float TrailTime = 0.5f;
        public float StartWidth = 0.1f;
        public float EndWidth = 0f;
        public Gradient ColorGradient;
        public Material TrailMaterial;
        public bool EmitOnMove = true;
        public float MinVelocity = 0.1f;
        public bool DetachOnDestroy = true;
        public float DetachFadeTime = 0.5f;

        /// <summary>
        /// 应用配置到拖尾效果
        /// </summary>
        public void ApplyTo(ProjectileTrailEffect trail)
        {
            trail.SetTrailTime(TrailTime);
            trail.SetTrailWidth(StartWidth, EndWidth);

            if (ColorGradient != null)
            {
                var renderer = trail.TrailRenderer;
                if (renderer != null)
                {
                    renderer.colorGradient = ColorGradient;
                }
            }

            if (TrailMaterial != null)
            {
                trail.SetTrailMaterial(TrailMaterial);
            }
        }
    }
}
