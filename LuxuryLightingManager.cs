using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class LuxuryLightingManager : MonoBehaviour
{
    [Header("Main Lighting")]
    [SerializeField] private Light keyLight;
    [SerializeField] private Light fillLight;
    [SerializeField] private Light rimLight;
    [SerializeField] private Light environmentLight;
    
    [Header("Key Light Settings (Main Light)")]
    [SerializeField] private Color keyLightColor = new Color(1f, 0.95f, 0.8f, 1f);
    [SerializeField] private float keyLightIntensity = 1.2f;
    [SerializeField] private Vector3 keyLightRotation = new Vector3(45f, -30f, 0f);
    [SerializeField] private LightShadows keyLightShadows = LightShadows.Soft;
    
    [Header("Fill Light Settings (Soft Secondary)")]
    [SerializeField] private Color fillLightColor = new Color(0.7f, 0.8f, 1f, 1f);
    [SerializeField] private float fillLightIntensity = 0.4f;
    [SerializeField] private Vector3 fillLightRotation = new Vector3(-15f, 120f, 0f);
    
    [Header("Rim Light Settings (Edge Highlight)")]
    [SerializeField] private Color rimLightColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private float rimLightIntensity = 0.8f;
    [SerializeField] private Vector3 rimLightRotation = new Vector3(10f, 200f, 0f);
    
    [Header("Environment Settings")]
    [SerializeField] private Color ambientColor = new Color(0.3f, 0.35f, 0.4f, 1f);
    [SerializeField] private float ambientIntensity = 0.3f;
    [SerializeField] private Gradient ambientGradient;
    
    [Header("Focus Lighting")]
    [SerializeField] private bool adjustLightingOnFocus = true;
    [SerializeField] private float focusLightIntensityMultiplier = 1.4f;
    [SerializeField] private Color focusAmbientColor = new Color(0.2f, 0.25f, 0.3f, 1f);
    
    [Header("Lighting Presets")]
    [SerializeField] private LightingPreset currentPreset = LightingPreset.Luxury;
    
    [Header("Post Processing")]
    [SerializeField] private Volume globalVolume;
    [SerializeField] private bool enablePostProcessing = true;
    
    public enum LightingPreset
    {
        Custom,
        Apple,
        Nike, 
        Luxury
    }
    
    private Camera mainCamera;
    private RTSCameraController cameraController;
    private bool originalLightingStored = false;
    private LightingState originalLighting;
    
    private struct LightingState
    {
        public float keyIntensity;
        public float fillIntensity;
        public float rimIntensity;
        public Color ambientColor;
        public float ambientIntensity;
    }

    void Start()
    {
        InitializeLighting();
        ApplyPreset();
        SetupEventListeners();
    }

    private void InitializeLighting()
    {
        mainCamera = Camera.main;
        cameraController = FindObjectOfType<RTSCameraController>();
        
        CreateLightsIfNeeded();
        SetupKeyLight();
        SetupFillLight();
        SetupRimLight();
        SetupEnvironmentLighting();
        SetupPostProcessing();
        
        StoreLightingState();
    }

    private void CreateLightsIfNeeded()
    {
        if (keyLight == null)
        {
            GameObject keyLightObj = new GameObject("Key Light");
            keyLight = keyLightObj.AddComponent<Light>();
            keyLightObj.transform.SetParent(transform);
        }

        if (fillLight == null)
        {
            GameObject fillLightObj = new GameObject("Fill Light");
            fillLight = fillLightObj.AddComponent<Light>();
            fillLightObj.transform.SetParent(transform);
        }

        if (rimLight == null)
        {
            GameObject rimLightObj = new GameObject("Rim Light");
            rimLight = rimLightObj.AddComponent<Light>();
            rimLightObj.transform.SetParent(transform);
        }
    }

    private void SetupKeyLight()
    {
        keyLight.type = LightType.Directional;
        keyLight.color = keyLightColor;
        keyLight.intensity = keyLightIntensity;
        keyLight.shadows = keyLightShadows;
        keyLight.shadowStrength = 0.6f;
        keyLight.shadowResolution = LightShadowResolution.VeryHigh;
        keyLight.transform.rotation = Quaternion.Euler(keyLightRotation);
        
        // Soft shadows for luxury feel
        if (keyLight.shadows != LightShadows.None)
        {
            keyLight.shadowNormalBias = 0.1f;
            keyLight.shadowBias = 0.001f;
        }
    }

    private void SetupFillLight()
    {
        fillLight.type = LightType.Directional;
        fillLight.color = fillLightColor;
        fillLight.intensity = fillLightIntensity;
        fillLight.shadows = LightShadows.None;
        fillLight.transform.rotation = Quaternion.Euler(fillLightRotation);
    }

    private void SetupRimLight()
    {
        rimLight.type = LightType.Directional;
        rimLight.color = rimLightColor;
        rimLight.intensity = rimLightIntensity;
        rimLight.shadows = LightShadows.None;
        rimLight.transform.rotation = Quaternion.Euler(rimLightRotation);
    }

    private void SetupEnvironmentLighting()
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = ambientColor;
        RenderSettings.ambientEquatorColor = ambientColor * 0.7f;
        RenderSettings.ambientGroundColor = ambientColor * 0.3f;
        RenderSettings.ambientIntensity = ambientIntensity;
        
        // Enable fog for atmospheric depth
        RenderSettings.fog = true;
        RenderSettings.fogColor = ambientColor;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.01f;
    }

    private void SetupPostProcessing()
    {
        if (!enablePostProcessing || globalVolume == null) return;
        
        // You can add post-processing effects here
        // This would require URP post-processing package
    }

    private void SetupEventListeners()
    {
        // Event listeners can be added here if needed
        // The RTSCameraController can call OnObjectFocused/OnFocusExited directly
    }

    public void OnObjectFocused(FocusableObject focusedObject)
    {
        if (!adjustLightingOnFocus) return;
        
        StoreLightingState();
        
        // Enhance lighting for focused object
        keyLight.intensity = keyLightIntensity * focusLightIntensityMultiplier;
        fillLight.intensity = fillLightIntensity * 1.2f;
        rimLight.intensity = rimLightIntensity * 1.3f;
        
        // Darken ambient for drama
        RenderSettings.ambientSkyColor = focusAmbientColor;
        RenderSettings.ambientEquatorColor = focusAmbientColor * 0.7f;
        RenderSettings.ambientGroundColor = focusAmbientColor * 0.3f;
        RenderSettings.ambientIntensity = ambientIntensity * 0.6f;
        
        // Adjust lights to highlight the focused object
        AdjustLightsForObject(focusedObject);
    }

    public void OnFocusExited()
    {
        if (!adjustLightingOnFocus) return;
        
        RestoreLightingState();
    }

    private void AdjustLightsForObject(FocusableObject focusedObject)
    {
        Vector3 objectPosition = focusedObject.GetFocusPosition();
        
        // Angle key light to highlight the object better
        Vector3 toObject = (objectPosition - keyLight.transform.position).normalized;
        Vector3 idealKeyDirection = Vector3.Lerp(keyLight.transform.forward, toObject, 0.3f);
        keyLight.transform.rotation = Quaternion.LookRotation(idealKeyDirection);
        
        // Position rim light to create edge lighting
        Vector3 cameraToObject = (objectPosition - mainCamera.transform.position).normalized;
        Vector3 rimDirection = Quaternion.AngleAxis(140f, Vector3.up) * cameraToObject;
        rimLight.transform.rotation = Quaternion.LookRotation(rimDirection);
    }

    private void StoreLightingState()
    {
        if (originalLightingStored) return;
        
        originalLighting = new LightingState
        {
            keyIntensity = keyLight.intensity,
            fillIntensity = fillLight.intensity,
            rimIntensity = rimLight.intensity,
            ambientColor = RenderSettings.ambientSkyColor,
            ambientIntensity = RenderSettings.ambientIntensity
        };
        
        originalLightingStored = true;
    }

    private void RestoreLightingState()
    {
        if (!originalLightingStored) return;
        
        keyLight.intensity = originalLighting.keyIntensity;
        fillLight.intensity = originalLighting.fillIntensity;
        rimLight.intensity = originalLighting.rimIntensity;
        
        RenderSettings.ambientSkyColor = originalLighting.ambientColor;
        RenderSettings.ambientEquatorColor = originalLighting.ambientColor * 0.7f;
        RenderSettings.ambientGroundColor = originalLighting.ambientColor * 0.3f;
        RenderSettings.ambientIntensity = originalLighting.ambientIntensity;
        
        // Reset light rotations
        keyLight.transform.rotation = Quaternion.Euler(keyLightRotation);
        rimLight.transform.rotation = Quaternion.Euler(rimLightRotation);
    }

    // Public methods for runtime adjustments
    public void SetLightingPreset(string presetName)
    {
        switch (presetName.ToLower())
        {
            case "apple":
                SetAppleLighting();
                break;
            case "nike":
                SetNikeLighting();
                break;
            case "luxury":
                SetLuxuryLighting();
                break;
            default:
                InitializeLighting();
                break;
        }
    }

    private void SetAppleLighting()
    {
        // Clean, minimalist Apple-style lighting
        keyLightColor = new Color(1f, 0.98f, 0.95f, 1f);
        keyLightIntensity = 1.1f;
        fillLightColor = new Color(0.8f, 0.85f, 0.9f, 1f);
        fillLightIntensity = 0.5f;
        rimLightIntensity = 0.6f;
        ambientColor = new Color(0.4f, 0.42f, 0.45f, 1f);
        
        ApplyLightingSettings();
    }

    private void SetNikeLighting()
    {
        // Dynamic, energetic Nike-style lighting
        keyLightColor = new Color(1f, 0.9f, 0.7f, 1f);
        keyLightIntensity = 1.4f;
        fillLightColor = new Color(0.6f, 0.7f, 1f, 1f);
        fillLightIntensity = 0.6f;
        rimLightIntensity = 1.0f;
        ambientColor = new Color(0.25f, 0.3f, 0.4f, 1f);
        
        ApplyLightingSettings();
    }

    private void SetLuxuryLighting()
    {
        // Premium, sophisticated lighting
        keyLightColor = new Color(1f, 0.95f, 0.85f, 1f);
        keyLightIntensity = 1.3f;
        fillLightColor = new Color(0.7f, 0.8f, 1f, 1f);
        fillLightIntensity = 0.4f;
        rimLightIntensity = 0.9f;
        ambientColor = new Color(0.3f, 0.32f, 0.35f, 1f);
        
        ApplyLightingSettings();
    }

    private void ApplyLightingSettings()
    {
        SetupKeyLight();
        SetupFillLight();
        SetupRimLight();
        SetupEnvironmentLighting();
    }

    void OnValidate()
    {
        if (Application.isPlaying)
        {
            ApplyPreset();
        }
    }
    
    private void ApplyPreset()
    {
        switch (currentPreset)
        {
            case LightingPreset.Apple:
                SetAppleLighting();
                break;
            case LightingPreset.Nike:
                SetNikeLighting();
                break;
            case LightingPreset.Luxury:
                SetLuxuryLighting();
                break;
            case LightingPreset.Custom:
                ApplyLightingSettings();
                break;
        }
    }
}