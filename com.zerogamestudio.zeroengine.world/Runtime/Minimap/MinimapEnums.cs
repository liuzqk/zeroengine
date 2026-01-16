using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZeroEngine.Minimap
{
    /// <summary>小地图标记类型</summary>
    public enum MinimapMarkerType
    {
        Player,         // 玩家
        PartyMember,    // 队友
        Enemy,          // 敌人
        NPC,            // NPC
        Quest,          // 任务目标
        Waypoint,       // 路标
        Building,       // 建筑
        Portal,         // 传送门
        Item,           // 物品
        Treasure,       // 宝箱
        Shop,           // 商店
        Custom          // 自定义
    }

    /// <summary>小地图缩放模式</summary>
    public enum MinimapZoomMode
    {
        Fixed,          // 固定缩放
        Dynamic,        // 动态缩放 (根据移动速度)
        Manual          // 手动缩放
    }

    /// <summary>小地图旋转模式</summary>
    public enum MinimapRotationMode
    {
        NorthUp,        // 北朝上 (固定)
        PlayerUp        // 玩家朝上 (旋转)
    }

    /// <summary>小地图事件类型</summary>
    public enum MinimapEventType
    {
        MarkerAdded,
        MarkerRemoved,
        ZoomChanged,
        TargetChanged
    }

    /// <summary>小地图事件参数</summary>
    public class MinimapEventArgs
    {
        public MinimapEventType Type { get; private set; }
        public MinimapMarker Marker { get; private set; }
        public float ZoomLevel { get; private set; }
        public Transform Target { get; private set; }

        public static MinimapEventArgs MarkerAdded(MinimapMarker marker)
            => new() { Type = MinimapEventType.MarkerAdded, Marker = marker };

        public static MinimapEventArgs MarkerRemoved(MinimapMarker marker)
            => new() { Type = MinimapEventType.MarkerRemoved, Marker = marker };

        public static MinimapEventArgs ZoomChanged(float zoom)
            => new() { Type = MinimapEventType.ZoomChanged, ZoomLevel = zoom };

        public static MinimapEventArgs TargetChanged(Transform target)
            => new() { Type = MinimapEventType.TargetChanged, Target = target };
    }

    /// <summary>标记图标配置</summary>
    [Serializable]
    public class MarkerIconConfig
    {
        public MinimapMarkerType MarkerType;
        public Sprite Icon;
        public Color Color = Color.white;
        public float Scale = 1f;
    }

    /// <summary>小地图标记管理器 - 静态追踪所有活动标记</summary>
    public static class MinimapMarkerManager
    {
        private static readonly List<MinimapMarker> _markers = new List<MinimapMarker>();

        public static IReadOnlyList<MinimapMarker> Markers => _markers;
        public static int MarkerCount => _markers.Count;

        public static event Action<MinimapMarker> OnMarkerRegistered;
        public static event Action<MinimapMarker> OnMarkerUnregistered;

        public static void Register(MinimapMarker marker)
        {
            if (marker == null) return;
            if (!_markers.Contains(marker))
            {
                _markers.Add(marker);
                OnMarkerRegistered?.Invoke(marker);
            }
        }

        public static void Unregister(MinimapMarker marker)
        {
            if (marker == null) return;
            if (_markers.Remove(marker))
            {
                OnMarkerUnregistered?.Invoke(marker);
            }
        }

        public static void GetMarkersOfType(MinimapMarkerType type, List<MinimapMarker> results)
        {
            results.Clear();
            foreach (var marker in _markers)
            {
                if (marker.MarkerType == type)
                    results.Add(marker);
            }
        }

        public static void GetVisibleMarkers(Vector3 viewerPosition, List<MinimapMarker> results)
        {
            results.Clear();
            foreach (var marker in _markers)
            {
                if (marker.IsVisibleFrom(viewerPosition))
                    results.Add(marker);
            }
        }

        public static MinimapMarker FindNearest(Vector3 position, MinimapMarkerType type)
        {
            MinimapMarker nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (var marker in _markers)
            {
                if (marker.MarkerType != type) continue;

                float dist = Vector3.Distance(position, marker.transform.position);
                if (dist < nearestDistance)
                {
                    nearestDistance = dist;
                    nearest = marker;
                }
            }

            return nearest;
        }

        public static void ClearAll()
        {
            _markers.Clear();
        }
    }
}
