using UnityEngine;
using UnityEngine.Events;

public class GridGenerator : MonoBehaviour
{
    public GameObject tilePrefab;

    [SerializeField] private int width = 10;
    [SerializeField] private int height = 10;
    [SerializeField] private float tileWidth = 1f;
    [SerializeField] private float tileHeight = 0.5f;

    public int _width
    {
        get => width;
        set
        {
            if (width != value)
            {
                width = value;
                if (Application.isPlaying) RegenerateGrid();
            }
        }
    }

    public int _height
    {
        get => height;
        set
        {
            if (height != value)
            {
                height = value;
                if (Application.isPlaying) RegenerateGrid();
            }
        }
    }

    public float _tileWidth
    {
        get => tileWidth;
        set
        {
            if (tileWidth != value)
            {
                tileWidth = value;
                if (Application.isPlaying) RegenerateGrid();
            }
        }
    }

    public float _tileHeight
    {
        get => tileHeight;
        set
        {
            if (tileHeight != value)
            {
                tileHeight = value;
                if (Application.isPlaying) RegenerateGrid();
            }
        }
    }

    void Start()
    {
        GenerateGrid();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
            GenerateGrid();
        }
    }

    void GenerateGrid()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Convert grid coordinates to isometric space
                float isometricX = (x - y) * tileWidth * 0.5f + 3.5f;
                float isometricZ = (x + y) * tileHeight * 0.5f;
                
                Vector3 tilePosition = new Vector3(isometricX, 23, isometricZ);
                GameObject tile = Instantiate(tilePrefab, tilePosition, Quaternion.Euler(0, 45, 0));
                
                tile.GetComponent<TileSettings>().Initzialize(TileSettings.OccupantType.None, x, y); // Initialize tile settings
                tile.transform.parent = transform;  // Keep hierarchy organized
            }
        }
    }

    private void RegenerateGrid()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        GenerateGrid();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            RegenerateGrid();
        }
    }
#endif
}