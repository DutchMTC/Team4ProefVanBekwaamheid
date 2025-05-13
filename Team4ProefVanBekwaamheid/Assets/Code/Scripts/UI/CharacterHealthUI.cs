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

    private float maxHealth;
    private float logicalCurrentHealth;
    private bool isPlayerCharacter;
    private Coroutine topLayerFastDrainCoroutine;
    private Coroutine bottomLayerSlowDrainCoroutine;

    public void Initialize(TileOccupants stats, float initialMaxHealth, float initialCurrentHealth, bool isPlayer)
    {
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
                                         
        logicalCurrentHealth = Mathf.Clamp(newAbsoluteHealthValue, 0, maxHealth);
        float targetNormalizedHealth = logicalCurrentHealth / maxHealth;

        // Current visual state of the sliders.
        // For damage, top layer (mainHealthSlider) starts fast drain from its current position.
        // Bottom layer (damagePreviewSlider) starts slow drain from its current position after a delay.
        float topLayerStartNormalized = mainHealthSlider.value;
        float bottomLayerStartNormalized = damagePreviewSlider.value;

        // If healing, both sliders should move towards the target health.
        // The "fast drain" for the top layer still applies, but it will be an increase.
        // The "slow drain" for the bottom layer also applies as an increase.
        
        topLayerFastDrainCoroutine = StartCoroutine(AnimateSlider(
            mainHealthSlider,
            topLayerStartNormalized,
            targetNormalizedHealth,
            fastDrainAnimationCurve,
            fastDrainDuration,
            0f // No delay for top layer
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