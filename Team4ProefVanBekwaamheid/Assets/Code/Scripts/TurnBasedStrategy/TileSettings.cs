using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TileSettings : MonoBehaviour
{
    public enum OccupantType
    {
        None,
        Player,
        Enemy,
        Obstacle
    }
    
    // Settings
    public OccupantType occupantType;
    public int column;
    public int row;
    internal UnityEvent OccupationChangedEvent;
    private Material _tileMaterial;
    private Color _defaultTileColor = new Color(1.0f, 1.0f, 1.0f, 0.2f);
    private Color _occupiedTileColor = new Color(1.0f, 0f, 0f, 0.5f);
    private Color _playerTileColor = new Color(0f, 0f, 1.0f, 0.5f);

    public override bool Equals(object other)
    {
        if (other is TileSettings otherTile)
        {
            return row == otherTile.row && column == otherTile.column;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return row.GetHashCode() ^ column.GetHashCode();
    }
    
    public void Initzialize(OccupantType occupantType, int column, int row)
    {
        this.column = column;
        this.row = row;
        this.occupantType = occupantType;
    }

    void Start()
    {
        OccupationChangedEvent = new UnityEvent();
        OccupationChangedEvent.AddListener(OnOccupationChange);
        _tileMaterial = GetComponent<Renderer>().material;
        _tileMaterial.color = _defaultTileColor; // Default color for empty tiles
    }

    public void OnOccupationChange()
    {
        switch (occupantType)
        {
            case OccupantType.None:
                _tileMaterial.color = _defaultTileColor; // Default color for empty tiles
                break;
            case OccupantType.Player:
                _tileMaterial.color = _playerTileColor; // Default color for player tiles
                break;
            case OccupantType.Enemy:
                _tileMaterial.color = _occupiedTileColor; // Color for Occupied tiles
                break;
            case OccupantType.Obstacle:
                _tileMaterial.color = _occupiedTileColor; // Color for Occupied tiles
                break;
        }
    }

    public void SetTileColor(Color color)
    {
        _tileMaterial.color = color; // Set the tile color to the specified color
    }
}