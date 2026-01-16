using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ZeroEngine.ModSystem.Steam
{
    /// <summary>
    /// Workshop与ModLoader的集成桥接器
    /// </summary>
    public class WorkshopModBridge
    {
        private SteamWorkshopManager _workshopManager;
        
        public WorkshopModBridge()
        {
            _workshopManager = new SteamWorkshopManager();
        }
        
        /// <summary>
        /// 初始化并加载Workshop订阅的mod
        /// </summary>
        public void Initialize()
        {
            if (!_workshopManager.Initialize())
            {
                Debug.LogWarning("[WorkshopModBridge] Steam Workshop not available.");
                return;
            }
            
            _workshopManager.OnItemDownloaded += OnWorkshopItemDownloaded;
        }
        
        /// <summary>
        /// 刷新并加载所有Workshop订阅的mod
        /// </summary>
        public void LoadSubscribedMods()
        {
            _workshopManager.RefreshSubscribedItems();
            
            foreach (var item in _workshopManager.SubscribedItems)
            {
                if (!string.IsNullOrEmpty(item.LocalPath) && Directory.Exists(item.LocalPath))
                {
                    try
                    {
                        ModLoader.Instance.LoadMod(item.LocalPath);
                        Debug.Log($"[WorkshopModBridge] Loaded Workshop mod: {item.PublishedFileId}");
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[WorkshopModBridge] Failed to load Workshop mod {item.PublishedFileId}: {ex.Message}");
                    }
                }
            }
        }
        
        /// <summary>
        /// 当Workshop条目下载完成时自动加载
        /// </summary>
        private void OnWorkshopItemDownloaded(WorkshopItem item)
        {
            if (!string.IsNullOrEmpty(item.LocalPath) && Directory.Exists(item.LocalPath))
            {
                try
                {
                    ModLoader.Instance.LoadMod(item.LocalPath);
                    Debug.Log($"[WorkshopModBridge] Auto-loaded new Workshop mod: {item.PublishedFileId}");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[WorkshopModBridge] Failed to auto-load Workshop mod: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// 发布mod到Workshop
        /// </summary>
        public void PublishMod(string modPath, string previewImagePath = null)
        {
            var manifestPath = Path.Combine(modPath, "manifest.json");
            if (!File.Exists(manifestPath))
            {
                Debug.LogError("[WorkshopModBridge] Cannot publish: manifest.json not found.");
                return;
            }
            
            var json = File.ReadAllText(manifestPath);
            var manifest = JsonUtility.FromJson<ModManifest>(json);
            
            _workshopManager.PublishItem(
                manifest.Name,
                manifest.Description ?? "",
                modPath,
                previewImagePath,
                new[] { "Mod" }
            );
        }
        
        /// <summary>
        /// 获取SteamWorkshopManager实例
        /// </summary>
        public SteamWorkshopManager GetWorkshopManager() => _workshopManager;
        
        /// <summary>
        /// 需要在Update中调用
        /// </summary>
        public void Update()
        {
            _workshopManager.RunCallbacks();
        }
    }
}
