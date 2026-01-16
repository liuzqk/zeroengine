using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ZeroEngine.SpineSkin.UI
{
    /// <summary>
    /// 角色选择界面
    /// </summary>
    public class CharacterSelectView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CharacterManager characterManager;
        
        [Header("UI - Character List")]
        [SerializeField] private Transform characterListContainer;
        [SerializeField] private GameObject characterItemPrefab;
        
        [Header("UI - Buttons")]
        [SerializeField] private Button createCharacterButton;
        [SerializeField] private Button confirmButton;
        
        [Header("UI - Preview")]
        [SerializeField] private TMP_Text selectedCharacterName;
        
        [Header("Settings")]
        [SerializeField] private bool autoSelectFirstCharacter = true;
        
        // 缓存的UI元素
        private readonly Dictionary<string, GameObject> characterItemCache = new();
        private string selectedCharacterId;
        
        // 事件
        public event Action OnCreateCharacterRequested;
        public event Action<CharacterData> OnCharacterConfirmed;
        public event Action OnViewOpened;
        public event Action OnViewClosed;
        
        private void OnEnable()
        {
            BindEvents();
            RefreshCharacterList();
            OnViewOpened?.Invoke();
        }
        
        private void OnDisable()
        {
            UnbindEvents();
            OnViewClosed?.Invoke();
        }
        
        private void BindEvents()
        {
            if (createCharacterButton != null)
                createCharacterButton.onClick.AddListener(OnCreateCharacterClicked);
            
            if (confirmButton != null)
                confirmButton.onClick.AddListener(OnConfirmClicked);
            
            if (characterManager != null)
            {
                characterManager.OnCharacterCreated += OnCharacterCreated;
                characterManager.OnCharacterDeleted += OnCharacterDeleted;
                characterManager.OnCharactersLoaded += RefreshCharacterList;
            }
        }
        
        private void UnbindEvents()
        {
            if (createCharacterButton != null)
                createCharacterButton.onClick.RemoveListener(OnCreateCharacterClicked);
            
            if (confirmButton != null)
                confirmButton.onClick.RemoveListener(OnConfirmClicked);
            
            if (characterManager != null)
            {
                characterManager.OnCharacterCreated -= OnCharacterCreated;
                characterManager.OnCharacterDeleted -= OnCharacterDeleted;
                characterManager.OnCharactersLoaded -= RefreshCharacterList;
            }
        }
        
        /// <summary>
        /// 刷新角色列表
        /// </summary>
        public void RefreshCharacterList()
        {
            if (characterManager == null || characterListContainer == null) return;
            
            // 隐藏所有缓存的项
            foreach (var item in characterItemCache.Values)
                item.SetActive(false);
            
            // 显示角色
            foreach (var character in characterManager.Characters)
            {
                ShowCharacterItem(character);
            }
            
            // 更新创建按钮状态
            UpdateCreateButtonState();
            
            // 自动选择
            if (autoSelectFirstCharacter && string.IsNullOrEmpty(selectedCharacterId) && characterManager.HasAnyCharacter)
            {
                SelectCharacter(characterManager.Characters[0].Id);
            }
        }
        
        private void ShowCharacterItem(CharacterData character)
        {
            if (!characterItemCache.TryGetValue(character.Id, out var itemGO))
            {
                if (characterItemPrefab == null) return;
                
                itemGO = Instantiate(characterItemPrefab, characterListContainer);
                characterItemCache[character.Id] = itemGO;
                
                // 绑定点击事件
                var button = itemGO.GetComponent<Button>();
                if (button != null)
                {
                    var id = character.Id; // 捕获变量
                    button.onClick.AddListener(() => SelectCharacter(id));
                }
                
                // 绑定删除按钮
                var deleteButton = itemGO.transform.Find("DeleteButton")?.GetComponent<Button>();
                if (deleteButton != null)
                {
                    var id = character.Id;
                    deleteButton.onClick.AddListener(() => OnDeleteCharacterClicked(id));
                }
            }
            
            itemGO.SetActive(true);
            
            // 更新显示
            var nameText = itemGO.GetComponentInChildren<TMP_Text>();
            if (nameText != null)
                nameText.text = character.Name;
            
            // 更新选中状态
            UpdateItemSelectionVisual(character.Id, itemGO);
        }
        
        private void UpdateItemSelectionVisual(string characterId, GameObject itemGO)
        {
            var isSelected = characterId == selectedCharacterId;
            
            // 可以通过CanvasGroup、Image颜色等方式表示选中状态
            var selectionIndicator = itemGO.transform.Find("SelectionIndicator");
            if (selectionIndicator != null)
                selectionIndicator.gameObject.SetActive(isSelected);
        }
        
        /// <summary>
        /// 选择角色
        /// </summary>
        public void SelectCharacter(string characterId)
        {
            selectedCharacterId = characterId;
            
            // 更新所有项的选中状态
            foreach (var kvp in characterItemCache)
            {
                UpdateItemSelectionVisual(kvp.Key, kvp.Value);
            }
            
            // 更新预览
            var character = characterManager?.GetCharacter(characterId);
            if (selectedCharacterName != null && character != null)
            {
                selectedCharacterName.text = character.Name;
            }
            
            // 更新确认按钮
            if (confirmButton != null)
                confirmButton.interactable = character != null;
        }
        
        private void UpdateCreateButtonState()
        {
            if (createCharacterButton != null && characterManager != null)
            {
                createCharacterButton.interactable = characterManager.CanCreateCharacter;
            }
        }
        
        private void OnCreateCharacterClicked()
        {
            OnCreateCharacterRequested?.Invoke();
        }
        
        private void OnConfirmClicked()
        {
            if (string.IsNullOrEmpty(selectedCharacterId)) return;
            
            var character = characterManager?.GetCharacter(selectedCharacterId);
            if (character != null)
            {
                characterManager.SelectCharacter(character);
                OnCharacterConfirmed?.Invoke(character);
            }
        }
        
        private void OnDeleteCharacterClicked(string characterId)
        {
            // 这里可以弹出确认对话框
            characterManager?.DeleteCharacter(characterId);
        }
        
        private void OnCharacterCreated(CharacterData character)
        {
            RefreshCharacterList();
            SelectCharacter(character.Id);
        }
        
        private void OnCharacterDeleted(string characterId)
        {
            if (characterItemCache.TryGetValue(characterId, out var item))
            {
                Destroy(item);
                characterItemCache.Remove(characterId);
            }
            
            if (selectedCharacterId == characterId)
            {
                selectedCharacterId = null;
                if (characterManager.HasAnyCharacter)
                    SelectCharacter(characterManager.Characters[0].Id);
            }
            
            UpdateCreateButtonState();
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (characterManager == null)
                characterManager = GetComponentInParent<CharacterManager>();
        }
#endif
    }
}
