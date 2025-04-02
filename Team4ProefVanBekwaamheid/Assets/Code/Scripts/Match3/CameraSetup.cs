using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraSetup : MonoBehaviour
{
    public GridManager gridManager;
    private Camera _mainCamera;
    
    [SerializeField]
    private Camera _topCamera;
    
    [SerializeField]
    private float _bottomHalfHeight = 0.5f;

    private void Start()
    {
        _mainCamera = GetComponent<Camera>();
        AdjustCameras();
    }

    private void AdjustCameras()
    {
        if (gridManager == null) return;

        // Set up bottom camera (main camera)
        _mainCamera.rect = new Rect(0, 0, 1, _bottomHalfHeight);
        
        // Position bottom camera to center of grid
        float x = (gridManager.gridWidth - 1) * 0.5f;
        float y = (gridManager.gridHeight - 1) * 0.5f;
        transform.position = new Vector3(x, y, -10f);

        // Set up top camera viewport only
        //_topCamera.rect = new Rect(0, _bottomHalfHeight, 1, 1 - _bottomHalfHeight);
        
        // Adjust orthographic size for bottom camera only
        float aspectRatio = (Screen.width / (float)Screen.height) * (1f / _bottomHalfHeight);
        float gridAspectRatio = gridManager.gridWidth / (float)gridManager.gridHeight;
        
        if (gridAspectRatio > aspectRatio)
        {
            _mainCamera.orthographicSize = gridManager.gridWidth / (2f * aspectRatio);
        }
        else
        {
            _mainCamera.orthographicSize = gridManager.gridHeight / 2f;
        }

        // Add padding to bottom camera
        _mainCamera.orthographicSize *= 1.1f;
    }
}