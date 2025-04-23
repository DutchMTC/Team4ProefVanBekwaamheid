using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;

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

public class PowerUpManager : MonoBehaviour
{
    public PowerUpInventory powerUpInventory;

    [Header("UI Button Images")]
    public Image swordButtonImage;
    public Image shieldButtonImage;
    public Image wallButtonImage;
    public Image stepsButtonImage;

    [Header("UI Fill Images (Set Type to Filled)")]
    public Image swordFillImage;
    public Image shieldFillImage;
    public Image wallFillImage;
    public Image stepsFillImage;

    [Header("PowerUp Specific Sprites")]
    public PowerUpSprites swordSprites;
    public PowerUpSprites shieldSprites;
    public PowerUpSprites wallSprites;
    public PowerUpSprites stepsSprites;

    [Header("Animation Settings")]
    [SerializeField] private float fillAnimationDuration = 0.25f;
    [SerializeField] private AnimationCurve fillAnimationCurve = AnimationCurve.Linear(0, 0, 1, 1);

    // Dictionaries to map PowerUpType to UI elements and state
    private Dictionary<PowerUpInventory.PowerUpType, Image> powerUpImageMap;
    private Dictionary<PowerUpInventory.PowerUpType, PowerUpSprites> powerUpSpriteMap;
    private Dictionary<PowerUpInventory.PowerUpType, Image> powerUpFillImageMap;
    private Dictionary<PowerUpInventory.PowerUpType, Coroutine> fillCoroutines;
    private Dictionary<PowerUpInventory.PowerUpType, PowerUpState> powerUpPreviousStates;

    // Constants for State Thresholds
    private const int USABLE_THRESHOLD = 1;
    private const int CHARGED_THRESHOLD = 15;
    private const int SUPERCHARGED_THRESHOLD = 25;


    private void Awake()
    {
        // Initialize mappings
        powerUpImageMap = new Dictionary<PowerUpInventory.PowerUpType, Image>
        {
            { PowerUpInventory.PowerUpType.Sword, swordButtonImage },
            { PowerUpInventory.PowerUpType.Shield, shieldButtonImage },
            { PowerUpInventory.PowerUpType.Wall, wallButtonImage },
            { PowerUpInventory.PowerUpType.Steps, stepsButtonImage }
        };

        powerUpSpriteMap = new Dictionary<PowerUpInventory.PowerUpType, PowerUpSprites>
        {
            { PowerUpInventory.PowerUpType.Sword, swordSprites },
            { PowerUpInventory.PowerUpType.Shield, shieldSprites },
            { PowerUpInventory.PowerUpType.Wall, wallSprites },
            { PowerUpInventory.PowerUpType.Steps, stepsSprites }
        };

       fillCoroutines = new Dictionary<PowerUpInventory.PowerUpType, Coroutine>();
       powerUpPreviousStates = new Dictionary<PowerUpInventory.PowerUpType, PowerUpState>();

        powerUpFillImageMap = new Dictionary<PowerUpInventory.PowerUpType, Image>
        {
            { PowerUpInventory.PowerUpType.Sword, swordFillImage },
            { PowerUpInventory.PowerUpType.Shield, shieldFillImage },
            { PowerUpInventory.PowerUpType.Wall, wallFillImage },
            { PowerUpInventory.PowerUpType.Steps, stepsFillImage }
        };

        // Ensure fill images are correctly configured
        foreach (var kvp in powerUpFillImageMap)
        {
            if (kvp.Value != null && kvp.Value.type != Image.Type.Filled)
            {
                Debug.LogWarning($"Image for {kvp.Key} Fill is not set to 'Filled' type in the Inspector. Fill effect may not work correctly.");
            }
        }


        // Initialize previous states and initial visuals
        foreach (PowerUpInventory.PowerUpType type in System.Enum.GetValues(typeof(PowerUpInventory.PowerUpType)))
        {
           if (System.Enum.IsDefined(typeof(PowerUpInventory.PowerUpType), type))
           {
               powerUpPreviousStates[type] = DetermineStateFromCount(powerUpInventory.GetPowerUpCount(type));
               UpdatePowerUpVisual(type); // Set initial visual state (will snap, not animate)
             }
        }

        // Subscribe to inventory changes
        PowerUpInventory.OnPowerUpCountChanged += HandlePowerUpCountChanged;
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent errors and memory leaks
        PowerUpInventory.OnPowerUpCountChanged -= HandlePowerUpCountChanged;
    }

    private void HandlePowerUpCountChanged(PowerUpInventory.PowerUpType type, int newCount)
    {
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
        // Usage logic might differ slightly from visual state determination
        if (count >= SUPERCHARGED_THRESHOLD) return PowerUpState.Supercharged;
        if (count >= CHARGED_THRESHOLD) return PowerUpState.Charged;
        // Original logic didn't have a distinct 'Usable' trigger state, only Charged/Supercharged
        return PowerUpState.Unusable; // Default to Unusable if not Charged/Supercharged
    }


    /// Updates the visual representation (background sprite and fill image) of a specific power-up button.
    public void UpdatePowerUpVisual(PowerUpInventory.PowerUpType type)
    {
        if (powerUpInventory == null)
        {
            Debug.LogError("PowerUpInventory reference is not set!");
            return;
        }

        int count = powerUpInventory.GetPowerUpCount(type);
        PowerUpState currentState = DetermineStateFromCount(count);
        PowerUpState previousState = powerUpPreviousStates.ContainsKey(type) ? powerUpPreviousStates[type] : currentState; // Get previous or assume current if first time

        // Get references
        bool bgImageFound = powerUpImageMap.TryGetValue(type, out Image bgImage);
        bool fillImageFound = powerUpFillImageMap.TryGetValue(type, out Image fillImage);
        bool spritesFound = powerUpSpriteMap.TryGetValue(type, out PowerUpSprites sprites);

        if (!bgImageFound || bgImage == null) Debug.LogWarning($"Background Image for {type} not found/assigned.");
        if (!fillImageFound || fillImage == null) Debug.LogWarning($"Fill Image for {type} not found/assigned.");
        if (!spritesFound || sprites == null) Debug.LogWarning($"Sprites for {type} not found/assigned.");

        if (bgImage == null || fillImage == null || sprites == null) return; // Cannot proceed

        // Determine sprites and fill amount based on current state
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
                animFillSprite = GetSpriteForState(sprites, currentState);
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


       // --- Start Animation (if needed) ---

      // Stop existing animation for this power-up type
      if (fillCoroutines.TryGetValue(type, out Coroutine runningCoroutine) && runningCoroutine != null)
      {
          StopCoroutine(runningCoroutine);
          fillCoroutines[type] = null;
         }

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
                fillCoroutines[type] = newCoroutine;
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
             fillCoroutines[type] = StartCoroutine(AnimateFillFromZero(type, fillImage, postAnimFillAmount, secondPhaseDuration, postAnimShowFill));
         }
             else
             {
                // Ensure final state is correct even if no animation needed
                fillImage.fillAmount = animationTargetFill;
                fillImage.gameObject.SetActive(showFill);
                fillCoroutines[type] = null;
            }
        }
        else {
             // Ensure coroutine ref is cleared for downward state change
             fillCoroutines[type] = null;
        }



        // Update the previous state for the next check
        powerUpPreviousStates[type] = currentState;

        // Debug.Log($"Type: {type}, Count: {count}, State: {currentState}, PrevState: {previousState}, TargetFill: {animationTargetFill}, TriggerSwitch: {triggerInstantSwitch}");
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
            float currentFill = Mathf.LerpUnclamped(startFillAmount, targetFillAmount, curveValue);
            image.fillAmount = Mathf.Clamp01(currentFill);
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
          fillCoroutines[type] = StartCoroutine(AnimateFillFromZero(type, image, postAnimFillAmount, secondPhaseDuration, postAnimShowFill));
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
           if (fillCoroutines.ContainsKey(type))
           {
               fillCoroutines[type] = null;
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
          float currentFill = Mathf.LerpUnclamped(startFillAmount, targetFillAmount, curveValue);
          image.fillAmount = Mathf.Clamp01(currentFill);
          yield return null;
      }

      // Ensure final value and visibility
      image.fillAmount = targetFillAmount;
      if (image.gameObject.activeSelf != finalVisibility)
      {
           image.gameObject.SetActive(finalVisibility);
      }

      // Clean up coroutine reference only if this is still the active one
      if (fillCoroutines.TryGetValue(type, out Coroutine current) && current == fillCoroutines[type])
      {
           fillCoroutines[type] = null;
      }
  }


    /// Updates the visuals for all power-up buttons.
    public void UpdateAllPowerUpVisuals()
    {
        foreach (PowerUpInventory.PowerUpType type in System.Enum.GetValues(typeof(PowerUpInventory.PowerUpType)))
        {
            if (System.Enum.IsDefined(typeof(PowerUpInventory.PowerUpType), type))
            {
                 UpdatePowerUpVisual(type);
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
                Debug.LogWarning($"Unhandled PowerUpState for sprite selection: {state}");
                return sprites.unusable; // Default to unusable sprite
        }
    }


    public bool TryUsePowerUp(PowerUpInventory.PowerUpType type)
    {
        if (powerUpInventory == null)
        {
            Debug.LogError("PowerUpInventory reference is not set!");
            return false;
        }

        PowerUpState usageState = GetCurrentUsageState(type);

        switch (usageState)
        {
            case PowerUpState.Unusable:
                int currentCount = powerUpInventory.GetPowerUpCount(type);
                Debug.Log($"Power-up {type} unusable: Not enough charge ({currentCount}). Needs {CHARGED_THRESHOLD} for Charged, {SUPERCHARGED_THRESHOLD} for Supercharged.");
                return false;

            case PowerUpState.Supercharged:
                ActivateEffect(type, usageState);
                powerUpInventory.SetPowerUpCount(type, 0); // Reset count - This will trigger the event
                Debug.Log($"Used Supercharged {type}. Count for {type} reset to 0.");
                return true;

            case PowerUpState.Charged:
                ActivateEffect(type, usageState);
                powerUpInventory.DecreasePowerUpCount(type, CHARGED_THRESHOLD); // Decrease count - This will trigger the event
                Debug.Log($"Used Charged {type}. Count for {type} reduced by {CHARGED_THRESHOLD}.");
                return true;

            // Note: PowerUpState.Usable is not a trigger state for using the power-up here.
            default:
                Debug.LogWarning($"Unhandled PowerUpState in TryUsePowerUp: {usageState} for type {type}");
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

    public void UseWallPowerUp()
    {
        TryUsePowerUp(PowerUpInventory.PowerUpType.Wall);
    }

    public void UseStepsPowerUp()
    {
        TryUsePowerUp(PowerUpInventory.PowerUpType.Steps);
    }

    // --- Private Helper Methods ---

    private void ActivateEffect(PowerUpInventory.PowerUpType type, PowerUpState state)
    {
        Debug.Log($"Activating {state} effect for {type}");

        switch (type)
        {
            case PowerUpInventory.PowerUpType.Sword:
                if (state == PowerUpState.Charged) HandleSwordCharged();
                else if (state == PowerUpState.Supercharged) HandleSwordSupercharged();
                break;
            case PowerUpInventory.PowerUpType.Shield:
                if (state == PowerUpState.Charged) HandleShieldCharged();
                else if (state == PowerUpState.Supercharged) HandleShieldSupercharged();
                break;
            case PowerUpInventory.PowerUpType.Wall:
                if (state == PowerUpState.Charged) HandleWallCharged();
                else if (state == PowerUpState.Supercharged) HandleWallSupercharged();
                break;
            case PowerUpInventory.PowerUpType.Steps:
                if (state == PowerUpState.Charged) HandleStepsCharged();
                else if (state == PowerUpState.Supercharged) HandleStepsSupercharged();
                break;
            default:
                 Debug.LogWarning($"Unhandled PowerUpType {type} in ActivateEffect");
                 break;
        }
    }

    // Placeholder effect handlers
    private void HandleSwordCharged() { Debug.Log("Handling Sword Charged effect!"); }
    private void HandleSwordSupercharged() { Debug.Log("Handling Sword Supercharged effect!"); }
    private void HandleShieldCharged() { Debug.Log("Handling Shield Charged effect!"); }
    private void HandleShieldSupercharged() { Debug.Log("Handling Shield Supercharged effect!"); }
    private void HandleWallCharged() { Debug.Log("Handling Wall Charged effect!"); }
    private void HandleWallSupercharged() { Debug.Log("Handling Wall Supercharged effect!"); }
    private void HandleStepsCharged() { Debug.Log("Handling Steps Charged effect!"); }
    private void HandleStepsSupercharged() { Debug.Log("Handling Steps Supercharged effect!"); }
}