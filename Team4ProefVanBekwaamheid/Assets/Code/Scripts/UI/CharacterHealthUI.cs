using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CharacterHealthUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider _mainHealthSlider;
    [SerializeField] private Image _mainSliderFillImage;
    [SerializeField] private Image _mainSliderBackgroundImage;
    // mainSliderFillAreaImage is removed as only preview slider has a managed fill area sprite
    [SerializeField] private Slider _damagePreviewSlider; // This is the bottom layer
    [SerializeField] private Image _previewSliderFillImage;
    [SerializeField] private Image _previewSliderBackgroundImage;
    [SerializeField] private Image _previewSliderFillAreaImage;
    [SerializeField] private GameObject _armorIcon; // Added for armor UI

    [Header("Health State Sprites")]
    [Tooltip("Sprite for the actual Fill of the sliders.")]
    [SerializeField] private Sprite _fillSpriteFullHealth;
    [SerializeField] private Sprite _fillSpriteMidHealth; // <= 66%
    [SerializeField] private Sprite _fillSpriteLowHealth;  // <= 33%
    [SerializeField] private Sprite _fillSpriteDead;     // <= 0%
    [Tooltip("Sprite for the Background of the sliders.")]
    [SerializeField] private Sprite _backgroundSpriteFullHealth;
    [SerializeField] private Sprite _backgroundSpriteMidHealth;
    [SerializeField] private Sprite _backgroundSpriteLowHealth;
    [SerializeField] private Sprite _backgroundSpriteDead;   // <= 0%
    // Removed separate Fill Area sprite fields as they will use the Fill sprites.

    [Header("Animation Settings")]
    [Tooltip("Curve for the top layer's fast drain.")]
    [SerializeField] private AnimationCurve _fastDrainAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float _fastDrainDuration = 0.25f;
    [Tooltip("Curve for the bottom layer's slow drain (becomes visible).")]
    [SerializeField] private AnimationCurve _slowDrainAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float _slowDrainDuration = 0.75f;
    [Tooltip("Delay before the bottom layer starts its slow drain after top layer starts.")]
    [SerializeField] private float _slowDrainStartDelay = 0.1f;

    [Header("Shake Effect Settings")]
    [Tooltip("How far the health bar can move during shake.")]
    [SerializeField] private float _shakeIntensity = 5f;
    [Tooltip("How long the shake effect lasts.")]
    [SerializeField] private float _shakeDuration = 0.15f;

    private float _maxHealth;
    private float _logicalCurrentHealth;
    private bool _isPlayerCharacter;
    private Coroutine _topLayerFastDrainCoroutine;
    private Coroutine _bottomLayerSlowDrainCoroutine;
    private Coroutine _shakeCoroutine;
    private RectTransform _rectTransform;
    private Vector2 _originalAnchoredPosition;

    private const float MID_HEALTH_THRESHOLD = 0.66f;
    private const float LOW_HEALTH_THRESHOLD = 0.33f;
    private const float DEAD_HEALTH_THRESHOLD = 0f;

    void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        if (_rectTransform != null)
        {
            _originalAnchoredPosition = _rectTransform.anchoredPosition;
        }
    }

    public void Initialize(TileOccupants stats, float initialMaxHealth, float initialCurrentHealth, bool isPlayer)
    {
        // Ensure original position is captured if Awake hasn't run or RectTransform was null then.
        if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>();
        if (_rectTransform != null && _originalAnchoredPosition == Vector2.zero) // A simple check, might need refinement if (0,0) is a valid start
        {
            _originalAnchoredPosition = _rectTransform.anchoredPosition;
        }
        
        _maxHealth = initialMaxHealth;
        _logicalCurrentHealth = initialCurrentHealth;
        _isPlayerCharacter = isPlayer;

        // Adjusted null checks: mainSliderFillAreaImage is no longer required.
        if (_mainHealthSlider == null || _damagePreviewSlider == null ||
            _mainSliderFillImage == null || _mainSliderBackgroundImage == null ||
            _previewSliderFillImage == null || _previewSliderBackgroundImage == null || _previewSliderFillAreaImage == null)
        {
            return;
        }

        if (_armorIcon != null)
        {
            _armorIcon.SetActive(false); // Initially hide armor icon
        }

        _mainHealthSlider.minValue = 0;
        _mainHealthSlider.maxValue = 1;
        _damagePreviewSlider.minValue = 0;
        _damagePreviewSlider.maxValue = 1;

        // mainHealthSlider is the TOP, FAST-MOVING layer.
        // damagePreviewSlider is the BOTTOM, SLOW-MOVING layer that gets revealed.
        // All health bars will now fill from Left to Right.
        Slider.Direction direction = Slider.Direction.LeftToRight;
        _mainHealthSlider.direction = direction;
        _damagePreviewSlider.direction = direction;

        float initialNormalizedHealth = _maxHealth > 0 ? _logicalCurrentHealth / _maxHealth : 0;
        _mainHealthSlider.value = initialNormalizedHealth;
        _damagePreviewSlider.value = initialNormalizedHealth;
        UpdateHealthBarSprites(initialNormalizedHealth); // Set initial sprites
    }

    public void OnHealthChanged(float newAbsoluteHealthValue)
    {
        if (_maxHealth <= 0) return;

        if (_topLayerFastDrainCoroutine != null) StopCoroutine(_topLayerFastDrainCoroutine);
        if (_bottomLayerSlowDrainCoroutine != null) StopCoroutine(_bottomLayerSlowDrainCoroutine);
        if (_shakeCoroutine != null)
        {
            StopCoroutine(_shakeCoroutine);
            if(_rectTransform != null) _rectTransform.anchoredPosition = _originalAnchoredPosition;
        }
        
        float previousLogicalHealthNormalized = _maxHealth > 0 ? _logicalCurrentHealth / _maxHealth : 1f;
        _logicalCurrentHealth = Mathf.Clamp(newAbsoluteHealthValue, 0, _maxHealth);
        float targetNormalizedHealth = _maxHealth > 0 ? _logicalCurrentHealth / _maxHealth : 0f;

        float topLayerStartNormalized = _mainHealthSlider.value;
        float bottomLayerStartNormalized = _damagePreviewSlider.value;
        // Update sprites to reflect the target health state immediately
        UpdateHealthBarSprites(targetNormalizedHealth);

        if (targetNormalizedHealth < previousLogicalHealthNormalized && _shakeIntensity > 0 && _shakeDuration > 0 && _rectTransform != null)
        {
            _shakeCoroutine = StartCoroutine(ShakeHealthBar());
        }
        
        _topLayerFastDrainCoroutine = StartCoroutine(AnimateSlider(
            _mainHealthSlider,
            _mainSliderFillImage,
            _mainSliderBackgroundImage,
            null, // No fill area image for main slider
            topLayerStartNormalized,
            targetNormalizedHealth,
            _fastDrainAnimationCurve,
            _fastDrainDuration,
            0f
        ));
        
        _bottomLayerSlowDrainCoroutine = StartCoroutine(AnimateSlider(
            _damagePreviewSlider,
            _previewSliderFillImage,
            _previewSliderBackgroundImage,
            _previewSliderFillAreaImage, // Only preview slider has a fill area image to manage
            bottomLayerStartNormalized,
            targetNormalizedHealth,
            _slowDrainAnimationCurve,
            _slowDrainDuration,
            _slowDrainStartDelay
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
            selectedFillSprite = _fillSpriteDead;
            selectedBackgroundSprite = _backgroundSpriteDead;
        }
        else if (normalizedHealth <= LOW_HEALTH_THRESHOLD)
        {
            selectedFillSprite = _fillSpriteLowHealth;
            selectedBackgroundSprite = _backgroundSpriteLowHealth;
        }
        else if (normalizedHealth <= MID_HEALTH_THRESHOLD)
        {
            selectedFillSprite = _fillSpriteMidHealth;
            selectedBackgroundSprite = _backgroundSpriteMidHealth;
        }
        else
        {
            selectedFillSprite = _fillSpriteFullHealth;
            selectedBackgroundSprite = _backgroundSpriteFullHealth;
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
        UpdateHealthBarSprites(normalizedHealth, _mainSliderFillImage, _mainSliderBackgroundImage, null);
        UpdateHealthBarSprites(normalizedHealth, _previewSliderFillImage, _previewSliderBackgroundImage, _previewSliderFillAreaImage);
    }


    private IEnumerator ShakeHealthBar()
    {
        if (_rectTransform == null) yield break;

        float elapsedTime = 0f;
        _originalAnchoredPosition = _rectTransform.anchoredPosition; // Re-capture in case it moved due to layout

        while (elapsedTime < _shakeDuration)
        {
            elapsedTime += Time.deltaTime;
            float offsetX = Random.Range(-1f, 1f) * _shakeIntensity;
            float offsetY = Random.Range(-1f, 1f) * _shakeIntensity;
            _rectTransform.anchoredPosition = _originalAnchoredPosition + new Vector2(offsetX, offsetY);
            yield return null;
        }
        _rectTransform.anchoredPosition = _originalAnchoredPosition;
        _shakeCoroutine = null;
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
        _maxHealth = newMaxHealth;
        if (_maxHealth <= 0) // Prevent division by zero
        {
            _mainHealthSlider.value = 0;
            _damagePreviewSlider.value = 0;
            return;
        }
        // Recalculate and update display based on new max health and current logical health
        // This will trigger an animation if the normalized value changes.
        OnHealthChanged(_logicalCurrentHealth);
    }

    public void UpdateArmorStatus(bool hasArmor)
    {
        if (_armorIcon != null)
        {
            _armorIcon.SetActive(hasArmor);
        }
    }
}