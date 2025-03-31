using UnityEngine;

public class MovementBetweenTiles : MonoBehaviour
{
    [SerializeField]
    private Camera topCamera;  // Reference to the top camera
    private Vector3 _touchStart;
    private GameObject _selectedTile;

    [SerializeField] private GameObject player; // Reference to the player object
    
    void Update()
    {
        // Handle mouse input
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = topCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            // Use a larger max distance to ensure we can hit tiles at Y=23
            if (Physics.Raycast(ray, out hit, 100f))
            {
                _selectedTile = hit.collider.gameObject;
                Debug.Log("Touched object: " + _selectedTile.name + " at position: " + hit.point);
                // Visual debug to see the ray
                Debug.DrawLine(ray.origin, hit.point, Color.red, 1f);

                // Get the tile's position and maintain the player's Y position
                Vector3 tilePos = _selectedTile.transform.position;
                player.transform.position = new Vector3(tilePos.x, player.transform.position.y, tilePos.z);
            }
            else
            {
                // Visual debug to see why ray might be missing
                Debug.DrawRay(ray.origin, ray.direction * 100f, Color.yellow, 1f);
                Debug.Log("No hit detected. Ray origin: " + ray.origin + ", direction: " + ray.direction);
            }
        }

        // Handle touch input
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            
            if (touch.phase == TouchPhase.Began)
            {
                Ray ray = topCamera.ScreenPointToRay(touch.position);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, 100f))
                {
                    _selectedTile = hit.collider.gameObject;
                    Vector3 tilePos = _selectedTile.transform.position;
                    player.transform.position = new Vector3(tilePos.x, player.transform.position.y, tilePos.z);
                    Debug.Log("Touch hit object: " + _selectedTile.name + " at position: " + hit.point);
                }
            }
        }
    }
}
