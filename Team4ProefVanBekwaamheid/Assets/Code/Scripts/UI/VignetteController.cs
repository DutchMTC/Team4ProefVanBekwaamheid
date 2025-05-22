using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;

public class VignetteController : MonoBehaviour
{
    [Header("Vignette Volumes")]
    [SerializeField] private Volume damageVignetteVolume;
    [SerializeField] private Volume healVignetteVolume;

    [Header("Animation Settings")]
    [SerializeField] private float fadeInDuration = 0.2f; 
    [SerializeField] private float fadeOutDuration = 0.8f; 
    private Coroutine damageVignetteCoroutine;
    private Coroutine healVignetteCoroutine;

    void Start()
    {
        // Ensure vignettes are off at the start
        if (damageVignetteVolume != null)
        {
            damageVignetteVolume.weight = 0f;
        }
        if (healVignetteVolume != null)
        {
            healVignetteVolume.weight = 0f;
        }
    }

    public void PlayDamageVignette()
    {
        if (damageVignetteVolume != null)
        {
            if (damageVignetteCoroutine != null)
            {
                StopCoroutine(damageVignetteCoroutine);
            }
            damageVignetteCoroutine = StartCoroutine(AnimateVignette(damageVignetteVolume));
        }
    }

    public void PlayHealVignette()
    {
        if (healVignetteVolume != null)
        {
            if (healVignetteCoroutine != null)
            {
                StopCoroutine(healVignetteCoroutine);
            }
            healVignetteCoroutine = StartCoroutine(AnimateVignette(healVignetteVolume));
        }
    }

    private IEnumerator AnimateVignette(Volume vignetteVolume)
    {
        float elapsedTime = 0f;

        // Animate from 0 to 1 (Fade In)
        while (elapsedTime < fadeInDuration)
        {
            vignetteVolume.weight = Mathf.Lerp(0f, 1f, elapsedTime / fadeInDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        vignetteVolume.weight = 1f;

        // Animate from 1 to 0 (Fade Out)
        elapsedTime = 0f;
        while (elapsedTime < fadeOutDuration)
        {
            vignetteVolume.weight = Mathf.Lerp(1f, 0f, elapsedTime / fadeOutDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        vignetteVolume.weight = 0f;
    }
}