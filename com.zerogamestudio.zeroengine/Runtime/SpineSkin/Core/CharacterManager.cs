using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ZeroEngine.SpineSkin
{
    /// <summary>
    /// 多角色管理器 - 负责角色的创建、切换和存储
    /// </summary>
    public class CharacterManager : MonoBehaviour
    {
        [SerializeField] private SpineSkinConfig config;
        [SerializeField] private SpineSkinManager skinManager;
        
        private readonly List<CharacterData> characters = new();
        private CharacterData currentCharacter;
        
        /// <summary>
        /// 配置
        /// </summary>
        public SpineSkinConfig Config => config;
        
        /// <summary>
        /// 皮肤管理器
        /// </summary>
        public SpineSkinManager SkinManager => skinManager;
        
        /// <summary>
        /// 当前选中的角色
        /// </summary>
        public CharacterData CurrentCharacter => currentCharacter;
        
        /// <summary>
        /// 所有角色
        /// </summary>
        public IReadOnlyList<CharacterData> Characters => characters;
        
        /// <summary>
        /// 是否可以创建新角色
        /// </summary>
        public bool CanCreateCharacter => 
            config == null || config.MaxCharacterCount <= 0 || characters.Count < config.MaxCharacterCount;
        
        /// <summary>
        /// 最大角色数量
        /// </summary>
        public int MaxCharacterCount => config?.MaxCharacterCount ?? 0;
        
        // 事件
        public event Action<CharacterData> OnCharacterCreated;
        public event Action<CharacterData> OnCharacterSelected;
        public event Action<string> OnCharacterDeleted;
        public event Action OnCharactersLoaded;
        
        private void Awake()
        {
            if (skinManager == null)
                skinManager = GetComponent<SpineSkinManager>();
        }
        
        /// <summary>
        /// 创建新角色
        /// </summary>
        /// <param name="name">角色名称</param>
        /// <param name="gender">性别（可选，默认使用配置的默认性别）</param>
        /// <returns>创建的角色数据，如果达到上限返回null</returns>
        public CharacterData CreateCharacter(string name, string gender = null)
        {
            if (!CanCreateCharacter)
            {
                Debug.LogWarning($"[CharacterManager] Cannot create character: max count ({MaxCharacterCount}) reached.");
                return null;
            }
            
            gender ??= config?.DefaultGender ?? "Female";
            
            var character = new CharacterData(name, gender);
            
            // 初始化默认皮肤
            if (config?.SkinSlots != null)
            {
                foreach (var slot in config.SkinSlots)
                {
                    if (!string.IsNullOrEmpty(slot.DefaultSkin))
                    {
                        character.EquippedSkins[slot.SlotId] = slot.DefaultSkin;
                    }
                }
            }
            
            characters.Add(character);
            OnCharacterCreated?.Invoke(character);
            
            return character;
        }
        
        /// <summary>
        /// 选择角色（切换当前角色）
        /// </summary>
        public void SelectCharacter(string characterId)
        {
            var character = characters.Find(c => c.Id == characterId);
            if (character == null)
            {
                Debug.LogWarning($"[CharacterManager] Character not found: {characterId}");
                return;
            }
            
            SelectCharacter(character);
        }
        
        /// <summary>
        /// 选择角色（切换当前角色）
        /// </summary>
        public void SelectCharacter(CharacterData character)
        {
            if (character == null) return;
            
            currentCharacter = character;
            
            // 应用角色的皮肤到SkinManager
            if (skinManager != null)
            {
                skinManager.SetAllSkins(character.Gender, character.EquippedSkins);
            }
            
            OnCharacterSelected?.Invoke(character);
        }
        
        /// <summary>
        /// 删除角色
        /// </summary>
        public bool DeleteCharacter(string characterId)
        {
            var index = characters.FindIndex(c => c.Id == characterId);
            if (index < 0) return false;
            
            characters.RemoveAt(index);
            
            // 如果删除的是当前角色，清空当前角色
            if (currentCharacter?.Id == characterId)
            {
                currentCharacter = characters.FirstOrDefault();
                if (currentCharacter != null)
                    SelectCharacter(currentCharacter);
            }
            
            OnCharacterDeleted?.Invoke(characterId);
            return true;
        }
        
        /// <summary>
        /// 保存当前角色的皮肤更改
        /// </summary>
        public void SaveCurrentCharacterSkins()
        {
            if (currentCharacter == null || skinManager == null) return;
            
            currentCharacter.Gender = skinManager.CurrentGender;
            currentCharacter.EquippedSkins = skinManager.ExportEquippedSkins();
            currentCharacter.MarkModified();
        }
        
        /// <summary>
        /// 更新角色名称
        /// </summary>
        public void UpdateCharacterName(string characterId, string newName)
        {
            var character = characters.Find(c => c.Id == characterId);
            if (character != null)
            {
                character.Name = newName;
                character.MarkModified();
            }
        }
        
        /// <summary>
        /// 导出存档数据
        /// </summary>
        public CharacterSaveData ExportSaveData()
        {
            // 保存当前角色的最新状态
            SaveCurrentCharacterSkins();
            
            return new CharacterSaveData
            {
                Characters = characters.Select(c => c.Clone()).ToList(),
                CurrentCharacterId = currentCharacter?.Id
            };
        }
        
        /// <summary>
        /// 导入存档数据
        /// </summary>
        public void ImportSaveData(CharacterSaveData saveData)
        {
            characters.Clear();
            currentCharacter = null;
            
            if (saveData?.Characters != null)
            {
                characters.AddRange(saveData.Characters);
                
                if (!string.IsNullOrEmpty(saveData.CurrentCharacterId))
                {
                    var current = characters.Find(c => c.Id == saveData.CurrentCharacterId);
                    if (current != null)
                        SelectCharacter(current);
                }
            }
            
            OnCharactersLoaded?.Invoke();
        }
        
        /// <summary>
        /// 获取角色
        /// </summary>
        public CharacterData GetCharacter(string characterId)
        {
            return characters.Find(c => c.Id == characterId);
        }
        
        /// <summary>
        /// 是否有任何角色
        /// </summary>
        public bool HasAnyCharacter => characters.Count > 0;
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (skinManager == null)
                skinManager = GetComponent<SpineSkinManager>();
        }
#endif
    }
}
