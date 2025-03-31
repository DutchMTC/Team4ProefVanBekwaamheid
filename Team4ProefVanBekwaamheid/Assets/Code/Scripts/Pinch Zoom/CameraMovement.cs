using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    private Camera topCamera;

    void Start()
    {
        topCamera = GetComponent<Camera>();
    }
    
    public void CameraZoom(float increment)
    {
        topCamera.orthographicSize = Mathf.Clamp(topCamera.orthographicSize + increment, 1, 4.4f);
    } 
}