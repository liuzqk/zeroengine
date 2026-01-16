using System;
using System.Collections;
using UnityEngine;

namespace ZeroEngine.Save
{
    /// <summary>
    /// 截图捕获组件 (v1.8.0 性能优化版)
    /// 用于存档预览截图
    ///
    /// 优化点:
    /// - GPU 缩放替代 CPU GetPixel (100x 提速)
    /// - 纹理复用，减少 GC 分配
    /// - Camera 缓存，避免每次查找
    /// </summary>
    public class ScreenshotCapture : MonoBehaviour
    {
        private int _width = 320;
        private int _height = 180;
        private int _quality = 75;
        private bool _initialized;

        // 复用纹理
        private RenderTexture _renderTexture;
        private RenderTexture _scaleRT;
        private Texture2D _captureTexture;
        private Texture2D _screenTexture;

        // Camera 缓存
        private Camera _cachedCamera;

        /// <summary>
        /// 初始化截图配置
        /// </summary>
        public void Initialize(int width, int height, int quality)
        {
            _width = Mathf.Clamp(width, 64, 1920);
            _height = Mathf.Clamp(height, 64, 1080);
            _quality = Mathf.Clamp(quality, 1, 100);
            _initialized = true;

            // 预创建纹理
            EnsureTextures();
        }

        /// <summary>
        /// 刷新 Camera 缓存 (场景切换后调用)
        /// </summary>
        public void RefreshCameraCache()
        {
            _cachedCamera = Camera.main;
            if (_cachedCamera == null)
            {
                _cachedCamera = FindFirstObjectByType<Camera>();
            }
        }

        /// <summary>
        /// 异步捕获截图
        /// </summary>
        public void CaptureAsync(Action<byte[]> callback)
        {
            if (!_initialized)
            {
                callback?.Invoke(null);
                return;
            }

            StartCoroutine(CaptureCoroutine(callback));
        }

        /// <summary>
        /// 同步捕获截图 (会阻塞一帧)
        /// </summary>
        public byte[] CaptureSync()
        {
            if (!_initialized) return null;

            try
            {
                EnsureTextures();

                // 使用缓存的 Camera
                if (_cachedCamera == null)
                {
                    RefreshCameraCache();
                }

                if (_cachedCamera == null)
                {
                    Debug.LogWarning("[ScreenshotCapture] No camera found.");
                    return null;
                }

                // 保存原始设置
                var originalRT = _cachedCamera.targetTexture;

                // 渲染到 RenderTexture
                _cachedCamera.targetTexture = _renderTexture;
                _cachedCamera.Render();

                // 读取像素
                RenderTexture.active = _renderTexture;
                _captureTexture.ReadPixels(new Rect(0, 0, _width, _height), 0, 0);
                _captureTexture.Apply();

                // 恢复设置
                _cachedCamera.targetTexture = originalRT;
                RenderTexture.active = null;

                return _captureTexture.EncodeToJPG(_quality);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ScreenshotCapture] Capture failed: {e.Message}");
                return null;
            }
        }

        private IEnumerator CaptureCoroutine(Action<byte[]> callback)
        {
            yield return new WaitForEndOfFrame();

            byte[] result = null;

            try
            {
                EnsureCaptureTexture();

                // 计算捕获区域
                int srcX, srcY, srcWidth, srcHeight;
                CalculateCaptureRegion(out srcX, out srcY, out srcWidth, out srcHeight);

                // 复用或重建屏幕纹理
                EnsureScreenTexture(srcWidth, srcHeight);

                // 读取屏幕像素
                _screenTexture.ReadPixels(new Rect(srcX, srcY, srcWidth, srcHeight), 0, 0);
                _screenTexture.Apply();

                // GPU 缩放
                ScaleTextureGPU(_screenTexture, _captureTexture);

                result = _captureTexture.EncodeToJPG(_quality);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ScreenshotCapture] Capture failed: {e.Message}");
            }

            callback?.Invoke(result);
        }

        private void CalculateCaptureRegion(out int srcX, out int srcY, out int srcWidth, out int srcHeight)
        {
            float screenAspect = (float)Screen.width / Screen.height;
            float targetAspect = (float)_width / _height;

            if (screenAspect > targetAspect)
            {
                // 屏幕更宽，从两侧裁剪
                srcHeight = Screen.height;
                srcWidth = Mathf.RoundToInt(srcHeight * targetAspect);
                srcX = (Screen.width - srcWidth) / 2;
                srcY = 0;
            }
            else
            {
                // 屏幕更高，从上下裁剪
                srcWidth = Screen.width;
                srcHeight = Mathf.RoundToInt(srcWidth / targetAspect);
                srcX = 0;
                srcY = (Screen.height - srcHeight) / 2;
            }
        }

        /// <summary>
        /// GPU 加速缩放 (使用 Graphics.Blit)
        /// </summary>
        private void ScaleTextureGPU(Texture2D source, Texture2D destination)
        {
            // 确保缩放用 RenderTexture 存在
            if (_scaleRT == null || _scaleRT.width != destination.width || _scaleRT.height != destination.height)
            {
                if (_scaleRT != null)
                {
                    _scaleRT.Release();
                    Destroy(_scaleRT);
                }
                _scaleRT = new RenderTexture(destination.width, destination.height, 0, RenderTextureFormat.ARGB32);
                _scaleRT.filterMode = FilterMode.Bilinear;
            }

            // GPU blit 带双线性过滤
            var prevRT = RenderTexture.active;
            Graphics.Blit(source, _scaleRT);

            // 读取结果
            RenderTexture.active = _scaleRT;
            destination.ReadPixels(new Rect(0, 0, destination.width, destination.height), 0, 0);
            destination.Apply();
            RenderTexture.active = prevRT;
        }

        private void EnsureTextures()
        {
            if (_renderTexture == null || _renderTexture.width != _width || _renderTexture.height != _height)
            {
                if (_renderTexture != null)
                {
                    _renderTexture.Release();
                    Destroy(_renderTexture);
                }
                _renderTexture = new RenderTexture(_width, _height, 24);
            }

            EnsureCaptureTexture();
        }

        private void EnsureCaptureTexture()
        {
            if (_captureTexture == null || _captureTexture.width != _width || _captureTexture.height != _height)
            {
                if (_captureTexture != null)
                {
                    Destroy(_captureTexture);
                }
                _captureTexture = new Texture2D(_width, _height, TextureFormat.RGB24, false);
            }
        }

        private void EnsureScreenTexture(int width, int height)
        {
            if (_screenTexture == null || _screenTexture.width != width || _screenTexture.height != height)
            {
                if (_screenTexture != null)
                {
                    Destroy(_screenTexture);
                }
                _screenTexture = new Texture2D(width, height, TextureFormat.RGB24, false);
            }
        }

        private void OnDestroy()
        {
            if (_renderTexture != null)
            {
                _renderTexture.Release();
                Destroy(_renderTexture);
            }

            if (_scaleRT != null)
            {
                _scaleRT.Release();
                Destroy(_scaleRT);
            }

            if (_captureTexture != null)
            {
                Destroy(_captureTexture);
            }

            if (_screenTexture != null)
            {
                Destroy(_screenTexture);
            }
        }
    }
}