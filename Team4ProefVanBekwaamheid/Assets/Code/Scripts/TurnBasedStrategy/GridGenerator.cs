using UnityEngine;
using System.Collections.Generic; // Add this line for using List

public class GridGenerator : MonoBehaviour
{
    public GameObject tilePrefab;

    [SerializeField] private int _width;
    [SerializeField] private int _height;
    [SerializeField] private float _tileWidth;
    [SerializeField] private float _tileHeight;
    [SerializeField] private float _gridHeight;
    [SerializeField] private float _horizontalOffset;

    public int width
    {
        get => _width;
        set
        {
            if (_width != value)
            {
                _width = value;
                if (Application.isPlaying) RegenerateGrid();
            }
        }
    }

    public int height
    {
        get => _height;
        set
        {
            if (_height != value)
            {
                _height = value;
                if (Application.isPlaying) RegenerateGrid();
            }
        }
    }

    public float tileWidth
    {
        get => _tileWidth;
        set
        {
            if (_tileWidth != value)
            {
                _tileWidth = value;
                if (Application.isPlaying) RegenerateGrid();
            }
        }
    }

    public float tileHeight
    {
        get => _tileHeight;
        set
        {
            if (_tileHeight != value)
            {
                _tileHeight = value;
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
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                // Convert grid coordinates to isometric space
                float isometricX = (x - y) * _tileWidth * 0.5f + _horizontalOffset;
                float isometricZ = (x + y) * _tileHeight * 0.5f;
                
                Vector3 tilePosition = new Vector3(isometricX, _gridHeight, isometricZ);
                GameObject tile = Instantiate(tilePrefab, tilePosition, Quaternion.Euler(0, 45, 0));
                
                tile.GetComponent<TileSettings>().Initzialize(TileSettings.OccupantType.None, x, y, null); // Initialize tile settings with null occupant
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

    public List<TileSettings> GetAllTiles() // Add this method
    {
        var tiles = new List<TileSettings>();
        foreach (Transform child in transform)
        {
            var tile = child.GetComponent<TileSettings>();
            if (tile != null)
            {
                tiles.Add(tile);
            }
        }
        return tiles;
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