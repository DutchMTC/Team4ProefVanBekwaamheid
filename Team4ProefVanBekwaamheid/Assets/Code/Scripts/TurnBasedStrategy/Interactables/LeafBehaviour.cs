using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeafBehaviour : MonoBehaviour
{

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space))
        {
            StartFadeOut(1f);
        }
    }
    public void StartFadeOut(float duration = 1f)
    {
        StartCoroutine(FadeOutCoroutine(duration));
    }    
    private IEnumerator FadeOutCoroutine(float duration)
    {
        // Get all MeshRenderers in children recursively
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>(true);
        if (renderers == null || renderers.Length == 0 || renderers[0].material == null) 
        {
            yield break;
        }

        // We'll apply the fade to all child renderers
        Material[] materials = new Material[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            materials[i] = renderers[i].material;
        }

        // Configure all materials for alpha clipping
        foreach (Material mat in materials)
        {
            mat.SetFloat("_AlphaClip", 1f);
            mat.EnableKeyword("_ALPHATEST_ON");
            mat.SetFloat("_Surface", 0f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            mat.SetInt("_ZWrite", 1);
        }

        float startCutoff = 0f; // Start fully visible
        float targetCutoff = 1f; // End fully clipped
        
        float elapsedTime = 0;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            
            // Use smooth step for a more pleasing fade effect
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);
            float currentCutoff = Mathf.Lerp(startCutoff, targetCutoff, smoothProgress);
            
            // Update all materials
            foreach (Material mat in materials)
            {
                if (mat.HasProperty("_Cutoff"))
                {
                    mat.SetFloat("_Cutoff", currentCutoff);
                }
            }
              if (progress >= 0.95f) // In the last 5% of the animation
            {
                // Calculate extra smoothing for the very end
                float finalSmoothing = Mathf.SmoothStep(0f, 1f, (progress - 0.95f) / 0.05f);
                currentCutoff = Mathf.Lerp(currentCutoff, targetCutoff, finalSmoothing);
            }
            
            yield return null;
        }
        
        // Immediately destroy when the fade is complete
        Destroy(gameObject);
    }
}
