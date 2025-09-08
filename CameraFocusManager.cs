using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class CameraFocusManager : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private CinematicCameraController cameraController;
    
    [Header("Focus Settings")]
    [SerializeField] private LayerMask focusableLayer = -1;
    
    [Header("Exit Focus")]
    [SerializeField] private KeyCode exitFocusKey = KeyCode.Escape;
    [SerializeField] private bool allowClickToExitFocus = true;
    
    [Header("Lighting Integration")]
    [SerializeField] private bool adjustLightingOnFocus = true;
    
    private static CameraFocusManager instance;
    private FocusableObject currentFocusedObject;
    private List<MaterialFadeController> allFadeControllers;
    private LuxuryLightingManager lightingManager;
    private bool isInFocusMode = false;

    public static CameraFocusManager Instance
    {
        get
        {
            if (instance == null)
                instance = FindObjectOfType<CameraFocusManager>();
            return instance;
        }
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        InitializeSystem();
    }

    void Update()
    {
        HandleInput();
    }

    private void InitializeSystem()
    {
        if (cameraController == null)
            cameraController = FindObjectOfType<CinematicCameraController>();
            
        if (lightingManager == null)
            lightingManager = FindObjectOfType<LuxuryLightingManager>();

        RefreshFadeControllers();
    }

    private void RefreshFadeControllers()
    {
        allFadeControllers = FindObjectsOfType<MaterialFadeController>().ToList();
        CreateMissingFadeControllers();
    }
    
    private void CreateMissingFadeControllers()
    {
        Renderer[] allRenderers = FindObjectsOfType<Renderer>();
        
        foreach (var renderer in allRenderers)
        {
            // Skip UI elements, particle systems, etc.
            if (renderer.gameObject.layer == LayerMask.NameToLayer("UI")) continue;
            if (renderer is ParticleSystemRenderer) continue;
            if (renderer is LineRenderer) continue;
            if (renderer is TrailRenderer) continue;
            
            // Skip objects that have FocusableObject component or are children of one
            if (IsPartOfFocusableObject(renderer.gameObject)) continue;
            
            // Add fade controller if missing
            if (renderer.GetComponent<MaterialFadeController>() == null)
            {
                MaterialFadeController newController = renderer.gameObject.AddComponent<MaterialFadeController>();
                allFadeControllers.Add(newController);
            }
        }
    }

    private bool IsPartOfFocusableObject(GameObject obj)
    {
        Transform current = obj.transform;
        while (current != null)
        {
            if (current.GetComponent<FocusableObject>() != null)
                return true;
            current = current.parent;
        }
        return false;
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(exitFocusKey))
        {
            ExitFocus();
        }

        if (allowClickToExitFocus && currentFocusedObject != null && Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, focusableLayer))
            {
                FocusableObject clickedObject = hit.collider.GetComponent<FocusableObject>();
                if (clickedObject == null || clickedObject != currentFocusedObject)
                {
                    ExitFocus();
                }
            }
            else
            {
                ExitFocus();
            }
        }
    }

    public void FocusOnObject(FocusableObject targetObject)
    {
        if (targetObject == null || cameraController == null || cameraController.IsTransitioning) return;

        if (currentFocusedObject != null)
        {
            currentFocusedObject.SetFocused(false);
        }

        currentFocusedObject = targetObject;
        targetObject.SetFocused(true);
        isInFocusMode = true;

        // Simple focus - move camera to a good viewing position
        Vector3 focusPosition = targetObject.GetFocusPosition();
        Vector3 cameraOffset = targetObject.GetCameraPosition() - focusPosition;
        
        StartCoroutine(SmoothFocusTransition(focusPosition + cameraOffset, focusPosition));
        
        FadeNonFocusedObjects(targetObject);
        
        // Adjust lighting for luxury effect
        if (adjustLightingOnFocus && lightingManager != null)
        {
            lightingManager.OnObjectFocused(targetObject);
        }
    }

    private System.Collections.IEnumerator SmoothFocusTransition(Vector3 targetPos, Vector3 lookAtPos)
    {
        Vector3 startPos = Camera.main.transform.position;
        Quaternion startRot = Camera.main.transform.rotation;
        Quaternion targetRot = Quaternion.LookRotation((lookAtPos - targetPos).normalized);
        
        float duration = 1.5f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            
            Camera.main.transform.position = Vector3.Lerp(startPos, targetPos, t);
            Camera.main.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            
            yield return null;
        }
        
        Camera.main.transform.position = targetPos;
        Camera.main.transform.rotation = targetRot;
    }

    public void ExitFocus()
    {
        if (currentFocusedObject == null) return;

        currentFocusedObject.SetFocused(false);
        currentFocusedObject = null;
        isInFocusMode = false;

        // Return camera to home position
        if (cameraController != null)
        {
            cameraController.ReturnHome();
        }

        RestoreAllObjects();
        
        // Restore original lighting
        if (adjustLightingOnFocus && lightingManager != null)
        {
            lightingManager.OnFocusExited();
        }
    }

    private void FadeNonFocusedObjects(FocusableObject focusedObject)
    {
        RefreshFadeControllers();
        
        foreach (var fadeController in allFadeControllers)
        {
            if (fadeController == null) continue;
            
            bool isPartOfFocusedObject = IsChildOfObject(fadeController.gameObject, focusedObject.gameObject);
            
            if (isPartOfFocusedObject)
            {
                fadeController.FadeIn(true);
            }
            else
            {
                fadeController.FadeOut();
            }
        }
    }

    private void RestoreAllObjects()
    {
        foreach (var fadeController in allFadeControllers)
        {
            if (fadeController != null)
            {
                fadeController.FadeIn();
            }
        }
    }

    private bool IsChildOfObject(GameObject child, GameObject parent)
    {
        Transform currentTransform = child.transform;
        while (currentTransform != null)
        {
            if (currentTransform.gameObject == parent)
                return true;
            currentTransform = currentTransform.parent;
        }
        return false;
    }

    public void RegisterFadeController(MaterialFadeController controller)
    {
        if (allFadeControllers == null)
            allFadeControllers = new List<MaterialFadeController>();
            
        if (!allFadeControllers.Contains(controller))
            allFadeControllers.Add(controller);
    }

    public void UnregisterFadeController(MaterialFadeController controller)
    {
        if (allFadeControllers != null)
            allFadeControllers.Remove(controller);
    }

    public bool IsTransitioning => cameraController != null && cameraController.IsTransitioning;
    public bool HasFocusedObject => currentFocusedObject != null;
    public FocusableObject CurrentFocusedObject => currentFocusedObject;
    public bool IsInFocusMode => isInFocusMode;

    void OnDrawGizmos()
    {
        if (currentFocusedObject != null)
        {
            Gizmos.color = Color.magenta;
            Vector3 focusPos = currentFocusedObject.GetFocusPosition();
            Gizmos.DrawWireSphere(focusPos, 1f);
            Gizmos.DrawLine(Camera.main.transform.position, focusPos);
        }
    }
}