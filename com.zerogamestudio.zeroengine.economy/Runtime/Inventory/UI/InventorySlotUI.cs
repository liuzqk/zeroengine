using System;
using UnityEngine;
using UnityEngine.UI;
// Reusing MVVM system or simple Mono?
// Since we have MVVM, let's try to stick to standards, but for "Core migration" simple UI is often easier to debug.
// However, I should use the Event System to refresh.

namespace ZeroEngine.Inventory.UI
{
    public class InventorySlotUI : MonoBehaviour
    {
        public Image IconImage;
        public Text AmountText;
        public Button ClickButton;
        
        private InventorySlot _slot;

        public void Bind(InventorySlot slot)
        {
            _slot = slot;
            Refresh();
        }

        public void Refresh()
        {
            if (_slot == null || _slot.IsEmpty)
            {
                IconImage.gameObject.SetActive(false);
                AmountText.text = "";
                ClickButton.interactable = false;
            }
            else
            {
                IconImage.gameObject.SetActive(true);
                if (_slot.ItemData != null)
                {
                    IconImage.sprite = _slot.ItemData.Icon;
                }
                
                AmountText.text = _slot.Amount > 1 ? _slot.Amount.ToString() : "";
                ClickButton.interactable = true;
            }
        }
        
        // Drag Drop logic would go here
    }
}
