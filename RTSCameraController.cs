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
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private float homeTransitionDuration = 2f;
    
    [Header("Auto Rotation")]
    [SerializeField] private bool enableAutoRotation = true;
    [SerializeField] private float autoRotationSpeed = 10f;
    [SerializeField] private float idleTimeBeforeAutoRotation = 3f;
    
    // Current state
    private Vector3 currentOrbitCenter;
    private float currentDistance;
    private float horizontalAngle;
    private float verticalAngle;
    
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
        
        // Calculate initial distance and angles based on home position
        Vector3 directionFromTarget = transform.position - currentOrbitCenter;
        currentDistance = directionFromTarget.magnitude;
        
        // Calculate angles from the direction
        horizontalAngle = Mathf.Atan2(directionFromTarget.x, directionFromTarget.z) * Mathf.Rad2Deg;
        verticalAngle = Mathf.Asin(directionFromTarget.normalized.y) * Mathf.Rad2Deg;
        
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
            currentDistance -= scroll * zoomSensitivity;
            currentDistance = Mathf.Clamp(currentDistance, minZoomDistance, maxZoomDistance);
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
        
        // Right mouse button for panning
        if (Input.GetMouseButtonDown(1))
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
            
            horizontalAngle += mouseDelta.x * orbitSensitivity * 0.1f;
            verticalAngle -= mouseDelta.y * orbitSensitivity * 0.1f;
            verticalAngle = Mathf.Clamp(verticalAngle, minVerticalAngle, maxVerticalAngle);
            
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
            currentOrbitCenter += panMovement;
            
            lastMousePosition = Input.mousePosition;
            hasUserInput = true;
        }
    }
    
    private void UpdateCameraPosition()
    {
        // Calculate desired position based on angles and distance
        Quaternion rotation = Quaternion.Euler(verticalAngle, horizontalAngle, 0f);
        Vector3 direction = rotation * Vector3.back;
        Vector3 desiredPosition = currentOrbitCenter + direction * currentDistance;
        
        // Smoothly move to desired position
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * smoothSpeed);
        
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
            horizontalAngle += autoRotationSpeed * Time.deltaTime;
        }
    }
    
    public void FocusOnObject(Vector3 focusPosition, float focusDistance = 8f)
    {
        if (isTransitioning) return;
        
        // Store original state
        originalOrbitCenter = currentOrbitCenter;
        originalDistance = currentDistance;
        originalHorizontalAngle = horizontalAngle;
        originalVerticalAngle = verticalAngle;
        
        // Set focus state
        isInFocusMode = true;
        currentOrbitCenter = focusPosition;
        currentDistance = focusDistance;
        
        // Reset input timer
        lastInputTime = Time.time;
    }
    
    public void ExitFocus()
    {
        if (!isInFocusMode) return;
        
        // Restore original state
        currentOrbitCenter = originalOrbitCenter;
        currentDistance = originalDistance;
        horizontalAngle = originalHorizontalAngle;
        verticalAngle = originalVerticalAngle;
        
        isInFocusMode = false;
        lastInputTime = Time.time;
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
        float targetDistance = directionFromTarget.magnitude;
        float targetHorizontalAngle = Mathf.Atan2(directionFromTarget.x, directionFromTarget.z) * Mathf.Rad2Deg;
        float targetVerticalAngle = Mathf.Asin(directionFromTarget.normalized.y) * Mathf.Rad2Deg;
        
        float elapsed = 0f;
        
        while (elapsed < homeTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / homeTransitionDuration);
            
            // Smoothly interpolate all camera parameters
            currentOrbitCenter = Vector3.Lerp(startOrbitCenter, homeTargetOrbitCenter, t);
            currentDistance = Mathf.Lerp(startDistance, targetDistance, t);
            horizontalAngle = Mathf.LerpAngle(startHorizontalAngle, targetHorizontalAngle, t);
            verticalAngle = Mathf.Lerp(startVerticalAngle, targetVerticalAngle, t);
            
            // Calculate position using orbital parameters
            Quaternion rotation = Quaternion.Euler(verticalAngle, horizontalAngle, 0f);
            Vector3 direction = rotation * Vector3.back;
            transform.position = currentOrbitCenter + direction * currentDistance;
            transform.LookAt(currentOrbitCenter, Vector3.up);
            
            yield return null;
        }
        
        // Ensure exact final values
        currentOrbitCenter = homeTargetOrbitCenter;
        currentDistance = targetDistance;
        horizontalAngle = targetHorizontalAngle;
        verticalAngle = targetVerticalAngle;
        
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