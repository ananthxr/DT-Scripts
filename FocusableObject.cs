using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class FocusableObject : MonoBehaviour
{
    [Header("Focus Settings")]
    [SerializeField] private Vector3 focusOffset = Vector3.zero;
    [SerializeField] private float focusDistance = 10f;
    [SerializeField] private float focusHeight = 5f;
    [SerializeField] private bool autoCalculateBounds = true;
    
    [Header("Camera Orientation")]
    [SerializeField] private Transform cameraOrientationTransform;
    [SerializeField] private bool useCustomOrientation = true;
    [Tooltip("If no transform assigned, use these angles. X=Vertical, Y=Horizontal, Z=unused")]
    [SerializeField] private Vector3 cameraAngles = new Vector3(30f, 45f, 0f);
    
    [Header("Visual Feedback")]
    [SerializeField] private Color hoverColor = new Color(1f, 1f, 1f, 0.2f);
    [SerializeField] private float hoverIntensity = 1.2f;
    
    private Renderer[] renderers;
    private Material[] originalMaterials;
    private Material[] hoverMaterials;
    private bool isHovered = false;
    private bool isFocused = false;
    private Bounds combinedBounds;

    void Start()
    {
        InitializeMaterials();
        CalculateBounds();
    }

    private void InitializeMaterials()
    {
        renderers = GetComponentsInChildren<Renderer>();
        List<Material> originals = new List<Material>();
        List<Material> hovers = new List<Material>();

        foreach (var renderer in renderers)
        {
            foreach (var material in renderer.materials)
            {
                originals.Add(material);
                
                Material hoverMat = new Material(material);
                if (hoverMat.HasProperty("_Color"))
                {
                    Color baseColor = hoverMat.color;
                    hoverMat.color = baseColor * hoverIntensity;
                }
                if (hoverMat.HasProperty("_EmissionColor"))
                {
                    hoverMat.EnableKeyword("_EMISSION");
                    hoverMat.SetColor("_EmissionColor", hoverColor);
                }
                hovers.Add(hoverMat);
            }
        }

        originalMaterials = originals.ToArray();
        hoverMaterials = hovers.ToArray();
    }

    private void CalculateBounds()
    {
        if (!autoCalculateBounds) return;

        combinedBounds = new Bounds(transform.position, Vector3.zero);
        foreach (var renderer in renderers)
        {
            combinedBounds.Encapsulate(renderer.bounds);
        }

        if (focusDistance == 10f)
            focusDistance = combinedBounds.size.magnitude * 1.5f;
    }

    void OnMouseEnter()
    {
        RTSCameraController cameraController = FindObjectOfType<RTSCameraController>();
        if (!isFocused && (cameraController == null || !cameraController.IsTransitioning))
        {
            SetHovered(true);
        }
    }

    void OnMouseExit()
    {
        if (!isFocused)
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
        if (isHovered == hovered) return;
        
        isHovered = hovered;
        
        int materialIndex = 0;
        foreach (var renderer in renderers)
        {
            Material[] materials = new Material[renderer.materials.Length];
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = hovered ? hoverMaterials[materialIndex] : originalMaterials[materialIndex];
                materialIndex++;
            }
            renderer.materials = materials;
        }
    }

    public void SetFocused(bool focused)
    {
        isFocused = focused;
        if (focused)
        {
            SetHovered(false);
        }
    }

    public Vector3 GetFocusPosition()
    {
        return autoCalculateBounds ? combinedBounds.center + focusOffset : transform.position + focusOffset;
    }

    public float GetFocusDistance()
    {
        return focusDistance;
    }

    public float GetFocusHeight()
    {
        return focusHeight;
    }

    public Bounds GetBounds()
    {
        return autoCalculateBounds ? combinedBounds : new Bounds(transform.position, Vector3.one);
    }

    public Vector3 GetCameraAngles()
    {
        if (useCustomOrientation)
        {
            if (cameraOrientationTransform != null)
            {
                Vector3 eulerAngles = cameraOrientationTransform.eulerAngles;
                return new Vector3(eulerAngles.x, eulerAngles.y, eulerAngles.z);
            }
            return cameraAngles;
        }
        
        // Default behavior - look at object from current camera direction
        Vector3 directionToTarget = (Camera.main.transform.position - GetFocusPosition()).normalized;
        float horizontalAngle = Mathf.Atan2(directionToTarget.x, directionToTarget.z) * Mathf.Rad2Deg;
        float verticalAngle = Mathf.Asin(directionToTarget.y) * Mathf.Rad2Deg;
        return new Vector3(verticalAngle, horizontalAngle, 0f);
    }

    public Vector3 GetCameraPosition()
    {
        if (cameraOrientationTransform != null && useCustomOrientation)
        {
            // Return the exact position of the cameraOrientationTransform
            return cameraOrientationTransform.position;
        }
        
        // Fallback to calculated position
        Vector3 focusPos = GetFocusPosition();
        Vector3 angles = GetCameraAngles();
        
        float horizontalRad = angles.y * Mathf.Deg2Rad;
        float verticalRad = angles.x * Mathf.Deg2Rad;
        
        Vector3 calculatedDirection = new Vector3(
            Mathf.Sin(horizontalRad) * Mathf.Cos(verticalRad),
            Mathf.Sin(verticalRad),
            Mathf.Cos(horizontalRad) * Mathf.Cos(verticalRad)
        );
        
        return focusPos + calculatedDirection * focusDistance;
    }
    
    public Quaternion GetCameraRotation()
    {
        if (cameraOrientationTransform != null && useCustomOrientation)
        {
            // Return the exact rotation of the cameraOrientationTransform
            return cameraOrientationTransform.rotation;
        }
        
        // Fallback to looking at focus position
        Vector3 focusPos = GetFocusPosition();
        Vector3 cameraPos = GetCameraPosition();
        return Quaternion.LookRotation((focusPos - cameraPos).normalized);
    }
    
    public Transform GetCameraOrientationTransform()
    {
        return cameraOrientationTransform;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 focusPos = GetFocusPosition();
        Gizmos.DrawWireSphere(focusPos, 0.5f);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(focusPos, focusDistance);
        
        // Draw camera orientation
        if (useCustomOrientation)
        {
            Vector3 cameraPos = GetCameraPosition();
            Gizmos.color = Color.red;
            Gizmos.DrawLine(focusPos, cameraPos);
            Gizmos.DrawWireCube(cameraPos, Vector3.one * 0.5f);
            
            if (cameraOrientationTransform != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(cameraOrientationTransform.position, cameraOrientationTransform.forward * 2f);
            }
        }
        
        if (autoCalculateBounds)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(combinedBounds.center, combinedBounds.size);
        }
    }
}