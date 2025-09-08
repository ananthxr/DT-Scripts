using UnityEngine;
using System.Collections;

public class RTSCameraController : MonoBehaviour
{
    [Header("Transforms")]
    [SerializeField] private Transform homeTransform;
    [SerializeField] private Transform targetTransform;
    
    [Header("Movement Settings")]
    [SerializeField] private float orbitSensitivity = 2f;
    [SerializeField] private float panSensitivity = 1f;
    [SerializeField] private float zoomSensitivity = 5f;
    
    [Header("Limits")]
    [SerializeField] private float minZoomDistance = 5f;
    [SerializeField] private float maxZoomDistance = 50f;
    [SerializeField] private float minVerticalAngle = -80f;
    [SerializeField] private float maxVerticalAngle = 80f;
    
    [Header("Smoothing")]
    [SerializeField] private float rotationSmoothness = 8f;
    [SerializeField] private float panSmoothness = 6f;
    [SerializeField] private float zoomSmoothness = 5f;
    [SerializeField] private float homeTransitionDuration = 2f;
    [SerializeField] private float focusTransitionDuration = 1.5f;
    
    [Header("Auto Rotation")]
    [SerializeField] private bool enableAutoRotation = true;
    [SerializeField] private float autoRotationSpeed = 10f;
    [SerializeField] private float idleTimeBeforeAutoRotation = 3f;
    
    // Current state
    private Vector3 currentOrbitCenter;
    private Vector3 targetOrbitCenter;
    private float currentDistance;
    private float targetDistance;
    private float horizontalAngle;
    private float verticalAngle;
    private float targetHorizontalAngle;
    private float targetVerticalAngle;
    
    // Input state
    private bool isDragging = false;
    private bool isPanning = false;
    private Vector3 lastMousePosition;
    
    // Transition state
    private bool isTransitioning = false;
    
    // Auto rotation state
    private float lastInputTime;
    private bool hasUserInput = false;
    
    // Focus state
    private bool isInFocusMode = false;
    private Vector3 originalOrbitCenter;
    private float originalDistance;
    private float originalHorizontalAngle;
    private float originalVerticalAngle;
    private FocusableObject currentFocusedObject;
    
    void Start()
    {
        InitializeCamera();
    }
    
    void Update()
    {
        if (!isTransitioning)
        {
            HandleInput();
            UpdateCameraPosition();
            HandleAutoRotation();
        }
    }
    
    private void InitializeCamera()
    {
        if (homeTransform == null || targetTransform == null)
        {
            Debug.LogError("RTSCameraController: Home and Target transforms must be assigned!");
            return;
        }
        
        // Position camera at home transform
        transform.position = homeTransform.position;
        transform.rotation = homeTransform.rotation;
        
        // Set orbit center to target position
        currentOrbitCenter = targetTransform.position;
        targetOrbitCenter = currentOrbitCenter;
        
        // Calculate initial distance and angles based on home position
        Vector3 directionFromTarget = transform.position - currentOrbitCenter;
        currentDistance = directionFromTarget.magnitude;
        targetDistance = currentDistance;
        
        // Calculate angles from the direction
        horizontalAngle = Mathf.Atan2(directionFromTarget.x, directionFromTarget.z) * Mathf.Rad2Deg;
        verticalAngle = Mathf.Asin(directionFromTarget.normalized.y) * Mathf.Rad2Deg;
        
        // Initialize target angles
        targetHorizontalAngle = horizontalAngle;
        targetVerticalAngle = verticalAngle;
        
        // Initialize input tracking
        lastInputTime = Time.time;
        hasUserInput = false;
    }
    
    private void HandleInput()
    {
        hasUserInput = false;
        
        // ESC key to return home or exit focus
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isInFocusMode)
                ExitFocus();
            else
                ReturnToHome();
            return;
        }
        
        // Mouse input handling
        HandleMouseInput();
        
        // Scroll wheel for zoom
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            targetDistance -= scroll * zoomSensitivity;
            targetDistance = Mathf.Clamp(targetDistance, minZoomDistance, maxZoomDistance);
            hasUserInput = true;
        }
        
        // Update input timer
        if (hasUserInput)
        {
            lastInputTime = Time.time;
        }
    }
    
    private void HandleMouseInput()
    {
        // Left mouse button for orbiting
        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            lastMousePosition = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }
        
        // Right mouse button for panning (disabled in focus mode)
        if (Input.GetMouseButtonDown(1) && !isInFocusMode)
        {
            isPanning = true;
            lastMousePosition = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(1))
        {
            isPanning = false;
        }
        
        // Handle dragging
        if (isDragging)
        {
            Vector3 mouseDelta = Input.mousePosition - lastMousePosition;
            
            targetHorizontalAngle += mouseDelta.x * orbitSensitivity * 0.1f;
            targetVerticalAngle -= mouseDelta.y * orbitSensitivity * 0.1f;
            targetVerticalAngle = Mathf.Clamp(targetVerticalAngle, minVerticalAngle, maxVerticalAngle);
            
            lastMousePosition = Input.mousePosition;
            hasUserInput = true;
        }
        else if (isPanning)
        {
            Vector3 mouseDelta = Input.mousePosition - lastMousePosition;
            
            // Simple horizontal panning like Clash of Clans - no vertical movement
            Vector3 right = transform.right;
            Vector3 forward = Vector3.Cross(Vector3.up, right).normalized; // Forward on ground plane
            
            Vector3 panMovement = (-right * mouseDelta.x + forward * mouseDelta.y) * panSensitivity * 0.01f;
            targetOrbitCenter += panMovement;
            
            lastMousePosition = Input.mousePosition;
            hasUserInput = true;
        }
    }
    
    private void UpdateCameraPosition()
    {
        // Handle focus mode separately
        if (isInFocusMode)
        {
            UpdateFocusCameraPosition();
            return;
        }
        
        // Smooth rotation angles
        horizontalAngle = Mathf.LerpAngle(horizontalAngle, targetHorizontalAngle, Time.deltaTime * rotationSmoothness);
        verticalAngle = Mathf.Lerp(verticalAngle, targetVerticalAngle, Time.deltaTime * rotationSmoothness);
        
        // Smooth panning (orbit center movement)
        currentOrbitCenter = Vector3.Lerp(currentOrbitCenter, targetOrbitCenter, Time.deltaTime * panSmoothness);
        
        // Smooth zoom
        currentDistance = Mathf.Lerp(currentDistance, targetDistance, Time.deltaTime * zoomSmoothness);
        
        // Calculate position based on smoothed angles and distance
        Quaternion rotation = Quaternion.Euler(verticalAngle, horizontalAngle, 0f);
        Vector3 direction = rotation * Vector3.back;
        Vector3 targetPosition = currentOrbitCenter + direction * currentDistance;
        
        // Set position directly - no position smoothing, just angle and pan smoothing
        transform.position = targetPosition;
        
        // Always look at the orbit center
        transform.LookAt(currentOrbitCenter, Vector3.up);
    }
    
    private void HandleAutoRotation()
    {
        if (!enableAutoRotation || isTransitioning || isDragging || isPanning || isInFocusMode)
            return;
            
        float timeSinceInput = Time.time - lastInputTime;
        if (timeSinceInput > idleTimeBeforeAutoRotation)
        {
            targetHorizontalAngle += autoRotationSpeed * Time.deltaTime;
        }
    }
    
    public void FocusOnObject(FocusableObject focusableObject)
    {
        if (isTransitioning) return;
        
        StartCoroutine(SmoothFocusTransition(focusableObject));
    }
    
    public void ExitFocus()
    {
        if (!isInFocusMode) return;
        
        StartCoroutine(SmoothExitFocus());
    }
    
    public void ReturnToHome()
    {
        if (homeTransform == null || targetTransform == null || isTransitioning)
            return;
        
        // Exit focus mode if active
        if (isInFocusMode)
        {
            isInFocusMode = false;
        }
            
        StartCoroutine(SmoothReturnHome());
    }
    
    private IEnumerator SmoothReturnHome()
    {
        isTransitioning = true;
        
        Vector3 startPosition = transform.position;
        Vector3 startOrbitCenter = currentOrbitCenter;
        float startDistance = currentDistance;
        float startHorizontalAngle = horizontalAngle;
        float startVerticalAngle = verticalAngle;
        
        // Calculate target values from home transform
        Vector3 homeTargetOrbitCenter = targetTransform.position;
        Vector3 directionFromTarget = homeTransform.position - homeTargetOrbitCenter;
        float homeTargetDistance = directionFromTarget.magnitude;
        float targetHorizontalAngle = Mathf.Atan2(directionFromTarget.x, directionFromTarget.z) * Mathf.Rad2Deg;
        float targetVerticalAngle = Mathf.Asin(directionFromTarget.normalized.y) * Mathf.Rad2Deg;
        
        float elapsed = 0f;
        
        while (elapsed < homeTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / homeTransitionDuration);
            
            // Smoothly interpolate all camera parameters
            currentOrbitCenter = Vector3.Lerp(startOrbitCenter, homeTargetOrbitCenter, t);
            currentDistance = Mathf.Lerp(startDistance, homeTargetDistance, t);
            targetDistance = currentDistance;
            horizontalAngle = Mathf.LerpAngle(startHorizontalAngle, targetHorizontalAngle, t);
            verticalAngle = Mathf.Lerp(startVerticalAngle, targetVerticalAngle, t);
            
            // Calculate position using orbital parameters
            Quaternion rotation = Quaternion.Euler(verticalAngle, horizontalAngle, 0f);
            Vector3 direction = rotation * Vector3.back;
            transform.position = currentOrbitCenter + direction * currentDistance;
            transform.LookAt(currentOrbitCenter, Vector3.up);
            
            yield return null;
        }
        
        // Ensure exact final values - sync all target and current values
        currentOrbitCenter = homeTargetOrbitCenter;
        targetOrbitCenter = currentOrbitCenter;
        currentDistance = homeTargetDistance;
        targetDistance = currentDistance;
        horizontalAngle = targetHorizontalAngle;
        verticalAngle = targetVerticalAngle;
        this.targetHorizontalAngle = horizontalAngle;
        this.targetVerticalAngle = verticalAngle;
        
        isTransitioning = false;
        lastInputTime = Time.time;
    }
    
    private IEnumerator SmoothFocusTransition(FocusableObject focusableObject)
    {
        isTransitioning = true;
        
        // Store original state
        originalOrbitCenter = targetOrbitCenter;
        originalDistance = targetDistance;
        originalHorizontalAngle = horizontalAngle;
        originalVerticalAngle = verticalAngle;
        
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;
        Vector3 startOrbitCenter = currentOrbitCenter;
        
        // Get the transform you dragged into the field - camera goes exactly there
        Transform cameraTargetTransform = focusableObject.GetCameraTargetTransform();
        
        if (cameraTargetTransform == null)
        {
            Debug.LogWarning("No Camera Target Transform assigned to " + focusableObject.name);
            isTransitioning = false;
            yield break;
        }
        
        Vector3 targetCameraPosition = cameraTargetTransform.position;
        Quaternion targetCameraRotation = cameraTargetTransform.rotation;
        
        float elapsed = 0f;
        
        while (elapsed < focusTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / focusTransitionDuration);
            
            // Smoothly interpolate camera transform directly to the orientation transform
            transform.position = Vector3.Lerp(startPosition, targetCameraPosition, t);
            transform.rotation = Quaternion.Slerp(startRotation, targetCameraRotation, t);
            
            // Also interpolate orbit center for when we exit focus
            currentOrbitCenter = Vector3.Lerp(startOrbitCenter, focusableObject.GetFocusPosition(), t);
            
            yield return null;
        }
        
        // Camera sits exactly at the Focus transform - no movement after this
        transform.position = targetCameraPosition;
        transform.rotation = targetCameraRotation;
        
        // Store the focus transform position for rotation controls
        currentOrbitCenter = focusableObject.GetFocusPosition();
        targetOrbitCenter = currentOrbitCenter;
        
        // Initialize rotation from current camera rotation for smooth controls
        Vector3 eulerAngles = transform.rotation.eulerAngles;
        horizontalAngle = eulerAngles.y;
        verticalAngle = eulerAngles.x;
        // Handle angle wrapping for smooth interpolation
        if (verticalAngle > 180f) verticalAngle -= 360f;
        targetHorizontalAngle = horizontalAngle;
        targetVerticalAngle = verticalAngle;
        
        // Store reference to focused object
        currentFocusedObject = focusableObject;
        
        isInFocusMode = true;
        isTransitioning = false;
        lastInputTime = Time.time;
        
        // Fade non-focused objects
        FadeNonFocusedObjects(focusableObject);
    }
    
    private void UpdateFocusCameraPosition()
    {
        if (currentFocusedObject == null) return;
        
        // Keep camera at Focus transform position, but allow rotation
        Transform focusTransform = currentFocusedObject.GetCameraTargetTransform();
        if (focusTransform != null)
        {
            // Camera stays at Focus transform position
            transform.position = focusTransform.position;
            
            // Check if there's a look-at target
            Transform lookAtTarget = currentFocusedObject.GetLookAtTarget();
            if (lookAtTarget != null)
            {
                // Look at the specified target
                transform.LookAt(lookAtTarget.position, Vector3.up);
            }
            else
            {
                // Free rotation with mouse input
                horizontalAngle = Mathf.LerpAngle(horizontalAngle, targetHorizontalAngle, Time.deltaTime * rotationSmoothness);
                verticalAngle = Mathf.Lerp(verticalAngle, targetVerticalAngle, Time.deltaTime * rotationSmoothness);
                
                // Apply rotation
                transform.rotation = Quaternion.Euler(verticalAngle, horizontalAngle, 0f);
            }
        }
    }
    
    private void FadeNonFocusedObjects(FocusableObject focusedObject)
    {
        // Find all renderers in the scene
        Renderer[] allRenderers = FindObjectsOfType<Renderer>();
        
        foreach (Renderer renderer in allRenderers)
        {
            // Skip UI elements, particle systems, etc.
            if (renderer.gameObject.layer == LayerMask.NameToLayer("UI")) continue;
            if (renderer is ParticleSystemRenderer) continue;
            if (renderer is LineRenderer) continue;
            if (renderer is TrailRenderer) continue;
            
            // Check if this renderer belongs to the focused object
            bool isPartOfFocusedObject = IsChildOfObject(renderer.gameObject, focusedObject.gameObject);
            
            if (!isPartOfFocusedObject)
            {
                // Fade out non-focused objects
                StartCoroutine(FadeRenderer(renderer, 0.3f, 0.5f));
            }
        }
    }
    
    private void RestoreAllObjects()
    {
        // Find all renderers and restore them
        Renderer[] allRenderers = FindObjectsOfType<Renderer>();
        
        foreach (Renderer renderer in allRenderers)
        {
            StartCoroutine(FadeRenderer(renderer, 1f, 0.3f));
        }
    }
    
    private bool IsChildOfObject(GameObject child, GameObject parent)
    {
        Transform current = child.transform;
        while (current != null)
        {
            if (current.gameObject == parent)
                return true;
            current = current.parent;
        }
        return false;
    }
    
    private System.Collections.IEnumerator FadeRenderer(Renderer renderer, float targetAlpha, float duration)
    {
        if (renderer == null) yield break;
        
        Material[] materials = renderer.materials;
        Color[] originalColors = new Color[materials.Length];
        
        // Store original colors
        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i].HasProperty("_Color"))
            {
                originalColors[i] = materials[i].color;
            }
        }
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i].HasProperty("_Color"))
                {
                    Color color = originalColors[i];
                    color.a = Mathf.Lerp(color.a, targetAlpha, t);
                    materials[i].color = color;
                }
            }
            
            yield return null;
        }
        
        // Ensure final alpha
        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i].HasProperty("_Color"))
            {
                Color color = originalColors[i];
                color.a = targetAlpha;
                materials[i].color = color;
            }
        }
    }
    
    private IEnumerator SmoothExitFocus()
    {
        isTransitioning = true;
        
        Vector3 startPosition = transform.position;
        Vector3 startOrbitCenter = currentOrbitCenter;
        float startDistance = currentDistance;
        float startHorizontalAngle = horizontalAngle;
        float startVerticalAngle = verticalAngle;
        
        // Calculate target position from original orbital parameters
        Quaternion originalRotation = Quaternion.Euler(originalVerticalAngle, originalHorizontalAngle, 0f);
        Vector3 originalDirection = originalRotation * Vector3.back;
        Vector3 targetPosition = originalOrbitCenter + originalDirection * originalDistance;
        
        float elapsed = 0f;
        
        while (elapsed < focusTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / focusTransitionDuration);
            
            // Smoothly interpolate back to original state
            currentOrbitCenter = Vector3.Lerp(startOrbitCenter, originalOrbitCenter, t);
            currentDistance = Mathf.Lerp(startDistance, originalDistance, t);
            horizontalAngle = Mathf.LerpAngle(startHorizontalAngle, originalHorizontalAngle, t);
            verticalAngle = Mathf.Lerp(startVerticalAngle, originalVerticalAngle, t);
            
            // Calculate position using interpolated orbital parameters
            Quaternion rotation = Quaternion.Euler(verticalAngle, horizontalAngle, 0f);
            Vector3 direction = rotation * Vector3.back;
            transform.position = currentOrbitCenter + direction * currentDistance;
            transform.LookAt(currentOrbitCenter, Vector3.up);
            
            yield return null;
        }
        
        // Ensure exact final values - restore original state
        currentOrbitCenter = originalOrbitCenter;
        targetOrbitCenter = currentOrbitCenter;
        currentDistance = originalDistance;
        targetDistance = currentDistance;
        horizontalAngle = originalHorizontalAngle;
        verticalAngle = originalVerticalAngle;
        this.targetHorizontalAngle = horizontalAngle;
        this.targetVerticalAngle = verticalAngle;
        
        // Restore all objects
        RestoreAllObjects();
        
        // Clear focused object reference
        currentFocusedObject = null;
        
        isInFocusMode = false;
        isTransitioning = false;
        lastInputTime = Time.time;
    }
    
    // Public properties for external access
    public bool IsTransitioning => isTransitioning;
    public Vector3 OrbitCenter => currentOrbitCenter;
    public float CurrentDistance => currentDistance;
    public bool IsInFocusMode => isInFocusMode;
    
    void OnDrawGizmosSelected()
    {
        // Draw target transform
        if (targetTransform != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(targetTransform.position, 1f);
            Gizmos.DrawLine(transform.position, targetTransform.position);
        }
        
        // Draw home transform
        if (homeTransform != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(homeTransform.position, Vector3.one);
            Gizmos.DrawRay(homeTransform.position, homeTransform.forward * 3f);
        }
        
        // Draw current orbit center
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(currentOrbitCenter, 0.5f);
        
        // Draw zoom limits
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(currentOrbitCenter, minZoomDistance);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(currentOrbitCenter, maxZoomDistance);
    }
}