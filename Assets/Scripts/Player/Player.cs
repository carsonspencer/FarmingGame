﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : SingletonMonobehaviour<Player>
{
    // Prefab for the tree used to test the pool manager
    // public GameObject canyonOakTreePrefab;

    // The pause after using the tool/lifting tool/picking animation before we can use the tool or walk again
    private WaitForSeconds afterUseToolAnimationPause;
    private WaitForSeconds afterLiftToolAnimationPause;
    private WaitForSeconds afterPickAnimationPause;

    // The pause while using the tool/lifting tool/picking animation before we can use the tool or walk again, which corresponds to the animation time of using the tools
    private WaitForSeconds useToolAnimationPause;
    private WaitForSeconds liftToolAnimationPause;
    private WaitForSeconds pickAnimationPause;

    // This will hold our animation overrides
    private AnimationOverrides animationOverrides;

    // This is the grid cursor and cursor for valid/invalid item drops
    private GridCursor gridCursor;
    private Cursor cursor;


    // Movement Parameters
    public float xInput;
    public float yInput;
    public bool isWalking;
    public bool isRunning;
    public bool isIdle;
    public bool isCarrying = false;
    public bool isUsingToolRight;
    public bool isUsingToolLeft;
    public bool isUsingToolUp;
    public bool isUsingToolDown;
    public bool isLiftingToolRight;
    public bool isLiftingToolLeft;
    public bool isLiftingToolUp;
    public bool isLiftingToolDown;
    public bool isSwingingToolRight;
    public bool isSwingingToolLeft;
    public bool isSwingingToolUp;
    public bool isSwingingToolDown;
    public bool isPickingRight;
    public bool isPickingLeft;
    public bool isPickingUp;
    public bool isPickingDown;
    public bool idleUp;
    public bool idleDown;
    public bool idleLeft;
    public bool idleRight;
    private ToolEffect toolEffect = ToolEffect.none;

    private Camera mainCamera;

    // Bool is enabled while the tool is in motion, so we can't do other stuff
    private bool playerToolUseDisabled = false;

    private Rigidbody2D rigidBody2D;

#pragma warning disable 414
    private Direction playerDirection;
#pragma warning restore 414

    // List for characterAttribute Structs that we want to swap animations for. This is what we pass into the AnimationOverride
    private List<CharacterAttribute> characterAttributeCustomisationList;

    private float movementSpeed;

    // Serialized field which we will populate with a prefab to show equipped item above a players head.
    [Tooltip("Should be populated in the prefab with the equipped item sprite rendered")]
    [SerializeField] private SpriteRenderer equippedItemSpriteRenderer = null;

    // Player attributes that can be swapped
    private CharacterAttribute armsCharacterAttribute;
    private CharacterAttribute toolCharacterAttribute;

    private bool _playerInputIsDisabled = false;

    public bool PlayerInputIsDisabled {get => _playerInputIsDisabled; set => _playerInputIsDisabled = value;}

    protected override void Awake()
    {
        base.Awake();

        rigidBody2D = GetComponent<Rigidbody2D>();

        // This will get all of the AnimationOverrides found in the children of player (arm, leg, etc)
        animationOverrides = GetComponentInChildren<AnimationOverrides>();

        // initialize our swappable character attributes (a struct) from the enums for the body part, the color, and the type. Do this for the arms 
        // animation override, and the hoe
        armsCharacterAttribute = new CharacterAttribute(CharacterPartAnimator.arms, PartVariantColor.none, PartVariantType.none);
        toolCharacterAttribute = new CharacterAttribute(CharacterPartAnimator.tool, PartVariantColor.none, PartVariantType.hoe);

        // Initialize the list of character attributes
        characterAttributeCustomisationList = new List<CharacterAttribute>();

        // Get reference to the main camera
        mainCamera = Camera.main;
    }


    // Subscribe the DisablePlayerInputAndResetMovement method to the BeforeSceneUnloadFadeOutEvent, so that before the scene starts to fade out,
    // We will reset the players movement and disable further input so they stay still while moving between scenes. Also subscribe the 
    // EnablePlayerInput method to the AfterSceneLoadFadeInEvent, so that once the new scene has faded in, we can move again
    private void OnEnable()
    {
        EventHandler.BeforeSceneUnloadFadeOutEvent += DisablePlayerInputAndResetMovement;
        EventHandler.AfterSceneLoadFadeInEvent += EnablePlayerInput;
    }


    private void OnDisable()
    {
        EventHandler.BeforeSceneUnloadFadeOutEvent -= DisablePlayerInputAndResetMovement;
        EventHandler.AfterSceneLoadFadeInEvent -= EnablePlayerInput;
    }


    // Populate the gridCursor variable with the game object found in fame!
    private void Start()
    {
        // Populates the gridcursor and cursor member variables
        gridCursor = FindObjectOfType<GridCursor>();
        cursor = FindObjectOfType<Cursor>();

        // Populated the tool/ lifting tool animation pauses using the settings file members
        useToolAnimationPause = new WaitForSeconds(Settings.useToolAnimationPause);
        liftToolAnimationPause = new WaitForSeconds(Settings.liftToolAnimationPause);
        pickAnimationPause = new WaitForSeconds(Settings.pickAnimationPause);

        afterUseToolAnimationPause = new WaitForSeconds(Settings.afterUseToolAnimationPause);
        afterLiftToolAnimationPause = new WaitForSeconds(Settings.afterLiftToolAnimationPause);
        afterPickAnimationPause = new WaitForSeconds(Settings.afterPickAnimationPause);
    }


    private void Update()
    {
        #region Player Input 

        if (!PlayerInputIsDisabled)
        {
            // Reset all the animation triggers to default to make sure
            ResetAnimationTriggers();

            // Take the keyboard input and occupy (xInput, yInput) with it, and determine player direction 
            PlayerMovementInput();

            // Check whether the player is walking (shift) or running
            PlayerWalkInput();
            
            // Player click to drop items
            PlayerClickInput();

            // Check if the testing keys for advancing game time have been pressed!
            PlayerTestInput();


            // From above two calls, we have xInput, yInput, movementSpeed, and isRunning, isWalking, and isIdle.
            // Now, send event info to delegate so any listeners will recieve player movement input
            EventHandler.CallMovementEvent(xInput, yInput, isWalking, isRunning, isIdle, isCarrying, toolEffect, 
                isUsingToolRight, isUsingToolLeft, isUsingToolUp, isUsingToolDown, 
                isLiftingToolRight, isLiftingToolLeft, isLiftingToolUp, isLiftingToolDown, 
                isPickingLeft, isPickingRight, isPickingUp, isPickingDown, 
                isSwingingToolRight, isSwingingToolLeft, isSwingingToolUp, isSwingingToolDown, 
                false, false, false, false);
        }

        #endregion
    }


    // Fixed update is for physics systems
    private void FixedUpdate()
    {   
        // Physically move the player!
        PlayerMovement();
    }


    private void PlayerMovement()
    {   
        // Calculate the new position to update each frame (Time.deltaTime is the FixedUpdate cycle time)
        Vector2 move = new Vector2(xInput * movementSpeed * Time.deltaTime, yInput * movementSpeed * Time.deltaTime);

        rigidBody2D.MovePosition(rigidBody2D.position + move);
    }


    private void ResetAnimationTriggers()
    {
        isPickingRight = false;
        isPickingLeft = false;
        isPickingUp = false;
        isPickingDown = false;
        isUsingToolRight = false;
        isUsingToolLeft = false;
        isUsingToolUp = false;
        isUsingToolDown = false;
        isLiftingToolRight = false;
        isLiftingToolLeft = false;
        isLiftingToolUp = false;
        isLiftingToolDown = false;
        isSwingingToolRight = false;
        isSwingingToolLeft = false;
        isSwingingToolUp = false;
        isSwingingToolDown = false;
        toolEffect = ToolEffect.none;
    }


    private void PlayerMovementInput()
    {   
        // These return only -1, 0, 1
        yInput = Input.GetAxisRaw("Vertical");
        xInput = Input.GetAxisRaw("Horizontal");

        if (yInput != 0 && xInput != 0)
        {
            xInput = xInput * 0.71f;
            yInput = yInput * 0.71f;
        }

        if (xInput != 0 || yInput != 0)
        {
            isRunning = true;
            isWalking = false;
            isIdle = false;

            movementSpeed = Settings.runningSpeed;

            // Capture player direction for save game
            if (xInput < 0)
            {
                playerDirection = Direction.left;
            }
            else if (xInput > 0)
            {
                playerDirection = Direction.right;
            }
            else if (yInput < 0)
            {
                playerDirection = Direction.down;
            }
            else
            {
                playerDirection = Direction.up;
            }
        }
        else if (xInput == 0 && yInput == 0)
        {
            isRunning = false;
            isWalking = false;
            isIdle = true;
        }

    }


    private void PlayerWalkInput()
    {
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            isRunning = false;
            isWalking = true;
            isIdle = false;
            movementSpeed = Settings.walkingSpeed;
        }
        else
        {
            isRunning = true;
            isWalking = false;
            isIdle = false;
            movementSpeed = Settings.runningSpeed;
        }
    }


    // See if the left mouse button is clicked. If so, if the gridCursor is currently enabled (i.e. an item is selected), we will process the input
    private void PlayerClickInput()
    {
        // Can't do click inputs (like using tool again) if the tool use is disabled! Wait for the enimation pause to be over before it's enabled again
        if (!playerToolUseDisabled)
        {
            if (Input.GetMouseButton(0))
            {   
                // Process the input if either the grid cursor is enabled (things like hoeing, watering squares), or the cursor is enabled (like reaping)
                if (gridCursor.CursorIsEnabled || cursor.CursorIsEnabled)
                {                 
                    // Get the cursor grid position
                    Vector3Int cursorGridPosition = gridCursor.GetGridPositionForCursor();

                    // Get the player's grid position
                    Vector3Int playerGridPosition = gridCursor.GetGridPositionForPlayer();

                    ProcessPlayerClickInput(cursorGridPosition, playerGridPosition);
                }
            }
        }
    }


    // Process the players click input - whether it be dropping a seed/colmmodity, using a tool, etc. For tools, we need to calculate the direction we want to use it in for the
    // proper animation
    private void ProcessPlayerClickInput(Vector3Int cursorGridPosition, Vector3Int playerGridPosition)
    {   
        // Reset the players movement
        ResetMovement();

        // Find the direction the player needs to face to click on the given cursor location
        Vector3Int playerDirection = GetPlayerClickDirection(cursorGridPosition, playerGridPosition);

        // Get the GridPropertyDetails at the cursor position (the GridCursor validation routine ensures that the grid property details are not null)
        GridPropertyDetails gridPropertyDetails = GridPropertiesManager.Instance.GetGridPropertyDetails(cursorGridPosition.x, cursorGridPosition.y);

        // Get the selected items ItemDetails
        ItemDetails itemDetails = InventoryManager.Instance.GetSelectedInventoryItemDetails(InventoryLocation.player);

        // If the itemDetails aren't null (i.e. nothing selected), check the itemType for Seed, Commodity, hoeing_tool, watering_tool, or none/count.
        if (itemDetails != null)
        {
            switch (itemDetails.itemType)
            {   
                // If it's a seed, check if it can be dropped, and if the current cursor position is valid. If so, publish an event so subscribers can see it
                case ItemType.Seed:
                    if (Input.GetMouseButtonDown(0))
                    {
                        ProcessPlayerClickInputSeed(gridPropertyDetails, itemDetails);
                    }
                    break;
                
                // Same for commodities
                case ItemType.Commodity:
                    if (Input.GetMouseButtonDown(0))
                    {
                        ProcessPlayerClickInputCommodity(itemDetails);
                    }
                    break;

                // If it's a hoeing/watering/reaping/collecting tool, we use the ProcessPlayerClickInputTool method, which checks which tool is being used. If it's a 
                // hoeing_tool/watering_tool/Reaping_tool/Collecting_tool and if the cursor position is valid, we will execute the player use hoe/water/reap/pick sequence - 
                // which runs the hoeing/watering/reaping/picking animation in the correct player direction, marks the ground gridPropertyDetails as dug/watered/reaped/picked, 
                // updates the soil sprite to dug/watered/collected, etc. (or destroys the reapableScenary)
                case ItemType.Watering_tool:
                case ItemType.Hoeing_tool:
                case ItemType.Reaping_tool:
                case ItemType.Collecting_tool:
                    if (Input.GetMouseButtonDown(0))
                    {
                        ProcessPlayerClickInputTool(gridPropertyDetails, itemDetails, playerDirection);
                    }
                    break;

                case ItemType.none:
                    break;

                case ItemType.count:
                    break;

                default:
                    break;
            }
        }
    }


    // Given the cursor grid position and the player's grid position, calculate which direction the player needs to use the tool in.
    private Vector3Int GetPlayerClickDirection(Vector3Int cursorGridPosition, Vector3Int playerGridPosition)
    {
        if (cursorGridPosition.x > playerGridPosition.x)
        {
            return Vector3Int.right;
        }

        else if (cursorGridPosition.x < playerGridPosition.x)
        {
            return Vector3Int.left;
        }

        else if (cursorGridPosition.y > playerGridPosition.y)
        {
            return Vector3Int.up;
        }

        else
        {
            return Vector3Int.down;
        }
    }


    // Given the cursor and player position, return the direction the player needs to face to use this tool (scythe)
    private Vector3Int GetPlayerDirection(Vector3 cursorPosition, Vector3 playerPosition)
    {
        // Check if the cursor is in the box to the right of the player (make sure it's to the right, and also not entirely in the upper/lower boxes)
        if (
            cursorPosition.x > playerPosition.x
            && 
            cursorPosition.y < (playerPosition.y + cursor.ItemUseRadius / 2f)
            &&
            cursorPosition.y > (playerPosition.y - cursor.ItemUseRadius / 2f)
            )
        {
            return Vector3Int.right;
        }

        // Check if the cursor is in the box to the left of the player (make sure it's to the left, and also not entirely in the upper/lower boxes)
        else if (
            cursorPosition.x < playerPosition.x
            && 
            cursorPosition.y < (playerPosition.y + cursor.ItemUseRadius / 2f)
            &&
            cursorPosition.y > (playerPosition.y - cursor.ItemUseRadius / 2f)
            )
        {
            return Vector3Int.left;
        }

        // Check if the cursor is in the box above the player
        else if (cursorPosition.y > playerPosition.y)
        {
            return Vector3Int.up;
        }

        // Check if the cursor is in the box below the player
        else
        {
            return Vector3Int.down;
        }
    }


    // Check if the selected seed item can be dropped, and if the current cursor position is valid (from distance from player, bool tilemap, etc.)
    // Also check if the item can be planted! If so, plant the seed at that grid cursor
    private void ProcessPlayerClickInputSeed(GridPropertyDetails gridPropertyDetails, ItemDetails itemDetails)
    {
        // check if the item can be dropped (we already know it's a seed), and if the gridCursor has a valid position, and if the ground HAS been dug, and 
        // that there isn't a seed already planted there
        if (itemDetails.canBeDropped && gridCursor.CursorPositionIsValid && gridPropertyDetails.daysSinceDug > -1 && gridPropertyDetails.seedItemCode == -1)
        {
            // This method will plant the seed at that gridCursor location
            PlantSeedAtCursor(gridPropertyDetails, itemDetails);
        } 
        // Otherwise, just publish the CallDropSelectedItemEvent, to drop the item on the ground
        else if (itemDetails.canBeDropped && gridCursor.CursorPositionIsValid)
        {   
            // If it's a valid drop, publish this event so subscribers can see it. UIInventorySlot.DropSelectedItemAtMousePosition will subscribe to this, and drop the item
            EventHandler.CallDropSelectedItemEvent();
        }
    }


    // This method will plant the selected seed at the grid cursor location, knowing that it's a seed and can be planted there.
    private void PlantSeedAtCursor(GridPropertyDetails gridPropertyDetails, ItemDetails itemDetails)
    {
        // Update the gridPropertyDetails with the seed item code, and set the number of days growth to 0
        gridPropertyDetails.seedItemCode = itemDetails.itemCode;
        gridPropertyDetails.growthDays = 0;

        // Display the planted crop at the gridPropertyDetails
        GridPropertiesManager.Instance.DisplayPlantedCrop(gridPropertyDetails);

        // Remove the item from the inventory
        EventHandler.CallRemoveSelectedItemFromInventoryEvent();        
    }


    // Check if the selected commodity item can be dropped, and if the current cursor position is valid (from distance from player, bool tilemap, etc.)
    private void ProcessPlayerClickInputCommodity(ItemDetails itemDetails)
    {
        if (itemDetails.canBeDropped && gridCursor.CursorPositionIsValid)
        {   
            // If it's a valid drop, publish this event so subscribers can see it. UIInventorySlot.DropSelectedItemAtMousePosition will subscribe to this, and drop the item
            EventHandler.CallDropSelectedItemEvent();
        }
    }


    // This will process the players click input for all tools. We check which kind of tool it is, and do the corresponding action for that tool
    private void ProcessPlayerClickInputTool(GridPropertyDetails gridPropertyDetails, ItemDetails itemDetails, Vector3Int playerDirection)
    {
        // Switch on tool to check for which tool is actually being used - only hoeing_tools for now
        switch (itemDetails.itemType)
        {
            case ItemType.Hoeing_tool:
                if (gridCursor.CursorPositionIsValid)
                {
                    // If it's a hoeing tool, and the grid cursor position is valid, initiate the HoeGround sequence (play the hoeing animation in the correct player direction, mark the
                    // soil as dug, update the ground sprite to dug, etc.)
                    HoeGroundAtCursor(gridPropertyDetails, playerDirection);
                }
                break;

            case ItemType.Watering_tool:
                if (gridCursor.CursorPositionIsValid)
                {
                    // If it's a watering tool, and the grid cursor position is valid, initiate the WaterGround sequence (play the watering animation in the correct player direction, mark the
                    // soil as watered, update the ground sprite to watered, etc.)
                    WaterGroundAtCursor(gridPropertyDetails, playerDirection);
                }
                break;

            case ItemType.Collecting_tool:
                if (gridCursor.CursorPositionIsValid)
                {
                    // If it's a collecting tool, and the gridcursor position is valid, initiate the pick sequence (play the picking animation in the correct 
                    // player direction, destroy the fully grown crop. First, get the players direction relative to the grid cursor
                    playerDirection = GetPlayerDirection(cursor.GetWorldPositionForCursor(), GetPlayerCenterPosition());
                    CollectInPlayerDirection(gridPropertyDetails, itemDetails, playerDirection);
                }
                break;

            case ItemType.Reaping_tool:
                if (cursor.CursorPositionIsValid)
                {
                    // If it's a reaping tool, and the cursor position is valid, initiate the Reap sequence (play the reaping animation in the correct player direction, 
                    // Destroy the reapable scenary.  First, get the players direction relative to the cursor (not the grid cursor)
                    playerDirection = GetPlayerDirection(cursor.GetWorldPositionForCursor(), GetPlayerCenterPosition());
                    ReapInPlayerDirectionAtCursor(itemDetails, playerDirection);
                }
                break;
            
            default:
                break;
        }
    }


    // This method just initiates the coroutine that enables the hoeing coroutine to initiate the animation, and update the GridPropertyDetails to dug at the square
    private void HoeGroundAtCursor(GridPropertyDetails gridPropertyDetails, Vector3Int playerDirection)
    {
        // Trigger the animation as a coroutine to run over several frames
        StartCoroutine(HoeGroundAtCursorRoutine(playerDirection, gridPropertyDetails));
    }


    // This coroutine initiates the hoeing animation, and updates the GridPropertyDetails at the square in question to be dug
    private IEnumerator HoeGroundAtCursorRoutine(Vector3Int playerDirection, GridPropertyDetails gridPropertyDetails)
    {
        // Disable player input and tool use so we can't walk away or use a tool again during the animation
        PlayerInputIsDisabled = true;
        playerToolUseDisabled = true;

        // Set the tool animation to hoe in the override animations. First, apply the 'hoe' part variant type, for the attribute we want swapped
        toolCharacterAttribute.partVariantType = PartVariantType.hoe;
        // Next, clear the list and add the tool character attribute struct to it. This list is used as an override to the animations
        characterAttributeCustomisationList.Clear();
        characterAttributeCustomisationList.Add(toolCharacterAttribute);
        // This method builds an animation ovveride list, and applies it to the tool animator
        animationOverrides.ApplyCharacterCustomizationParameters(characterAttributeCustomisationList);

        // Set the proper isUsingToolDirection bool for the player animation parameter.
        // Now that the overrides are active, these will be picked up in the update loop (movement event publisher!) to hoe in the right direction
        if (playerDirection == Vector3Int.right)
        {
            isUsingToolRight = true;
        }

        else if (playerDirection == Vector3Int.left)
        {
            isUsingToolLeft = true;
        }

        else if (playerDirection == Vector3Int.up)
        {
            isUsingToolUp = true;
        }

        else if (playerDirection == Vector3Int.down)
        {
            isUsingToolDown = true;
        }

        // Wait for useToolAnimationPause seconds (while animation goes with the animators!) before starting the next phase of the coroutine
        yield return useToolAnimationPause;

        // Set the Grid property details for the time since the ground was dug here
        if (gridPropertyDetails.daysSinceDug == -1)
        {
            gridPropertyDetails.daysSinceDug = 0;
        }

        // Set the grid property to dug with the above modified details (now that the ground is dug, we won't be able to dig again - red cursor)
        GridPropertiesManager.Instance.SetGridPropertyDetails(gridPropertyDetails.gridX, gridPropertyDetails.gridY, gridPropertyDetails);

        // Display the dug grid tiles
        GridPropertiesManager.Instance.DisplayDugGround(gridPropertyDetails);


        // Wait again for the tool animation pause for enabling input again, so we don't have to rapid of animations occuring
        yield return afterUseToolAnimationPause;

        // Enable player input and tool use so we can walk away or use a tool again
        PlayerInputIsDisabled = false;
        playerToolUseDisabled = false;
    }


    // This method just initiates the coroutine that enables the watering coroutine to initiate the animation, and update the GridPropertyDetails to watered at the square
    private void WaterGroundAtCursor(GridPropertyDetails gridPropertyDetails, Vector3Int playerDirection)
    {
        // Trigger the animation as a coroutine to run over several frames
        StartCoroutine(WaterGroundAtCursorRoutine(playerDirection, gridPropertyDetails));
    }


    // This coroutine initiates the watering animation, and updates the GridPropertyDetails at the square in question to be watered
    private IEnumerator WaterGroundAtCursorRoutine(Vector3Int playerDirection, GridPropertyDetails gridPropertyDetails)
    {
        // Disable player input and tool use so we can't walk away or use a tool again during the animation
        PlayerInputIsDisabled = true;
        playerToolUseDisabled = true;

        // Set the tool animation to wateringcan in the override animations. First, apply the 'wateringcan' part variant type, for the attribute we want swapped
        toolCharacterAttribute.partVariantType = PartVariantType.wateringCan;
        // Next, clear the list and add the tool character attribute struct to it. This list is used as an override to the animations
        characterAttributeCustomisationList.Clear();
        characterAttributeCustomisationList.Add(toolCharacterAttribute);
        // This method builds an animation ovveride list, and applies it to the tool animator
        animationOverrides.ApplyCharacterCustomizationParameters(characterAttributeCustomisationList);

        // TODO: If there is water in the watering can!
        // Add the watering tool effect to the animation parameters, which shows water pouring out
        toolEffect = ToolEffect.watering;

        // Set the proper isLiftingToolDirection bool for the player animation parameter.
        // Now that the overrides are active, these will be picked up in the update loop (movement event publisher!) to water in the right direction
        if (playerDirection == Vector3Int.right)
        {
            isLiftingToolRight = true;
        }

        else if (playerDirection == Vector3Int.left)
        {
            isLiftingToolLeft = true;
        }

        else if (playerDirection == Vector3Int.up)
        {
            isLiftingToolUp = true;
        }

        else if (playerDirection == Vector3Int.down)
        {
            isLiftingToolDown = true;
        }

        // Wait for liftToolAnimationPause seconds (while animation goes with the animators!) before starting the next phase of the coroutine
        yield return liftToolAnimationPause;

        // Set the Grid property details for the time since the ground was watered here
        if (gridPropertyDetails.daysSinceWatered == -1)
        {
            gridPropertyDetails.daysSinceWatered = 0;
        }

        // Set the grid property to watered with the above modified details (now that the ground is watered, we won't be able to water again - red cursor)
        GridPropertiesManager.Instance.SetGridPropertyDetails(gridPropertyDetails.gridX, gridPropertyDetails.gridY, gridPropertyDetails);

        // Display the grid tiles to reflect they've been watered
        GridPropertiesManager.Instance.DisplayWateredGround(gridPropertyDetails);

        // Wait again for the tool animation pause for enabling input again, so we don't have to rapid of animations occuring
        yield return afterLiftToolAnimationPause;

        // Enable player input and tool use so we can walk away or use a tool again
        PlayerInputIsDisabled = false;
        playerToolUseDisabled = false;
    }


    // This method just initiates the coroutine that enables the collecting coroutine to initiate the picking animation, 
    // and destroy the fully grown crop, add it to your inventory
    private void CollectInPlayerDirection(GridPropertyDetails gridPropertyDetails, ItemDetails equippedItemDetails, Vector3Int playerDirection)
    {
        StartCoroutine(CollectInPlayerDirectionRoutine(gridPropertyDetails, equippedItemDetails, playerDirection));
    }


    // This coroutine initiates the picking animation, and checks for fully grown crops, destroys the crop, and adds it to your inventory
    private IEnumerator CollectInPlayerDirectionRoutine(GridPropertyDetails gridPropertyDetails, ItemDetails equippedItemDetails, Vector3Int playerDirection)
    {  
        // Disable player input and tool use so we can't walk away or use a tool again during the animation
        PlayerInputIsDisabled = true;
        playerToolUseDisabled = true;

        // This method will take in the gridPropertyDetails you want to harvest, and the equipped item details you want
        // to harvest with, and properly processes what happens (like how to harvest it)
        ProcessCropWithEquippedItemInPlayerDirection(playerDirection, equippedItemDetails, gridPropertyDetails);
        
        // Pause to allow the pick animation to complete
        yield return pickAnimationPause;

        // extra pause for after the animation is done
        yield return afterPickAnimationPause;

        // Enable player input and tool use so we can walk away or use a tool again
        PlayerInputIsDisabled = false;
        playerToolUseDisabled = false;
    }


    // This method just initiates the coroutine that enables the reaping coroutine to initiate the animation, 
    // and check/destroy the reapable scenary in the way
    private void ReapInPlayerDirectionAtCursor(ItemDetails itemDetails, Vector3Int playerDirection)
    {
        StartCoroutine(ReapInPlayerDirectionAtCursorRoutine(itemDetails, playerDirection));
    }


    // This coroutine initiates the reaping animation, and checks for reapable scenary in the way, and destroys the objects
    private IEnumerator ReapInPlayerDirectionAtCursorRoutine(ItemDetails itemDetails, Vector3Int playerDirection)
    {   
        // Disable player input and tool use so we can't walk away or use a tool again during the animation
        PlayerInputIsDisabled = true;
        playerToolUseDisabled = true;

        // Set the tool animation to scythe in the override animations. First, apply the 'scythe' part variant type, for the attribute we want swapped
        toolCharacterAttribute.partVariantType = PartVariantType.scythe;
        // Next, clear the list and add the tool character attribute struct to it. This list is used as an override to the animations
        characterAttributeCustomisationList.Clear();
        characterAttributeCustomisationList.Add(toolCharacterAttribute);
        // This method builds an animation ovveride list, and applies it to the tool animator
        animationOverrides.ApplyCharacterCustomizationParameters(characterAttributeCustomisationList);

        // Reap in the players direction. This method will set up the animation parameters for the correct direction,
        // find the colliders in the reap path, and destroy some of them
        UseToolInPlayerDirection(itemDetails, playerDirection);

        // Wait for useToolAnimationPause seconds (while animation goes with the animators!) before starting the next phase of the coroutine
        yield return useToolAnimationPause;

        // Enable player input and tool use so we can walk away or use a tool again
        PlayerInputIsDisabled = false;
        playerToolUseDisabled = false;
    }


    // This methd will initiate the tool use sequence for normal cursor tools, like the scythe.
    // It first sets up the proper animation parameters (i.e. swingDirection) for that tool use animationOverride.
    // It then finds all of the collider2D objects in a box in the direction the player is facing, loops through them
    // and deletes up to Settings.maxTargetComponentsToDestroyPerReapSwing ReapableScenary items in that box
    private void UseToolInPlayerDirection(ItemDetails equippedItemDetails, Vector3Int playerDirection)
    {
        if (Input.GetMouseButton(0))
        {   
            // Check for which tool is being used for this animation. For now, we have only added the scythe
            switch (equippedItemDetails.itemType)
            {   
                // If the tool is the scythe, find the playerFacingDirection and set up the correct animation triggers for the scythe direction, which will be picked up 
                // with Update method, and the animation override set up previously
                case ItemType.Reaping_tool:
                    if (playerDirection == Vector3Int.right)
                    {
                        isSwingingToolRight = true;
                    }

                    else if (playerDirection == Vector3Int.left)
                    {
                        isSwingingToolLeft = true;
                    }

                    else if (playerDirection == Vector3Int.up)
                    {
                        isSwingingToolUp = true;
                    }

                    else if (playerDirection == Vector3Int.down)
                    {
                        isSwingingToolDown = true;
                    }
                    break;
            }

            // Define the center point of the square which will be used for collision testing, in the proper playerFacingDirection
            // Here we are adding a multiple of playerDirection (a unit vector in the up, down, left, right directions), which will give you
            // (-1, 0, 1), then multiplied by half of the item use radius, to get the center of the box in the direction the player is facing
            Vector2 point = new Vector2(GetPlayerCenterPosition().x + (playerDirection.x * (equippedItemDetails.itemUseRadius / 2f)), 
                                            GetPlayerCenterPosition().y + (playerDirection.y * (equippedItemDetails.itemUseRadius / 2f)));

            // Define the size of the square (itemUseRadius for the tool in both dimensions) that will be used for collision testing
            Vector2 size = new Vector2(equippedItemDetails.itemUseRadius, equippedItemDetails.itemUseRadius);

            // Get the Item components with 2D colliders located in the square at center point and size
            // The 2D colliders tested are limited to maxCollidersToTestPerReapSwing, to save overhead. This NonAlloc method is also
            // much more memory efficient, helpful because we will use this a lot, and it simply returns a list of up to
            // maxCollidersToTestPerReapSwing colliders found in the given box
            Item[] itemArray = HelperMethods.GetComponentsAtBoxLocationNonAlloc<Item>(Settings.maxCollidersToTestPerReapSwing, point, size, 0f);

            // We only want to actually destroy up to Settings.maxTargetComponentsToDestroyPerReapSwing reapableScenaries per swing, more realistic
            int reapableItemCount = 0;

            // Loop through all of the items retrieved backwards, and search for reapableItems
            for (int i = itemArray.Length - 1; i >= 0; i--)
            {
                if (itemArray[i] != null)
                {
                    // Destroy the item gameObject if it's reapable
                    if (InventoryManager.Instance.GetItemDetails(itemArray[i].ItemCode).itemType == ItemType.Reapable_scenary)
                    {
                        // Effect position to display the cutting effect at
                        Vector3 effectPosition = new Vector3(itemArray[i].transform.position.x, itemArray[i].transform.position.y + Settings.gridCellSize / 2f, itemArray[i].transform.position.z);

                        // Publish the Harvest Action Event, which will be picked up by the VFXManager to trigger the reaping particle 
                        // effect at the location that the reapable Scenary was found
                        EventHandler.CallHarvestActionEffectEvent(effectPosition, HarvestActionEffect.reaping);

                        Destroy(itemArray[i].gameObject);

                        // Check if we've reaped the maximum amount per swing, Settings.maxTargetComponentsToDestroyPerReapSwing yet. If so, stop checking
                        reapableItemCount++;
                        if (reapableItemCount >= Settings.maxTargetComponentsPerReapSwing)
                        {
                            break;
                        }
                    }
                }
            }
        }
    }


    /// <summary>
    /// Method to process the crop with the equipped item, and the players direction
    /// </summary>
    private void ProcessCropWithEquippedItemInPlayerDirection(Vector3Int playerDirection, ItemDetails equippedItemDetails, GridPropertyDetails gridPropertyDetails)
    {   
        // Check which tool is being used to harvest the crop (basket, hoe, axe, pickaxe, ...)
        switch (equippedItemDetails.itemType)
        {
            case ItemType.Collecting_tool:
                // Set the proper isPickingDirection bool for the player animation parameter.
                // Now that the overrides are active, these will be picked up in the update loop (movement event publisher!) 
                // to pick in the right direction
                if (playerDirection == Vector3Int.right)
                {
                    isPickingRight = true;
                }
                else if (playerDirection == Vector3Int.left)
                {
                    isPickingLeft = true;
                }
                else if (playerDirection == Vector3Int.up)
                {
                    isPickingUp = true;
                }
                else if (playerDirection == Vector3Int.down)
                {
                    isPickingDown = true;
                }
                break;

            // If no tool, just break out - nothing happens
            case ItemType.none:
                break;
        }

        // Get the crop at the cursorGridLocation
        // This method returns the overlapping Crop colliders Crop object at the grid cursors location
        Crop crop = GridPropertiesManager.Instance.GetCropObjectAtGridLocation(gridPropertyDetails);

        // If we found a crop there, execute the process tool action for the crop!
        if (crop != null)
        {
            // Check what the item type is again (basket, hoe, axe, pickaxe, ...)
            switch (equippedItemDetails.itemType)
            {
                case ItemType.Collecting_tool:
                    // This method on the Crop object will
                    crop.ProcessToolAction(equippedItemDetails);
                    break;
            }
        }
    }


    // TODO: Remove
    /// <summary>
    /// Temporary routine for test input to advance the game clock quickly
    /// </summary>
    private void PlayerTestInput()
    {
        // Trigger advance minute (will continue to be called if held down)
        if (Input.GetKey(KeyCode.T))
        {
            TimeManager.Instance.TestAdvanceGameMinute();
        }

        // Trigger advance day (will only be called once every time held down)
        if (Input.GetKeyDown(KeyCode.G))
        {
            TimeManager.Instance.TestAdvanceGameDay();
        }

        // Test scene unload/loading
        if (Input.GetKeyDown(KeyCode.L))
        {
            SceneControllerManager.Instance.FadeAndLoadScene(SceneName.Scene1_Farm.ToString(), transform.position);
        }

        // Test object pool!
        // if (Input.GetMouseButtonDown(1))
        // {
        //     GameObject tree = PoolManager.Instance.ReuseObject(canyonOakTreePrefab, mainCamera.ScreenToWorldPoint(
        //         new Vector3(Input.mousePosition.x, Input.mousePosition.y, -mainCamera.transform.position.z)), Quaternion.identity);

        //         tree.SetActive(true);
        // }
    }


    private void ResetMovement()
    {
        // Reset the players movement
        xInput = 0f;
        yInput = 0f;
        isRunning = false;
        isWalking = false;
        isIdle = true;
    }


    public void DisablePlayerInputAndResetMovement()
    {
        DisablePlayerInput();
        ResetMovement();

        // Send event to any listeners for movement input, with reset, stationary values 
        EventHandler.CallMovementEvent(xInput, yInput, isWalking, isRunning, isIdle, isCarrying, toolEffect, 
            isUsingToolRight, isUsingToolLeft, isUsingToolUp, isUsingToolDown, 
            isLiftingToolRight, isLiftingToolLeft, isLiftingToolUp, isLiftingToolDown, 
            isPickingLeft, isPickingRight, isPickingUp, isPickingDown, 
            isSwingingToolRight, isSwingingToolLeft, isSwingingToolUp, isSwingingToolDown, 
            false, false, false, false);
    }


    public void DisablePlayerInput()
    {
        PlayerInputIsDisabled = true;
    }


    public void EnablePlayerInput()
    {
        PlayerInputIsDisabled = false;
    }


    public void ClearCarriedItem()
    {
        // Set the equipped item sprite to null, with a not visible color
        equippedItemSpriteRenderer.sprite = null;
        equippedItemSpriteRenderer.color = new Color(1f, 1f, 1f, 0f);

        // Apply the base character arms customization to none
        armsCharacterAttribute.partVariantType = PartVariantType.none;

        // Clear the list and add the none characterAttribute struct to it
        characterAttributeCustomisationList.Clear();
        characterAttributeCustomisationList.Add(armsCharacterAttribute);

        // This method builds an animation ovveride list, and applies it to the arms animator. Now the type is none so nothing will be overriden!
        animationOverrides.ApplyCharacterCustomizationParameters(characterAttributeCustomisationList);
        
        // Set the flag so the player is not carrying the item
        isCarrying = false;
    }


    public void ShowCarriedItem(int itemCode)
    {
        // Extract the item codes item detailsd
        ItemDetails itemDetails = InventoryManager.Instance.GetItemDetails(itemCode);

        if (itemDetails != null)
        {
            // Set the equipped item sprite to the one applied in the item details, with a visible color (default is not visible..)
            equippedItemSpriteRenderer.sprite = itemDetails.itemSprite;
            equippedItemSpriteRenderer.color = new Color(1f, 1f, 1f, 1f);

            // Apply the 'carry' character arms customization
            armsCharacterAttribute.partVariantType = PartVariantType.carry;

            // Clear the list and add the arms character attribute struct to it
            characterAttributeCustomisationList.Clear();
            characterAttributeCustomisationList.Add(armsCharacterAttribute);

            // This method builds an animation ovveride list, and applies it to the arms animator
            animationOverrides.ApplyCharacterCustomizationParameters(characterAttributeCustomisationList);
            
            // Set the flag so the player is carrying the item
            isCarrying = true;
        }
    }


    public Vector3 GetPlayerViewportPosition()
    {
        // Vector3 viewport position for player ((0, 0) is viewport bottom left, and (1,1) is viewport top right)
        // This is the position of the player in the cameras field of view, so the UI bar can tell if the player is near the bottom
        return mainCamera.WorldToViewportPoint(transform.position);
    }


    // This will return the world position for the CENTER of the player's GameObject, so we include a y-offset to bring this position up from 
    // The standard pivot point at the feet.
    public Vector3 GetPlayerCenterPosition()
    {
        return new Vector3(transform.position.x, transform.position.y + Settings.playerCenterYOffset, transform.position.z);
    }
}
