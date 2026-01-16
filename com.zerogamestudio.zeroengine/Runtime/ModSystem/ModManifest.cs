using System;

namespace ZeroEngine.ModSystem
{
    /// <summary>
    /// Mod清单数据结构，定义mod的元信息
    /// </summary>
    [Serializable]
    public class ModManifest
    {
        /// <summary>
        /// 唯一标识符，如 "zerogame.example_mod"
        /// </summary>
        public string Id;
        
        /// <summary>
        /// 显示名称
        /// </summary>
        public string Name;
        
        /// <summary>
        /// 语义化版本，如 "1.0.0"
        /// </summary>
        public string Version;
        
        /// <summary>
        /// 作者
        /// </summary>
        public string Author;
        
        /// <summary>
        /// 描述
        /// </summary>
        public string Description;
        
        /// <summary>
        /// 依赖的其他mod id
        /// </summary>
        public string[] Dependencies;
        
        /// <summary>
        /// 冲突的mod id
        /// </summary>
        public string[] Conflicts;
        
        /// <summary>
        /// 支持的游戏版本范围，如 ">=1.0.0"
        /// </summary>
        public string GameVersion;
        
        /// <summary>
        /// 相对于mod根目录的内容目录
        /// </summary>
        public string[] ContentPaths;
        
        /// <summary>
        /// 是否启用（用户可禁用mod）
        /// </summary>
        public bool Enabled = true;
        
        /// <summary>
        /// Mod根目录路径（运行时填充）
        /// </summary>
        [NonSerialized]
        public string RootPath;
        
        /// <summary>
        /// 加载顺序（依赖解析后填充）
        /// </summary>
        [NonSerialized]
        public int LoadOrder;
        
        public override string ToString() => $"{Id} v{Version}";
    }
}
