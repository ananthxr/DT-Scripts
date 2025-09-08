using UnityEngine;
using System.Collections;

public class CinematicCameraController : MonoBehaviour
{
    [Header("Transforms")]
    [SerializeField] private Transform homeTransform;
    [SerializeField] private Transform targetTransform;
    [SerializeField] private float homeTransitionDuration = 2f;

    [Header("Orbit Settings")]
    [SerializeField] private float orbitDistance = 15f;
    [SerializeField] private float minOrbitDistance = 3f;
    [SerializeField] private float maxOrbitDistance = 50f;

    [Header("Input Sensitivity")]
    [SerializeField] private float orbitSensitivity = 2f;
    [SerializeField] private float panSensitivity = 1f;
    [SerializeField] private float zoomSensitivity = 3f;

    [Header("Smoothing")]
    [SerializeField] private float orbitSmoothing = 8f;
    [SerializeField] private float panSmoothing = 5f;
    [SerializeField] private float zoomSmoothing = 10f;

    [Header("Auto Rotation")]
    [SerializeField] private bool enableAutoRotation = true;
    [SerializeField] private float autoRotationSpeed = 5f;
    [SerializeField] private float idleTimeBeforeAutoRotation = 4f;

    // Camera state
    private float horizontalAngle = 0f;
    private float verticalAngle = 20f;
    private Vector3 currentOrbitCenter;
    private float currentOrbitDistance;
    
    // Input tracking
    private bool isRightMouseDown = false;
    private float lastInputTime;
    
    // Smoothing
    private float targetHorizontalAngle;
    private float targetVerticalAngle;
    private float targetOrbitDistance;
    private Vector3 targetOrbitCenter;
    
    // State management
    private bool isTransitioning = false;
    private bool isAtHome = false;

    void Start()
    {
        InitializeCamera();
    }

    void Update()
    {
        HandleInput();
        UpdateCameraPosition();
        HandleAutoRotation();
    }

    private void InitializeCamera()
    {
        // Always start at home
        if (homeTransform != null)
        {
            transform.position = homeTransform.position;
            transform.rotation = homeTransform.rotation;
            isAtHome = true;
        }

        // Set orbit center to target transform at HOME height
        if (targetTransform != null && homeTransform != null)
        {
            Vector3 targetPos = targetTransform.position;
            Vector3 homePos = homeTransform.position;
            currentOrbitCenter = new Vector3(targetPos.x, homePos.y, targetPos.z);
            targetOrbitCenter = currentOrbitCenter;
        }
        
        // Set initial distance
        currentOrbitDistance = orbitDistance;
        targetOrbitDistance = orbitDistance;
        
        targetHorizontalAngle = horizontalAngle;
        targetVerticalAngle = verticalAngle;
        
        lastInputTime = Time.time;
    }

    private void HandleInput()
    {
        // ESC to return home
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ReturnHome();
            return;
        }

        // Update orbit center from target transform at HOME height
        if (targetTransform != null && homeTransform != null)
        {
            Vector3 targetPos = targetTransform.position;
            Vector3 homePos = homeTransform.position;
            targetOrbitCenter = new Vector3(targetPos.x, homePos.y, targetPos.z);
        }

        // Right mouse button for panning
        if (Input.GetMouseButtonDown(1))
        {
            isRightMouseDown = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else if (Input.GetMouseButtonUp(1))
        {
            isRightMouseDown = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        bool hasInput = false;

        // Left mouse drag for orbit rotation
        if (Input.GetMouseButton(0) && !isRightMouseDown && !isTransitioning)
        {
            ExitHomeMode();
            
            float mouseX = Input.GetAxis("Mouse X") * orbitSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * orbitSensitivity;

            targetHorizontalAngle += mouseX;
            targetVerticalAngle -= mouseY;
            targetVerticalAngle = Mathf.Clamp(targetVerticalAngle, -80f, 80f);
            
            hasInput = true;
        }

        // Right mouse drag for panning (just moves around target)
        if (isRightMouseDown && !isTransitioning)
        {
            ExitHomeMode();
            
            float mouseX = Input.GetAxis("Mouse X") * panSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * panSensitivity;

            // Simple pan - just adjust the angles like orbiting
            targetHorizontalAngle += mouseX * 0.5f;
            targetVerticalAngle -= mouseY * 0.5f;
            targetVerticalAngle = Mathf.Clamp(targetVerticalAngle, -80f, 80f);
            
            hasInput = true;
        }

        // Mouse scroll for zoom
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f && !isTransitioning)
        {
            ExitHomeMode();
            
            targetOrbitDistance -= scroll * zoomSensitivity;
            targetOrbitDistance = Mathf.Clamp(targetOrbitDistance, minOrbitDistance, maxOrbitDistance);
            
            hasInput = true;
        }

        // Update input timer for auto rotation
        if (hasInput)
        {
            lastInputTime = Time.time;
        }
    }

    private void ExitHomeMode()
    {
        if (isAtHome)
        {
            isAtHome = false;
            
            // Use target position but keep HOME height
            if (targetTransform != null && homeTransform != null)
            {
                Vector3 targetPos = targetTransform.position;
                Vector3 homePos = homeTransform.position;
                
                // Target position but at HOME height
                currentOrbitCenter = new Vector3(targetPos.x, homePos.y, targetPos.z);
                targetOrbitCenter = currentOrbitCenter;
            }
            
            // Start with basic angles - no calculations
            horizontalAngle = 0f;
            verticalAngle = 0f; // Keep level with home height
            currentOrbitDistance = orbitDistance;
            
            targetHorizontalAngle = horizontalAngle;
            targetVerticalAngle = verticalAngle;
            targetOrbitDistance = currentOrbitDistance;
        }
    }

    private void UpdateCameraPosition()
    {
        if (isTransitioning || isAtHome) return;

        // Smooth angle transitions
        horizontalAngle = Mathf.LerpAngle(horizontalAngle, targetHorizontalAngle, Time.deltaTime * orbitSmoothing);
        verticalAngle = Mathf.Lerp(verticalAngle, targetVerticalAngle, Time.deltaTime * orbitSmoothing);

        // Smooth distance transition
        currentOrbitDistance = Mathf.Lerp(currentOrbitDistance, targetOrbitDistance, Time.deltaTime * zoomSmoothing);

        // Smooth center transition - follow target but keep HOME height
        if (targetTransform != null && homeTransform != null)
        {
            Vector3 targetPos = targetTransform.position;
            Vector3 homePos = homeTransform.position;
            Vector3 targetAtHomeHeight = new Vector3(targetPos.x, homePos.y, targetPos.z);
            currentOrbitCenter = Vector3.Lerp(currentOrbitCenter, targetAtHomeHeight, Time.deltaTime * panSmoothing);
        }

        // Calculate camera position around target
        Quaternion rotation = Quaternion.Euler(verticalAngle, horizontalAngle, 0f);
        Vector3 direction = rotation * Vector3.back;
        Vector3 targetPosition = currentOrbitCenter + direction * currentOrbitDistance;

        // Apply position and look at target
        transform.position = targetPosition;
        transform.LookAt(currentOrbitCenter, Vector3.up);
    }

    private void HandleAutoRotation()
    {
        if (!enableAutoRotation || isTransitioning || isAtHome) return;
        if (isRightMouseDown) return;

        float timeSinceInput = Time.time - lastInputTime;
        if (timeSinceInput > idleTimeBeforeAutoRotation)
        {
            targetHorizontalAngle += autoRotationSpeed * Time.deltaTime;
        }
    }

    public void ReturnHome()
    {
        if (homeTransform != null && !isTransitioning)
        {
            StartCoroutine(SmoothReturnHome());
        }
    }

    private IEnumerator SmoothReturnHome()
    {
        isTransitioning = true;
        
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        
        float elapsed = 0f;
        
        while (elapsed < homeTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / homeTransitionDuration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            
            transform.position = Vector3.Lerp(startPos, homeTransform.position, smoothT);
            transform.rotation = Quaternion.Slerp(startRot, homeTransform.rotation, smoothT);
            
            yield return null;
        }
        
        transform.position = homeTransform.position;
        transform.rotation = homeTransform.rotation;
        
        isAtHome = true;
        isTransitioning = false;
        lastInputTime = Time.time;
    }

    // Public properties for external access
    public bool IsTransitioning => isTransitioning;
    public bool IsAtHome => isAtHome;

    void OnDrawGizmosSelected()
    {
        // Draw target
        if (targetTransform != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(targetTransform.position, 0.5f);
            
            // Draw orbit sphere around target
            Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
            Gizmos.DrawWireSphere(targetTransform.position, orbitDistance);
            
            // Draw camera to target line
            if (!isAtHome)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, targetTransform.position);
            }
        }
        
        // Draw home transform
        if (homeTransform != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(homeTransform.position, Vector3.one);
            Gizmos.DrawRay(homeTransform.position, homeTransform.forward * 3f);
        }
    }
}