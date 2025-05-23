using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
using Team4ProefVanBekwaamheid.TurnBasedStrategy.PowerUps;

[Serializable] // Make it visible in the Inspector
public class PowerUpSprites
{
    public Sprite unusable;
    public Sprite usable;
    public Sprite charged;
    public Sprite supercharged;
}

public enum PowerUpState
{
    Unusable,
    Usable,
    Charged,
    Supercharged
}

// Enum to identify who is using the power-up
public enum PowerUpUser
{
    Player,
    Enemy
}

public class PowerUpManager : MonoBehaviour
{
    public PowerUpInventory powerUpInventory;

    [Header("UI Button Images")]
    public Image swordButtonImage;
    public Image shieldButtonImage;
    public Image trapButtonImage;
    public Image stepsButtonImage;

    [Header("UI Fill Images (Set Type to Filled)")]
    public Image swordFillImage;
    public Image shieldFillImage;
    public Image trapFillImage;
    public Image stepsFillImage;

    [Header("PowerUp Specific Sprites")]
    public PowerUpSprites swordSprites;
    public PowerUpSprites shieldSprites;
    public PowerUpSprites trapSprites;
    public PowerUpSprites stepsSprites;

    [Header("Animation Settings")]
    [SerializeField] private float fillAnimationDuration = 0.25f;
    [SerializeField] private AnimationCurve fillAnimationCurve = AnimationCurve.Linear(0, 0, 1, 1);

    // Dictionaries to map PowerUpType to UI elements and state
    private Dictionary<PowerUpInventory.PowerUpType, Image> _powerUpImageMap;
    private Dictionary<PowerUpInventory.PowerUpType, PowerUpSprites> _powerUpSpriteMap;
    private Dictionary<PowerUpInventory.PowerUpType, Image> _powerUpFillImageMap;
    private Dictionary<PowerUpInventory.PowerUpType, Coroutine> _fillCoroutines;
    private Dictionary<PowerUpInventory.PowerUpType, PowerUpState> _powerUpPreviousStates;
    private Dictionary<PowerUpInventory.PowerUpType, Button> _powerUpButtonMap; // Map type to Button component
    private bool _isUsingPowerUp = false; // Flag to prevent event-driven update during use
    private Color _colorUnusable;
    private readonly Color _colorActive = Color.white; // #FFFFFF

    // Constants for State Thresholds
    private const int USABLE_THRESHOLD = 1;
    private const int CHARGED_THRESHOLD = 15;
    private const int SUPERCHARGED_THRESHOLD = 25;

    // Power Up References
    [SerializeField] private MovementPowerUp _movementPowerUp; // Reference to the PowerUpInventory script
    [SerializeField] private AttackPowerUp _attackPowerUp; 
    [SerializeField] private TrapPowerUp _trapPowerUp;
    [SerializeField] private DefensePowerUp _defensePowerUp; // Reference to the PowerUpInventory script


    private void Awake()
    {
        // Initialize mappings
        _powerUpImageMap = new Dictionary<PowerUpInventory.PowerUpType, Image>
        {
            { PowerUpInventory.PowerUpType.Sword, swordButtonImage },
            { PowerUpInventory.PowerUpType.Shield, shieldButtonImage },
            { PowerUpInventory.PowerUpType.Trap, trapButtonImage },
            { PowerUpInventory.PowerUpType.Steps, stepsButtonImage }
        };

        _powerUpSpriteMap = new Dictionary<PowerUpInventory.PowerUpType, PowerUpSprites>
        {
            { PowerUpInventory.PowerUpType.Sword, swordSprites },
            { PowerUpInventory.PowerUpType.Shield, shieldSprites },
            { PowerUpInventory.PowerUpType.Trap, trapSprites },
            { PowerUpInventory.PowerUpType.Steps, stepsSprites }
        };

       _fillCoroutines = new Dictionary<PowerUpInventory.PowerUpType, Coroutine>();
       _powerUpPreviousStates = new Dictionary<PowerUpInventory.PowerUpType, PowerUpState>();

        _powerUpFillImageMap = new Dictionary<PowerUpInventory.PowerUpType, Image>
        {
            { PowerUpInventory.PowerUpType.Sword, swordFillImage },
            { PowerUpInventory.PowerUpType.Shield, shieldFillImage },
            { PowerUpInventory.PowerUpType.Trap, trapFillImage },
            { PowerUpInventory.PowerUpType.Steps, stepsFillImage }
        };

        _powerUpButtonMap = new Dictionary<PowerUpInventory.PowerUpType, Button>();
        // Populate the button map, assuming Button is on the same GameObject as the Image
        foreach (var kvp in _powerUpImageMap)
        {
            if (kvp.Value != null)
            {
                Button button = kvp.Value.GetComponent<Button>();
                if (button != null)
                {
                    _powerUpButtonMap[kvp.Key] = button;
                }
            }
        }

        ColorUtility.TryParseHtmlString("#6A6A6A", out _colorUnusable);

        // Initialize previous states and initial visuals
        foreach (PowerUpInventory.PowerUpType type in System.Enum.GetValues(typeof(PowerUpInventory.PowerUpType)))
        {
           if (System.Enum.IsDefined(typeof(PowerUpInventory.PowerUpType), type))
           {
               _powerUpPreviousStates[type] = DetermineStateFromCount(powerUpInventory.GetPowerUpCount(type));
               // Use instantUpdate true for initial setup to snap visuals without animation
               UpdatePowerUpVisual(type, instantUpdate: true);
            }
       }

       // Subscribe to GridManager's animation finished event
       GridManager.OnBlockAnimationFinished += HandleBlockAnimationFinished;
   }

   private void OnDestroy()
   {
       // Unsubscribe to prevent errors and memory leaks
       GridManager.OnBlockAnimationFinished -= HandleBlockAnimationFinished;
   }

   // This handler now triggers the visual update when a block animation finishes
   private void HandleBlockAnimationFinished(PowerUpInventory.PowerUpType type)
   {    
        if (_isUsingPowerUp) // Still check this flag just in case
        {
            return;
        }

        // --- Check if animation is already running for this type ---
        if (_fillCoroutines.TryGetValue(type, out Coroutine runningCoroutine) && runningCoroutine != null)
        {
            // Animation is already in progress (likely from the first block of this type in the match).
            // Do nothing and let the existing animation complete.
            // Debug.Log($"Ignoring subsequent arrival for {type}, animation already running.");
            return;
        }

        // --- No animation running, start it ---
        // Call UpdatePowerUpVisual to start the fill animation based on the current inventory count.
        // This will also store the new coroutine reference in fillCoroutines.
        // Debug.Log($"First arrival for {type}, starting fill animation.");
        UpdatePowerUpVisual(type);
   }


   private PowerUpState DetermineStateFromCount(int count)
    {
        if (count >= SUPERCHARGED_THRESHOLD) return PowerUpState.Supercharged;
        if (count >= CHARGED_THRESHOLD) return PowerUpState.Charged;
        if (count >= USABLE_THRESHOLD) return PowerUpState.Usable;
        return PowerUpState.Unusable;
    }

    public PowerUpState GetCurrentUsageState(PowerUpInventory.PowerUpType type)
    {
        int count = powerUpInventory.GetPowerUpCount(type);
        // Determine the state based on thresholds for usage purposes
        if (count >= SUPERCHARGED_THRESHOLD) return PowerUpState.Supercharged;
        if (count >= CHARGED_THRESHOLD) return PowerUpState.Charged;
        if (count >= USABLE_THRESHOLD) return PowerUpState.Usable; // Add Usable state check
        return PowerUpState.Unusable;
    }


    /// Updates the visual representation (background sprite and fill image) of a specific power-up button.
    /// <param name="instantReset">If true, forces visuals to unusable state immediately, stopping animations.</param>
    /// <param name="instantUpdate">If true, stops animation and snaps visuals to the current state immediately.</param>
    public void UpdatePowerUpVisual(PowerUpInventory.PowerUpType type, bool instantReset = false, bool instantUpdate = false)
    {
        if (powerUpInventory == null)
        {
            return;
        }

        int count = powerUpInventory.GetPowerUpCount(type);
        PowerUpState currentState = DetermineStateFromCount(count);
        PowerUpState previousState = _powerUpPreviousStates.ContainsKey(type) ? _powerUpPreviousStates[type] : currentState; // Get previous or assume current if first time

        // Get references
        bool bgImageFound = _powerUpImageMap.TryGetValue(type, out Image bgImage);
        bool fillImageFound = _powerUpFillImageMap.TryGetValue(type, out Image fillImage);
        bool spritesFound = _powerUpSpriteMap.TryGetValue(type, out PowerUpSprites sprites);

        if (bgImage == null || fillImage == null || sprites == null) return;

        // --- Stop existing animation ---
        if (_fillCoroutines.TryGetValue(type, out Coroutine runningCoroutine) && runningCoroutine != null)
        {
            StopCoroutine(runningCoroutine);
            _fillCoroutines[type] = null;
        }

        // --- Handle Instant Reset (Force Unusable) ---
        if (instantReset)
        {
            bgImage.sprite = sprites.unusable;
            fillImage.fillAmount = 0f;
            fillImage.gameObject.SetActive(false); // Hide fill
            _powerUpPreviousStates[type] = PowerUpState.Unusable; // Update previous state
            SetPowerUpStateColors(bgImage, PowerUpState.Unusable);
            // Debug.Log($"Instant Reset for {type}");
            return; // Skip normal update logic
        }

        // --- Handle Instant Update (Snap to Current State) ---
        if (instantUpdate)
        {
            int currentCount = powerUpInventory.GetPowerUpCount(type);
            PowerUpState stateNow = DetermineStateFromCount(currentCount);
            // Ensure components are valid before proceeding
             if (!bgImageFound || bgImage == null || !fillImageFound || fillImage == null || !spritesFound || sprites == null)
             {
                  return;
             }
            PowerUpSprites spritesNow = _powerUpSpriteMap[type];

            Sprite bgSpriteNow = GetSpriteForState(spritesNow, stateNow);
            Sprite fillSpriteNow; // Sprite for the *next* state's fill
            float fillAmountNow = 0f;
            bool showFillNow = false;
            int collectedSinceLastStateNow = 0;
            int requiredForNextStateNow = 1;

            switch (stateNow)
            {
                case PowerUpState.Unusable:
                    fillSpriteNow = spritesNow.usable;
                    requiredForNextStateNow = USABLE_THRESHOLD;
                    collectedSinceLastStateNow = currentCount;
                    showFillNow = currentCount > 0;
                    break;
                case PowerUpState.Usable:
                    fillSpriteNow = spritesNow.charged;
                    requiredForNextStateNow = CHARGED_THRESHOLD - USABLE_THRESHOLD;
                    collectedSinceLastStateNow = currentCount - USABLE_THRESHOLD;
                    showFillNow = true;
                    break;
                case PowerUpState.Charged:
                    fillSpriteNow = spritesNow.supercharged;
                    requiredForNextStateNow = SUPERCHARGED_THRESHOLD - CHARGED_THRESHOLD;
                    collectedSinceLastStateNow = currentCount - CHARGED_THRESHOLD;
                    showFillNow = true;
                    break;
                case PowerUpState.Supercharged:
                    fillSpriteNow = spritesNow.supercharged;
                    requiredForNextStateNow = 1; // Avoid division by zero
                    collectedSinceLastStateNow = 1;
                    showFillNow = false;
                    break;
                default: // Should not happen
                     fillSpriteNow = spritesNow.unusable;
                     break;
            }

            if (requiredForNextStateNow > 0)
            {
                fillAmountNow = Mathf.Clamp01((float)collectedSinceLastStateNow / requiredForNextStateNow);
            }
            else
            {
                fillAmountNow = (collectedSinceLastStateNow > 0) ? 1.0f : 0.0f;
            }

            bgImage.sprite = bgSpriteNow;
            fillImage.sprite = fillSpriteNow; // Set the fill sprite correctly
            fillImage.fillAmount = fillAmountNow;
            fillImage.gameObject.SetActive(showFillNow);
            _powerUpPreviousStates[type] = stateNow; // Update previous state
            SetPowerUpStateColors(bgImage, stateNow);
            // Debug.Log($"Instant Update for {type} to state {stateNow}, fill {fillAmountNow}");
            return; // Skip normal animation logic
        }


        // --- Normal Update Logic (with animation) ---
        // State info needed for animation logic if not handled by instant flags
        // 'count' is already available from the start of the method if we reach this point.
        currentState = DetermineStateFromCount(count); // Use existing 'count'
        previousState = _powerUpPreviousStates.ContainsKey(type) ? _powerUpPreviousStates[type] : currentState; // Use existing 'currentState'

        // Determine sprites and fill amount based on current state for animation
        Sprite bgSprite = sprites.unusable;
        Sprite fillSprite = sprites.usable;
        float fillAmount = 0f;
        bool showFill = false;

        int collectedSinceLastState = 0;
        int requiredForNextState = 1;

        switch (currentState)
        {
            case PowerUpState.Unusable:
                bgSprite = sprites.unusable;
                fillSprite = sprites.usable; // Next state sprite
                requiredForNextState = USABLE_THRESHOLD;
                collectedSinceLastState = count;
                showFill = count > 0; // Show fill only if progressing
                break;

            case PowerUpState.Usable:
                bgSprite = sprites.usable;
                fillSprite = sprites.charged; // Next state sprite
                requiredForNextState = CHARGED_THRESHOLD - USABLE_THRESHOLD;
                collectedSinceLastState = count - USABLE_THRESHOLD;
                showFill = true;
                break;

            case PowerUpState.Charged:
                bgSprite = sprites.charged;
                fillSprite = sprites.supercharged; // Next state sprite
                requiredForNextState = SUPERCHARGED_THRESHOLD - CHARGED_THRESHOLD;
                collectedSinceLastState = count - CHARGED_THRESHOLD;
                showFill = true;
                break;

            case PowerUpState.Supercharged:
                bgSprite = sprites.supercharged;
                fillSprite = sprites.supercharged; // No next state, fill is effectively complete or hidden
                requiredForNextState = 1; // Avoid division by zero
                collectedSinceLastState = 1;
                showFill = false; // Hide fill image when supercharged
                break;
        }

        // Calculate fill amount
        if (requiredForNextState > 0)
        {
            fillAmount = (float)collectedSinceLastState / requiredForNextState;
        }
        else // Should only happen if thresholds are equal, fallback
        {
            fillAmount = (collectedSinceLastState > 0) ? 1.0f : 0.0f;
        }
        fillAmount = Mathf.Clamp01(fillAmount);

        // --- Determine Animation Targets and Post-Animation State ---
        float animationTargetFill = fillAmount;
        bool isStateChange = currentState != previousState;
        bool triggerInstantSwitch = false; // Flag to indicate instant switch after animation

        // Store the visuals corresponding to the *new* state if a switch occurs
        Sprite postAnimBgSprite = bgSprite;
        Sprite postAnimFillSprite = fillSprite;
        float postAnimFillAmount = fillAmount;
        bool postAnimShowFill = showFill;

        // Determine sprites to be used *during* the animation
        Sprite animBgSprite = bgSprite;
        Sprite animFillSprite = fillSprite;

        if (isStateChange)
        {
            // State changed. Determine if we animate up to 1 or down to 0 before switching.
            if (currentState > previousState) // Increased state (e.g., Usable -> Charged)
            {
                animationTargetFill = 1.0f; // Animate the *previous* fill up to 1.0
                triggerInstantSwitch = true;
                // Sprites *during* animation: BG = previous state, Fill = the sprite that was filling *before* the change
                animBgSprite = GetSpriteForState(sprites, previousState);
                animFillSprite = GetSpriteForState(sprites, currentState); // This is the sprite of the state we are transitioning TO
                if (SFXManager.Instance != null)
                {
                    SFXManager.Instance.PlayActionSFX(SFXManager.ActionType.PowerUpNextLevelReached);
                }
            }
            else // Decreased state
            {
                // Snap directly to the new (lower) state's visuals
                animationTargetFill = fillAmount;
                triggerInstantSwitch = false;
                // Apply new state visuals immediately
                bgImage.sprite = bgSprite;
                fillImage.sprite = fillSprite;
                fillImage.fillAmount = fillAmount;
                fillImage.gameObject.SetActive(showFill);
            }
        }
        else
        {
            // No state change, standard animation
            animationTargetFill = fillAmount;
            triggerInstantSwitch = false;
        }


       // Apply sprites for the animation phase (if not already snapped)
       if (!(isStateChange && currentState < previousState))
       {
            bgImage.sprite = animBgSprite;
            fillImage.sprite = animFillSprite;
       }
       SetPowerUpStateColors(bgImage, currentState); // Update colors based on the current state


       // --- Start Animation (if needed) ---

        // Only start animation if not a downward state change (which snaps)
        if (!(isStateChange && currentState < previousState))
        {
            // Determine visibility during animation
            // If triggering a switch, the fill must be visible during the animation up to 1.0
            bool showDuringAnim = triggerInstantSwitch || showFill;

            // Only animate if the target is different or visibility needs changing
            if (Mathf.Abs(fillImage.fillAmount - animationTargetFill) > 0.001f || fillImage.gameObject.activeSelf != showDuringAnim)
            {
                Coroutine newCoroutine = StartCoroutine(AnimateFillAmount(
                    type,
                    fillImage,
                    bgImage,
                    animationTargetFill,
                    showDuringAnim,
                    triggerInstantSwitch,
                    postAnimBgSprite,
                    postAnimFillSprite,
                    postAnimFillAmount,
                    postAnimShowFill
                ));
                _fillCoroutines[type] = newCoroutine;
            }
            else if (triggerInstantSwitch) // If already at 1.0 and state increases, switch, reset fill to 0, and animate up
            {
                // Apply sprites & visibility instantly
                bgImage.sprite = postAnimBgSprite;
                fillImage.sprite = postAnimFillSprite;
                fillImage.gameObject.SetActive(postAnimShowFill);
                // Set fill to 0 instantly
                fillImage.fillAmount = 0f;
                // Start animation from 0 up to the target
                float secondPhaseDuration = fillAnimationDuration;
                _fillCoroutines[type] = StartCoroutine(AnimateFillFromZero(type, fillImage, postAnimFillAmount, secondPhaseDuration, postAnimShowFill));
            }
            else
            {
                // Ensure final state is correct even if no animation needed
                fillImage.fillAmount = animationTargetFill;
                fillImage.gameObject.SetActive(showFill);
                _fillCoroutines[type] = null;
            }
        }
        else
        {
            // Ensure coroutine ref is cleared for downward state change
            _fillCoroutines[type] = null;
        }
        // Update the previous state for the next check
        _powerUpPreviousStates[type] = currentState;
    }

   private IEnumerator AnimateFillAmount(
       PowerUpInventory.PowerUpType type,
       Image image,
       Image bgImage,
       float targetFillAmount,
       bool showDuringAnimation,
       bool triggerPostAnimationSwitch,
       Sprite postAnimBgSprite,
       Sprite postAnimFillSprite,
       float postAnimFillAmount,
       bool postAnimShowFill)
    {
        // Ensure the image GameObject is active *before* starting animation if it needs to be shown
        if (showDuringAnimation && !image.gameObject.activeSelf)
        {
             image.gameObject.SetActive(true);
        }

        float startFillAmount = image.fillAmount;
        float elapsedTime = 0f;

        while (elapsedTime < fillAnimationDuration)
        {
            elapsedTime += Time.deltaTime;
            float timeFraction = Mathf.Clamp01(elapsedTime / fillAnimationDuration);
            float curveValue = fillAnimationCurve.Evaluate(timeFraction);
            float newFill = Mathf.LerpUnclamped(startFillAmount, targetFillAmount, curveValue);

            image.fillAmount = Mathf.Clamp01(newFill);
            yield return null;
        }

        // Ensure the animation target value is set exactly
        image.fillAmount = targetFillAmount;

      // --- Post Animation ---
      if (triggerPostAnimationSwitch)
      {
          // Apply new sprites & visibility
          bgImage.sprite = postAnimBgSprite;
          image.sprite = postAnimFillSprite;
          image.gameObject.SetActive(postAnimShowFill);
          // Set fill to 0 instantly
          image.fillAmount = 0f;

          // Start the second phase animation: 0 up to target
          float secondPhaseDuration = fillAnimationDuration;
          _fillCoroutines[type] = StartCoroutine(AnimateFillFromZero(type, image, postAnimFillAmount, secondPhaseDuration, postAnimShowFill));
          // AnimateFillFromZero will handle clearing the coroutine reference
       }
       else
        {
            // Just ensure final visibility is correct if no state switch happened
             if (image.gameObject.activeSelf != showDuringAnimation)
             {
                 image.gameObject.SetActive(showDuringAnimation);
           }
           // Clean up coroutine reference if no switch happened
           if (_fillCoroutines.ContainsKey(type))
           {
               _fillCoroutines[type] = null;
           }
      }
  }

  // Coroutine to animate fill amount from 0 up to a target
  private IEnumerator AnimateFillFromZero(PowerUpInventory.PowerUpType type, Image image, float targetFillAmount, float duration, bool finalVisibility)
  {
      // Ensure fill starts at 0 and image is active if needed
      image.fillAmount = 0f;
      if (finalVisibility && !image.gameObject.activeSelf)
      {
          image.gameObject.SetActive(true);
      }

      float startFillAmount = 0f;
      float elapsedTime = 0f;

      while (elapsedTime < duration)
      {
          elapsedTime += Time.deltaTime;
          float timeFraction = Mathf.Clamp01(elapsedTime / duration);
          float curveValue = fillAnimationCurve.Evaluate(timeFraction);
          float newFill = Mathf.LerpUnclamped(startFillAmount, targetFillAmount, curveValue);

          image.fillAmount = Mathf.Clamp01(newFill);
          yield return null;
      }

      // Ensure final value and visibility
      image.fillAmount = targetFillAmount;
      if (image.gameObject.activeSelf != finalVisibility)
      {
           image.gameObject.SetActive(finalVisibility);
      }

      // Clean up coroutine reference only if this is still the active one
      if (_fillCoroutines.TryGetValue(type, out Coroutine current) && current == _fillCoroutines[type])
      {
           _fillCoroutines[type] = null;
      }
  }
  
  // New private coroutine to animate fill to a target and set visibility
  private IEnumerator AnimateFillToTargetCoroutine(PowerUpInventory.PowerUpType type, Image image, float targetFillAmount, bool setVisibleAfterAnimation, System.Action onComplete)
  {
      if (image == null)
      {
          onComplete?.Invoke(); // Invoke onComplete even if image is null to clean up dictionary
          yield break;
      }
  
      // Ensure image is active to see the animation if it's not already and there's a change in fill.
      if (!image.gameObject.activeSelf && image.fillAmount != targetFillAmount)
      {
          image.gameObject.SetActive(true);
      }
      
      float startFillAmount = image.fillAmount;
      float elapsedTime = 0f;
  
      while (elapsedTime < fillAnimationDuration)
      {
          elapsedTime += Time.deltaTime;
          float timeFraction = Mathf.Clamp01(elapsedTime / fillAnimationDuration);
          float curveValue = fillAnimationCurve.Evaluate(timeFraction);
          image.fillAmount = Mathf.LerpUnclamped(startFillAmount, targetFillAmount, curveValue);
          yield return null;
      }
  
      image.fillAmount = targetFillAmount;
      image.gameObject.SetActive(setVisibleAfterAnimation);
  
      onComplete?.Invoke();
  }


    /// Updates the visuals for all power-up buttons.
    /// <param name="instantReset">If true, forces all visuals to unusable state immediately.</param>
    /// <param name="instantUpdate">If true, snaps all visuals to the current state immediately.</param>
    public void UpdateAllPowerUpVisuals(bool instantReset = false, bool instantUpdate = false) // Added instantUpdate
    {
        foreach (PowerUpInventory.PowerUpType type in System.Enum.GetValues(typeof(PowerUpInventory.PowerUpType)))
        {
            if (System.Enum.IsDefined(typeof(PowerUpInventory.PowerUpType), type))
            {
                 UpdatePowerUpVisual(type, instantReset, instantUpdate); // Pass the flags
            }
        }
    }

    // Helper to get the correct sprite based on type and state
    private Sprite GetSpriteForState(PowerUpSprites sprites, PowerUpState state)
    {
        switch (state)
        {
            case PowerUpState.Unusable: return sprites.unusable;
            case PowerUpState.Usable: return sprites.usable;
            case PowerUpState.Charged: return sprites.charged;
            case PowerUpState.Supercharged: return sprites.supercharged;
            default:
                return sprites.unusable; // Default to unusable sprite
        }
    }

    private void SetPowerUpStateColors(Image powerUpButtonImage, PowerUpState state)
    {
        if (powerUpButtonImage == null) return; // Silently return if no image to work on

        Image state1Image = null;
        Image state2Image = null;
        Image state3Image = null;

        // Find children by tag and get their Image components
        foreach (Transform child in powerUpButtonImage.transform)
        {
            if (child.CompareTag("PowerUpState1")) state1Image = child.GetComponent<Image>();
            else if (child.CompareTag("PowerUpState2")) state2Image = child.GetComponent<Image>();
            else if (child.CompareTag("PowerUpState3")) state3Image = child.GetComponent<Image>();
        }

        // Default all found state images to unusable color
        if (state1Image != null) state1Image.color = _colorUnusable;
        if (state2Image != null) state2Image.color = _colorUnusable;
        if (state3Image != null) state3Image.color = _colorUnusable;

        // Set active color based on state
        switch (state)
        {
            case PowerUpState.Usable:
                if (state1Image != null) state1Image.color = _colorActive;
                break;
            case PowerUpState.Charged:
                if (state1Image != null) state1Image.color = _colorActive;
                if (state2Image != null) state2Image.color = _colorActive;
                break;
            case PowerUpState.Supercharged:
                if (state1Image != null) state1Image.color = _colorActive;
                if (state2Image != null) state2Image.color = _colorActive;
                if (state3Image != null) state3Image.color = _colorActive;
                break;
            case PowerUpState.Unusable:
                // Defaulted above
                break;
        }
    }

   /// <summary>
   /// Sets the interactable state of all power-up buttons.
   /// </summary>
   /// <param name="interactable">True to enable, false to disable.</param>
   public void SetButtonsInteractable(bool interactable)
   {
       foreach (var kvp in _powerUpButtonMap)
       {
           if (kvp.Value != null)
           {
               kvp.Value.interactable = interactable;
           }
       }
       // Optionally, add visual feedback for disabled state if needed (e.g., grey out)
       // This basic implementation just disables the Button component.
   }


    public bool TryUsePowerUp(PowerUpInventory.PowerUpType type)
    {
        if (powerUpInventory == null)
        {
            return false;
        }

        PowerUpState usageState = GetCurrentUsageState(type);

        switch (usageState)
        {
            case PowerUpState.Unusable:
                // No change in state, no action needed other than feedback
                int currentCount = powerUpInventory.GetPowerUpCount(type);
                // We still call instant update to stop any potential ongoing animation if clicked rapidly
                UpdatePowerUpVisual(type, instantUpdate: true);
                return false;

            case PowerUpState.Usable:
            case PowerUpState.Charged:
            case PowerUpState.Supercharged:
                // Any successful use resets count to 0 and visuals instantly
                _isUsingPowerUp = true;
                ActivateEffect(type, usageState); // Activate effect based on the state it was used in
                if (SFXManager.Instance != null)
                {
                    SFXManager.Instance.PlayActionSFX(SFXManager.ActionType.UsePowerUp);
                }
                powerUpInventory.SetPowerUpCount(type, 0); // Reset count to 0
                UpdatePowerUpVisual(type, instantReset: true); // Force instant visual reset to Unusable state
                _isUsingPowerUp = false; // Reset flag AFTER instant update
                return true;

            default:
                // This case should ideally not be reached
                return false;
        }
    }

    // --- Public Methods for UI Button OnClick Events ---

    public void UseSwordPowerUp()
    {
        TryUsePowerUp(PowerUpInventory.PowerUpType.Sword);
    }

    public void UseShieldPowerUp()
    {
        TryUsePowerUp(PowerUpInventory.PowerUpType.Shield);
    }

    public void UseTrapPowerUp()
    {
        TryUsePowerUp(PowerUpInventory.PowerUpType.Trap);
    }

    public void UseStepsPowerUp()
    {
        TryUsePowerUp(PowerUpInventory.PowerUpType.Steps);
    }

    // --- Private Helper Methods ---

    private void ActivateEffect(PowerUpInventory.PowerUpType type, PowerUpState state)
    {
        // Determine the user (In this context, it's always the player clicking the button)
        PowerUpUser user = PowerUpUser.Player;

        // Call the consolidated handler based on the power-up type
        switch (type)
        {
            case PowerUpInventory.PowerUpType.Sword:
                HandleSword(type, state, user);
                break;
            case PowerUpInventory.PowerUpType.Shield:
                HandleShield(type, state, user);
                break;
            case PowerUpInventory.PowerUpType.Trap:
                HandleTrap(type, state, user);
                break;
            case PowerUpInventory.PowerUpType.Steps:
                HandleSteps(type, state, user);
                break;
            default:
                 break;
        }
    }

    // --- Consolidated Effect Handlers ---
    // These methods now receive the state and user, and can pass this info
    // to another script responsible for the actual game logic.

    private void HandleSword(PowerUpInventory.PowerUpType type, PowerUpState state, PowerUpUser user)
    {
        _attackPowerUp.AttackPowerUpSelected(state, TileSelection.UserType.Player); // Call the attack power-up selection method
    }

    private void HandleShield(PowerUpInventory.PowerUpType type, PowerUpState state, PowerUpUser user)
    {
        _defensePowerUp.DefensePowerUpSelected(state, TileSelection.UserType.Player); // Call the defense power-up selection method
    }

    private void HandleTrap(PowerUpInventory.PowerUpType type, PowerUpState state, PowerUpUser user)
    {
        _trapPowerUp.TrapPowerUpSelected(state, TileSelection.UserType.Player); // Call the trap power-up selection method
    }

    /// <summary>
    /// Animates all power-up fills to disappear (fill to 0, then hide).
    /// Call this when the Player phase begins.
    /// </summary>
    public void AnimateFillsToDisappearForPlayerPhase()
    {
        foreach (PowerUpInventory.PowerUpType type in System.Enum.GetValues(typeof(PowerUpInventory.PowerUpType)))
            {
                if (System.Enum.IsDefined(typeof(PowerUpInventory.PowerUpType), type))
                {
                    if (_powerUpFillImageMap.TryGetValue(type, out Image fillImage) && fillImage != null)
                    {
                        // Stop any existing fill animation for this type and clear its registration
                        if (_fillCoroutines.TryGetValue(type, out Coroutine oldCoroutine) && oldCoroutine != null)
                        {
                            StopCoroutine(oldCoroutine);
                            _fillCoroutines[type] = null;
                        }
    
                        // Only animate if it's currently visible and has some fill
                        if (fillImage.gameObject.activeSelf && fillImage.fillAmount > 0)
                        {
                            Coroutine newCoroutineInstance = null;
                            newCoroutineInstance = StartCoroutine(AnimateFillToTargetCoroutine(type, fillImage, 0f, false,
                                () => { // OnComplete action
                                    // Only nullify if this specific coroutine instance is still the one registered
                                    if (_fillCoroutines.TryGetValue(type, out Coroutine currentRegistered) && currentRegistered == newCoroutineInstance)
                                    {
                                        _fillCoroutines[type] = null;
                                    }
                                }
                            ));
                            _fillCoroutines[type] = newCoroutineInstance; // Register the new coroutine
                        }
                        else if (fillImage.gameObject.activeSelf && fillImage.fillAmount == 0f)
                        {
                            // Already at 0 fill and active, just ensure it's hidden
                            fillImage.gameObject.SetActive(false);
                            // fillCoroutines[type] should be null from the stop logic above if one was running
                        }
                        // If not activeSelf, it's already considered hidden.
                    }
                }
            }
        }

    private void HandleSteps(PowerUpInventory.PowerUpType type, PowerUpState state, PowerUpUser user)
    {
        _movementPowerUp.MovementPowerUpSelected(state, TileSelection.UserType.Player); // Call the movement power-up selection method
        // TODO: Pass type, state, user to the actual effect execution script/system
    }

    /// <summary>
    /// Forces the visual representation of all power-ups to the 'Unusable' state
    /// without changing the underlying inventory counts. Stops any running animations.
    /// Call this when the enemy phase starts.
    /// </summary>
    public void SetVisualsToUnusable()
    {
        UpdateAllPowerUpVisuals(instantReset: true);
    }

    /// <summary>
    /// Updates the visual representation of all power-ups based on the current
    /// counts in the PowerUpInventory. Allows animations.
    /// Call this when the matching phase starts.
    /// </summary>
    public void RestoreVisualsFromInventory()
    {
        // Call UpdateAllPowerUpVisuals with default parameters (no instant flags)
        // This will read current counts and apply visuals, potentially animating.
        UpdateAllPowerUpVisuals();
    }
}