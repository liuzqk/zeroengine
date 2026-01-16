using UnityEngine;

namespace ZeroEngine.Projectile
{
    /// <summary>
    /// 弹道命中效果组件
    /// </summary>
    public class ProjectileHitEffect : MonoBehaviour
    {
        [Header("Visual Effects")]
        [SerializeField] private GameObject _hitVFXPrefab;
        [SerializeField] private float _vfxDuration = 2f;
        [SerializeField] private bool _alignToSurface = true;
        [SerializeField] private Vector3 _vfxOffset = Vector3.zero;
        [SerializeField] private Vector3 _vfxScale = Vector3.one;

        [Header("Audio")]
        [SerializeField] private AudioClip _hitSound;
        [SerializeField] private float _soundVolume = 1f;
        [SerializeField] private float _soundPitchVariance = 0.1f;

        [Header("Camera Shake")]
        [SerializeField] private bool _enableCameraShake = false;
        [SerializeField] private float _shakeIntensity = 0.1f;
        [SerializeField] private float _shakeDuration = 0.1f;

        [Header("Decal")]
        [SerializeField] private GameObject _decalPrefab;
        [SerializeField] private float _decalDuration = 5f;
        [SerializeField] private Vector2 _decalSizeRange = new Vector2(0.5f, 1f);

        private ProjectileBase _projectile;

        private void Awake()
        {
            _projectile = GetComponent<ProjectileBase>();
        }

        private void OnEnable()
        {
            ProjectileEvents.OnProjectileHit += OnHit;
        }

        private void OnDisable()
        {
            ProjectileEvents.OnProjectileHit -= OnHit;
        }

        private void OnHit(ProjectileHitEventArgs args)
        {
            // 只处理自身弹道
            if (args.Projectile != _projectile) return;

            // 播放命中特效
            SpawnHitVFX(args.HitPoint, args.HitNormal);

            // 播放命中音效
            PlayHitSound(args.HitPoint);

            // 相机震动
            if (_enableCameraShake)
            {
                TriggerCameraShake();
            }

            // 生成贴花
            SpawnDecal(args.HitPoint, args.HitNormal, args.HitCollider?.gameObject);
        }

        /// <summary>
        /// 生成命中特效
        /// </summary>
        private void SpawnHitVFX(Vector3 position, Vector3 direction)
        {
            if (_hitVFXPrefab == null) return;

            Quaternion rotation = _alignToSurface && direction != Vector3.zero
                ? Quaternion.LookRotation(-direction)
                : Quaternion.identity;

            Vector3 spawnPos = position + rotation * _vfxOffset;

            var vfx = Instantiate(_hitVFXPrefab, spawnPos, rotation);
            vfx.transform.localScale = _vfxScale;

            // 自动销毁
            if (_vfxDuration > 0)
            {
                Destroy(vfx, _vfxDuration);
            }
        }

        /// <summary>
        /// 播放命中音效
        /// </summary>
        private void PlayHitSound(Vector3 position)
        {
            if (_hitSound == null) return;

            float pitch = 1f + Random.Range(-_soundPitchVariance, _soundPitchVariance);

            // 使用 AudioSource.PlayClipAtPoint 播放位置音效
            AudioSource.PlayClipAtPoint(_hitSound, position, _soundVolume);
        }

        /// <summary>
        /// 触发相机震动
        /// </summary>
        private void TriggerCameraShake()
        {
            // 查找相机控制器（如果存在）
            // Camera.CameraController.Instance?.Shake(_shakeIntensity, _shakeDuration);

            // 简单实现：直接抖动主相机
            if (Camera.main != null)
            {
                StartCoroutine(ShakeCamera());
            }
        }

        private System.Collections.IEnumerator ShakeCamera()
        {
            var mainCamera = Camera.main;
            if (mainCamera == null) yield break;

            Vector3 originalPos = mainCamera.transform.localPosition;
            float elapsed = 0f;

            while (elapsed < _shakeDuration)
            {
                float x = Random.Range(-1f, 1f) * _shakeIntensity;
                float y = Random.Range(-1f, 1f) * _shakeIntensity;

                mainCamera.transform.localPosition = originalPos + new Vector3(x, y, 0);

                elapsed += Time.deltaTime;
                yield return null;
            }

            mainCamera.transform.localPosition = originalPos;
        }

        /// <summary>
        /// 生成贴花
        /// </summary>
        private void SpawnDecal(Vector3 position, Vector3 direction, GameObject hitObject)
        {
            if (_decalPrefab == null) return;

            // 射线检测获取表面信息
            if (Physics.Raycast(position - direction * 0.1f, direction, out var hit, 1f))
            {
                Quaternion rotation = Quaternion.LookRotation(-hit.normal);
                var decal = Instantiate(_decalPrefab, hit.point + hit.normal * 0.01f, rotation);

                float scale = Random.Range(_decalSizeRange.x, _decalSizeRange.y);
                decal.transform.localScale = Vector3.one * scale;

                // 随机旋转
                decal.transform.Rotate(0, 0, Random.Range(0f, 360f));

                // 父级到被击中物体（可选）
                if (hitObject != null && hitObject.isStatic == false)
                {
                    decal.transform.SetParent(hitObject.transform);
                }

                // 自动销毁
                if (_decalDuration > 0)
                {
                    Destroy(decal, _decalDuration);
                }
            }
        }

        #region Static Factory

        /// <summary>
        /// 在指定位置播放命中效果（静态方法）
        /// </summary>
        public static void PlayAtPosition(
            Vector3 position,
            Vector3 direction,
            GameObject vfxPrefab,
            AudioClip sound = null,
            float vfxDuration = 2f)
        {
            if (vfxPrefab != null)
            {
                Quaternion rotation = direction != Vector3.zero
                    ? Quaternion.LookRotation(-direction)
                    : Quaternion.identity;

                var vfx = Instantiate(vfxPrefab, position, rotation);

                if (vfxDuration > 0)
                {
                    Destroy(vfx, vfxDuration);
                }
            }

            if (sound != null)
            {
                AudioSource.PlayClipAtPoint(sound, position);
            }
        }

        #endregion
    }

    /// <summary>
    /// 命中效果配置数据
    /// </summary>
    [System.Serializable]
    public class HitEffectConfig
    {
        public GameObject VFXPrefab;
        public AudioClip HitSound;
        public float VFXDuration = 2f;
        public bool AlignToSurface = true;
        public Vector3 VFXOffset = Vector3.zero;
        public Vector3 VFXScale = Vector3.one;
        public float SoundVolume = 1f;

        /// <summary>
        /// 播放效果
        /// </summary>
        public void Play(Vector3 position, Vector3 direction)
        {
            ProjectileHitEffect.PlayAtPosition(position, direction, VFXPrefab, HitSound, VFXDuration);
        }
    }
}
