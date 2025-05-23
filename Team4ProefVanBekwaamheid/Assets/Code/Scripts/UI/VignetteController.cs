using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;

public class VignetteController : MonoBehaviour
{
    [Header("Vignette Volumes")]
    [SerializeField] private Volume _damageVignetteVolume;
    [SerializeField] private Volume _healVignetteVolume;

    [Header("Animation Settings")]
    [SerializeField] private float _fadeInDuration = 0.2f; 
    [SerializeField] private float _fadeOutDuration = 0.8f; 
    private Coroutine _damageVignetteCoroutine;
    private Coroutine _healVignetteCoroutine;

    void Start()
    {
        // Ensure vignettes are off at the start
        if (_damageVignetteVolume != null)
        {
            _damageVignetteVolume.weight = 0f;
        }
        if (_healVignetteVolume != null)
        {
            _healVignetteVolume.weight = 0f;
        }
    }

    public void PlayDamageVignette()
    {
        if (_damageVignetteVolume != null)
        {
            if (_damageVignetteCoroutine != null)
            {
                StopCoroutine(_damageVignetteCoroutine);
            }
            _damageVignetteCoroutine = StartCoroutine(AnimateVignette(_damageVignetteVolume));
        }
    }

    public void PlayHealVignette()
    {
        if (_healVignetteVolume != null)
        {
            if (_healVignetteCoroutine != null)
            {
                StopCoroutine(_healVignetteCoroutine);
            }
            _healVignetteCoroutine = StartCoroutine(AnimateVignette(_healVignetteVolume));
        }
    }

    private IEnumerator AnimateVignette(Volume vignetteVolume)
    {
        float elapsedTime = 0f;

        // Animate from 0 to 1 (Fade In)
        while (elapsedTime < _fadeInDuration)
        {
            vignetteVolume.weight = Mathf.Lerp(0f, 1f, elapsedTime / _fadeInDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        vignetteVolume.weight = 1f;

        // Animate from 1 to 0 (Fade Out)
        elapsedTime = 0f;
        while (elapsedTime < _fadeOutDuration)
        {
            vignetteVolume.weight = Mathf.Lerp(1f, 0f, elapsedTime / _fadeOutDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        vignetteVolume.weight = 0f;
    }
}