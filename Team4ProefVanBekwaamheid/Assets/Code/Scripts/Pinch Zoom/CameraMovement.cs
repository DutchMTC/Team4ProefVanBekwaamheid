using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [SerializeField] private float speed = 0.01f; // Speed of zoom and pan
    [SerializeField] private float minZoom = 0.1F; // Minimum zoom level
    [SerializeField] private float maxZoom = 1F; // Maximum zoom level
    [SerializeField] float minX, maxX, minY, maxY; // Camera movement bounds

    private Vector3 _touch; // Initial touch position
    private Camera _topCamera; // Camera reference

    void Start()
    {
        _topCamera = gameObject.GetComponent<Camera>(); // Get Camera component
    }

    void Update()
    {
        // Check for a input to start panning
        if (Input.GetMouseButtonDown(0) && _topCamera.pixelRect.Contains(Input.mousePosition))
        {
            StartPanning();
        }

        // Handle pinch zoom if two touches are detected
        if (Input.touchCount == 2 && _topCamera.pixelRect.Contains(Input.GetTouch(0).position) && _topCamera.pixelRect.Contains(Input.GetTouch(1).position))
        {
            HandlePinchZoom();
        }
        // Handle panning if the input is held down
        else if (Input.GetMouseButton(0) && _topCamera.pixelRect.Contains(Input.mousePosition))
        {
            HandlePanning();
        }
        
        // Handle zooming with the scroll wheel.
        HandleScrollWheelZoom();
    }

    private void StartPanning()
    {
        // Store the initial touch position in world coordinates for panning
        _touch = _topCamera.ScreenToWorldPoint(Input.mousePosition);
    }

    private void HandlePanning()
    {
        // Calculate the direction to move the camera based on touch movement
        Vector3 direction = _touch - _topCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector3 newPosition = _topCamera.transform.position + direction;

        // Clamp the new position within the defined movement bounds
        newPosition.x = Mathf.Clamp(newPosition.x, minX, maxX);
        newPosition.y = Mathf.Clamp(newPosition.y, minY, maxY);
        newPosition.z = _topCamera.transform.position.z;

        // Apply the clamped position to the camera
        _topCamera.transform.position = newPosition;
    }

    private void HandlePinchZoom()
    {
        // Get the two touch inputs for the pinch gesture
        var touchZero = Input.GetTouch(0);
        var touchOne = Input.GetTouch(1);

        // Calculate the previous and current distances between the two touches
        var prevMagnitude = (touchZero.position - touchZero.deltaPosition - (touchOne.position - touchOne.deltaPosition)).magnitude;
        var currentMagnitude = (touchZero.position - touchOne.position).magnitude;

        // Determine the difference in distances to calculate zoom amount
        var difference = currentMagnitude - prevMagnitude;

        // Find the midpoint between the two touches in screen space
        Vector2 midpoint = (touchZero.position + touchOne.position) / 2;
        Vector3 worldMidpoint = _topCamera.ScreenToWorldPoint(new Vector3(midpoint.x, midpoint.y, _topCamera.transform.position.z));

        // Adjust the camera zoom level
        CameraZoom(difference);

        // Adjust the camera position to keep the zoom centered on the midpoint
        Vector3 newWorldMidpoint = _topCamera.ScreenToWorldPoint(new Vector3(midpoint.x, midpoint.y, _topCamera.transform.position.z));
        _topCamera.transform.position += worldMidpoint - newWorldMidpoint;
    }

    private void HandleScrollWheelZoom()
    {
        CameraZoom(Input.GetAxis("Mouse ScrollWheel"));
    }

    private void CameraZoom(float increment)
    {
        // Adjust the camera's orthographic size to zoom in or out
        float zoomFactor = increment * speed;
        _topCamera.orthographicSize = Mathf.Clamp(_topCamera.orthographicSize - zoomFactor, minZoom, maxZoom);
    }
}