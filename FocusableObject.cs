using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FocusableObject : MonoBehaviour
{
    [Header("Focus Settings")]
    [SerializeField] private Transform cameraTargetTransform;
    [Tooltip("Drag and drop the transform where you want the camera to go")]
    [SerializeField] private Transform lookAtTarget;
    [Tooltip("Optional: Transform for camera to look at while focused. Leave empty for free rotation.")]
    
    [Header("Visual Feedback")]
    [SerializeField] private Color hoverColor = Color.yellow;
    [SerializeField] private bool enableHover = true;
    
    private bool isHovered = false;
    private Material[] originalMaterials;
    private Renderer[] renderers;

    void Start()
    {
        SetupMaterials();
    }
    
    private void SetupMaterials()
    {
        renderers = GetComponentsInChildren<Renderer>();
        originalMaterials = new Material[renderers.Length];
        
        for (int i = 0; i < renderers.Length; i++)
        {
            originalMaterials[i] = renderers[i].material;
        }
    }

    void OnMouseEnter()
    {
        if (enableHover && !isHovered)
        {
            RTSCameraController cameraController = FindObjectOfType<RTSCameraController>();
            if (cameraController != null && !cameraController.IsTransitioning)
            {
                SetHovered(true);
            }
        }
    }

    void OnMouseExit()
    {
        if (isHovered)
        {
            SetHovered(false);
        }
    }

    void OnMouseDown()
    {
        if (Input.GetMouseButtonDown(0))
        {
            RTSCameraController cameraController = FindObjectOfType<RTSCameraController>();
            if (cameraController != null && !cameraController.IsTransitioning)
            {
                cameraController.FocusOnObject(this);
            }
        }
    }
    
    private void SetHovered(bool hovered)
    {
        isHovered = hovered;
        
        for (int i = 0; i < renderers.Length; i++)
        {
            if (hovered)
            {
                Material hoverMat = new Material(originalMaterials[i]);
                hoverMat.color = hoverColor;
                renderers[i].material = hoverMat;
            }
            else
            {
                renderers[i].material = originalMaterials[i];
            }
        }
    }
    
    public Vector3 GetFocusPosition()
    {
        return transform.position;
    }
    
    public Transform GetCameraTargetTransform()
    {
        // First try the manually assigned transform
        if (cameraTargetTransform != null)
        {
            return cameraTargetTransform;
        }
        
        // If no transform assigned, find child named "Focus"
        Transform focusTransform = FindChildByName("Focus");
        if (focusTransform != null)
        {
            return focusTransform;
        }
        
        Debug.LogWarning("No camera target transform found on " + name + ". Assign one manually or create a child named 'Focus'");
        return null;
    }
    
    private Transform FindChildByName(string childName)
    {
        // Search through all children recursively to find the "Focus" transform
        return SearchChildren(transform, childName);
    }
    
    private Transform SearchChildren(Transform parent, string name)
    {
        // Check direct children first
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == name)
            {
                return child;
            }
        }
        
        // Then search grandchildren recursively
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            Transform result = SearchChildren(child, name);
            if (result != null)
            {
                return result;
            }
        }
        
        return null;
    }
    
    public Transform GetLookAtTarget()
    {
        return lookAtTarget;
    }
}