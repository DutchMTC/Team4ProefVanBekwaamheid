using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneFader : MonoBehaviour
{
    public Image fadeImage;
    public float fadeDuration = 1f;
    public bool fadeInOnStart = true;
    public float fadeInDelay = 0f;

    void Awake()
    {
        if (fadeImage == null)
        {
            CreateFadeImage();
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void CreateFadeImage()
    {
        // Check if a canvas for fading already exists to avoid duplicates if multiple faders are in a scene (though not typical for this setup)
        Canvas existingFadeCanvas = FindObjectOfType<Canvas>();
        bool canvasExists = false;
        if (existingFadeCanvas != null && existingFadeCanvas.gameObject.name == "FadeCanvasForSceneFader") {
            canvasExists = true;
        }

        GameObject canvasObject;
        if (canvasExists) {
            canvasObject = existingFadeCanvas.gameObject;
        } else {
            canvasObject = new GameObject("FadeCanvasForSceneFader"); // Unique name
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000; // High sorting order
        }


        // Attempt to find an existing FadeImage on this canvas
        Image existingImage = null;
        if (canvasExists) {
            existingImage = canvasObject.GetComponentInChildren<Image>();
             if (existingImage != null && existingImage.gameObject.name == "FadeImageForSceneFader") {
                fadeImage = existingImage;
             } else {
                existingImage = null; // Not the one we are looking for
             }
        }

        if (fadeImage == null) { // Only create if not found or not assigned
            GameObject imageObject = new GameObject("FadeImageForSceneFader"); // Unique name
            imageObject.transform.SetParent(canvasObject.transform, false);
            fadeImage = imageObject.AddComponent<Image>();
        }

        fadeImage.rectTransform.anchorMin = Vector2.zero;
        fadeImage.rectTransform.anchorMax = Vector2.one;
        fadeImage.rectTransform.offsetMin = Vector2.zero;
        fadeImage.rectTransform.offsetMax = Vector2.zero;
        
        Color initialColor = Color.black;
        // If fadeInOnStart is true, it will be set opaque in OnSceneLoaded, otherwise transparent.
        initialColor.a = 0f; 
        fadeImage.color = initialColor;
        fadeImage.gameObject.SetActive(true); // Active, visibility controlled by alpha
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // This SceneFader instance is specific to the scene it's in.
        // It will only act if it's enabled and present in the newly loaded scene.
        if (!this.enabled || !this.gameObject.activeInHierarchy) return;

        if (fadeImage == null)
        {
            // This might happen if the SceneFader was added to a scene but the image wasn't set up.
            CreateFadeImage(); 
            if (fadeImage == null)
            {
                Debug.LogError("SceneFader: CRITICAL - Failed to create or find fadeImage in OnSceneLoaded. Fading will not occur for scene '" + scene.name + "'.");
                return;
            }
        }

        if (fadeInOnStart)
        {
            Color c = fadeImage.color;
            c.a = 1f; // Prepare for fade-in by making it opaque
            fadeImage.color = c;
            fadeImage.gameObject.SetActive(true);
            StartCoroutine(StartFadeInInternal());
        }
        else
        {
            // If no fade-in, ensure it's transparent and potentially inactive
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
            fadeImage.gameObject.SetActive(false); // Can be set inactive if not fading in
        }
    }

    private IEnumerator StartFadeInInternal()
    {
        yield return new WaitForEndOfFrame(); // Ensure scene is rendered once opaque

        if (fadeInDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(fadeInDelay);
        }

        if (fadeImage == null)
        {
            Debug.LogError("SceneFader: StartFadeInInternal - fadeImage is NULL. Aborting fade-in.");
            yield break;
        }
        
        // Ensure it's opaque before starting fade to transparent
        Color preFadeColor = fadeImage.color;
        preFadeColor.a = 1f;
        fadeImage.color = preFadeColor;
        fadeImage.gameObject.SetActive(true);

        StartCoroutine(PerformFade(0f, fadeDuration));
    }

    private IEnumerator PerformFade(float targetAlpha, float duration, System.Action onComplete = null)
    {
        if (fadeImage == null)
        {
            Debug.LogError("SceneFader: PerformFade - fadeImage is null. Cannot perform fade.");
            onComplete?.Invoke();
            yield break;
        }

        fadeImage.gameObject.SetActive(true); // Ensure image is active for fading
        float startAlpha = fadeImage.color.a;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(elapsedTime / duration));
            Color color = fadeImage.color;
            color.a = newAlpha;
            fadeImage.color = color;
            yield return null;
        }

        Color finalColor = fadeImage.color;
        finalColor.a = targetAlpha;
        fadeImage.color = finalColor;

        if (targetAlpha == 0f)
        {
            fadeImage.gameObject.SetActive(false); // Deactivate if fully transparent
        }

        onComplete?.Invoke();
    }

    // INSTANCE METHOD: Call this on a SceneFader component in your current scene
    public void FadeToScene(string sceneName)
    {
        if (fadeImage == null) {
            // Attempt to create/find image if one isn't assigned, for robustness
            CreateFadeImage();
            if (fadeImage == null) {
                 Debug.LogError("SceneFader: FadeToScene - fadeImage is null and could not be created. Loading scene directly: " + sceneName);
                 SceneManager.LoadScene(sceneName);
                 return;
            }
        }
        StartCoroutine(FadeOutAndLoadSceneInternal(sceneName));
    }

    // INSTANCE METHOD: Call this on a SceneFader component in your current scene
    public void FadeToScene(int sceneIndex)
    {
         if (fadeImage == null) {
            CreateFadeImage();
            if (fadeImage == null) {
                 Debug.LogError("SceneFader: FadeToScene - fadeImage is null and could not be created. Loading scene directly by index: " + sceneIndex);
                 SceneManager.LoadScene(sceneIndex);
                 return;
            }
        }
        StartCoroutine(FadeOutAndLoadSceneInternal(sceneIndex));
    }

    private IEnumerator FadeOutAndLoadSceneInternal(object sceneIdentifier)
    {
        if (fadeImage == null) // Should have been handled by public FadeToScene, but double check
        {
            Debug.LogError("SceneFader: CRITICAL - fadeImage is null in FadeOutAndLoadSceneInternal. Loading scene directly.");
            LoadSceneByIdentifier(sceneIdentifier);
            yield break;
        }

        // Prepare for fade-out: set image to transparent initially, then fade to opaque
        Color c = fadeImage.color;
        c.a = 0f;
        fadeImage.color = c;
        fadeImage.gameObject.SetActive(true);

        yield return StartCoroutine(PerformFade(1f, fadeDuration, () => {
            LoadSceneByIdentifier(sceneIdentifier);
        }));
    }

    private void LoadSceneByIdentifier(object sceneIdentifier)
    {
        if (sceneIdentifier is string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }
        else if (sceneIdentifier is int sceneIndex)
        {
            SceneManager.LoadScene(sceneIndex);
        }
        else
        {
            Debug.LogError("SceneFader: LoadSceneByIdentifier - Invalid scene identifier type.");
        }
    }
}