using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace ZGS.Analytics
{
    /// <summary>
    /// 可序列化事件接口
    /// </summary>
    public interface ISerializableEvent
    {
        string ToJson();
    }

    /// <summary>
    /// 离线队列 - 使用 PlayerPrefs 缓存未发送的事件
    /// </summary>
    public class OfflineQueue
    {
        private const string QueueKey = "zgs_analytics_queue";
        private const int MaxQueueSize = 500;
        private const float SaveInterval = 5f; // 批量保存间隔（秒）

        private readonly Queue<string> _memoryQueue = new();
        private bool _isFlushing;
        private bool _isDirty;
        private float _lastSaveTime;

        public OfflineQueue()
        {
            LoadFromStorage();
            _lastSaveTime = Time.realtimeSinceStartup;
        }

        /// <summary>
        /// 将事件加入队列
        /// </summary>
        public void Enqueue(ISerializableEvent payload)
        {
            Enqueue(payload.ToJson());
        }

        /// <summary>
        /// 将 JSON 字符串加入队列
        /// </summary>
        public void Enqueue(string json)
        {
            if (string.IsNullOrEmpty(json)) return;

            lock (_memoryQueue)
            {
                _memoryQueue.Enqueue(json);

                // 防止内存溢出
                while (_memoryQueue.Count > MaxQueueSize)
                    _memoryQueue.Dequeue();
            }

            // 标记脏，延迟批量保存
            _isDirty = true;
            TrySaveIfNeeded();
        }

        /// <summary>
        /// 检查是否需要保存（防抖机制）
        /// </summary>
        private void TrySaveIfNeeded()
        {
            if (!_isDirty) return;

            float now = Time.realtimeSinceStartup;
            if (now - _lastSaveTime >= SaveInterval)
            {
                SaveToStorage();
                _isDirty = false;
                _lastSaveTime = now;
            }
        }

        /// <summary>
        /// 刷新所有事件到服务器
        /// </summary>
        public void FlushAll(string serverUrl, string secret)
        {
            // Flush 前强制保存未持久化的数据
            if (_isDirty)
            {
                SaveToStorage();
                _isDirty = false;
                _lastSaveTime = Time.realtimeSinceStartup;
            }

            if (_isFlushing) return;

            var runner = CoroutineRunner.Instance;
            if (runner != null)
                runner.StartCoroutine(FlushCoroutine(serverUrl, secret));
        }

        private IEnumerator FlushCoroutine(string serverUrl, string secret)
        {
            _isFlushing = true;
            
            while (_memoryQueue.Count > 0)
            {
                string json;
                lock (_memoryQueue)
                {
                    if (_memoryQueue.Count == 0) break;
                    json = _memoryQueue.Peek();
                }
                
                string body = $"{{\"secret\":\"{secret}\",\"body\":{json}}}";
                byte[] bodyRaw = Encoding.UTF8.GetBytes(body);
                
                using (var request = new UnityWebRequest(serverUrl, "POST"))
                {
                    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.SetRequestHeader("Content-Type", "application/json");
                    request.timeout = 10;
                    
                    yield return request.SendWebRequest();
                    
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        lock (_memoryQueue)
                        {
                            _memoryQueue.Dequeue();
                        }
                        SaveToStorage();
                    }
                    else
                    {
                        AnalyticsLog.LogWarning($"[ZGS.Analytics] Failed to send event: {request.error}");
                        AnalyticsLog.LogWarning($"[ZGS.Analytics] Response: {request.downloadHandler?.text}");
                        break;
                    }
                }
            }
            
            _isFlushing = false;
        }

        private void SaveToStorage()
        {
            lock (_memoryQueue)
            {
                var array = _memoryQueue.ToArray();
                var serialized = string.Join("\n", array);
                PlayerPrefs.SetString(QueueKey, serialized);
                PlayerPrefs.Save();
            }
        }

        private void LoadFromStorage()
        {
            var stored = PlayerPrefs.GetString(QueueKey, "");
            if (string.IsNullOrEmpty(stored)) return;
            
            var lines = stored.Split('\n');
            int skipped = 0;
            foreach (var line in lines)
            {
                if (string.IsNullOrEmpty(line)) continue;
                
                // 跳过损坏的 JSON (包含 C# 类型名)
                if (line.Contains("System.Collections.Generic"))
                {
                    skipped++;
                    continue;
                }
                
                _memoryQueue.Enqueue(line);
            }
            
            if (skipped > 0)
            {
                AnalyticsLog.LogWarning($"[ZGS.Analytics] Skipped {skipped} corrupted events from storage");
                SaveToStorage(); // 保存清理后的队列
            }
            
            if (_memoryQueue.Count > 0)
                AnalyticsLog.Log($"[ZGS.Analytics] Loaded {_memoryQueue.Count} pending events from storage");
        }

        /// <summary>
        /// 清空队列 (用于清除损坏的数据)
        /// </summary>
        public void ClearQueue()
        {
            lock (_memoryQueue)
            {
                _memoryQueue.Clear();
            }
            PlayerPrefs.DeleteKey(QueueKey);
            PlayerPrefs.Save();
            AnalyticsLog.Log("[ZGS.Analytics] Queue cleared");
        }

        public int Count => _memoryQueue.Count;
    }

    /// <summary>
    /// 协程运行器 - 线程安全单例
    /// </summary>
    public class CoroutineRunner : MonoBehaviour
    {
        private static CoroutineRunner _instance;
        private static readonly object Lock = new();
        
        public static CoroutineRunner Instance
        {
            get
            {
                if (_instance != null) return _instance;
                
                lock (Lock)
                {
                    if (_instance != null) return _instance;
                    
                    var go = new GameObject("[ZGS.Analytics.CoroutineRunner]");
                    _instance = go.AddComponent<CoroutineRunner>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
    }
}
