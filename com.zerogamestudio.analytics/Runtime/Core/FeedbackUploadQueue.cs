using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace ZGS.Analytics
{
    /// <summary>
    /// 反馈上传队列 - 持久化管理未成功上传的 ZIP 文件
    /// </summary>
    public static class FeedbackUploadQueue
    {
        private const string QueueKey = "zgs_feedback_queue";
        private const int MaxPendingCount = 10;
        private const int MaxPendingAgeDays = 7;
        private const int MaxRetries = 3;
        private static readonly int[] RetryDelays = { 2, 4, 8 }; // 秒

        private static List<PendingUpload> _pendingUploads;
        private static bool _isProcessing;

        /// <summary>
        /// 待上传项
        /// </summary>
        [Serializable]
        public class PendingUpload
        {
            public string zipPath;
            public string version;
            public string userName;
            public long createdAt;
            public int retryCount;
        }

        /// <summary>
        /// 队列包装类（用于 JSON 序列化）
        /// </summary>
        [Serializable]
        private class QueueWrapper
        {
            public List<PendingUpload> items = new();
        }

        /// <summary>
        /// 获取反馈文件存储目录
        /// </summary>
        public static string FeedbackDirectory
        {
            get
            {
                string dir = Path.Combine(Application.persistentDataPath, "PendingFeedback");
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                return dir;
            }
        }

        /// <summary>
        /// 初始化队列（从 PlayerPrefs 加载）
        /// </summary>
        public static void Initialize()
        {
            LoadQueue();
            CleanupExpired();
        }

        /// <summary>
        /// 将失败的上传加入队列
        /// </summary>
        public static void Enqueue(string zipPath, string version, string userName)
        {
            if (_pendingUploads == null)
                LoadQueue();

            // 检查是否已存在
            if (_pendingUploads.Exists(p => p.zipPath == zipPath))
                return;

            // 限制队列大小
            while (_pendingUploads.Count >= MaxPendingCount)
            {
                var oldest = _pendingUploads[0];
                RemoveAndDeleteFile(oldest);
            }

            var pending = new PendingUpload
            {
                zipPath = zipPath,
                version = version,
                userName = userName,
                createdAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                retryCount = 0
            };

            _pendingUploads.Add(pending);
            SaveQueue();

            AnalyticsLog.Log($"[FeedbackQueue] 已加入队列: {Path.GetFileName(zipPath)}");
        }

        /// <summary>
        /// 处理所有待上传的文件（启动时调用）
        /// </summary>
        public static IEnumerator ProcessPendingUploads()
        {
            if (_isProcessing) yield break;
            if (_pendingUploads == null) LoadQueue();
            if (_pendingUploads.Count == 0) yield break;

            _isProcessing = true;
            AnalyticsLog.Log($"[FeedbackQueue] 开始处理 {_pendingUploads.Count} 个待上传文件");

            // 复制列表，避免迭代时修改
            var toProcess = new List<PendingUpload>(_pendingUploads);

            foreach (var pending in toProcess)
            {
                // 检查文件是否存在
                if (!File.Exists(pending.zipPath))
                {
                    AnalyticsLog.LogWarning($"[FeedbackQueue] 文件不存在，移除: {pending.zipPath}");
                    _pendingUploads.Remove(pending);
                    continue;
                }

                // 尝试上传
                bool success = false;
                yield return TryUpload(pending, result => success = result);

                if (success)
                {
                    RemoveAndDeleteFile(pending);
                    AnalyticsLog.Log($"[FeedbackQueue] 上传成功: {Path.GetFileName(pending.zipPath)}");
                }
                else
                {
                    pending.retryCount++;
                    AnalyticsLog.LogWarning($"[FeedbackQueue] 上传失败，重试次数: {pending.retryCount}");
                }
            }

            SaveQueue();
            _isProcessing = false;
        }

        /// <summary>
        /// 带重试的上传（供 ZipAttachmentUploader 调用）
        /// </summary>
        public static IEnumerator UploadWithRetry(string zipPath, string version, string userName, Action<bool> onComplete)
        {
            bool success = false;

            for (int i = 0; i <= MaxRetries; i++)
            {
                if (i > 0)
                {
                    int delay = RetryDelays[Math.Min(i - 1, RetryDelays.Length - 1)];
                    AnalyticsLog.Log($"[FeedbackQueue] 第 {i} 次重试，等待 {delay} 秒...");
                    yield return new WaitForSeconds(delay);
                }

                yield return DoUpload(zipPath, version, result => success = result);

                if (success)
                {
                    AnalyticsLog.Log($"[FeedbackQueue] 上传成功: {Path.GetFileName(zipPath)}");
                    onComplete?.Invoke(true);
                    yield break;
                }
            }

            // 所有重试都失败，加入队列
            AnalyticsLog.LogWarning($"[FeedbackQueue] 重试 {MaxRetries} 次后仍失败，加入离线队列");
            Enqueue(zipPath, version, userName);
            onComplete?.Invoke(false);
        }

        /// <summary>
        /// 执行单次上传
        /// </summary>
        private static IEnumerator DoUpload(string zipPath, string version, Action<bool> onComplete)
        {
            if (!AnalyticsConfig.IsConfigured)
            {
                onComplete?.Invoke(false);
                yield break;
            }

            string baseUrl = AnalyticsConfig.ServerUrl.TrimEnd('/');
            string uploadUrl = baseUrl + "/upload";
            string secret = AnalyticsConfig.Secret;
            string fileName = Path.GetFileName(zipPath);
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            byte[] fileData;
            try
            {
                fileData = File.ReadAllBytes(zipPath);
            }
            catch (Exception e)
            {
                AnalyticsLog.LogWarning($"[FeedbackQueue] 读取文件失败: {e.Message}");
                onComplete?.Invoke(false);
                yield break;
            }

            var form = new WWWForm();
            form.AddField("version", version);
            form.AddField("timestamp", timestamp);
            form.AddField("secret", secret);
            form.AddBinaryData("file", fileData, fileName, "application/zip");

            using var request = UnityWebRequest.Post(uploadUrl, form);
            request.timeout = 60;

            yield return request.SendWebRequest();

            bool success = request.result == UnityWebRequest.Result.Success;
            if (!success)
            {
                AnalyticsLog.LogWarning($"[FeedbackQueue] 上传失败: {request.error}");
            }

            onComplete?.Invoke(success);
        }

        /// <summary>
        /// 尝试上传单个待处理项
        /// </summary>
        private static IEnumerator TryUpload(PendingUpload pending, Action<bool> onComplete)
        {
            yield return DoUpload(pending.zipPath, pending.version, onComplete);
        }

        /// <summary>
        /// 清理过期文件
        /// </summary>
        private static void CleanupExpired()
        {
            if (_pendingUploads == null) return;

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long maxAge = MaxPendingAgeDays * 24 * 60 * 60;

            var expired = _pendingUploads.FindAll(p => now - p.createdAt > maxAge);
            foreach (var item in expired)
            {
                AnalyticsLog.Log($"[FeedbackQueue] 清理过期文件: {Path.GetFileName(item.zipPath)}");
                RemoveAndDeleteFile(item);
            }

            if (expired.Count > 0)
                SaveQueue();
        }

        /// <summary>
        /// 从队列移除并删除文件
        /// </summary>
        private static void RemoveAndDeleteFile(PendingUpload item)
        {
            _pendingUploads.Remove(item);

            try
            {
                if (File.Exists(item.zipPath))
                    File.Delete(item.zipPath);
            }
            catch (Exception e)
            {
                AnalyticsLog.LogWarning($"[FeedbackQueue] 删除文件失败: {e.Message}");
            }
        }

        /// <summary>
        /// 从 PlayerPrefs 加载队列
        /// </summary>
        private static void LoadQueue()
        {
            _pendingUploads = new List<PendingUpload>();

            string json = PlayerPrefs.GetString(QueueKey, "");
            if (string.IsNullOrEmpty(json)) return;

            try
            {
                var wrapper = JsonUtility.FromJson<QueueWrapper>(json);
                if (wrapper?.items != null)
                    _pendingUploads = wrapper.items;
            }
            catch (Exception e)
            {
                AnalyticsLog.LogWarning($"[FeedbackQueue] 加载队列失败: {e.Message}");
                _pendingUploads = new List<PendingUpload>();
            }
        }

        /// <summary>
        /// 保存队列到 PlayerPrefs
        /// </summary>
        private static void SaveQueue()
        {
            var wrapper = new QueueWrapper { items = _pendingUploads };
            string json = JsonUtility.ToJson(wrapper);
            PlayerPrefs.SetString(QueueKey, json);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 获取待上传数量
        /// </summary>
        public static int PendingCount
        {
            get
            {
                if (_pendingUploads == null) LoadQueue();
                return _pendingUploads.Count;
            }
        }

        /// <summary>
        /// 清空队列（调试用）
        /// </summary>
        public static void ClearQueue()
        {
            if (_pendingUploads == null) LoadQueue();

            foreach (var item in _pendingUploads)
            {
                try
                {
                    if (File.Exists(item.zipPath))
                        File.Delete(item.zipPath);
                }
                catch { }
            }

            _pendingUploads.Clear();
            PlayerPrefs.DeleteKey(QueueKey);
            PlayerPrefs.Save();
            AnalyticsLog.Log("[FeedbackQueue] 队列已清空");
        }
    }
}
