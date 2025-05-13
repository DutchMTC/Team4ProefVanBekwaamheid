using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CharacterHealthUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider mainHealthSlider;
    [SerializeField] private Slider damagePreviewSlider;

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
    private bool isPlayerCharacter; // Still kept, though not used for direction, might be useful for other logic
    private Coroutine topLayerFastDrainCoroutine;
    private Coroutine bottomLayerSlowDrainCoroutine;
    private Coroutine shakeCoroutine;
    private RectTransform rectTransform;
    private Vector2 originalAnchoredPosition;

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

        if (mainHealthSlider == null || damagePreviewSlider == null)
        {
            Debug.LogError("Health sliders not assigned in CharacterHealthUI.", this);
            return;
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
        mainHealthSlider.value = initialNormalizedHealth; // Top layer
        damagePreviewSlider.value = initialNormalizedHealth; // Bottom layer, initially same as top
    }

    public void OnHealthChanged(float newAbsoluteHealthValue)
    {
        if (maxHealth <= 0) return;

        if (topLayerFastDrainCoroutine != null) StopCoroutine(topLayerFastDrainCoroutine);
        if (bottomLayerSlowDrainCoroutine != null) StopCoroutine(bottomLayerSlowDrainCoroutine);
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            if(rectTransform != null) rectTransform.anchoredPosition = originalAnchoredPosition; // Reset position if shake was interrupted
        }
        
        float previousLogicalHealthNormalized = logicalCurrentHealth / maxHealth; // Health before this change
        logicalCurrentHealth = Mathf.Clamp(newAbsoluteHealthValue, 0, maxHealth);
        float targetNormalizedHealth = logicalCurrentHealth / maxHealth;

        float topLayerStartNormalized = mainHealthSlider.value;
        float bottomLayerStartNormalized = damagePreviewSlider.value;

        // Trigger shake only if actual damage is taken (health decreased)
        if (targetNormalizedHealth < previousLogicalHealthNormalized && shakeIntensity > 0 && shakeDuration > 0 && rectTransform != null)
        {
            shakeCoroutine = StartCoroutine(ShakeHealthBar());
        }
        
        topLayerFastDrainCoroutine = StartCoroutine(AnimateSlider(
            mainHealthSlider,
            topLayerStartNormalized,
            targetNormalizedHealth,
            fastDrainAnimationCurve,
            fastDrainDuration,
            0f
        ));
        
        bottomLayerSlowDrainCoroutine = StartCoroutine(AnimateSlider(
            damagePreviewSlider,
            bottomLayerStartNormalized,
            targetNormalizedHealth,
            slowDrainAnimationCurve,
            slowDrainDuration,
            slowDrainStartDelay
        ));
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
        rectTransform.anchoredPosition = originalAnchoredPosition; // Reset to original position
        shakeCoroutine = null;
    }

    private IEnumerator AnimateSlider(Slider slider, float fromNormalized, float toNormalized, AnimationCurve curve, float duration, float delay)
    {
        if (delay > 0) yield return new WaitForSeconds(delay);

        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / duration);
            float curveValue = curve.Evaluate(progress);
            slider.value = Mathf.Lerp(fromNormalized, toNormalized, curveValue);
            yield return null;
        }
        slider.value = toNormalized;
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
}