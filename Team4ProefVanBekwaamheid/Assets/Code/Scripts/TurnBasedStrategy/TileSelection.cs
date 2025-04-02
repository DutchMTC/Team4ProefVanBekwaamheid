using UnityEngine;

public class TileSelection : MonoBehaviour
{
    [SerializeField] private Camera _topCamera;  // Reference to the top camera
    private GameObject _selectedTile; // The tile that was selected by the player
    private TileSettings _tileSettings; // Reference to the TileSettings script
    private TileOccupants _tileOccupants; // Reference to the TileOccupants script

    void Start()
    {
        _tileOccupants = this.gameObject.GetComponent<TileOccupants>();
    }

    void Update()
    {
        // Handle mouse input
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = _topCamera.ScreenPointToRay(Input.mousePosition);
            SelectTile(ray);
        }

        // Handle touch input
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            
            if (touch.phase == TouchPhase.Began)
            {
                Ray ray = _topCamera.ScreenPointToRay(touch.position);
                SelectTile(ray);
            }
        }
    }

    private void SelectTile(Ray _ray)  // Remove hit parameter from method signature
    {
        RaycastHit hit; // Declare hit variable here
        // Use a larger max distance to ensure we can hit tiles at Y=23
        if (Physics.Raycast(_ray, out hit, 100f))  // Use the local hit variable
        {
            _selectedTile = hit.collider.gameObject;                               

            UpdateCoordinates(); // Update coordinates based on the selected tile

            // Visual debug to see the ray
            Debug.DrawLine(_ray.origin, hit.point, Color.red, 1f);
        }
        else
        {
            // Visual debug to see why ray might be missing
            Debug.DrawRay(_ray.origin, _ray.direction * 100f, Color.yellow, 1f);
            Debug.Log("No hit detected. Ray origin: " + _ray.origin + ", direction: " + _ray.direction);
        }
    }

    private void UpdateCoordinates()
    {
        _tileSettings = _selectedTile.GetComponent<TileSettings>();

        if (_tileSettings.occupantType.Equals(TileSettings.OccupantType.None))
        {
            _tileOccupants.row = _tileSettings.row;
            _tileOccupants.column = _tileSettings.column;
            _tileOccupants.MoveToTile(); // Move the occupant to the selected tile
        }
        else
        {
            Debug.Log($"Selected tile is occupied by: {_tileSettings.occupantType} , cannot move here.");
        }
    }
}
