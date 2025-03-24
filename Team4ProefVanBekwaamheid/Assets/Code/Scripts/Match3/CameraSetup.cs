using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraSetup : MonoBehaviour
{
    public GridManager gridManager;
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = GetComponent<Camera>();
        AdjustCamera();
    }

    private void AdjustCamera()
    {
        if (gridManager == null) return;

        // Position camera to center of grid
        float x = (gridManager.gridWidth - 1) * 0.5f;
        float y = (gridManager.gridHeight - 1) * 0.5f;
        transform.position = new Vector3(x, y, -10f);

        // Adjust orthographic size to fit grid
        float aspectRatio = Screen.width / (float)Screen.height;
        float gridAspectRatio = gridManager.gridWidth / (float)gridManager.gridHeight;
        
        if (gridAspectRatio > aspectRatio)
        {
            mainCamera.orthographicSize = gridManager.gridWidth / (2f * aspectRatio);
        }
        else
        {
            mainCamera.orthographicSize = gridManager.gridHeight / 2f;
        }

        // Add padding
        mainCamera.orthographicSize *= 1.1f;
    }
}