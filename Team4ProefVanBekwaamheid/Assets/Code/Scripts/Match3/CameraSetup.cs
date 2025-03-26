using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraSetup : MonoBehaviour
{
    public GridManager gridManager;
    private Camera mainCamera;
    
    [SerializeField]
    private Camera topCamera;
    
    [SerializeField]
    private float bottomHalfHeight = 0.5f;

    private void Start()
    {
        mainCamera = GetComponent<Camera>();
        AdjustCameras();
    }

    private void AdjustCameras()
    {
        if (gridManager == null) return;

        // Set up bottom camera (main camera)
        mainCamera.rect = new Rect(0, 0, 1, bottomHalfHeight);
        
        // Position bottom camera to center of grid
        float x = (gridManager.gridWidth - 1) * 0.5f;
        float y = (gridManager.gridHeight - 1) * 0.5f;
        transform.position = new Vector3(x, y, -10f);
        
        // Adjust orthographic size for bottom camera only
        float aspectRatio = (Screen.width / (float)Screen.height) * (1f / bottomHalfHeight);
        float gridAspectRatio = gridManager.gridWidth / (float)gridManager.gridHeight;
        
        if (gridAspectRatio > aspectRatio)
        {
            mainCamera.orthographicSize = gridManager.gridWidth / (2f * aspectRatio);
        }
        else
        {
            mainCamera.orthographicSize = gridManager.gridHeight / 2f;
        }

        // Add padding to bottom camera
        mainCamera.orthographicSize *= 1.1f;
    }
}