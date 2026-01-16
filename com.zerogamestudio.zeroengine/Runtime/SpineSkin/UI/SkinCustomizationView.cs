using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if SPINE_UNITY
using Spine.Unity;
#endif

namespace ZeroEngine.SpineSkin.UI
{
    /// <summary>
    /// 皮肤自定义/换装界面
    /// </summary>
    public class SkinCustomizationView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SpineSkinManager skinManager;
        [SerializeField] private CharacterManager characterManager;
        
        [Header("UI - Categories")]
        [SerializeField] private Transform categoryContainer;
        [SerializeField] private GameObject categoryButtonPrefab;
        
        [Header("UI - Skin List")]
        [SerializeField] private Transform skinListContainer;
        [SerializeField] private GameObject skinButtonPrefab;
        
        [Header("UI - Preview")]
#if SPINE_UNITY
        [SerializeField] private SkeletonGraphic skeletonPreview;
#endif
        [SerializeField] private TMP_Text currentSkinLabel;
        
        [Header("UI - Actions")]
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button genderToggleButton;
        [SerializeField] private TMP_Text genderLabel;
        
        // 缓存
        private readonly Dictionary<string, Button> categoryButtonCache = new();
        private readonly Dictionary<string, GameObject> skinItemCache = new();
        private string currentSlotId;
        private string initialGender;
        private Dictionary<string, string> initialSkins;
        
        // 事件
        public event Action OnConfirmed;
        public event Action OnCancelled;
        public event Action OnViewOpened;
        public event Action OnViewClosed;
        
        private SpineSkinConfig Config => skinManager?.Config;
        
        private void OnEnable()
        {
            // 保存初始状态（用于取消时恢复）
            SaveInitialState();
            
            BindEvents();
            GenerateCategoryButtons();
            OnViewOpened?.Invoke();
        }
        
        private void OnDisable()
        {
            UnbindEvents();
            OnViewClosed?.Invoke();
        }
        
        private void SaveInitialState()
        {
            if (skinManager == null) return;
            
            initialGender = skinManager.CurrentGender;
            initialSkins = skinManager.ExportEquippedSkins();
        }
        
        private void RestoreInitialState()
        {
            if (skinManager == null || initialSkins == null) return;
            
            skinManager.SetAllSkins(initialGender, initialSkins);
        }
        
        private void BindEvents()
        {
            if (confirmButton != null)
                confirmButton.onClick.AddListener(OnConfirmClicked);
            
            if (cancelButton != null)
                cancelButton.onClick.AddListener(OnCancelClicked);
            
            if (genderToggleButton != null)
                genderToggleButton.onClick.AddListener(OnGenderToggleClicked);
            
            if (skinManager != null)
                skinManager.OnSkinChanged += OnSkinChanged;
        }
        
        private void UnbindEvents()
        {
            if (confirmButton != null)
                confirmButton.onClick.RemoveListener(OnConfirmClicked);
            
            if (cancelButton != null)
                cancelButton.onClick.RemoveListener(OnCancelClicked);
            
            if (genderToggleButton != null)
                genderToggleButton.onClick.RemoveListener(OnGenderToggleClicked);
            
            if (skinManager != null)
                skinManager.OnSkinChanged -= OnSkinChanged;
        }
        
        /// <summary>
        /// 生成分类按钮
        /// </summary>
        private void GenerateCategoryButtons()
        {
            if (Config?.SkinSlots == null || categoryContainer == null) return;
            
            // 隐藏所有缓存的按钮
            foreach (var btn in categoryButtonCache.Values)
                btn.gameObject.SetActive(false);
            
            // 按排序顺序生成
            var sortedSlots = new List<SkinSlotConfig>(Config.SkinSlots);
            sortedSlots.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
            
            foreach (var slot in sortedSlots)
            {
                if (!categoryButtonCache.TryGetValue(slot.SlotId, out var button))
                {
                    if (categoryButtonPrefab == null) continue;
                    
                    var go = Instantiate(categoryButtonPrefab, categoryContainer);
                    button = go.GetComponent<Button>();
                    categoryButtonCache[slot.SlotId] = button;
                    
                    var slotId = slot.SlotId; // 捕获
                    button.onClick.AddListener(() => OnCategoryClicked(slotId));
                }
                
                button.gameObject.SetActive(true);
                
                // 设置图标和文本
                var icon = button.transform.Find("Icon")?.GetComponent<Image>();
                if (icon != null && slot.Icon != null)
                {
                    icon.sprite = slot.Icon;
                    icon.enabled = true;
                }
                
                var text = button.GetComponentInChildren<TMP_Text>();
                if (text != null)
                    text.text = slot.DisplayName;
            }
            
            // 默认选择第一个分类
            if (sortedSlots.Count > 0 && string.IsNullOrEmpty(currentSlotId))
            {
                OnCategoryClicked(sortedSlots[0].SlotId);
            }
        }
        
        /// <summary>
        /// 点击分类
        /// </summary>
        private void OnCategoryClicked(string slotId)
        {
            currentSlotId = slotId;
            
            // 更新分类按钮选中状态
            UpdateCategorySelection();
            
            // 生成该分类的皮肤列表
            GenerateSkinButtons(slotId);
        }
        
        private void UpdateCategorySelection()
        {
            foreach (var kvp in categoryButtonCache)
            {
                var isSelected = kvp.Key == currentSlotId;
                kvp.Value.interactable = !isSelected;
                
                // 可以添加更多视觉反馈
            }
        }
        
        /// <summary>
        /// 生成皮肤按钮
        /// </summary>
        private void GenerateSkinButtons(string slotId)
        {
            if (skinManager == null || skinListContainer == null) return;
            
            // 隐藏所有
            foreach (var item in skinItemCache.Values)
                item.SetActive(false);
            
            var availableSkins = skinManager.GetAvailableSkins(slotId);
            var slotConfig = Config?.GetSlotConfig(slotId);
            
            // 如果不是必须槽位，添加"空"选项
            if (slotConfig?.IsRequired != true)
            {
                ShowSkinItem("empty_" + slotId, null, slotId, isEmptyOption: true);
            }
            
            foreach (var skinName in availableSkins)
            {
                ShowSkinItem(skinName, skinName, slotId);
            }
            
            // 更新当前选中
            UpdateSkinSelection(slotId);
        }
        
        private void ShowSkinItem(string key, string skinName, string slotId, bool isEmptyOption = false)
        {
            if (!skinItemCache.TryGetValue(key, out var itemGO))
            {
                if (skinButtonPrefab == null) return;
                
                itemGO = Instantiate(skinButtonPrefab, skinListContainer);
                skinItemCache[key] = itemGO;
                
                var button = itemGO.GetComponent<Button>();
                if (button != null)
                {
                    var capturedSlotId = slotId;
                    var capturedSkinName = skinName;
                    var capturedIsEmpty = isEmptyOption;
                    
                    button.onClick.AddListener(() =>
                    {
                        if (capturedIsEmpty)
                            skinManager?.UnequipSkin(capturedSlotId);
                        else
                            skinManager?.EquipSkin(capturedSlotId, capturedSkinName);
                    });
                }
            }
            
            itemGO.SetActive(true);
            
            // 设置显示内容
            var text = itemGO.GetComponentInChildren<TMP_Text>();
            if (text != null)
            {
                if (isEmptyOption)
                {
                    text.text = "None";
                }
                else if (Config != null && Config.TryParseSkinName(skinName, out _, out _, out var name))
                {
                    text.text = name;
                }
                else
                {
                    text.text = skinName;
                }
            }
            
            // 空选项显示特殊图标
            if (isEmptyOption)
            {
                var icon = itemGO.transform.Find("Icon")?.GetComponent<Image>();
                if (icon != null && Config?.EmptySlotIcon != null)
                {
                    icon.sprite = Config.EmptySlotIcon;
                    icon.enabled = true;
                }
            }
        }
        
        private void UpdateSkinSelection(string slotId)
        {
            var equippedSkin = skinManager?.GetEquippedSkin(slotId);
            
            foreach (var kvp in skinItemCache)
            {
                if (!kvp.Value.activeSelf) continue;
                
                bool isSelected;
                if (kvp.Key.StartsWith("empty_"))
                {
                    isSelected = string.IsNullOrEmpty(equippedSkin);
                }
                else
                {
                    isSelected = kvp.Key == equippedSkin;
                }
                
                // 更新选中视觉
                var indicator = kvp.Value.transform.Find("SelectionIndicator");
                if (indicator != null)
                    indicator.gameObject.SetActive(isSelected);
            }
            
            // 更新标签
            UpdateCurrentSkinLabel(slotId, equippedSkin);
        }
        
        private void UpdateCurrentSkinLabel(string slotId, string skinName)
        {
            if (currentSkinLabel == null) return;
            
            var slotConfig = Config?.GetSlotConfig(slotId);
            var slotDisplayName = slotConfig?.DisplayName ?? slotId;
            
            string skinDisplayName;
            if (string.IsNullOrEmpty(skinName))
            {
                skinDisplayName = "None";
            }
            else if (Config != null && Config.TryParseSkinName(skinName, out _, out _, out var name))
            {
                skinDisplayName = name;
            }
            else
            {
                skinDisplayName = skinName;
            }
            
            currentSkinLabel.text = $"{slotDisplayName}: {skinDisplayName}";
        }
        
        private void OnSkinChanged(string slotId, string skinName)
        {
            if (slotId == currentSlotId)
            {
                UpdateSkinSelection(slotId);
            }
            
            // 更新预览
            UpdatePreview();
        }
        
        private void UpdatePreview()
        {
#if SPINE_UNITY
            if (skeletonPreview == null || skinManager == null) return;
            
            // 如果预览使用独立的SkeletonGraphic，需要同步皮肤
            // 这里假设预览与主角色共享SpineSkinManager
            skeletonPreview.UpdateMesh();
#endif
        }
        
        private void OnGenderToggleClicked()
        {
            if (skinManager == null || Config?.GenderNames == null) return;
            
            var genders = Config.GenderNames;
            if (genders.Count < 2) return;
            
            var currentIndex = genders.IndexOf(skinManager.CurrentGender);
            var nextIndex = (currentIndex + 1) % genders.Count;
            
            skinManager.SetGender(genders[nextIndex]);
            
            if (genderLabel != null)
                genderLabel.text = genders[nextIndex];
            
            // 重新生成皮肤列表（因为可用皮肤可能因性别而变化）
            GenerateSkinButtons(currentSlotId);
        }
        
        private void OnConfirmClicked()
        {
            // 保存到当前角色
            characterManager?.SaveCurrentCharacterSkins();
            
            OnConfirmed?.Invoke();
        }
        
        private void OnCancelClicked()
        {
            // 恢复初始状态
            RestoreInitialState();
            
            OnCancelled?.Invoke();
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (skinManager == null)
                skinManager = GetComponentInParent<SpineSkinManager>();
            if (characterManager == null)
                characterManager = GetComponentInParent<CharacterManager>();
        }
#endif
    }
}
