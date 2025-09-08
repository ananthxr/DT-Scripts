using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class MaterialFadeController : MonoBehaviour
{
    [Header("Fade Settings")]
    [SerializeField] private float fadeInDuration = 1f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Header("Fade Values")]
    [SerializeField] private float fadeAlpha = 0.4f;
    [SerializeField] private Color fadeColor = new Color(0.6f, 0.6f, 0.6f, 1f);
    [SerializeField] private float fadeSaturation = 0.1f;
    [SerializeField] private float fadeDesaturationStrength = 0.8f;

    private Renderer[] renderers;
    private Material[] originalMaterials;
    private Material[] fadedMaterials;
    private bool isFaded = false;
    private Coroutine currentFadeCoroutine;

    void Start()
    {
        InitializeMaterials();
    }
    
    void Awake()
    {
        // Ensure materials are initialized early
        if (renderers == null)
            InitializeMaterials();
    }

    private void InitializeMaterials()
    {
        if (renderers != null && originalMaterials != null) return; // Already initialized
        
        renderers = GetComponentsInChildren<Renderer>();
        List<Material> originals = new List<Material>();
        List<Material> faded = new List<Material>();

        foreach (var renderer in renderers)
        {
            if (renderer == null) continue;
            
            foreach (var material in renderer.materials)
            {
                if (material == null) continue;
                
                originals.Add(material);
                
                Material fadedMat = CreateFadedMaterial(material);
                faded.Add(fadedMat);
            }
        }

        originalMaterials = originals.ToArray();
        fadedMaterials = faded.ToArray();
        
        Debug.Log($"MaterialFadeController on {gameObject.name}: Initialized {originalMaterials.Length} materials");
    }

    private Material CreateFadedMaterial(Material originalMaterial)
    {
        Material fadedMat = new Material(originalMaterial);
        
        // Create a much more visible fade effect
        Color fadeTargetColor = new Color(0.5f, 0.5f, 0.5f, fadeAlpha);
        
        if (fadedMat.HasProperty("_Color"))
        {
            Color originalColor = fadedMat.color;
            Debug.Log($"Original color: {originalColor}, Setting to: {fadeTargetColor}");
            fadedMat.color = fadeTargetColor;
        }

        if (fadedMat.HasProperty("_BaseColor"))
        {
            Color originalColor = fadedMat.GetColor("_BaseColor");
            Debug.Log($"Original base color: {originalColor}, Setting to: {fadeTargetColor}");
            fadedMat.SetColor("_BaseColor", fadeTargetColor);
        }

        // Force transparency for Standard shader
        if (fadedMat.HasProperty("_Mode"))
        {
            fadedMat.SetFloat("_Mode", 3); // Transparent mode
            fadedMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            fadedMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            fadedMat.SetInt("_ZWrite", 0);
            fadedMat.DisableKeyword("_ALPHATEST_ON");
            fadedMat.EnableKeyword("_ALPHABLEND_ON");
            fadedMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            fadedMat.renderQueue = 3000;
        }
        
        // Force transparency for URP materials
        if (fadedMat.HasProperty("_Surface"))
        {
            fadedMat.SetFloat("_Surface", 1); // Transparent
            fadedMat.SetFloat("_Blend", 0);   // Alpha blend
            fadedMat.renderQueue = 3000;
        }

        // Remove emission
        if (fadedMat.HasProperty("_EmissionColor"))
        {
            fadedMat.SetColor("_EmissionColor", Color.black);
        }

        Debug.Log($"Created faded material for {originalMaterial.name}: Color set to {fadeTargetColor}");
        return fadedMat;
    }

    public void FadeOut(bool immediate = false)
    {
        // Ensure materials are initialized
        if (originalMaterials == null || fadedMaterials == null)
            InitializeMaterials();
            
        if (isFaded && currentFadeCoroutine == null) return;
        
        if (currentFadeCoroutine != null)
            StopCoroutine(currentFadeCoroutine);

        Debug.Log($"MaterialFadeController on {gameObject.name}: FadeOut called (immediate: {immediate})");
        
        if (immediate)
        {
            ApplyMaterials(fadedMaterials);
            isFaded = true;
        }
        else
        {
            currentFadeCoroutine = StartCoroutine(FadeCoroutine(false, fadeOutDuration));
        }
    }

    public void FadeIn(bool immediate = false)
    {
        // Ensure materials are initialized
        if (originalMaterials == null || fadedMaterials == null)
            InitializeMaterials();
            
        if (!isFaded && currentFadeCoroutine == null) return;
        
        if (currentFadeCoroutine != null)
            StopCoroutine(currentFadeCoroutine);

        Debug.Log($"MaterialFadeController on {gameObject.name}: FadeIn called (immediate: {immediate})");
        
        if (immediate)
        {
            ApplyMaterials(originalMaterials);
            isFaded = false;
        }
        else
        {
            currentFadeCoroutine = StartCoroutine(FadeCoroutine(true, fadeInDuration));
        }
    }

    private IEnumerator FadeCoroutine(bool fadeIn, float duration)
    {
        float elapsedTime = 0f;
        Material[] startMaterials = fadeIn ? fadedMaterials : originalMaterials;
        Material[] endMaterials = fadeIn ? originalMaterials : fadedMaterials;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            float easedProgress = fadeCurve.Evaluate(progress);

            InterpolateMaterials(startMaterials, endMaterials, easedProgress);
            yield return null;
        }

        ApplyMaterials(endMaterials);
        isFaded = !fadeIn;
        currentFadeCoroutine = null;
    }

    private void InterpolateMaterials(Material[] startMaterials, Material[] endMaterials, float progress)
    {
        int materialIndex = 0;
        foreach (var renderer in renderers)
        {
            Material[] currentMaterials = new Material[renderer.materials.Length];
            
            for (int i = 0; i < currentMaterials.Length; i++)
            {
                Material interpolatedMat = new Material(startMaterials[materialIndex]);
                Material startMat = startMaterials[materialIndex];
                Material endMat = endMaterials[materialIndex];

                if (interpolatedMat.HasProperty("_Color"))
                {
                    Color startColor = startMat.color;
                    Color endColor = endMat.color;
                    interpolatedMat.color = Color.Lerp(startColor, endColor, progress);
                }

                if (interpolatedMat.HasProperty("_BaseColor"))
                {
                    Color startColor = startMat.GetColor("_BaseColor");
                    Color endColor = endMat.GetColor("_BaseColor");
                    interpolatedMat.SetColor("_BaseColor", Color.Lerp(startColor, endColor, progress));
                }

                if (interpolatedMat.HasProperty("_EmissionColor"))
                {
                    Color startEmission = startMat.GetColor("_EmissionColor");
                    Color endEmission = endMat.GetColor("_EmissionColor");
                    interpolatedMat.SetColor("_EmissionColor", Color.Lerp(startEmission, endEmission, progress));
                }

                currentMaterials[i] = interpolatedMat;
                materialIndex++;
            }
            
            renderer.materials = currentMaterials;
        }
    }

    private void ApplyMaterials(Material[] materials)
    {
        Debug.Log($"ApplyMaterials called on {gameObject.name} with {materials.Length} materials");
        
        int materialIndex = 0;
        foreach (var renderer in renderers)
        {
            if (renderer == null) continue;
            
            Material[] rendererMaterials = new Material[renderer.materials.Length];
            for (int i = 0; i < rendererMaterials.Length; i++)
            {
                rendererMaterials[i] = materials[materialIndex];
                Debug.Log($"Applied material {materials[materialIndex].name} to renderer {renderer.name}");
                materialIndex++;
            }
            renderer.materials = rendererMaterials;
            Debug.Log($"Updated renderer {renderer.name} with {rendererMaterials.Length} materials");
        }
    }

    public bool IsFaded => isFaded;
    public bool IsTransitioning => currentFadeCoroutine != null;
}