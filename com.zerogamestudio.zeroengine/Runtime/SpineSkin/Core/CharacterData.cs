using System;
using System.Collections.Generic;

namespace ZeroEngine.SpineSkin
{
    /// <summary>
    /// 单个角色的存档数据
    /// </summary>
    [Serializable]
    public class CharacterData
    {
        /// <summary>
        /// 角色唯一ID
        /// </summary>
        public string Id;
        
        /// <summary>
        /// 角色名称
        /// </summary>
        public string Name;
        
        /// <summary>
        /// 性别（对应配置中的GenderNames）
        /// </summary>
        public string Gender;
        
        /// <summary>
        /// 已装备的皮肤 (SlotId -> SkinName)
        /// </summary>
        public Dictionary<string, string> EquippedSkins = new();
        
        /// <summary>
        /// 创建时间
        /// </summary>
        public long CreatedTimestamp;
        
        /// <summary>
        /// 最后修改时间
        /// </summary>
        public long LastModifiedTimestamp;
        
        public CharacterData()
        {
            Id = Guid.NewGuid().ToString();
            CreatedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            LastModifiedTimestamp = CreatedTimestamp;
        }
        
        public CharacterData(string name, string gender) : this()
        {
            Name = name;
            Gender = gender;
        }
        
        /// <summary>
        /// 标记为已修改
        /// </summary>
        public void MarkModified()
        {
            LastModifiedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
        
        /// <summary>
        /// 克隆角色数据
        /// </summary>
        public CharacterData Clone()
        {
            return new CharacterData
            {
                Id = Id,
                Name = Name,
                Gender = Gender,
                EquippedSkins = new Dictionary<string, string>(EquippedSkins),
                CreatedTimestamp = CreatedTimestamp,
                LastModifiedTimestamp = LastModifiedTimestamp
            };
        }
    }
    
    /// <summary>
    /// 角色系统存档数据
    /// </summary>
    [Serializable]
    public class CharacterSaveData
    {
        /// <summary>
        /// 所有角色
        /// </summary>
        public List<CharacterData> Characters = new();
        
        /// <summary>
        /// 当前选中的角色ID
        /// </summary>
        public string CurrentCharacterId;
    }
}
