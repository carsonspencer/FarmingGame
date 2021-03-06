using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIInventoryBar : MonoBehaviour
{
    [SerializeField] private Sprite blank16x16sprite = null;
    [SerializeField] private UIInventorySlot[] inventorySlot = null; // This is populated in-editor with the 12 inventory slot UI gameObjects
    public GameObject inventoryBarDraggedItem;

    [HideInInspector] public GameObject inventoryTextBoxGameObject;

    private RectTransform rectTransform;

    private bool _isInventoryBarPositionBottom = true;

    public bool IsInventoryBarPositionBottom {get => _isInventoryBarPositionBottom; set => _isInventoryBarPositionBottom = value;}


    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }


    private void OnDisable()
    {
        // When this item is disabled, we will unsubscribe to the InventoryUpdatedEvent
        EventHandler.InventoryUpdatedEvent -= InventoryUpdated;
    }


    private void OnEnable()
    {
        // When this item is enabled, we will subscribe to the InventoryUpdatedEvent so we will catch it every time it is triggered
        EventHandler.InventoryUpdatedEvent += InventoryUpdated;
    }


    private void ClearInventorySlots()
    {
        if (inventorySlot.Length > 0)
        {
            // Loop through the inventory slots and update with the blank sprite empty string, null item details, and 0 quantity
            for (int i = 0; i < inventorySlot.Length; i++)
            {
                inventorySlot[i].inventorySlotImage.sprite = blank16x16sprite;
                inventorySlot[i].textMeshProUGUI.text = "";
                inventorySlot[i].itemDetails = null;
                inventorySlot[i].itemQuantity = 0;

                // Clear the highlight on the spot if it isn't selected
                SetHighlightedInventorySlots(i);
            }
        }
    }


    private void InventoryUpdated(InventoryLocation inventoryLocation, List<InventoryItem> inventoryList)
    {
        if (inventoryLocation == InventoryLocation.player)
        {
            ClearInventorySlots();

            // If the inventorySlot list (populated in the editor with the 12 UI inventory slot gameObjects) is greater than 0, and there are items in the inventory list,
            if (inventorySlot.Length > 0 && inventoryList.Count > 0)
            {
                // Loop through inventory slots and update with corresponding inventory list item, as long as the current slot is less than the total items in the inventory list
                for (int i = 0; i < inventorySlot.Length; i++)
                {
                    if (i < inventoryList.Count)
                    {
                        int itemCode = inventoryList[i].itemCode;

                        ItemDetails itemDetails = InventoryManager.Instance.GetItemDetails(itemCode);

                        if (itemDetails != null)
                        {
                            // Add the image, text, details, and quantity to the inventory item slot
                            inventorySlot[i].inventorySlotImage.sprite = itemDetails.itemSprite;
                            inventorySlot[i].textMeshProUGUI.text = inventoryList[i].itemQuantity.ToString();
                            inventorySlot[i].itemDetails = itemDetails;
                            inventorySlot[i].itemQuantity = inventoryList[i].itemQuantity;

                            // Set the highlight on the the updated inventory slots if they are supposed to be highlighted.
                            SetHighlightedInventorySlots(i);
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }
    }


    private void Update()
    {
        // Switch the inventory bar position depending on the players position
        SwitchInventoryBarPosition();
    }


    /// <summary>
    /// Clear all of the highlights from the inventory bar
    /// </summary>
    public void ClearHighlightOnInventorySlots()
    {
        // inventorySlot is the list of 12 inventory slots we added in editor
        if (inventorySlot.Length > 0)
        {
            // Loop through all the inventory slots and set the highlight sprites to 0 alpha 
            for (int i = 0; i < inventorySlot.Length; i++)
            {
                if (inventorySlot[i].isSelected)
                {
                    inventorySlot[i].isSelected = false;
                    inventorySlot[i].inventorySlotHighlight.color = new Color(0f, 0f, 0f, 0f);

                    // Update the inventory to show the item as not selected
                    InventoryManager.Instance.ClearSelectedInventoryItem(InventoryLocation.player);
                }
            }
        }
    }


    /// <summary>
    /// Set the selected highlight if set on all inventory item positions
    /// </summary>
    public void SetHighlightedInventorySlots()
    {
        if (inventorySlot.Length > 0)
        {
            // Loop through inventory slots (added all 12 in the editor) and clear the highlight sprites
            for (int i = 0; i < inventorySlot.Length; i++)
            {
                SetHighlightedInventorySlots(i);
            }
        }
    }


    /// <summary>
    /// Overloaded method to set the selected highlight if set on an inventory item for a given slot position
    /// </summary>
    public void SetHighlightedInventorySlots(int itemPosition)
    {
        // If there are inventory slots (12 added in the editor), and the current slot has an item that is not null, and the slot is selected, change the highlight color to alpha = 1
        if (inventorySlot.Length > 0 && inventorySlot[itemPosition].itemDetails != null)
        {
            if (inventorySlot[itemPosition].isSelected)
            {
                inventorySlot[itemPosition].inventorySlotHighlight.color = new Color(1f, 1f, 1f, 1f);

                // Update the inventory list to show item as selected
                InventoryManager.Instance.SetSelectedInventoryItem(InventoryLocation.player, inventorySlot[itemPosition].itemDetails.itemCode);
            }
        }
    }


    private void SwitchInventoryBarPosition()
    {
        // This vector is the players position on the camera field of view (not world coords), as computed in the Player class
        Vector3 playerViewportPosition = Player.Instance.GetPlayerViewportPosition();

        // Check if the player's viewport position is at the bottom of the screen or not. If it is, move the UI bar to the top of the viewport. 
        // Else, move it (keep it) to the bottom
        if (playerViewportPosition.y > 0.3f && IsInventoryBarPositionBottom == false)
        {
            // These rectTransform values are just the default ones we set up in the editor
            rectTransform.pivot = new Vector2(0.5f, 0f);
            rectTransform.anchorMin = new Vector2(0.5f, 0f);
            rectTransform.anchorMax = new Vector2(0.5f, 0f);
            rectTransform.anchoredPosition = new Vector2(0f, 2.5f);

            IsInventoryBarPositionBottom = true;
        }
        else if (playerViewportPosition.y <= 0.3f && IsInventoryBarPositionBottom == true)
        {
            // These rectTransform values are for the top of the screen instead
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.anchorMin = new Vector2(0.5f, 1f);
            rectTransform.anchorMax = new Vector2(0.5f, 1f);
            rectTransform.anchoredPosition = new Vector2(0f, -2.5f);

            IsInventoryBarPositionBottom = false;
        }
    }


    // This method will simply destroy all dragged objects, i.e. when we enter the pause menu
    public void DestroyCurrentlyDraggedItems()
    {
        // Loop through all of the inventory slots
        for (int i = 0; i < inventorySlot.Length; i++)
        {
            // If the current slot has an item currently being dragged, destroy the dragged item
            if (inventorySlot[i].draggedItem != null)
            {
                Destroy(inventorySlot[i].draggedItem);
            }
        }
    }


    // This method will clear all of the currently selected items on the inventory slot, for e.g. when we enter the pause menu
    public void ClearCurrentlySelectedItems()
    {
        // Loop through all of the inventory slots
        for (int i = 0; i < inventorySlot.Length; i++)
        {
            // Clear the selected items on each slot
            inventorySlot[i].ClearSelectedItem();
        }
    }
}
