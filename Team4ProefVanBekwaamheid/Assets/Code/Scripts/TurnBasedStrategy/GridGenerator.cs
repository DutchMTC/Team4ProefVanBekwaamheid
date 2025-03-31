using UnityEngine;

public class GridGenerator : MonoBehaviour
{
    public GameObject tilePrefab;

    [SerializeField] private int _width = 10;
    [SerializeField] private int _height = 10;
    [SerializeField] private float _tileWidth = 1f;
    [SerializeField] private float _tileHeight = 0.5f;

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
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Convert grid coordinates to isometric space
                float isoX = (x - y) * tileWidth * 0.5f + 3.5f;
                float isoZ = (x + y) * tileHeight * 0.5f;

                Vector3 tilePosition = new Vector3(isoX, 23, isoZ);
                GameObject tile = Instantiate(tilePrefab, tilePosition, Quaternion.Euler(0, 45, 0));
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