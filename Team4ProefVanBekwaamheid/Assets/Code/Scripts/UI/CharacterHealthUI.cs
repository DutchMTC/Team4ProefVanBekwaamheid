using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CharacterHealthUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider mainHealthSlider;
    [SerializeField] private Image mainSliderFillImage;
    [SerializeField] private Image mainSliderBackgroundImage;
    // mainSliderFillAreaImage is removed as only preview slider has a managed fill area sprite
    [SerializeField] private Slider damagePreviewSlider; // This is the bottom layer
    [SerializeField] private Image previewSliderFillImage;
    [SerializeField] private Image previewSliderBackgroundImage;
    [SerializeField] private Image previewSliderFillAreaImage;
    [SerializeField] private GameObject armorIcon; // Added for armor UI

    [Header("Health State Sprites")]
    [Tooltip("Sprite for the actual Fill of the sliders.")]
    [SerializeField] private Sprite fillSpriteFullHealth;
    [SerializeField] private Sprite fillSpriteMidHealth; // <= 66%
    [SerializeField] private Sprite fillSpriteLowHealth;  // <= 33%
    [SerializeField] private Sprite fillSpriteDead;     // <= 0%
    [Tooltip("Sprite for the Background of the sliders.")]
    [SerializeField] private Sprite backgroundSpriteFullHealth;
    [SerializeField] private Sprite backgroundSpriteMidHealth;
    [SerializeField] private Sprite backgroundSpriteLowHealth;
    [SerializeField] private Sprite backgroundSpriteDead;   // <= 0%
    // Removed separate Fill Area sprite fields as they will use the Fill sprites.

    [Header("Animation Settings")]
    [Tooltip("Curve for the top layer's fast drain.")]
    [SerializeField] private AnimationCurve fastDrainAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float fastDrainDuration = 0.25f;
    [Tooltip("Curve for the bottom layer's slow drain (becomes visible).")]
    [SerializeField] private AnimationCurve slowDrainAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float slowDrainDuration = 0.75f;
    [Tooltip("Delay before the bottom layer starts its slow drain after top layer starts.")]
    [SerializeField] private float slowDrainStartDelay = 0.1f;

    [Header("Shake Effect Settings")]
    [Tooltip("How far the health bar can move during shake.")]
    [SerializeField] private float shakeIntensity = 5f;
    [Tooltip("How long the shake effect lasts.")]
    [SerializeField] private float shakeDuration = 0.15f;

    private float maxHealth;
    private float logicalCurrentHealth;
    private bool isPlayerCharacter;
    private Coroutine topLayerFastDrainCoroutine;
    private Coroutine bottomLayerSlowDrainCoroutine;
    private Coroutine shakeCoroutine;
    private RectTransform rectTransform;
    private Vector2 originalAnchoredPosition;

    private const float MID_HEALTH_THRESHOLD = 0.66f;
    private const float LOW_HEALTH_THRESHOLD = 0.33f;
    private const float DEAD_HEALTH_THRESHOLD = 0f;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            originalAnchoredPosition = rectTransform.anchoredPosition;
        }
        else
        {
            Debug.LogError("CharacterHealthUI requires a RectTransform component on the same GameObject.", this);
        }
    }

    public void Initialize(TileOccupants stats, float initialMaxHealth, float initialCurrentHealth, bool isPlayer)
    {
        // Ensure original position is captured if Awake hasn't run or RectTransform was null then.
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null && originalAnchoredPosition == Vector2.zero) // A simple check, might need refinement if (0,0) is a valid start
        {
            originalAnchoredPosition = rectTransform.anchoredPosition;
        }
        
        maxHealth = initialMaxHealth;
        logicalCurrentHealth = initialCurrentHealth;
        isPlayerCharacter = isPlayer;

        // Adjusted null checks: mainSliderFillAreaImage is no longer required.
        if (mainHealthSlider == null || damagePreviewSlider == null ||
            mainSliderFillImage == null || mainSliderBackgroundImage == null ||
            previewSliderFillImage == null || previewSliderBackgroundImage == null || previewSliderFillAreaImage == null)
        {
            Debug.LogError("One or more UI references (Sliders, Fill/Background Images for main slider, or Fill/Background/FillArea Images for preview slider) not assigned in CharacterHealthUI.", this);
            return;
        }

        if (armorIcon != null)
        {
            armorIcon.SetActive(false); // Initially hide armor icon
        }

        mainHealthSlider.minValue = 0;
        mainHealthSlider.maxValue = 1;
        damagePreviewSlider.minValue = 0;
        damagePreviewSlider.maxValue = 1;

        // mainHealthSlider is the TOP, FAST-MOVING layer.
        // damagePreviewSlider is the BOTTOM, SLOW-MOVING layer that gets revealed.
        // All health bars will now fill from Left to Right.
        Slider.Direction direction = Slider.Direction.LeftToRight;
        mainHealthSlider.direction = direction;
        damagePreviewSlider.direction = direction;

        float initialNormalizedHealth = maxHealth > 0 ? logicalCurrentHealth / maxHealth : 0;
        mainHealthSlider.value = initialNormalizedHealth;
        damagePreviewSlider.value = initialNormalizedHealth;
        UpdateHealthBarSprites(initialNormalizedHealth); // Set initial sprites
    }

    public void OnHealthChanged(float newAbsoluteHealthValue)
    {
        if (maxHealth <= 0) return;

        if (topLayerFastDrainCoroutine != null) StopCoroutine(topLayerFastDrainCoroutine);
        if (bottomLayerSlowDrainCoroutine != null) StopCoroutine(bottomLayerSlowDrainCoroutine);
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            if(rectTransform != null) rectTransform.anchoredPosition = originalAnchoredPosition;
        }
        
        float previousLogicalHealthNormalized = maxHealth > 0 ? logicalCurrentHealth / maxHealth : 1f;
        logicalCurrentHealth = Mathf.Clamp(newAbsoluteHealthValue, 0, maxHealth);
        float targetNormalizedHealth = maxHealth > 0 ? logicalCurrentHealth / maxHealth : 0f;

        float topLayerStartNormalized = mainHealthSlider.value;
        float bottomLayerStartNormalized = damagePreviewSlider.value;

        // Log the values used for sprite determination to help diagnose the issue.
        Debug.Log($"[CharacterHealthUI] OnHealthChanged: newAbsoluteHealthValue={newAbsoluteHealthValue}, logicalCurrentHealth={logicalCurrentHealth}, maxHealth={maxHealth}, targetNormalizedHealth={targetNormalizedHealth}");

        // Update sprites to reflect the target health state immediately
        UpdateHealthBarSprites(targetNormalizedHealth);

        if (targetNormalizedHealth < previousLogicalHealthNormalized && shakeIntensity > 0 && shakeDuration > 0 && rectTransform != null)
        {
            shakeCoroutine = StartCoroutine(ShakeHealthBar());
        }
        
        topLayerFastDrainCoroutine = StartCoroutine(AnimateSlider(
            mainHealthSlider,
            mainSliderFillImage,
            mainSliderBackgroundImage,
            null, // No fill area image for main slider
            topLayerStartNormalized,
            targetNormalizedHealth,
            fastDrainAnimationCurve,
            fastDrainDuration,
            0f
        ));
        
        bottomLayerSlowDrainCoroutine = StartCoroutine(AnimateSlider(
            damagePreviewSlider,
            previewSliderFillImage,
            previewSliderBackgroundImage,
            previewSliderFillAreaImage, // Only preview slider has a fill area image to manage
            bottomLayerStartNormalized,
            targetNormalizedHealth,
            slowDrainAnimationCurve,
            slowDrainDuration,
            slowDrainStartDelay
        ));
    }

    // fillAreaImage can be null if not applicable (e.g., for mainHealthSlider)
    private void UpdateHealthBarSprites(float normalizedHealth, Image fillImage, Image backgroundImage, Image fillAreaImage)
    {
        // Adjusted null check for fillAreaImage
        if (fillImage == null || backgroundImage == null) return;
        // If fillAreaImage is provided, it must not be null for the logic below to proceed for it.
        // However, the core fill/background logic can proceed even if fillAreaImage is null.

        Sprite selectedFillSprite;
        Sprite selectedBackgroundSprite;
        // Sprite selectedFillAreaSprite; // No longer needed, will use selectedFillSprite

        if (normalizedHealth <= DEAD_HEALTH_THRESHOLD)
        {
            selectedFillSprite = fillSpriteDead;
            selectedBackgroundSprite = backgroundSpriteDead;
        }
        else if (normalizedHealth <= LOW_HEALTH_THRESHOLD)
        {
            selectedFillSprite = fillSpriteLowHealth;
            selectedBackgroundSprite = backgroundSpriteLowHealth;
        }
        else if (normalizedHealth <= MID_HEALTH_THRESHOLD)
        {
            selectedFillSprite = fillSpriteMidHealth;
            selectedBackgroundSprite = backgroundSpriteMidHealth;
        }
        else
        {
            selectedFillSprite = fillSpriteFullHealth;
            selectedBackgroundSprite = backgroundSpriteFullHealth;
        }

        if (fillImage.sprite != selectedFillSprite && selectedFillSprite != null)
        {
            fillImage.sprite = selectedFillSprite;
        }
        if (backgroundImage.sprite != selectedBackgroundSprite && selectedBackgroundSprite != null)
        {
            backgroundImage.sprite = selectedBackgroundSprite;
        }
        
        // Fill Area (if provided) now uses the same sprite as the Fill
        if (fillAreaImage != null) // Check if fillAreaImage is assigned before trying to use it
        {
            if (fillAreaImage.sprite != selectedFillSprite && selectedFillSprite != null)
            {
                fillAreaImage.sprite = selectedFillSprite;
            }
        }
    }
    
    private void UpdateHealthBarSprites(float normalizedHealth)
    {
        // mainSliderFillAreaImage is no longer passed here as it's not managed for main slider
        UpdateHealthBarSprites(normalizedHealth, mainSliderFillImage, mainSliderBackgroundImage, null);
        UpdateHealthBarSprites(normalizedHealth, previewSliderFillImage, previewSliderBackgroundImage, previewSliderFillAreaImage);
    }


    private IEnumerator ShakeHealthBar()
    {
        if (rectTransform == null) yield break;

        float elapsedTime = 0f;
        originalAnchoredPosition = rectTransform.anchoredPosition; // Re-capture in case it moved due to layout

        while (elapsedTime < shakeDuration)
        {
            elapsedTime += Time.deltaTime;
            float offsetX = Random.Range(-1f, 1f) * shakeIntensity;
            float offsetY = Random.Range(-1f, 1f) * shakeIntensity;
            rectTransform.anchoredPosition = originalAnchoredPosition + new Vector2(offsetX, offsetY);
            yield return null;
        }
        rectTransform.anchoredPosition = originalAnchoredPosition;
        shakeCoroutine = null;
    }

    private IEnumerator AnimateSlider(Slider slider, Image fillImage, Image backgroundImage, Image fillAreaImage, float fromNormalized, float toNormalized, AnimationCurve curve, float duration, float delay)
    {
        if (delay > 0) yield return new WaitForSeconds(delay);

        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / duration);
            float curveValue = curve.Evaluate(progress);
            float currentSliderValue = Mathf.Lerp(fromNormalized, toNormalized, curveValue);
            slider.value = currentSliderValue;
            // Sprites are now updated before animation starts, so no need to update them here per frame.
            // UpdateHealthBarSprites(currentSliderValue, fillImage, backgroundImage, fillAreaImage);
            yield return null;
        }
        slider.value = toNormalized;
        // Sprites are already set to the target state.
        // UpdateHealthBarSprites(toNormalized, fillImage, backgroundImage, fillAreaImage);
    }

    public void UpdateMaxHealth(float newMaxHealth)
    {
        maxHealth = newMaxHealth;
        if (maxHealth <= 0) // Prevent division by zero
        {
            mainHealthSlider.value = 0;
            damagePreviewSlider.value = 0;
            return;
        }
        // Recalculate and update display based on new max health and current logical health
        // This will trigger an animation if the normalized value changes.
        OnHealthChanged(logicalCurrentHealth);
    }

    public void UpdateArmorStatus(bool hasArmor)
    {
        if (armorIcon != null)
        {
            armorIcon.SetActive(hasArmor);
        }
    }
}