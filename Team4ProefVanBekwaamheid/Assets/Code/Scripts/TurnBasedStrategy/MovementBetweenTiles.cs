using UnityEngine;

public class MovementBetweenTiles : MonoBehaviour
{
    [SerializeField]
    private Camera topCamera;  // Reference to the top camera
    private GameObject selectedTile; // The tile that was selected by the player
    
    private TileSettings tileSettings; // Reference to the TileSettings script
    [SerializeField] private TileOccupants tileOccupants; // Reference to the TileOccupants script

    void Start()
    {
        tileOccupants = this.gameObject.GetComponent<TileOccupants>();
    }

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
                selectedTile = hit.collider.gameObject;               
                tileSettings = selectedTile.GetComponent<TileSettings>();

                if (!tileSettings.occupied)
                {
                    tileOccupants.row = tileSettings.row;
                    tileOccupants.column = tileSettings.column;
                }
                else
                {
                    Debug.Log($"Selected tile is occupied by: {tileSettings.occupantType} , cannot move here.");
                }

                // Visual debug to see the ray
                Debug.DrawLine(ray.origin, hit.point, Color.red, 1f);
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
                    selectedTile = hit.collider.gameObject;


                    Debug.Log("Touch hit object: " + selectedTile.name + " at position: " + hit.point);
                }
            }
        }
    }
}
