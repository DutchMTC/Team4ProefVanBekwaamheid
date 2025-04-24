using UnityEngine;

[ExecuteAlways] // Optional: Run in Edit mode too
public class UniformScaleMaintainer : MonoBehaviour
{
    [Tooltip("The desired uniform world scale for this object.")]
    public Vector3 targetWorldScale = Vector3.one * 0.01f; // Adjust this base scale as needed

    private Transform parentTransform;

    void Awake()
    {
        parentTransform = transform.parent;
    }

    void LateUpdate()
    {
        ApplyScale();
    }

    #if UNITY_EDITOR
    void Update() {
        // Ensure it updates in edit mode if ExecuteAlways is enabled and parent changes
        if (!Application.isPlaying) {
            if (parentTransform != transform.parent) {
                parentTransform = transform.parent;
            }
            ApplyScale();
        }
    }
    #endif


    void ApplyScale()
    {
        if (parentTransform == null)
        {
            // No parent, just apply target scale directly as local scale
            transform.localScale = targetWorldScale;
            return;
        }

        // Get parent's world scale
        Vector3 parentWorldScale = parentTransform.lossyScale;

        // Calculate the required local scale to achieve the target world scale
        Vector3 requiredLocalScale;
        requiredLocalScale.x = targetWorldScale.x / Mathf.Max(0.0001f, parentWorldScale.x); // Avoid division by zero
        requiredLocalScale.y = targetWorldScale.y / Mathf.Max(0.0001f, parentWorldScale.y);
        requiredLocalScale.z = targetWorldScale.z / Mathf.Max(0.0001f, parentWorldScale.z);

        // Apply the calculated local scale
        transform.localScale = requiredLocalScale;
    }
}