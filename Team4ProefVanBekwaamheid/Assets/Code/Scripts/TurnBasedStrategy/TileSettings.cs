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
        Obstacle,
        Item // Added Item type
    }
    public GameObject tileOccupant { get; private set; } // Made setter private, managed by SetOccupant

    // Settings
    public OccupantType occupantType { get; private set; } // Made setter private, managed by SetOccupant
    public int gridX; // Renamed from column
    public int gridY; // Renamed from row
    internal UnityEvent OccupationChangedEvent;
    private Material _tileMaterial;
    private Color _defaultTileColor = new Color(1.0f, 1.0f, 1.0f, 0.2f);
    private Color _occupiedTileColor = new Color(1.0f, 0f, 0f, 0.5f);
    private Color _playerTileColor = new Color(0f, 0f, 1.0f, 0.5f);
    private Color _itemTileColor = new Color(0f, 1.0f, 0f, 0.5f); // Color for items

    public override bool Equals(object other)
    {
        if (other is TileSettings otherTile)
        {
            return gridY == otherTile.gridY && gridX == otherTile.gridX;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return gridY.GetHashCode() ^ gridX.GetHashCode();
    }
    
    public void Initzialize(OccupantType initialOccupantType, int initialGridX, int initialGridY, GameObject initialOccupant = null)
    {
        this.gridX = initialGridX;
        this.gridY = initialGridY;
        SetOccupant(initialOccupantType, initialOccupant); // Use SetOccupant for initialization
    }

    public void SetOccupant(OccupantType newOccupantType, GameObject newOccupant)
    {
        if (this.occupantType != newOccupantType || this.tileOccupant != newOccupant)
        {
            this.occupantType = newOccupantType;
            this.tileOccupant = newOccupant;
            OccupationChangedEvent?.Invoke();
        }
    }

    void Start()
    {
        OccupationChangedEvent = new UnityEvent();
        OccupationChangedEvent.AddListener(OnOccupationChange);
        _tileMaterial = GetComponent<Renderer>().material;
        _tileMaterial.color = _defaultTileColor; // Default color for empty tiles
        getObjects();        
    }

    public GameObject[] getObjects() 
    {
	    TileOccupants[] tileOccupants = FindObjectsOfType<TileOccupants>();
	    GameObject[] objects = new GameObject[tileOccupants.Length];		
        for (int i = 0; i < objects.Length; i++)
        {
            if (tileOccupants[i] == null) continue; // Skip if the TileOccupant is null
            
            // Check if the TileOccupant is on this tile
            if (tileOccupants[i].gridY != gridY || tileOccupants[i].gridX != gridX) continue; // Changed to .gridY and .gridX
            
            // If it is, set tileOccupant to be that gameobject
            // tileOccupant = tileOccupants[i].gameObject; // This should be handled by SetOccupant
            SetOccupant(tileOccupants[i].myOccupantType, tileOccupants[i].gameObject); // Corrected to use myOccupantType
            //Debug.Log(tileOccupant.name);
        }
        
        return objects;
    }

    public void OnOccupationChange()
    {
        switch (occupantType)
        {
            case OccupantType.None:
                _tileMaterial.color = _defaultTileColor;
                break;
            case OccupantType.Player:
                //_tileMaterial.color = _playerTileColor;
                break;
            case OccupantType.Enemy:
                _tileMaterial.color = _occupiedTileColor;
                break;
            case OccupantType.Obstacle:
                _tileMaterial.color = _occupiedTileColor;
                break;
            case OccupantType.Item:
                _tileMaterial.color = _itemTileColor; // Set color for Item
                break;
        }
    }

    public void SetTileColor(Color color)
    {
        _tileMaterial.color = color; // Set the tile color to the specified color
    }
}